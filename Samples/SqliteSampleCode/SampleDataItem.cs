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

//ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite
//(applies to the entire content of this file)
using Portable.Data.Sqlite;

namespace SampleApp.Shared.SqliteSampleCode {

    //CRITICALLY IMPORTANT: Must inherit from EncryptedTableItem
    public class SampleDataItem : EncryptedTableItem {

        string _firstName;
        string _lastName;
        DateTime _birthdate;
        string _stateAbbreviation;
        string _zipCode;
        List<Tuple<DateTime, string>> _majorEvents = new List<Tuple<DateTime, string>>();

        //NOTE THAT ALL COLUMNS IN THE SQLITE TABLE THAT IS CREATED FOR THIS OBJECT TYPE 
        //  WILL BE ENCRYPTED EXCEPT ANY COLUMNS SPECIFICALLY MARKED [NotEncrypted]

        //The following three property/columns in the encrypted-to-be-created will be "searchable"
        //  - the data WILL BE encrypted, but you will be able to search these columns without
        //  retrieving, decrypting and instantiating the entire record/object.

        //Using SetChanged<T>() when setting property values allows the object to track
        //  whether it has changed or not, with respect to the database.
        //  When changes are written to the database, the object will have tracked whether
        //  it has been updated or not.

        //In examples below, we are using CheckChange<T>() when setting property values.
        //  The difference between SetChanged and CheckChange is that using SetChanged will
        //  always set the object to a status of 'Modified', while CheckChange will only do \
        //  this if the new value is different than the old value.

        [Searchable]
        public string FirstName {
            get { return _firstName; }
            set { _firstName = SetChanged(value); }
        }

        [Searchable]
        public string LastName {
            get { return _lastName; }
            set { _lastName = SetChanged(value); }
        }

        [Searchable]
        public DateTime Birthdate {
            get { return _birthdate; }
            set { _birthdate = SetChanged(value); }
        }

        //The following property/column demonstrates all of the possible attributes
        //  other than "Searchable" (the "Searchable" attribute cannot be combined with other attributes):

        //  [NotEncrypted] - store the value of this property in a non-encrypted column for easy querying
        //  [ColumnName] - specifies the name of the table column, instead of using the default - which would
        //    be 'StateAbbreviation' in this case
        //  [NotNull] - the SQLite table column created for this property should be marked NOT NULL
        //  [ColumnDefaultValue] - use the specified value for the default value of the column.

        [NotEncrypted, ColumnName("State"), NotNull, ColumnDefaultValue("MN")]
        public string StateAbbreviation {
            get { return _stateAbbreviation; }
            set { _stateAbbreviation = CheckChange(_stateAbbreviation, value); }
        }

        // A non-encrypted column will be created for the following property in the database, for easy querying

        [NotEncrypted]
        public string ZipCode {
            get { return _zipCode; }
            set { _zipCode = CheckChange(_zipCode, value); }
        }

        //The following property will be encrypted and will not be able to be searched without 
        //  retrieving, decrypting and instantiating the entire record/object

        public List<Tuple<DateTime, string>> MajorEvents {
            get { return _majorEvents; }
            set { _majorEvents = CheckChange(_majorEvents, value ?? new List<Tuple<DateTime, string>>()); }
        }

        //A little static method to check and see how many records are in the encrypted table,
        //  that I will use multiple times in the sample code.
        //  No need to include something like this in your implementations of EncryptedTableItem
        public static long GetNumRecords(SqliteConnection dbConn, string tableName) {
            string sql = "SELECT COUNT(*) FROM " + tableName + ";";
            using (var cmd = new SqliteCommand(sql, dbConn)) {
                dbConn.SafeOpen();
                return (long)cmd.ExecuteScalar();
            }
        }

    }

}