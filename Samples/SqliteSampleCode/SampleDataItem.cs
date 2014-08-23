using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        //NOTE THAT ALL COLUMNS IN THIS TABLE WILL BE ENCRYPTED EXCEPT ANY COLUMNS SPECIFICALLY
        //  MARKED [NotEncrypted]

        //The following three property/columns in the encrypted-to-be-created will be "searchable"
        //  - the data WILL BE encrypted, but you will be able to search these columns without
        //  retrieving, decrypting and instantiating the entire record/object.

        [Searchable]
        public string FirstName {
            get { return _firstName; }
            set { _firstName = value; }
        }

        [Searchable]
        public string LastName {
            get { return _lastName; }
            set { _lastName = value; }
        }

        [Searchable]
        public DateTime Birthdate {
            get { return _birthdate; }
            set { _birthdate = value; }
        }

        //The following property/column demonstrates all of the possible attributes
        //  other than "Searchable" (the "Searchable" attribute cannot be combined with other attributes):

        //  [NotEncrypted] - store the value of this property in a non-encrypted column for easy querying
        //  [ColumnName] - specifies the name of the table column, instead of using the default - which would
        //    be [StateAbbreviation]
        //  [NotNull] - the table column created for this property should be marked NOT NULL
        //  [ColumnDefaultValue] - use the specified value for the default value of the column.

        [NotEncrypted, ColumnName("State"), NotNull, ColumnDefaultValue("MN")]
        public string StateAbbreviation {
            get { return _stateAbbreviation; }
            set { _stateAbbreviation = value; }
        }

        // A non-encrypted column will be created for the following property in the database, for easy querying

        [NotEncrypted]
        public string ZipCode {
            get { return _zipCode; }
            set { _zipCode = value; }
        }

        //The following property will be encrypted and will not be able to be searched without 
        //  retrieving, decrypting and instantiating the entire record/object

        public List<Tuple<DateTime, string>> MajorEvents {
            get { return _majorEvents; }
            set { _majorEvents = value ?? new List<Tuple<DateTime, string>>(); }
        }

        //A little static method to check and see how many records are in the encrypted table,
        //  that I will use multiple times in the sample code.
        //  No need to include something like this in your implementations of EncryptedTableItem
        public static long GetNumRecords(SqliteAdoConnection dbConn, string tableName) {
            string sql = "SELECT COUNT(*) FROM " + tableName + ";";
            using (var cmd = new SqliteCommand(sql, dbConn)) {
                dbConn.SafeOpen();
                return (long)cmd.ExecuteScalar();
            }
        }

    }

}