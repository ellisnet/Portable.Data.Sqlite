/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

//Was: namespace Mono.Data.Sqlite {
namespace Portable.Data.Sqlite {
    using System;
    //using System.Data;
    //using System.Data.Common;
    using Portable.Data;
    using Portable.Data.Common;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;

    /// <summary>
    /// SQLite implementation of DbDataReader.
    /// </summary>
    public sealed class SqliteDataReader : DbDataReader {
        /// <summary>
        /// Underlying command this reader is attached to
        /// </summary>
        private SqliteCommand _command;
        /// <summary>
        /// Index of the current statement in the command being processed
        /// </summary>
        private int _activeStatementIndex;
        /// <summary>
        /// Current statement being Read()
        /// </summary>
        private SqliteStatement _activeStatement;
        /// <summary>
        /// State of the current statement being processed.
        /// -1 = First Step() executed, so the first Read() will be ignored
        ///  0 = Actively reading
        ///  1 = Finished reading
        ///  2 = Non-row-returning statement, no records
        /// </summary>
        private int _readingState;
        /// <summary>
        /// Number of records affected by the insert/update statements executed on the command
        /// </summary>
        private int _rowsAffected;
        /// <summary>
        /// Count of fields (columns) in the row-returning statement currently being processed
        /// </summary>
        private int _fieldCount;
        /// <summary>
        /// Datatypes of active fields (columns) in the current statement, used for type-restricting data
        /// </summary>
        private SqliteType[] _fieldTypeArray;

        private long _lastInsertRowId = -1;
        internal long LastInsertRowId {
            get { return _lastInsertRowId; }
        }

        private bool _setRowId = false;
        internal bool SetRowId {
            get { return _setRowId; }
            set { _setRowId = value; }
        }


        /// <summary>
        /// The behavior of the datareader
        /// </summary>
        private CommandBehavior _commandBehavior;

        /// <summary>
        /// If set, then dispose of the command object when the reader is finished
        /// </summary>
        internal bool _disposeCommand;

        /// <summary>
        /// An array of rowid's for the active statement if CommandBehavior.KeyInfo is specified
        /// </summary>
        private List<string> _columns;

        internal long _version; // Matches the version of the connection

        private IObjectCryptEngine _cryptEngine = null;
        private static readonly string NO_CRYPT_ENGINE = "Cryptography has not been enabled on this SQLite data reader.";

        internal SqliteDataReader(SqliteCommand cmd, bool setRowId, CommandBehavior behave = CommandBehavior.Default, IObjectCryptEngine cryptEngine = null) {
            _command = cmd;
            _version = _command.Connection._version;
            _cryptEngine = cryptEngine;
            _setRowId = setRowId;

            _commandBehavior = behave;
            _activeStatementIndex = -1;
            _activeStatement = null;
            _rowsAffected = -1;
            _fieldCount = 0;

            if (_command != null) {
                _cryptEngine = _cryptEngine ?? _command._cryptEngine;
                NextResult();
            }
        }

        /// <summary>
        /// Constructor, initializes the datareader and sets up to begin executing statements
        /// </summary>
        /// <param name="cmd">The SqliteCommand this data reader is for</param>
        /// <param name="behave">The expected behavior of the data reader</param>
        /// <param name="cryptEngine">The cryptography 'engine' to use for encryption/decryption operations</param>
        public SqliteDataReader(SqliteCommand cmd, CommandBehavior behave = CommandBehavior.Default, IObjectCryptEngine cryptEngine = null) 
            : this(cmd, false, behave, cryptEngine) {
        }

        internal void Cancel() {
            _version = 0;
        }

        /// <summary>
        /// Closes the datareader, potentially closing the connection as well if CommandBehavior.CloseConnection was specified.
        /// </summary>
        public override void Close() {
            _cryptEngine = null;
            if (_command != null) {
                try {
                    try {
                        // Make sure we've not been canceled
                        if (_version != 0) {
                            try {
                                while (NextResult()) {
                                }
                            }
                            catch {
                            }
                        }
                        _command.ClearDataReader();
                    }
                    finally {
                        // If the datareader's behavior includes closing the connection, then do so here.
                        if ((_commandBehavior & CommandBehavior.CloseConnection) != 0 && _command.Connection != null) {
                            // We need to call Dispose on the command before we call Dispose on the Connection,
                            // otherwise we'll get a SQLITE_LOCKED exception.
                            var conn = _command.Connection;
                            _command.Dispose();
                            conn.Close();
                            _disposeCommand = false;
                        }
                    }
                }
                finally {
                    if (_disposeCommand)
                        _command.Dispose();
                }
            }

            _command = null;
            _activeStatement = null;
            _fieldTypeArray = null;
        }

        /// <summary>
        /// Throw an error if the datareader is closed
        /// </summary>
        private void CheckClosed() {
            if (_command == null)
                throw new InvalidOperationException("DataReader has been closed");

            if (_version == 0)
                throw new SqliteException((int)SqliteErrorCode.Abort, "Execution was aborted by the user");

            if (_command.Connection.State != ConnectionState.Open || _command.Connection._version != _version)
                throw new InvalidOperationException("Connection was closed, statement was terminated");
        }

        /// <summary>
        /// Throw an error if a row is not loaded
        /// </summary>
        private void CheckValidRow() {
            if (_readingState != 0)
                throw new InvalidOperationException("No current row");
        }

        /// <summary>
        /// Enumerator support
        /// </summary>
        /// <returns>Returns a DbEnumerator object.</returns>
        public override global::System.Collections.IEnumerator GetEnumerator() {
            return new DbEnumerator(this, ((_commandBehavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection));
        }

        /// <summary>
        /// Not implemented.  Returns 0
        /// </summary>
        public override int Depth {
            get {
                CheckClosed();
                return 0;
            }
        }

        /// <summary>
        /// Returns the number of columns in the current resultset
        /// </summary>
        public override int FieldCount {
            get {
                CheckClosed();

                return _fieldCount;
            }
        }

        /// <summary>
        /// Returns the number of visible fielsd in the current resultset
        /// </summary>
        public override int VisibleFieldCount {
            get {
                CheckClosed();
                return _fieldCount;
            }
        }

        /// <summary>
        /// Returns the names of columns in the current resultset
        /// </summary>
        public List<string> Columns {
            get { return _columns; }
        }

        /// <summary>
        /// SQLite is inherently un-typed.  All datatypes in SQLite are natively strings.  The definition of the columns of a table
        /// and the affinity of returned types are all we have to go on to type-restrict data in the reader.
        /// 
        /// This function attempts to verify that the type of data being requested of a column matches the datatype of the column.  In
        /// the case of columns that are not backed into a table definition, we attempt to match up the affinity of a column (int, double, string or blob)
        /// to a set of known types that closely match that affinity.  It's not an exact science, but its the best we can do.
        /// </summary>
        /// <returns>
        /// This function throws an InvalidTypeCast() exception if the requested type doesn't match the column's definition or affinity.
        /// </returns>
        /// <param name="i">The index of the column to type-check</param>
        /// <param name="typ">The type we want to get out of the column</param>
        private TypeAffinity VerifyType(int i, DbType typ) {
            CheckClosed();
            CheckValidRow();
            TypeAffinity affinity = GetSqliteType(i).Affinity;

            switch (affinity) {
                case TypeAffinity.Int64:
                    if (typ == DbType.Int16) return affinity;
                    if (typ == DbType.Int32) return affinity;
                    if (typ == DbType.Int64) return affinity;
                    if (typ == DbType.Boolean) return affinity;
                    if (typ == DbType.Byte) return affinity;
                    if (typ == DbType.DateTime) return affinity;
                    if (typ == DbType.Single) return affinity;
                    if (typ == DbType.Double) return affinity;
                    if (typ == DbType.Decimal) return affinity;
                    break;
                case TypeAffinity.Double:
                    if (typ == DbType.Single) return affinity;
                    if (typ == DbType.Double) return affinity;
                    if (typ == DbType.Decimal) return affinity;
                    if (typ == DbType.DateTime) return affinity;
                    break;
                case TypeAffinity.Text:
                    if (typ == DbType.SByte) return affinity;
                    if (typ == DbType.String) return affinity;
                    if (typ == DbType.SByte) return affinity;
                    if (typ == DbType.Guid) return affinity;
                    if (typ == DbType.DateTime) return affinity;
                    if (typ == DbType.Decimal) return affinity;
                    break;
                case TypeAffinity.Blob:
                    if (typ == DbType.Guid) return affinity;
                    if (typ == DbType.String) return affinity;
                    if (typ == DbType.Binary) return affinity;
                    break;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Retrieves the decrypted value of a column as the specified type
        /// </summary>
        /// <typeparam name="T">The type of the object or value to be retrieved</typeparam>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="dbNullHandling">Determines how table column values of NULL are handled</param>
        /// <returns>Specified type</returns>
        public T GetDecrypted<T>(int i, DbNullHandling dbNullHandling = DbNullHandling.ThrowDbNullException) {
            T result = default(T);

            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (IsDBNull(i)) {
                if (dbNullHandling == DbNullHandling.ThrowDbNullException) throw new DbNullException();
            }
            else {
                if (GetSqliteType(i).Affinity != TypeAffinity.Text)
                    throw new Exception("The column value is not of the correct data type.");
                var encrypted = _activeStatement._sql.GetText(_activeStatement, i);
                if (String.IsNullOrWhiteSpace(encrypted)) {
                    throw new Exception("The column value to be decrypted is empty.");
                }
                else {
                    result = _cryptEngine.DecryptObject<T>(encrypted);
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the decrypted value of a column as the specified type
        /// </summary>
        /// <typeparam name="T">The type of the object or value to be retrieved</typeparam>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="dbNullHandling">Determines how table column values of NULL are handled</param>
        /// <returns>Specified type</returns>
        public T GetDecrypted<T>(string name, DbNullHandling dbNullHandling = DbNullHandling.ThrowDbNullException) {
            return this.GetDecrypted<T>(this.GetOrdinal(name), dbNullHandling);
        }

        /// <summary>
        /// Tries to decrypt the column as a value of the specified type
        /// </summary>
        /// <typeparam name="T">The type of the object or value to be retrieved</typeparam>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecrypt<T>(int i, out T value, bool failOnDbNull) {
            var result = false;
            value = default(T);

            if (IsDBNull(i)) {
                result = !failOnDbNull;
            }
            else {
                if ((_cryptEngine == null) || (GetSqliteType(i).Affinity != TypeAffinity.Text)) {
                    result = false;
                }
                else {
                    var encrypted = _activeStatement._sql.GetText(_activeStatement, i);
                    if (String.IsNullOrWhiteSpace(encrypted)) {
                        result = !failOnDbNull;
                    }
                    else {
                        try {
                            value = _cryptEngine.DecryptObject<T>(encrypted);
                            result = true;
                        }
                        catch (Exception) {
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Tries to decrypt the column as a value of the specified type
        /// </summary>
        /// <typeparam name="T">The type of the object or value to be retrieved</typeparam>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecrypt<T>(string name, out T value, bool failOnDbNull) {
            return TryDecrypt<T>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the column as a boolean value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>bool</returns>
        public override bool GetBoolean(int i) {
            VerifyType(i, DbType.Boolean);
            return Convert.ToBoolean(GetValue(i), CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Retrieves the column as a boolean value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>bool</returns>
        public bool GetBoolean(string name) {
            return this.GetBoolean(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a Boolean value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptBoolean(int i, out bool value, bool failOnDbNull = false) {
            return TryDecrypt<bool>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Boolean value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptBoolean(string name, out bool value, bool failOnDbNull = false) {
            return TryDecrypt<bool>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the column as a single byte value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>byte</returns>
        public override byte GetByte(int i) {
            VerifyType(i, DbType.Byte);
            return Convert.ToByte(_activeStatement._sql.GetInt32(_activeStatement, i));
        }

        /// <summary>
        /// Retrieves the column as a single byte value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>byte</returns>
        public byte GetByte(string name) {
            return this.GetByte(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a byte value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptByte(int i, out byte value, bool failOnDbNull = true) {
            return TryDecrypt<byte>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a byte value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptByte(string name, out byte value, bool failOnDbNull = true) {
            return TryDecrypt<byte>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves a column as an array of bytes (blob)
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="fieldOffset">The zero-based index of where to begin reading the data</param>
        /// <param name="buffer">The buffer to write the bytes into</param>
        /// <param name="bufferoffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of bytes written into the array</returns>
        /// <remarks>
        /// To determine the number of bytes in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
            VerifyType(i, DbType.Binary);
            return _activeStatement._sql.GetBytes(_activeStatement, i, (int)fieldOffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Retrieves a column as an array of bytes (blob)
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="fieldOffset">The zero-based index of where to begin reading the data</param>
        /// <param name="buffer">The buffer to write the bytes into</param>
        /// <param name="bufferoffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of bytes written into the array</returns>
        /// <remarks>
        /// To determine the number of bytes in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public long GetBytes(string name, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
            return this.GetBytes(this.GetOrdinal(name), fieldOffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Tries to decrypt the column value as a byte array
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptBytes(int i, out byte[] value, bool failOnDbNull = false) {
            return TryDecrypt<byte[]>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a byte array
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptBytes(string name, out byte[] value, bool failOnDbNull = false) {
            return TryDecrypt<byte[]>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Returns the column as a single character
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>char</returns>
        public override char GetChar(int i) {
            VerifyType(i, DbType.SByte);
            return Convert.ToChar(_activeStatement._sql.GetInt32(_activeStatement, i));
        }

        /// <summary>
        /// Returns the column as a single character
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>char</returns>
        public char GetChar(string name) {
            return this.GetChar(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a char value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptChar(int i, out char value, bool failOnDbNull = true) {
            return TryDecrypt<char>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a char value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptChar(string name, out char value, bool failOnDbNull = true) {
            return TryDecrypt<char>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves a column as an array of chars (blob)
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="fieldoffset">The zero-based index of where to begin reading the data</param>
        /// <param name="buffer">The buffer to write the characters into</param>
        /// <param name="bufferoffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of characters written into the array</returns>
        /// <remarks>
        /// To determine the number of characters in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) {
            VerifyType(i, DbType.String);
            return _activeStatement._sql.GetChars(_activeStatement, i, (int)fieldoffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Retrieves a column as an array of chars (blob)
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="fieldoffset">The zero-based index of where to begin reading the data</param>
        /// <param name="buffer">The buffer to write the characters into</param>
        /// <param name="bufferoffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of characters written into the array</returns>
        /// <remarks>
        /// To determine the number of characters in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public long GetChars(string name, long fieldoffset, char[] buffer, int bufferoffset, int length) {
            return this.GetChars(this.GetOrdinal(name), fieldoffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Tries to decrypt the column value as a char array
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptChars(int i, out char[] value, bool failOnDbNull = false) {
            return TryDecrypt<char[]>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a char array
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptChars(string name, out char[] value, bool failOnDbNull = false) {
            return TryDecrypt<char[]>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the name of the back-end datatype of the column
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>string</returns>
        public override string GetDataTypeName(int i) {
            SqliteType typ = GetSqliteType(i);
            if (typ.Type == DbType.Object) return SqliteConvert.SqliteTypeToType(typ).Name;
            return _activeStatement._sql.ColumnType(_activeStatement, i, out typ.Affinity);
        }

        /// <summary>
        /// Retrieves the name of the back-end datatype of the column
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>string</returns>
        public string GetDataTypeName(string name) {
            return this.GetDataTypeName(this.GetOrdinal(name));
        }

        /// <summary>
        /// Retrieve the column as a date/time value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>DateTime</returns>
        public override DateTime GetDateTime(int i) {
            VerifyType(i, DbType.DateTime);
            return _activeStatement._sql.GetDateTime(_activeStatement, i);
        }

        /// <summary>
        /// Retrieve the column as a date/time value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>DateTime</returns>
        public DateTime GetDateTime(string name) {
            return this.GetDateTime(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a DateTime value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDateTime(int i, out DateTime value, bool failOnDbNull = true) {
            return TryDecrypt<DateTime>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a DateTime value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDateTime(string name, out DateTime value, bool failOnDbNull = true) {
            return TryDecrypt<DateTime>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieve the column as a decimal value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>decimal</returns>
        public override decimal GetDecimal(int i) {
            VerifyType(i, DbType.Decimal);
            return Decimal.Parse(_activeStatement._sql.GetText(_activeStatement, i), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieve the column as a decimal value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>decimal</returns>
        public decimal GetDecimal(string name) {
            return this.GetDecimal(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a Decimal value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDecimal(int i, out Decimal value, bool failOnDbNull = true) {
            return TryDecrypt<Decimal>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Decimal value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDecimal(string name, out Decimal value, bool failOnDbNull = true) {
            return TryDecrypt<Decimal>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Returns the column as a double
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>double</returns>
        public override double GetDouble(int i) {
            VerifyType(i, DbType.Double);
            return _activeStatement._sql.GetDouble(_activeStatement, i);
        }

        /// <summary>
        /// Returns the column as a double
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>double</returns>
        public double GetDouble(string name) {
            return this.GetDouble(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a double value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDouble(int i, out double value, bool failOnDbNull = true) {
            return TryDecrypt<double>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a double value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptDouble(string name, out double value, bool failOnDbNull = true) {
            return TryDecrypt<double>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Returns the .NET type of a given column
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Type</returns>
        public override Type GetFieldType(int i) {
            return SqliteConvert.SqliteTypeToType(GetSqliteType(i));
        }

        /// <summary>
        /// Returns the .NET type of a given column
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Type</returns>
        public Type GetFieldType(string name) {
            return this.GetFieldType(this.GetOrdinal(name));
        }

        /// <summary>
        /// Returns a column as a float value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>float</returns>
        public override float GetFloat(int i) {
            VerifyType(i, DbType.Single);
            return Convert.ToSingle(_activeStatement._sql.GetDouble(_activeStatement, i));
        }

        /// <summary>
        /// Returns a column as a float value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>float</returns>
        public float GetFloat(string name) {
            return this.GetFloat(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a float value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptFloat(int i, out float value, bool failOnDbNull = true) {
            return TryDecrypt<float>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a float value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptFloat(string name, out float value, bool failOnDbNull = true) {
            return TryDecrypt<float>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Returns the column as a Guid
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Guid</returns>
        public override Guid GetGuid(int i) {
            TypeAffinity affinity = VerifyType(i, DbType.Guid);
            if (affinity == TypeAffinity.Blob) {
                byte[] buffer = new byte[16];
                _activeStatement._sql.GetBytes(_activeStatement, i, 0, buffer, 0, 16);
                return new Guid(buffer);
            }
            else
                return new Guid(_activeStatement._sql.GetText(_activeStatement, i));
        }

        /// <summary>
        /// Returns the column as a Guid
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Guid</returns>
        public Guid GetGuid(string name) {
            return this.GetGuid(this.GetOrdinal(name));    
        }

        /// <summary>
        /// Tries to decrypt the column value as a Guid value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptGuid(int i, out Guid value, bool failOnDbNull = true) {
            return TryDecrypt<Guid>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Guid value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptGuid(string name, out Guid value, bool failOnDbNull = true) {
            return TryDecrypt<Guid>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Returns the column as a short
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Int16</returns>
        public override Int16 GetInt16(int i) {
            VerifyType(i, DbType.Int16);
            return Convert.ToInt16(_activeStatement._sql.GetInt32(_activeStatement, i));
        }

        /// <summary>
        /// Returns the column as a short
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Int16</returns>
        public Int16 GetInt16(string name) {
            return this.GetInt16(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int16 value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt16(int i, out Int16 value, bool failOnDbNull = true) {
            return TryDecrypt<Int16>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int16 value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt16(string name, out Int16 value, bool failOnDbNull = true) {
            return TryDecrypt<Int16>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the column as an int
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Int32</returns>
        public override Int32 GetInt32(int i) {
            VerifyType(i, DbType.Int32);
            return _activeStatement._sql.GetInt32(_activeStatement, i);
        }

        /// <summary>
        /// Retrieves the column as an int
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Int32</returns>
        public Int32 GetInt32(string name) {
            return this.GetInt32(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int32 value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt32(int i, out Int32 value, bool failOnDbNull = true) {
            return TryDecrypt<Int32>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int32 value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt32(string name, out Int32 value, bool failOnDbNull = true) {
            return TryDecrypt<Int32>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the column as a long
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Int64</returns>
        public override Int64 GetInt64(int i) {
            VerifyType(i, DbType.Int64);
            return _activeStatement._sql.GetInt64(_activeStatement, i);
        }

        /// <summary>
        /// Retrieves the column as a long
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Int64</returns>
        public Int64 GetInt64(string name) {
            return this.GetInt64(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int64 value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt64(int i, out Int64 value, bool failOnDbNull = true) {
            return TryDecrypt<Int64>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a Int64 value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptInt64(string name, out Int64 value, bool failOnDbNull = true) {
            return TryDecrypt<Int64>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the name of the column
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>string</returns>
        public override string GetName(int i) {
            return _activeStatement._sql.ColumnName(_activeStatement, i);
        }

        /// <summary>
        /// Retrieves the i of a column, given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>The int i of the column</returns>
        public override int GetOrdinal(string name) {
            var index = _columns.IndexOf(name.ToUpperInvariant());
            if (index == -1)
                throw new ArgumentException("Column does not exist.");
            return index;
        }

        /// <summary>
        /// Retrieves the column as a string
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>string</returns>
        public override string GetString(int i) {
            VerifyType(i, DbType.String);
            return _activeStatement._sql.GetText(_activeStatement, i);
        }

        /// <summary>
        /// Retrieves the column as a string
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>string</returns>
        public string GetString(string name) {
            return this.GetString(this.GetOrdinal(name));
        }

        /// <summary>
        /// Tries to decrypt the column value as a string value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptString(int i, out string value, bool failOnDbNull = false) {
            return TryDecrypt<string>(i, out value, failOnDbNull);
        }

        /// <summary>
        /// Tries to decrypt the column value as a string value
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="value">The decrypted value</param>
        /// <param name="failOnDbNull">Fail when column is NULL? (Else returns default value)</param>
        /// <returns>bool</returns>
        public bool TryDecryptString(string name, out string value, bool failOnDbNull = false) {
            return TryDecrypt<string>(this.GetOrdinal(name), out value, failOnDbNull);
        }

        /// <summary>
        /// Retrieves the column as an object corresponding to the underlying datatype of the column
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>object</returns>
        public override object GetValue(int i) {
            SqliteType typ = GetSqliteType(i);

            return _activeStatement._sql.GetValue(_activeStatement, i, typ);
        }

        /// <summary>
        /// Retrieves the column as an object corresponding to the underlying datatype of the column
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>object</returns>
        public object GetValue(string name) {
            return this.GetValue(this.GetOrdinal(name));
        }

        /// <summary>
        /// Retreives the values of multiple columns, up to the size of the supplied array
        /// </summary>
        /// <param name="values">The array to fill with values from the columns in the current resultset</param>
        /// <returns>The number of columns retrieved</returns>
        public override int GetValues(object[] values) {
            int nMax = FieldCount;
            if (values.Length < nMax) nMax = values.Length;

            for (int n = 0; n < nMax; n++) {
                values[n] = GetValue(n);
            }

            return nMax;
        }

        /// <summary>
        /// Returns True if the resultset has rows that can be fetched
        /// </summary>
        public override bool HasRows {
            get {
                CheckClosed();
                return (_readingState != 1);
            }
        }

        /// <summary>
        /// Returns True if the data reader is closed
        /// </summary>
        public override bool IsClosed {
            get { return (_command == null); }
        }

        /// <summary>
        /// Returns True if the specified column is null
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>True or False</returns>
        public override bool IsDBNull(int i) {
            return _activeStatement._sql.IsNull(_activeStatement, i);
        }

        /// <summary>
        /// Returns True if the specified column is null
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>True or False</returns>
        public bool IsDBNull(string name) {
            return this.IsDBNull(this.GetOrdinal(name));
        }

        /// <summary>
        /// Moves to the next resultset in multiple row-returning SQL command.
        /// </summary>
        /// <returns>True if the command was successful and a new resultset is available, False otherwise.</returns>
        public override bool NextResult() {
            CheckClosed();

            SqliteStatement stmt = null;
            int fieldCount;

            while (true) {
                if (_activeStatement != null && stmt == null) {
                    // Reset the previously-executed statement
                    _activeStatement._sql.Reset(_activeStatement);

                    // If we're only supposed to return a single rowset, step through all remaining statements once until
                    // they are all done and return false to indicate no more resultsets exist.
                    if ((_commandBehavior & CommandBehavior.SingleResult) != 0) {
                        for (; ; ) {
                            stmt = _command.GetStatement(_activeStatementIndex + 1);
                            if (stmt == null) break;
                            _activeStatementIndex++;

                            if (_setRowId) {
                                Tuple<SQLitePCL.SQLiteResult, long> stepResult = stmt._sql.StepWithRowId(stmt);
                                if (stepResult.Item2 > -1) _lastInsertRowId = stepResult.Item2;
                            }
                            else {
                                stmt._sql.Step(stmt);
                            }
                            if (stmt._sql.ColumnCount(stmt) == 0) {
                                if (_rowsAffected == -1) _rowsAffected = 0;
                                _rowsAffected += stmt._sql.Changes;
                            }
                            stmt._sql.Reset(stmt); // Gotta reset after every step to release any locks and such!
                        }
                        return false;
                    }
                }

                // Get the next statement to execute
                stmt = _command.GetStatement(_activeStatementIndex + 1);

                // If we've reached the end of the statements, return false, no more resultsets
                if (stmt == null)
                    return false;

                // If we were on a current resultset, set the state to "done reading" for it
                if (_readingState < 1)
                    _readingState = 1;

                _activeStatementIndex++;

                fieldCount = stmt._sql.ColumnCount(stmt);

                // If the statement is not a select statement or we're not retrieving schema only, then perform the initial step
                if ((_commandBehavior & CommandBehavior.SchemaOnly) == 0 || fieldCount == 0) {
                    SQLitePCL.SQLiteResult stmtResult;
                    if (_setRowId) {
                        Tuple<SQLitePCL.SQLiteResult, long> stepResult = stmt._sql.StepWithRowId(stmt);
                        stmtResult = stepResult.Item1;
                        if (stepResult.Item2 > -1) _lastInsertRowId = stepResult.Item2;
                    }
                    else {
                        stmtResult = stmt._sql.Step(stmt);
                    }
                    if (stmtResult == SQLitePCL.SQLiteResult.ROW) {
                        _readingState = -1;
                    }
                    else if (fieldCount == 0) // No rows returned, if fieldCount is zero, skip to the next statement
                    {
                        if (_rowsAffected == -1) _rowsAffected = 0;
                        _rowsAffected += stmt._sql.Changes;
                        stmt._sql.Reset(stmt);
                        continue; // Skip this command and move to the next, it was not a row-returning resultset
                    }
                    else // No rows, fieldCount is non-zero so stop here
                    {
                        _readingState = 1; // This command returned columns but no rows, so return true, but HasRows = false and Read() returns false
                    }
                }

                // Ahh, we found a row-returning resultset eligible to be returned!
                _activeStatement = stmt;
                _fieldCount = fieldCount;
                _fieldTypeArray = null;

                var cols = new string[_fieldCount];

                // load column names
                for (int i = 0; i < _fieldCount; i++) {
                    cols[i] = _activeStatement._sql.ColumnName(_activeStatement, i).ToUpperInvariant();
                }

                _columns = new List<string>(cols);

                return true;
            }
        }

        /// <summary>
        /// Retrieves the SQLiteType for a given column, and caches it to avoid repetetive interop calls.
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>A SQLiteType structure</returns>
        private SqliteType GetSqliteType(int i) {
            SqliteType typ;

            // Initialize the field types array if not already initialized
            if (_fieldTypeArray == null)
                _fieldTypeArray = new SqliteType[VisibleFieldCount];

            // Initialize this column's field type instance
            if (_fieldTypeArray[i] == null) _fieldTypeArray[i] = new SqliteType();

            typ = _fieldTypeArray[i];

            // If not initialized, then fetch the declared column datatype and attempt to convert it 
            // to a known DbType.
            if (typ.Affinity == TypeAffinity.Uninitialized)
                typ.Type = SqliteConvert.TypeNameToDbType(_activeStatement._sql.ColumnType(_activeStatement, i, out typ.Affinity));
            else
                typ.Affinity = _activeStatement._sql.ColumnAffinity(_activeStatement, i);

            return typ;
        }

        /// <summary>
        /// Reads the next row from the resultset
        /// </summary>
        /// <returns>True if a new row was successfully loaded and is ready for processing</returns>
        public override bool Read() {
            CheckClosed();

            if (_readingState == -1) // First step was already done at the NextResult() level, so don't step again, just return true.
      {
                _readingState = 0;
                return true;
            }
            else if (_readingState == 0) // Actively reading rows
      {
                // Don't read a new row if the command behavior dictates SingleRow.  We've already read the first row.
                if ((_commandBehavior & CommandBehavior.SingleRow) == 0) {
                    //if (_activeStatement._sql.Step(_activeStatement) == true) {
                    //    return true;
                    //}
                    SQLitePCL.SQLiteResult stmtResult;
                    if (_setRowId) {
                        Tuple<SQLitePCL.SQLiteResult, long> stepResult = _activeStatement._sql.StepWithRowId(_activeStatement);
                        stmtResult = stepResult.Item1;
                        if (stepResult.Item2 > -1) _lastInsertRowId = stepResult.Item2;
                    }
                    else {
                        stmtResult = _activeStatement._sql.Step(_activeStatement);
                    }
                    if (stmtResult == SQLitePCL.SQLiteResult.ROW) {
                        return true;
                    }
                }

                _readingState = 1; // Finished reading rows
            }

            return false;
        }

        /// <summary>
        /// Retrieve the count of records affected by an update/insert command.  Only valid once the data reader is closed!
        /// </summary>
        public override int RecordsAffected {
            get { return (_rowsAffected < 0) ? 0 : _rowsAffected; }
        }

        /// <summary>
        /// Indexer to retrieve data from a column given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve data for</param>
        /// <returns>The value contained in the column</returns>
        public override object this[string name] {
            get { return GetValue(GetOrdinal(name)); }
        }

        /// <summary>
        /// Indexer to retrieve data from a column given its i
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>The value contained in the column</returns>
        public override object this[int i] {
            get { return GetValue(i); }
        }

        /// <summary>
        /// Dispose object and free resources
        /// </summary>
        public override void Dispose() {
            _cryptEngine = null;
            base.Dispose();
        }
    }
}
