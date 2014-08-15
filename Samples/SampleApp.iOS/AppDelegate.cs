using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

//ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite
using Portable.Data.Sqlite;

namespace SampleApp.iOS {
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate {
        // class-level declarations
        public override UIWindow Window {
            get;
            set;
        }

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
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
                    string libraryPath = System.IO.Path.Combine(documentsPath, "..", "Library"); // Library folder
                    _databasePath = System.IO.Path.Combine(libraryPath, _databaseName);
                }
                return _databasePath; 
            }
        }

        SQLitePCL.ISQLiteConnection _sqliteConnection;
        public SQLitePCL.ISQLiteConnection SqliteConnection {
            get {
                if (_sqliteConnection == null) {

                    //CRITICAL STEP: Per the documentation for SQLitePCL, you must perform this step
                    //  (i.e. in the following line) on iOS, before using SQLitePCL functions.
                    SQLitePCL.CurrentPlatform.Init();

                    _sqliteConnection = new SQLitePCL.SQLiteConnection(this.DatabasePath);
                }
                return _sqliteConnection; 
            }
            set { _sqliteConnection = value; }
        }

        #endregion

        // This method is invoked when the application is about to move from active to inactive state.
        // OpenGL applications should use this method to pause.
        public override void OnResignActivation(UIApplication application) {
        }
        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication application) {
        }
        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication application) {
        }
        // This method is called when the application is about to terminate. Save data, if needed.
        public override void WillTerminate(UIApplication application) {
        }
    }
}
