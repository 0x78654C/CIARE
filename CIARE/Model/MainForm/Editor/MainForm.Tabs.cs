using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;

        // Used for tab's auto-resize
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private void UpdateOpenTabsAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (EditorTabControl == null)
                return;

            bool selectedTabChanged = false;
            for (int i = 0; i < EditorTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = EditorTabControl.TabPages[i];
                string tabPath = tabPage.ToolTipText?.Trim();
                string renamedPath = GetRenamedExplorerPath(tabPath, oldPath, newPath, renamedDirectory);
                if (string.IsNullOrEmpty(renamedPath))
                    continue;

                tabPage.ToolTipText = renamedPath;
                tabPage.Text = $"{Path.GetFileName(renamedPath)}               ";

                if (GlobalVariables.OStartUp)
                {
                    TabControllerManage.DeleteFileSize(EditorTabControl, tabPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePath, i.ToString());
                    TabControllerManage.StoreFileMD5(renamedPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePath, i);
                    TabControllerManage.StoreDeleteTabs(tabPath, renamedPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePathAll, i);
                }

                if (ReferenceEquals(tabPage, EditorTabControl.SelectedTab))
                    selectedTabChanged = true;
            }

            if (selectedTabChanged)
                UpdateActiveEditorPathFromSelectedTab();
        }

        private void UpdateActiveEditorPathFromSelectedTab()
        {
            try
            {
                string filePath = EditorTabControl.SelectedTab?.ToolTipText?.Trim();
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                GlobalVariables.openedFilePath = filePath;
                GlobalVariables.openedFileName = Path.GetFileName(filePath);
                if (File.Exists(filePath))
                    FileManage.SetFileMD5(filePath);

                if (!string.IsNullOrEmpty(GlobalVariables.openedFileName))
                    Text = $"{GlobalVariables.openedFileName} : {FileManage.GetFilePath(filePath)} - CIARE {GlobalVariables.versionName}";
            }
            catch
            {
            }
        }

        /// <summary>
        /// Add new tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewHotKeyTab(object sender, DoWorkEventArgs e)
        {
            if (outputRBT.ForeColor == Color.Red)
                GlobalVariables.isRed = true;
            TabControllerManage.AddNewTab(EditorTabControl);
        }

        /// <summary>
        /// Create/close new tab with new editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            TabControllerManage.CloseTab(EditorTabControl, e);
        }

        /// <summary>
        /// Handler for resize tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_HandleCreated(object sender, EventArgs e) =>
            SendMessage(this.EditorTabControl.Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);

        /// <summary>
        /// Autoresize tab names.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (isLoaded && e.TabPageIndex == 0 && EditorTabControl.TabCount > 1)
            {
                // The plus header is an action, never an empty document page.
                // Its mouse handler creates and selects the actual editor tab.
                e.Cancel = true;
                return;
            }
            if (Instance == null || e.TabPage == null || e.TabPageIndex <= 0)
                return;

            // Tab counts also change on removal; only an empty page needs a new editor.
            selectedEditor = e.TabPage.Controls.OfType<TextEditorControl>().FirstOrDefault();
            if (selectedEditor == null)
            {
                e.TabPage.SuspendLayout();
                try
                {
                    selectedEditor = new TextEditorControl { Visible = false };
                    ConfigureEditorTabPageLayout(e.TabPage);
                    SetDesignEditor(ref selectedEditor);
                    e.TabPage.Controls.Add(selectedEditor);
                    InitializeEditorSettings(selectedEditor, e.TabPageIndex);
                    if (e.TabPage is EditorTabPage filePage && filePage.InitialText != null)
                    {
                        string initialText = filePage.InitialText;
                        filePage.InitialText = null;
                        selectedEditor.Text = initialText;
                    }
                }
                finally
                {
                    e.TabPage.ResumeLayout(true);
                }
                selectedEditor.Visible = true;
            }
            QueueEditorLayoutRefresh();

            string titleTab = e.TabPage.Text.Trim();
            string filePath = e.TabPage.ToolTipText.Trim();

            if (isLoaded)
            {
                try
                {
                    GlobalVariables.textAreaFirst = selectedEditor.primaryTextArea;
                    GlobalVariables.textAreaSecond = selectedEditor.secondaryTextArea;
                    InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, selectedEditor);
                }
                catch
                {
                    // Ignore index 0 error. The area will be set after load on first split anyway.
                }
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                GlobalVariables.openedFilePath = filePath;
                var fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                GlobalVariables.openedFileName = fileInfo.Name;
                if (File.Exists(filePath))
                {
                    try
                    {
                        FileManage.SetFileMD5(filePath);
                    }
                    catch { }
                }
            }
            if (!titleTab.Contains("New Pag") && !titleTab.Contains("+"))
            {
                this.Text = $"{titleTab.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";

            }
            else
            {
                this.Text = $"CIARE {GlobalVariables.versionName}";
            }

            //TODO: Will see in future if is needed
            // FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath);

            // Clear line/col position on new tab switch
            ClearInfoLinescs.ClearLinesInfo();
            LinesManage.GetTotalLinesCount(linesCountLbl);
        }

        /// <summary>
        /// Re-run real-time check whenever the active editor tab changes.
        /// </summary>
        private void EditorTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isLoaded) return;
            Interlocked.Increment(ref _completionTextVersion);
            try
            {
                var editor = SelectedEditor.GetSelectedEditor();
                if (editor == null) return;
                CompletionScopeSnapshot completionScope = RefreshCompletionScope(GetActiveEditorFilePath());
                if (string.IsNullOrEmpty(completionScope.WorkspaceFolder))
                {
                    ClearProjectPackageCompletionReferences();
                    ClearWorkspaceCompletionData();
                }
                RefreshProjectPackageContext(GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                ScheduleCurrentTypeCheck(editor);
            }
            catch { }
        }

        /// <summary>
        /// Draw new tab with X for close after.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw tab on initialize.
            TabControllerManage.DrawTabControl(EditorTabControl, e);

            // Set transparent header bar.
            TabControllerManage.SetTransparentTabBar(EditorTabControl, e,
                GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);

            // Color tab to red if live shared started on that index.
            if (GlobalVariables.apiConnected || GlobalVariables.apiRemoteConnected)
                TabControllerManage.ColorTab(EditorTabControl, GlobalVariables.liveTabIndex, e, Color.Red);
            var taBindex = EditorTabControl.SelectedIndex;

            // Color light green if editor data is unsaved.
            TabControllerManage.ColorTab(EditorTabControl, taBindex, e, Color.LightGray);
        }

        /// <summary>
        /// Show the menu strip on right click tab event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = EditorTabControl.PointToClient(Cursor.Position);
                for (int i = 0; i < EditorTabControl.TabCount; i++)
                {
                    Rectangle r = EditorTabControl.GetTabRect(i);
                    if (r.Contains(p))
                    {
                        if (i >= 1)
                        {
                            EditorTabControl.SelectedIndex = i;
                            tabMenu.Show(EditorTabControl, e.Location);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Close event on right click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeTab_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseTabEvent(EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close event on right click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeAllTabs_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseAllTabs(EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close all tabs but not selected one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeAllTabsOne_Click(object sender, EventArgs e)
        {
            int index = EditorTabControl.SelectedIndex;
            TabControllerManage.CloseAllTabsOne(EditorTabControl, SelectedEditor.GetSelectedEditor(), index);
        }

        /// <summary>
        /// Cancel left/right and home/end key scroll.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.Handled = true;
            if (e.KeyCode == Keys.End || e.KeyCode == Keys.Home)
                e.Handled = true;
        }
    }
}
