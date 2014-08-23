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
using System.Collections.Generic;
using System.Linq;
using Portable.Data;
using System.Runtime.InteropServices;

using Portable.Data.Sqlite;

namespace Portable.Data.Sqlite.Wrapper {
    internal class SqliteConnectionWrapper : SqliteBase {

        protected SQLitePCL.ISQLiteConnection _sqliteDbConnection;
        private readonly Object dbLock = new Object();
        private bool _isOpen = false;
        //TODO: Find a way to update this number of changes (i.e. rows affected) after a statement runs
        private int _lastChanges = 0;  //number of records changed in by last statement??
        private string _lastError = "";
        private string _lastErrorStatement = "";

        private List<SQLitePCL.SQLiteResult> _okResultMessages = new List<SQLitePCL.SQLiteResult> {
            SQLitePCL.SQLiteResult.OK,
            SQLitePCL.SQLiteResult.DONE,
            SQLitePCL.SQLiteResult.ROW
        };

        internal SqliteConnectionWrapper(SQLitePCL.ISQLiteConnection connection, SqliteDateFormats fmt)
            : base(connection, fmt) 
        {
            if (connection == null) throw new ArgumentNullException("connection");
            _sqliteDbConnection = connection;
        }

        internal override string Version {
            get { throw new NotImplementedException(); }
        }

        internal override int Changes {
            get { return _lastChanges; }
        }

        internal override void Open(string strFilename, SqliteOpenFlagsEnum flags, int maxPoolSize, bool usePool) {
            if (usePool) throw new NotImplementedException("The 'usePool' option is not implemented.");
            //Not really anything to do with SQLitePCL to open the database.
            _isOpen = true;
        }

        internal override void Close() {
            //Not really anything to do with SQLitePCL to close the database.
            _isOpen = false;
        }

        internal override void SetTimeout(int timeout) {
            throw new NotImplementedException();
        }

        internal override string SqliteLastError() {
            return _lastError;
        }

        internal string SqliteLastErrorStatement() {
            return _lastErrorStatement;
        }

        internal override void ClearPool() {
            throw new NotImplementedException();
        }

        internal override SqliteStatement Prepare(string strSql, SqliteStatement previous, uint timeout, out string strRemain) {
            lock (dbLock) {
                SqliteStatement result = null;

                //TODO: Not using timeout for anything at the moment
                strRemain = null;
                strSql = (strSql ?? "").Trim();
                do {
                    try {
                        if (strSql == "") break;
                        while (strSql.Length > 1 && strSql.Substring(0, 1) == ";") {
                            strSql = strSql.Substring(1).Trim();
                        }
                        if (strSql == "" || strSql == ";") break;

                        if (strSql.IndexOf(";") > 0 && strSql.IndexOf(";") < (strSql.Length - 1)) {
                            strSql = strSql.Substring(0, (strSql.IndexOf(";") + 1));
                            strRemain = strSql.Substring(strSql.IndexOf(";") + 1);
                        }

                        var stmt = _sqliteDbConnection.Prepare(strSql);
                        result = new SqliteStatement(this, stmt, strSql, previous);
                    }
                    catch (Exception ex) {
                        _lastError = ex.Message ?? "No exception info";
                        _lastErrorStatement = strSql;
                        throw;
                    }

	            } while (false);

                return result;
            }
        }

        internal override SQLitePCL.SQLiteResult Step(SqliteStatement stmt) {
            lock (dbLock) {
                var result = SQLitePCL.SQLiteResult.ERROR;

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = stmt._sqlite_stmt.Step();
                    if (!_okResultMessages.Contains(result)) {
                        _lastError = SqliteConvert.ToResultText(result);
                        _lastErrorStatement = stmt._sqlStatement ?? "(none)";
                        throw new Exception(_lastError);
                    }
                }
                
                return result;
            }
        }

        internal override Tuple<SQLitePCL.SQLiteResult, long> StepWithRowId(SqliteStatement stmt) {
            lock (dbLock) {
                long rowid = -1;
                var result = Tuple.Create<SQLitePCL.SQLiteResult, long>(SQLitePCL.SQLiteResult.ERROR, rowid);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    SQLitePCL.SQLiteResult stmtResult = stmt._sqlite_stmt.Step();
                    if (!_okResultMessages.Contains(stmtResult)) {
                        _lastError = SqliteConvert.ToResultText(stmtResult);
                        _lastErrorStatement = stmt._sqlStatement ?? "(none)";
                        throw new Exception(_lastError);
                    }
                    rowid = stmt._sql.LastInsertRowIdNoThreadLock;
                    result = Tuple.Create(stmtResult, rowid);
                }

                return result;
            }
        }

        internal override int Reset(SqliteStatement stmt) {
            lock (dbLock) {
                if (stmt == null || stmt._sqlite_stmt == null) {
                    stmt._sqlite_stmt.Reset();
                    stmt._sqlite_stmt.ClearBindings();
                    return 0;
                }
                else { return -1; }
            }
        }

        internal override void Cancel() {
            throw new NotImplementedException();
        }

        private void _bindValue(SqliteStatement stmt, int index, object value) {
            lock (dbLock) {
                if (stmt != null && stmt._sqlite_stmt != null)
                    stmt._sqlite_stmt.Bind(index, value);
            }
        }

        internal override void Bind_Double(SqliteStatement stmt, int index, double value) {
            this._bindValue(stmt, index, value);
        }

        internal override void Bind_Int32(SqliteStatement stmt, int index, int value) {
            this._bindValue(stmt, index, value);
        }

        internal override void Bind_Int64(SqliteStatement stmt, int index, long value) {
            this._bindValue(stmt, index, value);
        }

        internal override void Bind_Text(SqliteStatement stmt, int index, string value) {
            this._bindValue(stmt, index, value);
        }

        internal override void Bind_Blob(SqliteStatement stmt, int index, byte[] blobData) {
            this._bindValue(stmt, index, blobData);
        }

        internal override void Bind_DateTime(SqliteStatement stmt, int index, DateTime dt) {
            this._bindValue(stmt, index, dt.ToString());
        }

        internal override void Bind_Null(SqliteStatement stmt, int index) {
            //this._bindValue(stmt, index, Portable.Data.DBNull.Value);
            lock (dbLock) {
                if (stmt != null && stmt._sqlite_stmt != null)
                    stmt._sqlite_stmt.Bind(index, null);
            }
        }

        internal override int Bind_ParamCount(SqliteStatement stmt) {
            return stmt.ParamList.Count;
        }

        internal override string Bind_ParamName(SqliteStatement stmt, int index) {
            string result = null;
            if (stmt == null || stmt.ParamList == null || (!stmt.ParamList.TryGetValue(index, out result)))
                throw new ArgumentException("The parameter at position '" + index.ToString() + "' could not be found.", "index");
            return result;
        }

        internal override int Bind_ParamIndex(SqliteStatement stmt, string paramName) {
            int result = default(int);
            if (paramName == null) throw new ArgumentNullException("paramName");
            paramName = paramName.Trim();
            if (paramName == "") throw new ArgumentOutOfRangeException("paramName");
            if (stmt == null || stmt.ParamList == null ||
                stmt.ParamList.Where(p => p.Value == paramName.ToLower()).Count() == 0) 
            {
                throw new ArgumentException("The parameter '" + paramName + "' could not be found.", "paramName");
            }
            else {
                result = stmt.ParamList.Where(p => p.Value == paramName.ToLower()).First().Key;
            }
            return result;
        }

        internal override int ColumnCount(SqliteStatement stmt) {
            lock (dbLock) {
                return stmt._sqlite_stmt.ColumnCount;
            }
        }

        internal override string ColumnName(SqliteStatement stmt, int index) {
            lock (dbLock) {
                return stmt._sqlite_stmt.ColumnName(index);
            }
        }

        internal override TypeAffinity ColumnAffinity(SqliteStatement stmt, int index) {
            lock (dbLock) {
                TypeAffinity result = TypeAffinity.Text;

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = SqliteConvert.TypeToAffinity(
                        SqliteConvert.DbTypeToType(
                        SqliteConvert.TypeNameToDbType(
                        stmt._sqlite_stmt.DataType(index).ToString())));
                }

                return result;
            }
        }

        internal override string ColumnType(SqliteStatement stmt, int index, out TypeAffinity nAffinity) {
            lock (dbLock) {
                string result = null;
                nAffinity = TypeAffinity.Text;

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = stmt._sqlite_stmt.DataType(index).ToString();
                    nAffinity = SqliteConvert.TypeToAffinity(SqliteConvert.DbTypeToType(SqliteConvert.TypeNameToDbType(result)));
                }

                return result;
            }
        }

        internal override int ColumnIndex(SqliteStatement stmt, string columnName) {
            throw new NotImplementedException();
        }

        internal override string ColumnOriginalName(SqliteStatement stmt, int index) {
            throw new NotImplementedException();
        }

        internal override string ColumnDatabaseName(SqliteStatement stmt, int index) {
            throw new NotImplementedException();
        }

        internal override string ColumnTableName(SqliteStatement stmt, int index) {
            throw new NotImplementedException();
        }

        internal override void ColumnMetaData(string dataBase, string table, string column, out string dataType, out string collateSequence, out bool notNull, out bool primaryKey, out bool autoIncrement) {
            throw new NotImplementedException();
        }

        internal override void GetIndexColumnExtendedInfo(string database, string index, string column, out int sortMode, out int onError, out string collationSequence) {
            throw new NotImplementedException();
        }

        internal override double GetDouble(SqliteStatement stmt, int index) {
            lock (dbLock) {
                double result = default(double);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = stmt._sqlite_stmt.GetFloat(index);
                }

                return result;
            }
        }

        internal override int GetInt32(SqliteStatement stmt, int index) {
            lock (dbLock) {
                int result = default(int);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = Convert.ToInt32(stmt._sqlite_stmt.GetInteger(index));
                }

                return result;
            }
        }

        internal override long GetInt64(SqliteStatement stmt, int index) {
            lock (dbLock) {
                long result = default(long);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = stmt._sqlite_stmt.GetInteger(index);
                }

                return result;
            }
        }

        internal override string GetText(SqliteStatement stmt, int index) {
            lock (dbLock) {
                string result = default(string);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = stmt._sqlite_stmt.GetText(index);
                }

                return result;
            }
        }

        internal override long GetBytes(SqliteStatement stmt, int index, int nDataOffset, byte[] bDest, int nStart, int nLength) {
            lock (dbLock) {
                int result = default(int);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    byte[] bytes = stmt._sqlite_stmt.GetBlob(index);

                    if (bytes != null) {
                        result = bytes.Length;
                        if (bDest != null) {
                            int nCopied = nLength;
                            if (nCopied + nStart > bDest.Length) {
                                nCopied = bDest.Length - nStart;
                            }
                            if (nCopied + nDataOffset > result) {
                                nCopied = result - nDataOffset;
                            }

                            if (nCopied > 0) {
                                Array.Copy(bytes, nStart + nDataOffset, bDest, 0, nCopied);
                            }
                            else {
                                nCopied = 0;
                            }

                            result = nCopied;
                        }
                    }
                }

                return result;
            }
        }

        internal override long GetChars(SqliteStatement stmt, int index, int nDataOffset, char[] bDest, int nStart, int nLength) {
            lock (dbLock) {
                int result = default(int);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    string chars = stmt._sqlite_stmt.GetText(index);

                    if (chars != null) {
                        result = chars.Length;
                        if (bDest != null) {
                            int nCopied = nLength;
                            if (nCopied + nStart > bDest.Length) {
                                nCopied = bDest.Length - nStart;
                            }
                            if (nCopied + nDataOffset > result) {
                                nCopied = result - nDataOffset;
                            }

                            if (nCopied > 0) {
                                chars.CopyTo(nDataOffset, bDest, nStart, nCopied);
                            }
                            else {
                                nCopied = 0;
                            }

                            result = nCopied;
                        }
                    }
                }

                return result;
            }
        }

        internal override DateTime GetDateTime(SqliteStatement stmt, int index) {
            lock (dbLock) {
                DateTime result = default(DateTime);

                if (stmt != null && stmt._sqlite_stmt != null) {
                    if (!DateTime.TryParse(stmt._sqlite_stmt.GetText(index), out result)) result = default(DateTime);
                }

                return result;
            }
        }

        internal override bool IsNull(SqliteStatement stmt, int index) {
            lock (dbLock) {
                bool result = true;

                if (stmt != null && stmt._sqlite_stmt != null) {
                    result = (stmt._sqlite_stmt.DataType(index) == SQLitePCL.SQLiteType.NULL);
                }

                return result;
            }
        }

        internal override void CreateCollation(string strCollation, Wrapper.SqliteCollation func, Wrapper.SqliteCollation func16) {
            throw new NotImplementedException();
        }

        internal override void CreateFunction(string strFunction, int nArgs, bool needCollSeq, Wrapper.SqliteCallback func, Wrapper.SqliteCallback funcstep, Wrapper.SqliteFinalCallback funcfinal) {
            throw new NotImplementedException();
        }

        internal override CollationSequence GetCollationSequence(SqliteFunction func, Wrapper.SqliteContextHandle context) {
            throw new NotImplementedException();
        }

        internal override int ContextCollateCompare(CollationEncodingEnum enc, Wrapper.SqliteContextHandle context, string s1, string s2) {
            throw new NotImplementedException();
        }

        internal override int ContextCollateCompare(CollationEncodingEnum enc, Wrapper.SqliteContextHandle context, char[] c1, char[] c2) {
            throw new NotImplementedException();
        }

        internal override Wrapper.SqliteValueHandle AggregateContext(Wrapper.SqliteContextHandle context) {
            throw new NotImplementedException();
        }

        internal override long GetParamValueBytes(Wrapper.SqliteValueHandle ptr, int nDataOffset, byte[] bDest, int nStart, int nLength) {
            throw new NotImplementedException();
        }

        internal override double GetParamValueDouble(Wrapper.SqliteValueHandle ptr) {
            throw new NotImplementedException();
        }

        internal override int GetParamValueInt32(Wrapper.SqliteValueHandle ptr) {
            throw new NotImplementedException();
        }

        internal override long GetParamValueInt64(Wrapper.SqliteValueHandle ptr) {
            throw new NotImplementedException();
        }

        internal override string GetParamValueText(Wrapper.SqliteValueHandle ptr) {
            throw new NotImplementedException();
        }

        internal override TypeAffinity GetParamValueType(Wrapper.SqliteValueHandle ptr) {
            throw new NotImplementedException();
        }

        internal override void ReturnBlob(Wrapper.SqliteContextHandle context, byte[] value) {
            throw new NotImplementedException();
        }

        internal override void ReturnDouble(Wrapper.SqliteContextHandle context, double value) {
            throw new NotImplementedException();
        }

        internal override void ReturnError(Wrapper.SqliteContextHandle context, string value) {
            throw new NotImplementedException();
        }

        internal override void ReturnInt32(Wrapper.SqliteContextHandle context, int value) {
            throw new NotImplementedException();
        }

        internal override void ReturnInt64(Wrapper.SqliteContextHandle context, long value) {
            throw new NotImplementedException();
        }

        internal override void ReturnNull(Wrapper.SqliteContextHandle context) {
            throw new NotImplementedException();
        }

        internal override void ReturnText(Wrapper.SqliteContextHandle context, string value) {
            throw new NotImplementedException();
        }

        internal override void SetPassword(string passwordBytes) {
            throw new NotImplementedException();
        }

        internal override void ChangePassword(string newPasswordBytes) {
            throw new NotImplementedException();
        }

        internal override void SetUpdateHook(Wrapper.SqliteUpdateHookDelegate func) {
            throw new NotImplementedException();
        }

        internal override void SetCommitHook(Wrapper.SqliteCommitHookDelegate func) {
            throw new NotImplementedException();
        }

        internal override void SetRollbackHook(Wrapper.SqliteRollbackHookDelegate func) {
            throw new NotImplementedException();
        }

        internal override int GetCursorForTable(SqliteStatement stmt, int database, int rootPage) {
            throw new NotImplementedException();
        }

        internal override long GetRowIdForCursor(SqliteStatement stmt, int cursor) {
            throw new NotImplementedException();
        }

        internal override long LastInsertRowIdNoThreadLock {
            get {
                Int64 result = -1;

                try {
                    if (_sqliteDbConnection != null)
                        result = _sqliteDbConnection.LastInsertRowId();
                }
                catch (Exception ex) {
                    _lastError = (ex.Message ?? "").Trim();
                    _lastErrorStatement = "Called SQLiteConnection.LastInsertRowId()";
                    result = -1;
                }

                return result;
            }
        }

        internal override long LastInsertRowId {
            get {
                lock (dbLock) {
                    return this.LastInsertRowIdNoThreadLock;
                }
            }
        }

        internal override object GetValue(SqliteStatement stmt, int index, SqliteType typ) {
            lock (dbLock) {
                object result = null;

                if (stmt != null && stmt._sqlite_stmt != null && stmt._sqlite_stmt.DataType(index) != SQLitePCL.SQLiteType.NULL) {
                //    result = Convert.ChangeType(stmt._sqlite_stmt.GetText(index),
                //        SqliteConvert.DbTypeToType(SqliteConvert.TypeNameToDbType(stmt._sqlite_stmt.DataType(index).ToString())));
                    switch (stmt._sqlite_stmt.DataType(index)) {
                        case SQLitePCL.SQLiteType.BLOB:
                            result = stmt._sqlite_stmt.GetBlob(index);
                            break;
                        case SQLitePCL.SQLiteType.FLOAT:
                            result = stmt._sqlite_stmt.GetFloat(index);
                            break;
                        case SQLitePCL.SQLiteType.INTEGER:
                            result = stmt._sqlite_stmt.GetInteger(index);
                            break;
                        default:
                            result = stmt._sqlite_stmt.GetText(index);
                            break;
                    }
                }

                return result;
            }
        }



        public override void Dispose() {
            _sqliteDbConnection = null;
            base.Dispose();
        }
    }
}
