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
using System.Text;
using System.Threading.Tasks;

namespace Portable.Data.Sqlite {

    /// <summary>
    /// The type of search to perform on encrypted table object properties
    /// </summary>
    public enum TableSearchType {
        /// <summary>
        /// All property values of the specified object must match the search criteria
        /// </summary>
        And = 0,
        /// <summary>
        /// One or more property values of the specified object must match the search criteria
        /// </summary>
        Or
    }

    /// <summary>
    /// The definition for a search of encrypted table objects based on their properties
    /// </summary>
    public class TableSearch {

        List<TableSearchItem> _matchItems = new List<TableSearchItem>();
        TableSearchType _searchType = TableSearchType.And;

        /// <summary>
        /// List of properties and values to be used in identifying matching objects - IMPORTANT: If this list contains no members, all compared objects will match
        /// </summary>
        public List<TableSearchItem> MatchItems {
            get { return _matchItems; }
            set { _matchItems = value ?? new List<TableSearchItem>(); }
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
