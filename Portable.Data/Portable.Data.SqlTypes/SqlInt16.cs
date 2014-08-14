//
// System.Data.SqlTypes.SqlInt16
//
// Author:
//   Tim Coleman <tim@timcoleman.com>
//   Ville Palo <vi64pa@koti.soon.fi>
//
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
    public struct SqlInt16 : INullable, IComparable
    {
        #region Fields

        private readonly short value;
        private readonly bool notNull;

        public static readonly SqlInt16 MaxValue = new SqlInt16(32767);
        public static readonly SqlInt16 MinValue = new SqlInt16(-32768);
        public static readonly SqlInt16 Null;
        public static readonly SqlInt16 Zero = new SqlInt16(0);

        #endregion

        #region Constructors

        public SqlInt16(short value)
        {
            this.value = value;
            this.notNull = true;
            ;
        }

        #endregion

        #region Properties

        public bool IsNull
        {
            get { return !this.notNull; }
        }

        public short Value
        {
            get
            {
                if (this.IsNull)
                {
                    throw new SqlNullValueException();
                }
                else
                {
                    return this.value;
                }
            }
        }

        #endregion

        #region Methods

        public static SqlInt16 Add(SqlInt16 x, SqlInt16 y)
        {
            return (x + y);
        }

        public static SqlInt16 BitwiseAnd(SqlInt16 x, SqlInt16 y)
        {
            return (x & y);
        }

        public static SqlInt16 BitwiseOr(SqlInt16 x, SqlInt16 y)
        {
            return (x | y);
        }

        public int CompareTo(object value)
        {
            if (value == null)
            {
                return 1;
            }
            else if (!(value is SqlInt16))
            {
                throw new ArgumentException(Locale.GetText("Value is not a System.Data.SqlTypes.SqlInt16"));
            }
            return this.CompareSqlInt16((SqlInt16) value);
        }

        public int CompareTo(SqlInt16 value)
        {
            return this.CompareSqlInt16(value);
        }

        private int CompareSqlInt16(SqlInt16 value)
        {
            if ((value).IsNull)
            {
                return 1;
            }
            else
            {
                return this.value.CompareTo(value.Value);
            }
        }

        public static SqlInt16 Divide(SqlInt16 x, SqlInt16 y)
        {
            return (x/y);
        }

        public override bool Equals(object value)
        {
            if (!(value is SqlInt16))
            {
                return false;
            }
            else if (this.IsNull)
            {
                return ((SqlInt16) value).IsNull;
            }
            else if (((SqlInt16) value).IsNull)
            {
                return false;
            }
            else
            {
                return (bool) (this == (SqlInt16) value);
            }
        }

        public static SqlBoolean Equals(SqlInt16 x, SqlInt16 y)
        {
            return (x == y);
        }

        public override int GetHashCode()
        {
            return this.value;
        }

        public static SqlBoolean GreaterThan(SqlInt16 x, SqlInt16 y)
        {
            return (x > y);
        }

        public static SqlBoolean GreaterThanOrEqual(SqlInt16 x, SqlInt16 y)
        {
            return (x >= y);
        }

        public static SqlBoolean LessThan(SqlInt16 x, SqlInt16 y)
        {
            return (x < y);
        }

        public static SqlBoolean LessThanOrEqual(SqlInt16 x, SqlInt16 y)
        {
            return (x <= y);
        }

        public static SqlInt16 Mod(SqlInt16 x, SqlInt16 y)
        {
            return (x%y);
        }

        public static SqlInt16 Modulus(SqlInt16 x, SqlInt16 y)
        {
            return (x%y);
        }

        public static SqlInt16 Multiply(SqlInt16 x, SqlInt16 y)
        {
            return (x*y);
        }

        public static SqlBoolean NotEquals(SqlInt16 x, SqlInt16 y)
        {
            return (x != y);
        }

        public static SqlInt16 OnesComplement(SqlInt16 x)
        {
            if (x.IsNull)
            {
                return Null;
            }

            return ~x;
        }

        public static SqlInt16 Parse(string s)
        {
            checked
            {
                return new SqlInt16(Int16.Parse(s));
            }
        }

        public static SqlInt16 Subtract(SqlInt16 x, SqlInt16 y)
        {
            return (x - y);
        }

        public SqlBoolean ToSqlBoolean()
        {
            return ((SqlBoolean) this);
        }

        public SqlByte ToSqlByte()
        {
            return ((SqlByte) this);
        }

        public SqlDecimal ToSqlDecimal()
        {
            return (this);
        }

        public SqlDouble ToSqlDouble()
        {
            return (this);
        }

        public SqlInt32 ToSqlInt32()
        {
            return (this);
        }

        public SqlInt64 ToSqlInt64()
        {
            return (this);
        }

        public SqlMoney ToSqlMoney()
        {
            return (this);
        }

        public SqlSingle ToSqlSingle()
        {
            return (this);
        }

        public SqlString ToSqlString()
        {
            return ((SqlString) this);
        }

        public override string ToString()
        {
            if (this.IsNull)
            {
                return "Null";
            }
            else
            {
                return this.value.ToString();
            }
        }

        public static SqlInt16 Xor(SqlInt16 x, SqlInt16 y)
        {
            return (x ^ y);
        }

        public static SqlInt16 operator +(SqlInt16 x, SqlInt16 y)
        {
            checked
            {
                return new SqlInt16((short) (x.Value + y.Value));
            }
        }

        public static SqlInt16 operator &(SqlInt16 x, SqlInt16 y)
        {
            return new SqlInt16((short) (x.value & y.Value));
        }

        public static SqlInt16 operator |(SqlInt16 x, SqlInt16 y)
        {
            return new SqlInt16((short) (x.Value | y.Value));
        }

        public static SqlInt16 operator /(SqlInt16 x, SqlInt16 y)
        {
            checked
            {
                return new SqlInt16((short) (x.Value/y.Value));
            }
        }

        public static SqlBoolean operator ==(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(x.Value == y.Value);
            }
        }

        public static SqlInt16 operator ^(SqlInt16 x, SqlInt16 y)
        {
            return new SqlInt16((short) (x.Value ^ y.Value));
        }

        public static SqlBoolean operator >(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(x.Value > y.Value);
            }
        }

        public static SqlBoolean operator >=(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(x.Value >= y.Value);
            }
        }

        public static SqlBoolean operator !=(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(!(x.Value == y.Value));
            }
        }

        public static SqlBoolean operator <(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(x.Value < y.Value);
            }
        }

        public static SqlBoolean operator <=(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
            {
                return SqlBoolean.Null;
            }
            else
            {
                return new SqlBoolean(x.Value <= y.Value);
            }
        }

        public static SqlInt16 operator %(SqlInt16 x, SqlInt16 y)
        {
            return new SqlInt16((short) (x.Value%y.Value));
        }

        public static SqlInt16 operator *(SqlInt16 x, SqlInt16 y)
        {
            checked
            {
                return new SqlInt16((short) (x.Value*y.Value));
            }
        }

        public static SqlInt16 operator ~(SqlInt16 x)
        {
            if (x.IsNull)
            {
                return Null;
            }

            return new SqlInt16((short) (~x.Value));
        }

        public static SqlInt16 operator -(SqlInt16 x, SqlInt16 y)
        {
            checked
            {
                return new SqlInt16((short) (x.Value - y.Value));
            }
        }

        public static SqlInt16 operator -(SqlInt16 x)
        {
            checked
            {
                return new SqlInt16((short) (-x.Value));
            }
        }

        public static explicit operator SqlInt16(SqlBoolean x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlInt16(x.ByteValue);
            }
        }

        public static explicit operator SqlInt16(SqlDecimal x)
        {
            checked
            {
                if (x.IsNull)
                {
                    return Null;
                }
                else
                {
                    return new SqlInt16((short) x.Value);
                }
            }
        }

        public static explicit operator SqlInt16(SqlDouble x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlInt16(checked ((short) x.Value));
            }
        }

        public static explicit operator short(SqlInt16 x)
        {
            return x.Value;
        }

        public static explicit operator SqlInt16(SqlInt32 x)
        {
            checked
            {
                if (x.IsNull)
                {
                    return Null;
                }
                else
                {
                    return new SqlInt16((short) x.Value);
                }
            }
        }

        public static explicit operator SqlInt16(SqlInt64 x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                checked
                {
                    return new SqlInt16((short) x.Value);
                }
            }
        }

        public static explicit operator SqlInt16(SqlMoney x)
        {
            checked
            {
                if (x.IsNull)
                {
                    return Null;
                }
                else
                {
                    return new SqlInt16((short) Math.Round(x.Value));
                }
            }
        }


        public static explicit operator SqlInt16(SqlSingle x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                checked
                {
                    return new SqlInt16((short) x.Value);
                }
            }
        }

        public static explicit operator SqlInt16(SqlString x)
        {
            if (x.IsNull)
            {
                return Null;
            }

            return Parse(x.Value);
        }

        public static implicit operator SqlInt16(short x)
        {
            return new SqlInt16(x);
        }

        public static implicit operator SqlInt16(SqlByte x)
        {
            return new SqlInt16(x.Value);
        }

        #endregion
    }
}
