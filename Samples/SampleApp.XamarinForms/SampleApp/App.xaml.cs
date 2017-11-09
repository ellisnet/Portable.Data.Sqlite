using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace SampleApp
{
    public partial class App : Application
    {
        #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

        public static Portable.Data.Sqlite.IDatabasePath AppDatabasePath { get; set; }
        public static Portable.Data.Sqlite.IObjectCryptEngine AppCryptEngine { get; set; }

        private static readonly string _appDataKey = "Thi$T3stPa$$w0rd";

        #endregion

        public App()
        {
            InitializeComponent();

            //Was: MainPage = new SampleApp.MainPage();
            
            #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

            AppCryptEngine.Initialize(_appDataKey);
            MainPage = new SampleApp.Views.SqliteTestPage();

            #endregion
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
