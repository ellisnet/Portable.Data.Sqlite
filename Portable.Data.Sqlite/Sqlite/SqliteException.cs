using System;

using Portable.Data.Common;

namespace Portable.Data.Sqlite
{
#if NET45 || MAC
	[Serializable]
#endif
	public sealed class SqliteException : DbException
	{
		public SqliteException(SqliteErrorCode errorCode)
			: this(errorCode, null)
		{
		}

		internal SqliteException(SqliteErrorCode errorCode, SqliteDatabaseHandle database)
			: base(GetErrorString(errorCode, database), (int) errorCode)
		{
		}

		private static string GetErrorString(SqliteErrorCode errorCode, SqliteDatabaseHandle database)
		{
#if NET45
		    string errorString = (database == null)
		        ? SqliteConnection.FromUtf8(NativeMethods.sqlite3_errstr(errorCode))  //have to use native method, because no lock context
		        : SqliteConnection.FromUtf8(database.LockContext.sqlite3_errstr(errorCode));
#else
			string errorString = errorCode.ToString();
#endif
            return database != null ? "{0}: {1}".FormatInvariant(errorString, SqliteConnection.FromUtf8(database.LockContext.sqlite3_errmsg(database)))
				: errorString;
		}
	}
}
