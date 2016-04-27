// ReSharper disable InconsistentNaming
// ReSharper disable RedundantCast

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Portable.Data.Sqlite {
	public sealed class SqliteDataReader : IDataReader {
        private readonly SqliteLockContext _lockContext;
        public SqliteLockContext LockContext {
            get { return _lockContext; }
        }

        private IObjectCryptEngine _cryptEngine = null;
        private static readonly string NO_CRYPT_ENGINE = "Cryptography has not been enabled on this SQLite data reader.";

        private List<string> _columns = new List<string>();

        /// <summary>
        /// A list of column names of the recordset referenced by the DataReader - note that the list will be empty before the first read.
        /// </summary>
	    public List<string> Columns {
	        get {
	            return _columns;
	        }
	    }

        public void Close()
		{
			// NOTE: Dispose calls Close, so we can't put our logic in Dispose(bool) and call Dispose() from this method.
			Reset();
			Utility.Dispose(ref m_statementPreparer);

			if (m_behavior.HasFlag(CommandBehavior.CloseConnection))
			{
				var dbConnection = m_command.Connection;
				m_command.Dispose();
				dbConnection.Dispose();
			}

			m_command = null;
		}

		public bool NextResult() {
			return NextResultAsyncCore(CancellationToken.None).Result;
		}

        public Task<bool> NextResultAsync(CancellationToken cancellationToken) {
            return NextResultAsyncCore(cancellationToken);
        }

        private Task<bool> NextResultAsyncCore(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();

			Reset();
			m_currentStatementIndex++;
			m_currentStatement = m_statementPreparer.Get(m_currentStatementIndex, cancellationToken);
			if (m_currentStatement == null)
				return s_falseTask;

			bool success = false;
			try
			{
				for (int i = 0; i < m_command.Parameters.Count; i++)
				{
					SqliteParameter parameter = (SqliteParameter)m_command.Parameters[i];
					string parameterName = parameter.ParameterName;
					int index;
					if (parameterName != null)
					{
						if (parameterName[0] != '@')
							parameterName = "@" + parameterName;
						index = _lockContext.sqlite3_bind_parameter_index(m_currentStatement, SqliteConnection.ToNullTerminatedUtf8(parameterName));
					}
					else
					{
						index = i + 1;
					}
					if (index > 0)
					{
						object value = parameter.Value;
						if (value == null || value.Equals(DBNull.Value))
                            _lockContext.sqlite3_bind_null(m_currentStatement, index).ThrowOnError();
						else if (value is int || (value is Enum && Enum.GetUnderlyingType(value.GetType()) == typeof(int)))
                            _lockContext.sqlite3_bind_int(m_currentStatement, index, (int) value).ThrowOnError();
						else if (value is bool)
                            _lockContext.sqlite3_bind_int(m_currentStatement, index, ((bool) value) ? 1 : 0).ThrowOnError();
						else if (value is string)
							BindText(index, (string) value);
						else if (value is byte[])
							BindBlob(index, (byte[]) value);
						else if (value is long)
                            _lockContext.sqlite3_bind_int64(m_currentStatement, index, (long) value).ThrowOnError();
						else if (value is float)
                            _lockContext.sqlite3_bind_double(m_currentStatement, index, (float) value).ThrowOnError();
						else if (value is double)
                            _lockContext.sqlite3_bind_double(m_currentStatement, index, (double) value).ThrowOnError();
						else if (value is DateTime)
							BindText(index, ToString((DateTime) value));
						else if (value is Guid)
							BindBlob(index, ((Guid) value).ToByteArray());
						else if (value is byte)
                            _lockContext.sqlite3_bind_int(m_currentStatement, index, (byte) value).ThrowOnError();
						else if (value is short)
                            _lockContext.sqlite3_bind_int(m_currentStatement, index, (short) value).ThrowOnError();
						else
							BindText(index, Convert.ToString(value, CultureInfo.InvariantCulture));
					}
				}

				success = true;
			}
			finally
			{
				if (!success)
                    _lockContext.sqlite3_reset(m_currentStatement).ThrowOnError();
			}

			return s_trueTask;
		}

        public DataTable GetSchemaTable() {
            throw new NotSupportedException();
        }

        private long _lastInsertRowId = -1;
        public long LastInsertRowId {
            get { return _lastInsertRowId; }
        }

        private bool _setRowId;
        public bool SetRowId {
            get { return _setRowId; }
            set { _setRowId = value; }
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
                if (GetColumnType(i) != SqliteColumnType.Text) {
                    throw new Exception("The column value is not of the correct data type.");
                }
                var encrypted = GetString(i);
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
                if ((_cryptEngine == null) || (GetColumnType(i) != SqliteColumnType.Text)) {
                    result = false;
                }
                else {
                    var encrypted = GetString(i);
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

        public bool Read() {
			VerifyNotDisposed();
			return ReadAsyncCore(CancellationToken.None).Result;
		}

		internal static IDataReader Create(SqliteCommand command, CommandBehavior behavior) {
			var dataReader = new SqliteDataReader(command, behavior, true);
			dataReader.NextResult();
			return dataReader;
		}

		internal static async Task<IDataReader> CreateAsync(SqliteCommand command, CommandBehavior behavior, CancellationToken cancellationToken) {
		    var dataReader = new SqliteDataReader(command, behavior, true);
			await dataReader.NextResultAsync(cancellationToken);
			return dataReader;
		}

		internal SqliteDataReader(SqliteCommand command, CommandBehavior behavior, bool internallyCreatedReader) {
		    if (command == null) { throw new ArgumentNullException(nameof(command));}
            if (command.LockContext == null) { throw new ArgumentException("The LockContext cannot be null.", nameof(command));}

			m_command = command;
		    _lockContext = command.LockContext;
            _cryptEngine = command._cryptEngine;
            m_behavior = behavior;
			m_statementPreparer = command.GetStatementPreparer();

			m_startingChanges = _lockContext.sqlite3_total_changes(DatabaseHandle);
			m_currentStatementIndex = -1;
		}

        public SqliteDataReader(SqliteCommand command, CommandBehavior behavior = CommandBehavior.Default) {
            if (command == null) { throw new ArgumentNullException(nameof(command)); }
            if (command.LockContext == null) { throw new ArgumentException("The LockContext cannot be null.", nameof(command)); }

            m_command = command;
            _lockContext = command.LockContext;
            _cryptEngine = command._cryptEngine;
            m_behavior = behavior;
            m_statementPreparer = command.GetStatementPreparer();

            m_startingChanges = _lockContext.sqlite3_total_changes(DatabaseHandle);
            m_currentStatementIndex = -1;
            this.NextResult();
        }

        private Task<bool> ReadAsyncCore(CancellationToken cancellationToken) {
			Random random = null;
			while (!cancellationToken.IsCancellationRequested) {
			    SqliteErrorCode errorCode; //= _lockContext.sqlite3_step(m_currentStatement);

			    if (_setRowId) {
			        _lastInsertRowId = _lockContext.sqlite3_step_return_rowid(DatabaseHandle, m_currentStatement, out errorCode);
			    }
			    else {
                    errorCode = _lockContext.sqlite3_step(m_currentStatement);
                }

				switch (errorCode)
				{
				case SqliteErrorCode.Done:
					Reset();
					return s_falseTask;

				case SqliteErrorCode.Row:
					m_hasRead = true;
				    if (m_columnType == null) {
				        int numColumns = _lockContext.sqlite3_column_count(m_currentStatement);
				        if (_columns.Count == 0 && numColumns > 0) {
				            for (int i = 0; i < numColumns; i++) {
				                _columns.Add(SqliteConnection.FromUtf8(_lockContext.sqlite3_column_name(m_currentStatement, i)));
				            }
				        }
                        m_columnType = new DbType?[numColumns];
                    }	
					return s_trueTask;

				case SqliteErrorCode.Busy:
				case SqliteErrorCode.Locked:
				case SqliteErrorCode.CantOpen:
					if (cancellationToken.IsCancellationRequested)
						return s_canceledTask;
					if (random == null)
						random = new Random();
					Thread.Sleep(random.Next(1, 150));
					break;

				case SqliteErrorCode.Interrupt:
					return s_canceledTask;

				default:
					throw new SqliteException(errorCode);
				}
			}

			return cancellationToken.IsCancellationRequested ? s_canceledTask : s_trueTask;
		}

		public bool IsClosed
		{
			get { return m_command == null; }
		}

		public int RecordsAffected
		{
			get { return _lockContext.sqlite3_total_changes(DatabaseHandle) - m_startingChanges; }
		}

        /// <summary>
        /// Retrieves the column as a boolean value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>bool</returns>
        public bool GetBoolean(int i) {
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
        public byte GetByte(int i)
		{
			return (byte) GetValue(i);
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
        /// <param name="bufferOffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of bytes written into the array</returns>
        /// <remarks>
        /// To determine the number of bytes in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
		{
			var sqliteType = _lockContext.sqlite3_column_type(m_currentStatement, i);
			if (sqliteType == SqliteColumnType.Null)
				return 0;
			else if (sqliteType != SqliteColumnType.Blob)
				throw new InvalidCastException("Cannot convert '{0}' to bytes.".FormatInvariant(sqliteType));

			int availableLength = _lockContext.sqlite3_column_bytes(m_currentStatement, i);
			if (buffer == null)
			{
				// this isn't required by the DbDataReader.GetBytes API documentation, but is what Portable.Data.Sqlite does
				// (as does SqlDataReader: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getbytes.aspx)
				return availableLength;
			}

			if (bufferOffset + length > buffer.Length)
				throw new ArgumentException("bufferOffset + length cannot exceed buffer.Length", nameof(length));

			IntPtr ptr = _lockContext.sqlite3_column_blob(m_currentStatement, i);
			int lengthToCopy = Math.Min(availableLength - (int)fieldOffset, length);
			Marshal.Copy(new IntPtr(ptr.ToInt64() + fieldOffset), buffer, bufferOffset, lengthToCopy);
			return lengthToCopy;
		}

        /// <summary>
        /// Retrieves a column as an array of bytes (blob)
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <param name="fieldOffset">The zero-based index of where to begin reading the data</param>
        /// <param name="buffer">The buffer to write the bytes into</param>
        /// <param name="bufferOffset">The zero-based index of where to begin writing into the array</param>
        /// <param name="length">The number of bytes to retrieve</param>
        /// <returns>The actual number of bytes written into the array</returns>
        /// <remarks>
        /// To determine the number of bytes in the column, pass a null value for the buffer.  The total length will be returned.
        /// </remarks>
        public long GetBytes(string name, long fieldOffset, byte[] buffer, int bufferOffset, int length) {
            return this.GetBytes(this.GetOrdinal(name), fieldOffset, buffer, bufferOffset, length);
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
        public char GetChar(int i)
		{
			return (char) GetValue(i);
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

        //public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        //{
        //	throw new NotImplementedException();
        //}

        /// <summary>
        /// Returns the column as a Guid
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>Guid</returns>
        public Guid GetGuid(int i)
		{
			return (Guid) GetValue(i);
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
        public short GetInt16(int i)
		{
			return (short) GetValue(i);
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
        public int GetInt32(int i)
		{
			object value = GetValue(i);
			if (value is short)
				return (short) value;
			else if (value is long)
				return checked((int) (long) value);
			return (int) value;
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
        public long GetInt64(int i)
		{
			object value = GetValue(i);
			if (value is short)
				return (short) value;
			if (value is int)
				return (int) value;
			return (long) value;
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
        /// Retrieve the column as a date/time value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>DateTime</returns>
        public DateTime GetDateTime(int i)
		{
			return (DateTime) GetValue(i);
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
        /// Retrieves the column as a string
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>string</returns>
		public string GetString(int i)
		{
			return (string) GetValue(i);
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
        /// Retrieve the column as a decimal value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>decimal</returns>
        public decimal GetDecimal(int i) {
            return Decimal.Parse(GetValue(i).ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
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
        public double GetDouble(int i)
		{
			object value = GetValue(i);
			if (value is float)
				return (float) value;
			return (double) value;
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
        /// Returns a column as a float value
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>float</returns>
        public float GetFloat(int i)
		{
			return (float) GetValue(i);
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

        public string GetName(int ordinal)
		{
			VerifyHasResult();
			if (ordinal < 0 || ordinal > FieldCount)
				throw new ArgumentOutOfRangeException(nameof(ordinal), "value must be between 0 and {0}.".FormatInvariant(FieldCount - 1));

			return SqliteConnection.FromUtf8(_lockContext.sqlite3_column_name(m_currentStatement, ordinal)); 
		}

		public int GetValues(object[] values)
		{
			VerifyRead();
			int count = Math.Min(values.Length, FieldCount);
			for (int i = 0; i < count; i++)
				values[i] = GetValue(i);
			return count;
		}

		public bool IsDBNull(int i)
		{
			VerifyRead();
			return _lockContext.sqlite3_column_type(m_currentStatement, i) == SqliteColumnType.Null;
		}

	    public bool IsDBNull(string name) {
	        return IsDBNull(GetOrdinal(name));
	    }

		public int FieldCount
		{
			get
			{
				VerifyNotDisposed();
				return m_hasRead ? m_columnType.Length : _lockContext.sqlite3_column_count(m_currentStatement);
			}
		}

		public object this[int ordinal]
		{
			get { return GetValue(ordinal); }
		}

		public object this[string name]
		{
			get { return GetValue(GetOrdinal(name)); }
		}

		public bool HasRows
		{
			get
			{
				VerifyNotDisposed();
				return m_hasRead;
			}
		}

		public int GetOrdinal(string name)
		{
			VerifyHasResult();

			if (m_columnNames == null)
			{
				var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				for (int i = 0; i < FieldCount; i++)
				{
					string columnName = SqliteConnection.FromUtf8(_lockContext.sqlite3_column_name(m_currentStatement, i));
					columnNames[columnName] = i;
				}
				m_columnNames = columnNames;
			}

			int ordinal;
			if (!m_columnNames.TryGetValue(name, out ordinal))
				throw new IndexOutOfRangeException("The column name '{0}' does not exist in the result set.".FormatInvariant(name));
			return ordinal;
		}

		public string GetDataTypeName(int ordinal)
		{
			throw new NotSupportedException();
		}

		public Type GetFieldType(int ordinal)
		{
			throw new NotSupportedException();
		}

	    public SqliteColumnType GetColumnType(int ordinal) {
            return _lockContext.sqlite3_column_type(m_currentStatement, ordinal);
        }

        public SqliteColumnType GetColumnType(string columnName) {
            return GetColumnType(GetOrdinal(columnName));
        }

	    public string GetColumnDeclaredDataType(int ordinal) {
	        string result = null;
            IntPtr declType = _lockContext.sqlite3_column_decltype(m_currentStatement, ordinal);
            if (declType != IntPtr.Zero) {
                string type = SqliteConnection.FromUtf8(declType);
                if (!String.IsNullOrWhiteSpace(type)) {
                    result = type.Trim().ToUpper();
                }
            }
            return result;
	    }

	    public string GetColumnDeclaredDataType(string columnName) {
	        return GetColumnDeclaredDataType(GetOrdinal(columnName));
	    }

	    public DbType GetColumnDbType(int ordinal) {
            // determine (and cache) the declared type of the column (e.g., from the SQL schema)
	        DbType result;
            if (m_columnType[ordinal].HasValue) {
                result = m_columnType[ordinal].Value;
            }
            else {
                IntPtr declType = _lockContext.sqlite3_column_decltype(m_currentStatement, ordinal);
                if (declType != IntPtr.Zero) {
                    string type = SqliteConnection.FromUtf8(declType);
                    if (!s_sqlTypeToDbType.TryGetValue(type, out result))
                        throw new NotSupportedException("The data type name '{0}' is not supported.".FormatInvariant(type));
                }
                else {
                    result = DbType.Object;
                }
                m_columnType[ordinal] = result;
            }
	        if (result == DbType.Object) {
                result = s_sqliteTypeToDbType[GetColumnType(ordinal)];
            }
	        return result;
	    }

	    public DbType GetColumnDbType(string columnName) {
	        return GetColumnDbType(GetOrdinal(columnName));
	    }

	    public object GetValue(int i) {
			VerifyRead();
			if (i < 0 || i > FieldCount)
				throw new ArgumentOutOfRangeException(nameof(i), "value must be between 0 and {0}.".FormatInvariant(FieldCount - 1));

	        DbType dbType = GetColumnDbType(i);
		    var sqliteType = GetColumnType(i);

			switch (sqliteType) {
			    case SqliteColumnType.Null:
				    return DBNull.Value;

			    case SqliteColumnType.Blob:
				    int byteCount = _lockContext.sqlite3_column_bytes(m_currentStatement, i);
				    byte[] bytes = new byte[byteCount];
				    if (byteCount > 0)
				    {
					    IntPtr bytePointer = _lockContext.sqlite3_column_blob(m_currentStatement, i);
					    Marshal.Copy(bytePointer, bytes, 0, byteCount);
				    }
				    return dbType == DbType.Guid && byteCount == 16 ? (object) new Guid(bytes) : (object) bytes;

			    case SqliteColumnType.Double:
				    double doubleValue = _lockContext.sqlite3_column_double(m_currentStatement, i);
				    return dbType == DbType.Single ? (object) (float) doubleValue : (object) doubleValue;

			    case SqliteColumnType.Integer:
				    long integerValue = _lockContext.sqlite3_column_int64(m_currentStatement, i);
				    return dbType == DbType.Int32 ? (object) (int) integerValue :
					    dbType == DbType.Boolean ? (object) (integerValue != 0) :
					    dbType == DbType.Int16 ? (object) (short) integerValue :
					    dbType == DbType.Byte ? (object) (byte) integerValue :
					    dbType == DbType.Single ? (object) (float) integerValue :
					    dbType == DbType.Double ? (object) (double) integerValue :
					    (object) integerValue;

			    case SqliteColumnType.Text:
				    int stringLength = _lockContext.sqlite3_column_bytes(m_currentStatement, i);
				    string stringValue = SqliteConnection.FromUtf8(_lockContext.sqlite3_column_text(m_currentStatement, i), stringLength);
				    return dbType == DbType.DateTime ? (object) DateTime.ParseExact(stringValue, s_dateTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None) :
					    (object) stringValue;

			    default:
				    throw new InvalidOperationException();
			}
		}

	    public object GetValue(string name) {
	        return GetValue(GetOrdinal(name));
	    }

		public int Depth
		{
			get { throw new NotSupportedException(); }
		}

		public Type GetProviderSpecificFieldType(int ordinal)
		{
			throw new NotSupportedException();
		}

		public object GetProviderSpecificValue(int ordinal)
		{
			throw new NotSupportedException();
		}

		public int GetProviderSpecificValues(object[] values)
		{
			throw new NotSupportedException();
		}

		public Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();

			return ReadAsyncCore(cancellationToken);
		}

        public IDataReader GetData(int i) {
            return ((IDataReader)this).GetData(i);
        }

        public IDataReader GetData(string name) {
            return GetData(GetOrdinal(name));
        }

        public int VisibleFieldCount
		{
			get { return FieldCount; }
		}

		private SqliteDatabaseHandle DatabaseHandle
		{
			get { return ((SqliteConnection) m_command.Connection).Handle; }
		}

		private void Reset()
		{
			if (m_currentStatement != null)
                _lockContext.sqlite3_reset(m_currentStatement);
			m_currentStatement = null;
			m_columnNames = null;
			m_columnType = null;
			m_hasRead = false;
		}

		private void VerifyHasResult()
		{
			VerifyNotDisposed();
			if (m_currentStatement == null)
				throw new InvalidOperationException("There is no current result set.");
		}

		private void VerifyRead()
		{
			VerifyHasResult();
			if (!m_hasRead)
				throw new InvalidOperationException("Read must be called first.");
		}

		private void VerifyNotDisposed()
		{
			if (m_command == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private void BindBlob(int ordinal, byte[] blob)
		{
            _lockContext.sqlite3_bind_blob(m_currentStatement, ordinal, blob, blob.Length, s_sqliteTransient).ThrowOnError();
		}

		private void BindText(int ordinal, string text)
		{
			byte[] bytes = SqliteConnection.ToUtf8(text);
            _lockContext.sqlite3_bind_text(m_currentStatement, ordinal, bytes, bytes.Length, s_sqliteTransient).ThrowOnError();
		}

		private static string ToString(DateTime dateTime)
		{
			// these are the Portable.Data.Sqlite default format strings (from SqliteConvert.cs)
			string formatString = dateTime.Kind == DateTimeKind.Utc ? "yyyy-MM-dd HH:mm:ss.FFFFFFFK" : "yyyy-MM-dd HH:mm:ss.FFFFFFF";
			return dateTime.ToString(formatString, CultureInfo.InvariantCulture);
		}

		private static Task<bool> CreateCanceledTask()
		{
			var source = new TaskCompletionSource<bool>();
			source.SetCanceled();
			return source.Task;
		}

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                this.Close();
            }
        }

        static readonly Dictionary<string, DbType> s_sqlTypeToDbType = new Dictionary<string, DbType>(StringComparer.OrdinalIgnoreCase)
		{
			{ "bigint", DbType.Int64 },
			{ "bit", DbType.Boolean },
			{ "blob", DbType.Binary },
			{ "bool", DbType.Boolean },
			{ "boolean", DbType.Boolean },
			{ "datetime", DbType.DateTime },
			{ "double", DbType.Double },
			{ "float", DbType.Double },
			{ "guid", DbType.Guid },
			{ "int", DbType.Int32 },
			{ "integer", DbType.Int64 },
			{ "long", DbType.Int64 },
			{ "real", DbType.Double },
			{ "single", DbType.Single},
			{ "string", DbType.String },
			{ "text", DbType.String },
            { "counter", DbType.Int64 },
            { "autoincrement", DbType.Int64 },
            { "identity", DbType.Int64 },
            { "longtext", DbType.String },
            { "longchar", DbType.String },
            { "longvarchar", DbType.String },
            { "tinyint", DbType.Byte },
            { "varchar", DbType.String },
            { "nvarchar", DbType.String },
            { "char", DbType.String },
            { "nchar", DbType.String },
            { "ntext", DbType.String },
            { "yesno", DbType.Boolean },
            { "logical", DbType.Boolean },
            { "numeric", DbType.Decimal },
            { "decimal", DbType.Decimal },
            { "money", DbType.Decimal },
            { "currency", DbType.Decimal },
            { "time", DbType.DateTime },
            { "date", DbType.DateTime },
            { "smalldate", DbType.DateTime },
            { "binary", DbType.Binary },
            { "varbinary", DbType.Binary },
            { "image", DbType.Binary },
            { "general", DbType.Binary },
            { "oleobject", DbType.Binary },
            { "guidblob", DbType.Guid },
            { "uniqueidentifier", DbType.Guid },
            { "memo", DbType.String },
            { "note", DbType.String },
            { "smallint", DbType.Int16 },
            { "timestamp", DbType.DateTime },
            { "encrypted", DbType.String }
        };

		static readonly Dictionary<SqliteColumnType, DbType> s_sqliteTypeToDbType = new Dictionary<SqliteColumnType, DbType>()
		{
			{ SqliteColumnType.Integer, DbType.Int64 },
			{ SqliteColumnType.Blob, DbType.Binary },
			{ SqliteColumnType.Text, DbType.String },
			{ SqliteColumnType.Double, DbType.Double },
			{ SqliteColumnType.Null, DbType.Object }
		};

		static readonly string[] s_dateTimeFormats =
		{
			"THHmmssK",
			"THHmmK",
			"HH:mm:ss.FFFFFFFK",
			"HH:mm:ssK",
			"HH:mmK",
			"yyyy-MM-dd HH:mm:ss.FFFFFFFK",
			"yyyy-MM-dd HH:mm:ssK",
			"yyyy-MM-dd HH:mmK",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
			"yyyy-MM-ddTHH:mmK",
			"yyyy-MM-ddTHH:mm:ssK",
			"yyyyMMddHHmmssK",
			"yyyyMMddHHmmK",
			"yyyyMMddTHHmmssFFFFFFFK",
			"THHmmss",
			"THHmm",
			"HH:mm:ss.FFFFFFF",
			"HH:mm:ss",
			"HH:mm",
			"yyyy-MM-dd HH:mm:ss.FFFFFFF",
			"yyyy-MM-dd HH:mm:ss",
			"yyyy-MM-dd HH:mm",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFF",
			"yyyy-MM-ddTHH:mm",
			"yyyy-MM-ddTHH:mm:ss",
			"yyyyMMddHHmmss",
			"yyyyMMddHHmm",
			"yyyyMMddTHHmmssFFFFFFF",
			"yyyy-MM-dd",
			"yyyyMMdd",
			"yy-MM-dd"
		};

        static readonly IntPtr s_sqliteTransient = new IntPtr(-1);
		static readonly Task<bool> s_canceledTask = CreateCanceledTask();
		static readonly Task<bool> s_falseTask = Task.FromResult(false);
		static readonly Task<bool> s_trueTask = Task.FromResult(true);

		SqliteCommand m_command;
		readonly CommandBehavior m_behavior;
		readonly int m_startingChanges;
		SqliteStatementPreparer m_statementPreparer;
		int m_currentStatementIndex;
		SqliteStatementHandle m_currentStatement;
		bool m_hasRead;
		DbType?[] m_columnType;
		Dictionary<string, int> m_columnNames;
	}
}
