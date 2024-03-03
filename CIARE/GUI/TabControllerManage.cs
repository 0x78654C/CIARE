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
        private static void CloseTabEvent(TabControl tabControl, TextEditorControl textEditorControl, MouseEventArgs e)
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

        /// <summary>
        /// Close event for right click menu
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        public static void CloseTabEvent(TabControl tabControl, TextEditorControl textEditorControl)
        {
            var index = tabControl.SelectedIndex;
            if (index <= 1) return;
            if (!GlobalVariables.apiConnected && !GlobalVariables.apiRemoteConnected)
                CloseSelectedIndex(textEditorControl, tabControl, index, false);
            return;
        }

        /// <summary>
        /// Close tab funciton on mouse down.
        /// </summary>
        /// <param name="tabControl"></param>
        public static void CloseTab(TabControl tabControl, MouseEventArgs e)
        {
            var tabCount = tabControl.TabCount;
            var lastIndex = tabControl.SelectedIndex;
            if (lastIndex == 0)
            {
                tabControl.TabPages.Insert(tabCount, $"New Page               ");
                tabControl.SelectedIndex = lastIndex + tabCount;
            }
            else
                CloseTabEvent(tabControl, SelectedEditor.GetSelectedEditor(), e);
        }


        /// <summary>
        /// Close all tabs.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        public static void CloseAllTabs(TabControl tabControl, TextEditorControl textEditorControl)
        {
            var tabPages = tabControl.TabPages;
            int tabCount = tabControl.TabCount - 1;
            int count = 0;
            FileManage.ManageUnsavedData(textEditorControl, 0, true);
            if (GlobalVariables.noClear)
                return;
            foreach (TabPage tabPage in tabPages)
            {
                count = tabCount - 1;
                if (count > 0)
                {
                    if (!GlobalVariables.apiConnected && !GlobalVariables.apiRemoteConnected)
                    {
                        tabControl.SelectTab(count);

                        var pathFile = tabControl.SelectedTab.ToolTipText;
                        tabControl.TabPages.RemoveAt(tabCount--);
                        tabControl.SelectTab(count);
                        if (GlobalVariables.OStartUp)
                        {
                            ClearTabsFile(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll);
                            ClearTabsFile(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath);
                        }
                        GlobalVariables.noClear = false;
                    }
                }
            }
        }

        /// <summary>
        /// Close all tabs.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        public static void CloseAllTabsOne(TabControl tabControl, TextEditorControl textEditorControl, int selectedIndex)
        {
            var tabPages = tabControl.TabPages;
            int tabCount = tabControl.TabCount - 1;
            int count = 0;
            FileManage.ManageUnsavedData(textEditorControl, 0, true);
            if (GlobalVariables.noClear)
                return;
            foreach (TabPage tabPage in tabPages)
            {
                count = tabCount - 1;
                if (count > 0)
                {
                    if (!GlobalVariables.apiConnected && !GlobalVariables.apiRemoteConnected)
                    {
                        tabControl.SelectTab(count);

                        var pathFile = tabControl.SelectedTab.ToolTipText;
                        var removeAt = tabCount--;
                        if (removeAt == selectedIndex)
                            continue;
                        tabControl.TabPages.RemoveAt(removeAt);
                        tabControl.SelectTab(count);
                        GlobalVariables.noClear = false;
                    }
                }
            }

            if (!GlobalVariables.apiConnected && !GlobalVariables.apiRemoteConnected)
            {
                tabControl.SelectTab(2);
                if (GlobalVariables.OStartUp)
                {
                    StoreSingleTab(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, tabControl.SelectedTab.ToolTipText, false);
                    StoreSingleTab(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabControl.SelectedTab.ToolTipText, true);
                }
            }
        }


        /// <summary>
        /// Clear tabs file all/size.
        /// </summary>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        private static void ClearTabsFile(string tempDir, string fileTabStore)
        {
            if (!Directory.Exists(tempDir))
                return;

            if (File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, "");
        }

        /// <summary>
        /// Clear tabs but not selected on and store file all/size.
        /// </summary>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        private static void StoreSingleTab(string tempDir, string fileTabStore, string filePath, bool isSize)
        {
            if (!Directory.Exists(tempDir))
                return;
            if (isSize)
            {
                if (File.Exists(fileTabStore))
                {
                    FileInfo fileInfo = new(filePath);
                    File.WriteAllText(fileTabStore, $"{filePath}|{fileInfo.Length}|2");
                }
            }
            else
                if (File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, $"{filePath}|2");

        }


        /// <summary>
        /// Close selected index is right dialog result.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="tabControl"></param>
        /// <param name="index"></param>
        /// <param name="checkAll"></param>
        private static void CloseSelectedIndex(TextEditorControl textEditorControl, TabControl tabControl, int index, bool checkAll)
        {
            FileManage.ManageUnsavedData(textEditorControl, index, checkAll);

            var pathFile = tabControl.SelectedTab.ToolTipText;
            if (!GlobalVariables.noClear)
            {
                tabControl.TabPages.RemoveAt(index);
                if (index >= tabControl.TabCount)
                    tabControl.SelectTab(index - 1);
                else
                    tabControl.SelectTab(index);

                if (GlobalVariables.OStartUp)
                    StoreDeleteTabs(pathFile, pathFile, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, true, pathFile);

                if (GlobalVariables.OStartUp)
                    DeleteFileSize(tabControl, pathFile, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, index.ToString());
                GlobalVariables.noClear = false;
            }
        }

        /// <summary>
        /// Add new tab with editor.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="textEditorControl"></param>
        /// <param name="e"></param>
        public static void AddNewTab(TabControl tabControl, int index = 0)
        {
            try
            {
                tabControl.Invoke(delegate
                {
                    tabControl.SelectedIndex = index;
                    var tabCount = tabControl.TabCount;
                    var lastIndex = tabControl.SelectedIndex;
                    tabControl.TabPages.Insert(tabCount, $"New Page              ");
                    tabControl.SelectedIndex = lastIndex + tabCount;
                });
            }
            catch { }
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
        ///  Funtiction to store tabs file size.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tempDir"></param>
        /// <param name="fileTabStore"></param>
        public static void DeleteFileSize(TabControl tabControl, string filePath, string tempDir, string fileTabStore, string index)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            if (!Directory.Exists(tempDir))
                return;

            if (!File.Exists(fileTabStore))
                File.WriteAllText(fileTabStore, "");

            FileInfo fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            var line = $"{filePath}|{fileSize}";
            List<string> lines = File.ReadAllLines(fileTabStore).ToList();

            for (int i = 1; i < lines.Count(); i++)
            {
                if (lines[i].StartsWith(filePath) && lines[i].EndsWith(index))
                    lines.Remove(lines[i]);
            }

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
            bool isLine = false;
            foreach (TabPage tabPage in MainForm.Instance.EditorTabControl.TabPages)
            {
                if (tabPage.ToolTipText.Contains(filePath))
                {
                    isLine = true;
                    break;
                }
            }
            if (!remove)
            {
                if (string.IsNullOrEmpty(previewTabPath))
                    previewTabPath = "!@#$$#@%^&\\@#$@#$"; // I din't think I need to do this.
                if (lines.Any(i => i.Contains(previewTabPath)))
                    lines.RemoveAll(i => i.Contains(previewTabPath));
                if (!lines.Any(i => i.Contains(filePath)))
                    lines.Add(line);
            }
            else
            {
                if (lines.Any(i => i.Contains(pathRmove)) && !isLine)
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
                    if (File.Exists(path))
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
                        AddNewTab(tabControl);
                        SelectedEditor.GetSelectedEditor().Text = reader.ReadToEnd();
                        MainForm.Instance.Text = $"{fileInfo.Name} - CIARE {MainForm.Instance.versionName}";
                        MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name}               ";
                        MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = item.Key;
                        var tabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                        StoreFileSize(item.Key, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabIndex);
                    }
                }
                else
                {
                    StoreDeleteTabs("", tabControl.SelectedTab.Text, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, true, item.Key);
                }
            }
            MainForm.Instance.EditorTabControl.SelectTab(1);
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
        public static void SetTransparentTabBar(TabControl tabControl, DrawItemEventArgs e, int red = 0, int green = 0, int blue = 0)
        {
            bool dark = GlobalVariables.darkColor;
            Color BackGroundColorForm = dark ? Color.FromArgb(red, green, blue) : SystemColors.Window;
            SolidBrush fillbrush = new SolidBrush(BackGroundColorForm);
            Rectangle lasttabrect = tabControl.GetTabRect(tabControl.TabPages.Count - 1);
            Rectangle background = new Rectangle();
            background.Location = new Point(lasttabrect.Right, 0);
            background.Size = new Size(tabControl.Right - background.Left, lasttabrect.Height + 1);
            e.Graphics.FillRectangle(fillbrush, background);
        }
    }
}
