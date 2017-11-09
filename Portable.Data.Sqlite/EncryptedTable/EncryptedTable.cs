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

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable InconsistentNaming
// ReSharper disable StaticMemberInGenericType
// ReSharper disable ArrangeThisQualifier

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Portable.Data.Sqlite {

    /// <summary>
    /// Extension methods for encrypted tables and encrypted table objects
    /// </summary>
    public static class EncryptedTableExtensions {
        
        /// <summary>
        /// Identifies whether the encrypted table object property is marked with the specified attribute
        /// </summary>
        /// <param name="property">The property to check</param>
        /// <param name="attributeType">The attribute to look for</param>
        /// <returns>If true, the property IS marked with the attribute</returns>
        public static bool HasAttribute(this PropertyInfo property, Type attributeType) {
            bool result = false;

            if (property != null && attributeType != null) {
                result = property.GetCustomAttributes(attributeType, true).Any();
            }

            return result;
        }

    }

    /// <summary>
    /// A SQLite table, featuring encrypted values, for the specified object type
    /// </summary>
    /// <typeparam name="T">The type of object to be stored in the table</typeparam>
    public class EncryptedTable<T> : IDisposable where T:IEncryptedTableItem {

        #region Private instance fields

        private IObjectCryptEngine _cryptEngine;
        private string _tableName;
        private SqliteConnection _dbConnection;
        private List<T> _tempItems = new List<T>();
        private bool _dbTableChecked = false;
        private TableIndex _fullTableIndex = null;
        private int _indexLifetimeSeconds = 600;
        private bool _writeChangesOnDispose = true;

        #endregion

        #region Private static fields

        //dbLock should be static, in case there are two EncryptedTable<myObject> instances
        //  declared and they are both writing to the same table.
        private static readonly Object dbLock = new Object();
        private static readonly Object setupLock = new Object();
        private static List<string> _searchablePropertyNames = null;
        private static List<string> _notEncryptedPropertyNames = null;
        private static Dictionary<string, TableColumn> _tableColumns = null;

        private static readonly string NO_CRYPT_ENGINE = "No cryptography provided for this encrypted table.";
        private static readonly string NO_DB_CONNECTION = "No database connection provided for this encrypted table.";

        #endregion

        #region Public instance properties

        /// <summary>
        /// The name of the SQLite table
        /// </summary>
        public string TableName {
            // ReSharper disable InconsistentlySynchronizedField
            get => _tableName;
            set {
                if (value == null) throw new Exception("Table name cannot be null.");
                if (value.Trim() == "") throw new Exception("Table name cannot be empty.");

                //Checking the validity of the table name
                string tempTableName = value.Trim().Replace(".", "_");
                string error = "";
                do {
                    if (!("abcdefghijklmnopqrstuvwxyz").Contains(tempTableName.Substring(0, 1).ToLower())) {
                        error += "table names must start with a letter.";
                        break;
                    }
                    foreach (char letter in tempTableName.ToLower()) {
                        if (("abcdefghijklmnopqrstuvwxyz0123456789_").IndexOf(letter) < 0) {
                            error += "table names may only contain letters, numbers and underscores.";
                            break;
                        }
                    }
                    if (error == "") _tableName = tempTableName;
                } while (false);
                if (error != "") throw new Exception("The specified table name is invalid - " + error);
            }
            // ReSharper restore InconsistentlySynchronizedField
        }

        /// <summary>
        /// The SQLite database connection to be used when interacting with the table
        /// </summary>
        public SqliteConnection DbConnection {
            // ReSharper disable InconsistentlySynchronizedField
            get => _dbConnection;
            set => _dbConnection = value;
            // ReSharper restore InconsistentlySynchronizedField
        }

        /// <summary>
        /// An in-memory cache of objects/table records that are currently being manipulated
        /// </summary>
        public List<T> TempItems {
            // ReSharper disable InconsistentlySynchronizedField
            get => _tempItems;
            set => _tempItems = (value ?? new List<T>());
            // ReSharper restore InconsistentlySynchronizedField
        }

        /// <summary>
        /// IMPORTANT: A COPY (CLONE) of in-memory index of the objects stored in the table, featuring values of object properties marked as [Searchable] and [NotEncrypted]
        /// </summary>
        public TableIndex FullTableIndex {
            get {
                lock (dbLock) {
                    _checkFullTableIndex(true);
                }
                //return _fullTableIndex;
                //returning a copy of _fullTableIndex:
                if (_fullTableIndex == null) {
                    return null;
                }
                else {
                    return _fullTableIndex.Clone();
                }
            }
        }

        /// <summary>
        /// The default time-to-live, in seconds, of the indexes created for this table object
        /// </summary>
        public int IndexLifetimeSeconds {
            get { return _indexLifetimeSeconds; }
            set { 
                _indexLifetimeSeconds = (value < 0) ? 0 : value;
                if (_fullTableIndex != null) _fullTableIndex.LifetimeSeconds = _indexLifetimeSeconds;
            }
        }

        /// <summary>
        /// When object is being disposed, write pending changes to table?
        /// </summary>
        public bool WriteChangesOnDispose {
            get { return _writeChangesOnDispose; }
            set { _writeChangesOnDispose = value; }
        }

        #endregion 

        #region Ctor

        /// <summary>
        /// Creates an instance of the encrypted table object based on the specified values
        /// </summary>
        /// <param name="cryptEngine">The cryptography 'engine' to be used when encrypting/decrypting values</param>
        /// <param name="dbConnection">The SQLite database connection to be used when interacting with the table</param>
        /// <param name="checkDbTable">Check to make sure the associated SQLite table exists, and create if necessary</param>
        /// <param name="tableName">Specify a desired name of the SQLite table, instead of using a name derived from the object type</param>
        public EncryptedTable(IObjectCryptEngine cryptEngine, SqliteConnection dbConnection = null, bool checkDbTable = true, string tableName = null) {
            if (cryptEngine == null) throw new ArgumentNullException(nameof(cryptEngine));
            _cryptEngine = cryptEngine;
            _dbConnection = dbConnection;
            tableName = (String.IsNullOrWhiteSpace(tableName) ? null : tableName.Trim());
            this.TableName = tableName ?? typeof(T).Name;
            _checkTableColumns();
            lock (setupLock) {
                if (_searchablePropertyNames == null || _notEncryptedPropertyNames == null) {
                    using (T temp = Activator.CreateInstance<T>()) {
                        if (_searchablePropertyNames == null)
                            _searchablePropertyNames = temp.GetSearchablePropertyNames();
                        if (_notEncryptedPropertyNames == null)
                            _notEncryptedPropertyNames = temp.GetNotEncryptedPropertyNames();
                    }
                }
            }
            if (_dbConnection != null && checkDbTable) {
                lock (dbLock) {
                    _checkDbTable();
                }
            }
        }

        /// <summary>
        /// Creates an instance of the encrypted table object based on the specified values, using the CryptEngine associated with the database connection
        /// </summary>
        /// <param name="dbConnection">The SQLite database connection to be used when interacting with the table</param>
        /// <param name="checkDbTable">Check to make sure the associated SQLite table exists, and create if necessary</param>
        /// <param name="tableName">Specify a desired name of the SQLite table, instead of using a name derived from the object type</param>
        public EncryptedTable(SqliteConnection dbConnection, bool checkDbTable = true, string tableName = null)
            : this(dbConnection?._cryptEngine, dbConnection, checkDbTable, tableName) {
        }

        #endregion

        /// <summary>
        /// A dictionary of SQLite table columns in the table associated with the encrypted table object
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public Dictionary<string, TableColumn> TableColumns => _tableColumns;

        private void _checkTableColumns() {
            lock (setupLock) {
                if (_tableColumns == null) {
                    _tableColumns = new Dictionary<string, TableColumn>();
                    _tableColumns.Add("Id", new TableColumn {
                        PropertyName = "Id",
                        ColumnName = "Id",
                        ColumnOrder = 0,
                        DbType = "INTEGER",
                        NetType = "System.Int64"
                    });
                    _tableColumns.Add("Encrypted_Searchable", new TableColumn {
                        ColumnName = "Encrypted_Searchable",
                        ColumnOrder = 200,
                        DbType = "ENCRYPTED",
                        NetType = "System.String"
                    });
                    _tableColumns.Add("Encrypted_Object", new TableColumn {
                        ColumnName = "Encrypted_Object",
                        ColumnOrder = 201,
                        DbType = "ENCRYPTED",
                        NetType = "System.String"
                    });

                    var props = typeof(T).GetProperties();
                    int columnIndex = 0;
                    foreach (PropertyInfo prop in props) {
                        if (prop.HasAttribute(typeof(SearchableAttribute))) {
                            if (prop.HasAttribute(typeof(NotEncryptedAttribute)))
                                throw new Exception("A property with the attribute 'Searchable' cannot have the attribute 'NotEncrypted'.");
                            if (prop.HasAttribute(typeof(NotNullAttribute)))
                                throw new Exception("A property with the attribute 'Searchable' cannot have the attribute 'NotNull'.");
                            if (prop.HasAttribute(typeof(ColumnDefaultValueAttribute)))
                                throw new Exception("A property with the attribute 'Searchable' cannot have the attribute 'DefaultValue'.");
                            if (prop.HasAttribute(typeof(ColumnNameAttribute)))
                                throw new Exception("A property with the attribute 'Searchable' cannot have the attribute 'ColumnName'.");
                        }
                        else if (prop.HasAttribute(typeof(NotEncryptedAttribute))) {
                            string columnName = prop.Name;
                            if (prop.HasAttribute(typeof(ColumnNameAttribute))) {
                                columnName = ((ColumnNameAttribute)prop.GetCustomAttributes(typeof(ColumnNameAttribute), true).Single()).Name;
                            }
                            string tempName = columnName;
                            Tuple<bool, string> checkResult = TableColumn.CheckColumnName(ref tempName);
                            if (!checkResult.Item1)
                                throw new Exception(String.Format("Problem with column name '{0}' - {1}", tempName, checkResult.Item2 ?? "Unknown"));
                            columnName = tempName;

                            if ((new [] { "id", "encrypted_object", "encrypted_searchable" }).Contains(columnName.ToLower()))
                                throw new Exception(String.Format("'{0}' is a reserved column name and cannot be re-used.", columnName));

                            string netType = prop.PropertyType.FullName;
                            SqliteColumnType colType = SqliteConversion.TypeToColumnType(Type.GetType(netType));
                            // ReSharper disable once RedundantAssignment
                            string dbType = "";
                            string defaultValue = null;
                            if (prop.HasAttribute(typeof(ColumnDefaultValueAttribute))) {
                                defaultValue = ((ColumnDefaultValueAttribute)prop.GetCustomAttributes(typeof(ColumnDefaultValueAttribute), true).Single()).Value;
                            }
                            switch (colType) {
                                case SqliteColumnType.Integer:
                                    dbType = "INTEGER";
                                    if (defaultValue != null && !Int64.TryParse(defaultValue, out _))
                                        throw new Exception(String.Format("The default value for column [{0}] ('{1}') cannot be converted to SQLite type {2}",
                                            columnName, defaultValue, dbType));
                                    break;
                                case SqliteColumnType.Double:
                                    dbType = "REAL";
                                    if (defaultValue != null && !Double.TryParse(defaultValue, out _))
                                        throw new Exception(String.Format("The default value for column [{0}] ('{1}') cannot be converted to SQLite type {2}",
                                            columnName, defaultValue, dbType));
                                    break;
                                case SqliteColumnType.Text:
                                    dbType = "TEXT";
                                    break;
                                case SqliteColumnType.ConvDateTime:
                                    dbType = "DATETIME";
                                    if (defaultValue != null && !DateTime.TryParse(defaultValue, out _))
                                        throw new Exception(String.Format("The default value for column [{0}] ('{1}') cannot be converted to SQLite type {2}",
                                            columnName, defaultValue, dbType));
                                    break;
                                default:
                                    throw new Exception(String.Format("The column named '{0}' cannot be unencrypted, because the data type '{1}' cannot be used for an unencrypted column.",
                                        columnName, netType));
                                //break;
                            }

                            columnIndex++;
                            _tableColumns.Add(columnName, new TableColumn {
                                ColumnName = columnName,
                                ColumnOrder = (columnIndex > 199) ? Convert.ToByte(199) : Convert.ToByte(columnIndex),
                                PropertyName = prop.Name,
                                NetType = netType,
                                DbType = dbType,
                                IsNotNull = prop.HasAttribute(typeof(NotNullAttribute)),
                                DefaultValue = defaultValue
                            });
                        }
                    }
                }
            }
        }

        private void _checkDbTable() {
            if (_dbConnection == null) throw new Exception("No database connection specified.");
            string sql = "";
            foreach (var column in _tableColumns.Values.OrderBy(c => c.ColumnOrder)) {
                sql += (sql == "") ? "" : ", ";
                if (column.ColumnName == "Id") {
                    sql += "Id INTEGER PRIMARY KEY AUTOINCREMENT";
                }
                else {
                    string defaultValue = "";
                    if (column.DefaultValue != null) {
                        if (column.DbType == "INTEGER" || column.DbType == "REAL") {
                            defaultValue = " DEFAULT " + column.DefaultValue;
                        }
                        else {
                            defaultValue = " DEFAULT '" + column.DefaultValue + "'";
                        }
                    }
                    sql += column.ColumnName + " " + column.DbType + (column.IsNotNull ? " NOT NULL" : "") +
                        defaultValue;
                }
            }
            sql = String.Format("CREATE TABLE IF NOT EXISTS [{0}]({1});", _tableName, sql);
            using (var cmd = new SqliteCommand(sql, _dbConnection)) {
                if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                cmd.ExecuteNonQuery();
            }

            List<string> dbColumns = GetTableColumnNames(_tableName);
            foreach (TableColumn column in _tableColumns.Values.OrderBy(c => c.ColumnOrder))
            {
                if (dbColumns.All(c => (c ?? "").ToLower() != column.ColumnName.ToLower()))
                {
                    //must add column
                    string defaultValue = "";
                    if (column.DefaultValue != null)
                    {
                        if (column.DbType == "INTEGER" || column.DbType == "REAL")
                        {
                            defaultValue = " DEFAULT " + column.DefaultValue;
                        }
                        else
                        {
                            defaultValue = " DEFAULT '" + column.DefaultValue + "'";
                        }
                    }
                    sql =
                        $"ALTER TABLE [{_tableName}] ADD COLUMN [{column.ColumnName}] {column.DbType}{(column.IsNotNull ? " NOT NULL" : "")}{defaultValue};";
                    using (var cmd = new SqliteCommand(sql, _dbConnection))
                    {
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            //OLD: This code does not properly check if columns are missing or not because
            // there are no columns on the SqliteDataReader (at least for a new table) -
            // even if the table was just created to have columns.
            //sql = String.Format("SELECT * FROM [{0}] LIMIT 1;", _tableName);
            //using (var cmd = new SqliteCommand(sql, _dbConnection)) {
            //    if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
            //    List<string> dbColumns;
            //    using (var dr = new SqliteDataReader(cmd)) {
            //        // ReSharper disable once UnusedVariable
            //        bool recordFound = dr.Read();
            //        dbColumns = dr.Columns;
            //    }
            //    foreach (TableColumn column in _tableColumns.Values.OrderBy(c => c.ColumnOrder)) {
            //        if (dbColumns.All(c => (c ?? "").ToLower() != column.ColumnName.ToLower())) {
            //            //must add column
            //            string defaultValue = "";
            //            if (column.DefaultValue != null) {
            //                if (column.DbType == "INTEGER" || column.DbType == "REAL") {
            //                    defaultValue = " DEFAULT " + column.DefaultValue;
            //                }
            //                else {
            //                    defaultValue = " DEFAULT '" + column.DefaultValue + "'";
            //                }
            //            }
            //            sql =
            //                $"ALTER TABLE [{_tableName}] ADD COLUMN [{column.ColumnName}] {column.DbType}{(column.IsNotNull ? " NOT NULL" : "")}{defaultValue};";
            //            cmd.CommandText = sql;
            //            if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //}

            _dbTableChecked = true;
        }

        private List<string> GetTableColumnNames(string tableName)
        {
            var result = new List<string>();

            if (_dbConnection == null) throw new Exception("No database connection specified.");
            tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
            if (tableName == "") { throw new ArgumentOutOfRangeException(nameof(tableName));}

            string sql = $"pragma table_info({tableName});";
            // ReSharper disable InconsistentlySynchronizedField
            using (var cmd = new SqliteCommand(sql, _dbConnection))
            {
                if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                using (var dr = new SqliteDataReader(cmd))
                {
                    while (dr.Read())
                    {
                        string columnName = dr.GetString("name");
                        if (!String.IsNullOrWhiteSpace(columnName)) { result.Add(columnName);}
                    }
                }
            }
            // ReSharper restore InconsistentlySynchronizedField

            return result;
        }

        private int _buildFullTableIndex() {
            int result = 0;

            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            if (!_dbTableChecked) _checkDbTable();

            var newIndex = new TableIndex { LifetimeSeconds = _indexLifetimeSeconds };
            //if (_fullTableIndex != null) newIndex.LifetimeSeconds = _fullTableIndex.LifetimeSeconds;

            string sql = "select [Id], [Encrypted_Searchable]";

            var notEncryptedColumnProps = new Dictionary<string, string>();

            foreach (TableColumn column in _tableColumns.Values.Where(c => c.PropertyName != null)) {
                if (_notEncryptedPropertyNames.Any(p => p.ToLower() == column.PropertyName.ToLower())) {
                    notEncryptedColumnProps.Add(column.ColumnName, column.PropertyName);
                    sql += ", [" + column.ColumnName + "]";
                }
            }

            sql += " from [" + _tableName + "];";

            Dictionary<string, string> currentItemIndex;
            long currentId;

            using (var cmd = new SqliteCommand(sql, _dbConnection)) {
                cmd._cryptEngine = _cryptEngine;
                if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                using (var dr = new SqliteDataReader(cmd)) {
                    while (dr.Read()) {
                        result++;
                        currentId = dr.GetInt64("Id");
                        try {
                            currentItemIndex = dr.GetDecrypted<Dictionary<string, string>>("Encrypted_Searchable") ?? new Dictionary<string, string>();
                        }
                        catch (Exception ex) {
                            string error = String.Format("Unable to decrypt the 'Encrypted_Searchable' column for record at Id: {0} - {1}", currentId, ex.Message);
                            throw new Exception(error);
                        }
                        foreach (var item in notEncryptedColumnProps) {
                            var val = dr.GetValue(item.Key);
                            currentItemIndex.Add(item.Value, ((val == null) ? null : val.ToString()));
                        }
                        newIndex.Index.Add(currentId, currentItemIndex);
                    }
                }
            }

            newIndex.Timestamp = DateTime.Now;
            _fullTableIndex = newIndex;

            return result;
        }

        private bool _checkFullTableIndex(bool rebuildIfExpired = false) {
            bool result;
            if (_fullTableIndex != null && _fullTableIndex.LifetimeSeconds != 0 &&
                _fullTableIndex.Timestamp.AddSeconds(_fullTableIndex.LifetimeSeconds) < DateTime.Now)
                _fullTableIndex = null;

            result = (_fullTableIndex != null);

            if ((!result) && rebuildIfExpired) {
                _buildFullTableIndex();
                result = true;
            }

            return result;
        }

        private Object _getPropertyValue(T item, string propertyName)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item));}
            if (propertyName == null) { throw new ArgumentNullException(nameof(propertyName));}
            if (propertyName.Trim() == "") { throw new ArgumentOutOfRangeException(nameof(propertyName));}

            return item.GetType().GetProperties()
                .Single(p => p.Name.ToLower() == propertyName.ToLower())
                .GetValue(item);
        }

        private long _writeNewItem(T item, bool skipUpdateIndex = false) {
            // ReSharper disable once RedundantAssignment
            Int64 result = -1;

            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            if (!_dbTableChecked) _checkDbTable();

            if (item is EncryptedTableItem)
            {
                var encryptedItem = (item as EncryptedTableItem);
                encryptedItem.CryptEngine = encryptedItem.CryptEngine ?? _cryptEngine;
            }
            using (var cmd = new SqliteCommand(_dbConnection)) {
                string columnSql = "";
                string valueSql = "";
                var paramValues = new Dictionary<string, Object>();
                columnSql += "[Encrypted_Searchable], [Encrypted_Object]";
                valueSql += "@Encrypted_Searchable, @Encrypted_Object";
                paramValues.Add("@Encrypted_Searchable", item.EncryptSearchable());
                paramValues.Add("@Encrypted_Object", item.Encrypt());

                foreach (string property in _notEncryptedPropertyNames) {
                    TableColumn column = _tableColumns.Values.Single(c => c.PropertyName == property);
                    columnSql += ", [" + column.ColumnName +"]";
                    valueSql += ", " + "@" + column.ColumnName;
                    paramValues.Add("@" + column.ColumnName, _getPropertyValue(item, property) ?? column.DefaultValue);
                }

                cmd.CommandText = String.Format("INSERT INTO [{0}] ({1}) VALUES ({2});",
                    _tableName, columnSql, valueSql);
                cmd.CommandType = CommandType.Text;

                foreach (var param in paramValues) {
                    cmd.Parameters.Add(new SqliteParameter(param.Key, param.Value));
                }

                if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                result = cmd.ExecuteReturnRowId();
                item.Id = result;
                item.SyncStatus = TableItemStatus.MatchesDb;
                item.SyncTimestamp = DateTime.Now;
            }

            if (_fullTableIndex != null && !skipUpdateIndex) {
                if (_fullTableIndex.Index.Keys.Contains(item.Id)) {
                    _fullTableIndex.Index[item.Id] = item.AllSearchableIndex;
                }
                else {
                    _fullTableIndex.Index.Add(item.Id, item.AllSearchableIndex);
                }
            }

            return result;
        }

        private bool _updateItem(T item, bool noCheck = false, bool allowWriteNew = false, bool writeMissingAsNew = false, bool skipUpdateIndex = false) {
            // ReSharper disable once RedundantAssignment
            bool result = false;

            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            if (!_dbTableChecked) _checkDbTable();

            if (item.Id < 0) {
                if (!allowWriteNew)
                    throw new Exception("The item to be updated in the table does not have a valid ID.");
                _writeNewItem(item);
                result = true;
            }
            else {
                bool recordExists = noCheck;
                // ReSharper disable once RedundantAssignment
                string sql = "";
                if (!recordExists) {
                    sql = String.Format("SELECT COUNT(*) FROM [{0}] WHERE [Id] = @ID;", _tableName);
                    using (var cmd = new SqliteCommand(sql, _dbConnection) { CommandType = CommandType.Text }) {
                        cmd.Parameters.Add(new SqliteParameter("@ID", item.Id));
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        recordExists = (int)cmd.ExecuteScalar() > 0;
                    }
                }
                if (!recordExists) {
                    if (!writeMissingAsNew)
                        throw new Exception(String.Format("The item to be updated in the table does not exist (ID: {0}).", item.Id));
                    _writeNewItem(item);
                    result = true;
                }
                else {
                    var paramValues = new Dictionary<string, Object>();
                    sql = "UPDATE [" + _tableName + "] SET [Encrypted_Searchable] = @Encrypted_Searchable, [Encrypted_Object] = @Encrypted_Object";
                    paramValues.Add("@Encrypted_Searchable", item.EncryptSearchable());
                    paramValues.Add("@Encrypted_Object", item.Encrypt());

                    TableColumn column;
                    foreach (string property in _notEncryptedPropertyNames) {
                        column = _tableColumns.Values.Where(c => c.PropertyName == property).Single();
                        sql += ", [" + column.ColumnName + "]" + " = @" + column.ColumnName;
                        paramValues.Add("@" + column.ColumnName, _getPropertyValue(item, property) ?? column.DefaultValue);
                    }
                    sql += " WHERE [Id] = @ID;";

                    using (var cmd = new SqliteCommand(sql, _dbConnection) { CommandType = CommandType.Text }) {
                        foreach (var param in paramValues) {
                            cmd.Parameters.Add(new SqliteParameter(param.Key, param.Value));
                        }
                        cmd.Parameters.Add(new SqliteParameter("@ID", item.Id));
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        cmd.ExecuteNonQuery();
                        item.SyncStatus = TableItemStatus.MatchesDb;
                        item.SyncTimestamp = DateTime.Now;
                        result = true;
                    }

                    if (_fullTableIndex != null && !skipUpdateIndex) {
                        if (_fullTableIndex.Index.Keys.Contains(item.Id)) {
                            _fullTableIndex.Index[item.Id] = item.AllSearchableIndex;
                        }
                        else {
                            _fullTableIndex.Index.Add(item.Id, item.AllSearchableIndex);
                        }
                    }
                }
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return result;
        }

        private bool _deleteItem(T item, bool noCheck = true, bool skipUpdateIndex = false) {
            // ReSharper disable once RedundantAssignment
            bool result = false;

            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            if (!_dbTableChecked) _checkDbTable();

            if (item.Id < 0) {
                throw new Exception("The item to be deleted in the table does not have a valid ID.");
            }
            else {
                bool recordExists = noCheck;
                // ReSharper disable once RedundantAssignment
                string sql = "";
                if (!recordExists) {
                    sql = String.Format("SELECT COUNT(*) FROM [{0}] WHERE [Id] = @ID;", _tableName);
                    using (var cmd = new SqliteCommand(sql, _dbConnection) { CommandType = CommandType.Text }) {
                        cmd.Parameters.Add(new SqliteParameter("@ID", item.Id));
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        recordExists = (int)cmd.ExecuteScalar() > 0;
                    }
                }
                if (!recordExists) {
                    throw new Exception(String.Format("The item to be deleted in the table does not exist (ID: {0}).", item.Id));
                }
                else {
                    sql = String.Format("DELETE FROM [{0}] WHERE [Id] = @ID;", _tableName);
                    using (var cmd = new SqliteCommand(sql, _dbConnection) { CommandType = CommandType.Text }) {
                        cmd.Parameters.Add(new SqliteParameter("@ID", item.Id));
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        cmd.ExecuteNonQuery();
                        item.SyncStatus = TableItemStatus.DeletedFromDb;
                        item.SyncTimestamp = DateTime.Now;
                        result = true;
                    }

                    if (_fullTableIndex != null && _fullTableIndex.Index.Keys.Contains(item.Id) && !skipUpdateIndex) {
                        _fullTableIndex.Index.Remove(item.Id);
                    }
                }
            }
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return result;
        }

        private void _checkTableSearch(TableSearch search) {
            if (search == null) throw new NullReferenceException("The specified search is null.");
            if (search.MatchItems.Count > 0) {
                foreach (TableSearchItem item in search.MatchItems) {
                    SearchItemPropertyType propertyType = SearchItemPropertyType.Unknown;
                    string columnName = "";

                    if (_notEncryptedPropertyNames.Any(p => p.ToLower() == item.PropertyName.ToLower())) {
                        item.FixedPropertyName = _notEncryptedPropertyNames.First(p => p.ToLower() == item.PropertyName.ToLower());
                        propertyType = SearchItemPropertyType.NotEncrypted;
                        // ReSharper disable once InconsistentlySynchronizedField
                        columnName = _tableColumns.Values.First(c => c.PropertyName != null && c.PropertyName == item.FixedPropertyName).ColumnName;
                    }
                    else if (_searchablePropertyNames.Any(p => p.ToLower() == item.PropertyName.ToLower())) {
                        item.FixedPropertyName = _searchablePropertyNames.First(p => p.ToLower() == item.PropertyName.ToLower());
                        propertyType = SearchItemPropertyType.Searchable;
                    }

                    if (propertyType == SearchItemPropertyType.Unknown) {
                        throw new Exception(String.Format("The specified property '{0}' is not a [NotEncrypted] or [Searchable] property.", item.PropertyName));
                    }
                    else {
                        item.PropertyType = propertyType;
                        item.ColumnName = columnName;
                    }
                }
            }
        }

        private string _getIndexSearchSql(TableSearch search) {
            string result = "SELECT [Id]";
            string andOr = "";

            if (search == null) throw new NullReferenceException("The specified search is null.");

            foreach (TableColumn column in _tableColumns.Values.Where(c => !(new [] { "id", "encrypted_object", "encrypted_searchable" }).Contains(c.ColumnName.ToLower()))) {
                result += ", [" + column.ColumnName + "]";
            }

            result += ", [Encrypted_Searchable] FROM [" + _tableName + "]";

            if (search.MatchItems.Any(m => m.PropertyType == SearchItemPropertyType.NotEncrypted)) {
                result += " WHERE";
                foreach (TableSearchItem searchItem in search.MatchItems.Where(m => m.PropertyType == SearchItemPropertyType.NotEncrypted)) {
                    result += andOr;
                    if (andOr == "" && search.SearchType == TableSearchType.MatchAll) andOr = " AND";
                    if (andOr == "" && search.SearchType == TableSearchType.MatchAny) andOr = " OR";

                    switch (searchItem.MatchType) {
                        case SearchItemMatchType.IsEqualTo:
                            if (searchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                result += " (LTRIM(RTRIM(CAST([" + searchItem.ColumnName + "] AS TEXT))) = '" + searchItem.Value.Trim().Replace("'", "''") + "')";
                            }
                            else if (searchItem.Trimming == SearchItemTrimming.None) {
                                result += " (CAST([" + searchItem.ColumnName + "] AS TEXT) = '" + searchItem.Value.Replace("'", "''") + "')";
                            }
                            else { throw new NotImplementedException(); }
                            break;
                        case SearchItemMatchType.IsNotEqualTo:
                            if (searchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                result += " (LTRIM(RTRIM(CAST([" + searchItem.ColumnName + "] AS TEXT))) <> '" + searchItem.Value.Trim().Replace("'", "''") + "')";
                            }
                            else if (searchItem.Trimming == SearchItemTrimming.None) {
                                result += " (CAST([" + searchItem.ColumnName + "] AS TEXT) <> '" + searchItem.Value.Replace("'", "''") + "')";
                            }
                            else { throw new NotImplementedException(); }
                            break;
                        case SearchItemMatchType.Contains:
                            if (searchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                result += " (LTRIM(RTRIM(CAST([" + searchItem.ColumnName + "] AS TEXT))) LIKE '%" + searchItem.Value.Trim().Replace("'", "''") + "%')";
                            }
                            else if (searchItem.Trimming == SearchItemTrimming.None) {
                                result += " (CAST([" + searchItem.ColumnName + "] AS TEXT) LIKE '%" + searchItem.Value.Replace("'", "''") + "%')";
                            }
                            else { throw new NotImplementedException(); }
                            break;
                        case SearchItemMatchType.DoesNotContain:
                            if (searchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                result += " (NOT LTRIM(RTRIM(CAST([" + searchItem.ColumnName + "] AS TEXT))) LIKE '%" + searchItem.Value.Trim().Replace("'", "''") + "%')";
                            }
                            else if (searchItem.Trimming == SearchItemTrimming.None) {
                                result += " (NOT CAST([" + searchItem.ColumnName + "] AS TEXT) LIKE '%" + searchItem.Value.Replace("'", "''") + "%')";
                            }
                            else { throw new NotImplementedException(); }
                            break;
                        case SearchItemMatchType.IsNull:
                            result += " ([" + searchItem.ColumnName + "] IS NULL)";
                            break;
                        case SearchItemMatchType.IsNotNull:
                            result += " (NOT [" + searchItem.ColumnName + "] IS NULL)";
                            break;
                        default:
                            throw new NotImplementedException();
                            //break;
                    }
                }
            }

            return result + ";";
        }

        private bool _indexItemMatch(KeyValuePair<long, Dictionary<string, string>> indexItem, TableSearch search) {
            bool result;

            //if (indexItem == null) throw new ArgumentNullException("indexItem");
            if (search == null) throw new ArgumentNullException(nameof(search));
            if (indexItem.Value == null) throw new ArgumentException("Missing indexItem Value.", nameof(indexItem));
            if (search.MatchItems == null) throw new ArgumentException("Missing search MatchItems", nameof(search));

            if (search.MatchItems.Count == 0) {
                result = true;
            }
            else {
                var itemValues = indexItem.Value;
                bool itemMatch;
                string matchValue;
                string TmatchValue;
                string LmatchValue;
                string TLmatchValue;

                switch (search.SearchType) {
                    case TableSearchType.MatchAll:
                        itemMatch = true;
                        break;
                    case TableSearchType.MatchAny:
                        itemMatch = false;
                        break;
                    default:
                        throw new NotImplementedException();
                    //break;
                }

                foreach (var matchItem in search.MatchItems) {
                    bool foundMatch;
                    matchValue = matchItem.Value;
                    TmatchValue = matchItem.Value.Trim();
                    LmatchValue = matchItem.Value.ToLower();
                    TLmatchValue = matchItem.Value.Trim().ToLower();

                    switch (matchItem.MatchType) {
                        case SearchItemMatchType.IsEqualTo:
                            #region Check IsEqualTo
                            if (matchItem.CaseSensitivity == SearchItemCaseSensitivity.CaseInsensitive) {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Trim().ToLower() == TLmatchValue));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.ToLower() == LmatchValue));
                                }
                            }
                            else {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Trim() == TmatchValue));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value == matchValue));
                                }
                            }
                            #endregion
                            break;
                        case SearchItemMatchType.IsNotEqualTo:
                            #region Check IsNotEqualTo
                            if (matchItem.CaseSensitivity == SearchItemCaseSensitivity.CaseInsensitive) {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Trim().ToLower() != TLmatchValue));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.ToLower() != LmatchValue));
                                }
                            }
                            else {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Trim() != TmatchValue));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value != matchValue));
                                }
                            }
                            #endregion
                            break;
                        case SearchItemMatchType.Contains:
                            #region Check Contains
                            if (matchItem.CaseSensitivity == SearchItemCaseSensitivity.CaseInsensitive) {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.ToLower().Contains(TLmatchValue)));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.ToLower().Contains(LmatchValue)));
                                }
                            }
                            else {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Contains(TmatchValue)));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && v.Value.Contains(matchValue)));
                                }
                            }
                            #endregion
                            break;
                        case SearchItemMatchType.DoesNotContain:
                            #region Check DoesNotContain
                            if (matchItem.CaseSensitivity == SearchItemCaseSensitivity.CaseInsensitive) {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && (!v.Value.ToLower().Contains(TLmatchValue))));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && (!v.Value.ToLower().Contains(LmatchValue))));
                                }
                            }
                            else {
                                if (matchItem.Trimming == SearchItemTrimming.AutoTrim) {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && (!v.Value.Contains(TmatchValue))));
                                }
                                else {
                                    foundMatch =
                                        (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null && (!v.Value.Contains(matchValue))));
                                }
                            }
                            #endregion
                            break;
                        case SearchItemMatchType.IsNull:
                            foundMatch =
                                (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value == null));
                            break;
                        case SearchItemMatchType.IsNotNull:
                            foundMatch =
                                (itemValues.Any(v => v.Key == matchItem.FixedPropertyName && v.Value != null));
                            break;
                        default:
                            throw new NotImplementedException();
                        //break;
                    }

                    if (search.SearchType == TableSearchType.MatchAll) {
                        itemMatch &= foundMatch;
                        if (!itemMatch) break;
                    }
                    else {
                        itemMatch |= foundMatch;
                        if (itemMatch) break;
                    }
                }
                result = itemMatch;
            }

            return result;
        }

        /// <summary>
        /// Check to make sure the associated SQLite table exists, and create if necessary
        /// </summary>
        /// <param name="dbConnection">The SQLite database connection to be used when interacting with the table</param>
        public void CheckDbTable(SqliteConnection dbConnection = null) {
            // ReSharper disable InconsistentlySynchronizedField
            _dbConnection = dbConnection ?? _dbConnection;
            // ReSharper restore InconsistentlySynchronizedField
            lock (dbLock) {
                _checkDbTable();
            }
        }

        /// <summary>
        /// Builds an in-memory index of the objects stored in the SQLite table for searching
        /// </summary>
        /// <returns>The number of items in the index</returns>
        public int BuildFullTableIndex() {
            lock (dbLock) {
                return _buildFullTableIndex();
            }
        }

        /// <summary>
        /// Drops the current in-memory index of the objects stored in the SQLite table
        /// </summary>
        public void DropFullTableIndex() {
            _fullTableIndex = null;
        }

        /// <summary>
        /// Checks to see if the in-memory index of the object table has expired, and drops if appropriate
        /// </summary>
        /// <param name="rebuildIfExpired">Rebuild the index, if it has expired</param>
        /// <returns>If true, a current index exists</returns>
        public bool CheckFullTableIndex(bool rebuildIfExpired = false) {
            lock (dbLock) {
                return _checkFullTableIndex(rebuildIfExpired);
            }
        }

        /// <summary>
        /// Writes a new item/object/record to the encrypted object table
        /// </summary>
        /// <param name="item">The object to be written as a record</param>
        /// <returns>The Id of the newly written object</returns>
        public long WriteNewTableItem(T item) {
            lock (dbLock) {
                return _writeNewItem(item);
            }
        }

        /// <summary>
        /// Writes a new item/object/record to the encrypted table from the TempItems collection
        /// </summary>
        /// <param name="itemId">The Id of the item in the TempItems collection</param>
        /// <returns>The Id of the newly written object</returns>
        public long WriteNewTableItem(long itemId) {
            lock (dbLock) {
                long result = -1;
                if (this.TempItems.Where(i => i.Id == itemId).Count() == 0) {
                    throw new Exception("Unable to find item with ID: " + itemId.ToString());
                }
                else {
                    foreach (T item in this.TempItems.Where(i => i.Id == itemId)) {
                        result = _writeNewItem(item);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Updates an existing record in the encrypted table with values from the specified object
        /// </summary>
        /// <param name="item">The item with values to be written</param>
        /// <param name="noCheckForExisting">If true, skips a check to make sure the record to be updated exists in the table</param>
        /// <param name="allowWriteNew">If the object to be written is marked 'New', creates a record (instead of updating anything)</param>
        /// <param name="writeMissingAsNew">Write any object where the record does not currently exist as a new record</param>
        /// <returns>If true, the object was written to the record</returns>
        public bool UpdateTableItem(T item, bool noCheckForExisting = false, bool allowWriteNew = false, bool writeMissingAsNew = false) {
            lock (dbLock) {
                return _updateItem(item, noCheckForExisting, allowWriteNew, writeMissingAsNew);
            }
        }

        /// <summary>
        /// Updates an existing record in the encrypted table with values from a object in the TempItems collection
        /// </summary>
        /// <param name="itemId">The Id of the item in the TempItems collection</param>
        /// <param name="noCheckForExisting">If true, skips a check to make sure the record to be updated exists in the table</param>
        /// <param name="allowWriteNew">If the object to be written is marked 'New', creates a record (instead of updating anything)</param>
        /// <param name="writeMissingAsNew">Write any object where the record does not currently exist as a new record</param>
        /// <returns>If true, the object was written to the record</returns>
        public bool UpdateTableItem(long itemId, bool noCheckForExisting = false, bool allowWriteNew = false, bool writeMissingAsNew = false) {
            lock (dbLock) {
                bool result = true;
                if (this.TempItems.All(i => i.Id != itemId)) {
                    // ReSharper disable once RedundantAssignment
                    result = false;
                    throw new Exception("Unable to find item with ID: " + itemId.ToString());
                }
                else {
                    foreach (T item in this.TempItems.Where(i => i.Id == itemId)) {
                        result = result & _updateItem(item, noCheckForExisting, allowWriteNew, writeMissingAsNew);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Deletes a record from the encrypted table
        /// </summary>
        /// <param name="item">The item whose record is to be deleted</param>
        /// <param name="noCheckForExisting">Skip a check to make sure the record exists before deleting it</param>
        /// <returns>If true, the item was deleted</returns>
        public bool DeleteTableItem(T item, bool noCheckForExisting = true) {
            lock (dbLock) {
                return _deleteItem(item, noCheckForExisting);
            }
        }

        /// <summary>
        /// Deletes a record associated with an object in the TempItems collection from the encrypted table
        /// </summary>
        /// <param name="itemId">The Id of the item in the TempItems collection</param>
        /// <param name="noCheckForExisting">Skip a check to make sure the record exists before deleting it</param>
        /// <returns>If true, the item was deleted</returns>
        public bool DeleteTableItem(long itemId, bool noCheckForExisting = true) {
            lock (dbLock) {
                bool result = true;
                if (this.TempItems.All(i => i.Id != itemId)) {
                    // ReSharper disable once RedundantAssignment
                    result = false;
                    throw new Exception("Unable to find item with ID: " + itemId.ToString());
                }
                else {
                    foreach (T item in this.TempItems.Where(i => i.Id == itemId)) {
                        result = result & _deleteItem(item, noCheckForExisting);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Updates the encrypted table records based on the contents of the TempItems collection
        /// </summary>
        /// <param name="forceWriteAll">Write all objects in the TempItems collection to the table, even if they are marked as matching the table</param>
        /// <param name="rebuildFullIndex">After writing changes, perform a rebuild of the in-memory table index</param>
        /// <param name="dbConnection">The SQLite database connection to be used when interacting with the table</param>
        /// <returns>The number of table records modified</returns>
        public int WriteItemChanges(bool forceWriteAll = false, bool rebuildFullIndex = false, SqliteConnection dbConnection = null) {
            int result = 0;

            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            // ReSharper disable InconsistentlySynchronizedField
            _dbConnection = dbConnection ?? _dbConnection;
            // ReSharper restore InconsistentlySynchronizedField
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            lock (dbLock) {
                if (!_dbTableChecked) _checkDbTable();
                // ReSharper disable once RedundantArgumentDefaultValue
                _checkFullTableIndex(false);

                IEnumerable<T> itemsToWrite;
                IEnumerable<T> itemsToUpdate;
                IEnumerable<T> itemsToDelete;

                if (forceWriteAll) {
                    itemsToWrite = new List<T>();
                    itemsToUpdate = _tempItems.Where(i => i.SyncStatus == TableItemStatus.New ||
                        i.SyncStatus == TableItemStatus.Modified || i.SyncStatus == TableItemStatus.MatchesDb);
                    itemsToDelete = _tempItems.Where(i => i.SyncStatus == TableItemStatus.ToBeDeleted ||
                        i.SyncStatus == TableItemStatus.DeletedFromDb);
                }
                else {
                    itemsToWrite = _tempItems.Where(i => i.SyncStatus == TableItemStatus.New);
                    itemsToUpdate = _tempItems.Where(i => i.SyncStatus == TableItemStatus.Modified);
                    itemsToDelete = _tempItems.Where(i => i.SyncStatus == TableItemStatus.ToBeDeleted);
                }

                foreach (T item in itemsToDelete) {
                    result++;
                    _deleteItem(item, true, rebuildFullIndex);
                }

                foreach (T item in itemsToUpdate) {
                    result++;
                    _updateItem(item, forceWriteAll, forceWriteAll, forceWriteAll, rebuildFullIndex);
                }

                foreach (T item in itemsToWrite) {
                    result++;
                    _writeNewItem(item, rebuildFullIndex);
                }

                foreach (T item in _tempItems.Where(i => i.SyncStatus == TableItemStatus.DeletedFromDb).ToList()) {
                    _tempItems.Remove(item);
                }

                if (_fullTableIndex != null) _fullTableIndex.Timestamp = DateTime.Now;
                if (rebuildFullIndex) _buildFullTableIndex();
            }
            
            return result;
        }

        /// <summary>
        /// Updates the encrypted table records based on the contents of the TempItems collection, then empties TempItems and disposes FullTextIndex
        /// </summary>
        /// <param name="forceWriteAll">Write all objects in the TempItems collection to the table, even if they are marked as matching the table</param>
        /// <param name="dbConnection">The SQLite database connection to be used when interacting with the table</param>
        public void WriteChangesAndFlush(bool forceWriteAll = false, SqliteConnection dbConnection = null) {
            this.WriteItemChanges(forceWriteAll, false, dbConnection);
            _fullTableIndex = null;
            this.TempItems = null;
        }

        /// <summary>
        /// Adds an item for the encrypted table to the TempItems collection
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <param name="immediateWriteToTable">If true, the item is immediately written to the table (vs. waiting for the next WriteItemChanges)</param>
        /// <returns>The Id of the item (-1 for new, unwritten items)</returns>
        public long AddItem(T item, bool immediateWriteToTable = false) {
            Int64 result = -1;

            if (item == null) throw new ArgumentNullException(nameof(item));

            item.SyncStatus = TableItemStatus.New;

            if (immediateWriteToTable) {
                lock (dbLock) {
                    _writeNewItem(item);
                }
            }

            this.TempItems.Add(item);

            return result;
        }

        /// <summary>
        /// Writes a single item (or all new items if -1 is specified for itemId) from the TempItems collection to the encrypted table
        /// </summary>
        /// <param name="itemId">The Id of the item to be written</param>
        /// <param name="allowWriteNew">If true and -1 is specified for itemId, new items will also be written</param>
        /// <returns>If true, the item(s) was successfully written</returns>
        public bool WriteItemToTable(long itemId, bool allowWriteNew = true) {
            lock (dbLock) {
                bool result;

                if (allowWriteNew && itemId == -1) {
                    foreach (T item in this.TempItems.Where(i => i.Id == itemId)) {
                        _writeNewItem(item);
                    }
                    result = true;
                }
                else {
                    int numItems = this.TempItems.Where(i => i.Id == itemId).Count();
                    if (numItems == 0) {
                        throw new Exception("Unable to find item with ID: " + itemId.ToString());
                    }
                    else if (numItems > 1) {
                        throw new Exception("Multiple items were found with ID: " + itemId.ToString());
                    }
                    else {
                        T item = this.TempItems.Where(i => i.Id == itemId).Single();
                        switch (item.SyncStatus) {
                            case TableItemStatus.ToBeDeleted:
                                result = _deleteItem(item);
                                break;
                            case TableItemStatus.DeletedFromDb:
                                throw new Exception("The following item has been deleted from the table, and cannot be written - ID: " + itemId.ToString());
                            //break;
                            default:
                                result = _updateItem(item, true, true, true);
                                break;
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Marks an item in the TempItems collection as "to be deleted" from the encrypted table
        /// </summary>
        /// <param name="itemId">The Id of the item to be deleted</param>
        /// <param name="immediateWriteToTable">If true, the associated record is immediately deleted from the table</param>
        /// <returns>If true, the item was successfully marked or deleted</returns>
        public bool RemoveItem(long itemId, bool immediateWriteToTable = false) {
            bool result;

            int numItems = this.TempItems.Count(i => i.Id == itemId);
            if (numItems == 0) {
                throw new Exception("Unable to find item with ID: " + itemId.ToString());
            }
            else if (numItems > 1) {
                throw new Exception("Multiple items were found with ID: " + itemId.ToString());
            }
            else {
                T item = this.TempItems.Single(i => i.Id == itemId);
                item.SyncStatus = TableItemStatus.ToBeDeleted;

                result = true;

                if (immediateWriteToTable) {
                    lock (dbLock) {
                        result = _deleteItem(item);
                    }
                    if (result) this.TempItems.Remove(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Get a TableIndex object with a list of records in the encrypted table that match the specified search criteria
        /// </summary>
        /// <param name="search">An object containing the search criteria to be used to identify matching records</param>
        /// <param name="writeChangesFirst">Write changes in the TempItems collection to the table, before performing the record search</param>
        /// <param name="bypassFullTableIndex">If true, do not use the full table index (if it exists) i.e. check the database directly</param>
        /// <returns>An index of the matching records</returns>
        public TableIndex GetTableItemIndex(TableSearch search, bool writeChangesFirst = true, bool bypassFullTableIndex = false) {
            // ReSharper disable once RedundantAssignment
            TableIndex result = null;

            _checkTableSearch(search);

            if (writeChangesFirst) this.WriteItemChanges();

            TableIndex initialIndex;

            #region Getting the initial index to be searched

            if (search.MatchItems.All(m => m.PropertyType != SearchItemPropertyType.NotEncrypted)) {
                //initial index will be full table index
                if (bypassFullTableIndex || (!this.CheckFullTableIndex())) this.BuildFullTableIndex();
                initialIndex = _fullTableIndex.Clone();
            }
            else {
                //getting filtered initial index from table
                Dictionary<string, string> currentItemIndex;
                long currentId;

                initialIndex = new TableIndex { LifetimeSeconds = _indexLifetimeSeconds };

                var notEncryptedColumnProps = new Dictionary<string, string>();

                // ReSharper disable once InconsistentlySynchronizedField
                foreach (TableColumn column in _tableColumns.Values.Where(c => c.PropertyName != null)) {
                    if (_notEncryptedPropertyNames.Any(p => p.ToLower() == column.PropertyName.ToLower())) {
                        notEncryptedColumnProps.Add(column.ColumnName, column.PropertyName);
                    }
                }

                lock (dbLock) {
                    if (!_dbTableChecked) _checkDbTable();

                    using (var cmd = new SqliteCommand(_getIndexSearchSql(search), _dbConnection)) {
                        cmd._cryptEngine = _cryptEngine;
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        using (var dr = new SqliteDataReader(cmd)) {
                            while (dr.Read()) {
                                currentId = dr.GetInt64("Id");
                                try {
                                    currentItemIndex = dr.GetDecrypted<Dictionary<string, string>>("Encrypted_Searchable") ?? new Dictionary<string, string>();
                                }
                                catch (Exception ex) {
                                    string error = String.Format("Unable to decrypt the 'Encrypted_Searchable' column for record at Id: {0} - {1}", currentId, ex.Message);
                                    throw new Exception(error);
                                }
                                foreach (var item in notEncryptedColumnProps) {
                                    var val = dr.GetValue(item.Key);
                                    currentItemIndex.Add(item.Value, ((val == null) ? null : val.ToString()));
                                }
                                initialIndex.Index.Add(currentId, currentItemIndex);
                            }
                        }
                    }                    
                }

                initialIndex.Timestamp = DateTime.Now;
            }

            #endregion

            if (search.MatchItems.Count == 0) {
                result = initialIndex;
            }
            else {
                result = new TableIndex { Timestamp = initialIndex.Timestamp, LifetimeSeconds = initialIndex.LifetimeSeconds };

                foreach (var item in initialIndex.Index.Where(i => i.Value.Count > 0)) {
                    if (_indexItemMatch(item, search)) {
                        result.Index.Add(item.Key, item.Value);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the encrypted table object with the specified Id, first checks the TempItems collection and then the table
        /// </summary>
        /// <param name="itemId">The id of the item to retrieve</param>
        /// <param name="exceptionOnMissingItem">If true, throws an exception if the item is not found; if false, returns null.</param>
        /// <returns>The requested object</returns>
        public T GetItem(long itemId, bool exceptionOnMissingItem = false) {
            T result = default(T);

            if (itemId < 0) throw new ArgumentOutOfRangeException(nameof(itemId));

            if (_tempItems.Where(i => i.Id == itemId).Count() == 1) {
                result = _tempItems.Where(i => i.Id == itemId).Single();
            }
            else if (_tempItems.Where(i => i.Id == itemId).Count() > 1) {
                throw new Exception("Multiple items were found with ID: " + itemId.ToString());
            }
            else {
                if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
                if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);
                bool recordFound = false;
                lock (dbLock) {
                    if (!_dbTableChecked) _checkDbTable();
                    string sql = String.Format("SELECT [Encrypted_Object] FROM [{0}] WHERE [Id] = @ID;", _tableName);
                    using (var cmd = new SqliteCommand(sql, _dbConnection)) {
                        cmd._cryptEngine = _cryptEngine;
                        cmd.Parameters.Add(new SqliteParameter("@ID", itemId));
                        if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();
                        using (var dr = new SqliteDataReader(cmd)) {
                            while (dr.Read()) {
                                if (recordFound) {
                                    throw new Exception("Multiple items were found with ID: " + itemId.ToString());
                                }
                                else {
                                    result = dr.GetDecrypted<T>("Encrypted_Object");
                                    result.Id = itemId;
                                    result.SyncTimestamp = DateTime.Now;
                                    result.SyncStatus = TableItemStatus.MatchesDb;
                                    recordFound = true;
                                }
                            }
                        }
                    }
                }
                if (recordFound) _tempItems.Add(result);
                if (exceptionOnMissingItem && !recordFound)
                    throw new Exception("No item could be found with ID: " + itemId.ToString());
            }

            return result;
        }

        /// <summary>
        /// Retrieves a collection of objects with properties matching the specified search - IMPORTANT: Automatically writes TempItems object changes to the table
        /// </summary>
        /// <param name="search">An object containing the search criteria to be used to identify matching records</param>
        /// <param name="exceptionOnMissingItem">If true, throws an exception if a record identified as matching is missing from the table</param>
        /// <returns>Collection of objects</returns>
        public List<T> GetItems(TableSearch search, bool exceptionOnMissingItem = true) {
            var result = new List<T>();

            _checkTableSearch(search);

            if (_cryptEngine == null) throw new Exception(NO_CRYPT_ENGINE);
            if (_dbConnection == null) throw new Exception(NO_DB_CONNECTION);

            this.WriteItemChanges();
            this.CheckFullTableIndex();

            var itemsToGet = new TableIndex { Timestamp = DateTime.Now, LifetimeSeconds = _indexLifetimeSeconds }; 

            if (_fullTableIndex != null) {
                foreach (var item in _fullTableIndex.Index) {
                    if (_indexItemMatch(item, search)) itemsToGet.Index.Add(item.Key, item.Value);
                }
            }
            else {
                // ReSharper disable once RedundantArgumentDefaultValue
                itemsToGet = this.GetTableItemIndex(search, false, false);
            }

            foreach (var item in itemsToGet.Index) {
                result.Add(this.GetItem(item.Key, true));
            }

            return result;
        }

        /// <summary>
        /// Dispose the EncryptedTable object and free up resources
        /// </summary>
        public void Dispose() {
            if (_writeChangesOnDispose && _tempItems != null) {
                if (_tempItems.Any(i => i.SyncStatus == TableItemStatus.New || i.SyncStatus == TableItemStatus.Modified ||
                    i.SyncStatus == TableItemStatus.ToBeDeleted)) 
                {
                    if (_dbConnection == null)
                        throw new Exception("This object is set to write changes when being disposed, and there are pending object changes - but no database connection.");
                    // ReSharper disable once RedundantArgumentDefaultValue
                    this.WriteChangesAndFlush(false);
                }
            }
            _cryptEngine = null;
            _dbConnection = null;
            _tempItems = null;
            _fullTableIndex = null;
        }
    }
}
