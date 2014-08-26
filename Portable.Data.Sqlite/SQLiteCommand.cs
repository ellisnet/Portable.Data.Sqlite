﻿/********************************************************
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
    using System.ComponentModel;

    /// <summary>
    /// SQLite implementation of DbCommand.
    /// </summary>
    public sealed class SqliteCommand : DbCommand, ICloneable, IDisposable { 
        /// <summary>
        /// The command text this command is based on
        /// </summary>
        private string _commandText;
        /// <summary>
        /// The connection the command is associated with
        /// </summary>
        private SqliteAdoConnection _cnn;
        /// <summary>
        /// The version of the connection the command is associated with
        /// </summary>
        private long _version;
        /// <summary>
        /// Indicates whether or not a DataReader is active on the command.
        /// </summary>
        private WeakReference _activeReader;
        /// <summary>
        /// The timeout for the command, kludged because SQLite doesn't support per-command timeout values
        /// </summary>
        internal int _commandTimeout;
        /// <summary>
        /// Designer support
        /// </summary>
        private bool _designTimeVisible;
        /// <summary>
        /// Used by DbDataAdapter to determine updating behavior
        /// </summary>
        private UpdateRowSource _updateRowSource;
        /// <summary>
        /// The collection of parameters for the command
        /// </summary>
        private SqliteParameterCollection _parameterCollection;

        /// <summary>
        /// The SQL command text, broken into individual SQL statements as they are executed
        /// </summary>
        internal List<SqliteStatement> _statementList;

        /// <summary>
        /// Unprocessed SQL text that has not been executed
        /// </summary>
        internal string _remainingText;
        /// <summary>
        /// Transaction associated with this command
        /// </summary>
        private SqliteTransaction _transaction;

        internal IObjectCryptEngine _cryptEngine = null;
        private static readonly string NO_CRYPT_ENGINE = "Cryptography has not been enabled on this SQLite command.";
        //private readonly Object encryptLock = new Object();

        ///<overloads>
        /// Constructs a new SqliteCommand
        /// </overloads>
        /// <summary>
        /// Default constructor
        /// </summary>
        public SqliteCommand()
            : this(null, null) {
        }

        /// <summary>
        /// Initializes the command with the given command text
        /// </summary>
        /// <param name="commandText">The SQL command text</param>
        public SqliteCommand(string commandText)
            : this(commandText, null, null) {
        }

        /// <summary>
        /// Initializes the command with the given SQL command text and attach the command to the specified
        /// connection.
        /// </summary>
        /// <param name="commandText">The SQL command text</param>
        /// <param name="connection">The connection to associate with the command</param>
        public SqliteCommand(string commandText, SqliteAdoConnection connection)
            : this(commandText, connection, null) {
        }

        /// <summary>
        /// Initializes the command and associates it with the specified connection.
        /// </summary>
        /// <param name="connection">The connection to associate with the command</param>
        public SqliteCommand(SqliteAdoConnection connection)
            : this(null, connection, null) {
        }

        private SqliteCommand(SqliteCommand source)
            : this(source.CommandText, source.Connection, source.Transaction) {
            CommandTimeout = source.CommandTimeout;
            UpdatedRowSource = source.UpdatedRowSource;

            foreach (SqliteParameter param in source._parameterCollection) {
                Parameters.Add(param.Clone());
            }
        }

        /// <summary>
        /// Initializes a command with the given SQL, connection and transaction
        /// </summary>
        /// <param name="commandText">The SQL command text</param>
        /// <param name="connection">The connection to associate with the command</param>
        /// <param name="transaction">The transaction the command should be associated with</param>
        /// <param name="cryptEngine">The cryptography 'engine' to use for encryption/decryption operations</param>
        public SqliteCommand(string commandText, SqliteAdoConnection connection, SqliteTransaction transaction, IObjectCryptEngine cryptEngine = null) {
            _statementList = null;
            _activeReader = null;
            _commandTimeout = 30;
            _parameterCollection = new SqliteParameterCollection(this);
            _designTimeVisible = true;
            _updateRowSource = UpdateRowSource.None;
            _transaction = null;
            _cryptEngine = cryptEngine;

            if (commandText != null)
                CommandText = commandText;

            if (connection != null) {
                DbConnection = connection;
                _commandTimeout = connection.DefaultTimeout;
                if (_cryptEngine == null) _cryptEngine = connection._cryptEngine;
            }

            if (transaction != null)
                Transaction = transaction;
        }

        /// <summary>
        /// Disposes of the command and clears all member variables
        /// </summary>
        public override void Dispose() {
            // If a reader is active on this command, don't destroy the command, instead let the reader do it
            SqliteDataReader reader = null;
            if (_activeReader != null) {
                try {
                    reader = _activeReader.Target as SqliteDataReader;
                }
                catch {
                }
            }

            if (reader != null) {
                reader._disposeCommand = true;
                _activeReader = null;
                return;
            }

            _cryptEngine = null;
            Connection = null;
            _parameterCollection.Clear();
            _commandText = null;
        }

        /// <summary>
        /// Clears and destroys all statements currently prepared
        /// </summary>
        internal void ClearCommands() {
            if (_activeReader != null) {
                SqliteDataReader reader = null;
                try {
                    reader = _activeReader.Target as SqliteDataReader;
                }
                catch {
                }

                if (reader != null)
                    reader.Close();

                _activeReader = null;
            }

            if (_statementList == null) return;

            int x = _statementList.Count;
            for (int n = 0; n < x; n++)
                _statementList[n].Dispose();

            _statementList = null;

            _parameterCollection.Unbind();
        }

        /// <summary>
        /// Builds an array of prepared statements for each complete SQL statement in the command text
        /// </summary>
        internal SqliteStatement BuildNextCommand() {
            SqliteStatement stmt = null;

            try {
                if (_statementList == null)
                    _remainingText = _commandText;

                //stmt = _cnn._sql.Prepare(_cnn, _remainingText, (_statementList == null) ? null : _statementList[_statementList.Count - 1], (uint)(_commandTimeout * 1000), out _remainingText);
                stmt = _cnn._sql.Prepare(_remainingText, (_statementList == null) ? null : _statementList[_statementList.Count - 1], (uint)(_commandTimeout * 1000), out _remainingText);
                if (stmt != null) {
                    stmt._command = this;
                    if (_statementList == null)
                        _statementList = new List<SqliteStatement>();

                    _statementList.Add(stmt);

                    _parameterCollection.MapParameters(stmt);
                    stmt.BindParameters();
                }
                return stmt;
            }
            catch (Exception) {
                if (stmt != null) {
                    if (_statementList.Contains(stmt))
                        _statementList.Remove(stmt);

                    stmt.Dispose();
                }

                // If we threw an error compiling the statement, we cannot continue on so set the remaining text to null.
                _remainingText = null;

                throw;
            }
        }

        internal SqliteStatement GetStatement(int index) {
            // Haven't built any statements yet
            if (_statementList == null) return BuildNextCommand();

            // If we're at the last built statement and want the next unbuilt statement, then build it
            if (index == _statementList.Count) {
                if (String.IsNullOrWhiteSpace(_remainingText) == false) return BuildNextCommand();
                else return null; // No more commands
            }

            SqliteStatement stmt = _statementList[index];
            stmt.BindParameters();

            return stmt;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override void Cancel() {
            if (_activeReader != null) {
                SqliteDataReader reader = _activeReader.Target as SqliteDataReader;
                if (reader != null)
                    reader.Cancel();
            }
        }

        /// <summary>
        /// The SQL command text associated with the command
        /// </summary>
        [DefaultValue("")]
        public override string CommandText {
            get {
                return _commandText;
            }
            set {
                if (_commandText == value) return;

                if (_activeReader != null && _activeReader.IsAlive) {
                    throw new InvalidOperationException("Cannot set CommandText while a DataReader is active");
                }

                ClearCommands();
                _commandText = value;

                if (_cnn == null) return;
            }
        }

        /// <summary>
        /// The amount of time to wait for the connection to become available before erroring out
        /// </summary>
        [DefaultValue((int)30)]
        public override int CommandTimeout {
            get {
                return _commandTimeout;
            }
            set {
                _commandTimeout = value;
            }
        }

        /// <summary>
        /// The type of the command.  SQLite only supports CommandType.Text
        /// </summary>
        [DefaultValue(CommandType.Text)]
        public override CommandType CommandType {
            get {
                return CommandType.Text;
            }
            set {
                if (value != CommandType.Text) {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Forwards to the local CreateParameter() function
        /// </summary>
        /// <returns></returns>
        protected override DbParameter CreateDbParameter() {
            return CreateParameter();
        }

        /// <summary>
        /// Create a new parameter
        /// </summary>
        /// <returns></returns>
        public new SqliteParameter CreateParameter() {
            return new SqliteParameter();
        }

        /// <summary>
        /// The connection associated with this command
        /// </summary>
        [DefaultValue((string)null)]
        public new SqliteAdoConnection Connection {
            get { return _cnn; }
            set {
                if (_activeReader != null && _activeReader.IsAlive)
                    throw new InvalidOperationException("Cannot set Connection while a DataReader is active");

                if (_cnn != null) {
                    ClearCommands();
                    //_cnn.RemoveCommand(this);
                }

                _cnn = value;
                if (_cnn != null) {
                    _version = _cnn._version;
                    _cryptEngine = _cryptEngine ?? _cnn._cryptEngine;
                }
                //if (_cnn != null)
                //  _cnn.AddCommand(this);
            }
        }

        /// <summary>
        /// Forwards to the local Connection property
        /// </summary>
        protected override DbConnection DbConnection {
            get {
                return Connection;
            }
            set {
                Connection = (SqliteAdoConnection)value;
            }
        }

        /// <summary>
        /// Returns the SqliteParameterCollection for the given command
        /// </summary>
        public new SqliteParameterCollection Parameters {
            get { return _parameterCollection; }
        }

        /// <summary>
        /// Forwards to the local Parameters property
        /// </summary>
        protected override DbParameterCollection DbParameterCollection {
            get {
                return Parameters;
            }
        }

        /// <summary>
        /// The transaction associated with this command.  SQLite only supports one transaction per connection, so this property forwards to the
        /// command's underlying connection.
        /// </summary>
        public new SqliteTransaction Transaction {
            get { return _transaction; }
            set {
                if (_cnn != null) {
                    if (_activeReader != null && _activeReader.IsAlive)
                        throw new InvalidOperationException("Cannot set Transaction while a DataReader is active");

                    if (value != null) {
                        if (value._cnn != _cnn)
                            throw new ArgumentException("Transaction is not associated with the command's connection");
                    }
                    _transaction = value;
                }
                else {
                    Connection = value.Connection;
                    _transaction = value;
                }
            }
        }

        /// <summary>
        /// Forwards to the local Transaction property
        /// </summary>
        protected override DbTransaction DbTransaction {
            get {
                return Transaction;
            }
            set {
                Transaction = (SqliteTransaction)value;
            }
        }

        /// <summary>
        /// This function ensures there are no active readers, that we have a valid connection,
        /// that the connection is open, that all statements are prepared and all parameters are assigned
        /// in preparation for allocating a data reader.
        /// </summary>
        private void InitializeForReader() {
            if (_activeReader != null && _activeReader.IsAlive)
                throw new InvalidOperationException("DataReader already active on this command");

            if (_cnn == null)
                throw new InvalidOperationException("No connection associated with this command");

            if (_cnn.State != ConnectionState.Open)
                throw new InvalidOperationException("Database is not open");

            // If the version of the connection has changed, clear out any previous commands before starting
            if (_cnn._version != _version) {
                _version = _cnn._version;
                ClearCommands();
            }

            // Map all parameters for statements already built
            _parameterCollection.MapParameters(null);

            //// Set the default command timeout
            //_cnn._sql.SetTimeout(_commandTimeout * 1000);
        }

        /// <summary>
        /// Creates a new SqliteDataReader to execute/iterate the array of SQLite prepared statements
        /// </summary>
        /// <param name="behavior">The behavior the data reader should adopt</param>
        /// <returns>Returns a SqliteDataReader object</returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            return ExecuteReader(behavior);
        }

        /// <summary>
        /// Overrides the default behavior to return a SqliteDataReader specialization class
        /// </summary>
        /// <param name="behavior">The flags to be associated with the reader</param>
        /// <returns>A SqliteDataReader</returns>
        public new SqliteDataReader ExecuteReader(CommandBehavior behavior) {
            InitializeForReader();

            SqliteDataReader rd = new SqliteDataReader(this, behavior);
            _activeReader = new WeakReference(rd, false);

            return rd;
        }

        internal SqliteDataReader ExecuteReaderWithRowId(CommandBehavior behavior) {
            InitializeForReader();

            SqliteDataReader rd = new SqliteDataReader(this, true, behavior, null);
            _activeReader = new WeakReference(rd, false);

            return rd;
        }

        /// <summary>
        /// Overrides the default behavior of DbDataReader to return a specialized SqliteDataReader class
        /// </summary>
        /// <returns>A SqliteDataReader</returns>
        public new SqliteDataReader ExecuteReader() {
            return ExecuteReader(CommandBehavior.Default);
        }

        /// <summary>
        /// Called by the SqliteDataReader when the data reader is closed.
        /// </summary>
        internal void ClearDataReader() {
            _activeReader = null;
        }

        /// <summary>
        /// Execute the command and return the number of rows inserted/updated affected by it.
        /// </summary>
        /// <returns>Number of rows</returns>
        public override int ExecuteNonQuery() {
            using (SqliteDataReader reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult)) {
                while (reader.NextResult()) ;
                return reader.RecordsAffected;
            }
        }

        /// <summary>
        /// Executes the command and return the Id of last inserted row.
        /// </summary>
        /// <returns>Id of last inserted row</returns>
        public long ExecuteReturnRowId() {
            using (SqliteDataReader reader = ExecuteReaderWithRowId(CommandBehavior.SingleRow | CommandBehavior.SingleResult)) {
                while (reader.NextResult()) ;
                return reader.LastInsertRowId;
            }
        }

        /// <summary>
        /// Execute the command and return the first column of the first row of the resultset
        /// (if present), or null if no resultset was returned.
        /// </summary>
        /// <returns>The first column of the first row of the first resultset from the query</returns>
        public override object ExecuteScalar() {
            using (SqliteDataReader reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult)) {
                if (reader.Read())
                    return reader[0];
            }
            return null;
        }

        /// <summary>
        /// A variation of DbCommand.ExecuteScalar() that allows you to specify the Type of the returned value
        /// </summary>
        /// <typeparam name="T">The type of value to return</typeparam>
        /// <param name="dbNullHandling">Determines how table column values of NULL are handled</param>
        /// <returns>A value of the specified type</returns>
        public T ExecuteScalar<T>(DbNullHandling dbNullHandling = DbNullHandling.ThrowDbNullException) {
            T result = default(T);
            using (SqliteDataReader reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult)) {
                if (reader.Read()) {
                    if (reader.IsDBNull(0)) {
                        if (dbNullHandling == DbNullHandling.ThrowDbNullException) throw new DbNullException();
                    }
                    else {
                        result = (T)Convert.ChangeType(reader[0], typeof(T));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// A variation of DbCommand.ExecuteScalar() that allows you to specify that a decrypted value of the specified type will be returned
        /// </summary>
        /// <typeparam name="T">The type of value to return after decryption</typeparam>
        /// <param name="dbNullHandling">Determines how table column values of NULL are handled</param>
        /// <returns>A decrypted value of the specified type</returns>
        public T ExecuteDecrypt<T>(DbNullHandling dbNullHandling = DbNullHandling.ThrowDbNullException) {
            T result = default(T);
            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            using (SqliteDataReader reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult)) {
                if (reader.Read()) {
                    if (reader.IsDBNull(0)) {
                        if (dbNullHandling == DbNullHandling.ThrowDbNullException) throw new DbNullException();
                    }
                    else {
                        result = _cryptEngine.DecryptObject<T>(reader[0].ToString());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Does nothing.  Commands are prepared as they are executed the first time, and kept in prepared state afterwards.
        /// </summary>
        public override void Prepare() {
        }

        /// <summary>
        /// Sets the method the SqliteCommandBuilder uses to determine how to update inserted or updated rows in a DataTable.
        /// </summary>
        [DefaultValue(UpdateRowSource.None)]
        public override UpdateRowSource UpdatedRowSource {
            get {
                return _updateRowSource;
            }
            set {
                _updateRowSource = value;
            }
        }

        /// <summary>
        /// Clones a command, including all its parameters
        /// </summary>
        /// <returns>A new SqliteCommand with the same commandtext, connection and parameters</returns>
        public object Clone() {
            return new SqliteCommand(this);
        }

        /// <summary>
        /// Adds a Sqlite parameter to the command with properties matching the specified parameter, but with the value encrypted
        /// </summary>
        /// <param name="parameter">The parameter to be added after the value has been encrypted</param>
        public void AddEncryptedParameter(SqliteParameter parameter) {
            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (parameter != null && parameter.ParameterName != null && parameter.ParameterName.Trim() != "") {
                if (parameter.Direction != ParameterDirection.Input)
                    throw new ArgumentException("Only Input parameters can be encrypted.", "parameter");
                this.Parameters.Add(new SqliteParameter(parameter.ParameterName, DbType.String) {
                    Value = _cryptEngine.EncryptObject(parameter.Value),
                    Direction = ParameterDirection.Input
                });
            }
        }

    }
}