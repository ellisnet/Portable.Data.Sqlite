// ReSharper disable InconsistentNaming

using System;

namespace Portable.Data.Sqlite
{
	public sealed class SqliteTransaction : IDbTransaction {

		internal SqliteTransaction(SqliteConnection connection, IsolationLevel isolationLevel)
		{
			m_connection = connection;
			m_isolationLevel = isolationLevel;
		}

		public void Commit()
		{
			VerifyNotDisposed();
			if (m_isFinished)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				if (m_connection.IsOnlyTransaction(this))
					m_connection.ExecuteNonQuery(this, "COMMIT");
				m_connection.PopTransaction();
				m_isFinished = true;
			}
			else if (m_connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (m_connection.CurrentTransaction == null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		public void Rollback()
		{
			VerifyNotDisposed();
			if (m_isFinished)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				if (m_connection.IsOnlyTransaction(this))
				{
					m_connection.ExecuteNonQuery(this, "ROLLBACK");
					m_connection.PopTransaction();
					m_isFinished = true;
				}
				else
				{
					throw new InvalidOperationException("Can't roll back nested transaction.");
				}
			}
			else if (m_connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (m_connection.CurrentTransaction == null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		public IDbConnection Connection
		{
			get
			{
				VerifyNotDisposed();
				return m_connection;
			}
		}

		public IsolationLevel IsolationLevel
		{
			get
			{
				VerifyNotDisposed();
				return m_isolationLevel;
			}
		}

		private void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (!m_isFinished && m_connection != null && m_connection.CurrentTransaction == this && m_connection.IsOnlyTransaction(this))
					{
						m_connection.ExecuteNonQuery(this, "ROLLBACK");
						m_connection.PopTransaction();
					}
					m_connection = null;
				}
			}
			finally
			{
				//nothing
			}
		}

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void VerifyNotDisposed()
		{
			if (m_connection == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		SqliteConnection m_connection;
		readonly IsolationLevel m_isolationLevel;
		bool m_isFinished;
	}
}
