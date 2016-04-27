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

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable ArrangeThisQualifier

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Portable.Data.Sqlite {

    /// <summary>
    /// Base class for an object to be stored in an encrypted SQLite table
    /// </summary>
    public abstract class EncryptedTableItem : IEncryptedTableItem {

        #region ID (primary key)

        /// <summary>
        /// The primary key Id of the object's record in the table; new, unwritten objects have an Id of -1
        /// </summary>
        protected Int64 _id = -1;

        /// <summary>
        /// The primary key Id of the object's record in the table; new, unwritten objects have an Id of -1
        /// </summary>
        public Int64 Id {
            get { return _id; }
            set { _id = value; }
        }

        #endregion

        #region Fields/properties/methods for table record syncing
        
        /// <summary>
        /// Indicates the status of the object with respect to its table record
        /// </summary>
        protected TableItemStatus _syncStatus;

        /// <summary>
        /// Indicates the last time that the object was determined to match the table record
        /// </summary>
        protected DateTime _syncTimestamp;

        private IObjectCryptEngine _cryptEngine = null;

        /// <summary>
        /// Indicates the status of the object with respect to its table record
        /// </summary>
        public TableItemStatus SyncStatus {
            get { return _syncStatus; }
            set { _syncStatus = value; }
        }

        /// <summary>
        /// Indicates the last time that the object was determined to match the table record
        /// </summary>
        public DateTime SyncTimestamp {
            get { return _syncTimestamp; }
            set { _syncTimestamp = value; }
        }

        /// <summary>
        /// If true, the object currently does not match its table record
        /// </summary>
        public virtual bool IsDirty {
            get {
                return (_syncStatus != TableItemStatus.MatchesDb);
            }
        }

        /// <summary>
        /// The implementation of IObjectCryptEngine that will be responsible for encrypting this object
        /// </summary>
        internal IObjectCryptEngine CryptEngine {
            get {
                return _cryptEngine;
            }
            set {
                _cryptEngine = value;
            }
        }

        /// <summary>
        /// Directly set this object to not match the table record, if currently matching
        /// </summary>
        public virtual void SetChanged() {
            //May need to add more complex sync status checking logic here
            if (_syncStatus == TableItemStatus.MatchesDb)
                _syncStatus = TableItemStatus.Modified;
        }

        /// <summary>
        /// Use this to set the values of properties, so that SetChanged is called
        /// </summary>
        /// <typeparam name="T">The type of value</typeparam>
        /// <param name="value">The value to use when setting the property</param>
        /// <returns>The specified value</returns>
        protected virtual T SetChanged<T>(T value) {
            this.SetChanged();
            return value;
        }

        /// <summary>
        /// Check the proposed change of value, and call SetChanged if the new value is not the same as the old value 
        /// </summary>
        /// <typeparam name="T">The type of value</typeparam>
        /// <param name="currentValue">The current value of the property</param>
        /// <param name="newValue">The proposed new value of the property</param>
        /// <returns>The final value to assign to the property</returns>
        protected virtual T CheckChange<T>(T currentValue, T newValue) {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) {
                return currentValue;
            }
            else {
                SetChanged();
                return newValue;
            }
        }

        #endregion

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [Searchable]
        /// </summary>
        public Dictionary<string, string> SearchablePropertyIndex {
            get {
                var index = new Dictionary<string, string>();

                List<string> propNames = GetSearchablePropertyNames();

                foreach (var property in this.GetType().GetProperties()) {
                    if (propNames.Contains(property.Name)) {
                        var val = property.GetValue(this);
                        index.Add(property.Name, ((val == null) ? null : val.ToString()));
                    }
                }

                return index;
            }
        }

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [NotEncrypted]
        /// </summary>
        public Dictionary<string, string> NotEncryptedPropertyIndex {
            get {
                var index = new Dictionary<string, string>();

                List<string> propNames = GetNotEncryptedPropertyNames();

                foreach (var property in this.GetType().GetProperties()) {
                    if (propNames.Contains(property.Name)) {
                        var val = property.GetValue(this);
                        index.Add(property.Name, ((val == null) ? null : val.ToString()));
                    }
                }

                return index;
            }
        }

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [NotEncrypted] or [Searchable]
        /// </summary>
        public Dictionary<string, string> AllSearchableIndex {
            get {
                var index = new Dictionary<string, string>();

                List<string> notEncNames = GetNotEncryptedPropertyNames();
                List<string> searchNames = GetSearchablePropertyNames();

                foreach (var property in this.GetType().GetProperties()) {
                    if (notEncNames.Contains(property.Name) || searchNames.Contains(property.Name)) {
                        var val = property.GetValue(this);
                        index.Add(property.Name, ((val == null) ? null : val.ToString()));
                    }
                }

                return index;
            }
        }

        /// <summary>
        /// Retrieves a list of object properties marked [Searchable]
        /// </summary>
        /// <returns>Searchable property list</returns>
        public List<string> GetSearchablePropertyNames() {
            var names = new List<string>();
            foreach (var property in this.GetType().GetProperties()) {
                if (property.GetCustomAttributes(typeof(SearchableAttribute), true).Count() > 0)
                    names.Add(property.Name);
            }
            return names;
        }

        /// <summary>
        /// Retrieves a list of object properties marked [NotEncrypted]
        /// </summary>
        /// <returns>NotEncrypted property list</returns>
        public List<string> GetNotEncryptedPropertyNames() {
            var names = new List<string>();
            foreach (var property in this.GetType().GetProperties()) {
                if (property.GetCustomAttributes(typeof(NotEncryptedAttribute), true).Count() > 0)
                    names.Add(property.Name);
            }
            return names;
        }

        /// <summary>
        /// Retrieves the name of the object type
        /// </summary>
        /// <returns>Object type name</returns>
        public string GetTypeName() {
            return this.GetType().Name;
        }

        /// <summary>
        /// Retrieves the full name of the object type
        /// </summary>
        /// <returns>Object type full name</returns>
        public string GetTypeFullName() {
            return this.GetType().FullName;
        }

        /// <summary>
        /// Encrypts the specified object to a string
        /// </summary>
        /// <param name="objectToEncrypt">The object to be encrypted</param>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        public static string EncryptObject(IEncryptedTableItem objectToEncrypt, IObjectCryptEngine cryptEngine) {
            if (objectToEncrypt == null) throw new ArgumentNullException(nameof(objectToEncrypt));
            if (cryptEngine == null) throw new ArgumentNullException(nameof(cryptEngine));
            return cryptEngine.EncryptObject(objectToEncrypt);
        }

        /// <summary>
        /// Encrypts the Searchable index of the specified object to a string
        /// </summary>
        /// <param name="objectToEncrypt">The object whose Searchable index should be encrypted</param>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        public static string EncryptObjectSearchIndex(IEncryptedTableItem objectToEncrypt, IObjectCryptEngine cryptEngine) {
            if (objectToEncrypt == null) throw new ArgumentNullException(nameof(objectToEncrypt));
            if (cryptEngine == null) throw new ArgumentNullException(nameof(cryptEngine));
            return cryptEngine.EncryptObject(objectToEncrypt.SearchablePropertyIndex);
        }

        /// <summary>
        /// Encrypts the object to a string
        /// </summary>
        /// <returns>Encrypted string</returns>
        public string Encrypt() {
            if (_cryptEngine == null) throw new Exception("No cryptography engine has been specified.");
            return this.Encrypt(_cryptEngine);
        }

        /// <summary>
        /// Encrypts the object to a string, using the specified cryptography 'engine'
        /// </summary>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        public string Encrypt(IObjectCryptEngine cryptEngine) {
            if (cryptEngine == null) throw new ArgumentNullException(nameof(cryptEngine));
            return EncryptedTableItem.EncryptObject(this, cryptEngine);
        }

        /// <summary>
        /// Encrypts the Searchable index of the object to a string
        /// </summary>
        /// <returns>Encrypted string</returns>
        public string EncryptSearchable() {
            if (_cryptEngine == null) throw new Exception("No cryptography engine has been specified.");
            return this.EncryptSearchable(_cryptEngine);
        }

        /// <summary>
        /// Encrypts the Searchable index of the object to a string, using the specified cryptography 'engine'
        /// </summary>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        public string EncryptSearchable(IObjectCryptEngine cryptEngine) {
            if (cryptEngine == null) throw new ArgumentNullException(nameof(cryptEngine));
            return EncryptedTableItem.EncryptObjectSearchIndex(this, cryptEngine);
        }

        /// <summary>
        /// Disposes the object and frees up resources
        /// </summary>
        public void Dispose() {
            _cryptEngine = null;
        }

    }
}
