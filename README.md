Portable.Data.Sqlite
====================

This is a portable cross-platform ADO provider for SQLite databases, featuring table-column-level and table-record-level data encryption.  It is intended to be a Portable Class Library (PCL) that does the following:

  1. Works properly on the following platforms:
    * Windows desktop (with .NET Framework 4.5 or higher)
    * Windows Runtime (a.k.a. Windows Store) 8 and higher
    * Windows Phone 8 and higher
    * Xamarin.iOS
    * Xamarin.Android
  2. Runs on top of (and requires) the Portable Class Library for SQLite (SQLitePCL) from MSOpenTech - info available [here](http://sqlitepcl.codeplex.com/) - NuGet package available [here](http://www.nuget.org/packages/SQLitePCL)
  3. Provides access to SQLite databases via the native built-into-the-operating-system implementations of SQLite for the platforms listed above (where an implementation exists).
  4. Provides a PCL-based ADO-style way of interacting with SQLite databases; based on a portable (PCL) implementation of Mono.Data.Sqlite that was adapted by Matthew Leibowitz (@mattleibow) - available [here](https://github.com/mattleibow/Mono.Data.Sqlite)
  5. Enables **easy table record-level and column-level encryption of data** in your SQLite database.

The developer of this library welcomes all feedback, suggestions, issue/bug reports, and pull requests. Please log questions and issues in the Portable.Data.Sqlite GitHub *Issues* section - available [here](https://github.com/ellisnet/Portable.Data.Sqlite/issues)

Important Notes About Encryption
--------------------------------
  1. For various reasons, this library **does not include an encryption algorithm**.  All operating systems listed above have built-in AES encryption that can be used with this library (as an example of one type of encryption algorithm that works well).  It is up to you to specify the algorithm to use by implementing the *IObjectCryptEngine* interface.  This allows you to choose exactly how your data will be encrypted.  Taking a well understood encryption algorithm and implementing your own *encryption engine* should not be too difficult; see detailed information below.
  2. This library **does not implement full database encryption** - for that, please investigate SQLCipher - available [here](http://sqlcipher.net/)  
It is up to you - the developer who is using this library - to decide which data in the database to encrypt; and which data not to.  You can encrypt any column in any table, and you can encrypt an entire table (i.e. all of the records in a table).

Table of Contents
-----------------
Since I am adding quite a lot of information to this readme file, here is an outline of the sections below - to make finding what you are looking for easier:

  1. Setting up SQLitePCL and implementing *IObjectCryptEngine* - the only potentially difficult parts of using the library.  May as well cover those up front...
  2. Explanation of sample projects in the Samples folder.
  3. Example code showing use of the library, Part 1: Doing ADO stuff - Creating SQLite-based DbConnections, using SQLite versions of DbCommand and DbDataReader.  Also included: sample code for encrypting a single column in a SQLite table.
  4. Example code showing use of the library, Part 2: Using EncryptedTable&lt;T&gt; - How to create an object in your code that will have a table where all of the record data is encrypted, how to read and write values, and how to perform searches of the data without the need to decrypt every object in the table.
  5. Information about storing your SQLite database password securely on the various platforms
  6. How to use this library asynchronously, using *async/await*
  7. How to use this library with Xamarin.Forms
  8. How to use this library with MvvmCross
  9. Answers to frequently asked questions (FAQ)

Dealing with the (Potentially) Difficult Parts Up Front
-------------------------------------------------------
So let's just get straight to the potentially difficult parts.  They are:

  1. It may be difficult to set up SQLitePCL (i.e. the Portable Class Library for SQLite mentioned above) if you are developing for a platform that doesn't come with SQLite built-in - like Windows (desktop) or Windows Store.  You have to install an add-on for Visual Studio and/or download a .DLL or two.  But there is excellent information available on the SQLitePCL CodePlex site - [documentation here](https://sqlitepcl.codeplex.com/documentation)  
Note that there is also an extra step needed for Xamarin.iOS, where you initialize/load SQLitePCL.Ext.dll by calling *SQLitePCL.CurrentPlatform.Init()*
  2. You need to implement your chosen encryption algorithm, by creating a class that implements *Portable.Data.Sqlite.IObjectCryptEngine* - You will need to create a class that has EncryptObject() and DecryptObject&lt;T&gt;() methods.  EncryptObject() will take just about any CLR Object and will serialize it and encrypt it, and then return the byte-array as a string; DecryptObject&lt;T&gt;() will take a byte-array-as-a-string, decrypt it and de-serialize it back to an object of the type specified as &lt;T&gt;.  So, DecryptObject&lt;MyObject&gt;(myEncryptedString) should decrypt myEncryptedString and turn it into a MyObject-class object and return it.  I have found that using the popular JSON.NET library (from [here](http://www.newtonsoft.com) ) works great for the serializing and de-serializing part.  Also, if you are looking for a PCL-based library with a bunch of encryption algorithms, take a look at Bouncy Castle PCL (Portable.BouncyCastle) - available on NuGet [here](http://www.nuget.org/packages/Portable.BouncyCastle)

So, here is an example of how you *could* implement *IObjectCryptEngine* - using AES encryption that comes built into the OS on all supported platforms.  The following code should provide reasonable data encryption/security, but has a few issues as described in the comments:

```c#
using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Portable.Data.Sqlite;

//Example AES-based "crypt engine" for use with Portable.Data.Sqlite,
//  should work on all platforms supported by Portable.Data.Sqlite.

//Disclaimer:
//  THIS SAMPLE CODE IS BEING PROVIDED FOR DEMONSTRATION PURPOSES ONLY, AND
//  IS NOT INTENDED FOR USE WITH SOFTWARE THAT MUST PROVIDE ACTUAL DATA SECURITY.
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
//  SOFTWARE.

public class SimpleAesCryptEngine : IObjectCryptEngine {

    string _cryptoKey;
    Aes _aesProvider;

    private byte[] getBytes(string text, int requiredLength) {
        var result = new byte[requiredLength];
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        int offset = 0;
        while (offset < requiredLength) {
            int toCopy = (requiredLength >= (offset + textBytes.Length)) ?
                textBytes.Length : requiredLength - offset;
            Buffer.BlockCopy(textBytes, 0, result, offset, toCopy);
            offset += toCopy;
        }
        return result;
    }

    public SimpleAesCryptEngine(string cryptoKey) {
        _cryptoKey = cryptoKey;
        _aesProvider = Aes.Create();
        _aesProvider.Key = getBytes(cryptoKey, _aesProvider.Key.Length);
        //Here we are using the same value for all initialization vectors.
        //  This is NOT RECOMMENDED - it should be randomly generated;
        //  however, then you need a way to retrieve it for decryption.
        //  More info: http://en.wikipedia.org/wiki/Initialization_vector
        _aesProvider.IV = getBytes("THIS SHOULD BE RANDOM", _aesProvider.IV.Length);
    }

    public T DecryptObject<T>(string stringToDecrypt) {
        T result = default(T);
        if (stringToDecrypt != null) {
            byte[] bytesToDecrypt = Convert.FromBase64String(stringToDecrypt);
            byte[] decryptedBytes = 
                _aesProvider.CreateDecryptor().TransformFinalBlock(bytesToDecrypt, 0, bytesToDecrypt.Length);
            result = 
                JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decryptedBytes));
        }
        return result;
    }

    public string EncryptObject(object objectToEncrypt) {
        string result = null;
        if (objectToEncrypt != null) {
            byte[] bytesToEncrypt = 
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToEncrypt));
            //Not sure if I should be using TransformFinalBlock() here, 
            //  or if it is more secure if I break the byte array into
            //  blocks and process one block at a time.
            byte[] encryptedBytes = 
                _aesProvider.CreateEncryptor().TransformFinalBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
            result = Convert.ToBase64String(encryptedBytes);
        }
        return result;
    }

    public void Dispose() {
        _cryptoKey = null;
        _aesProvider.Dispose();
        _aesProvider = null;
    }
}
```

Explanation of Sample Projects
------------------------------
In the Samples folder of this repository you will find the folders listed below, with samples for each of the supported platforms:  Windows desktop (with .NET Framework 4.5 or higher); Windows Runtime (a.k.a. Windows Store) 8 and higher; Windows Phone 8 and higher (sample coming soon); Xamarin.iOS; and Xamarin.Android.

These are no-frills *File - New Project* projects where I added some code to the initial "Window/Page/Activity/ViewController" Load event to create a SQLite database, and then create (and manipulate) some tables and records in it.  There is nothing to see in the UI - to check things out, you should read the code and then see what is happening in the Output window of your IDE when you run the application in Debug configuration.

If you want to see exactly what code I added to each *File - New Project* for demonstrating SQLite functionality, search for *ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite*.  Any code that I added to the automatically generated project files is tagged with that comment.

Samples sub-folders:
  * SqliteSampleCode - code that is shared (via file linking) by all projects, includes a class - *SampleDataItem* - with detailed comments about how to create an object for use with EncryptedTable&lt;T&gt;
  * SampleApp.Wpf - a sample project for Windows desktop (WPF) with .NET Framework 4.5 or higher. Most of the sample code is in *MainWindow.xaml.cs*, with some application-level stuff in the *App.xaml.cs* file. I find this one to be the most helpful for development/testing, because it is easy for me to examine the SQLite database file using Navicat for SQLite, and see what is happening in the database.  Navicat for SQLite is available [here](http://www.navicat.com/products/navicat-for-sqlite) - there is also an excellent free SQLite database tool available [here](http://sqlitebrowser.org/)
  * SampleApp.Android - a sample project for use with Xamarin.Android, with the sample code in *Main.Activity.cs*
  * SampleApp.iOS - a sample project for use with Xamarin.iOS, with the sample code in *SampleApp.iOSViewController.cs*, and some application-level code in *AppDelegate.cs*
  * SampleApp.WinStore - a sample project for use with Windows 8.1 and higher (i.e. a "metro"-style app). Most of the sample code is in *MainPage.xaml.cs*, with some application-level stuff in the *App.xaml.cs* file.
  * SampleApp.WinPhone - (COMING SOON) a sample project for use with Windows Phone 8 and higher

Examples Part 1: Doing ADO Stuff
--------------------------------
Note: This library doesn't have a "full" implementation of ADO.NET as you might be used to with Microsoft SQL Server (System.Data.SqlClient).  This is partly due to limitations of the versions of SQLite that ship with mobile devices, partly due to limitations of the SQLitePCL library, and partly... well... things just haven't been implemented and/or tested yet.  There is some code relating to Transactions for example (Portable.Data.Transactions) that may do some things, but I have ignored this and have done **zero testing** with it.  Mainly, I stick to using DbConnections (i.e. SqliteAdoConnection), DbCommands (i.e. SqliteCommand) and DbDataReaders (i.e. SqliteDataReader).  Those are all you need for most SQLite operations.

Step 1) Opening a connection to our database (database file will be created if it doesn't exist), creating a table and adding/reading (i.e. INSERTing/SELECTing) a record.

```c#
//using Portable.Data.Sqlite;

string sql;
string databasePath = System.IO.Path.Combine(pathToDatabaseFolder, "mydatabase.sqlite");
using (IObjectCryptEngine myCryptEngine = new MyCryptEngine("my encryption password")) 
{
    using (var myConnection = new SqliteAdoConnection(
        new SQLitePCL.SQLiteConnection(databasePath), myCryptEngine)) 
    {
        sql = "CREATE TABLE IF NOT EXISTS [myTableName] " 
            + "(Id INTEGER PRIMARY KEY AUTOINCREMENT, FirstWord TEXT, SecondWord TEXT);";
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
		{
            myConnection.SafeOpen(); //myConnection.SafeOpen() is the same as 
                //  myConnection.Open() - but doesn't throw an exception if the connection
                //  is already open.
            myCommand.ExecuteNonQuery();
        }
		//NOTE: All of the other example SQL in this section - Part 1: ADO stuff - goes here
    }
}
```
Here is what the above code does:

We identified a new SQLite database [file] called *mydatabase.sqlite* in the folder specified in *pathToDatabaseFolder*.  The recommended folder varies based on the platform (Windows Store, iOS, Android, etc.)  I will probably add a list of the recommended folder paths for each platform here; but at the moment, I haven't verified those.  You can look at the examples in the Samples folder to see which folders they are using.

Next we created an instance of *MyCryptEngine* which is our implementation of *IObjectCryptEngine* as described above, with our secret password.

Next we created an instance of *SqliteAdoConnection* based on a *SQLitePCL.SQLiteConnection* to our database [file]; and we passed in our crypt engine. Creating the new *SQLitePCL.SQLiteConnection*, with the path to our file, created our database file if it didn't already exist.

The *sql = "CREATE TABLE IF NOT EXISTS..."* line is our SQL statement to create our new table.  Using the *SqliteCommand.ExecuteNonQuery()* line, we can send pretty much any SQLite-legal SQL statements to our database.  Note that SQLite SQL is not the same as the T-SQL that you use with Microsoft's SQL products.  For example - as you will see below - instead of *SELECT TOP 1 * FROM myTableName* (T-SQL), we will use *SELECT * FROM myTableName LIMIT 1* (SQLite SQL).  Lots of information about SQLite's flavor of SQL is available [here](http://www.sqlite.org/lang.html)

Finally, we create a *SqliteCommand* with our SQL statement and connection, open the database connection and execute it.

```c#
        //Add/insert a record
        sql = "INSERT INTO [myTableName] (FirstWord, SecondWord) " +
            "VALUES (@firstword, @secondword);";
        int newRowId;
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.Parameters.Add(new SqliteParameter("@firstword", "Hello"));
            myCommand.Parameters.Add(new SqliteParameter("@secondword", "SQLite"));
            myConnection.SafeOpen();
            newRowId = (int)myCommand.ExecuteReturnRowId();
        }

        //Select a record
        sql = "SELECT * FROM [myTableName] WHERE [Id] = @rowid;";
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.Parameters.Add(new SqliteParameter("@rowid", value: newRowId));
            myConnection.SafeOpen();
            using (SqliteDataReader myReader = myCommand.ExecuteReader()) 
            {
                while (myReader.Read()) 
                {
                    int rowId = myReader.GetInt32("Id");
                    string firstWord = myReader["FirstWord"].ToString() ?? "";
                    string lastWord = myReader.GetString("SecondWord") ?? "";
                    Console.WriteLine("{0} {1}!",  firstWord, lastWord); // Output: Hello SQLite!
                }
            }
        }

        //Select the 'TOP 1' record - via 'LIMIT 1'
        sql = "SELECT [Id] FROM [myTableName] ORDER BY [Id] DESC LIMIT 1;";
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myConnection.SafeOpen();
            int highestRowId = Convert.ToInt32(myCommand.ExecuteScalar());
            Console.WriteLine("The highest [Id] column value so far is: {0}", highestRowId);
        }
```
Here is what the above code does:

First we INSERTed a row into our table with the values ('*Hello*', '*SQLite*').  We used *SQLiteCommand.ExecuteReturnRowId()* so we would get back the RowId (i.e. the value of our INTEGER PRIMARY KEY [Id] column). Note that SQLite mostly deals in the long/Int64 datatype for Integer values, so that is what *SQLiteCommand.ExecuteReturnRowId()* returned, and we cast it as an int/Int32.

Next we SELECTed the row using *SqliteCommand.ExecuteReader()* to create a *SqliteDataReader*, and then *SqliteDataReader.Read()* to iterate through the rows of our recordset.  If more than row had been returned, the *while (myReader.Read()) {}* loop would repeat for each row - i.e. *SqliteDataReader.Read()* returns false when there are no more rows. Notice that when doing *SqliteDataReader.Read()*, there a few different ways to get a particular column value.  You can use *SqliteDataReader[columnName]* or *SqliteDataReader[columnIndex]*, which return *System.Object*; or you can use *SqliteDataReader.GetXXX(columnName)* or *SqliteDataReader.GetXXX(columnIndex)* to get a value of a particular type (e.g. *SqliteDataReader.GetInt32()* or *SqliteDataReader.GetString()* as shown above).

Finally, we used *SqliteCommand.ExecuteScalar()* to get a single value. It returns the value of the first column of the first returned row. But *SqliteCommand.ExecuteScalar()* just returns a *System.Object*; it doesn't know what datatype it is returning, so we had to convert that into an Integer.

Believe it or not, you can do almost everything you could want in your SQLite database with just the above code - and some knowledge of the [SQLite flavor of SQL](http://www.sqlite.org/lang.html).

Step 2) Working with an encrypted column.

```c#
        //Adding the encrypted column
        sql = "ALTER TABLE [myTableName] ADD COLUMN EncryptedColumn ENCRYPTED;";
        myConnection.SafeOpen();
        (new SqliteCommand(sql, myConnection)).ExecuteNonQuery();

        //Adding some encrypted values
        string valueToEncrypt1 = "This string will be encrypted in the database.";
        Tuple<int, string, string> valueToEncrypt2  = 
            Tuple.Create(1, "This object will also", "be encrypted.");
        long encryptedRowId1, encryptedRowId2;
        sql = "INSERT INTO [myTableName] (EncryptedColumn) VALUES (@encrypted);";
        //add value #1
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.AddEncryptedParameter(new SqliteParameter("@encrypted", valueToEncrypt1));
            myConnection.SafeOpen();
            encryptedRowId1 = myCommand.ExecuteReturnRowId();
        }
        //add value #2
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.AddEncryptedParameter(new SqliteParameter("@encrypted", valueToEncrypt2));
            myConnection.SafeOpen();
            encryptedRowId2 = myCommand.ExecuteReturnRowId();
        }

        //Check the encrypted values
        sql = "SELECT [EncryptedColumn] FROM [myTableName] WHERE [Id] = @rowid;";
        //get value #1
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.Parameters.Add(new SqliteParameter("@rowid", value: encryptedRowId1));
            myConnection.SafeOpen();
            using (SqliteDataReader myReader = myCommand.ExecuteReader()) 
            {
                while (myReader.Read()) 
                {
                    string encryptedValue = myReader.GetString("EncryptedColumn");
                    string decryptedValue = myReader.GetDecrypted<string>("EncryptedColumn");
                    Console.WriteLine("The encrypted value is: " + encryptedValue);
                        //Output: The encrypted value is: (random characters)
                    Console.WriteLine("The decrypted value is: " + decryptedValue);
                        //Output: The decrypted value is: This string will be encrypted in the database.
                }
            }
        }
        //get value #1
        using (var myCommand = new SqliteCommand(sql, myConnection)) 
        {
            myCommand.Parameters.Add(new SqliteParameter("@rowid", value: encryptedRowId2));
            myConnection.SafeOpen();
            using (SqliteDataReader myReader = myCommand.ExecuteReader()) 
            {
                while (myReader.Read()) 
                {
                    string encryptedValue = myReader.GetString("EncryptedColumn");
                    Tuple<int, string, string> decryptedValue =
                        myReader.GetDecrypted<Tuple<int, string, string>>("EncryptedColumn");
                    Console.WriteLine("The encrypted value is: " + encryptedValue);
                        //Output: The encrypted value is: (random characters)
                    Console.WriteLine("The decrypted value is: {0} - {1} {2}", 
                        decryptedValue.Item1, decryptedValue.Item2, decryptedValue.Item3);
                        //Output: The decrypted value is: 1 - This object will also be encrypted.
                }
            }
        }
```
Here is what the above code does:

First we added an *EncryptedColumn* column to our table.  We set the datatype for the column to be *ENCRYPTED*, but that is just for show.  It really is *TEXT* - there is no such thing in SQLite as a column of datatype *ENCRYPTED*.  However, I like to do that, in case I look at the table column list - it helps me to know which columns are encrypted.  Note that if this SQL statement was executed again, we would get an exception because the column has already been added to the table, and we can't add it again.  There is a *CREATE TABLE IF NOT EXISTS* but not an *ADD COLUMN IF NOT EXISTS*, but we could have added this column in our original table definition and avoided this problem.  The code in the sample applications get around this problem by performing a *PRAGMA* command to get a list of columns, and then determine if the column existed or not.

We added our values-to-be-encrypted using *SqliteParameter* almost as normal, but the parameter values were added with *SqliteCommand.AddEncryptedParameter(new SqliteParameter(columnName, valueToEncrypt))* - that is the only thing special we had to do.

Then, we got our decrypted value back, by using a *SqliteDataReader* just as before; but we used *SqliteDataReader.GetDecrypted&lt;T&gt;(columnName)*.  Note that this will cause an exception if the value in the table column is NULL, though there is an optional boolean *suppressExceptions* parameter to prevent that.  You can also use *SqliteDataReader.TryDecrypt&lt;T&gt;()* method that will just return a false if the decryption couldn't happen (e.g. if the column value was NULL).

Important Final Notes for ADO:

  1. We were able to store an object (a Tuple, in the above example) - not just a string or integer value - in an encrypted column, because SQLite doesn't really care what we put in there.  It will all be text, as far as SQLite is concerned. We could basically put just about any type of CLR object in an encrypted column... which is pretty powerful.
  2. However searching for matching records based on values in an encrypted column becomes difficult, because you would have to search on an exact encrypted string; or decrypt all of the values in your table to see which records had a matching value.  That is the reason for Part 2: EncryptedTable&lt;T&gt; - so keep reading...

Examples Part 2: Using EncryptedTable&lt;T&gt;
----------------------------------------
(Working on these right now - hopefully available later today or tomorrow - 8/24/2014)

Information about Storing your SQLite Database Password Securely
----------------------------------------------------------------
Coming soon...

How to Use this Library Asynchronously Using *async/await*
----------------------------------------------------------
Coming soon...

How to Use this Library with Xamarin.Forms
------------------------------------------
Coming soon...

How to Use this Library with MvvmCross
--------------------------------------
Coming soon...

Frequently Asked Questions
--------------------------
Answers coming when there are some questions...
