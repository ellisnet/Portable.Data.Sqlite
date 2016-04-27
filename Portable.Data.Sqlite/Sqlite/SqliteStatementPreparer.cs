// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;

namespace Portable.Data.Sqlite
{
	internal sealed class SqliteStatementPreparer : IDisposable
	{
		public SqliteStatementPreparer(SqliteDatabaseHandle database, string commandText)
		{
            if (database == null) { throw new ArgumentNullException(nameof(database));}
			m_database = database;
			m_commandTextBytes = SqliteConnection.ToUtf8(commandText);
			m_statements = new List<SqliteStatementHandle>();
			m_refCount = 1;
		}

		public SqliteStatementHandle Get(int index, CancellationToken cancellationToken)
		{
			if (m_statements == null)
				throw new ObjectDisposedException(GetType().Name);
			if (index < 0 || index > m_statements.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (index < m_statements.Count)
				return m_statements[index];
			if (m_bytesUsed == m_commandTextBytes.Length)
				return null;

			Random random = null;
			SqliteErrorCode errorCode;
			do
			{
				unsafe
				{
					fixed (byte* sqlBytes = &m_commandTextBytes[m_bytesUsed])
					{
						byte* remainingSqlBytes;
						SqliteStatementHandle statement;
						errorCode = m_database.LockContext.sqlite3_prepare_v2(m_database, sqlBytes, m_commandTextBytes.Length - m_bytesUsed, out statement, out remainingSqlBytes);
						switch (errorCode)
						{
						case SqliteErrorCode.Ok:
							m_bytesUsed += (int) (remainingSqlBytes - sqlBytes);
							m_statements.Add(statement);
							break;

						case SqliteErrorCode.Busy:
						case SqliteErrorCode.Locked:
						case SqliteErrorCode.CantOpen:
							if (cancellationToken.IsCancellationRequested)
								return null;
							if (random == null)
								random = new Random();
							Thread.Sleep(random.Next(1, 150));
							break;

						default:
							throw new SqliteException(errorCode, m_database);
						}
					}
				}
			} while (errorCode != SqliteErrorCode.Ok);

			return m_statements[index];
		}

		public void AddRef()
		{
			if (m_refCount == 0)
				throw new ObjectDisposedException(GetType().Name);
			m_refCount++;
		}

		public void Dispose()
		{
			m_refCount--;
			if (m_refCount == 0)
			{
				foreach (var statement in m_statements)
					statement.Dispose();
				m_statements = null;
			}
			else if (m_refCount < 0)
			{
				throw new InvalidOperationException("SqliteStatementList ref count decremented below zero.");
			}
		}

		readonly SqliteDatabaseHandle m_database;
		readonly byte[] m_commandTextBytes;
		List<SqliteStatementHandle> m_statements;
		int m_bytesUsed;
		int m_refCount;
	}
}
