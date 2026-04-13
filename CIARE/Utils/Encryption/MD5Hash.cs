using System;
using System.Security.Cryptography;
using System.Text;

namespace CIARE.Utils.Encryption
{
    public class FileHash
    {
        /// <summary>
        /// Get SHA256 hash from string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetFileHash(string data)
        {
            string result = "";
            if (string.IsNullOrEmpty(data)) return "";
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            return result;
        }
    }
}
