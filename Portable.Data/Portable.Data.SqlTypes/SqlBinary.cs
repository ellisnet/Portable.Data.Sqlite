//
// System.Data.SqlTypes.SqlBinary
//
// Author:
//   Rodrigo Moya (rodrigo@ximian.com)
//   Tim Coleman (tim@timcoleman.com)
//   Ville Palo (vi64pa@koti.soon.fi)
//
// (C) Ximian, Inc.
// (C) Copyright 2002 Tim Coleman
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

//Was: namespace  System.Data.SqlTypes 
namespace Portable.Data.SqlTypes
{
    /// <summary>
    /// Represents a variable-length stream of binary data to be stored in or retrieved from a database.
    /// </summary>
    public struct SqlBinary : INullable, IComparable
    {
        #region Fields

        private readonly byte[] value;
        private readonly bool notNull;

        public static readonly SqlBinary Null;

        #endregion

        #region Constructors

        public SqlBinary(byte[] value)
        {
            this.value = value;
            this.notNull = true;
        }

        #endregion

        #region Properties

        public bool IsNull
        {
            get { return !this.notNull; }
        }

        public byte this[int index]
        {
            get
            {
                if (this.IsNull)
                {
                    throw new SqlNullValueException("The property contains Null.");
                }
                else if (index >= this.Length)
                {
                    throw new IndexOutOfRangeException(
                        "The index parameter indicates a position beyond the length of the byte array.");
                }
                else
                {
                    return this.value[index];
                }
            }
        }

        public int Length
        {
            get
            {
                if (this.IsNull)
                {
                    throw new SqlNullValueException("The property contains Null.");
                }
                else
                {
                    return this.value.Length;
                }
            }
        }

        public byte[] Value
        {
            get
            {
                if (this.IsNull)
                {
                    throw new SqlNullValueException("The property contains Null.");
                }
                else
                {
                    return this.value;
                }
            }
        }

        #endregion

        #region Methods

        public static SqlBinary Add(SqlBinary x, SqlBinary y)
        {
            return (x + y);
        }

        public int CompareTo(object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is SqlBinary))
            {
                throw new ArgumentException(Locale.GetText("Value is not a System.Data.SqlTypes.SqlBinary"));
            }

            return this.CompareTo((SqlBinary) value);
        }

        public int CompareTo(SqlBinary value)
        {
            if (value.IsNull)
            {
                return 1;
            }
            else
            {
                return Compare(this, value);
            }
        }

        public static SqlBinary Concat(SqlBinary x, SqlBinary y)
        {
            return (x + y);
        }

        public override bool Equals(object value)
        {
            if (!(value is SqlBinary))
            {
                return false;
            }
            else if (this.IsNull)
            {
                return ((SqlBinary) value).IsNull;
            }
            else if (((SqlBinary) value).IsNull)
            {
                return false;
            }
            else
            {
                return (bool) (this == (SqlBinary) value);
            }
        }

        public static SqlBoolean Equals(SqlBinary x, SqlBinary y)
        {
            return (x == y);
        }

        public override int GetHashCode()
        {
            // FIXME: I'm not sure is this a right way
            int result = 10;
            for (int i = 0; i < this.value.Length; i++)
            {
                result = 91*result + this.value[i];
            }

            return result;
        }

        #endregion

        #region Operators

        public static SqlBoolean GreaterThan(SqlBinary x, SqlBinary y)
        {
            return (x > y);
        }

        public static SqlBoolean GreaterThanOrEqual(SqlBinary x, SqlBinary y)
        {
            return (x >= y);
        }

        public static SqlBoolean LessThan(SqlBinary x, SqlBinary y)
        {
            return (x < y);
        }

        public static SqlBoolean LessThanOrEqual(SqlBinary x, SqlBinary y)
        {
            return (x <= y);
        }

        public static SqlBoolean NotEquals(SqlBinary x, SqlBinary y)
        {
            return (x != y);
        }

        public SqlGuid ToSqlGuid()
        {
            return (SqlGuid) this;
        }

        public override string ToString()
        {
            if (!this.notNull)
            {
                return "Null";
            }
            return "SqlBinary(" + this.value.Length + ")";
        }

        #endregion

        #region Operators

        public static SqlBinary operator +(SqlBinary x, SqlBinary y)
        {
            var b = new byte[x.Value.Length + y.Value.Length];
            int j = 0;
            int i;

            for (i = 0; i < x.Value.Length; i++)
            {
                b[i] = x.Value[i];
            }


            for (; i < (x.Value.Length + y.Value.Length); i++)
            {
                b[i] = y.Value[j];
                j++;
            }

            return new SqlBinary(b);
        }

        public static SqlBoolean operator ==(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(Compare(x, y) == 0);
            }
        }

        public static SqlBoolean operator >(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }

            return new SqlBoolean(Compare(x, y) > 0);
        }

        public static SqlBoolean operator >=(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }

            return new SqlBoolean(Compare(x, y) >= 0);
        }

        public static SqlBoolean operator !=(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(Compare(x, y) != 0);
            }
        }

        public static SqlBoolean operator <(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }

            return new SqlBoolean(Compare(x, y) < 0);
        }

        public static SqlBoolean operator <=(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }

            return new SqlBoolean(Compare(x, y) <= 0);
        }

        public static explicit operator byte[](SqlBinary x)
        {
            return x.Value;
        }

        public static explicit operator SqlBinary(SqlGuid x)
        {
            return new SqlBinary(x.ToByteArray());
        }

        public static implicit operator SqlBinary(byte[] x)
        {
            return new SqlBinary(x);
        }

        #endregion

        // Helper method to Compare methods and operators.
        // Returns 0 if x == y
        //         1 if x > y
        //        -1 if x < y
        private static int Compare(SqlBinary x, SqlBinary y)
        {
            int LengthDiff = 0;

            // If they are different size test are bytes something else than 0
            if (x.Value.Length != y.Value.Length)
            {
                LengthDiff = x.Value.Length - y.Value.Length;

                // If more than zero, x is longer
                if (LengthDiff > 0)
                {
                    for (int i = x.Value.Length - 1; i > x.Value.Length - LengthDiff; i--)
                    {
                        // If byte is more than zero the x is bigger
                        if (x.Value[i] != 0)
                        {
                            return 1;
                        }
                    }
                }
                else
                {
                    for (int i = y.Value.Length - 1; i > y.Value.Length - LengthDiff; i--)
                    {
                        // If byte is more than zero then y is bigger
                        if (y.Value[i] != 0)
                        {
                            return -1;
                        }
                    }
                }
            }

            // choose shorter
            int lenght = (LengthDiff > 0) ? y.Value.Length : x.Value.Length;

            for (int i = lenght - 1; i > 0; i--)
            {
                byte X = x.Value[i];
                byte Y = y.Value[i];

                if (X > Y)
                {
                    return 1;
                }
                else if (X < Y)
                {
                    return -1;
                }
            }

            // If we are here, x and y were same size
            return 0;
        }
    }
}
