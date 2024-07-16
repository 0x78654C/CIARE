using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    internal static class ExtensionMethods
    {
        internal static bool ContainsText(this string source, string searchText)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.ToLowerInvariant().Contains(searchText.ToLowerInvariant());
        }

        internal static bool ContainsParameter(this IEnumerable<string> parameters, string parameter)
        {
            return parameters.Any(p => p.Equals(parameter, StringComparison.InvariantCulture));
        }

        internal static string ParameterAfter(this IEnumerable<string> parameters, string parameter)
        {
            var parms = parameters.ToList();
            string p = string.Join(" ", parms);
            int index = parms.FindIndex(s => s.Equals(parameter, StringComparison.InvariantCulture));

            // Return an empty string if the parameter does not exist,
            // or if there is not another value after the searched parameter.
            if (index == -1 || index + 1 == parms.Count)
            {
                return "";
            }

            return parms[index + 1];
        }

        internal static string SplitByText(this string input, string parameter, int index)
        {
            // return Regex.Split(input, parameter)[index];
            string[] output = input.Split(new string[] { parameter }, StringSplitOptions.None);
            return output[index];
        }

        internal static bool IsNotNullEmptyOrWhitespace(this string text)
        {
            if (text == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(text);
        }

        internal static string MiddleString(this string input, string firstParam, string secondParam)
        {
            string firstParamSplit = input.SplitByText(firstParam, 1);
            return firstParamSplit.SplitByText(secondParam, 0);
        }

        internal static void ScrollToEnd(this RichTextBox richTextBox, bool isShown = false)
        {
            if(!isShown)
                richTextBox.Text += "\n";
            richTextBox.SelectionStart = richTextBox.Text.Length+1;
            richTextBox.ScrollToCaret();
        }


        /// <summary>
        /// Check if file is locked.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        internal static bool IsLocked(this FileInfo f)
        {
            try
            {
                string fpath = f.FullName;
                FileStream fs = File.OpenWrite(fpath);
                fs.Close();
                return false;
            }
            catch (Exception) { return true; }
        }
    }
}
