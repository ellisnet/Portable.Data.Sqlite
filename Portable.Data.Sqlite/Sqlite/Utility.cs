using System;
using System.Globalization;

namespace Portable.Data.Sqlite
{
	internal static class Utility
	{
		public static void Dispose<T>(ref T disposable)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				disposable.Dispose();
				disposable = null;
			}
		}

		public static int ExecuteNonQuery(this IDbConnection connection, string commandText)
		{
			return ExecuteNonQuery(connection, null, commandText);
		}

		public static int ExecuteNonQuery(this IDbConnection connection, IDbTransaction transaction, string commandText)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;
				command.Transaction = transaction;
				return command.ExecuteNonQuery();
			}
		}

		public static IDataReader ExecuteReader(this IDbConnection connection, string commandText)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;
				return command.ExecuteReader();
			}
		}
		
		public static string FormatInvariant(this string format, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, format, args);
		}

		public static T ParseEnum<T>(string value)
			where T : struct
		{
			return (T) Enum.Parse(typeof(T), value, true);
		}

		public static void ThrowOnError(this SqliteErrorCode errorCode)
		{
			if (errorCode != SqliteErrorCode.Ok)
				throw new SqliteException(errorCode);
		}
	}
}
