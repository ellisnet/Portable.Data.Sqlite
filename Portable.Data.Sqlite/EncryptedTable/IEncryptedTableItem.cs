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
    /// Object status with respect to the table record
    /// </summary>
    public enum TableItemStatus {
        /// <summary>
        /// The object is new and does not yet have a record in the table
        /// </summary>
        New = 0,
        /// <summary>
        /// The object is unchanged and matches the current table record
        /// </summary>
        MatchesDb,
        /// <summary>
        /// The object has been changed but the table record has not yet been updated
        /// </summary>
        Modified,
        /// <summary>
        /// The object has a record in the table, but this record will be deleted
        /// </summary>
        ToBeDeleted,
        /// <summary>
        /// The object's record has been deleted from (and is no longer present in) the table
        /// </summary>
        DeletedFromDb
    }

    /// <summary>
    /// Interface definition for objects inheriting from EncryptedTableItem
    /// </summary>
    public interface IEncryptedTableItem : IDisposable {

        /// <summary>
        /// The primary key Id of the object's record in the table; new, unwritten objects have an Id of -1
        /// </summary>
        Int64 Id { get; set; }

        /// <summary>
        /// Indicates the status of the object with respect to its table record
        /// </summary>
        TableItemStatus SyncStatus { get; set; }

        /// <summary>
        /// Indicates the last time that the object was determined to match the table record
        /// </summary>
        DateTime SyncTimestamp { get; set; }

        /// <summary>
        /// If true, the object currently does not match its table record
        /// </summary>
        bool IsDirty { get; }
        //IObjectCryptEngine CryptEngine { get; set; }

        /// <summary>
        /// Directly set this object to not match the table record, if currently matching
        /// </summary>
        void SetChanged();
        //T SetChanged<T>(T value);
        //T CheckChange<T>(T currentValue, T newValue);

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [Searchable]
        /// </summary>
        Dictionary<string, string> SearchablePropertyIndex { get; }

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [NotEncrypted]
        /// </summary>
        Dictionary<string, string> NotEncryptedPropertyIndex { get; }

        /// <summary>
        /// A dictionary of property names and (ToString()) values for object properties marked [NotEncrypted] or [Searchable]
        /// </summary>
        Dictionary<string, string> AllSearchableIndex { get; }

        /// <summary>
        /// Retrieves a list of object properties marked [Searchable]
        /// </summary>
        /// <returns>Searchable property list</returns>
        List<string> GetSearchablePropertyNames();

        /// <summary>
        /// Retrieves a list of object properties marked [NotEncrypted]
        /// </summary>
        /// <returns>NotEncrypted property list</returns>
        List<string> GetNotEncryptedPropertyNames();

        /// <summary>
        /// Retrieves the name of the object type
        /// </summary>
        /// <returns>Object type name</returns>
        string GetTypeName();

        /// <summary>
        /// Retrieves the full name of the object type
        /// </summary>
        /// <returns>Object type full name</returns>
        string GetTypeFullName();

        /// <summary>
        /// Encrypts the object to a string
        /// </summary>
        /// <returns>Encrypted string</returns>
        string Encrypt();

        /// <summary>
        /// Encrypts the object to a string, using the specified cryptography 'engine'
        /// </summary>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        string Encrypt(IObjectCryptEngine cryptEngine);

        /// <summary>
        /// Encrypts the Searchable index of the object to a string
        /// </summary>
        /// <returns>Encrypted string</returns>
        string EncryptSearchable();

        /// <summary>
        /// Encrypts the Searchable index of the object to a string, using the specified cryptography 'engine'
        /// </summary>
        /// <param name="cryptEngine">The cryptography 'engine' to be used for encryption</param>
        /// <returns>Encrypted string</returns>
        string EncryptSearchable(IObjectCryptEngine cryptEngine);

    }
}
