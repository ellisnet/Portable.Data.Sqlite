﻿/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

//Was: namespace Mono.Data.Sqlite {
namespace Portable.Data.Sqlite {
    using System;
    //using System.Data;
    using Portable.Data;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// This base class provides datatype conversion services for the SQLite provider.
    /// </summary>
    public abstract class SqliteConvert {
        /// <summary>
        /// The value for the Unix epoch (e.g. January 1, 1970 at midnight, in UTC).
        /// </summary>
        protected static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// An array of ISO8601 datetime formats we support conversion from
        /// </summary>
        private static string[] _datetimeFormats = new string[] {
      "THHmmss",
      "THHmm",
      "HH:mm:ss",
      "HH:mm",
      "HH:mm:ss.FFFFFFF",
      "yy-MM-dd",
      "yyyy-MM-dd",
      "yyyy-MM-dd HH:mm:ss.FFFFFFF",
      "yyyy-MM-dd HH:mm:ss",
      "yyyy-MM-dd HH:mm",                               
      "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
      "yyyy-MM-ddTHH:mm",
      "yyyy-MM-ddTHH:mm:ss",
      "yyyyMMddHHmmss",
      "yyyyMMddHHmm",
      "yyyyMMddTHHmmssFFFFFFF",
      "yyyyMMdd"
    };

        /// <summary>
        /// An UTF-8 Encoding instance, so we can convert strings to and from UTF-8
        /// </summary>
        private static Encoding _utf8 = new UTF8Encoding();
        /// <summary>
        /// The default DateTime format for this instance
        /// </summary>
        internal SqliteDateFormats _datetimeFormat;
        /// <summary>
        /// Initializes the conversion class
        /// </summary>
        /// <param name="fmt">The default date/time format to use for this instance</param>
        internal SqliteConvert(SqliteDateFormats fmt) {
            _datetimeFormat = fmt;
        }

        #region UTF-8 Conversion Functions
        /// <summary>
        /// Converts a string to a UTF-8 encoded byte array sized to include a null-terminating character.
        /// </summary>
        /// <param name="sourceText">The string to convert to UTF-8</param>
        /// <returns>A byte array containing the converted string plus an extra 0 terminating byte at the end of the array.</returns>
        public static string ToUTF8(string sourceText) {
            return sourceText;
        }

        /// <summary>
        /// Convert a DateTime to a UTF-8 encoded, zero-terminated byte array.
        /// </summary>
        /// <remarks>
        /// This function is a convenience function, which first calls ToString() on the DateTime, and then calls ToUTF8() with the
        /// string result.
        /// </remarks>
        /// <param name="dateTimeValue">The DateTime to convert.</param>
        /// <returns>The UTF-8 encoded string, including a 0 terminating byte at the end of the array.</returns>
        public string ToUTF8(DateTime dateTimeValue) {
            return ToUTF8(ToString(dateTimeValue));
        }

        /// <summary>
        /// Converts a UTF-8 encoded IntPtr of the specified length into a .NET string
        /// </summary>
        /// <param name="nativestring">The pointer to the memory where the UTF-8 string is encoded</param>
        /// <param name="nativestringlen">The number of bytes to decode</param>
        /// <returns>A string containing the translated character(s)</returns>
        public virtual string ToString(string nativestring, int nativestringlen) {
            return UTF8ToString(nativestring, nativestringlen);
        }

        /// <summary>
        /// Converts a UTF-8 encoded IntPtr of the specified length into a .NET string
        /// </summary>
        /// <param name="nativestring">The pointer to the memory where the UTF-8 string is encoded</param>
        /// <param name="nativestringlen">The number of bytes to decode</param>
        /// <returns>A string containing the translated character(s)</returns>
        public static string UTF8ToString(string nativestring, int nativestringlen) {
            if (nativestringlen == -1) return nativestring;
            else return nativestring.Substring(0, nativestringlen);
        }
        public static string UTF8ToString(IntPtr nativestring, int nativestringlen) {
            if (nativestringlen == 0 || nativestring == IntPtr.Zero) return "";
            if (nativestringlen == -1) {
                do {
                    nativestringlen++;
                } while (Marshal.ReadByte(nativestring, nativestringlen) != 0);
            }

            byte[] byteArray = new byte[nativestringlen];

            Marshal.Copy(nativestring, byteArray, 0, nativestringlen);

            return _utf8.GetString(byteArray, 0, nativestringlen);
        }


        #endregion

        #region DateTime Conversion Functions
        /// <summary>
        /// Converts a string into a DateTime, using the current DateTimeFormat specified for the connection when it was opened.
        /// </summary>
        /// <remarks>
        /// Acceptable ISO8601 DateTime formats are:
        ///   yyyy-MM-dd HH:mm:ss
        ///   yyyyMMddHHmmss
        ///   yyyyMMddTHHmmssfffffff
        ///   yyyy-MM-dd
        ///   yy-MM-dd
        ///   yyyyMMdd
        ///   HH:mm:ss
        ///   THHmmss
        /// </remarks>
        /// <param name="dateText">The string containing either a Tick value, a JulianDay double, or an ISO8601-format string</param>
        /// <returns>A DateTime value</returns>
        public DateTime ToDateTime(string dateText) {
            switch (_datetimeFormat) {
                case SqliteDateFormats.Ticks:
                    return new DateTime(Convert.ToInt64(dateText, CultureInfo.InvariantCulture));
                case SqliteDateFormats.JulianDay:
                    return ToDateTime(Convert.ToDouble(dateText, CultureInfo.InvariantCulture));
                case SqliteDateFormats.UnixEpoch:
                    return UnixEpoch.AddSeconds(Convert.ToInt32(dateText, CultureInfo.InvariantCulture));
                default:
                    return DateTime.ParseExact(dateText, _datetimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None);
            }
        }

        /// <summary>
        /// Converts a julianday value into a DateTime
        /// </summary>
        /// <param name="julianDay">The value to convert</param>
        /// <returns>A .NET DateTime</returns>
        private static DateTime ToDateTime(double julianDay) {
            int month;
            int year;
            double dblZ = Math.Floor(julianDay + 0.5);
            double dblW = Math.Floor((dblZ - 1867216.25) / 36524.25);
            double dblX = Math.Floor(dblW / 4);
            double dblA = dblZ + 1 + dblW - dblX;
            double dblB = dblA + 1524;
            double dblC = Math.Floor((dblB - 122.1) / 365.25);
            double dblD = Math.Floor(365.25 * dblC);
            double dblE = Math.Floor((dblB - dblD) / 30.6001);
            double dblF = Math.Floor(30.6001 * dblE);
            int day = Convert.ToInt32(dblB - dblD - dblF);
            month = dblE > 13 ? Convert.ToInt32(dblE - 13) : Convert.ToInt32(dblE - 1);
            year = month == 1 || month == 2 ? Convert.ToInt32(dblC - 4715) : Convert.ToInt32(dblC - 4716);
            return new DateTime(year, month, day);
        }

        /// <summary>
        /// Converts a DateTime struct to a JulianDay double
        /// </summary>
        /// <param name="value">The DateTime to convert</param>
        /// <returns>The JulianDay value the Datetime represents</returns>
        public static double ToJulianDay(DateTime value) {
            int year = value.Year;
            int month = value.Month;
            int day = value.Day;
            double hour = value.Hour;
            double minute = value.Minute;
            double second = value.Second;
            int isGregorianCal = 1;
            double fraction = day + ((hour + (minute / 60) + (second / 60 / 60)) / 24);

            if (year < 1582) {
                isGregorianCal = 0;
            }

            if (month < 3) {
                year = year - 1;
                month = month + 12;
            }

            var A = year / 100;
            var B = (2 - A + (A / 4)) * isGregorianCal;
            var C = year < 0 ? (365.25 * year) - 0.75 : 365.25 * year;
            var D = 30.6001 * (month + 1);

            var JD = (int)B + (int)C + (int)D + 1720994.5 + fraction;

            return JD;
        }

        /// <summary>
        /// Converts a DateTime to a string value, using the current DateTimeFormat specified for the connection when it was opened.
        /// </summary>
        /// <param name="dateValue">The DateTime value to convert</param>
        /// <returns>Either a string consisting of the tick count for DateTimeFormat.Ticks, a JulianDay double, or a date/time in ISO8601 format.</returns>
        public string ToString(DateTime dateValue) {
            switch (_datetimeFormat) {
                case SqliteDateFormats.Ticks:
                    return dateValue.Ticks.ToString(CultureInfo.InvariantCulture);
                case SqliteDateFormats.JulianDay:
                    return ToJulianDay(dateValue).ToString(CultureInfo.InvariantCulture);
                case SqliteDateFormats.UnixEpoch:
                    return ((long)(dateValue.Subtract(UnixEpoch).Ticks / TimeSpan.TicksPerSecond)).ToString();
                default:
                    return dateValue.ToString(_datetimeFormats[7], CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Internal function to convert a UTF-8 encoded IntPtr of the specified length to a DateTime.
        /// </summary>
        /// <remarks>
        /// This is a convenience function, which first calls ToString() on the IntPtr to convert it to a string, then calls
        /// ToDateTime() on the string to return a DateTime.
        /// </remarks>
        /// <param name="ptr">A pointer to the UTF-8 encoded string</param>
        /// <param name="len">The length in bytes of the string</param>
        /// <returns>The parsed DateTime value</returns>
        internal DateTime ToDateTime(string ptr, int len) {
            return ToDateTime(ToString(ptr, len));
        }

        #endregion

        /// <summary>
        /// Smart method of splitting a string.  Skips quoted elements, removes the quotes.
        /// </summary>
        /// <remarks>
        /// This split function works somewhat like the String.Split() function in that it breaks apart a string into
        /// pieces and returns the pieces as an array.  The primary differences are:
        /// <list type="bullet">
        /// <item><description>Only one character can be provided as a separator character</description></item>
        /// <item><description>Quoted text inside the string is skipped over when searching for the separator, and the quotes are removed.</description></item>
        /// </list>
        /// Thus, if splitting the following string looking for a comma:<br/>
        /// One,Two, "Three, Four", Five<br/>
        /// <br/>
        /// The resulting array would contain<br/>
        /// [0] One<br/>
        /// [1] Two<br/>
        /// [2] Three, Four<br/>
        /// [3] Five<br/>
        /// <br/>
        /// Note that the leading and trailing spaces were removed from each item during the split.
        /// </remarks>
        /// <param name="source">Source string to split apart</param>
        /// <param name="separator">Separator character</param>
        /// <returns>A string array of the split up elements</returns>
        public static string[] Split(string source, char separator) {
            char[] toks = new char[2] { '\"', separator };
            char[] quot = new char[1] { '\"' };
            int n = 0;
            List<string> ls = new List<string>();
            string s;

            while (source.Length > 0) {
                n = source.IndexOfAny(toks, n);
                if (n == -1) break;
                if (source[n] == toks[0]) {
                    //source = source.Remove(n, 1);
                    n = source.IndexOfAny(quot, n + 1);
                    if (n == -1) {
                        //source = "\"" + source;
                        break;
                    }
                    n++;
                    //source = source.Remove(n, 1);
                }
                else {
                    s = source.Substring(0, n).Trim();
                    if (s.Length > 1 && s[0] == quot[0] && s[s.Length - 1] == s[0])
                        s = s.Substring(1, s.Length - 2);

                    source = source.Substring(n + 1).Trim();
                    if (s.Length > 0) ls.Add(s);
                    n = 0;
                }
            }
            if (source.Length > 0) {
                s = source.Trim();
                if (s.Length > 1 && s[0] == quot[0] && s[s.Length - 1] == s[0])
                    s = s.Substring(1, s.Length - 2);
                ls.Add(s);
            }

            string[] ar = new string[ls.Count];
            ls.CopyTo(ar, 0);

            return ar;
        }

        /// <summary>
        /// Convert a value to true or false.
        /// </summary>
        /// <param name="source">A string or number representing true or false</param>
        /// <returns></returns>
        public static bool ToBoolean(object source) {
            if (source is bool) return (bool)source;

            return ToBoolean(source.ToString());
        }

        /// <summary>
        /// Convert a string to true or false.
        /// </summary>
        /// <param name="source">A string representing true or false</param>
        /// <returns></returns>
        /// <remarks>
        /// "yes", "no", "y", "n", "0", "1", "on", "off" as well as Boolean.FalseString and Boolean.TrueString will all be
        /// converted to a proper boolean value.
        /// </remarks>
        public static bool ToBoolean(string source) {
            if (String.Compare(source, bool.TrueString, StringComparison.OrdinalIgnoreCase) == 0) return true;
            else if (String.Compare(source, bool.FalseString, StringComparison.OrdinalIgnoreCase) == 0) return false;

            switch (source.ToLower()) {
                case "yes":
                case "y":
                case "1":
                case "on":
                    return true;
                case "no":
                case "n":
                case "0":
                case "off":
                    return false;
                default:
                    throw new ArgumentException("source");
            }
        }

        #region Type Conversions
        /// <summary>
        /// Determines the data type of a column in a statement
        /// </summary>
        /// <param name="stmt">The statement to retrieve information for</param>
        /// <param name="i">The column to retrieve type information on</param>
        /// <param name="typ">The SQLiteType to receive the affinity for the given column</param>
        internal static void ColumnToType(SqliteStatement stmt, int i, SqliteType typ) {
            typ.Type = TypeNameToDbType(stmt._sql.ColumnType(stmt, i, out typ.Affinity));
        }

        /// <summary>
        /// Converts a SQLiteType to a .NET Type object
        /// </summary>
        /// <param name="t">The SQLiteType to convert</param>
        /// <returns>Returns a .NET Type object</returns>
        internal static Type SqliteTypeToType(SqliteType t) {
            if (t.Type == DbType.Object)
                return _affinitytotype[(int)t.Affinity];
            else
                return SqliteConvert.DbTypeToType(t.Type);
        }

        private static Type[] _affinitytotype = {
      typeof(object),
      typeof(Int64),
      typeof(Double),
      typeof(string),
      typeof(byte[]),
      typeof(object),
      typeof(DateTime),
      typeof(object)
    };

        /// <summary>
        /// For a given intrinsic type, return a DbType
        /// </summary>
        /// <param name="typ">The native type to convert</param>
        /// <returns>The corresponding (closest match) DbType</returns>
        internal static DbType TypeToDbType(Type typ) {
            var tc = NetTypes.GetNetType(typ);
            if (tc == NetType.Object) {
                if (typ == typeof(byte[])) return DbType.Binary;
                if (typ == typeof(Guid)) return DbType.Guid;
                return DbType.String;
            }
            return _typetodbtype[(int)tc];
        }

        private static DbType[] _typetodbtype = {
      DbType.Object,
      DbType.Binary,
      DbType.Object,
      DbType.Boolean,
      DbType.SByte,
      DbType.SByte,
      DbType.Byte,
      DbType.Int16, // 7
      DbType.UInt16,
      DbType.Int32,
      DbType.UInt32,
      DbType.Int64, // 11
      DbType.UInt64,
      DbType.Single,
      DbType.Double,
      DbType.Decimal,
      DbType.DateTime,
      DbType.Object,
      DbType.String,
    };

        /// <summary>
        /// Returns the ColumnSize for the given DbType
        /// </summary>
        /// <param name="typ">The DbType to get the size of</param>
        /// <returns></returns>
        internal static int DbTypeToColumnSize(DbType typ) {
            return _dbtypetocolumnsize[(int)typ];
        }

        private static int[] _dbtypetocolumnsize = {
      2147483647,   // 0
      2147483647,   // 1
      1,     // 2
      1,     // 3
      8,  // 4
      8, // 5
      8, // 6
      8,  // 7
      8,   // 8
      16,     // 9
      2,
      4,
      8,
      2147483647,
      1,
      4,
      2147483647,
      8,
      2,
      4,
      8,
      8,
      2147483647,
      2147483647,
      2147483647,
      2147483647,   // 25 (Xml)
    };

        internal static object DbTypeToNumericPrecision(DbType typ) {
            return _dbtypetonumericprecision[(int)typ];
        }

        private static object[] _dbtypetonumericprecision = {
      DBNull.Value, // 0
      DBNull.Value, // 1
      3,
      DBNull.Value,
      19,
      DBNull.Value, // 5
      DBNull.Value, // 6
      53,
      53,
      DBNull.Value,
      5,
      10,
      19,
      DBNull.Value,
      3,
      24,
      DBNull.Value,
      DBNull.Value,
      5,
      10,
      19,
      53,
      DBNull.Value,
      DBNull.Value,
      DBNull.Value
    };

        internal static object DbTypeToNumericScale(DbType typ) {
            return _dbtypetonumericscale[(int)typ];
        }

        private static object[] _dbtypetonumericscale = {
      DBNull.Value, // 0
      DBNull.Value, // 1
      0,
      DBNull.Value,
      4,
      DBNull.Value, // 5
      DBNull.Value, // 6
      DBNull.Value,
      DBNull.Value,
      DBNull.Value,
      0,
      0,
      0,
      DBNull.Value,
      0,
      DBNull.Value,
      DBNull.Value,
      DBNull.Value,
      0,
      0,
      0,
      0,
      DBNull.Value,
      DBNull.Value,
      DBNull.Value
    };

        internal static string DbTypeToTypeName(DbType typ) {
            for (int n = 0; n < _dbtypeNames.Length; n++) {
                if (_dbtypeNames[n].dataType == typ)
                    return _dbtypeNames[n].typeName;
            }

            return String.Empty;
        }

        private static SqliteTypeNames[] _dbtypeNames = {
      new SqliteTypeNames("INTEGER", DbType.Int64),
      new SqliteTypeNames("TINYINT", DbType.Byte),
      new SqliteTypeNames("INT", DbType.Int32),
      new SqliteTypeNames("VARCHAR", DbType.AnsiString),
      new SqliteTypeNames("NVARCHAR", DbType.String),
      new SqliteTypeNames("CHAR", DbType.AnsiStringFixedLength),
      new SqliteTypeNames("NCHAR", DbType.StringFixedLength),
      new SqliteTypeNames("FLOAT", DbType.Double),
      new SqliteTypeNames("REAL", DbType.Single),          
      new SqliteTypeNames("BIT", DbType.Boolean),
      new SqliteTypeNames("DECIMAL", DbType.Decimal),
      new SqliteTypeNames("DATETIME", DbType.DateTime),
      new SqliteTypeNames("BLOB", DbType.Binary),
      new SqliteTypeNames("UNIQUEIDENTIFIER", DbType.Guid),
      new SqliteTypeNames("SMALLINT", DbType.Int16),
    };
        /// <summary>
        /// Convert a DbType to a Type
        /// </summary>
        /// <param name="typ">The DbType to convert from</param>
        /// <returns>The closest-match .NET type</returns>
        internal static Type DbTypeToType(DbType typ) {
            return _dbtypeToType[(int)typ];
        }

        private static Type[] _dbtypeToType = {
      typeof(string),   // 0
      typeof(byte[]),   // 1
      typeof(byte),     // 2
      typeof(bool),     // 3
      typeof(decimal),  // 4
      typeof(DateTime), // 5
      typeof(DateTime), // 6
      typeof(decimal),  // 7
      typeof(double),   // 8
      typeof(Guid),     // 9
      typeof(Int16),
      typeof(Int32),
      typeof(Int64),
      typeof(object),
      typeof(sbyte),
      typeof(float),
      typeof(string),
      typeof(DateTime),
      typeof(UInt16),
      typeof(UInt32),
      typeof(UInt64),
      typeof(double),
      typeof(string),
      typeof(string),
      typeof(string),
      typeof(string),   // 25 (Xml)
    };

        /// <summary>
        /// For a given type, return the closest-match SQLite TypeAffinity, which only understands a very limited subset of types.
        /// </summary>
        /// <param name="typ">The type to evaluate</param>
        /// <returns>The SQLite type affinity for that type.</returns>
        internal static TypeAffinity TypeToAffinity(Type typ) {
            var tc = NetTypes.GetNetType(typ);
            if (tc == NetType.Object) {
                if (typ == typeof(byte[]) || typ == typeof(Guid))
                    return TypeAffinity.Blob;
                else
                    return TypeAffinity.Text;
            }
            return _typecodeAffinities[(int)tc];
        }

        private static TypeAffinity[] _typecodeAffinities = {
      TypeAffinity.Null,
      TypeAffinity.Blob,
      TypeAffinity.Null,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64, // 7
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64,
      TypeAffinity.Int64, // 11
      TypeAffinity.Int64,
      TypeAffinity.Double,
      TypeAffinity.Double,
      TypeAffinity.Double,
      TypeAffinity.DateTime,
      TypeAffinity.Null,
      TypeAffinity.Text,
    };

        /// <summary>
        /// For a given type name, return a closest-match .NET type
        /// </summary>
        /// <param name="Name">The name of the type to match</param>
        /// <returns>The .NET DBType the text evaluates to.</returns>
        internal static DbType TypeNameToDbType(string Name) {
            if (String.IsNullOrEmpty(Name)) return DbType.Object;

            int x = _typeNames.Length;
            for (int n = 0; n < x; n++) {
                if (String.Compare(Name, _typeNames[n].typeName, StringComparison.OrdinalIgnoreCase) == 0)
                    return _typeNames[n].dataType;
            }
            return DbType.Object;
        }
        #endregion

        private static SqliteTypeNames[] _typeNames = {
      new SqliteTypeNames("COUNTER", DbType.Int64),
      new SqliteTypeNames("AUTOINCREMENT", DbType.Int64),
      new SqliteTypeNames("IDENTITY", DbType.Int64),
      new SqliteTypeNames("LONGTEXT", DbType.String),
      new SqliteTypeNames("LONGCHAR", DbType.String),
      new SqliteTypeNames("LONGVARCHAR", DbType.String),
      new SqliteTypeNames("LONG", DbType.Int64),
      new SqliteTypeNames("TINYINT", DbType.Byte),
      new SqliteTypeNames("INTEGER", DbType.Int64),
      new SqliteTypeNames("INT", DbType.Int32),
      new SqliteTypeNames("VARCHAR", DbType.String),
      new SqliteTypeNames("NVARCHAR", DbType.String),
      new SqliteTypeNames("CHAR", DbType.String),
      new SqliteTypeNames("NCHAR", DbType.String),
      new SqliteTypeNames("TEXT", DbType.String),
      new SqliteTypeNames("NTEXT", DbType.String),
      new SqliteTypeNames("STRING", DbType.String),
      new SqliteTypeNames("DOUBLE", DbType.Double),
      new SqliteTypeNames("FLOAT", DbType.Double),
      new SqliteTypeNames("REAL", DbType.Single),          
      new SqliteTypeNames("BIT", DbType.Boolean),
      new SqliteTypeNames("YESNO", DbType.Boolean),
      new SqliteTypeNames("LOGICAL", DbType.Boolean),
      new SqliteTypeNames("BOOL", DbType.Boolean),
      new SqliteTypeNames("BOOLEAN", DbType.Boolean),
      new SqliteTypeNames("NUMERIC", DbType.Decimal),
      new SqliteTypeNames("DECIMAL", DbType.Decimal),
      new SqliteTypeNames("MONEY", DbType.Decimal),
      new SqliteTypeNames("CURRENCY", DbType.Decimal),
      new SqliteTypeNames("TIME", DbType.DateTime),
      new SqliteTypeNames("DATE", DbType.DateTime),
      new SqliteTypeNames("SMALLDATE", DbType.DateTime),
      new SqliteTypeNames("BLOB", DbType.Binary),
      new SqliteTypeNames("BINARY", DbType.Binary),
      new SqliteTypeNames("VARBINARY", DbType.Binary),
      new SqliteTypeNames("IMAGE", DbType.Binary),
      new SqliteTypeNames("GENERAL", DbType.Binary),
      new SqliteTypeNames("OLEOBJECT", DbType.Binary),
      new SqliteTypeNames("GUID", DbType.Guid),
      new SqliteTypeNames("GUIDBLOB", DbType.Guid),
      new SqliteTypeNames("UNIQUEIDENTIFIER", DbType.Guid),
      new SqliteTypeNames("MEMO", DbType.String),
      new SqliteTypeNames("NOTE", DbType.String),
      new SqliteTypeNames("SMALLINT", DbType.Int16),
      new SqliteTypeNames("BIGINT", DbType.Int64),
      new SqliteTypeNames("TIMESTAMP", DbType.DateTime),
      new SqliteTypeNames("DATETIME", DbType.DateTime),
    };

        /// <summary>
        /// Translates a SQLite result message into a helpful text description (note: creating these helpful text descriptions is currently a work-in-progress).
        /// </summary>
        /// <param name="result">The SQLite result to translate</param>
        /// <returns>Text description</returns>
        public static string ToResultText(SQLitePCL.SQLiteResult result) {
            string resultText = "An unexpected SQLite result was encountered";

            switch (result) {
                case SQLitePCL.SQLiteResult.ABORT:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.AUTH:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.BUSY:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.CANTOPEN:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.CONSTRAINT:
                    resultText = "A SQLite table/column constraint violation occurred.";
                    break;
                case SQLitePCL.SQLiteResult.CORRUPT:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.DONE:
                    resultText = "A SQLite DONE result was returned.";
                    break;
                case SQLitePCL.SQLiteResult.EMPTY:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.ERROR:
                    resultText = "A SQLite ERROR result was returned.";
                    break;
                case SQLitePCL.SQLiteResult.FORMAT:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.FULL:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.INTERNAL:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.INTERRUPT:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.IOERR:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.LOCKED:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.MISMATCH:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.MISUSE:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.NOLFS:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.NOMEM:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.NOTADB:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.NOTFOUND:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.NOTICE:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.OK:
                    resultText = "A SQLite OK result was returned.";
                    break;
                case SQLitePCL.SQLiteResult.PERM:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.PROTOCOL:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.RANGE:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.READONLY:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.ROW:
                    resultText = "A SQLite ROW result was returned.";
                    break;
                case SQLitePCL.SQLiteResult.SCHEMA:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.TOOBIG:
                    resultText += ": " + result.ToString();
                    break;
                case SQLitePCL.SQLiteResult.WARNING:
                    resultText += ": " + result.ToString();
                    break;
                default:
                    resultText += ".";
                    break;
            }

            return resultText;
        }

    }

    /// <summary>
    /// SQLite has very limited types, and is inherently text-based.  The first 5 types below represent the sum of all types SQLite
    /// understands.  The DateTime extension to the spec is for internal use only.
    /// </summary>
    public enum TypeAffinity {
        /// <summary>
        /// Not used
        /// </summary>
        Uninitialized = 0,
        /// <summary>
        /// All integers in SQLite default to Int64
        /// </summary>
        Int64 = 1,
        /// <summary>
        /// All floating point numbers in SQLite default to double
        /// </summary>
        Double = 2,
        /// <summary>
        /// The default data type of SQLite is text
        /// </summary>
        Text = 3,
        /// <summary>
        /// Typically blob types are only seen when returned from a function
        /// </summary>
        Blob = 4,
        /// <summary>
        /// Null types can be returned from functions
        /// </summary>
        Null = 5,
        /// <summary>
        /// Used internally by this provider
        /// </summary>
        DateTime = 10,
        /// <summary>
        /// Used internally
        /// </summary>
        None = 11,
    }

    /// <summary>
    /// This implementation of SQLite for ADO.NET can process date/time fields in databases in only one of three formats.  Ticks, ISO8601
    /// and JulianDay.
    /// </summary>
    /// <remarks>
    /// ISO8601 is more compatible, readable, fully-processable, but less accurate as it doesn't provide time down to fractions of a second.
    /// JulianDay is the numeric format the SQLite uses internally and is arguably the most compatible with 3rd party tools.  It is
    /// not readable as text without post-processing.
    /// Ticks less compatible with 3rd party tools that query the database, and renders the DateTime field unreadable as text without post-processing.
    /// 
    /// The preferred order of choosing a datetime format is JulianDay, ISO8601, and then Ticks.  Ticks is mainly present for legacy 
    /// code support.
    /// </remarks>
    public enum SqliteDateFormats {
        /// <summary>
        /// Using ticks is not recommended and is not well supported with LINQ.
        /// </summary>
        Ticks = 0,
        /// <summary>
        /// The default format for this provider.
        /// </summary>
        ISO8601 = 1,
        /// <summary>
        /// JulianDay format, which is what SQLite uses internally
        /// </summary>
        JulianDay = 2,
        /// <summary>
        /// The whole number of seconds since the Unix epoch (January 1, 1970).
        /// </summary>
        UnixEpoch = 3,
    }

    /// <summary>
    /// This enum determines how SQLite treats its journal file.
    /// </summary>
    /// <remarks>
    /// By default SQLite will create and delete the journal file when needed during a transaction.
    /// However, for some computers running certain filesystem monitoring tools, the rapid
    /// creation and deletion of the journal file can cause those programs to fail, or to interfere with SQLite.
    /// 
    /// If a program or virus scanner is interfering with SQLite's journal file, you may receive errors like "unable to open database file"
    /// when starting a transaction.  If this is happening, you may want to change the default journal mode to Persist.
    /// </remarks>
    public enum SqliteJournalModeEnum {
        /// <summary>
        /// The default mode, this causes SQLite to create and destroy the journal file as-needed.
        /// </summary>
        Delete = 0,
        /// <summary>
        /// When this is set, SQLite will keep the journal file even after a transaction has completed.  It's contents will be erased,
        /// and the journal re-used as often as needed.  If it is deleted, it will be recreated the next time it is needed.
        /// </summary>
        Persist = 1,
        /// <summary>
        /// This option disables the rollback journal entirely.  Interrupted transactions or a program crash can cause database
        /// corruption in this mode!
        /// </summary>
        Off = 2
    }

    /// <summary>
    /// Struct used internally to determine the datatype of a column in a resultset
    /// </summary>
    internal class SqliteType {
        /// <summary>
        /// The DbType of the column, or DbType.Object if it cannot be determined
        /// </summary>
        internal DbType Type;
        /// <summary>
        /// The affinity of a column, used for expressions or when Type is DbType.Object
        /// </summary>
        internal TypeAffinity Affinity;
    }

    internal struct SqliteTypeNames {
        internal SqliteTypeNames(string newtypeName, DbType newdataType) {
            typeName = newtypeName;
            dataType = newdataType;
        }

        internal string typeName;
        internal DbType dataType;
    }

    /// <summary>
    /// Intermediary types for converting between Sqlite data types and CLR data types
    /// </summary>
    public enum NetType {
        /// <summary>
        /// Empty data type
        /// </summary>
        Empty = 0,
        /// <summary>
        /// Object data type
        /// </summary>
        Object = 1,
        /// <summary>
        /// SQL NULL value
        /// </summary>
        DBNull = 2,
        /// <summary>
        /// Boolean data type
        /// </summary>
        Boolean = 3,
        /// <summary>
        /// Character data type
        /// </summary>
        Char = 4,
        /// <summary>
        /// Signed byte data type
        /// </summary>
        SByte = 5,
        /// <summary>
        /// Byte data type
        /// </summary>
        Byte = 6,
        /// <summary>
        /// Short/Int16 data type
        /// </summary>
        Int16 = 7,
        /// <summary>
        /// Unsigned short data type
        /// </summary>
        UInt16 = 8,
        /// <summary>
        /// Integer data type
        /// </summary>
        Int32 = 9,
        /// <summary>
        /// Unsigned integer data type
        /// </summary>
        UInt32 = 10,
        /// <summary>
        /// Long/Int64 data type
        /// </summary>
        Int64 = 11,
        /// <summary>
        /// Unsigned long data type
        /// </summary>
        UInt64 = 12,
        /// <summary>
        /// Single precision float data type
        /// </summary>
        Single = 13,
        /// <summary>
        /// Double precision float data type
        /// </summary>
        Double = 14,
        /// <summary>
        /// Decimal data type
        /// </summary>
        Decimal = 15,
        /// <summary>
        /// DateTime data type
        /// </summary>
        DateTime = 16,
        /// <summary>
        /// String data type
        /// </summary>
        String = 18,
    }

    internal static class NetTypes {
        private static Dictionary<Type, NetType> types = new Dictionary<Type, NetType> {
      {typeof(bool), NetType.Boolean},
      {typeof(char), NetType.Char},
      {typeof(sbyte), NetType.SByte},
      {typeof(byte), NetType.Byte},
      {typeof(short), NetType.Int16},
      {typeof(ushort), NetType.UInt16},
      {typeof(int), NetType.Int32},
      {typeof(uint), NetType.UInt32},
      {typeof(long), NetType.Int64},
      {typeof(ulong), NetType.UInt64},
      {typeof(float), NetType.Single},
      {typeof(double), NetType.Double},
      {typeof(decimal), NetType.Decimal},
      {typeof(DateTime), NetType.DateTime},
      {typeof(string), NetType.String}
    };
        public static NetType GetNetType(Type type) {
            if (type == (Type)null)
                return NetType.Empty;
            else if (type != type.GetUnderlyingSystemType() && type.GetUnderlyingSystemType() != (Type)null)
                return GetNetType(type.GetUnderlyingSystemType());
            else
                return GetNetTypeImpl(type);
        }
        private static NetType GetNetTypeImpl(Type type) {
            if (types.ContainsKey(type)) {
                return types[type];
            }

            if (type.IsEnum()) {
                return types[Enum.GetUnderlyingType(type)];
            }

            return NetType.Object;
        }
    }

}

//#if PORTABLE

namespace System.Runtime.InteropServices
{
    public static class Marshal
    {
        public static int ReadByte(IntPtr nativestring, int nativestringlen)
        {
            throw new System.NotImplementedException();
        }

        public static void Copy(IntPtr nativestring, byte[] byteArray, int p, int nativestringlen)
        {
            throw new System.NotImplementedException();
        }
    }
}

//#endif
