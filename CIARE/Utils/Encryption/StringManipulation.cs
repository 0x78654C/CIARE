using System.Security;

namespace CIARE
{
    /// <summary>
    /// String manipulation class for using secure string..
    /// </summary>
    public static class StringManipulation
    {
        /// <summary>
        /// Converts the secure string to string.
        /// </summary>
        /// <returns>The secure string to string.</returns>
        /// <param name="data">Data.</param>
        public static string ConvertSecureStringToString(this SecureString data)
        {
            return new System.Net.NetworkCredential(string.Empty, data).Password;
        }
        /// <summary>
        /// Convert string to secure string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SecureString StringToSecureString(this string data)
        {
            var secureString = new SecureString();
            foreach (var c in data)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }
    }
}
