/*
   Copyright 2014 Ellisnet - Jeremy Ellis (jeremy@ellisnet.com)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;

namespace Portable.Data.Sqlite {
    public class SqliteLockContext {

        private readonly object _locker = new object();

        internal SqliteBackupHandle sqlite3_backup_init(SqliteDatabaseHandle pDest, byte[] zDestName,
            SqliteDatabaseHandle pSource, byte[] zSourceName) {
            lock (_locker) {
                SqliteBackupHandle result = NativeMethods.sqlite3_backup_init(pDest, zDestName, pSource, zSourceName);
                if (result != null) { result.LockContext = this;}
                return result;
            }
        }

        internal SqliteErrorCode sqlite3_backup_step(SqliteBackupHandle p, int nPage) {
            lock (_locker) {
                return NativeMethods.sqlite3_backup_step(p, nPage);
            }
        }

        internal SqliteErrorCode sqlite3_backup_finish(IntPtr p) {
            lock (_locker) {
                return NativeMethods.sqlite3_backup_finish(p);
            }
        }

        internal int sqlite3_backup_remaining(SqliteBackupHandle p) {
            lock (_locker) {
                return NativeMethods.sqlite3_backup_remaining(p);
            }
        }

        internal int sqlite3_backup_pagecount(SqliteBackupHandle p) {
            lock (_locker) {
                return NativeMethods.sqlite3_backup_pagecount(p);
            }
        }

        internal SqliteErrorCode sqlite3_bind_blob(SqliteStatementHandle stmt, int ordinal, byte[] value, int count, IntPtr free) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_blob(stmt, ordinal, value, count, free);
            }
        }

        internal SqliteErrorCode sqlite3_bind_double(SqliteStatementHandle stmt, int ordinal, double value) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_double(stmt, ordinal, value);
            }
        }

        internal SqliteErrorCode sqlite3_bind_int(SqliteStatementHandle stmt, int ordinal, int value) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_int(stmt, ordinal, value);
            }
        }

        internal SqliteErrorCode sqlite3_bind_int64(SqliteStatementHandle stmt, int ordinal, long value) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_int64(stmt, ordinal, value);
            }
        }

        internal SqliteErrorCode sqlite3_bind_null(SqliteStatementHandle stmt, int ordinal) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_null(stmt, ordinal);
            }
        }

        internal int sqlite3_bind_parameter_index(SqliteStatementHandle stmt, byte[] zName) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_parameter_index(stmt, zName);
            }
        }

        internal SqliteErrorCode sqlite3_bind_text(SqliteStatementHandle stmt, int ordinal, byte[] value, int count, IntPtr free) {
            lock (_locker) {
                return NativeMethods.sqlite3_bind_text(stmt, ordinal, value, count, free);
            }
        }

        internal SqliteErrorCode sqlite3_close(IntPtr db) {
            lock (_locker) {
                return NativeMethods.sqlite3_close(db);
            }
        }

        internal SqliteErrorCode sqlite3_close_v2(IntPtr db) {
            lock (_locker) {
                return NativeMethods.sqlite3_close_v2(db);
            }
        }

        internal IntPtr sqlite3_column_blob(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_blob(stmt, index);
            }
        }

        internal int sqlite3_column_bytes(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_bytes(stmt, index);
            }
        }

        internal int sqlite3_column_count(SqliteStatementHandle stmt) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_count(stmt);
            }
        }

        internal IntPtr sqlite3_column_decltype(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_decltype(stmt, index);
            }
        }

        internal double sqlite3_column_double(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_double(stmt, index);
            }
        }

        internal int sqlite3_column_int(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_int(stmt, index);
            }
        }

        internal long sqlite3_column_int64(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_int64(stmt, index);
            }
        }

        internal IntPtr sqlite3_column_name(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_name(stmt, index);
            }
        }

        internal IntPtr sqlite3_column_text(SqliteStatementHandle stmt, int index) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_text(stmt, index);
            }
        }

        internal SqliteColumnType sqlite3_column_type(SqliteStatementHandle statement, int ordinal) {
            lock (_locker) {
                return NativeMethods.sqlite3_column_type(statement, ordinal);
            }
        }

        internal SqliteErrorCode sqlite3_config_log(SqliteConfigOpsEnum op, SQLiteLogCallback func, IntPtr pvUser) {
            lock (_locker) {
                return NativeMethods.sqlite3_config_log(op, func, pvUser);
            }
        }

        internal SqliteErrorCode sqlite3_config_log_arm64(SqliteConfigOpsEnum op, IntPtr p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6, IntPtr p7, IntPtr p8, SQLiteLogCallback func, IntPtr pvUser) {
            lock (_locker) {
                return NativeMethods.sqlite3_config_log_arm64(op, p2, p3, p4, p5, p6, p7, p8, func, pvUser);
            }
        }

        internal int sqlite3_db_readonly(SqliteDatabaseHandle db, string zDbName) {
            lock (_locker) {
                return NativeMethods.sqlite3_db_readonly(db, zDbName);
            }
        }

        internal SqliteErrorCode sqlite3_errcode(SqliteDatabaseHandle db) {
            lock (_locker) {
                return NativeMethods.sqlite3_errcode(db);
            }
        }

        internal IntPtr sqlite3_errmsg(SqliteDatabaseHandle db) {
            lock (_locker) {
                return NativeMethods.sqlite3_errmsg(db);
            }
        }

        internal IntPtr sqlite3_errstr(SqliteErrorCode rc) {
            lock (_locker) {
                return NativeMethods.sqlite3_errstr(rc);
            }
        }

        internal SqliteErrorCode sqlite3_finalize(IntPtr stmt) {
            lock (_locker) {
                return NativeMethods.sqlite3_finalize(stmt);
            }
        }

        internal SqliteErrorCode sqlite3_key(SqliteDatabaseHandle db, byte[] key, int keylen) {
            lock (_locker) {
                return NativeMethods.sqlite3_key(db, key, keylen);
            }
        }

        internal SqliteErrorCode sqlite3_open_v2(byte[] utf8Filename, out SqliteDatabaseHandle db, SqliteOpenFlags flags, byte[] vfs) {
            lock (_locker) {
                SqliteErrorCode result = NativeMethods.sqlite3_open_v2(utf8Filename, out db, flags, vfs);
                if (db != null) {
                    db.LockContext = this;
                }
                return result;
            }
        }

        internal unsafe SqliteErrorCode sqlite3_prepare_v2(SqliteDatabaseHandle db, byte* pSql, int nBytes, out SqliteStatementHandle stmt, out byte* pzTail) {
            lock (_locker) {
                SqliteErrorCode result = NativeMethods.sqlite3_prepare_v2(db, pSql, nBytes, out stmt, out pzTail);
                if (stmt != null) {
                    stmt.LockContext = this;
                }
                return result;
            }
        }

        internal void sqlite3_profile(SqliteDatabaseHandle db, SqliteProfileCallback callback, IntPtr userData) {
            lock (_locker) {
                NativeMethods.sqlite3_profile(db, callback, userData);
            }
        }

        internal void sqlite3_progress_handler(SqliteDatabaseHandle db, int virtualMachineInstructions, SQLiteProgressCallback callback, IntPtr userData) {
            lock (_locker) {
                NativeMethods.sqlite3_progress_handler(db, virtualMachineInstructions, callback, userData);
            }
        }

        internal SqliteErrorCode sqlite3_reset(SqliteStatementHandle stmt) {
            lock (_locker) {
                return NativeMethods.sqlite3_reset(stmt);
            }
        }

        internal SqliteErrorCode sqlite3_step(SqliteStatementHandle stmt) {
            lock (_locker) {
                return NativeMethods.sqlite3_step(stmt);
            }
        }

        internal long sqlite3_last_insert_rowid(SqliteDatabaseHandle db) {
            lock (_locker) {
                return NativeMethods.sqlite3_last_insert_rowid(db);
            }
        }

        internal long sqlite3_step_return_rowid(SqliteDatabaseHandle db, SqliteStatementHandle stmt, out SqliteErrorCode code) {
            lock (_locker) {
                code = NativeMethods.sqlite3_step(stmt);
                switch (code) {
                    case SqliteErrorCode.Ok:
                    case SqliteErrorCode.Row:
                    case SqliteErrorCode.Done:
                        return NativeMethods.sqlite3_last_insert_rowid(db);
                    default:
                        return -1;
                }
            }
        }

        internal int sqlite3_total_changes(SqliteDatabaseHandle db) {
            lock (_locker) {
                return NativeMethods.sqlite3_total_changes(db);
            }
        }

    }
}
