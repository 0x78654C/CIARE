using ICSharpCode.TextEditor;

namespace CIARE.Utils
{
    /*
     Split ICSharpCode.TextEditor window.
     */
    public class SplitEditorWindow
    {
        /// <summary>
        /// Split text editor window.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SplitWindow(TextEditorControl textEditor, bool horizontal)
        {
            TextEditorControl editor = textEditor;
            if (editor != null)
                editor.Split(horizontal);

            GlobalVariables.splitWindowPosition = horizontal;
        }
        /// <summary>
        /// Check and set realtime spliter position to middle on texteditor resize.
        /// </summary>
        /// <param name="textEditor"></param>
        /// <param name="horizontal"></param>
        public static void  SetSplitWindowSize(TextEditorControl textEditor, bool horizontal)
        {
            TextEditorControl editor = textEditor;
            if (editor != null)
                editor.SplitPosition(horizontal);
        }

        public static void SetActiveSplit(TextEditorControl textEditor, bool isPrimary)
        {
            var areaControl = textEditor.ActiveTextAreaControl;
            textEditor.ActiveTextAreaControl.TextArea.GotFocus += delegate
            {
                textEditor.SetActiveTextAreaControl(areaControl);
            };
        }
    }
}
