//// System.DBNull.cs
////
//// Authors:
////   Duncan Mak (duncan@ximian.com)
////   Ben Maurer (bmaurer@users.sourceforge.net)
////
//// (C) 2002 Ximian, Inc. http://www.ximian.com
//// (C) 2003 Ben Maurer
////

////
//// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
//// 
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace Portable.Data
{
	public sealed class DBNull
	{
		// Fields
		public static readonly DBNull Value = new DBNull ();

		// Private constructor
		private DBNull ()
		{
		}

		public override string ToString ()
		{
			return string.Empty;
		}

		public string ToString (IFormatProvider provider)
		{
			return string.Empty;
		}
	}

    /// <summary>
    /// Specifies what should happen if an unexpected NULL value is encountered in a SQLite table field
    /// </summary>
    public enum DbNullHandling {
        /// <summary>
        /// An exception will be thrown, indicating that the specified field was null
        /// </summary>
        ThrowDbNullException = 0,
        /// <summary>
        /// The default value of the specified Type (not column default value) will be returned, i.e. 'null' for reference types
        /// </summary>
        ReturnTypeDefaultValue
    }

    /// <summary>
    /// Exception indicating that a database table column value of NULL was encountered.
    /// </summary>
    public class DbNullException : Exception {
        public DbNullException()
            : base("The table column value is NULL.") {
        }

        public DbNullException(string s)
            : base(s) {
        }

        public DbNullException(string s, Exception innerException)
            : base(s, innerException) {
        }
    }
}
