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
            var indexLive = GlobalVariables.liveTabIndex;
            bool isLiveIndex = index == indexLive;
            if (index <= 1) return;
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle r = tabControl.GetTabRect(i);
                Rectangle closeButton = new Rectangle(r.Right - 16, r.Top + 3, 9, 9);
                if (closeButton.Contains(e.Location))
                {
                    if (!isLiveIndex && !GlobalVariables.apiConnected)
                    {
                        FileManage.ManageUnsavedData(textEditorControl, index, true);
                        tabControl.TabPages.RemoveAt(i);
                        tabControl.SelectTab(index - 1);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Add new tab with editor.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        /// <param name="e"></param>
        public static void AddNewTab(ref TabControl tabControl)
        {
            tabControl.SelectedIndex = 0;
            var tabCount = tabControl.TabCount;
            var lastIndex = tabControl.SelectedIndex;
            tabControl.TabPages.Insert(tabCount, $"New Page ({tabCount})          ");
            tabControl.SelectedIndex = lastIndex + tabCount;
        }

        /// <summary>
        /// Swith between tabs with ctrl + left/right key.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="left"></param>
        public static void SwitchTabs(ref TabControl tabControl, bool left)
        {
            var tabCount = tabControl.TabCount;
            var tabIndex = tabControl.SelectedIndex;

            if (left)
            {
                if (tabIndex > 1)
                    tabControl.SelectTab(tabIndex - 1);
            }
            else
                if (tabIndex <= tabCount - 2)
                tabControl.SelectTab(tabIndex + 1);
        }

        public static void ColorTab(TabControl tabControl, int index, DrawItemEventArgs e)
        {
            //Get the working area of the TabControl main control
            Rectangle rec = tabControl.ClientRectangle;
            //Create a StringFormat object to set the layout of the label text
            StringFormat StrFormat = new StringFormat();
            StrFormat.LineAlignment = StringAlignment.Center;// Set the text to be centered vertically
            StrFormat.Alignment = StringAlignment.Center;// Set the text to be centered horizontally

            // The background fill color of the label, it can also be a picture (e.Graphics.DrawImage)
            //SolidBrush backColor = new SolidBrush(Color.Gray);
            SolidBrush fontColor;// Label font color
                                 //Draw the background of the main control
            //e.Graphics.FillRectangle(backColor, rec);

            //Draw label style
            Font fntTab = e.Font;
            Brush bshBack = new SolidBrush(Color.Red);
            Rectangle recBounds = tabControl.GetTabRect(index);
            RectangleF tabTextArea = (RectangleF)tabControl.GetTabRect(index);
            e.Graphics.FillRectangle(bshBack, recBounds);
            fontColor = new SolidBrush(Color.Black);
            e.Graphics.DrawString(tabControl.TabPages[index].Text, fntTab, fontColor, tabTextArea, StrFormat);
        }
    }
}
