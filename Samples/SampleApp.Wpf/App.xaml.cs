using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Portable.Data.Sqlite;

namespace SampleApp.Wpf {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

        private string _appPassword = "myT3stPassword";

        private IObjectCryptEngine _appCryptEngine;
        public IObjectCryptEngine AppCryptEngine {
            get {
                _appCryptEngine = _appCryptEngine ?? new AesCryptEngine(_appPassword);
                return _appCryptEngine;
            }
        }

        private string _databaseName = "testdb.sqlite";
        public string DatabaseName {
            get { return _databaseName; }
        }

        private string _databasePath;
        public string DatabasePath {
            get {
                _databasePath = _databasePath ?? System.IO.Path.Combine(@"C:\temp", _databaseName);
                return _databasePath;
            }
        }

        #endregion

    }
}
