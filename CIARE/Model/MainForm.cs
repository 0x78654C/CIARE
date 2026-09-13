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
using System.ComponentModel;
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
using static global::CIARE.Utils.Completion.CompletionParsing;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
        public bool isLoaded = false;

        public static MainForm Instance { get; private set; }

        private string s_args = SplitArguments.GetCommandLineArgs();


        // Feature instances are ready before InitializeComponent wires events.
        internal global::CIARE.LiveShareManage.LiveShareEvents LiveShareEventsFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionParsing CompletionParsingFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionProject CompletionProjectFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionReferences CompletionReferencesFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionResources CompletionResourcesFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionSyntax CompletionSyntaxFeature { get; }
        internal global::CIARE.Utils.Completion.CompletionWorkspace CompletionWorkspaceFeature { get; }
        internal global::CIARE.Utils.Completion.RoslynCompletion RoslynCompletionFeature { get; }
        internal global::CIARE.Utils.Editor.Editor EditorFeature { get; }
        internal global::CIARE.Utils.Editor.EditorCommands EditorCommandsFeature { get; }
        internal global::CIARE.Utils.Editor.EditorLayout EditorLayoutFeature { get; }
        internal global::CIARE.Utils.Editor.Errors ErrorsFeature { get; }
        internal global::CIARE.Utils.Editor.Tabs TabsFeature { get; }
        internal global::CIARE.Utils.Explorer.Explorer ExplorerFeature { get; }
        internal global::CIARE.Utils.Explorer.ExplorerActions ExplorerActionsFeature { get; }
        internal global::CIARE.Utils.Explorer.ExplorerLayout ExplorerLayoutFeature { get; }
        internal global::CIARE.Utils.Explorer.ExplorerTree ExplorerTreeFeature { get; }
        internal global::CIARE.Utils.Explorer.ExplorerWatcher ExplorerWatcherFeature { get; }
        internal global::CIARE.Utils.Navigation.Definitions DefinitionsFeature { get; }
        internal global::CIARE.Utils.Navigation.FindUsages FindUsagesFeature { get; }
        internal global::CIARE.Utils.Navigation.FindUsagesWindow FindUsagesWindowFeature { get; }
        internal global::CIARE.Utils.Navigation.UsageDocuments UsageDocumentsFeature { get; }
        internal global::CIARE.Utils.NuGetManage.NuGet NuGetFeature { get; }
        internal global::CIARE.Utils.NuGetManage.NuGetMetadata NuGetMetadataFeature { get; }
        internal global::CIARE.Utils.Projects.ProjectBuild ProjectBuildFeature { get; }
        internal global::CIARE.Utils.Projects.ProjectCommands ProjectCommandsFeature { get; }
        internal global::CIARE.Utils.Projects.ProjectContext ProjectContextFeature { get; }
        internal global::CIARE.Utils.Projects.ProjectPaths ProjectPathsFeature { get; }
        internal global::CIARE.Utils.Projects.ProjectReferences ProjectReferencesFeature { get; }
        internal global::CIARE.Utils.Projects.StartupProject StartupProjectFeature { get; }
        internal global::CIARE.Utils.Window.UpdateEvents UpdateEventsFeature { get; }
        internal global::CIARE.Utils.Window.WindowState WindowStateFeature { get; }
        internal global::CIARE.Utils.Window.WindowTheme WindowThemeFeature { get; }
        internal Utils.Window.MenuStatusLayout MenuStatusLayoutFeature { get; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
            EditorCommandsFeature.ProcessCmdKey(ref msg, keyData) || base.ProcessCmdKey(ref msg, keyData);

        private void aboutToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.aboutToolStripMenuItem_Click(sender, e);

        private void askAiErrorMenuItem_Click(object sender, global::System.EventArgs e) =>
            ErrorsFeature.askAiErrorMenuItem_Click(sender, e);

        internal global::System.Threading.Tasks.Task BuildExplorerProjectAsync(string projectPath) =>
            ProjectBuildFeature.BuildExplorerProjectAsync(projectPath);

        private void chatGPTCTRLShiftPToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.chatGPTCTRLShiftPToolStripMenuItem_Click(sender, e);

        private void closeAllTabs_Click(object sender, global::System.EventArgs e) =>
            TabsFeature.closeAllTabs_Click(sender, e);

        private void closeAllTabsOne_Click(object sender, global::System.EventArgs e) =>
            TabsFeature.closeAllTabsOne_Click(sender, e);

        private void closeTab_Click(object sender, global::System.EventArgs e) =>
            TabsFeature.closeTab_Click(sender, e);

        private void cmdLinesArgsStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.cmdLinesArgsStripMenuItem_Click(sender, e);

        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.compileToDLLCtrlSfitBToolStripMenuItem_Click(sender, e);

        private void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.compileToexeCtrlShiftBToolStripMenuItem_Click(sender, e);

        private void copyErrorMenuItem_Click(object sender, global::System.EventArgs e) =>
            ErrorsFeature.copyErrorMenuItem_Click(sender, e);

        private void copyStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.copyStripMenuItem_Click(sender, e);

        private void cutStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.cutStripMenuItem_Click(sender, e);

        private void deleteStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.deleteStripMenuItem_Click(sender, e);

        public const string DummyFileName = global::CIARE.Utils.Completion.CompletionParsing.DummyFileName;

        private void EditorTabControl_DrawItem(object sender, global::System.Windows.Forms.DrawItemEventArgs e) =>
            TabsFeature.EditorTabControl_DrawItem(sender, e);

        private void EditorTabControl_HandleCreated(object sender, global::System.EventArgs e) =>
            TabsFeature.EditorTabControl_HandleCreated(sender, e);

        private void EditorTabControl_KeyDown(object sender, global::System.Windows.Forms.KeyEventArgs e) =>
            TabsFeature.EditorTabControl_KeyDown(sender, e);

        private void EditorTabControl_MouseClick(object sender, global::System.Windows.Forms.MouseEventArgs e) =>
            TabsFeature.EditorTabControl_MouseClick(sender, e);

        private void EditorTabControl_MouseDown(object sender, global::System.Windows.Forms.MouseEventArgs e) =>
            TabsFeature.EditorTabControl_MouseDown(sender, e);

        private void EditorTabControl_SelectedIndexChanged(object sender, global::System.EventArgs e) =>
            TabsFeature.EditorTabControl_SelectedIndexChanged(sender, e);

        private void EditorTabControl_Selecting(object sender, global::System.Windows.Forms.TabControlCancelEventArgs e) =>
            TabsFeature.EditorTabControl_Selecting(sender, e);

        private void errorsLV_ColumnClick(object sender, global::System.Windows.Forms.ColumnClickEventArgs e) =>
            ErrorsFeature.errorsLV_ColumnClick(sender, e);

        private void errorsLV_ItemActivate(object sender, global::System.EventArgs e) =>
            ErrorsFeature.errorsLV_ItemActivate(sender, e);

        private void errorsLV_MouseDown(object sender, global::System.Windows.Forms.MouseEventArgs e) =>
            ErrorsFeature.errorsLV_MouseDown(sender, e);

        private void exitToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.exitToolStripMenuItem_Click(sender, e);

        internal global::System.Collections.ArrayList FilterCompletionDataForActiveProject(global::System.Collections.ArrayList completionData) =>
            CompletionWorkspaceFeature.FilterCompletionDataForActiveProject(completionData);

        internal global::System.Collections.ArrayList FilterCompletionDataForActiveProject(global::System.Collections.ArrayList completionData, string activeFilePath) =>
            CompletionWorkspaceFeature.FilterCompletionDataForActiveProject(completionData, activeFilePath);

        internal (string FilePath, int Line) FindDefinition(string name, int offset) =>
            DefinitionsFeature.FindDefinition(name, offset);

        internal (string FilePath, int Line) FindDefinition(string name) =>
            DefinitionsFeature.FindDefinition(name);

        private void finStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.finStripMenuItem_Click(sender, e);

        private void fullScreenToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            WindowStateFeature.fullScreenToolStripMenuItem_Click(sender, e);

        public string GetActiveCompileProjectPath() =>
            ProjectContextFeature.GetActiveCompileProjectPath();

        internal string GetActiveEditorFilePathForCompletion() =>
            EditorFeature.GetActiveEditorFilePathForCompletion();

        public string GetActivePackageInstallProjectPath() =>
            ProjectContextFeature.GetActivePackageInstallProjectPath();

        public string GetActivePackageProjectPath() =>
            ProjectContextFeature.GetActivePackageProjectPath();

        public string GetActiveRunProjectPath() =>
            ProjectContextFeature.GetActiveRunProjectPath();

        internal global::System.Collections.ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix) =>
            RoslynCompletionFeature.GetRoslynCtrlSpaceCompletionData(code, caretOffset, prefix);

        internal global::System.Collections.ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix, string currentFilePath) =>
            RoslynCompletionFeature.GetRoslynCtrlSpaceCompletionData(code, caretOffset, prefix, currentFilePath);

        internal global::System.Collections.ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix, string currentFilePath, global::System.Threading.CancellationToken cancellationToken) =>
            RoslynCompletionFeature.GetRoslynCtrlSpaceCompletionData(code, caretOffset, prefix, currentFilePath, cancellationToken);

        internal global::System.Collections.ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText) =>
            RoslynCompletionFeature.GetRoslynMemberCompletionData(code, caretOffset, expressionText);

        internal global::System.Collections.ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText, string currentFilePath) =>
            RoslynCompletionFeature.GetRoslynMemberCompletionData(code, caretOffset, expressionText, currentFilePath);

        internal global::System.Collections.ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText, string currentFilePath, global::System.Threading.CancellationToken cancellationToken) =>
            RoslynCompletionFeature.GetRoslynMemberCompletionData(code, caretOffset, expressionText, currentFilePath, cancellationToken);

        internal global::System.Collections.ArrayList GetWorkspaceMemberCompletionData(string expression) =>
            CompletionWorkspaceFeature.GetWorkspaceMemberCompletionData(expression);

        internal global::System.Collections.ArrayList GetWorkspaceMethodCompletionData(string prefix) =>
            CompletionWorkspaceFeature.GetWorkspaceMethodCompletionData(prefix);

        private void goToLineStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.goToLineStripMenuItem_Click(sender, e);

        private void HotKeyToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.HotKeyToolStripMenuItem_Click(sender, e);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal global::Microsoft.AspNetCore.SignalR.Client.HubConnection hubConnection
        {
            get => LiveShareEventsFeature.hubConnection;
            set => LiveShareEventsFeature.hubConnection = value;
        }

        internal void InitializeUpdateMenu() =>
            UpdateEventsFeature.InitializeUpdateMenu();

        internal void InitializeUpdates() =>
            UpdateEventsFeature.InitializeUpdates();

        public static bool IsVisualBasic
        {
            get => global::CIARE.Utils.Completion.CompletionParsing.IsVisualBasic;
            set => global::CIARE.Utils.Completion.CompletionParsing.IsVisualBasic = value;
        }

        private void liveShareHostToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            LiveShareEventsFeature.liveShareHostToolStripMenuItem_Click(sender, e);

        private void liveStatusPb_Paint(object sender, global::System.Windows.Forms.PaintEventArgs e) =>
            LiveShareEventsFeature.liveStatusPb_Paint(sender, e);

        private void LoadCStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.LoadCStripMenuItem_Click(sender, e);

        private void MainForm_Resize(object sender, global::System.EventArgs e) =>
            WindowStateFeature.MainForm_Resize(sender, e);

        private void markStartFileChk_CheckedChanged(object sender, global::System.EventArgs e) =>
            WindowStateFeature.markStartFileChk_CheckedChanged(sender, e);

        internal static global::ICSharpCode.SharpDevelop.Dom.DefaultProjectContent myProjectContent
        {
            get => global::CIARE.Utils.Completion.CompletionParsing.myProjectContent;
            set => global::CIARE.Utils.Completion.CompletionParsing.myProjectContent = value;
        }

        internal void NavigateToDefinition(string filePath, int lineNumber) =>
            DefinitionsFeature.NavigateToDefinition(filePath, lineNumber);

        private void newProjectStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            ProjectCommandsFeature.newProjectStripMenuItem_Click(sender, e);

        private void openProjectStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            ProjectCommandsFeature.openProjectStripMenuItem_Click(sender, e);

        private void openToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.openToolStripMenuItem_Click(sender, e);

        private void optionsToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.optionsToolStripMenuItem_Click(sender, e);

        private void outputRBT_MouseWheel(object sender, global::System.Windows.Forms.MouseEventArgs e) =>
            ErrorsFeature.outputRBT_MouseWheel(sender, e);

        private void OutputTabControl_DrawItem(object sender, global::System.Windows.Forms.DrawItemEventArgs e) =>
            ErrorsFeature.OutputTabControl_DrawItem(sender, e);

        internal static global::ICSharpCode.SharpDevelop.Dom.ParseInformation parseInformation
        {
            get => global::CIARE.Utils.Completion.CompletionParsing.parseInformation;
            set => global::CIARE.Utils.Completion.CompletionParsing.parseInformation = value;
        }

        private void pasteStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.pasteStripMenuItem_Click(sender, e);

        public static global::ICSharpCode.SharpDevelop.Dom.ProjectContentRegistry pcRegistry
        {
            get => global::CIARE.Utils.Completion.CompletionParsing.pcRegistry;
            set => global::CIARE.Utils.Completion.CompletionParsing.pcRegistry = value;
        }

        internal string PrepareCodeForNRefactoryCompletion(string code, out int prefixLineOffset, out int wrapLineOffset, out int bodyStartLine) =>
            CompletionSyntaxFeature.PrepareCodeForNRefactoryCompletion(code, out prefixLineOffset, out wrapLineOffset, out bodyStartLine);

        internal string PrepareCodeForNRefactoryCompletion(string code, string filePath, out int prefixLineOffset, out int wrapLineOffset, out int bodyStartLine, global::System.Threading.CancellationToken cancellationToken = default) =>
            CompletionSyntaxFeature.PrepareCodeForNRefactoryCompletion(code, filePath, out prefixLineOffset, out wrapLineOffset, out bodyStartLine, cancellationToken);

        private void redoToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.redoToolStripMenuItem_Click(sender, e);

        private void referenceAddToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.referenceAddToolStripMenuItem_Click(sender, e);

        internal void RefreshActiveCompletionUnit(string code) =>
            CompletionParsingFeature.RefreshActiveCompletionUnit(code);

        internal void RefreshActiveCompletionUnit(string code, string currentFilePath) =>
            CompletionParsingFeature.RefreshActiveCompletionUnit(code, currentFilePath);

        public void RefreshExplorerNuGetPackages() =>
            NuGetFeature.RefreshExplorerNuGetPackages();

        internal void RefreshPreparedActiveCompletionUnit(string preparedCode, string currentFilePath, global::System.Threading.CancellationToken cancellationToken) =>
            CompletionParsingFeature.RefreshPreparedActiveCompletionUnit(preparedCode, currentFilePath, cancellationToken);

        public void RefreshProjectPackageContext(string projectPath, bool restoreProject, bool showRestoreFailure = true) =>
            NuGetFeature.RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure);

        public void RefreshStandaloneReferenceContext() =>
            NuGetFeature.RefreshStandaloneReferenceContext();

        public void RefreshTopMost() =>
            WindowStateFeature.RefreshTopMost();

        public void ReloadRef() =>
            CompletionParsingFeature.ReloadRef();

        private void replaceStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.replaceStripMenuItem_Click(sender, e);

        internal void ResetCompletionWorkspaceIfInactive() =>
            CompletionWorkspaceFeature.ResetCompletionWorkspaceIfInactive();

        internal void ResetCompletionWorkspaceIfInactive(string currentFilePath) =>
            CompletionWorkspaceFeature.ResetCompletionWorkspaceIfInactive(currentFilePath);

        private void runCodePb_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.runCodePb_Click(sender, e);

        private void saveAsStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.saveAsStripMenuItem_Click(sender, e);

        private void saveToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.saveToolStripMenuItem_Click(sender, e);

        private void selectAllStripMenuItem3_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.selectAllStripMenuItem3_Click(sender, e);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public global::ICSharpCode.TextEditor.TextEditorControl selectedEditor
        {
            get => EditorFeature.selectedEditor;
            set => EditorFeature.selectedEditor = value;
        }

        public void SetHighLighter(global::ICSharpCode.TextEditor.TextEditorControl textEditorControl, string highlight, bool persistSetting = true) =>
            WindowThemeFeature.SetHighLighter(textEditorControl, highlight, persistSetting);

        private void showHideExplorerToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.showHideExplorerToolStripMenuItem_Click(sender, e);

        private void showHideSCToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.showHideSCToolStripMenuItem_Click(sender, e);

        private void splitEditorToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.splitEditorToolStripMenuItem_Click(sender, e);

        private void splitVEditorToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.splitVEditorToolStripMenuItem_Click(sender, e);

        private void toolStripMenuItem1_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.toolStripMenuItem1_Click(sender, e);

        private void undoToolStripMenuItem_Click(object sender, global::System.EventArgs e) =>
            EditorCommandsFeature.undoToolStripMenuItem_Click(sender, e);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool visibleSplitContainer
        {
            get => EditorFeature.visibleSplitContainer;
            set => EditorFeature.visibleSplitContainer = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool visibleSplitContainerAutoHide
        {
            get => EditorFeature.visibleSplitContainerAutoHide;
            set => EditorFeature.visibleSplitContainerAutoHide = value;
        }

        internal static string WrapTopLevelStatementsForNRefactory(string code, out int lineOffset, out int bodyStartLine, global::System.Threading.CancellationToken cancellationToken = default) =>
            global::CIARE.Utils.Completion.CompletionSyntax.WrapTopLevelStatementsForNRefactory(code, out lineOffset, out bodyStartLine, cancellationToken);

        public MainForm()
        {
            MenuStatusLayoutFeature = new Utils.Window.MenuStatusLayout(this);
            LiveShareEventsFeature = new global::CIARE.LiveShareManage.LiveShareEvents(this);
            CompletionParsingFeature = new global::CIARE.Utils.Completion.CompletionParsing(this);
            CompletionProjectFeature = new global::CIARE.Utils.Completion.CompletionProject(this);
            CompletionReferencesFeature = new global::CIARE.Utils.Completion.CompletionReferences(this);
            CompletionResourcesFeature = new global::CIARE.Utils.Completion.CompletionResources(this);
            CompletionSyntaxFeature = new global::CIARE.Utils.Completion.CompletionSyntax(this);
            CompletionWorkspaceFeature = new global::CIARE.Utils.Completion.CompletionWorkspace(this);
            RoslynCompletionFeature = new global::CIARE.Utils.Completion.RoslynCompletion(this);
            EditorFeature = new global::CIARE.Utils.Editor.Editor(this);
            EditorCommandsFeature = new global::CIARE.Utils.Editor.EditorCommands(this);
            EditorLayoutFeature = new global::CIARE.Utils.Editor.EditorLayout(this);
            ErrorsFeature = new global::CIARE.Utils.Editor.Errors(this);
            TabsFeature = new global::CIARE.Utils.Editor.Tabs(this);
            ExplorerFeature = new global::CIARE.Utils.Explorer.Explorer(this);
            ExplorerActionsFeature = new global::CIARE.Utils.Explorer.ExplorerActions(this);
            ExplorerLayoutFeature = new global::CIARE.Utils.Explorer.ExplorerLayout(this);
            ExplorerTreeFeature = new global::CIARE.Utils.Explorer.ExplorerTree(this);
            ExplorerWatcherFeature = new global::CIARE.Utils.Explorer.ExplorerWatcher(this);
            DefinitionsFeature = new global::CIARE.Utils.Navigation.Definitions(this);
            FindUsagesFeature = new global::CIARE.Utils.Navigation.FindUsages(this);
            FindUsagesWindowFeature = new global::CIARE.Utils.Navigation.FindUsagesWindow(this);
            UsageDocumentsFeature = new global::CIARE.Utils.Navigation.UsageDocuments(this);
            NuGetFeature = new global::CIARE.Utils.NuGetManage.NuGet(this);
            NuGetMetadataFeature = new global::CIARE.Utils.NuGetManage.NuGetMetadata(this);
            ProjectBuildFeature = new global::CIARE.Utils.Projects.ProjectBuild(this);
            ProjectCommandsFeature = new global::CIARE.Utils.Projects.ProjectCommands(this);
            ProjectContextFeature = new global::CIARE.Utils.Projects.ProjectContext(this);
            ProjectPathsFeature = new global::CIARE.Utils.Projects.ProjectPaths(this);
            ProjectReferencesFeature = new global::CIARE.Utils.Projects.ProjectReferences(this);
            StartupProjectFeature = new global::CIARE.Utils.Projects.StartupProject(this);
            UpdateEventsFeature = new global::CIARE.Utils.Window.UpdateEvents(this);
            WindowStateFeature = new global::CIARE.Utils.Window.WindowState(this);
            WindowThemeFeature = new global::CIARE.Utils.Window.WindowTheme(this);

            InitializeEditor.CreateUserDataDirectory(GlobalVariables.userProfileDirectory, GlobalVariables.markFile);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, "highlight", "C#-Dark");
            var autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.CheckSetAtiveFormState();
            autoStartFile.OpenFilesOnLongOn(ReadArgs(s_args));
            InitializeComponent();
            MenuStatusLayoutFeature.Initialize();
            ErrorsFeature.ConfigureErrorsListView();
            // Buffer individual surfaces. Compositing the entire HWND tree delays
            // editor paints while child controls hold a graphics context.
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        internal void TryBeginInvoke(Action action)
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
                LiveShareEventsFeature._apiConnectionEvents = new ApiConnectionEvents();
                EditorTabControl.SelectedIndex = 1;
                FoldingCode.CheckFoldingCodeStatus(GlobalVariables.registryPath);
                LineNumber.CheckLineNumberStatus(GlobalVariables.registryPath);
                ExplorerFeature.InitializeFileExplorerPane();
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

                ExplorerTreeFeature.RestoreFileExplorerState();
            }
            finally
            {
                EditorTabControl.ResumeLayout(true);
                splitContainer1.ResumeLayout(true);
                ResumeLayout(true);
            }

            // Restore pane sizes before the first paint, once their parents have final bounds.
            ExplorerLayoutFeature.ApplyEditorExplorerMinimumWidths();
            ExplorerLayoutFeature.ApplyFileExplorerLayoutValues();
            ExplorerLayoutFeature.PositionFileExplorerShowButton();
            EditorLayoutFeature.RefreshEditorLayoutBounds();
            isLoaded = true;
            ExplorerLayoutFeature.QueueFileExplorerLayoutApply();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (IsDisposed || Disposing)
                return;

            ReloadRef();
            EditorFeature.QueueEditorTextRefresh();
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

            WindowStateFeature._windowPlacementSaveTimer?.Stop();
            WindowStateFeature.SaveWindowPlacement();

            if (EditorLayoutFeature._editorLayoutRefreshTimer != null)
            {
                EditorLayoutFeature._editorLayoutRefreshTimer.Stop();
                EditorLayoutFeature._editorLayoutRefreshTimer.Tick -= EditorLayoutFeature.OnEditorLayoutRefreshTimer;
                EditorLayoutFeature._editorLayoutRefreshTimer.Dispose();
                EditorLayoutFeature._editorLayoutRefreshTimer = null;
            }
            EditorLayoutFeature._pendingEditorLayoutRefresh = false;
            if (EditorFeature._editorTextRefreshTimer != null)
            {
                EditorFeature._editorTextRefreshTimer.Stop();
                EditorFeature._editorTextRefreshTimer.Tick -= EditorFeature.OnEditorTextRefreshTimer;
                EditorFeature._editorTextRefreshTimer.Dispose();
                EditorFeature._editorTextRefreshTimer = null;
            }
            EditorFeature._pendingEditorTextRefresh = false;

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
            Task.Run(() => LiveShareEventsFeature._apiConnectionEvents.CloseConnection(hubConnection));

            ExplorerLayoutFeature.SaveFileExplorerWidth(force: true);
            ExplorerLayoutFeature.SaveFileExplorerNuGetHeight(force: true);
            ExplorerTreeFeature.SaveFileExplorerExpandedState();
            ExplorerWatcherFeature.StopFileExplorerWatcher();
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
