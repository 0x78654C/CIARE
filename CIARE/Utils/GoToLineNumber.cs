using ICSharpCode.TextEditor;
using System;

namespace CIARE.Utils
{
    public class GoToLineNumber
    {
        /// <summary>
        /// Got to line number.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="lineNumber"></param>
        public static void GoToLine(TextEditorControl textEditorControl , int lineNumber)
        {
            TextArea textArea = textEditorControl.ActiveTextAreaControl.TextArea;
            textArea.Caret.Line = lineNumber - 1;
            textArea.ScrollToCaret();
        }
     
        /// <summary>
        /// Set position of caret by column and line.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="position"></param>
        public static void SetPositionCaret(TextEditorControl textEditorControl, string position)
        {
            if (!string.IsNullOrEmpty(position))
            {
                int line = Int32.Parse(position.Split('|')[0]);
                int column = Int32.Parse(position.Split('|')[1]);
                textEditorControl.ActiveTextAreaControl.TextArea.Caret.Line = line;
                textEditorControl.ActiveTextAreaControl.TextArea.Caret.Column = column;
                textEditorControl.ActiveTextAreaControl.TextArea.ScrollTo(line+22);
            }
        }

        /// <summary>
        /// Get Line number.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <returns></returns>
        public static int GetLineNumber(TextEditorControl textEditorControl)
        {
            TextArea textArea = textEditorControl.ActiveTextAreaControl.TextArea;
            return textArea.Caret.Line;
        }

        /// <summary>
        /// Get column number.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <returns></returns>
        public static int GetColumnNumber(TextEditorControl textEditorControl) => textEditorControl.ActiveTextAreaControl.TextArea.Caret.Column;
    }
}
