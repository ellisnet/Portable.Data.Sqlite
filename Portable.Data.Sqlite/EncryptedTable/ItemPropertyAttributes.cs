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

namespace Portable.Data.Sqlite {

    /// <summary>
    /// Specifies that the property should be stored in an unencrypted table column - IMPORTANT: Values of the marked property will NOT BE ENCRYPTED in the table
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotEncryptedAttribute : Attribute {
    }

    /// <summary>
    /// Specifies that the column used to store the property value in the table should be marked NOT NULL - NOTE: Only applies to NotEncrypted properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotNullAttribute : Attribute {
    }

    /// <summary>
    /// Specifies that the property WILL BE ENCRYPTED in the table, but will also be searchable and contained in in-memory indexes of the table
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SearchableAttribute : Attribute {
    }

    /// <summary>
    /// Specifies the SQLite default column value, if the property value is NULL - NOTE: Only applies to NotEncrypted properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnDefaultValueAttribute : Attribute {

        private string _value;

        /// <summary>
        /// The default value for the column, if a column value is not specified during record creation
        /// </summary>
        public string Value {
            get { return _value; }
        }

        /// <summary>
        /// Creates an instance of the attribute with the specified value
        /// </summary>
        /// <param name="value">The value to use as default</param>
        public ColumnDefaultValueAttribute(string value) {
            if (value == null) throw new ArgumentNullException(nameof(value), "Column default value cannot be null.");
            if (value.Contains("'")) throw new ArgumentException("Column default values cannot contain single quotes.", nameof(value));
            _value = value;
        }
    }

    /// <summary>
    /// Specifies a desired name for the SQLite table column, rather than using the default column name derived from the property name - NOTE: Only applies to NotEncrypted properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute {

        private string _name;

        /// <summary>
        /// The desired name of the column
        /// </summary>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// Creates an instance of the attribute with the specified name
        /// </summary>
        /// <param name="name"></param>
        public ColumnNameAttribute(string name) {
            string tempName = name;
            var checkResult = TableColumn.CheckColumnName(ref tempName);
            if (!checkResult.Item1)
                throw new Exception(String.Format("Problem with column name '{0}' - {1}", name, checkResult.Item2 ?? "Unknown"));
            _name = tempName;
        }
    }

}
