using CIARE.Utils;
using ICSharpCode.TextEditor;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class TabControllerManage
    {
        private static int s_hoverIndex = -1;

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
                Rectangle r = tabControl.GetTabRect(index);
                Rectangle closeButton = new Rectangle(r.Right - 16, r.Top + 3, 9, 9);
                if (closeButton.Contains(e.Location))
                    if (!GlobalVariables.apiConnected && !GlobalVariables.apiRemoteConnected)
                        CloseSelectedIndex(textEditorControl, tabControl, index,false);
                return;
        }


        private static void CloseSelectedIndex(TextEditorControl textEditorControl, TabControl tabControl, int index, bool checkAll)
        {
            FileManage.ManageUnsavedData(textEditorControl, index, checkAll);
            tabControl.TabPages.RemoveAt(index);
            tabControl.SelectTab(index - 1);
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
            tabControl.TabPages.Insert(tabCount, $"New Page              ");
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

        /// <summary>
        /// Set color for selected tab.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="index"></param>
        /// <param name="e"></param>
        public static void ColorTab(TabControl tabControl, int index, DrawItemEventArgs e, Color color)
        {
            Rectangle rec = tabControl.ClientRectangle;
            StringFormat StrFormat = new StringFormat();
            StrFormat.LineAlignment = StringAlignment.Center;
            StrFormat.Alignment = StringAlignment.Center;
            SolidBrush fontColor;
            Font fntTab = e.Font;
            Brush bshBack = new SolidBrush(color);
            Rectangle recBounds = tabControl.GetTabRect(index);
            RectangleF tabTextArea = (RectangleF)tabControl.GetTabRect(index);
            e.Graphics.FillRectangle(bshBack, recBounds);
            fontColor = new SolidBrush(Color.Black);
            e.Graphics.DrawString(tabControl.TabPages[index].Text, fntTab, fontColor, tabTextArea, StrFormat);

            var g = e.Graphics;
            var tp = tabControl.TabPages[e.Index];
            var rt = e.Bounds;
            var rx = new Rectangle(rt.Right - 20, (rt.Y + (rt.Height - 12)) / 2 + 1, 12, 12);

            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
            {
                rx.Offset(0, 2);
            }

            rt.Inflate(-rx.Width, 0);
            rt.Offset(-(rx.Width / 2), 0);

            using (Font f = new Font("Marlett", 8f))
            using (StringFormat sf = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            })
            {
                //g.DrawString(tp.Text, tp.Font ?? Control.DefaultFont, Brushes.Black, rt, sf);
                if (e.Index > 1)
                    g.DrawString("r", f, s_hoverIndex == e.Index ? Brushes.Black : Brushes.Gray, rx, sf);
            }
            tp.Tag = rx;
        }

        /// <summary>
        /// Draw tab in tabcontrl and set x pointer for close.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="e"></param>
        public static void DrawTabControl(TabControl tabControl, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tp = tabControl.TabPages[e.Index];
            var rt = e.Bounds;
            var rx = new Rectangle(rt.Right - 20, (rt.Y + (rt.Height - 12)) / 2 + 1, 12, 12);

            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
            {
                rx.Offset(0, 2);
            }

            rt.Inflate(-rx.Width, 0);
            rt.Offset(-(rx.Width / 2), 0);

            using (Font f = new Font("Marlett", 8f))
            using (StringFormat sf = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            })
            {
                g.DrawString(tp.Text, tp.Font ?? Control.DefaultFont, Brushes.Black, rt, sf);
                if (e.Index > 1)
                    g.DrawString("r", f, s_hoverIndex == e.Index ? Brushes.Black : Brushes.Gray, rx, sf);
            }
            tp.Tag = rx;
        }

        /// <summary>
        /// Set Transparent tab bar.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="e"></param>
        /// 
        public static void SetTransparentTabBar(TabControl tabControl, DrawItemEventArgs e)
        {
            bool dark = GlobalVariables.darkColor;
            Color BackGroundColorForm = dark ? Color.FromArgb(51, 51, 51) : SystemColors.Window;
            SolidBrush fillbrush = new SolidBrush(BackGroundColorForm);
            Rectangle lasttabrect = tabControl.GetTabRect(tabControl.TabPages.Count - 1);
            Rectangle background = new Rectangle();
            background.Location = new Point(lasttabrect.Right, 0);
            background.Size = new Size(tabControl.Right - background.Left, lasttabrect.Height + 1);
            e.Graphics.FillRectangle(fillbrush, background);
        }
    }
}
