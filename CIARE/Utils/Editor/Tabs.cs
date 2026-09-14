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
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Projects.ProjectPaths;

namespace CIARE.Utils.Editor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class Tabs
    {
        private readonly MainForm _mainForm;

        internal Tabs(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;

        // Used for tab's auto-resize
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        internal void UpdateOpenTabsAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (_mainForm.EditorTabControl == null)
                return;

            bool selectedTabChanged = false;
            for (int i = 0; i < _mainForm.EditorTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = _mainForm.EditorTabControl.TabPages[i];
                string tabPath = tabPage.ToolTipText?.Trim();
                string renamedPath = GetRenamedExplorerPath(tabPath, oldPath, newPath, renamedDirectory);
                if (string.IsNullOrEmpty(renamedPath))
                    continue;

                tabPage.ToolTipText = renamedPath;
                tabPage.Text = $"{Path.GetFileName(renamedPath)}               ";

                if (GlobalVariables.OStartUp)
                {
                    TabControllerManage.DeleteFileSize(_mainForm.EditorTabControl, tabPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePath, i.ToString());
                    TabControllerManage.StoreFileMD5(renamedPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePath, i);
                    TabControllerManage.StoreDeleteTabs(tabPath, renamedPath, GlobalVariables.userProfileDirectory,
                        GlobalVariables.tabsFilePathAll, i);
                }

                if (ReferenceEquals(tabPage, _mainForm.EditorTabControl.SelectedTab))
                    selectedTabChanged = true;
            }

            if (selectedTabChanged)
                UpdateActiveEditorPathFromSelectedTab();
        }

        private void UpdateActiveEditorPathFromSelectedTab()
        {
            try
            {
                string filePath = _mainForm.EditorTabControl.SelectedTab?.ToolTipText?.Trim();
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                GlobalVariables.openedFilePath = filePath;
                GlobalVariables.openedFileName = Path.GetFileName(filePath);
                if (File.Exists(filePath))
                    FileManage.SetFileMD5(filePath);

                if (!string.IsNullOrEmpty(GlobalVariables.openedFileName))
                    _mainForm.Text = $"{GlobalVariables.openedFileName} : {FileManage.GetFilePath(filePath)} - CIARE {GlobalVariables.versionName}";
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
        internal void NewHotKeyTab(object sender, DoWorkEventArgs e)
        {
            if (_mainForm.outputRBT.ForeColor == Color.Red)
                GlobalVariables.isRed = true;
            TabControllerManage.AddNewTab(_mainForm.EditorTabControl);
        }

        /// <summary>
        /// Create/close new tab with new editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            TabControllerManage.CloseTab(_mainForm.EditorTabControl, e);
        }

        /// <summary>
        /// Handler for resize tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_HandleCreated(object sender, EventArgs e) =>
            SendMessage(_mainForm.EditorTabControl.Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);

        /// <summary>
        /// Autoresize tab names.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (_mainForm.isLoaded && e.TabPageIndex == 0 && _mainForm.EditorTabControl.TabCount > 1)
            {
                // The plus header is an action, never an empty document page.
                // Its mouse handler creates and selects the actual editor tab.
                e.Cancel = true;
                return;
            }
            if (MainForm.Instance == null || e.TabPage == null || e.TabPageIndex <= 0)
                return;

            // Tab counts also change on removal; only an empty page needs a new editor.
            _mainForm.EditorFeature.selectedEditor = e.TabPage.Controls.OfType<TextEditorControl>().FirstOrDefault();
            if (_mainForm.EditorFeature.selectedEditor == null)
            {
                e.TabPage.SuspendLayout();
                try
                {
                    _mainForm.EditorFeature.selectedEditor = new TextEditorControl { Visible = false };
                    _mainForm.EditorLayoutFeature.ConfigureEditorTabPageLayout(e.TabPage);
                    _mainForm.EditorFeature.SetDesignEditor(ref _mainForm.EditorFeature.selectedEditor);
                    e.TabPage.Controls.Add(_mainForm.EditorFeature.selectedEditor);
                    _mainForm.EditorFeature.InitializeEditorSettings(_mainForm.EditorFeature.selectedEditor, e.TabPageIndex);
                    if (e.TabPage is EditorTabPage filePage && filePage.InitialText != null)
                    {
                        string initialText = filePage.InitialText;
                        filePage.InitialText = null;
                        _mainForm.EditorFeature.selectedEditor.Text = initialText;
                    }
                }
                finally
                {
                    e.TabPage.ResumeLayout(true);
                }
                _mainForm.EditorFeature.selectedEditor.Visible = true;
            }
            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();

            string titleTab = e.TabPage.Text.Trim();
            string filePath = e.TabPage.ToolTipText.Trim();

            if (_mainForm.isLoaded)
            {
                try
                {
                    GlobalVariables.textAreaFirst = _mainForm.EditorFeature.selectedEditor.primaryTextArea;
                    GlobalVariables.textAreaSecond = _mainForm.EditorFeature.selectedEditor.secondaryTextArea;
                    InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _mainForm.EditorFeature._editFontSize, _mainForm.EditorFeature.selectedEditor);
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
                _mainForm.Text = $"{titleTab.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";

            }
            else
            {
                _mainForm.Text = $"CIARE {GlobalVariables.versionName}";
            }

            //TODO: Will see in future if is needed
            // FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath);

            // Clear line/col position on new tab switch
            ClearInfoLinescs.ClearLinesInfo();
            LinesManage.GetTotalLinesCount(_mainForm.linesCountLbl);
        }

        /// <summary>
        /// Re-run real-time check whenever the active editor tab changes.
        /// </summary>
        internal void EditorTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_mainForm.isLoaded) return;
            Interlocked.Increment(ref _mainForm.CompletionParsingFeature._completionTextVersion);
            try
            {
                var editor = SelectedEditor.GetSelectedEditor();
                if (editor == null) return;
                CompletionScopeSnapshot completionScope = _mainForm.CompletionWorkspaceFeature.RefreshCompletionScope(_mainForm.EditorFeature.GetActiveEditorFilePath());
                if (string.IsNullOrEmpty(completionScope.WorkspaceFolder))
                {
                    _mainForm.CompletionReferencesFeature.ClearProjectPackageCompletionReferences();
                    _mainForm.CompletionWorkspaceFeature.ClearWorkspaceCompletionData();
                }
                _mainForm.NuGetFeature.RefreshProjectPackageContext(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                _mainForm.EditorFeature.ScheduleCurrentTypeCheck(editor);
            }
            catch { }
        }

        /// <summary>
        /// Draw new tab with X for close after.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw tab on initialize.
            TabControllerManage.DrawTabControl(_mainForm.EditorTabControl, e);

            // Set transparent header bar.
            TabControllerManage.SetTransparentTabBar(_mainForm.EditorTabControl, e,
                GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);

            // Color tab to red if live shared started on that index.
            if (GlobalVariables.apiConnected || GlobalVariables.apiRemoteConnected)
                TabControllerManage.ColorTab(_mainForm.EditorTabControl, GlobalVariables.liveTabIndex, e, Color.Red);
            var taBindex = _mainForm.EditorTabControl.SelectedIndex;

            // Color light green if editor data is unsaved.
            TabControllerManage.ColorTab(_mainForm.EditorTabControl, taBindex, e, Color.LightGray);
        }

        /// <summary>
        /// Show the menu strip on right click tab event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = _mainForm.EditorTabControl.PointToClient(Cursor.Position);
                for (int i = 0; i < _mainForm.EditorTabControl.TabCount; i++)
                {
                    Rectangle r = _mainForm.EditorTabControl.GetTabRect(i);
                    if (r.Contains(p))
                    {
                        if (i >= 1)
                        {
                            _mainForm.EditorTabControl.SelectedIndex = i;
                            _mainForm.tabMenu.Show(_mainForm.EditorTabControl, e.Location);
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
        internal void closeTab_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseTabEvent(_mainForm.EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close event on right click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void closeAllTabs_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseAllTabs(_mainForm.EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close all tabs but not selected one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void closeAllTabsOne_Click(object sender, EventArgs e)
        {
            int index = _mainForm.EditorTabControl.SelectedIndex;
            TabControllerManage.CloseAllTabsOne(_mainForm.EditorTabControl, SelectedEditor.GetSelectedEditor(), index);
        }

        /// <summary>
        /// Cancel left/right and home/end key scroll.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void EditorTabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.Handled = true;
            if (e.KeyCode == Keys.End || e.KeyCode == Keys.Home)
                e.Handled = true;
        }
    }
}
