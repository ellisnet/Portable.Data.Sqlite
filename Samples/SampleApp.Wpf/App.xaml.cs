using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

//ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite
using Portable.Data.Sqlite;

namespace SampleApp.Wpf {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

        string _appPassword = "myT3stPassword";
        public string AppPassword {
            get { return _appPassword; }
            set { _appPassword = value; }
        }

        IObjectCryptEngine _appCryptEngine = null;
        public IObjectCryptEngine AppCryptEngine {
            get { return _appCryptEngine; }
            set { _appCryptEngine = value; }
        }

        string _databaseName = "testdb.sqlite";
        public string DatabaseName {
            get { return _databaseName; }
        }

        string _databasePath = null;
        public string DatabasePath {
            get {
                if (_databasePath == null) {
                    //There is probably a better place to store the database file, but for now just 
                    //  storing it in application folder.
                    _databasePath = _databaseName;
                }
                return _databasePath;
            }
        }

        SQLitePCL.ISQLiteConnection _sqliteConnection;
        public SQLitePCL.ISQLiteConnection SqliteConnection {
            get {
                if (_sqliteConnection == null) {
                    _sqliteConnection = new SQLitePCL.SQLiteConnection(this.DatabasePath);
                }
                return _sqliteConnection;
            }
            set { _sqliteConnection = value; }
        }

        #endregion

    }
}
