using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace CIARE.Utils.Encryption
{
    public class AESEncryption
    {
        private static readonly Encoding encoding = Encoding.UTF8;


        /// <summary>
        /// AES Encryption
        /// </summary>
        /// <param name="plainText">String input for encryption.</param>
        /// <param name="password">Master Password</param>>
        /// <returns>string</returns>
        public static string Encrypt(string plainText, string password)
        {
            try
            {
                var salt = new byte[16];
                RandomNumberGenerator.Fill(salt);

                using Aes aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.Key = CreateKey(password, salt);
                aes.GenerateIV();
                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] buffer = encoding.GetBytes(plainText);
                string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));
                string mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, password)).Replace("-", "").ToLower();
                var keyValues = new Dictionary<string, object>
                {
                    { "iv", Convert.ToBase64String(aes.IV) },
                    { "value", encryptedText },
                    { "mac", mac },
                    { "salt", Convert.ToBase64String(salt) },
                };
                return Convert.ToBase64String(encoding.GetBytes(JsonSerializer.Serialize(keyValues)));
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// AES Decryption 
        /// </summary>
        /// <param name="plainText">String input for decryption</param>
        /// <param name="password">Master Password</param>
        /// <returns>string</returns>
        public static string Decrypt(string plainText, string password)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                byte[] base64Decoded = Convert.FromBase64String(plainText);
                string base64DecodedStr = encoding.GetString(base64Decoded);
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(base64DecodedStr);
                // Support legacy ciphertexts (no salt field) for backward compatibility.
                byte[] salt = payload.TryGetValue("salt", out var saltStr) && !string.IsNullOrEmpty(saltStr)
                    ? Convert.FromBase64String(saltStr)
                    : LegacySalt(password);
                aes.Key = CreateKey(password, salt);
                aes.IV = Convert.FromBase64String(payload["iv"]);
                ICryptoTransform AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] buffer = Convert.FromBase64String(payload["value"]);
                return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
            }
            catch { return string.Empty; }
        }

        private static byte[] CreateKey(string password, byte[] salt, int keyBytes = 32)
        {
            const int Iterations = 100_000;
            using var keyGenerator = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return keyGenerator.GetBytes(keyBytes);
        }

        /// <summary>
        /// Legacy salt derivation kept only for decrypting old ciphertexts.
        /// Do NOT use for new encryption.
        /// </summary>
        private static byte[] LegacySalt(string password) => password.Select(Convert.ToByte).Take(5).ToArray();

        /// <summary>
        /// Hash computation with SHA256
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static byte[] HmacSHA256(String data, String key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(key)))
            {
                return hmac.ComputeHash(encoding.GetBytes(data));
            }
        }
    }
}
