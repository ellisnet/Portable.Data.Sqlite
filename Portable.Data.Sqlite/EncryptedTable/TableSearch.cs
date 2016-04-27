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

namespace Portable.Data.Sqlite {

    /// <summary>
    /// The type of search to perform on encrypted table object properties
    /// </summary>
    public enum TableSearchType {
        /// <summary>
        /// All property values of the specified object must match the search criteria - equivalent to AND in SQL
        /// </summary>
        MatchAll = 0,
        /// <summary>
        /// One or more property values of the specified object must match the search criteria - equivalent to OR in SQL
        /// </summary>
        MatchAny
    }

    /// <summary>
    /// The definition for a search of encrypted table objects based on their properties
    /// </summary>
    public class TableSearch {

        List<TableSearchItem> _matchItems = new List<TableSearchItem>();
        TableSearchType _searchType = TableSearchType.MatchAll;

        SearchItemCaseSensitivity _forcedItemCaseSensitivity = SearchItemCaseSensitivity.CaseInsensitive;
        bool _caseSensitivityForced = false;
        SearchItemTrimming _forcedItemTrimming = SearchItemTrimming.AutoTrim;
        bool _trimmingForced = false;

        /// <summary>
        /// Set the (forced) Case Sensitivity of all table search match items
        /// </summary>
        public SearchItemCaseSensitivity ForcedItemCaseSensitivity {
            set { _forcedItemCaseSensitivity = value; _caseSensitivityForced = true;  }
        }

        /// <summary>
        /// Set the (forced) Trimming of all table search match items
        /// </summary>
        public SearchItemTrimming ForcedItemTrimming {
            set { _forcedItemTrimming = value; _trimmingForced = true; }
        }

        /// <summary>
        /// List of properties and values to be used in identifying matching objects - IMPORTANT: If this list contains no members, all compared objects will match
        /// </summary>
        public List<TableSearchItem> MatchItems {
            get {
                if (_caseSensitivityForced) {
                    foreach (var item in _matchItems) {
                        item.CaseSensitivity = _forcedItemCaseSensitivity;
                    }
                }
                if (_trimmingForced) {
                    foreach (var item in _matchItems) {
                        item.Trimming = _forcedItemTrimming;
                    }
                }
                return _matchItems; 
            }
            set {
                if (value == null) {
                    _matchItems = new List<TableSearchItem>();
                }
                else {
                    _matchItems = value;
                    if (_caseSensitivityForced) {
                        foreach (var item in _matchItems) {
                            item.CaseSensitivity = _forcedItemCaseSensitivity;
                        }
                    }
                    if (_trimmingForced) {
                        foreach (var item in _matchItems) {
                            item.Trimming = _forcedItemTrimming;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The type of search to be performed, i.e. all items must match vs. one or more items must match
        /// </summary>
        public TableSearchType SearchType {
            get { return _searchType; }
            set { _searchType = value; }
        }

    }
}
