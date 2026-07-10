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
        private const string CurrentPayloadVersion = "2";
        private const int EncryptionKeyBytes = 32;
        private const int MacKeyBytes = 32;


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
                byte[] keyMaterial = CreateKey(password, salt, EncryptionKeyBytes + MacKeyBytes);
                aes.Key = keyMaterial.Take(EncryptionKeyBytes).ToArray();
                aes.GenerateIV();
                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] buffer = encoding.GetBytes(plainText);
                string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));
                string iv = Convert.ToBase64String(aes.IV);
                string saltText = Convert.ToBase64String(salt);
                byte[] macKey = keyMaterial.Skip(EncryptionKeyBytes).Take(MacKeyBytes).ToArray();
                string mac = Convert.ToHexString(HmacSHA256(
                    BuildMacInput(CurrentPayloadVersion, iv, encryptedText, saltText), macKey))
                    .ToLowerInvariant();
                var keyValues = new Dictionary<string, object>
                {
                    { "version", CurrentPayloadVersion },
                    { "iv", iv },
                    { "value", encryptedText },
                    { "mac", mac },
                    { "salt", saltText },
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
                if (payload == null ||
                    !payload.TryGetValue("iv", out string ivText) ||
                    !payload.TryGetValue("value", out string encryptedText) ||
                    !payload.TryGetValue("mac", out string suppliedMac))
                {
                    return string.Empty;
                }

                // Support legacy ciphertexts (no salt field) for backward compatibility.
                byte[] salt = payload.TryGetValue("salt", out var saltStr) && !string.IsNullOrEmpty(saltStr)
                    ? Convert.FromBase64String(saltStr)
                    : LegacySalt(password);
                bool isCurrentPayload = payload.TryGetValue("version", out string version) &&
                    string.Equals(version, CurrentPayloadVersion, StringComparison.Ordinal);
                byte[] keyMaterial = CreateKey(password, salt,
                    isCurrentPayload ? EncryptionKeyBytes + MacKeyBytes : EncryptionKeyBytes);
                byte[] expectedMac = isCurrentPayload
                    ? HmacSHA256(BuildMacInput(version, ivText, encryptedText, saltStr),
                        keyMaterial.Skip(EncryptionKeyBytes).Take(MacKeyBytes).ToArray())
                    : HmacSHA256(ivText + encryptedText, password);
                byte[] suppliedMacBytes = Convert.FromHexString(suppliedMac);
                if (suppliedMacBytes.Length != expectedMac.Length ||
                    !CryptographicOperations.FixedTimeEquals(suppliedMacBytes, expectedMac))
                {
                    return string.Empty;
                }

                aes.Key = keyMaterial.Take(EncryptionKeyBytes).ToArray();
                aes.IV = Convert.FromBase64String(ivText);
                ICryptoTransform AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] buffer = Convert.FromBase64String(encryptedText);
                return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
            }
            catch { return string.Empty; }
        }

        private static byte[] CreateKey(string password, byte[] salt, int keyBytes = 32)
        {
            const int Iterations = 100_000;
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations,
                HashAlgorithmName.SHA256, keyBytes);
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
            return HmacSHA256(data, encoding.GetBytes(key));
        }

        private static byte[] HmacSHA256(string data, byte[] key)
        {
            using HMACSHA256 hmac = new HMACSHA256(key);
            return hmac.ComputeHash(encoding.GetBytes(data));
        }

        private static string BuildMacInput(string version, string iv, string value, string salt)
            => string.Join("|", version, iv, value, salt);
    }
}
