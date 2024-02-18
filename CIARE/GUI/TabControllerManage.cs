using CIARE.Utils;
using ICSharpCode.TextEditor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
                    CloseSelectedIndex(textEditorControl, tabControl, index, false);
            return;
        }


        private static void CloseSelectedIndex(TextEditorControl textEditorControl, TabControl tabControl, int index, bool checkAll)
        {
            FileManage.ManageUnsavedData(textEditorControl, index, checkAll);
            if (GlobalVariables.OStartUp)
                StoreDeleteTabs("", tabControl.SelectedTab.Text, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, true, tabControl.SelectedTab.ToolTipText);
            tabControl.TabPages.RemoveAt(index);
            if (index >= tabControl.TabCount)
                tabControl.SelectTab(index-1);
            else
                tabControl.SelectTab(index);

        }
        /// <summary>
        /// Add new tab with editor.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        /// <param name="e"></param>
        public static void AddNewTab(ref TabControl tabControl, int index = 0)
        {
            tabControl.SelectedIndex = index;
            var tabCount = tabControl.TabCount;
            var lastIndex = tabControl.SelectedIndex;
            tabControl.TabPages.Insert(tabCount, $"New Page              ");
            tabControl.SelectedIndex = lastIndex + tabCount;
        }

        /// <summary>
        /// Funtiction to store tabs file size.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        public static void StoreFileSize(string filePath, string tempDir, string fileTabStore, int tabIndex)
        {
            if (!Directory.Exists(tempDir))
                return;

            if (!File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, "");

            FileInfo fileInfo = new FileInfo(filePath);

            var fileSize = fileInfo.Length;
            var line = $"{filePath}|{fileSize}|{tabIndex}";
            List<string> lines = File.ReadAllLines(fileTabStore).ToList();

            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].EndsWith($"|{tabIndex}"))
                    lines.Remove(lines[i]);
            }

            lines.Add(line);
            File.WriteAllText(fileTabStore, string.Join("\n", lines));
        }

        /// <summary>
        /// Clean file size file.
        /// </summary>
        /// <param name="fileTabStore"></param>
        public static void CleanFileSizeStoreFile(string fileTabStore) => File.WriteAllText(fileTabStore, string.Empty);

        /// <summary>
        /// Store/delete Tabs title and index.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        /// <param name="tabIndex"></param>
        public static void StoreDeleteTabs(string previewTabPath, string filePath, string tempDir, string fileTabStore, int tabIndex, bool remove = false, string pathRmove = "")
        {
            if (!Directory.Exists(tempDir))
                return;

            if (!File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, "");

            var line = $"{filePath}|{tabIndex}";
            List<string> lines = File.ReadAllLines(fileTabStore).ToList();
            if (!remove)
            {
                if (lines.Any(i => i.Contains(previewTabPath)) && !string.IsNullOrEmpty(previewTabPath))
                    lines.RemoveAll(i => i.Contains(previewTabPath));
                if (!lines.Any(i => i.Contains(filePath)))
                    lines.Add(line);
            }
            else
            {
                if (lines.Any(i => i.Contains(pathRmove)))
                    lines.RemoveAll(i => i.Contains(pathRmove));
            }
            File.WriteAllText(fileTabStore, string.Join("\n", lines));
        }

        /// <summary>
        /// Clean stored tabs for reopen. Session clean on startup.
        /// </summary>
        /// <param name="fileTabStore"></param>
        /// <param name="isSeesionActive"></param>
        public static void CleanStoredTabs(string tempDir, string fileTabStore)
        {
            if (!Directory.Exists(tempDir))
                return;

            if (File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, "");
        }

        /// <summary>
        /// Read tabs title and index from local file stored file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        public static void ReadTabs(TabControl tabControl, TextEditorControl textEditor, string tempDir, string fileTabStore)
        {
            if (!Directory.Exists(tempDir))
                return;

            if (!File.Exists(fileTabStore))
                return;

            var lines = File.ReadAllLines(fileTabStore);
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var path = line.Split('|')[0].Trim();
                    int index = Int32.Parse(line.Split('|')[1].Trim());
                    list.Add(new KeyValuePair<string, int>(path, index));
                }
            }

            // Sort by value
            list = list.OrderBy(x => x.Value).ToList();

            foreach (var item in list)
            {
                if (File.Exists(item.Key))
                {
                    FileInfo fileInfo = new FileInfo(item.Key);

                    using (var reader = new StreamReader(item.Key))
                    {
                        AddNewTab(ref tabControl);
                        SelectedEditor.GetSelectedEditor().Text = reader.ReadToEnd();
                        MainForm.Instance.Text = $"{fileInfo.Name} - CIARE {MainForm.Instance.versionName}";
                        MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name}      ";
                        MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = item.Key;
                    }
                }
                else
                {
                    StoreDeleteTabs("", tabControl.SelectedTab.Text, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, true, item.Key);
                }
            }
            GlobalVariables.isStoringTabs = true;
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

        public static void SwitchTabs2(ref TabControl tabControl, bool left)
        {
            var tabCount = tabControl.TabCount;
            var tabIndex = tabControl.SelectedIndex;


            if (tabIndex > 1)
                tabControl.SelectTab(tabIndex - 1);
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
