using CIARE.Utils;
using ICSharpCode.TextEditor;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class TabControllerManage
    {
        /// <summary>
        /// Manage event on close tab.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        /// <param name="e"></param>
        public static void CloseTabEvent(TabControl tabControl, TextEditorControl textEditorControl, MouseEventArgs e)
        {
            var index = tabControl.SelectedIndex;
            if (index <= 1) return;
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle r = tabControl.GetTabRect(i);
                Rectangle closeButton = new Rectangle(r.Right - 15, r.Top + 4, 9, 7);
                if (closeButton.Contains(e.Location))
                {
                    //bool isNotSaved = tabControl.SelectedTab.Text.StartsWith("*")
                    //if (isNotSaved)
                    //{
                    //    FileManage.ManageUnsavedData(textEditorControl, index, true);
                    //    tabControl.TabPages.RemoveAt(i);
                    //    tabControl.SelectTab(index - 1);
                    //    break;
                    //}
                    //else
                    //{
                    //    FileManage.ManageUnsavedData(textEditorControl, index, true);
                    //    tabControl.TabPages.RemoveAt(i);
                    //    tabControl.SelectTab(index - 1);
                    //    break;
                    //}
                    FileManage.ManageUnsavedData(textEditorControl, index, true);
                    tabControl.TabPages.RemoveAt(i);
                    tabControl.SelectTab(index - 1);
                    break;
                }
            }
        }


    }
}
