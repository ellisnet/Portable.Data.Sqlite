using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite
using Portable.Data.Sqlite;
using SampleApp.Shared.SqliteSampleCode;

namespace SampleApp.Wpf {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {

            #region ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite

            try {
                string myTableName = "TestTable1";
                string myEncryptedTableName;

                //Get a handle to App and instantiate my "crypt engine"
                var myApp = (App)Application.Current;
                myApp.AppCryptEngine = myApp.AppCryptEngine ?? new FakeCryptEngine(myApp.AppPassword);

                //Create a table, add a record, add a column, add encrypted data, read back data
                using (var dbConn = new SqliteAdoConnection(myApp.SqliteConnection, myApp.AppCryptEngine)) {

                    Console.WriteLine("PART 1 - Doing ADO stuff");

                    //Create the table if it doesn't exist
                    string sql = "CREATE TABLE IF NOT EXISTS " + myTableName + " (IdColumn INTEGER PRIMARY KEY AUTOINCREMENT, DateTimeColumn DATETIME, TextColumn TEXT);";
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        dbConn.SafeOpen();
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Table [" + myTableName + "] created (if it didn't exist).");
                    }

                    //Add a record
                    sql = "INSERT INTO " + myTableName + " (DateTimeColumn, TextColumn) VALUES (@date, @text);";
                    int newRowId;
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        cmd.Parameters.Add(new SqliteParameter("@date", DateTime.Now));
                        cmd.Parameters.Add(new SqliteParameter("@text", "Hello SQLite."));
                        dbConn.SafeOpen();
                        newRowId = Convert.ToInt32(cmd.ExecuteReturnRowId());  //Note: INTEGER columns in SQLite are always long/Int64 - including ID columns, so converting to int
                        Console.WriteLine("A record with ID " + newRowId.ToString() + " was created in table [" + myTableName + "].");
                    }

                    //Read the datetime column on the oldest record
                    sql = "SELECT [DateTimeColumn] FROM " + myTableName + " ORDER BY [DateTimeColumn] LIMIT 1;";
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        dbConn.SafeOpen();
                        DateTime oldest = Convert.ToDateTime(cmd.ExecuteScalar());
                        Console.WriteLine("The oldest record in table [" + myTableName + "] has timestamp: " + oldest.ToString());
                    }

                    //Add an encrypted column to the table
                    //NOTE: There is no benefit to creating the column as SQLite datatype ENCRYPTED vs. TEXT
                    //  It is actually a TEXT column - but I think it is nice to set it to type ENCRYPTED for future purposes.
                    //  Hopefully a future version of SQLitePCL will make it easy to figure out if a column is defined as ENCRYPTED or TEXT
                    //  (right now, it identifies both as TEXT)
                    sql = "ALTER TABLE " + myTableName + " ADD COLUMN EncryptedColumn ENCRYPTED;";
                    //Note: This column shouldn't exist, since I just created the table above.  But if this application has been run multiple times, the column
                    //  may already exist in the table - so I need to check for it.
                    bool columnAlreadyExists = false;
                    #region Check for column
                    using (var checkCmd = new SqliteCommand(dbConn)) {
                        checkCmd.CommandText = "PRAGMA table_info (" + myTableName + ");";
                        dbConn.SafeOpen();
                        using (var checkDr = new SqliteDataReader(checkCmd)) {
                            while (checkDr.Read()) {
                                if (checkDr.GetString("NAME") == "EncryptedColumn") {
                                    Console.WriteLine("The [EncryptedColumn] column already exists.");
                                    columnAlreadyExists = true;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion
                    if (!columnAlreadyExists) {
                        using (var cmd = new SqliteCommand(sql, dbConn)) {
                            dbConn.SafeOpen();
                            cmd.ExecuteNonQuery();
                            Console.WriteLine("The [EncryptedColumn] column was created in table [" + myTableName + "].");
                        }
                    }

                    //Add a record with an encrypted column value
                    sql = "INSERT INTO " + myTableName + " (DateTimeColumn, TextColumn, EncryptedColumn) VALUES (@date, @text, @encrypted);";
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        cmd.Parameters.Add(new SqliteParameter("@date", DateTime.Now));
                        cmd.Parameters.Add(new SqliteParameter("@text", "Hello data."));
                        cmd.AddEncryptedParameter(new SqliteParameter("@encrypted",
                            Tuple.Create<string, string, string>("Hello", "encrypted", "data")));
                        dbConn.SafeOpen();
                        newRowId = Convert.ToInt32(cmd.ExecuteReturnRowId());  //Note: INTEGER columns in SQLite are always long/Int64 - including ID columns, so converting to int
                        Console.WriteLine("A record featuring encrypted data with ID " + newRowId.ToString() + " was created in table [" + myTableName + "].");
                    }

                    //Get the value of the encrypted column
                    sql = "SELECT [EncryptedColumn] FROM " + myTableName + " WHERE [IdColumn] = @id;";
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        cmd.Parameters.Add(new SqliteParameter("@id", newRowId));
                        dbConn.SafeOpen();
                        string encryptedColumnValue = cmd.ExecuteScalar().ToString();
                        var decryptedValue = myApp.AppCryptEngine.DecryptObject<Tuple<string, string, string>>(encryptedColumnValue);
                        Console.WriteLine("The actual (encrypted) value of the [EncryptedColumn] column of record ID " + newRowId.ToString() + " is: " + encryptedColumnValue);
                        Console.WriteLine("The decrypted value of the [EncryptedColumn] column of record ID " + newRowId.ToString() + " is: " +
                            decryptedValue.Item1 + " " + decryptedValue.Item2 + " " + decryptedValue.Item3);
                    }

                    //Using a SqliteDataReader and GetDecrypted<T> to get all of the encrypted values
                    sql = "SELECT [IdColumn], [DateTimeColumn], [EncryptedColumn] FROM " + myTableName + ";";
                    using (var cmd = new SqliteCommand(sql, dbConn)) {
                        dbConn.SafeOpen();
                        using (var dr = new SqliteDataReader(cmd)) {
                            while (dr.Read()) {
                                var sb = new StringBuilder();
                                sb.Append("ID: " + dr.GetInt32("IdColumn").ToString());
                                sb.Append(" - Timestamp: " + dr.GetDateTime("DateTimeColumn").ToString());
                                //IMPORTANT: GetDecrypted<T> will throw an exception on a NULL column value, unless suppressExceptions is set to True
                                //  as in the line below.  You may want to use TryDecrypt<T> instead.
                                var decryptedValue = dr.GetDecrypted<Tuple<string, string, string>>("EncryptedColumn", true);
                                sb.Append(" - Value: " + ((decryptedValue == null) ? "NULL" :
                                    decryptedValue.Item1 + " " + decryptedValue.Item2 + " " + decryptedValue.Item3));
                                Console.WriteLine(sb.ToString());
                            }
                        }
                    }

                    Console.WriteLine(" ");
                    Console.WriteLine("PART 2 - Doing EncryptedTable stuff");

                    using (var encTable = new EncryptedTable<SampleDataItem>(myApp.AppCryptEngine, dbConn)) {
                        myEncryptedTableName = encTable.TableName;

                        //Shouldn't need to call CheckDbTable manually, but I am going to check to see if there are
                        //  records in the table, so I need to make sure the table exists
                        //This will check the table and create the table and/or any missing columns if needed.
                        encTable.CheckDbTable();

                        //Using ADO to see if there are already records in the table
                        long numRecords;
                        sql = "SELECT COUNT(*) FROM " + myEncryptedTableName + ";";
                        using (var cmd = new SqliteCommand(sql, dbConn)) {
                            dbConn.SafeOpen();
                            numRecords = (long)cmd.ExecuteScalar();
                        }
                        Console.WriteLine("There are currently " + numRecords.ToString() + " records in the table: " + myEncryptedTableName);

                        #region Add some data
                        encTable.AddItem(new SampleDataItem {
                            FirstName = "Jim",
                            LastName = "Kirk",
                            Birthdate = new DateTime(2233, 3, 22),
                            StateAbbreviation = "IA",
                            ZipCode = "12345",
                            MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(2251, 4, 18), "Joined starfleet"),
                            new Tuple<DateTime, string>(new DateTime(2255, 9, 26), "Kobayashi maru, 'nuff said"),
                            new Tuple<DateTime, string>(new DateTime(2258, 5, 19), "Promoted to captain, U.S.S. Enterprise"),
                            new Tuple<DateTime, string>(new DateTime(2270, 7, 22), "Promoted to Addmeerrall")
                        }
                        });

                        encTable.AddItem(new SampleDataItem {
                            FirstName = "Joan",
                            LastName = "Johnson",
                            Birthdate = new DateTime(1964, 8, 22),
                            ZipCode = "54321",
                            MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1987, 9, 18), "Married Bob")
                        }
                        });

                        encTable.AddItem(new SampleDataItem {
                            FirstName = "Bob",
                            LastName = "Johnson",
                            Birthdate = new DateTime(1961, 5, 23),
                            ZipCode = "54321",
                            MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1987, 9, 18), "Married Joan")
                        }
                        });

                        encTable.AddItem(new SampleDataItem {
                            FirstName = "Ned",
                            LastName = "Johnson",
                            Birthdate = new DateTime(1992, 12, 18),
                            ZipCode = "54321",
                            MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(2010, 9, 12), "Off to college")
                        }
                        });
                        #endregion

                        Console.WriteLine(String.Format("There are currently {0} items to be written to the encrypted table: {1}",
                            encTable.TempItems.Where(i => i.IsDirty).Count(), myEncryptedTableName));

                        //Note that at this moment, nothing new has been written to the table.  
                        //  The table will be updated on encTable.Dispose (happens automatically at the end of this using() code 
                        //  block, or we could force it now with encTable.WriteItemChanges()

                        //encTable.WriteItemChanges();
                    }



                }
                Console.WriteLine("Done.");
            }
            catch (Exception ex) {
                MessageBox.Show("An error occurred: " + ex.Message + "\n\nDetails:\n" + ex.ToString(), "ERROR", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            #endregion

        }
    }
}
