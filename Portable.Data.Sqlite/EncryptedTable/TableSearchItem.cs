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
    /// The type of matching to perform when searching for encrypted table items
    /// </summary>
    public enum SearchItemMatchType {
        /// <summary>
        /// Match if item property (ToString()) is equal to the specified text value
        /// </summary>
        IsEqualTo = 0,
        /// <summary>
        /// Match if item property (ToString()) is not equal to the specified value
        /// </summary>
        IsNotEqualTo,
        /// <summary>
        /// Match if the item property (ToString()) contains the specified (text) value
        /// </summary>
        Contains,
        /// <summary>
        /// Match if the item property (ToString()) does not contain the specified (text) value
        /// </summary>
        DoesNotContain,
        /// <summary>
        /// Match if the item property is NULL
        /// </summary>
        IsNull,
        /// <summary>
        /// Match if the item property is not NULL
        /// </summary>
        IsNotNull
    }

    /// <summary>
    /// When matching string values, specifies case sensitivity of string matching
    /// </summary>
    public enum SearchItemCaseSensitivity {
        /// <summary>
        /// Matching of string values is not case sensitive
        /// </summary>
        CaseInsensitive = 0,
        /// <summary>
        /// Matching of string values is case sensitive
        /// </summary>
        CaseSensitive
    }

    /// <summary>
    /// When matching string values, specifies whether strings should be trimmed before comparison
    /// </summary>
    public enum SearchItemTrimming {
        /// <summary>
        /// Trim all string values before comparison
        /// </summary>
        AutoTrim = 0,
        /// <summary>
        /// Do not trim string values before comparison
        /// </summary>
        None
    }

    /// <summary>
    /// Specifies the type of property (Searchable vs. NotEncrypted) being evaluated for a match
    /// </summary>
    public enum SearchItemPropertyType {
        /// <summary>
        /// The property type is not yet known
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The property type is marked as 'Searchable' (and encrypted in the table)
        /// </summary>
        Searchable,
        /// <summary>
        /// The property type is NotEncrypted (in the table)
        /// </summary>
        NotEncrypted
    }

    /// <summary>
    /// A match item to be used when comparing encrypted table object properties to specified values
    /// </summary>
    public class TableSearchItem {

        private SearchItemMatchType _matchType = SearchItemMatchType.IsEqualTo;
        private SearchItemCaseSensitivity _caseSensitivity = SearchItemCaseSensitivity.CaseInsensitive;
        private SearchItemTrimming _trimming = SearchItemTrimming.AutoTrim;
        private string _propertyName = null;
        private string _fixedPropertyName = null;
        private string _value = "";
        private SearchItemPropertyType _propertyType = SearchItemPropertyType.Unknown;
        private string _columnName = null;

        /// <summary>
        /// Type of match to use when comparing properties to the specified value
        /// </summary>
        public SearchItemMatchType MatchType {
            get { return _matchType; }
            set { _matchType = value; }
        }

        /// <summary>
        /// When matching string values, identifies the case sensitivity of the match
        /// </summary>
        public SearchItemCaseSensitivity CaseSensitivity {
            get { return _caseSensitivity; }
            set { _caseSensitivity = value; }
        }

        /// <summary>
        /// When matching string values, identifies whether values should be trimmed before comparison
        /// </summary>
        public SearchItemTrimming Trimming {
            get { return _trimming; }
            set { _trimming = value; }
        }

        /// <summary>
        /// The name of the object property to compare with the specified value
        /// </summary>
        public string PropertyName {
            get { return _propertyName; }
            set {
                if (String.IsNullOrWhiteSpace(value)) throw new Exception("The Property Name cannot be null or empty.");
                _propertyName = value.Trim();
            }
        }

        /// <summary>
        /// The verified name of the property to compare, e.g. in case the PropertyName is the wrong case
        /// </summary>
        internal string FixedPropertyName {
            get { return _fixedPropertyName; }
            set { _fixedPropertyName = value; }
        }

        /// <summary>
        /// The string value to compare to the object's property (ToString())
        /// </summary>
        public string Value {
            get { return _value; }
            set { _value = value ?? ""; }
        }

        /// <summary>
        /// The type of property - Searchable = an encrypted field, NotEncrypted = a non-encrypted field/column
        /// </summary>
        public SearchItemPropertyType PropertyType {
            get { return _propertyType; }
            internal set { _propertyType = value; }
        }

        /// <summary>
        /// The name of the column associated with the property, for NotEncrypted properties
        /// </summary>
        public string ColumnName {
            get { return _columnName; }
            set { _columnName = (String.IsNullOrWhiteSpace(value) ? null : value.Trim()); }
        }

        /// <summary>
        /// Creates a new instance of TableSearchItem with the specified values
        /// </summary>
        /// <param name="propertyName">The name of the property to be used for comparison/matching</param>
        /// <param name="value">The value to compare the property value to</param>
        /// <param name="matchType">The type of comparison to perform between the property value and the specified value</param>
        public TableSearchItem(string propertyName, string value, SearchItemMatchType matchType = SearchItemMatchType.IsEqualTo) {
            if (String.IsNullOrWhiteSpace(propertyName)) throw new Exception("The Property Name cannot be null or empty.");
            _propertyName = propertyName.Trim();
            _value = value ?? "";
            _matchType = matchType;
        }

    }

}
