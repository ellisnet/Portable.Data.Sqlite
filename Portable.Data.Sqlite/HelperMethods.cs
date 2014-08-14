using System;
using System.Linq;
using System.Reflection;

//Was: namespace Mono.Data.Sqlite {
namespace Portable.Data.Sqlite {

    /// <summary>
    /// Static class with methods for getting type information
    /// </summary>
    public static class HelperMethods {
//#if NETFX_CORE
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
        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Determines the underlying CLR type of the specified type
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <returns>The underlying CLR type</returns>
        public static Type GetUnderlyingSystemType(this Type type)
        {
            return type;
        }
//#else
//        public static bool IsEnum(this Type type) {
//            return type.IsEnum;
//        }
//        public static Type GetUnderlyingSystemType(this Type type) {
//            return type.UnderlyingSystemType;
//        }
//#endif
    }
}
