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
using Portable.Data.Sqlite;

[assembly: Xamarin.Forms.Dependency(typeof(DatabasePath))]
public class DatabasePath : IDatabasePath {
    public DatabasePath() { }

    public string GetPath(string databaseName) {
#if __ANDROID__
    	//Android code:
        string libraryPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        return System.IO.Path.Combine(libraryPath, databaseName);
#endif

#if __IOS__
        //iOS code:
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
        string libraryPath = System.IO.Path.Combine(documentsPath, "..", "Library"); // Library folder
        return System.IO.Path.Combine(libraryPath, databaseName);
#endif

#if NETFX_CORE
        //Windows Universal (UWP) code
        string libraryPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        return System.IO.Path.Combine(libraryPath, databaseName);
#endif
    }
}
