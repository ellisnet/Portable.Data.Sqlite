/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

//Was: namespace MonoDataSqliteWrapper {
namespace Portable.Data.Sqlite.Wrapper {
    public sealed class SqliteContextHandle { }
    public sealed class SqliteStatementHandle { }
    public sealed class SqliteConnectionHandle { }
    public sealed class SqliteValueHandle { }

    public delegate int SqliteCommitHookDelegate(object argument);
    public delegate void SqliteUpdateHookDelegate(object argument, int b, string c, string d, long e);
    public delegate void SqliteRollbackHookDelegate(object argument);
    public delegate void SqliteCallback(SqliteContextHandle context, int nArgs, SqliteValueHandle[] args);
    public delegate void SqliteFinalCallback(SqliteContextHandle context);
    public delegate int SqliteCollation(object puser, int len1, string pv1, int len2, string pv2);

    public sealed class UnsafeNativeMethods {
        public static SqliteValueHandle sqlite3_aggregate_context(SqliteContextHandle context, int nBytes) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_blob(SqliteStatementHandle statement, int index, byte[] value, int length, object dummy) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_double(SqliteStatementHandle statement, int index, double value) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_int(SqliteStatementHandle statement, int index, int value) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_int64(SqliteStatementHandle statement, int index, long value) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_null(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_parameter_count(SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_parameter_index(SqliteStatementHandle statement, string name) { throw new System.NotImplementedException(); }
        public static string sqlite3_bind_parameter_name(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_text(SqliteStatementHandle statement, int index, string value, int length, object dummy) { throw new System.NotImplementedException(); }
        public static int sqlite3_bind_text16(SqliteStatementHandle statement, int index, string value, int length) { throw new System.NotImplementedException(); }
        public static int sqlite3_busy_timeout(SqliteConnectionHandle db, int miliseconds) { throw new System.NotImplementedException(); }
        public static int sqlite3_changes(SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static int sqlite3_close(SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static byte[] sqlite3_column_blob(SqliteStatementHandle __unnamed000, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_column_bytes(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_column_count(SqliteStatementHandle rstatement) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_database_name(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_database_name16(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_decltype(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static double sqlite3_column_double(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_column_int(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static long sqlite3_column_int64(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_name(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_name16(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_origin_name(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_origin_name16(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_table_name(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_table_name16(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_text(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static string sqlite3_column_text16(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static int sqlite3_column_type(SqliteStatementHandle statement, int index) { throw new System.NotImplementedException(); }
        public static void sqlite3_commit_hook(SqliteConnectionHandle db, SqliteCommitHookDelegate callback, object userState) { throw new System.NotImplementedException(); }
        public static int sqlite3_config(int option, object[] arguments) { throw new System.NotImplementedException(); }
        public static string sqlite3_errmsg(SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static int sqlite3_exec(SqliteConnectionHandle db, string query, out string errmsg) { throw new System.NotImplementedException(); }
        public static int sqlite3_finalize(SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static void sqlite3_interrupt(SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static int sqlite3_key(SqliteConnectionHandle db, string key, int length) { throw new System.NotImplementedException(); }
        public static long sqlite3_last_insert_rowid(SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static string sqlite3_libversion() { throw new System.NotImplementedException(); }
        public static SqliteStatementHandle sqlite3_next_stmt(SqliteConnectionHandle db, SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static int sqlite3_open(string filename, out SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static int sqlite3_open_v2(string filename, out SqliteConnectionHandle db, int flags, string zVfs) { throw new System.NotImplementedException(); }
        public static int sqlite3_open16(string filename, out SqliteConnectionHandle db) { throw new System.NotImplementedException(); }
        public static int sqlite3_prepare_v2(SqliteConnectionHandle db, string query, out SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static int sqlite3_prepare16(SqliteConnectionHandle db, string query, int length, out SqliteStatementHandle statement, out string strRemain) { throw new System.NotImplementedException(); }
        public static int sqlite3_rekey(SqliteConnectionHandle db, string key, int length) { throw new System.NotImplementedException(); }
        public static int sqlite3_reset(SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_blob(SqliteContextHandle context, byte[] value, int length, object dummy) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_double(SqliteContextHandle statement, double value) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_error(SqliteContextHandle statement, string value, int index) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_error16(SqliteContextHandle statement, string value, int index) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_int(SqliteContextHandle statement, int value) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_int64(SqliteContextHandle statement, long value) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_null(SqliteContextHandle statement) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_text(SqliteContextHandle statement, string value, int index, object dummy) { throw new System.NotImplementedException(); }
        public static void sqlite3_result_text16(SqliteContextHandle statement, string value, int index, object dummy) { throw new System.NotImplementedException(); }
        public static void sqlite3_rollback_hook(SqliteConnectionHandle db, SqliteRollbackHookDelegate callback, object userState) { throw new System.NotImplementedException(); }
        public static int sqlite3_step(SqliteStatementHandle statement) { throw new System.NotImplementedException(); }
        public static int sqlite3_table_column_metadata(SqliteConnectionHandle db, string dbName, string tableName, string columnName, out string dataType, out string collSeq, out int notNull, out int primaryKey, out int autoInc) { throw new System.NotImplementedException(); }
        public static void sqlite3_update_hook(SqliteConnectionHandle db, SqliteUpdateHookDelegate callback, object userState) { throw new System.NotImplementedException(); }
        public static byte[] sqlite3_value_blob(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static int sqlite3_value_bytes(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static double sqlite3_value_double(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static int sqlite3_value_int(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static long sqlite3_value_int64(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static string sqlite3_value_text(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static string sqlite3_value_text16(SqliteValueHandle value) { throw new System.NotImplementedException(); }
        public static int sqlite3_value_type(SqliteValueHandle value) { throw new System.NotImplementedException(); }
    }
}
