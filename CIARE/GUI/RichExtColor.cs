using System.Windows.Forms;
using System.Drawing;
using System.Runtime.Versioning;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class RichExtColor
    {
        /// <summary>
        /// Color words in  richtextbox.
        /// </summary>
        /// <param name="myRtb"></param>
        /// <param name="word"></param>
        /// <param name="color"></param>
        public static void HighlightText(RichTextBox myRtb, string word, Color color)
        {

            if (word == string.Empty)
                return;

            int s_start = myRtb.SelectionStart, startIndex = 0, index;

            while ((index = myRtb.Text.IndexOf(word, startIndex)) != -1)
            {
                myRtb.Select(index, word.Length);
                myRtb.SelectionColor = color;
                startIndex = index + word.Length;
            }

            myRtb.SelectionStart = s_start;
            myRtb.SelectionLength = 0;
            myRtb.SelectionColor = Color.White;
        }

        /// <summary>
        /// Display error messages with red color.
        /// </summary>
        /// <param name="logOutput"></param>
        /// <param name="message"></param>
        public static void ErrorDisplay(RichTextBox logOutput, string message)
        {
            logOutput.ForeColor = Color.Red;
            logOutput.Text = message;
        }

        /// <summary>
        /// Mark word with color in richtextbox and scroll to caret.
        /// </summary>
        /// <param name="richTextBox"></param>
        /// <param name="word"></param>
        /// <param name="color"></param>
        public static void MarkWordRtb(RichTextBox richTextBox, string word, Color color)
        {
            RichExtColor.HighlightText(richTextBox, word, color);
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        }
    }
}
