using CIARE.GUI;
using ICSharpCode.TextEditor;
using System.Runtime.Versioning;

namespace CIARE.Utils
{
    /*
     Split ICSharpCode.TextEditor window.
     */
    public class SplitEditorWindow
    {
        [SupportedOSPlatform("windows")]
        /// <summary>
        /// Split text editor window.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SplitWindow(TextEditorControl textEditor, bool horizontal)
        {
            TextEditorControl editor = textEditor;
            if (editor != null)
                editor.Split(horizontal);
            GlobalVariables.textAreaFirst = SelectedEditor.GetSelectedEditor().primaryTextArea;
            GlobalVariables.textAreaSecond = SelectedEditor.GetSelectedEditor().secondaryTextArea;
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
    }
}
