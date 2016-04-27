using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

using SampleApp.Views;

namespace SampleApp {
    public class App : Application {

        #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

        public static Portable.Data.Sqlite.IDatabasePath AppDatabasePath { get; set; }
        public static Portable.Data.Sqlite.IObjectCryptEngine AppCryptEngine { get; set; }

        private static readonly string _appDataKey = "Thi$T3stPa$$w0rd";

        #endregion

        public App() {

            #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

            AppCryptEngine.Initialize(_appDataKey);
            MainPage = new SqliteTestPage();

            #endregion

            //Was:

            // The root page of your application
            //MainPage = new ContentPage {
            //    Content = new StackLayout {
            //        VerticalOptions = LayoutOptions.Center,
            //        Children = {
            //            new Label {
            //                HorizontalTextAlignment = TextAlignment.Center,
            //                Text = "Welcome to Xamarin Forms!"
            //            }
            //        }
            //    }
            //};
        }

        protected override void OnStart() {
            // Handle when your app starts
        }

        protected override void OnSleep() {
            // Handle when your app sleeps
        }

        protected override void OnResume() {
            // Handle when your app resumes
        }
    }
}
