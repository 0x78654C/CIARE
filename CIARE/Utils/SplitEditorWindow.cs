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
        }
    }
}
