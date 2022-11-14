using CIARE.Utils.Options;
using ICSharpCode.TextEditor;

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
            textArea.Caret.UpdateCaretPosition();
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
    }
}
