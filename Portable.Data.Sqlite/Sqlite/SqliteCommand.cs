using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portable.Data.Sqlite {

	public sealed class SqliteCommand : IDbCommand {

        private SqliteLockContext _lockContext;
        public SqliteLockContext LockContext {
            get { return _lockContext; }
        }

        internal IObjectCryptEngine _cryptEngine = null;
        private static readonly string NO_CRYPT_ENGINE = "Cryptography has not been enabled on this SQLite command.";

        public SqliteCommand()
			: this(null, null, null, null)
		{
		}

        public SqliteCommand(IObjectCryptEngine cryptEngine)
            : this(null, null, null, cryptEngine) 
        {
        }

        public SqliteCommand(string commandText, IObjectCryptEngine cryptEngine = null)
			: this(commandText, null, null, cryptEngine)
		{
		}

		public SqliteCommand(SqliteConnection connection, IObjectCryptEngine cryptEngine = null)
			: this(null, connection, null, cryptEngine)
		{
		}

		public SqliteCommand(string commandText, SqliteConnection connection, IObjectCryptEngine cryptEngine = null)
			: this(commandText, connection, null, cryptEngine)
		{
		}

		public SqliteCommand(string commandText, SqliteConnection connection, IDbTransaction transaction, IObjectCryptEngine cryptEngine = null) {
			_commandText = String.IsNullOrWhiteSpace(commandText) ? null : commandText.Trim();
            _dbConnection = connection;
		    _lockContext = connection?.LockContext;
		    _cryptEngine = cryptEngine ?? connection?._cryptEngine;
		    Transaction = transaction;
			m_parameterCollection = new SqliteParameterCollection();
		}

		public void Prepare() {
			if (m_statementPreparer == null)
				m_statementPreparer = new SqliteStatementPreparer(DatabaseHandle, _commandText);
		}

	    private string _commandText;
	    public string CommandText {
	        get { return _commandText; }
	        set {
	            _commandText = String.IsNullOrWhiteSpace(value) ? null : value.Trim();
                //need to reset the statementpreparer, since the statement to be executed has changed
	            m_statementPreparer = null;
	        }
	    }

	    public int CommandTimeout { get; set; }

		public CommandType CommandType {
			get {
				return CommandType.Text;
			}
			set {
				if (value != CommandType.Text)
					throw new ArgumentException("CommandType must be Text.", nameof(value));
			}
		}

		public UpdateRowSource UpdatedRowSource {
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public IDbConnection Connection {
		    get { return _dbConnection; }
		    set { this.DbConnection = value as SqliteConnection; }
		}

	    private SqliteConnection _dbConnection;
        public SqliteConnection DbConnection {
            get { return _dbConnection; }
            set {
                _dbConnection = value;
                _lockContext = value?.LockContext;
            }
        }

		public IDataParameterCollection Parameters {
			get {
				VerifyCommandNotDisposed();
				return m_parameterCollection;
			}
		}

		public IDbTransaction Transaction { get; set; }

		public void Cancel() {
			throw new NotImplementedException();
		}

		public IDbDataParameter CreateParameter() {
			VerifyCommandNotDisposed();
			return new SqliteParameter();
		}

		public IDataReader ExecuteReader(CommandBehavior behavior) {
			VerifyCommandIsValid();
			Prepare();
			return SqliteDataReader.Create(this, behavior);
		}

	    public IDataReader ExecuteReader() {
            return ExecuteReader(CommandBehavior.Default);
        }

        /// <summary>
        /// Execute the command and return the number of rows inserted/updated affected by it.
        /// </summary>
        /// <returns>Number of rows</returns>
        public int ExecuteNonQuery() {
			using (var reader = ExecuteReader()) {
				do {
					while (reader.Read()) {}
				} while (reader.NextResult());
				return reader.RecordsAffected;
			}
		}

        /// <summary>
        /// Executes the command and return the Id of last inserted row.
        /// </summary>
        /// <returns>Id of last inserted row</returns>
        public long ExecuteReturnRowId() {
            using (var reader = ExecuteReader()) {
                reader.SetRowId = true;
                do {
                    while (reader.Read()) { }
                } while (reader.NextResult());
                return reader.LastInsertRowId;
            }
        }

        /// <summary>
        /// Execute the command and return the first column of the first row of the resultset
        /// (if present), or null if no resultset was returned.
        /// </summary>
        /// <returns>The first column of the first row of the first resultset from the query</returns>
        public object ExecuteScalar() {
			using (var reader = ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow)) {
				do {
					if (reader.Read())
						return reader.GetValue(0);
				} while (reader.NextResult());
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
            using (var reader = ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow)) {
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
            using (var reader = ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow)) {
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

        //public Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken) {
        //    return Task.Run(() => {
        //        return ExecuteReader(CommandBehavior.Default);
        //    }, cancellationToken);
        //}

        //public Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
        //    return Task.Run(() => {
        //        return ExecuteReader(behavior);
        //    }, cancellationToken);
        //}

        public Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken) {
	        return ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
	    }

	    public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
            using (var reader = await ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)) {
                do {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                    }
                } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
                return reader.RecordsAffected;
            }
        }

        public Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
            VerifyCommandIsValid();
            Prepare();
            return SqliteDataReader.CreateAsync(this, behavior, cancellationToken);
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) {
            using (var reader = await ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false)) {
                do {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        return reader.GetValue(0);
                } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }
            return null;
        }

        /// <summary>
        /// Adds a Sqlite parameter to the command with properties matching the specified parameter, but with the value encrypted
        /// </summary>
        /// <param name="parameter">The parameter to be added after the value has been encrypted</param>
        public void AddEncryptedParameter(SqliteParameter parameter) {
            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (parameter != null && parameter.ParameterName != null && parameter.ParameterName.Trim() != "") {
                if (parameter.Direction != ParameterDirection.Input)
                    throw new ArgumentException("Only Input parameters can be encrypted.", nameof(parameter));
                this.Parameters.Add(new SqliteParameter(parameter.ParameterName, DbType.String) {
                    Value = _cryptEngine.EncryptObject(parameter.Value),
                    Direction = ParameterDirection.Input
                });
            }
        }

        private void Dispose(bool disposing) {
			try
			{
				m_parameterCollection = null;
				Utility.Dispose(ref m_statementPreparer);
			}
			finally
			{
				//not needed
			}
		}

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //protected bool CanRaiseEvents
        //{
        //    get { return false; }
        //}

        internal SqliteStatementPreparer GetStatementPreparer() {
            if (m_statementPreparer == null) {
                VerifyCommandIsValid();
                Prepare();
            }
            m_statementPreparer.AddRef();
			return m_statementPreparer;
		}

		private SqliteDatabaseHandle DatabaseHandle {
			get { return ((SqliteConnection) Connection).Handle; }
		}

		private void VerifyCommandNotDisposed() {
			if (m_parameterCollection == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private void VerifyCommandIsValid() {
			VerifyCommandNotDisposed();
			if (Connection == null)
				throw new InvalidOperationException("Connection property must be non-null.");
			if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
				throw new InvalidOperationException("Connection must be Open; current state is {0}.".FormatInvariant(Connection.State));
			if (Transaction != ((SqliteConnection) Connection).CurrentTransaction)
				throw new InvalidOperationException("The transaction associated with this command is not the connection's active transaction.");
			if (string.IsNullOrWhiteSpace(_commandText))
				throw new InvalidOperationException("CommandText must be specified");
		}

	    // ReSharper disable once InconsistentNaming
		SqliteParameterCollection m_parameterCollection;
	    // ReSharper disable once InconsistentNaming
		SqliteStatementPreparer m_statementPreparer;
	}
}
