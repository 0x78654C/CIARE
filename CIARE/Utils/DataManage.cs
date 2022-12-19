using System.Text.RegularExpressions;

namespace CIARE.Utils
{
    public class DataManage
    {
        private static readonly Regex s_regexNumber = new Regex("[^0-9.-]+"); //regex that matches disallowed text

        /// <summary>
        /// Check string if contains numbers only.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsNumberAllowed(string text) => !s_regexNumber.IsMatch(text);
    }
}
