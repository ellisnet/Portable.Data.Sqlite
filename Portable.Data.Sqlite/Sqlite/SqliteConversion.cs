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
using System.Linq;
using System.Reflection;

namespace Portable.Data.Sqlite {

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

    internal static class SqliteConversion {

#if PORTABLE || NETFX_CORE

        /// <summary>
        /// Get the custom attributes of the specified type
        /// </summary>
        /// <param name="type">The type to check for attributes</param>
        /// <param name="attributeType">The type of attribute to look for</param>
        /// <param name="inherit">Get attributes that are inherited?</param>
        /// <returns>Array of attributes</returns>
        public static object[] GetCustomAttributes(this Type type, Type attributeType, bool inherit) {
            return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit).ToArray();
        }

        /// <summary>
        /// Get the properties of the specified type
        /// </summary>
        /// <param name="type">The type to check for properties</param>
        /// <returns>Array of properties</returns>
        public static PropertyInfo[] GetProperties(this Type type) {
            return type.GetTypeInfo().DeclaredProperties.ToArray();
        }

        /// <summary>
        /// Determines if specified type is an Enum
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>If true, specified type is an Enum</returns>
        public static bool IsEnum(this Type type) {
            return type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Determines the underlying CLR type of the specified type
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <returns>The underlying CLR type</returns>
        public static Type GetUnderlyingSystemType(this Type type) {
            return type;
        }

#else
        public static bool IsEnum(this Type type) {
            return type.IsEnum;
        }
        public static Type GetUnderlyingSystemType(this Type type) {
            return type.UnderlyingSystemType;
        }
#endif

        /// <summary>
        /// For a given type, return the closest-match SQLite column type, which only understands a very limited subset of types.
        /// </summary>
        /// <param name="typ">The type to evaluate</param>
        /// <returns>The SQLite type column type for that type.</returns>
        internal static SqliteColumnType TypeToColumnType(Type typ) {
            var tc = NetTypes.GetNetType(typ);
            if (tc == NetType.Object) {
                if (typ == typeof(byte[]) || typ == typeof(Guid))
                    return SqliteColumnType.Blob;
                else
                    return SqliteColumnType.Text;
            }
            return _typecodeColumnTypes[(int)tc];
        }

        private static SqliteColumnType[] _typecodeColumnTypes = {
            SqliteColumnType.Null,
            SqliteColumnType.Blob,
            SqliteColumnType.Null,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer, // 7
            SqliteColumnType.Integer,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer,
            SqliteColumnType.Integer, // 11
            SqliteColumnType.Integer,
            SqliteColumnType.Double,
            SqliteColumnType.Double,
            SqliteColumnType.Double,
            SqliteColumnType.ConvDateTime,
            SqliteColumnType.Null,
            SqliteColumnType.Text,
        };

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
}
