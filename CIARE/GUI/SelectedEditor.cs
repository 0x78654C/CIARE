using ICSharpCode.TextEditor;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace CIARE.GUI
{
    [SupportedOSPlatform("Windows")]
    public class SelectedEditor
    {
        /// <summary>
        /// Get editor controler from selected tab.
        /// </summary>
        /// <returns></returns>
        public static TextEditorControl GetSelectedEditor()
        {
            int selectedTab = MainForm.Instance.EditorTabControl.SelectedIndex;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[selectedTab].Controls[0];
            var textEditor = ctrl as TextEditorControl;
            return textEditor;
        }
    }
}
