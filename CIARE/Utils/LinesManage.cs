using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using ICSharpCode.TextEditor;

namespace CIARE.Utils
{
    [SupportedOSPlatform("Windows")]
    public class LinesManage
    {
        /// <summary>
        /// Get care position(line,column) from editor data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void GetCaretPositon(object sender, EventArgs e)
        {
            int selectedTab = MainForm.Instance.EditorTabControl.SelectedIndex;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[selectedTab].Controls[0];
            var textEditor = ctrl as TextEditorControl;
            if (!string.IsNullOrEmpty(textEditor.Text))
                MainForm.Instance.linesPositionLbl.Text =
                    $"[Line {textEditor.ActiveTextAreaControl.TextArea.Caret.Position.Line + 1}, Col {textEditor.ActiveTextAreaControl.TextArea.Caret.Column}]";
            else
                MainForm.Instance.linesPositionLbl.Text = string.Empty;
        }

        /// <summary>
        /// Get total lines count from editor data.
        /// </summary>
        /// <param name="mainEditor"></param>
        /// <param name="totalLinesCountLbl"></param>
        public static void GetTotalLinesCount(Label totalLinesCountLbl)
        {
            int selectedTab = MainForm.Instance.EditorTabControl.SelectedIndex;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[selectedTab].Controls[0];
            var textEditor = ctrl as TextEditorControl;
            if (!string.IsNullOrEmpty(textEditor.Text))
                totalLinesCountLbl.Text = $"Lines: {textEditor.Document.TotalNumberOfLines}";
            else
                totalLinesCountLbl.Text = string.Empty;
        }
    }
}
