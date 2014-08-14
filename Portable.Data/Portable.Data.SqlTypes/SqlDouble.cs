//
// System.Data.SqlTypes.SqlDouble
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
    public struct SqlDouble : INullable, IComparable
    {
        #region Fields

        private readonly double value;
        private readonly bool notNull;

        public static readonly SqlDouble MaxValue = new SqlDouble(1.7976931348623157E+308);
        public static readonly SqlDouble MinValue = new SqlDouble(-1.7976931348623157E+308);
        public static readonly SqlDouble Null;
        public static readonly SqlDouble Zero = new SqlDouble(0);

        #endregion

        #region Constructors

        public SqlDouble(double value)
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

        public double Value
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

        public static SqlDouble Add(SqlDouble x, SqlDouble y)
        {
            return (x + y);
        }

        public int CompareTo(object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is SqlDouble))
            {
                throw new ArgumentException(Locale.GetText("Value is not a System.Data.SqlTypes.SqlDouble"));
            }

            return this.CompareTo((SqlDouble) value);
        }

        public int CompareTo(SqlDouble value)
        {
            if (value.IsNull)
            {
                return 1;
            }
            else
            {
                return this.value.CompareTo(value.Value);
            }
        }

        public static SqlDouble Divide(SqlDouble x, SqlDouble y)
        {
            return (x/y);
        }

        public override bool Equals(object value)
        {
            if (!(value is SqlDouble))
            {
                return false;
            }
            if (this.IsNull)
            {
                return ((SqlDouble) value).IsNull;
            }
            else if (((SqlDouble) value).IsNull)
            {
                return false;
            }
            else
            {
                return (bool) (this == (SqlDouble) value);
            }
        }

        public static SqlBoolean Equals(SqlDouble x, SqlDouble y)
        {
            return (x == y);
        }

        public override int GetHashCode()
        {
            var LongValue = (long) this.value;
            return (int) (LongValue ^ (LongValue >> 32));
        }

        public static SqlBoolean GreaterThan(SqlDouble x, SqlDouble y)
        {
            return (x > y);
        }

        public static SqlBoolean GreaterThanOrEqual(SqlDouble x, SqlDouble y)
        {
            return (x >= y);
        }

        public static SqlBoolean LessThan(SqlDouble x, SqlDouble y)
        {
            return (x < y);
        }

        public static SqlBoolean LessThanOrEqual(SqlDouble x, SqlDouble y)
        {
            return (x <= y);
        }

        public static SqlDouble Multiply(SqlDouble x, SqlDouble y)
        {
            return (x*y);
        }

        public static SqlBoolean NotEquals(SqlDouble x, SqlDouble y)
        {
            return (x != y);
        }

        public static SqlDouble Parse(string s)
        {
            return new SqlDouble(Double.Parse(s));
        }

        public static SqlDouble Subtract(SqlDouble x, SqlDouble y)
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
            return ((SqlDecimal) this);
        }

        public SqlInt16 ToSqlInt16()
        {
            return ((SqlInt16) this);
        }

        public SqlInt32 ToSqlInt32()
        {
            return ((SqlInt32) this);
        }

        public SqlInt64 ToSqlInt64()
        {
            return ((SqlInt64) this);
        }

        public SqlMoney ToSqlMoney()
        {
            return ((SqlMoney) this);
        }

        public SqlSingle ToSqlSingle()
        {
            return ((SqlSingle) this);
        }

        public SqlString ToSqlString()
        {
            return ((SqlString) this);
        }

        public override string ToString()
        {
            if (!this.notNull)
            {
                return "Null";
            }
            else
            {
                return this.value.ToString();
            }
        }

        public static SqlDouble operator +(SqlDouble x, SqlDouble y)
        {
            double d = 0;
            d = x.Value + y.Value;

            if (Double.IsInfinity(d))
            {
                throw new OverflowException();
            }

            return new SqlDouble(d);
        }

        public static SqlDouble operator /(SqlDouble x, SqlDouble y)
        {
            double d = x.Value/y.Value;

            if (Double.IsInfinity(d))
            {
                if (y.Value == 0)
                {
                    throw new DivideByZeroException();
                }
            }

            return new SqlDouble(d);
        }

        public static SqlBoolean operator ==(SqlDouble x, SqlDouble y)
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

        public static SqlBoolean operator >(SqlDouble x, SqlDouble y)
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

        public static SqlBoolean operator >=(SqlDouble x, SqlDouble y)
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

        public static SqlBoolean operator !=(SqlDouble x, SqlDouble y)
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

        public static SqlBoolean operator <(SqlDouble x, SqlDouble y)
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

        public static SqlBoolean operator <=(SqlDouble x, SqlDouble y)
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

        public static SqlDouble operator *(SqlDouble x, SqlDouble y)
        {
            double d = x.Value*y.Value;

            if (Double.IsInfinity(d))
            {
                throw new OverflowException();
            }

            return new SqlDouble(d);
        }

        public static SqlDouble operator -(SqlDouble x, SqlDouble y)
        {
            double d = x.Value - y.Value;

            if (Double.IsInfinity(d))
            {
                throw new OverflowException();
            }

            return new SqlDouble(d);
        }

        public static SqlDouble operator -(SqlDouble x)
        {
            return new SqlDouble(-(x.Value));
        }

        public static explicit operator SqlDouble(SqlBoolean x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.ByteValue);
            }
        }

        public static explicit operator double(SqlDouble x)
        {
            return x.Value;
        }

        public static explicit operator SqlDouble(SqlString x)
        {
            checked
            {
                return Parse(x.Value);
            }
        }

        public static implicit operator SqlDouble(double x)
        {
            return new SqlDouble(x);
        }

        public static implicit operator SqlDouble(SqlByte x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.Value);
            }
        }

        public static implicit operator SqlDouble(SqlDecimal x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.ToDouble());
            }
        }

        public static implicit operator SqlDouble(SqlInt16 x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.Value);
            }
        }

        public static implicit operator SqlDouble(SqlInt32 x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.Value);
            }
        }

        public static implicit operator SqlDouble(SqlInt64 x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.Value);
            }
        }

        public static implicit operator SqlDouble(SqlMoney x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble((double) x.Value);
            }
        }

        public static implicit operator SqlDouble(SqlSingle x)
        {
            if (x.IsNull)
            {
                return Null;
            }
            else
            {
                return new SqlDouble(x.Value);
            }
        }

        #endregion
    }
}
