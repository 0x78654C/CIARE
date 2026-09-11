/*
       Description: Simple text editor for Windows with C# runtime compiler and code execution using Roslyn. 
       Useful to run code on the fly and get instant result.

       This app is distributed under the MIT License.
       Copyright © 2022 - 2026 x_coding. All rights reserved.

       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
*/
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using CIARE.Utils.Encryption;
using CIARE.Utils.FilesOpenOS;
using CIARE.Utils.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Dom = ICSharpCode.SharpDevelop.Dom;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
        public bool isLoaded = false;

        public static MainForm Instance { get; private set; }

        private string s_args = SplitArguments.GetCommandLineArgs();

        public MainForm()
        {
            InitializeEditor.CreateUserDataDirectory(GlobalVariables.userProfileDirectory, GlobalVariables.markFile);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, "highlight", "C#-Dark");
            var autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.CheckSetAtiveFormState();
            autoStartFile.OpenFilesOnLongOn(ReadArgs(s_args));
            InitializeComponent();
            ConfigureErrorsListView();
            // Buffer individual surfaces. Compositing the entire HWND tree delays
            // editor paints while child controls hold a graphics context.
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        private void TryBeginInvoke(Action action)
        {
            if (action == null || IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Instance = this;
            SuspendLayout();
            splitContainer1.SuspendLayout();
            EditorTabControl.SuspendLayout();
            try
            {
                this.Text = $"CIARE {GlobalVariables.versionName}";
                TabControllerManage.CleanFileSizeStoreFile(GlobalVariables.tabsFilePath);
                ThemeManager.LoadExternalThemes();
                InitializeEditor.ReadEditorWindowSize(this, GlobalVariables.registryPath);
                InitializeEditor.ReadOutputWindowState(GlobalVariables.registryPath, splitContainer1);
                InitializeEditor.WinLoginState(GlobalVariables.registryPath, GlobalVariables.OWinLogin, out GlobalVariables.OWinLoginState);
                CodeCompletion.CheckCodeCompletion(GlobalVariables.registryPath);
                myProjectContent = new Dom.DefaultProjectContent { Language = CurrentLanguageProperties };
                _apiConnectionEvents = new ApiConnectionEvents();
                EditorTabControl.SelectedIndex = 1;
                FoldingCode.CheckFoldingCodeStatus(GlobalVariables.registryPath);
                LineNumber.CheckLineNumberStatus(GlobalVariables.registryPath);
                InitializeFileExplorerPane();
                Console.SetOut(new ControlWriter(outputRBT));
                InitializeEditor.GenerateLiveSessionId();
                InitializeEditor.CleanNugetFolder(GlobalVariables.downloadNugetPath);
                StartFilesOS.CheckOSStartFile(GlobalVariables.registryPath);
                StartFilesOS.CheckWinLoginState(GlobalVariables.registryPath);
                BuildConfig.CheckConfig(GlobalVariables.registryPath);
                BuildConfig.CheckPlatform(GlobalVariables.registryPath);
                TargetFramework.CheckFramework(GlobalVariables.registryPath);
                LiveShare.CheckApiLiveShare(GlobalVariables.registryPath);
                OpenAISetting.CheckOpenAIData(GlobalVariables.registryPath);
                UnsafeCode.CheckUnsafeStatus(GlobalVariables.registryPath);
                Publish.CheckPublishStatus(GlobalVariables.registryPath);
                if (GlobalVariables.OStartUp)
                    TabControllerManage.ReadTabs(EditorTabControl, SelectedEditor.GetSelectedEditor(), GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll);
                else
                    TabControllerManage.CleanStoredTabs(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll);
                linesCountLbl.Text = string.Empty;
                linesPositionLbl.Text = string.Empty;

                //File open via parameters(Open with option..)
                string arg = ReadArgs(s_args);
                FileManage.OpenFileFromArgs(arg, EditorTabControl);
                //----------------------------------

                if (!GlobalVariables.isCLIOpen)
                {
                    InitializeEditor.GetTabIndexPosLine(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, EditorTabControl);
                }
                else
                {
                    TabControllerManage.IsFileOpenedInTab(MainForm.Instance.EditorTabControl, GlobalVariables.openedFilePath);
                }
                // Get last opened tab MD5.
                if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    GlobalVariables.openedFileMD5 = FileHash.GetFileHash(SelectedEditor.GetSelectedEditor().Text);

                if (GlobalVariables.darkColor)
                {
                    FrmColorMod.EnableDarkTitleBar(this.Handle);
                    FrmColorMod.EnableDarkTitleBar(EditorTabControl.Handle);
                }

                RestoreFileExplorerState();
            }
            finally
            {
                EditorTabControl.ResumeLayout(true);
                splitContainer1.ResumeLayout(true);
                ResumeLayout(true);
            }

            // Restore pane sizes before the first paint, once their parents have final bounds.
            ApplyEditorExplorerMinimumWidths();
            ApplyFileExplorerLayoutValues();
            PositionFileExplorerShowButton();
            RefreshEditorLayoutBounds();
            isLoaded = true;
            QueueFileExplorerLayoutApply();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (IsDisposed || Disposing)
                return;

            ReloadRef();
            QueueEditorTextRefresh();
        }

        /// <summary>
        /// Read arguments on CIARE start.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string ReadArgs(string args)
        {
            if (args.StartsWith("\"") && args.Length > 1)
            {
                int ix = args.IndexOf("\"", 1);

                if (ix != -1)
                {
                    args = args.Substring(ix + 1).TrimStart();
                }
            }
            else
            {
                int ix = args.IndexOf(" ");

                if (ix != -1)
                {
                    args = args.Substring(ix + 1).TrimStart();
                }
            }
            args = args.Trim('"');
            return args;
        }

        /// <summary>
        /// Run the method for unsaved data check on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Store tab text of current opened tab.
            var toolTipText = EditorTabControl.SelectedTab.ToolTipText.Trim();
            if (toolTipText.StartsWith("Add Tab"))
                TabControllerManage.StoreTabPosition(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, string.Empty);
            else
                TabControllerManage.StoreTabPosition(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, EditorTabControl.SelectedTab.ToolTipText.Trim());

            FileManage.ManageUnsavedData(SelectedEditor.GetSelectedEditor(), 0, true);
            if (GlobalVariables.noClear)
            {
                e.Cancel = true;
                GlobalVariables.noClear = false;
                return;
            }
            else
                e.Cancel = false;

            _windowPlacementSaveTimer?.Stop();
            SaveWindowPlacement();

            if (_editorLayoutRefreshTimer != null)
            {
                _editorLayoutRefreshTimer.Stop();
                _editorLayoutRefreshTimer.Tick -= OnEditorLayoutRefreshTimer;
                _editorLayoutRefreshTimer.Dispose();
                _editorLayoutRefreshTimer = null;
            }
            _pendingEditorLayoutRefresh = false;
            if (_editorTextRefreshTimer != null)
            {
                _editorTextRefreshTimer.Stop();
                _editorTextRefreshTimer.Tick -= OnEditorTextRefreshTimer;
                _editorTextRefreshTimer.Dispose();
                _editorTextRefreshTimer = null;
            }
            _pendingEditorTextRefresh = false;

            // Delete temp mark file.
            if (GetCiareProcesses() == 1)
            {
                AutoStartFile autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, "");
                autoStartFile.DelTempFile();
            }

            // Set if form is not active anymore if there is not process left
            if (ProcessRun.CheckActiveProcessCount("CIARE") <= 1)
            {
                CrashCheck crashCheck = new CrashCheck(GlobalVariables.registryPath, GlobalVariables.activeForm);
                crashCheck.SetClosedFormState();
            }

            // Stop Live share if connected.
            Task.Run(() => _apiConnectionEvents.CloseConnection(hubConnection));

            SaveFileExplorerWidth(force: true);
            SaveFileExplorerNuGetHeight(force: true);
            SaveFileExplorerExpandedState();
            StopFileExplorerWatcher();
        }

        /// <summary>
        /// Get count of all CIARE procesess opened.
        /// </summary>
        /// <returns></returns>
        private int GetCiareProcesses() => Process.GetProcessesByName("CIARE").Count();

        /// <summary>
        /// Check edited opened files by external application when CIARE is on Top Most event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (!isLoaded)
                return;

            try
            {
                FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath, GlobalVariables.savedFileNoMD5Check);
            }
            catch
            {
            }
        }
    }
}
