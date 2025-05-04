using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Versioning;

namespace CIARE.Utils.Encryption
{
    [SupportedOSPlatform("windows")]
    public class DPAPI
    {
        public string Data { get; set; }

        /// <summary>
        /// Ctor for DPAPI
        /// </summary>
        public DPAPI() { }
        
        /// <summary>
        /// Encrypts the data using DPAPI
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Encrypt(string data)
        {
            byte[] encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts the data using DPAPI
        /// </summary>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        public static string Decrypt(string encryptedData)
        {
            byte[] decryptedData = ProtectedData.Unprotect(Convert.FromBase64String(encryptedData), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}