using System;
using System.Security.Cryptography;
using System.Text;

namespace CIARE.Utils.Encryption
{
    public class MD5Hash
    {
        /// <summary>
        /// Get MD5 hash from string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetMD5Hash(string data)
        {
            string result = "";
            if (string.IsNullOrEmpty(data)) return "";
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            return result;
        }
    }
}
