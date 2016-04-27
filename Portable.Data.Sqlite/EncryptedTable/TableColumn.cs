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
    /// Encrypted table column information
    /// </summary>
    public class TableColumn {

        private string _columnName = null;
        private string _propertyName = null;
        private byte _columnOrder = 10;
        private string _netType = "System.String";
        private string _dbType = "TEXT";
        private Object _defaultValue = null;
        private bool _isNotNull = false;

        /// <summary>
        /// The name of the encrypted table column
        /// </summary>
        public string ColumnName {
            get { return _columnName; }
            set { _columnName = value; }
        }

        /// <summary>
        /// The property of the encrypted table object associated with the column
        /// </summary>
        public string PropertyName {
            get { return _propertyName; }
            set { 
                _propertyName = value;
                string tempName = value;
                if (_columnName == null && CheckColumnName(ref tempName).Item1)
                    _columnName = value;
            
            }
        }

        /// <summary>
        /// The numerical order of the column
        /// </summary>
        public byte ColumnOrder {
            get { return _columnOrder; }
            set { _columnOrder = value; }
        }

        /// <summary>
        /// The CLR data type of the data in the column
        /// </summary>
        public string NetType {
            get { return _netType; }
            set { _netType = value; }
        }

        /// <summary>
        /// The Sqlite data type of the data in the column
        /// </summary>
        public string DbType {
            get { return _dbType; }
            set { _dbType = value; }
        }

        /// <summary>
        /// The default value for data in this column
        /// </summary>
        public Object DefaultValue {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        /// <summary>
        /// Identifies whether the column is (or should be) marked NOT NULL
        /// </summary>
        public bool IsNotNull {
            get { return _isNotNull; }
            set { _isNotNull = value; }
        }

        /// <summary>
        /// Identifies whether the specified column name is valid or not
        /// </summary>
        /// <param name="name">The proposed column name to check</param>
        /// <returns>If Item1 is true, column name is valid; if not, Item2 contains a string explanation</returns>
        public static Tuple<bool, string> CheckColumnName(ref string name) {
            Tuple<bool, string> result = Tuple.Create(false, "Unidentified problem.");

            do {
                if (name == null) {
                    result = Tuple.Create(false, "Column names cannot be null.");
                    break;
                }

                if (name.Trim() == "") {
                    result = Tuple.Create(false, "Column names cannot be empty.");
                    break;
                }

                if (name.Trim() != name) {
                    result = Tuple.Create(false, "Column names cannot have leading or trailing spaces.");
                    break;
                }

                name = name.Replace(" ", "_").Replace(".", "_");

                if (!("abcdefghijklmnopqrstuvwxyz").Contains(name.Substring(0, 1).ToLower())) {
                    result = Tuple.Create(false, "Column names must start with a letter.");
                    break;
                }

                bool invalidCharFound = false;
                foreach (char letter in name.ToLower().ToCharArray()) {
                    if (("abcdefghijklmnopqrstuvwxyz0123456789_").IndexOf(letter) < 0) {
                        result = Tuple.Create(false, "Column names may only contain letters, numbers and underscores.");
                        invalidCharFound = true;
                        break;
                    }
                }
                if (invalidCharFound) break;

                result = Tuple.Create<bool, string>(true, null);
                
            } while (false);

            return result;
        }

    }
}
