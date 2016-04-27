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
    /// The definition of an in-memory index of an encrypted table
    /// </summary>
    public class TableIndex {

        private Dictionary<long, Dictionary<string, string>> _index = new Dictionary<long, Dictionary<string, string>>();
        private DateTime _timestamp = DateTime.MinValue;
        private int _lifetimeSeconds = 600;

        /// <summary>
        /// The in-memory index of all, or a subset, of the items in an encrypted table
        /// </summary>
        public Dictionary<long, Dictionary<string, string>> Index {
            get { return _index; }
            set { _index = value ?? new Dictionary<long, Dictionary<string, string>>(); }
        }

        /// <summary>
        /// The timestamp of the index, when it was last generated and/or sync'ed with table
        /// </summary>
        public DateTime Timestamp {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// The time-to-live, in seconds, of the index; if this number of seconds have elapsed since the Timestamp value, it is considered 'expired'
        /// </summary>
        public int LifetimeSeconds {
            get { return _lifetimeSeconds; }
            set { _lifetimeSeconds = (value < 0) ? 0 : value; }
        }

        /// <summary>
        /// Returns a copy/clone of the index
        /// </summary>
        /// <returns>A copy/clone of the index</returns>
        public TableIndex Clone() {
            var result = new TableIndex {
                Timestamp = _timestamp,
                LifetimeSeconds = _lifetimeSeconds
            };
            long itemId;
            Dictionary<string, string> itemIndex;
            foreach (var item in _index) {
                itemId = item.Key;
                if (item.Value == null) {
                    itemIndex = null;
                }
                else {
                    itemIndex = new Dictionary<string, string>();
                    foreach (var dicItem in item.Value) {
                        itemIndex.Add(dicItem.Key, dicItem.Value);
                    }
                }
                result.Index.Add(itemId, itemIndex);
            }
            return result;
        }
    }
}
