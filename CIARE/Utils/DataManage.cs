using System.Security.RightsManagement;
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

        /// <summary>
        /// Split fraze by number of words in line.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="lenghtSplit"></param>
        /// <returns></returns>
        public static string SplitTextByWordsInLine(string data, int lenghtSplit)
        {
            var splitData = data.Split(' ');
            var outData = "";
            int count = 0;
            foreach (var word in splitData)
            {
                if (count != lenghtSplit)
                    outData += $"{word} ";
                else
                {
                    outData += $"{word}\n";
                    count = 0;
                }
                count++;
            }
            return outData;
        }
    }
}
