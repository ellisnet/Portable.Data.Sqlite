using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace SampleApp.Droid {
    [Activity(Label = "SampleApp", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity {
        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

            //Should be set up before the Xamarin.Forms App is instantiated
            SampleApp.App.AppDatabasePath = new Portable.Data.Sqlite.DatabasePath();
            SampleApp.App.AppCryptEngine = new Portable.Data.Sqlite.AesCryptEngine();

            #endregion

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}

