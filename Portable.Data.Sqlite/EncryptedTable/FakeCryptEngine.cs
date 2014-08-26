using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

//IMPORTANT NOTE: Do not use this class. IT DOES NOT ENCRYPT DATA.  It is just for demonstration of the 
//  functionality of something that requires a cryptography engine, without providing any proprietary 
//  encryption code.
//  You will need to create your own implementation of ICryptEngine; for standard encryption algorithms 
//  that work properly with a portable class library, you may want to look at the code that is available 
//  here:  https://github.com/onovotny/BouncyCastle-PCL

namespace Portable.Data.Sqlite {

    /// <summary>
    /// DO NOT USE - A sample library that simulates encryption (but does not actually encrypt the data)
    /// </summary>
    public class FakeCryptEngine : IObjectCryptEngine {

        private string _cipherKey = "won't be doing anything with this";
        private bool _initialized = false;

        /// <summary>
        /// DO NOT USE - only simulates encryption, data is not encrypted
        /// </summary>
        /// <param name="cipherKey">The key that WILL NOT be used for encryption</param>
        public FakeCryptEngine(string cipherKey) {
            this.Initialize(new Dictionary<string, object>() { { "CipherKey", cipherKey } });
        }

        /// <summary>
        /// DO NOT USE - only simulates encryption, data is not encrypted (parameterless constructor)
        /// </summary>
        public FakeCryptEngine() { }

        /// <summary>
        /// DO NOT USE - only simulates encryption, data is not encrypted (initializer for parameterless constructor)
        /// </summary>
        /// <param name="cryptoParams">A list of parameters used for initialization</param>
        public void Initialize(Dictionary<string, object> cryptoParams) {
            _cipherKey = cryptoParams["CipherKey"].ToString();
            _initialized = true;
        }

        /// <summary>
        /// DO NOT USE - only simulates the encryption of a .NET object, data is not encrypted
        /// </summary>
        /// <param name="objectToEncrypt">The .NET object that WILL NOT be encrypted</param>
        /// <returns>Unencrypted base-64 encoded byte array of the serialized object</returns>
        public string EncryptObject(object objectToEncrypt) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            return (objectToEncrypt == null) ?
                null :
                Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToEncrypt)));
        }

        /// <summary>
        /// DO NOT USE - only simulates the decryption of a .NET object, data is not encrypted
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize and return</typeparam>
        /// <param name="stringToDecrypt">The string that WILL NOT be decrypted</param>
        /// <returns>The deserialized object</returns>
        public T DecryptObject<T>(string stringToDecrypt) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            byte[] bytesToDecrypt = String.IsNullOrWhiteSpace(stringToDecrypt) ?
                null :
                Convert.FromBase64String(stringToDecrypt.Trim());
            return (bytesToDecrypt == null) ?
                default(T) :
                JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytesToDecrypt, 0, bytesToDecrypt.Length));
        }

        /// <summary>
        /// Dispose resources used by the instance
        /// </summary>
        public void Dispose() {
            _cipherKey = null;
        }

    }
}
