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
#if NETFX_CORE
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
#else
using System.Security.Cryptography;
#endif
using System.Text;
using Newtonsoft.Json;

//Example AES-based "crypt engine" for use with Portable.Data.Sqlite,
//  should work on all platforms supported by Portable.Data.Sqlite.

//Disclaimer:
//  THIS SAMPLE CODE IS BEING PROVIDED FOR DEMONSTRATION PURPOSES ONLY, AND
//  IS NOT INTENDED TO PROVIDE ANY SPECIFIC LEVEL OF DATA SECURITY.
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
//  SOFTWARE.

namespace Portable.Data.Sqlite {

#if XAMARIN_FORMS
    [assembly: Xamarin.Forms.Dependency(typeof(AesCryptEngine))]
#endif
    public class AesCryptEngine : IObjectCryptEngine {

        private static readonly int cryptoKeySizeInBytes = 32;
        private static readonly int cryptoBlockSizeInBytes = 16;

        private string _cryptoKey;
        private byte[] _keyBytes;
        private bool _initialized = false;

        #region Private ByteArray methods

        private byte[] getByteArray(string text, int requiredLength) {
            var result = new byte[requiredLength];
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            int offset = 0;
            while (offset < requiredLength) {
                int toCopy = (requiredLength >= (offset + textBytes.Length)) ?
                    textBytes.Length : requiredLength - offset;
                System.Buffer.BlockCopy(textBytes, 0, result, offset, toCopy);
                offset += toCopy;
            }
            return result;
        }

        private byte[] getRandomBytes(int requiredLength) {
            if (requiredLength < 0) { requiredLength = 0; }
            byte[] result = (requiredLength == 0) ? new byte[] { } : new byte[requiredLength];

            if (requiredLength > 0) {
                (new Random()).NextBytes(result);

                //Use GUIDs to provide random bytes:
                //int currentOffset = 0;
                //while (currentOffset < requiredLength) {
                //    byte[] newBytes = Guid.NewGuid().ToByteArray();
                //    if ((currentOffset + newBytes.Length) < requiredLength) {
                //        System.Buffer.BlockCopy(newBytes, 0, result, currentOffset, newBytes.Length);
                //    }
                //    else {
                //        System.Buffer.BlockCopy(newBytes, 0, result, currentOffset, (requiredLength - currentOffset));
                //    }
                //    currentOffset += newBytes.Length;
                //}
            }

            return result;
        }

        private Tuple<byte[], byte[]> splitByteArray(byte[] byteArray, int firstArrayLength) {
            byte[] array1 = byteArray;
            byte[] array2 = null;

            if (firstArrayLength < 1) { throw new ArgumentOutOfRangeException(nameof(firstArrayLength)); }

            if (byteArray != null && byteArray.Length > firstArrayLength) {
                array1 = new byte[firstArrayLength];
                array2 = new byte[byteArray.Length - firstArrayLength];
                System.Buffer.BlockCopy(byteArray, 0, array1, 0, firstArrayLength);
                System.Buffer.BlockCopy(byteArray, firstArrayLength, array2, 0, array2.Length);
            }

            return Tuple.Create(array1, array2);
        }

        private byte[] joinByteArrays(byte[] byteArray1, byte[] byteArray2) {
            byte[] result = byteArray1;

            if (byteArray2 != null) {
                if (byteArray1 == null || byteArray1.Length == 0) {
                    result = byteArray2;
                }
                else if (byteArray2.Length > 0) {
                    result = new byte[byteArray1.Length + byteArray2.Length];
                    System.Buffer.BlockCopy(byteArray1, 0, result, 0, byteArray1.Length);
                    System.Buffer.BlockCopy(byteArray2, 0, result, byteArray1.Length, byteArray2.Length);
                }
            }

            return result;
        }

        #endregion

        //Parameterless constructor required for Xamarin.Forms when using Dependency Service
        public AesCryptEngine() { }

        public AesCryptEngine(string cryptoKey) {
            if (cryptoKey == null) { throw new ArgumentNullException(nameof(cryptoKey)); }
            if (String.IsNullOrWhiteSpace(cryptoKey)) { throw new ArgumentOutOfRangeException(nameof(cryptoKey)); }
            this.Initialize(new Dictionary<string, object>() { { "CryptoKey", cryptoKey } });
        }

        public void Initialize(string cryptoKey) {
            if (cryptoKey == null) { throw new ArgumentNullException(nameof(cryptoKey)); }
            if (String.IsNullOrWhiteSpace(cryptoKey)) { throw new ArgumentOutOfRangeException(nameof(cryptoKey)); }
            this.Initialize(new Dictionary<string, object>() { { "CryptoKey", cryptoKey } });
        }

        public void Initialize(Dictionary<string, object> cryptoParams) {
            if (cryptoParams == null) { throw new ArgumentNullException(nameof(cryptoParams)); }
            if ((!cryptoParams.ContainsKey("CryptoKey"))
                || String.IsNullOrWhiteSpace(cryptoParams["CryptoKey"]?.ToString())) {
                throw new ArgumentException("The 'CryptoKey' value is missing or empty.", nameof(cryptoParams));
            }

            _cryptoKey = cryptoParams["CryptoKey"].ToString();

            if (_cryptoKey != _cryptoKey.Trim()) {
                throw new ArgumentException("The 'CryptoKey' value cannot have leading or trailing whitespace.", nameof(cryptoParams));
            }

            _keyBytes = getByteArray(_cryptoKey, cryptoKeySizeInBytes);
            _initialized = true;
        }

#if NETFX_CORE

        public T DecryptObject<T>(string stringToDecrypt, bool throwExceptionOnNullObject = false) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            T result = default(T);
            if (!String.IsNullOrWhiteSpace(stringToDecrypt)) {
                byte[] bytesToDecrypt = Convert.FromBase64String(stringToDecrypt);
                byte[] decryptedBytes = null;
                //Splitting bytesToDecrypt into IV (Item1) and encrypted bytes (Item2)
                var splitBytes = splitByteArray(bytesToDecrypt, cryptoBlockSizeInBytes);
                if (splitBytes.Item2 != null && splitBytes.Item2.Length > 0) {
                    var aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
                    CryptographicKey aesKey = aesProvider.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(_keyBytes));
                    IBuffer aesIV = CryptographicBuffer.CreateFromByteArray(splitBytes.Item1);
                    IBuffer encrypted = CryptographicBuffer.CreateFromByteArray(splitBytes.Item2);
                    IBuffer decrypted = CryptographicEngine.Decrypt(aesKey, encrypted, aesIV);
                    using (var dr = DataReader.FromBuffer(decrypted)) {
                        decryptedBytes = new byte[dr.UnconsumedBufferLength];
                        dr.ReadBytes(decryptedBytes);
                    }
                }
                else if (throwExceptionOnNullObject) {
                    throw new Exception("The object to be decrypted was null or empty.");
                }

                if (decryptedBytes != null && decryptedBytes.Length > 0) {
                    result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decryptedBytes));
                }
                else if (throwExceptionOnNullObject) {
                    throw new Exception("The object to be decrypted was null or empty.");
                }
            }
            else if (throwExceptionOnNullObject) {
                if (stringToDecrypt == null) { throw new ArgumentNullException(nameof(stringToDecrypt)); }
                if (String.IsNullOrWhiteSpace(stringToDecrypt)) { throw new ArgumentOutOfRangeException(nameof(stringToDecrypt)); }
            }
            return result;
        }

        public string EncryptObject(object objectToEncrypt) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            string result = null;
            byte[] iv = getRandomBytes(cryptoBlockSizeInBytes);
            if (objectToEncrypt != null) {
                byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToEncrypt));
                var aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
                CryptographicKey aesKey = aesProvider.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(_keyBytes));            
                IBuffer aesIV = CryptographicBuffer.CreateFromByteArray(iv);
                IBuffer decrypted = CryptographicBuffer.CreateFromByteArray(bytesToEncrypt);
                IBuffer encrypted = CryptographicEngine.Encrypt(aesKey, decrypted, aesIV);
                byte[] encryptedBytes;
                using (var dr = DataReader.FromBuffer(encrypted)) {
                    encryptedBytes = new byte[dr.UnconsumedBufferLength];
                    dr.ReadBytes(encryptedBytes);
                }
                result = Convert.ToBase64String(joinByteArrays(iv, encryptedBytes));
            }
            result = result ?? Convert.ToBase64String(iv);

            return result;
        }

#else

        public T DecryptObject<T>(string stringToDecrypt, bool throwExceptionOnNullObject = false) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            T result = default(T);
            if (!String.IsNullOrWhiteSpace(stringToDecrypt)) {
                byte[] bytesToDecrypt = Convert.FromBase64String(stringToDecrypt);
                byte[] decryptedBytes = null;
                using (var aesProvider = new AesCryptoServiceProvider() {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    KeySize = (cryptoKeySizeInBytes * 8), //KeySize in bits
                    BlockSize = (cryptoBlockSizeInBytes * 8), //BlockSize in bits
                    Key = _keyBytes
                }) {
                    //Splitting bytesToDecrypt into IV (Item1) and encrypted bytes (Item2)
                    var splitBytes = splitByteArray(bytesToDecrypt, cryptoBlockSizeInBytes);
                    if (splitBytes.Item2 != null && splitBytes.Item2.Length > 0) {
                        aesProvider.IV = splitBytes.Item1;
                        decryptedBytes = aesProvider.CreateDecryptor().TransformFinalBlock(splitBytes.Item2, 0, splitBytes.Item2.Length);
                    }
                }
                if (decryptedBytes != null) {
                    result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decryptedBytes));
                }
                else if (throwExceptionOnNullObject) {
                    throw new Exception("The object to be decrypted was null or empty.");
                }
            }
            else if (throwExceptionOnNullObject) {
                if (stringToDecrypt == null) { throw new ArgumentNullException(nameof(stringToDecrypt)); }
                if (String.IsNullOrWhiteSpace(stringToDecrypt)) { throw new ArgumentOutOfRangeException(nameof(stringToDecrypt)); }
            }
            return result;
        }

        public string EncryptObject(object objectToEncrypt) {
            if (!_initialized) throw new Exception("Crypt engine is not initialized.");
            string result = null;
            if (objectToEncrypt != null) {
                byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToEncrypt));
                using (var aesProvider = new AesCryptoServiceProvider() {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    KeySize = (cryptoKeySizeInBytes * 8), //KeySize in bits
                    BlockSize = (cryptoBlockSizeInBytes * 8), //BlockSize in bits
                    Key = _keyBytes
                }) {
                    aesProvider.GenerateIV();
                    byte[] encryptedBytes = aesProvider.CreateEncryptor().TransformFinalBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
                    result = Convert.ToBase64String(joinByteArrays(aesProvider.IV, encryptedBytes));
                }
            }
            result = result ?? Convert.ToBase64String(getRandomBytes(cryptoBlockSizeInBytes));

            return result;
        }

#endif

        public void Dispose() {
            _cryptoKey = null;
            _keyBytes = null;
        }
    }
}
