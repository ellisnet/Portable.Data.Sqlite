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
    /// Standard interface for .NET object encryption engines (that encrypt and decrypt objects)
    /// </summary>
    public interface IObjectCryptEngine : IDisposable {

        /// <summary>
        /// Serialize and encrypt a .NET object
        /// </summary>
        /// <param name="objectToEncrypt">The object to be (serialized and) encrypted</param>
        /// <returns>Encrypted string (Base-64 encoded byte array)</returns>
        string EncryptObject(Object objectToEncrypt);

        /// <summary>
        /// Decrypt an encrypted string (Base-64 encoded byte array) into an object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="stringToDecrypt">The encrypted string (Base-64 encoded byte array)</param>
        /// <returns>An object of the specified type</returns>
        T DecryptObject<T>(string stringToDecrypt);

        /// <summary>
        /// Initializes the object encryption engine
        /// </summary>
        /// <param name="cryptoParams">A list of parameters used for initialization</param>
        void Initialize(Dictionary<string, Object> cryptoParams);

    }
}
