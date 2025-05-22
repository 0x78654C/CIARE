using System;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]

    public class CustomWrap
    {
        /// <summary>
        /// Wrap text in a string to fit within the specified control's width.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        public static string CustomWordWrap(string input, Control control)
        {
            var maxLineLength = EstimateCharactersPerLine(control)+20;
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var wrappedText = new StringBuilder();

            foreach (var line in lines)
            {
                // Split into words and whitespace (spaces, tabs) using regex
                var tokens = Regex.Matches(line, @"\S+|\s+")
                                  .Cast<Match>()
                                  .Select(m => m.Value)
                                  .ToList();

                int currentLineLength = 0;

                foreach (var token in tokens)
                {
                    // Handle tokens longer than maxLineLength (e.g., long words or tabs)
                    var tokenLength = token.Replace("\t", "    ").Length;

                    if (token == "\n")
                    {
                        wrappedText.AppendLine();
                        currentLineLength = 0;
                        continue;
                    }

                    if (currentLineLength + tokenLength > maxLineLength && !string.IsNullOrWhiteSpace(token))
                    {
                        wrappedText.AppendLine();
                        currentLineLength = 0;
                        // If leading whitespace on new line, skip it
                        if (char.IsWhiteSpace(token[0]))
                            continue;
                    }

                    wrappedText.Append(token);
                    currentLineLength += tokenLength;
                }

                wrappedText.AppendLine(); // Preserve original newlines
            }

            return wrappedText.ToString();
        }

        /// <summary>
        /// Estimate the number of characters that can fit in a line based on the control's width and font size.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private static int EstimateCharactersPerLine(Control control)
        {
            using (Graphics g = control.CreateGraphics())
            {
                // Measure the average width of a character (e.g., 'W' is wide)
                SizeF size = g.MeasureString("W", control.Font);
                float charWidth = size.Width;

                if (charWidth == 0) return 0;

                // Total usable width / width per character
                return (int)(control.ClientSize.Width / charWidth);
            }
        }
    }
}
