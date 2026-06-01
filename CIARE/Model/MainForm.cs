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
using CIARE.Utils;
using CIARE.GUI;
using CIARE.Utils.Options;
using ICSharpCode.TextEditor;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;
using NRefactory = ICSharpCode.NRefactory;
using Dom = ICSharpCode.SharpDevelop.Dom;
using System.Threading;
using CIARE.Roslyn;
using CIARE.Utils.FilesOpenOS;
using System.Runtime.Versioning;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using System.Xml.Linq;
using CIARE.LiveShareManage;
using System.Threading.Tasks;
using CIARE.Utils.OpenAISettings;
using CIARE.Utils.NuGetManage;
using Button = System.Windows.Forms.Button;
using CIARE.Model;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;
using CIARE.Utils.Encryption;
using System.Collections;
using System.Collections.Concurrent;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CIARE.Properties;
using VBFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using VBRecycleOption = Microsoft.VisualBasic.FileIO.RecycleOption;
using VBUIOption = Microsoft.VisualBasic.FileIO.UIOption;


namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED reduces flicker on resize and redraws.
        //        return cp;
        //    }
        //}
        public HubConnection hubConnection;
        public bool visibleSplitContainer = false;
        public bool visibleSplitContainerAutoHide = false;
        public bool isLoaded = false;
        private string _editFontSize = "editorFontSizeZoom";
        public static MainForm Instance { get; private set; }
        public static Dom.ProjectContentRegistry pcRegistry;
        internal static Dom.DefaultProjectContent myProjectContent;
        internal static Dom.ParseInformation parseInformation = new Dom.ParseInformation();
        public static bool IsVisualBasic = false;
        Dom.ICompilationUnit lastCompilationUnit;
        public const string DummyFileName = "edited.cs";
        static readonly Dom.LanguageProperties CurrentLanguageProperties = IsVisualBasic ? Dom.LanguageProperties.VBNet : Dom.LanguageProperties.CSharp;
        Thread parserThread;
        private string s_args = SplitArguments.GetCommandLineArgs();
        private ApiConnectionEvents _apiConnectionEvents;
        public TextEditorControl selectedEditor;
        TextEditorControl dynamicTextEdtior;
        private int _countTabs = 0;
        private bool _isFullScreen = false;
        private FormBorderStyle _savedBorderStyle;
        private FormWindowState _savedWindowState;
        private bool _markStartFileChkVisible;
        BackgroundWorker worker;
        private string[] _filesDrag;
        private const int FileExplorerDefaultWidth = 280;
        private const int FileExplorerMinWidth = 220;
        private const int EditorPaneMinWidth = 180;
        private const int FileExplorerDefaultNuGetHeight = 350;
        private const int FileExplorerNuGetMinHeight = 90;
        private const int FileExplorerTreeMinHeight = 120;
        private const int FileExplorerLayoutApplyMaxAttempts = 20;
        private const int FileExplorerLayoutApplyStabilizeAttempts = 4;
        private const int FileExplorerLayoutApplyInterval = 50;
        private const string FileExplorerLoadingTag = "__loading__";
        private const string FileExplorerPathKey = "fileExplorerPath";
        private const string FileExplorerVisibleKey = "fileExplorerVisible";
        private const string FileExplorerWidthKey = "fileExplorerWidth";
        private const string FileExplorerNuGetHeightKey = "fileExplorerNuGetHeight";
        private static readonly string FileExplorerExpandedPathsFilePath =
            Path.Combine(GlobalVariables.userProfileDirectory, "fileExplorerExpandedPaths.cDat");
        private static readonly string FileExplorerLayoutFilePath =
            Path.Combine(GlobalVariables.userProfileDirectory, "fileExplorerLayout.cDat");
        private Panel _editorWorkspacePanel;
        private SplitContainer _editorExplorerSplitContainer;
        private Panel _fileExplorerPanel;
        private TableLayoutPanel _fileExplorerHeader;
        private Label _fileExplorerTitleLabel;
        private Button _fileExplorerOpenFolderButton;
        private Button _fileExplorerHideButton;
        private Button _fileExplorerShowButton;
        private SplitContainer _fileExplorerContentSplitContainer;
        private TreeView _fileExplorerTree;
        private Panel _fileExplorerNuGetPanel;
        private Label _fileExplorerNuGetTitleLabel;
        private ListView _fileExplorerNuGetList;
        private ColumnHeader _fileExplorerNuGetPackageColumn;
        private ColumnHeader _fileExplorerNuGetVersionColumn;
        private ColumnHeader _fileExplorerNuGetUpdateColumn;
        private ColumnHeader _fileExplorerNuGetStatusColumn;
        private ContextMenuStrip _fileExplorerContextMenu;
        private ToolStripMenuItem _fileExplorerAddProjectMenuItem;
        private ToolStripMenuItem _fileExplorerAddProjectReferenceMenuItem;
        private ToolStripMenuItem _fileExplorerRemoveProjectReferenceMenuItem;
        private ToolStripMenuItem _fileExplorerSetStartupProjectMenuItem;
        private ToolStripMenuItem _fileExplorerNewFileMenuItem;
        private ToolStripMenuItem _fileExplorerNewFolderMenuItem;
        private ToolStripSeparator _fileExplorerProjectSeparator;
        private ToolStripSeparator _fileExplorerContextSeparator;
        private ToolStripMenuItem _fileExplorerRenameMenuItem;
        private ToolStripMenuItem _fileExplorerDeleteMenuItem;
        private ContextMenuStrip _fileExplorerNuGetContextMenu;
        private ToolStripMenuItem _fileExplorerNuGetUpdateMenuItem;
        private ToolStripMenuItem _fileExplorerNuGetRemoveMenuItem;
        private TreeNode _fileExplorerContextNode;
        private ImageList _fileExplorerImageList;
        private string _fileExplorerRootPath = string.Empty;
        private string _fileExplorerStartupProjectPath = string.Empty;
        private int _fileExplorerWidth = FileExplorerDefaultWidth;
        private int _fileExplorerNuGetHeight = FileExplorerDefaultNuGetHeight;
        private FileSystemWatcher _fileExplorerWatcher;
        private System.Windows.Forms.Timer _fileExplorerRefreshTimer;
        private System.Windows.Forms.Timer _fileExplorerNuGetRefreshTimer;
        private string _pendingProjectPackageRefreshPath = string.Empty;
        private bool _pendingProjectPackageRestore;
        private bool _pendingProjectPackageShowRestoreFailure;
        private int _projectPackageRefreshVersion;
        private int _fileExplorerNuGetListRefreshVersion;
        private bool _suppressFileExplorerExpandedStateSave;
        private bool _suppressFileExplorerLayoutSave;
        private bool _applyingFileExplorerLayout;
        private bool _pendingFileExplorerLayoutApply;
        private int _fileExplorerLayoutApplyAttempts;
        private System.Windows.Forms.Timer _fileExplorerLayoutApplyTimer;
        private bool _fileExplorerLayoutReadyForUserSave;
        private bool _fileExplorerWidthDragInProgress;
        private bool _fileExplorerNuGetHeightDragInProgress;
        private bool _refreshingEditorLayoutBounds;
        private bool _pendingEditorLayoutRefresh;
        private System.Windows.Forms.Timer _editorLayoutRefreshTimer;
        private readonly HashSet<string> _pendingRefreshPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const int WorkspaceCompletionMethodLimit = 300;
        private const int RoslynCompletionSourceFileLimit = 300;
        private static readonly string[] CompletionImplicitUsingNamespaces =
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks"
        };
        private readonly object _completionDataLock = new object();
        private int _completionWorkspaceVersion;
        private string _completionWorkspaceKey = string.Empty;
        private string _completionFileKey = string.Empty;
        private readonly Dictionary<string, Dom.ICompilationUnit> _workspaceCompilationUnits
            = new Dictionary<string, Dom.ICompilationUnit>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dom.IProjectContent> _projectPackageCompletionContents
            = new Dictionary<string, Dom.IProjectContent>(StringComparer.OrdinalIgnoreCase);
        private readonly List<WorkspaceCompletionClass> _workspaceCompletionClasses
            = new List<WorkspaceCompletionClass>();
        private readonly List<WorkspaceCompletionItem> _topLevelLocalFunctions
            = new List<WorkspaceCompletionItem>();
        private Form _findUsagesWindow;
        private static readonly object _usageReferencesLock = new object();
        private static List<MetadataReference> _usagePlatformReferences;
        private static List<MetadataReference> _usageCustomReferences = new List<MetadataReference>();
        private static string _usageCustomReferenceKey = string.Empty;
        private static readonly object _usageDocumentCacheLock = new object();
        private static readonly Dictionary<string, UsageDocumentCacheEntry> _usageDocumentCache
            = new Dictionary<string, UsageDocumentCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private const int UsageDocumentCacheLimit = 128;
        private const long UsageFastReadMaxFileBytes = 256L * 1024L;
        private const long UsageDocumentCacheMaxFileBytes = 1024L * 1024L;
        private bool _completionRegistryInitialized;
        private static readonly PropertyInfo DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);


        // Used for tab's auto-resize
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;
        //----------------------------

        public MainForm()
        {
            InitializeEditor.CreateUserDataDirectory(GlobalVariables.userProfileDirectory, GlobalVariables.markFile);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, "highlight", "C#-Dark");
            var autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.CheckSetAtiveFormState();
            autoStartFile.OpenFilesOnLongOn(ReadArgs(s_args));
            InitializeComponent();
            ConfigureErrorsListView();
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        private void ConfigureErrorsListView()
        {
            if (errorsLV == null)
                return;

            if (errorsLV.Columns.Count == 0)
            {
                errorsLV.Columns.Add(string.Empty, 34, HorizontalAlignment.Center);
                errorsLV.Columns.Add("Line", 64, HorizontalAlignment.Right);
                errorsLV.Columns.Add("Code", 84, HorizontalAlignment.Left);
                errorsLV.Columns.Add("Message", 320, HorizontalAlignment.Left);
            }

            errorsLV.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            errorsLV.ShowItemToolTips = true;
            if (errorsLV.ListViewItemSorter == null)
                errorsLV.ListViewItemSorter = new CIARE.GUI.ListViewColumnSorter();
            errorsLV.Resize += errorsLV_Resize;
            ResizeErrorsListViewColumns();
        }

        private void errorsLV_Resize(object sender, EventArgs e)
        {
            ResizeErrorsListViewColumns();
        }

        private void ResizeErrorsListViewColumns()
        {
            if (errorsLV == null || errorsLV.IsDisposed || errorsLV.Columns.Count < 4)
                return;

            int availableWidth = errorsLV.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6;
            if (availableWidth <= 0)
                return;

            int iconWidth = 34;
            int lineWidth = 64;
            int codeWidth = 84;
            int messageWidth = Math.Max(180, availableWidth - iconWidth - lineWidth - codeWidth);

            errorsLV.Columns[0].Width = iconWidth;
            errorsLV.Columns[1].Width = lineWidth;
            errorsLV.Columns[2].Width = codeWidth;
            errorsLV.Columns[3].Width = messageWidth;
        }

        /// <summary>
        /// Initiliaze settings for dynamic text edtior.
        /// </summary>
        /// <param name="index"></param>
        private void Initiliaze(int index = 1)
        {
            EditorTabControl.SelectedIndex = index;
            int selectedTab = EditorTabControl.SelectedIndex;
            int countTabs = EditorTabControl.TabCount - 1;
            Control ctrl = EditorTabControl.Controls[countTabs].Controls[0];
            selectedEditor = ctrl as TextEditorControl;
            selectedEditor.TextEditorProperties.StoreZoomSize = true;
            selectedEditor.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            InitializeEditor.ReadEditorWindowSize(this, GlobalVariables.registryPath);
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, selectedEditor);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, selectedEditor);
            InitializeEditor.ReadOutputWindowState(GlobalVariables.registryPath, splitContainer1);
            InitializeEditor.WinLoginState(GlobalVariables.registryPath, GlobalVariables.OWinLogin, out GlobalVariables.OWinLoginState);
            FoldingCode.CheckFoldingCodeStatus(GlobalVariables.registryPath);
            LineNumber.CheckLineNumberStatus(GlobalVariables.registryPath);
            SetCodeCompletion(index);
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;
            HookEditorAskAI(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Wire the "Ask AI" context-menu action for an editor instance.
        /// </summary>
        private static void HookEditorAskAI(TextEditorControl editor)
        {
            if (editor == null) return;
            var menu = editor.ActiveTextAreaControl.ContextMenuStrip as ICSharpCode.TextEditor.ContextMenu;
            if (menu != null)
            {
                menu.AskAIAction = () => AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
                menu.FindUsagesAction = () => Instance?.FindUsagesAtCaret();
            }
        }

        private void InitializeFileExplorerPane()
        {
            if (_editorExplorerSplitContainer != null)
                return;

            LoadFileExplorerLayoutValues();

            splitContainer1.Panel1.SuspendLayout();
            EditorTabControl.SuspendLayout();

            splitContainer1.Panel1.Controls.Remove(EditorTabControl);

            _editorWorkspacePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = GetEditorSurfaceBackColor()
            };

            _editorExplorerSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 5,
                BackColor = GetEditorSurfaceBackColor()
            };
            _editorExplorerSplitContainer.Panel1.Padding = Padding.Empty;
            _editorExplorerSplitContainer.Panel2.Padding = Padding.Empty;
            _editorExplorerSplitContainer.Panel1.BackColor = GetEditorSurfaceBackColor();
            _editorExplorerSplitContainer.Panel2.BackColor = GetEditorSurfaceBackColor();
            _editorExplorerSplitContainer.Panel1.Resize += (sender, args) => QueueEditorLayoutRefresh();
            _editorExplorerSplitContainer.SizeChanged += (sender, args) =>
            {
                OnFileExplorerLayoutContainerSizeChanged();
            };
            _editorExplorerSplitContainer.SplitterMoving += (sender, args) =>
            {
                _fileExplorerWidthDragInProgress = true;
                CancelPendingFileExplorerLayoutApplyForUserResize();
            };
            _editorExplorerSplitContainer.SplitterMoved += (sender, args) =>
            {
                if (_fileExplorerWidthDragInProgress)
                {
                    _fileExplorerWidthDragInProgress = false;
                    QueueFileExplorerWidthSave();
                }

                QueueEditorLayoutRefresh();
            };
            _editorExplorerSplitContainer.MouseUp += (sender, args) =>
            {
                if (_fileExplorerWidthDragInProgress)
                {
                    _fileExplorerWidthDragInProgress = false;
                    QueueFileExplorerWidthSave();
                }
            };

            _fileExplorerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = GetEditorSurfaceBackColor()
            };

            _fileExplorerContentSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 4,
                Panel2MinSize = FileExplorerNuGetMinHeight,
                BackColor = GetEditorSurfaceBackColor()
            };
            _fileExplorerContentSplitContainer.Panel1.BackColor = GetEditorSurfaceBackColor();
            _fileExplorerContentSplitContainer.Panel2.BackColor = GetEditorSurfaceBackColor();
            _fileExplorerContentSplitContainer.SizeChanged += (sender, args) =>
            {
                OnFileExplorerLayoutContainerSizeChanged();
            };
            _fileExplorerContentSplitContainer.SplitterMoving +=
                (sender, args) =>
                {
                    _fileExplorerNuGetHeightDragInProgress = true;
                    CancelPendingFileExplorerLayoutApplyForUserResize();
                };
            _fileExplorerContentSplitContainer.SplitterMoved +=
                (sender, args) =>
                {
                    if (_fileExplorerNuGetHeightDragInProgress)
                        QueueFileExplorerNuGetHeightSave();
                };
            _fileExplorerContentSplitContainer.MouseUp +=
                (sender, args) =>
                {
                    if (!_fileExplorerNuGetHeightDragInProgress)
                        return;

                    _fileExplorerNuGetHeightDragInProgress = false;
                    QueueFileExplorerNuGetHeightSave();
                };

            _fileExplorerHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4, 4, 4, 3)
            };
            _fileExplorerHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _fileExplorerHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            _fileExplorerHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            _fileExplorerHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _fileExplorerTitleLabel = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Text = "Explorer",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _fileExplorerOpenFolderButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Open Folder",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 2, 0),
                UseVisualStyleBackColor = false
            };
            toolTip1.SetToolTip(_fileExplorerOpenFolderButton, "Open folder");
            _fileExplorerOpenFolderButton.Click += fileExplorerOpenFolderButton_Click;

            _fileExplorerHideButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = ">>",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 0, 0),
                UseVisualStyleBackColor = false
            };
            toolTip1.SetToolTip(_fileExplorerHideButton, "Hide file explorer");
            _fileExplorerHideButton.Click += (sender, args) => ToggleFileExplorer(false);

            _fileExplorerTree = new TreeView
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowNodeToolTips = true,
                ImageList = CreateFileExplorerImageList()
            };
            _fileExplorerTree.BeforeExpand += fileExplorerTree_BeforeExpand;
            _fileExplorerTree.AfterExpand += fileExplorerTree_AfterExpand;
            _fileExplorerTree.AfterCollapse += fileExplorerTree_AfterCollapse;
            _fileExplorerTree.AfterSelect += fileExplorerTree_AfterSelect;
            _fileExplorerTree.MouseDown += fileExplorerTree_MouseDown;
            _fileExplorerTree.NodeMouseDoubleClick += fileExplorerTree_NodeMouseDoubleClick;
            _fileExplorerTree.KeyDown += fileExplorerTree_KeyDown;

            _fileExplorerAddProjectMenuItem = new ToolStripMenuItem
            {
                Text = "Add New Project..."
            };
            _fileExplorerAddProjectMenuItem.Click += fileExplorerAddProjectMenuItem_Click;

            _fileExplorerAddProjectReferenceMenuItem = new ToolStripMenuItem
            {
                Text = "Add Project Reference..."
            };
            _fileExplorerAddProjectReferenceMenuItem.Click += fileExplorerAddProjectReferenceMenuItem_Click;

            _fileExplorerRemoveProjectReferenceMenuItem = new ToolStripMenuItem
            {
                Text = "Remove Project Reference..."
            };
            _fileExplorerRemoveProjectReferenceMenuItem.Click += fileExplorerRemoveProjectReferenceMenuItem_Click;

            _fileExplorerSetStartupProjectMenuItem = new ToolStripMenuItem
            {
                Text = "Set as Startup Project"
            };
            _fileExplorerSetStartupProjectMenuItem.Click += fileExplorerSetStartupProjectMenuItem_Click;

            _fileExplorerNewFileMenuItem = new ToolStripMenuItem
            {
                Text = "New C# File..."
            };
            _fileExplorerNewFileMenuItem.Click += fileExplorerNewFileMenuItem_Click;

            _fileExplorerNewFolderMenuItem = new ToolStripMenuItem
            {
                Text = "New Folder..."
            };
            _fileExplorerNewFolderMenuItem.Click += fileExplorerNewFolderMenuItem_Click;

            _fileExplorerRenameMenuItem = new ToolStripMenuItem
            {
                Text = "Rename..."
            };
            _fileExplorerRenameMenuItem.Click += fileExplorerRenameMenuItem_Click;

            _fileExplorerDeleteMenuItem = new ToolStripMenuItem
            {
                Text = "Delete"
            };
            _fileExplorerDeleteMenuItem.Click += fileExplorerDeleteMenuItem_Click;

            _fileExplorerContextMenu = new ContextMenuStrip(components);
            _fileExplorerContextMenu.Opening += fileExplorerContextMenu_Opening;
            _fileExplorerContextMenu.Items.Add(_fileExplorerAddProjectMenuItem);
            _fileExplorerContextMenu.Items.Add(_fileExplorerAddProjectReferenceMenuItem);
            _fileExplorerContextMenu.Items.Add(_fileExplorerRemoveProjectReferenceMenuItem);
            _fileExplorerContextMenu.Items.Add(_fileExplorerSetStartupProjectMenuItem);
            _fileExplorerProjectSeparator = new ToolStripSeparator();
            _fileExplorerContextMenu.Items.Add(_fileExplorerProjectSeparator);
            _fileExplorerContextMenu.Items.Add(_fileExplorerNewFileMenuItem);
            _fileExplorerContextMenu.Items.Add(_fileExplorerNewFolderMenuItem);
            _fileExplorerContextSeparator = new ToolStripSeparator();
            _fileExplorerContextMenu.Items.Add(_fileExplorerContextSeparator);
            _fileExplorerContextMenu.Items.Add(_fileExplorerRenameMenuItem);
            _fileExplorerContextMenu.Items.Add(_fileExplorerDeleteMenuItem);
            _fileExplorerTree.ContextMenuStrip = _fileExplorerContextMenu;

            _fileExplorerNuGetPackageColumn = new ColumnHeader { Text = "Package", Width = 130 };
            _fileExplorerNuGetVersionColumn = new ColumnHeader { Text = "Version", Width = 80 };
            _fileExplorerNuGetUpdateColumn = new ColumnHeader { Text = "Update", Width = 80 };
            _fileExplorerNuGetStatusColumn = new ColumnHeader { Text = "Status", Width = 70 };

            _fileExplorerNuGetTitleLabel = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(6, 4, 4, 0),
                Text = "NuGet packages",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _fileExplorerNuGetList = new ListView
            {
                BorderStyle = BorderStyle.None,
                Columns =
                {
                    _fileExplorerNuGetPackageColumn,
                    _fileExplorerNuGetVersionColumn,
                    _fileExplorerNuGetUpdateColumn,
                    _fileExplorerNuGetStatusColumn
                },
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                View = View.Details
            };
            _fileExplorerNuGetList.Resize += (sender, args) => ResizeFileExplorerNuGetColumns();
            _fileExplorerNuGetList.MouseClick += fileExplorerNuGetList_MouseClick;

            _fileExplorerNuGetUpdateMenuItem = new ToolStripMenuItem
            {
                Text = "Update package"
            };
            _fileExplorerNuGetUpdateMenuItem.Click += fileExplorerNuGetUpdateMenuItem_Click;

            _fileExplorerNuGetRemoveMenuItem = new ToolStripMenuItem
            {
                Text = "Remove from Project"
            };
            _fileExplorerNuGetRemoveMenuItem.Click += fileExplorerNuGetRemoveMenuItem_Click;

            _fileExplorerNuGetContextMenu = new ContextMenuStrip(components);
            _fileExplorerNuGetContextMenu.Items.Add(_fileExplorerNuGetUpdateMenuItem);
            _fileExplorerNuGetContextMenu.Items.Add(new ToolStripSeparator());
            _fileExplorerNuGetContextMenu.Items.Add(_fileExplorerNuGetRemoveMenuItem);

            _fileExplorerNuGetPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            _fileExplorerNuGetPanel.Controls.Add(_fileExplorerNuGetList);
            _fileExplorerNuGetPanel.Controls.Add(_fileExplorerNuGetTitleLabel);

            _fileExplorerHeader.Controls.Add(_fileExplorerTitleLabel, 0, 0);
            _fileExplorerHeader.Controls.Add(_fileExplorerOpenFolderButton, 1, 0);
            _fileExplorerHeader.Controls.Add(_fileExplorerHideButton, 2, 0);
            _fileExplorerContentSplitContainer.Panel1.Controls.Add(_fileExplorerTree);
            _fileExplorerContentSplitContainer.Panel2.Controls.Add(_fileExplorerNuGetPanel);
            _fileExplorerPanel.Controls.Add(_fileExplorerContentSplitContainer);
            _fileExplorerPanel.Controls.Add(_fileExplorerHeader);

            ConfigureEditorTabControlLayout(configureAllTabs: true);

            _editorExplorerSplitContainer.Panel1.Controls.Add(EditorTabControl);
            _editorExplorerSplitContainer.Panel2.Controls.Add(_fileExplorerPanel);
            _editorWorkspacePanel.Controls.Add(_editorExplorerSplitContainer);

            _fileExplorerShowButton = new Button
            {
                Text = "<<",
                Size = new Size(34, 24),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            toolTip1.SetToolTip(_fileExplorerShowButton, "Show file explorer");
            _fileExplorerShowButton.Click += (sender, args) => ToggleFileExplorer(true);
            _editorWorkspacePanel.Controls.Add(_fileExplorerShowButton);
            _editorWorkspacePanel.Resize += (sender, args) => PositionFileExplorerShowButton();
            Shown += (sender, args) => QueueFileExplorerLayoutApply();

            EnableBufferedPainting(
                splitContainer1,
                splitContainer1.Panel1,
                _editorWorkspacePanel,
                _editorExplorerSplitContainer,
                _editorExplorerSplitContainer.Panel1,
                _editorExplorerSplitContainer.Panel2,
                _fileExplorerPanel,
                _fileExplorerHeader,
                _fileExplorerContentSplitContainer,
                _fileExplorerContentSplitContainer.Panel1,
                _fileExplorerContentSplitContainer.Panel2,
                _fileExplorerNuGetPanel,
                EditorTabControl);

            splitContainer1.Panel1.Controls.Add(_editorWorkspacePanel);
            splitContainer1.Panel1.ResumeLayout();
            EditorTabControl.ResumeLayout();
            BeginInvoke((Action)(() =>
            {
                ApplyEditorExplorerMinimumWidths();
                QueueFileExplorerLayoutApply();
                PositionFileExplorerShowButton();
                QueueEditorLayoutRefresh();
            }));

            ApplyFileExplorerTheme();
        }

        private ImageList CreateFileExplorerImageList()
        {
            _fileExplorerImageList = new ImageList(components)
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(16, 16),
                TransparentColor = Color.Transparent
            };

            _fileExplorerImageList.Images.Add("folder", DrawFolderIcon(false));
            _fileExplorerImageList.Images.Add("folder-open", DrawFolderIcon(true));
            _fileExplorerImageList.Images.Add("folder-project", DrawSpecialFolderIcon(false, Color.FromArgb(110, 89, 191), "P"));
            _fileExplorerImageList.Images.Add("folder-project-open", DrawSpecialFolderIcon(true, Color.FromArgb(110, 89, 191), "P"));
            _fileExplorerImageList.Images.Add("folder-solution", DrawSpecialFolderIcon(false, Color.FromArgb(45, 136, 199), "S"));
            _fileExplorerImageList.Images.Add("folder-solution-open", DrawSpecialFolderIcon(true, Color.FromArgb(45, 136, 199), "S"));
            _fileExplorerImageList.Images.Add("file", DrawFileIcon(Color.FromArgb(128, 128, 128), false));
            _fileExplorerImageList.Images.Add("cs", DrawFileIcon(Color.FromArgb(72, 133, 237), true));
            _fileExplorerImageList.Images.Add("project", DrawFileIcon(Color.FromArgb(110, 89, 191), false));
            _fileExplorerImageList.Images.Add("solution", DrawFileIcon(Color.FromArgb(45, 136, 199), false));
            _fileExplorerImageList.Images.Add("text", DrawFileIcon(Color.FromArgb(96, 166, 106), false));
            return _fileExplorerImageList;
        }

        private static Bitmap DrawFolderIcon(bool open)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            using (var outline = new Pen(Color.FromArgb(160, 116, 42)))
            using (var tab = new SolidBrush(Color.FromArgb(224, 165, 65)))
            using (var body = new SolidBrush(open ? Color.FromArgb(245, 189, 86) : Color.FromArgb(238, 198, 86)))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(tab, 1, 3, 6, 3);
                g.FillRectangle(body, 1, 5, 14, 9);
                g.DrawRectangle(outline, 1, 5, 13, 8);
            }
            return bitmap;
        }

        private static Bitmap DrawSpecialFolderIcon(bool open, Color accent, string label)
        {
            Bitmap bitmap = DrawFolderIcon(open);
            using (var g = Graphics.FromImage(bitmap))
            using (var accentBrush = new SolidBrush(accent))
            using (var font = new Font("Segoe UI", 5.5F, FontStyle.Bold, GraphicsUnit.Point))
            {
                g.FillRectangle(accentBrush, 8, 8, 7, 6);
                g.DrawString(label, font, Brushes.White, new PointF(8.2F, 6.9F));
            }

            return bitmap;
        }

        private static Bitmap DrawFileIcon(Color accent, bool csharp)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            using (var paper = new SolidBrush(Color.FromArgb(245, 245, 245)))
            using (var fold = new SolidBrush(Color.FromArgb(215, 215, 215)))
            using (var accentBrush = new SolidBrush(accent))
            using (var outline = new Pen(Color.FromArgb(150, 150, 150)))
            using (var textPen = new Pen(Color.FromArgb(160, 160, 160)))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(paper, 3, 1, 10, 14);
                g.FillPolygon(fold, new[] { new Point(10, 1), new Point(13, 4), new Point(10, 4) });
                g.DrawRectangle(outline, 3, 1, 9, 13);
                g.FillRectangle(accentBrush, 4, 11, 8, 3);
                if (csharp)
                {
                    using (var font = new Font("Segoe UI", 5.5F, FontStyle.Bold, GraphicsUnit.Point))
                        g.DrawString("C#", font, Brushes.White, new PointF(3.2F, 7.5F));
                }
                else
                {
                    g.DrawLine(textPen, 5, 5, 10, 5);
                    g.DrawLine(textPen, 5, 7, 11, 7);
                }
            }
            return bitmap;
        }

        private void ToggleFileExplorer(bool show, bool saveWidth = true)
        {
            if (_editorExplorerSplitContainer == null)
                return;

            _editorWorkspacePanel?.SuspendLayout();
            _editorExplorerSplitContainer.SuspendLayout();
            EditorTabControl.SuspendLayout();
            try
            {
                if (show)
                {
                    _editorExplorerSplitContainer.Panel2Collapsed = false;
                    ApplyEditorExplorerMinimumWidths();
                    ApplyFileExplorerLayoutValues();
                    QueueFileExplorerLayoutApply();
                    _fileExplorerShowButton.Visible = false;
                }
                else
                {
                    if (saveWidth)
                        SaveFileExplorerWidth(force: true);
                    _editorExplorerSplitContainer.Panel2Collapsed = true;
                    _fileExplorerShowButton.Visible = true;
                    PositionFileExplorerShowButton();
                    SelectedEditor.GetSelectedEditor()?.Focus();
                }

                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerVisibleKey, show.ToString());
            }
            finally
            {
                EditorTabControl.ResumeLayout(false);
                _editorExplorerSplitContainer.ResumeLayout(false);
                _editorWorkspacePanel?.ResumeLayout(false);
            }

            QueueEditorLayoutRefresh();
        }

        private void ToggleFileExplorer()
        {
            if (_editorExplorerSplitContainer == null)
                return;

            ToggleFileExplorer(_editorExplorerSplitContainer.Panel2Collapsed);
        }

        private void SetFileExplorerWidth(int width)
        {
            if (_editorExplorerSplitContainer == null)
                return;

            ApplyEditorExplorerMinimumWidths();

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
            {
                QueueEditorLayoutRefresh();
                return;
            }

            int minDist = _editorExplorerSplitContainer.Panel1MinSize;
            int maxDist = Math.Max(minDist, availableWidth - _editorExplorerSplitContainer.Panel2MinSize);
            int explorerWidth = GetClampedFileExplorerWidth(width);
            int dist = Math.Max(minDist, Math.Min(maxDist, availableWidth - explorerWidth));

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
                _editorExplorerSplitContainer.SplitterDistance = dist;

            QueueEditorLayoutRefresh();
        }

        private void SetFileExplorerNuGetHeight(int height)
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitHeight = _fileExplorerContentSplitContainer.ClientSize.Height;
            int availableHeight = splitHeight - _fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return;

            int nuGetHeight = GetClampedFileExplorerNuGetHeight(height);
            _fileExplorerContentSplitContainer.SplitterDistance = Math.Max(0, availableHeight - nuGetHeight);
        }

        private int GetClampedFileExplorerWidth(int width)
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return width;

            int availableWidth = GetEditorExplorerSplitWidth() - _editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
                return width;

            int minExplorerWidth = Math.Max(0, _editorExplorerSplitContainer.Panel2MinSize);
            int maxExplorerWidth = Math.Max(minExplorerWidth,
                availableWidth - Math.Max(0, _editorExplorerSplitContainer.Panel1MinSize));
            return Math.Max(minExplorerWidth, Math.Min(width, maxExplorerWidth));
        }

        private int GetClampedFileExplorerNuGetHeight(int height)
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed)
            {
                return height;
            }

            int availableHeight = _fileExplorerContentSplitContainer.ClientSize.Height -
                _fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return height;

            int minNuGetHeight = Math.Max(0, _fileExplorerContentSplitContainer.Panel2MinSize);
            int maxNuGetHeight = Math.Max(minNuGetHeight, availableHeight - FileExplorerTreeMinHeight);
            return Math.Max(minNuGetHeight, Math.Min(height, maxNuGetHeight));
        }

        private void LoadFileExplorerLayoutValues()
        {
            _fileExplorerWidth = ReadFileExplorerLayoutValue(FileExplorerWidthKey,
                FileExplorerDefaultWidth, FileExplorerMinWidth);
            _fileExplorerNuGetHeight = ReadFileExplorerLayoutValue(FileExplorerNuGetHeightKey,
                FileExplorerDefaultNuGetHeight, FileExplorerNuGetMinHeight);
        }

        private static int ReadFileExplorerLayoutValue(string key, int defaultValue, int minValue)
        {
            string savedFileValue = ReadFileExplorerLayoutFileValue(key);
            if (int.TryParse(savedFileValue, out int fileValue) && fileValue >= minValue)
                return fileValue;

            string savedValue = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", key);
            if (int.TryParse(savedValue, out int value) && value >= minValue)
                return value;

            return defaultValue;
        }

        private void ApplyFileExplorerLayoutValues()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return;

            _suppressFileExplorerLayoutSave = true;
            _applyingFileExplorerLayout = true;
            try
            {
                _editorExplorerSplitContainer.SuspendLayout();
                _fileExplorerContentSplitContainer?.SuspendLayout();

                if (!_editorExplorerSplitContainer.Panel2Collapsed)
                    SetFileExplorerWidth(_fileExplorerWidth);
                SetFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            }
            finally
            {
                _fileExplorerContentSplitContainer?.ResumeLayout(true);
                _editorExplorerSplitContainer.ResumeLayout(true);
                _applyingFileExplorerLayout = false;
                _suppressFileExplorerLayoutSave = false;
            }
        }

        private void QueueFileExplorerLayoutApply()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed || IsDisposed)
                return;

            _pendingFileExplorerLayoutApply = true;
            _fileExplorerLayoutApplyAttempts = 0;
            _fileExplorerLayoutReadyForUserSave = false;
            SchedulePendingFileExplorerLayoutApply();
        }

        private void OnFileExplorerLayoutContainerSizeChanged()
        {
            if (_fileExplorerWidthDragInProgress ||
                _fileExplorerNuGetHeightDragInProgress ||
                _applyingFileExplorerLayout ||
                !_pendingFileExplorerLayoutApply ||
                _fileExplorerLayoutReadyForUserSave)
            {
                return;
            }

            SchedulePendingFileExplorerLayoutApply();
        }

        private void CancelPendingFileExplorerLayoutApplyForUserResize()
        {
            _pendingFileExplorerLayoutApply = false;
            _fileExplorerLayoutReadyForUserSave = true;
            _fileExplorerLayoutApplyTimer?.Stop();
        }

        private void SchedulePendingFileExplorerLayoutApply()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            EnsureFileExplorerLayoutApplyTimer();
            _fileExplorerLayoutApplyTimer.Interval =
                _fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyMaxAttempts
                    ? FileExplorerLayoutApplyInterval
                    : Math.Max(FileExplorerLayoutApplyInterval, 250);
            _fileExplorerLayoutApplyTimer.Stop();
            _fileExplorerLayoutApplyTimer.Start();
        }

        private void EnsureFileExplorerLayoutApplyTimer()
        {
            if (_fileExplorerLayoutApplyTimer != null)
                return;

            _fileExplorerLayoutApplyTimer = new System.Windows.Forms.Timer(components)
            {
                Interval = FileExplorerLayoutApplyInterval
            };
            _fileExplorerLayoutApplyTimer.Tick += OnFileExplorerLayoutApplyTimer;
        }

        private void OnFileExplorerLayoutApplyTimer(object sender, EventArgs e)
        {
            _fileExplorerLayoutApplyTimer?.Stop();
            ApplyPendingFileExplorerLayoutValues();
        }

        private void ApplyPendingFileExplorerLayoutValues()
        {
            if (!_pendingFileExplorerLayoutApply ||
                _editorExplorerSplitContainer == null ||
                _editorExplorerSplitContainer.IsDisposed)
            {
                return;
            }

            if (!CanApplyFileExplorerLayoutValues())
            {
                _fileExplorerLayoutApplyAttempts++;
                SchedulePendingFileExplorerLayoutApply();
                return;
            }

            ApplyEditorExplorerMinimumWidths();
            ApplyFileExplorerLayoutValues();
            PositionFileExplorerShowButton();
            QueueEditorLayoutRefresh();

            _fileExplorerLayoutApplyAttempts++;
            if (_fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyStabilizeAttempts ||
                !IsFileExplorerLayoutApplied())
            {
                if (_fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyMaxAttempts)
                {
                    SchedulePendingFileExplorerLayoutApply();
                    return;
                }
            }

            _pendingFileExplorerLayoutApply = false;
            _fileExplorerLayoutReadyForUserSave = true;
        }

        private bool CanApplyFileExplorerLayoutValues()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return false;

            if (!Visible || WindowState == FormWindowState.Minimized)
                return false;

            int splitWidth = GetEditorExplorerSplitWidth();
            if (splitWidth <= _editorExplorerSplitContainer.SplitterWidth)
                return false;

            if (_fileExplorerContentSplitContainer == null || _fileExplorerContentSplitContainer.IsDisposed)
                return true;

            int splitHeight = _fileExplorerContentSplitContainer.ClientSize.Height;
            if (splitHeight <= _fileExplorerContentSplitContainer.SplitterWidth)
                return false;

            return true;
        }

        private bool IsFileExplorerLayoutApplied()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return false;

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
            {
                int currentWidth = GetCurrentFileExplorerWidth();
                int targetWidth = GetClampedFileExplorerWidth(_fileExplorerWidth);
                if (currentWidth <= 0 || Math.Abs(currentWidth - targetWidth) > 1)
                    return false;
            }

            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return true;
            }

            int currentHeight = GetCurrentFileExplorerNuGetHeight();
            int targetHeight = GetClampedFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            return currentHeight > 0 && Math.Abs(currentHeight - targetHeight) <= 1;
        }

        private void QueueFileExplorerWidthSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerWidth();
        }

        private void QueueFileExplorerNuGetHeightSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerNuGetHeight();
        }

        private void SaveFileExplorerWidth(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_editorExplorerSplitContainer == null ||
                _editorExplorerSplitContainer.IsDisposed ||
                _editorExplorerSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitterWidth = GetCurrentFileExplorerWidth();
            if (splitterWidth <= 0)
                return;

            _fileExplorerWidth = Math.Max(FileExplorerMinWidth, splitterWidth);
            WriteFileExplorerLayoutValue(FileExplorerWidthKey, _fileExplorerWidth);
        }

        private void SaveFileExplorerNuGetHeight(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitterHeight = GetCurrentFileExplorerNuGetHeight();
            if (splitterHeight <= 0)
                return;

            _fileExplorerNuGetHeight = Math.Max(FileExplorerNuGetMinHeight, splitterHeight);
            WriteFileExplorerLayoutValue(FileExplorerNuGetHeightKey, _fileExplorerNuGetHeight);
        }

        private int GetCurrentFileExplorerWidth()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return 0;

            if (_editorExplorerSplitContainer.Panel2.Width > 0)
                return _editorExplorerSplitContainer.Panel2.Width;

            int availableWidth = GetEditorExplorerSplitWidth() - _editorExplorerSplitContainer.SplitterWidth;
            return availableWidth > 0
                ? Math.Max(0, availableWidth - _editorExplorerSplitContainer.SplitterDistance)
                : 0;
        }

        private int GetCurrentFileExplorerNuGetHeight()
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed)
            {
                return 0;
            }

            if (_fileExplorerContentSplitContainer.Panel2.Height > 0)
                return _fileExplorerContentSplitContainer.Panel2.Height;

            int availableHeight = _fileExplorerContentSplitContainer.ClientSize.Height -
                _fileExplorerContentSplitContainer.SplitterWidth;
            return availableHeight > 0
                ? Math.Max(0, availableHeight - _fileExplorerContentSplitContainer.SplitterDistance)
                : 0;
        }

        private static void WriteFileExplorerLayoutValue(string key, int value)
        {
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, key, value.ToString());
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, key, value.ToString());
            WriteFileExplorerLayoutFileValue(key, value);
        }

        private static string ReadFileExplorerLayoutFileValue(string key)
        {
            try
            {
                if (!File.Exists(FileExplorerLayoutFilePath))
                    return string.Empty;

                foreach (string line in File.ReadAllLines(FileExplorerLayoutFilePath))
                {
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string savedKey = line.Substring(0, separatorIndex).Trim();
                    if (string.Equals(savedKey, key, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(separatorIndex + 1).Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void WriteFileExplorerLayoutFileValue(string key, int value)
        {
            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(FileExplorerLayoutFilePath))
                {
                    foreach (string line in File.ReadAllLines(FileExplorerLayoutFilePath))
                    {
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex <= 0)
                            continue;

                        values[line.Substring(0, separatorIndex).Trim()] =
                            line.Substring(separatorIndex + 1).Trim();
                    }
                }

                values[key] = value.ToString();
                Directory.CreateDirectory(GlobalVariables.userProfileDirectory);
                File.WriteAllLines(FileExplorerLayoutFilePath,
                    values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(pair => pair.Key + "=" + pair.Value));
            }
            catch
            {
                // Layout state persistence is best-effort.
            }
        }

        private int GetEditorExplorerSplitWidth()
        {
            if (_editorExplorerSplitContainer == null)
                return 0;

            return _editorExplorerSplitContainer.ClientSize.Width > 0
                ? _editorExplorerSplitContainer.ClientSize.Width
                : _editorExplorerSplitContainer.Width;
        }

        private static Color GetEditorSurfaceBackColor()
        {
            return GlobalVariables.darkColor ? GlobalVariables.controlBgColor : SystemColors.Window;
        }

        private static void EnableBufferedPainting(params Control[] controls)
        {
            foreach (Control control in controls)
            {
                if (control == null)
                    continue;

                try
                {
                    DoubleBufferedProperty?.SetValue(control, true, null);
                }
                catch
                {
                }
            }
        }

        private void ApplyEditorExplorerMinimumWidths()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return;

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
                return;

            int panel1Min = EditorPaneMinWidth;
            int panel2Min = FileExplorerMinWidth;
            if (panel1Min + panel2Min > availableWidth)
            {
                panel1Min = Math.Min(EditorPaneMinWidth, Math.Max(0, availableWidth / 2));
                panel2Min = Math.Min(FileExplorerMinWidth, Math.Max(0, availableWidth - panel1Min));
            }

            int minDist = panel1Min;
            int maxDist = Math.Max(minDist, availableWidth - panel2Min);
            int currentDist = Math.Max(0, _editorExplorerSplitContainer.SplitterDistance);
            int safeDist = Math.Max(minDist, Math.Min(maxDist, currentDist));

            _editorExplorerSplitContainer.Panel1MinSize = 0;
            _editorExplorerSplitContainer.Panel2MinSize = 0;

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
                _editorExplorerSplitContainer.SplitterDistance = safeDist;

            _editorExplorerSplitContainer.Panel1MinSize = panel1Min;
            _editorExplorerSplitContainer.Panel2MinSize = panel2Min;
        }

        private void PositionFileExplorerShowButton()
        {
            if (_editorWorkspacePanel == null || _fileExplorerShowButton == null)
                return;

            _fileExplorerShowButton.Location = new Point(
                Math.Max(0, _editorWorkspacePanel.ClientSize.Width - _fileExplorerShowButton.Width
                    - SystemInformation.VerticalScrollBarWidth - 4),
                6);
            _fileExplorerShowButton.BringToFront();
        }

        private void ConfigureEditorTabControlLayout(bool configureAllTabs = false)
        {
            if (EditorTabControl == null)
                return;

            EditorTabControl.SuspendLayout();
            try
            {
                if (EditorTabControl.Anchor != AnchorStyles.None)
                    EditorTabControl.Anchor = AnchorStyles.None;
                if (EditorTabControl.Dock != DockStyle.Fill)
                    EditorTabControl.Dock = DockStyle.Fill;
                if (EditorTabControl.Location != Point.Empty)
                    EditorTabControl.Location = Point.Empty;
                if (EditorTabControl.Margin != Padding.Empty)
                    EditorTabControl.Margin = Padding.Empty;

                Color editorSurfaceBackColor = GetEditorSurfaceBackColor();
                if (EditorTabControl.BackColor != editorSurfaceBackColor)
                    EditorTabControl.BackColor = editorSurfaceBackColor;

                if (configureAllTabs)
                {
                    foreach (TabPage tabPage in EditorTabControl.TabPages)
                        ConfigureEditorTabPageLayout(tabPage, editorSurfaceBackColor);
                }
                else if (EditorTabControl.SelectedTab != null)
                {
                    ConfigureEditorTabPageLayout(EditorTabControl.SelectedTab, editorSurfaceBackColor);
                }
            }
            finally
            {
                EditorTabControl.ResumeLayout(false);
            }
        }

        private void ConfigureEditorTabPageLayout(TabPage tabPage)
        {
            ConfigureEditorTabPageLayout(tabPage, GetEditorSurfaceBackColor());
        }

        private void ConfigureEditorTabPageLayout(TabPage tabPage, Color editorSurfaceBackColor)
        {
            if (tabPage == null)
                return;

            if (tabPage.AutoScroll)
                tabPage.AutoScroll = false;
            if (tabPage.Margin != Padding.Empty)
                tabPage.Margin = Padding.Empty;
            if (tabPage.Padding != Padding.Empty)
                tabPage.Padding = Padding.Empty;
            if (tabPage.UseVisualStyleBackColor)
                tabPage.UseVisualStyleBackColor = false;
            if (tabPage.BackColor != editorSurfaceBackColor)
                tabPage.BackColor = editorSurfaceBackColor;

            foreach (Control control in tabPage.Controls)
            {
                if (control is TextEditorControl editor)
                    ConfigureEditorControlLayout(editor);
            }
        }

        private void ConfigureEditorControlLayout(TextEditorControl editor)
        {
            if (editor == null)
                return;

            editor.SuspendLayout();
            if (editor.Anchor != AnchorStyles.None)
                editor.Anchor = AnchorStyles.None;
            if (editor.Dock != DockStyle.Fill)
                editor.Dock = DockStyle.Fill;
            if (editor.Location != Point.Empty)
                editor.Location = Point.Empty;
            if (editor.Margin != Padding.Empty)
                editor.Margin = Padding.Empty;
            ConfigureEditorScrollBars(editor);
            editor.ResumeLayout(true);
        }

        private void ConfigureEditorScrollBars(TextEditorControl editor)
        {
            var textAreaControl = editor?.ActiveTextAreaControl;
            if (textAreaControl == null)
                return;

            if (textAreaControl.AutoHideScrollbars)
                textAreaControl.AutoHideScrollbars = false;
            if (!textAreaControl.VScrollBar.Visible)
                textAreaControl.VScrollBar.Visible = true;
            if (!textAreaControl.HScrollBar.Visible)
                textAreaControl.HScrollBar.Visible = true;
            if (editor.Visible && textAreaControl.Width > 0 && textAreaControl.Height > 0)
            {
                textAreaControl.ResizeTextArea();
                textAreaControl.AdjustScrollBars();
            }
        }

        private void QueueEditorLayoutRefresh()
        {
            if (EditorTabControl == null || EditorTabControl.IsDisposed || IsDisposed)
                return;

            _pendingEditorLayoutRefresh = true;
            EnsureEditorLayoutRefreshTimer();
            _editorLayoutRefreshTimer.Stop();
            _editorLayoutRefreshTimer.Start();
        }

        private void EnsureEditorLayoutRefreshTimer()
        {
            if (_editorLayoutRefreshTimer != null)
                return;

            _editorLayoutRefreshTimer = new System.Windows.Forms.Timer(components)
            {
                Interval = 50
            };
            _editorLayoutRefreshTimer.Tick += OnEditorLayoutRefreshTimer;
        }

        private void OnEditorLayoutRefreshTimer(object sender, EventArgs e)
        {
            _editorLayoutRefreshTimer?.Stop();
            if (!_pendingEditorLayoutRefresh)
                return;

            _pendingEditorLayoutRefresh = false;
            RefreshEditorLayoutBounds();
        }

        private void RefreshEditorLayoutBounds()
        {
            if (EditorTabControl == null || EditorTabControl.IsDisposed)
                return;

            if (_refreshingEditorLayoutBounds)
                return;

            _refreshingEditorLayoutBounds = true;
            try
            {
                ConfigureEditorTabControlLayout();
                EditorTabControl.PerformLayout();

                EditorTabControl.SelectedTab?.PerformLayout();
            }
            finally
            {
                _refreshingEditorLayoutBounds = false;
            }
        }

        private void fileExplorerOpenFolderButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Open folder in file explorer";
                dialog.ShowNewFolderButton = false;

                if (Directory.Exists(_fileExplorerRootPath))
                    dialog.SelectedPath = _fileExplorerRootPath;
                else
                {
                    var filePath = GetActiveEditorFilePath();
                    if (File.Exists(filePath))
                        dialog.SelectedPath = Path.GetDirectoryName(filePath);
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadFileExplorerFolder(dialog.SelectedPath);
            }
        }

        private void RestoreFileExplorerState()
        {
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, FileExplorerPathKey, string.Empty);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, FileExplorerVisibleKey, "True");
            LoadFileExplorerLayoutValues();

            string savedPath = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerPathKey);
            string savedVisible = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerVisibleKey);

            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                LoadFileExplorerFolder(savedPath);

            if (savedVisible == "False")
                ToggleFileExplorer(false, saveWidth: false);
            else
                QueueFileExplorerLayoutApply();
        }

        private void LoadFileExplorerFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath) || _fileExplorerTree == null)
                return;

            _fileExplorerRootPath = folderPath;
            if (!IsProjectFilePath(_fileExplorerStartupProjectPath) ||
                !IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
            {
                _fileExplorerStartupProjectPath = string.Empty;
            }

            InvalidateCompletionWorkspace();
            Interlocked.Increment(ref _projectPackageRefreshVersion);
            ClearProjectPackageCompletionReferences();
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerPathKey, folderPath);
            var expandedPaths = ReadFileExplorerExpandedPaths(folderPath);
            _fileExplorerTree.BeginUpdate();
            _suppressFileExplorerExpandedStateSave = true;
            try
            {
                _fileExplorerTree.Nodes.Clear();
                var root = CreateDirectoryNode(new DirectoryInfo(folderPath));
                _fileExplorerTree.Nodes.Add(root);
                PopulateDirectoryNode(root);
                root.Expand();
                RestoreExpandedPaths(root, expandedPaths);
            }
            finally
            {
                _suppressFileExplorerExpandedStateSave = false;
                _fileExplorerTree.EndUpdate();
            }
            _fileExplorerTitleLabel.Text = "Explorer";
            toolTip1.SetToolTip(_fileExplorerTitleLabel, folderPath);

            StartFileExplorerWatcher(folderPath);
            SaveFileExplorerExpandedState();
            RefreshProjectPackageContext(GetActiveEditorPackageProjectPath(), restoreProject: false,
                showRestoreFailure: false);
            ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
        }

        private void StartFileExplorerWatcher(string folderPath)
        {
            StopFileExplorerWatcher();

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            _fileExplorerWatcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _fileExplorerWatcher.Created += OnFileExplorerWatcherEvent;
            _fileExplorerWatcher.Deleted += OnFileExplorerWatcherEvent;
            _fileExplorerWatcher.Changed += OnFileExplorerWatcherChanged;
            _fileExplorerWatcher.Renamed += OnFileExplorerWatcherRenamed;

            _fileExplorerRefreshTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _fileExplorerRefreshTimer.Tick += OnFileExplorerRefreshTimer;
        }

        private void StopFileExplorerWatcher()
        {
            if (_fileExplorerWatcher != null)
            {
                _fileExplorerWatcher.EnableRaisingEvents = false;
                _fileExplorerWatcher.Dispose();
                _fileExplorerWatcher = null;
            }
            if (_fileExplorerRefreshTimer != null)
            {
                _fileExplorerRefreshTimer.Stop();
                _fileExplorerRefreshTimer.Tick -= OnFileExplorerRefreshTimer;
                _fileExplorerRefreshTimer.Dispose();
                _fileExplorerRefreshTimer = null;
            }
            if (_fileExplorerNuGetRefreshTimer != null)
            {
                _fileExplorerNuGetRefreshTimer.Stop();
                _fileExplorerNuGetRefreshTimer.Tick -= OnFileExplorerNuGetRefreshTimer;
                _fileExplorerNuGetRefreshTimer.Dispose();
                _fileExplorerNuGetRefreshTimer = null;
            }
            _pendingProjectPackageRefreshPath = string.Empty;
            _pendingProjectPackageRestore = false;
            _pendingProjectPackageShowRestoreFailure = false;
            _pendingRefreshPaths.Clear();
        }

        private void OnFileExplorerWatcherEvent(object sender, FileSystemEventArgs e)
        {
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherRenamed(object sender, RenamedEventArgs e)
        {
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath) ||
                ShouldRefreshExplorerNuGetPackages(e.OldFullPath))
            {
                ScheduleExplorerNuGetRefresh();
            }
        }

        private void ScheduleExplorerRefresh(string dirPath)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            BeginInvoke((Action)(() =>
            {
                _pendingRefreshPaths.Add(dirPath);
                _fileExplorerRefreshTimer?.Stop();
                _fileExplorerRefreshTimer?.Start();
            }));
        }

        private void OnFileExplorerRefreshTimer(object sender, EventArgs e)
        {
            _fileExplorerRefreshTimer?.Stop();
            var toRefresh = new HashSet<string>(_pendingRefreshPaths, StringComparer.OrdinalIgnoreCase);
            _pendingRefreshPaths.Clear();
            RefreshExplorerNodes(toRefresh);
        }

        private void RefreshExplorerNodes(HashSet<string> paths)
        {
            if (_fileExplorerTree == null || _fileExplorerTree.IsDisposed || _fileExplorerTree.Nodes.Count == 0)
                return;

            _fileExplorerTree.BeginUpdate();
            foreach (string path in paths)
                RefreshExplorerNodeForPath(path);
            _fileExplorerTree.EndUpdate();
        }

        private void RefreshExplorerNodeForPath(string dirPath)
        {
            var node = FindTreeNodeByPath(_fileExplorerTree.Nodes[0], dirPath);
            if (node == null)
                return;

            if (!node.IsExpanded)
            {
                node.Nodes.Clear();
                node.Nodes.Add(new TreeNode("Loading...") { Tag = FileExplorerLoadingTag });
                return;
            }

            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectExpandedPaths(node, expandedPaths);
            PopulateDirectoryNode(node);
            RestoreExpandedPaths(node, expandedPaths);
        }

        private static TreeNode FindTreeNodeByPath(TreeNode root, string path)
        {
            if (string.Equals(root.Tag as string, path, StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (TreeNode child in root.Nodes)
            {
                string tag = child.Tag as string;
                if (tag == null) continue;
                if (string.Equals(tag, path, StringComparison.OrdinalIgnoreCase))
                    return child;
                if (path.StartsWith(tag + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    var found = FindTreeNodeByPath(child, path);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static void CollectExpandedPaths(TreeNode node, HashSet<string> paths)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.IsExpanded)
                {
                    string path = NormalizeCompletionPath(child.Tag as string);
                    if (!string.IsNullOrEmpty(path))
                        paths.Add(path);
                    CollectExpandedPaths(child, paths);
                }
            }
        }

        private void RestoreExpandedPaths(TreeNode node, HashSet<string> expandedPaths)
        {
            foreach (TreeNode child in node.Nodes)
            {
                string tag = NormalizeCompletionPath(child.Tag as string);
                if (tag != null && expandedPaths.Contains(tag))
                {
                    PopulateDirectoryNode(child);
                    child.Expand();
                    RestoreExpandedPaths(child, expandedPaths);
                }
            }
        }

        private TreeNode CreateDirectoryNode(DirectoryInfo directory)
        {
            string text = string.IsNullOrEmpty(directory.Name) ? directory.FullName : directory.Name;
            string imageKey = GetDirectoryImageKey(directory.FullName, open: false);
            string selectedImageKey = GetDirectoryImageKey(directory.FullName, open: true);
            var node = new TreeNode(text)
            {
                Tag = directory.FullName,
                ImageKey = imageKey,
                SelectedImageKey = selectedImageKey,
                ToolTipText = directory.FullName
            };
            ApplyStartupProjectNodeStyle(node);
            node.Nodes.Add(new TreeNode("Loading...") { Tag = FileExplorerLoadingTag });
            return node;
        }

        private TreeNode CreateFileNode(FileInfo file)
        {
            string imageKey = GetFileExplorerImageKey(file.FullName);
            var node = new TreeNode(file.Name)
            {
                Tag = file.FullName,
                ImageKey = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText = file.FullName
            };
            ApplyStartupProjectNodeStyle(node);
            return node;
        }

        private static string GetDirectoryImageKey(string folderPath, bool open)
        {
            if (DirectoryContainsSolutionFile(folderPath))
                return open ? "folder-solution-open" : "folder-solution";

            if (DirectoryContainsProjectFile(folderPath))
                return open ? "folder-project-open" : "folder-project";

            return open ? "folder-open" : "folder";
        }

        private static string GetFileExplorerImageKey(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return "cs";
                case ".csproj":
                    return "project";
                case ".sln":
                    return "solution";
                case ".txt":
                case ".md":
                case ".json":
                case ".xml":
                case ".config":
                case ".xshd":
                    return "text";
                default:
                    return "file";
            }
        }

        private void fileExplorerTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && Equals(e.Node.Nodes[0].Tag, FileExplorerLoadingTag))
                PopulateDirectoryNode(e.Node);
        }

        private void fileExplorerTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            SaveFileExplorerExpandedState();
        }

        private void fileExplorerTree_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            SaveFileExplorerExpandedState();
        }

        private void fileExplorerTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            RefreshExplorerNuGetPackages();
        }

        private void SaveFileExplorerExpandedState()
        {
            if (_suppressFileExplorerExpandedStateSave ||
                _fileExplorerTree == null ||
                _fileExplorerTree.IsDisposed ||
                _fileExplorerTree.Nodes.Count == 0)
            {
                return;
            }

            try
            {
                var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectExpandedPaths(_fileExplorerTree.Nodes[0], expandedPaths);

                Directory.CreateDirectory(GlobalVariables.userProfileDirectory);
                File.WriteAllLines(FileExplorerExpandedPathsFilePath,
                    expandedPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                // Explorer state persistence is best-effort.
            }
        }

        private HashSet<string> ReadFileExplorerExpandedPaths(string rootPath)
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rootPath) || !File.Exists(FileExplorerExpandedPathsFilePath))
                return expandedPaths;

            try
            {
                foreach (string line in File.ReadAllLines(FileExplorerExpandedPathsFilePath))
                {
                    string path = NormalizeCompletionPath(line);
                    if (string.IsNullOrEmpty(path) ||
                        !Directory.Exists(path) ||
                        !IsSameOrChildDirectory(path, rootPath))
                    {
                        continue;
                    }

                    expandedPaths.Add(path);
                }
            }
            catch
            {
            }

            return expandedPaths;
        }

        public void RefreshExplorerNuGetPackages()
        {
            if (_fileExplorerNuGetList == null || _fileExplorerNuGetList.IsDisposed)
                return;

            if (InvokeRequired)
            {
                TryBeginInvoke(RefreshExplorerNuGetPackages);
                return;
            }

            string projectPath = GetActivePackageProjectPath();
            int refreshVersion = Interlocked.Increment(ref _fileExplorerNuGetListRefreshVersion);
            List<ProjectNuGetPackageReference> packages = null;
            _fileExplorerNuGetList.BeginUpdate();
            try
            {
                _fileExplorerNuGetList.Items.Clear();
                if (string.IsNullOrEmpty(projectPath))
                {
                    _fileExplorerNuGetTitleLabel.Text = "NuGet packages";
                    toolTip1.SetToolTip(_fileExplorerNuGetTitleLabel, "Open a project folder or select a .csproj file");
                    AddFileExplorerNuGetPlaceholder("No project selected");
                    return;
                }

                _fileExplorerNuGetTitleLabel.Text = $"NuGet: {Path.GetFileName(projectPath)}";
                toolTip1.SetToolTip(_fileExplorerNuGetTitleLabel, projectPath);

                packages = ProjectNuGetManager.GetPackageReferences(projectPath);
                if (packages.Count == 0)
                {
                    AddFileExplorerNuGetPlaceholder("No PackageReference items");
                    return;
                }

                foreach (var package in packages)
                {
                    var item = new ListViewItem(new[]
                    {
                        package.Name,
                        package.Version,
                        "Checking...",
                        "Checking..."
                    })
                    {
                        Tag = package,
                        ToolTipText = FormatExplorerNuGetToolTip(package)
                    };
                    _fileExplorerNuGetList.Items.Add(item);
                }
            }
            finally
            {
                _fileExplorerNuGetList.EndUpdate();
                ResizeFileExplorerNuGetColumns();
            }

            if (packages != null && packages.Count > 0)
                ScheduleExplorerNuGetPackageMetadataRefresh(projectPath, packages, refreshVersion);
        }

        public void RefreshProjectPackageContext(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (InvokeRequired)
            {
                TryBeginInvoke(() => RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure));
                return;
            }

            int refreshVersion = Interlocked.Increment(ref _projectPackageRefreshVersion);
            bool useCompletionReferences = ShouldUseProjectPackageCompletionReferences(projectPath);
            RefreshExplorerNuGetPackages();

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                RealTimeChecker.InvalidateReferenceCache();
                ClearProjectPackageCompletionReferences();
                ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                return;
            }

            RealTimeChecker.InvalidateReferenceCache();
            if (!useCompletionReferences)
                ClearProjectPackageCompletionReferences();

            Task.Run(() =>
            {
                ProcessRunResult restoreResult = null;
                if (restoreProject)
                    restoreResult = ProjectNuGetManager.RestoreProject(projectPath);

                RealTimeChecker.InvalidateReferenceCache();
                if (useCompletionReferences &&
                    refreshVersion == Volatile.Read(ref _projectPackageRefreshVersion))
                {
                    RefreshProjectPackageCompletionReferences(projectPath);
                }
                return restoreResult;
            }).ContinueWith(task =>
            {
                if (IsDisposed || !IsHandleCreated || refreshVersion != _projectPackageRefreshVersion)
                    return;

                TryBeginInvoke(() =>
                {
                    if (refreshVersion != _projectPackageRefreshVersion)
                        return;

                    RefreshExplorerNuGetPackages();
                    ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());

                    ProcessRunResult restoreResult = null;
                    if (task.Status == TaskStatus.RanToCompletion)
                        restoreResult = task.Result;
                    else if (task.Exception != null)
                        restoreResult = new ProcessRunResult(-1,
                            task.Exception.GetBaseException()?.Message ?? task.Exception.Message);

                    if (showRestoreFailure && restoreResult != null && !restoreResult.Success)
                    {
                        MessageBox.Show(FormatProjectRestoreFailure(restoreResult.Output),
                            "NuGet restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                });
            });
        }

        private bool ShouldUseProjectPackageCompletionReferences(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return false;

            string activeProjectPath = GetCompletionProjectPath(GetActiveEditorFilePath());
            return !string.IsNullOrEmpty(activeProjectPath) &&
                string.Equals(NormalizeCompletionPath(activeProjectPath),
                    NormalizeCompletionPath(projectPath), StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshProjectPackageCompletionReferences(string projectPath)
        {
            if (!GlobalVariables.OCodeCompletion || pcRegistry == null || myProjectContent == null)
                return;

            var referencePaths = GetCompletionCompileReferencePaths(projectPath);
            var activeReferences = new HashSet<string>(referencePaths, StringComparer.OrdinalIgnoreCase);

            lock (_completionDataLock)
            {
                var removedReferences = _projectPackageCompletionContents.Keys
                    .Where(path => !activeReferences.Contains(path))
                    .ToList();

                foreach (var referencePath in removedReferences)
                {
                    Dom.IProjectContent projectContent = _projectPackageCompletionContents[referencePath];
                    myProjectContent.ReferencedContents.Remove(projectContent);
                    projectContent.Dispose();
                    _projectPackageCompletionContents.Remove(referencePath);
                }

                foreach (var referencePath in referencePaths)
                {
                    if (_projectPackageCompletionContents.ContainsKey(referencePath))
                        continue;

                    Dom.IProjectContent projectContent = LoadProjectPackageCompletionContent(referencePath);
                    if (projectContent == null)
                        continue;

                    _projectPackageCompletionContents[referencePath] = projectContent;
                    myProjectContent.AddReferencedContent(projectContent);
                }
            }
        }

        private void ClearProjectPackageCompletionReferences()
        {
            if (myProjectContent == null)
                return;

            lock (_completionDataLock)
            {
                foreach (var projectContent in _projectPackageCompletionContents.Values)
                {
                    myProjectContent.ReferencedContents.Remove(projectContent);
                    projectContent.Dispose();
                }

                _projectPackageCompletionContents.Clear();
            }
        }

        private Dom.IProjectContent LoadProjectPackageCompletionContent(string referencePath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFile(referencePath);
                var projectContent = new Dom.ReflectionProjectContent(assembly, pcRegistry);
                projectContent.InitializeReferences();
                return projectContent;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatProjectRestoreFailure(string restoreOutput)
        {
            string output = (restoreOutput ?? string.Empty).Trim();
            if (output.Length > 1800)
                output = output.Substring(0, 1800) + Environment.NewLine + "...";

            return string.IsNullOrWhiteSpace(output)
                ? "NuGet restore failed. Project references may be stale until restore succeeds."
                : "NuGet restore failed. Project references may be stale until restore succeeds." +
                  Environment.NewLine + Environment.NewLine + output;
        }

        private void ScheduleExplorerNuGetPackageMetadataRefresh(string projectPath,
            List<ProjectNuGetPackageReference> packages, int refreshVersion)
        {
            var packageSnapshot = packages
                .Select(package => new ProjectNuGetPackageReference
                {
                    Name = package.Name,
                    Version = package.Version,
                    ProjectPath = package.ProjectPath
                })
                .ToList();

            Task.Run(() =>
            {
                ProjectNuGetManager.PopulateLatestPackageVersions(packageSnapshot);
                ProjectNuGetManager.PopulateUnusedPackageStatus(projectPath, packageSnapshot);
                return packageSnapshot;
            }).ContinueWith(task =>
            {
                if (task.Status != TaskStatus.RanToCompletion ||
                    IsDisposed ||
                    !IsHandleCreated ||
                    refreshVersion != _fileExplorerNuGetListRefreshVersion)
                {
                    return;
                }

                TryBeginInvoke(() => ApplyExplorerNuGetPackageMetadata(projectPath, task.Result,
                    refreshVersion));
            });
        }

        private void ApplyExplorerNuGetPackageMetadata(string projectPath,
            List<ProjectNuGetPackageReference> packages, int refreshVersion)
        {
            if (refreshVersion != _fileExplorerNuGetListRefreshVersion ||
                !string.Equals(GetActivePackageProjectPath(), projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var metadataByName = packages
                .GroupBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            _fileExplorerNuGetList.BeginUpdate();
            try
            {
                foreach (ListViewItem item in _fileExplorerNuGetList.Items)
                {
                    if (!(item.Tag is ProjectNuGetPackageReference package) ||
                        !metadataByName.TryGetValue(package.Name, out var metadata))
                    {
                        continue;
                    }

                    package.LatestVersion = metadata.LatestVersion;
                    package.HasUpdate = metadata.HasUpdate;
                    package.UnusedCheckCompleted = metadata.UnusedCheckCompleted;
                    package.IsUnused = metadata.IsUnused;

                    EnsureExplorerNuGetSubItemCount(item);
                    item.SubItems[2].Text = FormatExplorerNuGetUpdateText(package);
                    item.SubItems[3].Text = FormatExplorerNuGetStatusText(package);
                    item.ToolTipText = FormatExplorerNuGetToolTip(package);
                    item.ForeColor = package.UnusedCheckCompleted && package.IsUnused
                        ? (GlobalVariables.darkColor ? Color.FromArgb(245, 174, 96) : Color.DarkOrange)
                        : _fileExplorerNuGetList.ForeColor;
                }
            }
            finally
            {
                _fileExplorerNuGetList.EndUpdate();
                ResizeFileExplorerNuGetColumns();
            }
        }

        private static void EnsureExplorerNuGetSubItemCount(ListViewItem item)
        {
            while (item.SubItems.Count < 4)
                item.SubItems.Add(string.Empty);
        }

        private static string FormatExplorerNuGetUpdateText(ProjectNuGetPackageReference package)
        {
            if (package.HasUpdate)
                return package.LatestVersion;

            return string.IsNullOrWhiteSpace(package.LatestVersion) ? "Unknown" : "Current";
        }

        private static string FormatExplorerNuGetStatusText(ProjectNuGetPackageReference package)
        {
            if (!package.UnusedCheckCompleted)
                return "Checking...";

            return package.IsUnused ? "Unused" : "Used";
        }

        private static string FormatExplorerNuGetToolTip(ProjectNuGetPackageReference package)
        {
            string updateStatus = package.HasUpdate
                ? $"Update available: {package.LatestVersion}"
                : (!string.IsNullOrWhiteSpace(package.LatestVersion) ? "No update available" : "Update unknown");
            string usageStatus = package.UnusedCheckCompleted
                ? (package.IsUnused ? "Marked unused by source scan" : "Used by source scan")
                : "Usage unknown";

            return $"{package.Name} {package.Version}{Environment.NewLine}{updateStatus}{Environment.NewLine}{usageStatus}";
        }

        private void AddFileExplorerNuGetPlaceholder(string text)
        {
            var item = new ListViewItem(new[] { text, string.Empty, string.Empty, string.Empty })
            {
                ForeColor = GlobalVariables.darkColor ? Color.FromArgb(150, 170, 165) : SystemColors.GrayText
            };
            _fileExplorerNuGetList.Items.Add(item);
        }

        private void ResizeFileExplorerNuGetColumns()
        {
            if (_fileExplorerNuGetList == null ||
                _fileExplorerNuGetList.IsDisposed ||
                _fileExplorerNuGetList.Columns.Count < 4)
            {
                return;
            }

            int availableWidth = _fileExplorerNuGetList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            if (availableWidth <= 0)
                return;

            int versionWidth = Math.Max(64, Math.Min(86, availableWidth / 5));
            int updateWidth = Math.Max(72, Math.Min(96, availableWidth / 4));
            int statusWidth = Math.Max(62, Math.Min(76, availableWidth / 5));
            int packageWidth = Math.Max(90, availableWidth - versionWidth - updateWidth - statusWidth);

            _fileExplorerNuGetVersionColumn.Width = versionWidth;
            _fileExplorerNuGetUpdateColumn.Width = updateWidth;
            _fileExplorerNuGetStatusColumn.Width = statusWidth;
            _fileExplorerNuGetPackageColumn.Width = packageWidth;
        }

        private void ScheduleExplorerNuGetRefresh()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(GetActiveEditorPackageProjectPath(),
                restoreProject: false, showRestoreFailure: false));
        }

        private void ScheduleProjectPackageContextRefresh(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(projectPath, restoreProject,
                    showRestoreFailure));
                return;
            }

            _pendingProjectPackageRefreshPath = projectPath;
            _pendingProjectPackageRestore = _pendingProjectPackageRestore || restoreProject;
            _pendingProjectPackageShowRestoreFailure =
                _pendingProjectPackageShowRestoreFailure || showRestoreFailure;

            if (_fileExplorerNuGetRefreshTimer == null)
            {
                _fileExplorerNuGetRefreshTimer = new System.Windows.Forms.Timer { Interval = 300 };
                _fileExplorerNuGetRefreshTimer.Tick += OnFileExplorerNuGetRefreshTimer;
            }

            _fileExplorerNuGetRefreshTimer.Stop();
            _fileExplorerNuGetRefreshTimer.Start();
        }

        private void OnFileExplorerNuGetRefreshTimer(object sender, EventArgs e)
        {
            _fileExplorerNuGetRefreshTimer?.Stop();

            string projectPath = _pendingProjectPackageRefreshPath;
            bool restoreProject = _pendingProjectPackageRestore;
            bool showRestoreFailure = _pendingProjectPackageShowRestoreFailure;

            _pendingProjectPackageRefreshPath = string.Empty;
            _pendingProjectPackageRestore = false;
            _pendingProjectPackageShowRestoreFailure = false;

            RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure);
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

        private static bool ShouldRefreshExplorerNuGetPackages(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "project.assets.json", StringComparison.OrdinalIgnoreCase);
        }

        private void fileExplorerNuGetList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || _fileExplorerNuGetList == null)
                return;

            ListViewHitTestInfo hit = _fileExplorerNuGetList.HitTest(e.Location);
            if (!(hit.Item?.Tag is ProjectNuGetPackageReference package))
                return;

            hit.Item.Selected = true;
            _fileExplorerNuGetUpdateMenuItem.Tag = package;
            _fileExplorerNuGetUpdateMenuItem.Enabled = package.HasUpdate &&
                !string.IsNullOrWhiteSpace(package.LatestVersion);
            _fileExplorerNuGetUpdateMenuItem.Text = _fileExplorerNuGetUpdateMenuItem.Enabled
                ? $"Update {package.Name} to {package.LatestVersion}"
                : "No update available";

            _fileExplorerNuGetRemoveMenuItem.Tag = package;
            _fileExplorerNuGetRemoveMenuItem.Text = $"Remove {package.Name} from Project";
            _fileExplorerNuGetContextMenu.Show(_fileExplorerNuGetList, e.Location);
        }

        private void fileExplorerNuGetUpdateMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_fileExplorerNuGetUpdateMenuItem.Tag is ProjectNuGetPackageReference package) ||
                !package.HasUpdate ||
                string.IsNullOrWhiteSpace(package.LatestVersion))
            {
                return;
            }

            DialogResult dialog = MessageBox.Show(
                $"Update NuGet package {package.Name} from {package.Version} to {package.LatestVersion}?",
                "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectNuGetManager.UpdatePackageReference(package.ProjectPath, package.Name,
                    package.LatestVersion, out string message))
            {
                MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshProjectPackageContext(package.ProjectPath, restoreProject: true);
            MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void fileExplorerNuGetRemoveMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_fileExplorerNuGetRemoveMenuItem.Tag is ProjectNuGetPackageReference package))
                return;

            DialogResult dialog = MessageBox.Show(
                $"Remove NuGet package {package.Name} from {Path.GetFileName(package.ProjectPath)}?",
                "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectNuGetManager.RemovePackageReference(package.ProjectPath, package.Name, out string message))
            {
                MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshProjectPackageContext(package.ProjectPath, restoreProject: true);
            MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PopulateDirectoryNode(TreeNode node)
        {
            string folderPath = node.Tag as string;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            node.Nodes.Clear();

            try
            {
                foreach (var directory in Directory.GetDirectories(folderPath)
                    .Select(path => new DirectoryInfo(path))
                    .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldHideExplorerItem(directory.Attributes))
                        continue;
                    node.Nodes.Add(CreateDirectoryNode(directory));
                }

                foreach (var file in Directory.GetFiles(folderPath)
                    .Select(path => new FileInfo(path))
                    .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldHideExplorerItem(file.Attributes))
                        continue;
                    node.Nodes.Add(CreateFileNode(file));
                }
            }
            catch
            {
                node.Nodes.Add(new TreeNode("Unable to read folder") { ImageKey = "file", SelectedImageKey = "file" });
            }
        }

        private static bool ShouldHideExplorerItem(FileAttributes attributes)
        {
            return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                   (attributes & FileAttributes.System) == FileAttributes.System;
        }

        private void fileExplorerTree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            TreeNode node = _fileExplorerTree.GetNodeAt(e.Location);
            if (node == null)
            {
                _fileExplorerContextNode = null;
                return;
            }

            _fileExplorerTree.SelectedNode = node;
            _fileExplorerContextNode = node;
        }

        private void fileExplorerContextMenu_Opening(object sender, CancelEventArgs e)
        {
            TreeNode node = _fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || Equals(path, FileExplorerLoadingTag))
            {
                e.Cancel = true;
                return;
            }

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
            {
                e.Cancel = true;
                return;
            }

            string solutionPath = GetSolutionPathFromExplorerPath(path);
            string projectPath = GetProjectPathFromExplorerPath(path);
            bool hasSolutionContext = !string.IsNullOrEmpty(solutionPath);
            bool canAddProjectToSolution = hasSolutionContext &&
                IsAddProjectToSolutionContext(path, solutionPath);
            bool hasProjectContext = !string.IsNullOrEmpty(projectPath);
            bool hasProjectReferenceCandidates = hasProjectContext &&
                ProjectReferenceManager.GetReferenceableProjects(projectPath, solutionPath,
                    _fileExplorerRootPath).Count > 0;
            bool hasProjectReferences = hasProjectContext &&
                ProjectReferenceManager.GetProjectReferences(projectPath).Count > 0;

            _fileExplorerAddProjectMenuItem.Visible = hasSolutionContext;
            _fileExplorerAddProjectMenuItem.Enabled = canAddProjectToSolution;
            _fileExplorerAddProjectReferenceMenuItem.Visible = hasProjectContext;
            _fileExplorerAddProjectReferenceMenuItem.Enabled = hasProjectReferenceCandidates;
            _fileExplorerRemoveProjectReferenceMenuItem.Visible = hasProjectContext;
            _fileExplorerRemoveProjectReferenceMenuItem.Enabled = hasProjectReferences;
            _fileExplorerSetStartupProjectMenuItem.Visible = hasProjectContext;
            _fileExplorerSetStartupProjectMenuItem.Enabled = hasProjectContext;
            _fileExplorerSetStartupProjectMenuItem.Checked = hasProjectContext &&
                IsStartupProjectPath(projectPath);
            _fileExplorerProjectSeparator.Visible = hasSolutionContext || hasProjectContext;
            _fileExplorerNewFileMenuItem.Visible = isDirectory;
            _fileExplorerNewFolderMenuItem.Visible = isDirectory;
            _fileExplorerContextSeparator.Visible = isDirectory;
            _fileExplorerRenameMenuItem.Visible = isDirectory || isFile;
            _fileExplorerRenameMenuItem.Enabled = !IsExplorerRootPath(path);
            _fileExplorerDeleteMenuItem.Visible = isDirectory || isFile;
            _fileExplorerDeleteMenuItem.Enabled = !IsExplorerRootPath(path);
        }

        private void fileExplorerAddProjectMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string;
            string solutionPath = GetSolutionPathFromExplorerPath(contextPath);
            if (string.IsNullOrEmpty(solutionPath))
                return;

            if (!IsAddProjectToSolutionContext(contextPath, solutionPath))
                return;

            using (var dialog = new NewProject(solutionPath))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                NewProjectResult project = dialog.CreatedProject;
                if (project == null)
                    return;

                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                string projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
                if (!string.IsNullOrEmpty(solutionDirectory))
                    RefreshAndExpandExplorerFolder(solutionDirectory);
                if (!string.IsNullOrEmpty(projectDirectory))
                    RefreshAndExpandExplorerFolder(projectDirectory);

                SelectExplorerPath(project.ProjectFilePath);
                if (File.Exists(project.StarterFilePath))
                    OpenFileFromExplorer(project.StarterFilePath);

                InvalidateCompletionWorkspace();
                RealTimeChecker.InvalidateReferenceCache();
                RefreshProjectPackageContext(project.ProjectFilePath, restoreProject: true,
                    showRestoreFailure: false);
                ShowProjectStatus("Added project to solution", project.ProjectFilePath, solutionPath);
            }
        }

        private void fileExplorerAddProjectReferenceMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string;
            string projectPath = GetProjectPathFromExplorerPath(contextPath);
            if (!IsProjectFilePath(projectPath))
                return;

            string solutionPath = GetSolutionPathFromExplorerPath(contextPath);
            var candidates = ProjectReferenceManager.GetReferenceableProjects(projectPath, solutionPath,
                _fileExplorerRootPath);
            if (candidates.Count == 0)
            {
                MessageBox.Show("No available project references were found in this solution.",
                    "Add Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string referencedProjectPath = ShowProjectReferencePicker(projectPath, candidates);
            if (string.IsNullOrEmpty(referencedProjectPath))
                return;

            if (!ProjectReferenceManager.AddProjectReference(projectPath, referencedProjectPath, out string message))
            {
                MessageBox.Show(message, "Add Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(projectDirectory))
                RefreshAndExpandExplorerFolder(projectDirectory);

            InvalidateCompletionWorkspace();
            RealTimeChecker.InvalidateReferenceCache();
            RefreshProjectPackageContext(projectPath, restoreProject: false, showRestoreFailure: false);
            ShowProjectStatus("Added project reference", projectPath, solutionPath, referencedProjectPath);
        }

        private void fileExplorerRemoveProjectReferenceMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string;
            string projectPath = GetProjectPathFromExplorerPath(contextPath);
            if (!IsProjectFilePath(projectPath))
                return;

            var references = ProjectReferenceManager.GetProjectReferences(projectPath);
            if (references.Count == 0)
            {
                MessageBox.Show("No project references were found in this project.",
                    "Remove Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string referencedProjectPath = ShowProjectReferencePicker(projectPath, references,
                "Remove Project Reference", "Remove");
            if (string.IsNullOrEmpty(referencedProjectPath))
                return;

            DialogResult dialog = MessageBox.Show(
                $"Remove project reference {Path.GetFileNameWithoutExtension(referencedProjectPath)} from {Path.GetFileName(projectPath)}?",
                "Remove Project Reference", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectReferenceManager.RemoveProjectReference(projectPath, referencedProjectPath, out string message))
            {
                MessageBox.Show(message, "Remove Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(projectDirectory))
                RefreshAndExpandExplorerFolder(projectDirectory);

            InvalidateCompletionWorkspace();
            RealTimeChecker.InvalidateReferenceCache();
            RefreshProjectPackageContext(projectPath, restoreProject: false, showRestoreFailure: false);
            ShowProjectStatus("Removed project reference", projectPath, GetSolutionPathFromExplorerPath(contextPath),
                referencedProjectPath);
        }

        private string ShowProjectReferencePicker(string projectPath, IList<string> candidateProjects,
            string title = "Add Project Reference", string actionText = "Add")
        {
            using (var dialog = new Form())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(560, 340);
                dialog.MinimumSize = new Size(460, 280);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.Font = SystemFonts.MessageBoxFont;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10)
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

                var targetLabel = new Label
                {
                    AutoEllipsis = true,
                    Dock = DockStyle.Fill,
                    Text = "Target: " + Path.GetFileNameWithoutExtension(projectPath),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var projectList = new ListBox
                {
                    Dock = DockStyle.Fill,
                    IntegralHeight = false,
                    HorizontalScrollbar = true
                };
                foreach (string candidate in candidateProjects)
                    projectList.Items.Add(new ProjectReferenceListItem(candidate, _fileExplorerRootPath));
                if (projectList.Items.Count > 0)
                    projectList.SelectedIndex = 0;

                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(0, 8, 0, 0)
                };
                var addButton = new Button
                {
                    Text = actionText,
                    Width = 92,
                    Height = 28,
                    DialogResult = DialogResult.OK
                };
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Width = 92,
                    Height = 28,
                    DialogResult = DialogResult.Cancel
                };
                buttons.Controls.Add(addButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(targetLabel, 0, 0);
                layout.Controls.Add(projectList, 0, 1);
                layout.Controls.Add(buttons, 0, 2);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = addButton;
                dialog.CancelButton = cancelButton;
                projectList.DoubleClick += (_, _) =>
                {
                    if (projectList.SelectedItem != null)
                        dialog.DialogResult = DialogResult.OK;
                };

                FrmColorMod.ToogleColorMode(dialog, GlobalVariables.darkColor);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return string.Empty;

                return (projectList.SelectedItem as ProjectReferenceListItem)?.ProjectPath ?? string.Empty;
            }
        }

        private sealed class ProjectReferenceListItem
        {
            private readonly string _workspaceFolder;

            public ProjectReferenceListItem(string projectPath, string workspaceFolder)
            {
                ProjectPath = projectPath;
                _workspaceFolder = workspaceFolder;
            }

            public string ProjectPath { get; }

            public override string ToString()
            {
                string name = Path.GetFileNameWithoutExtension(ProjectPath);
                string displayPath = ProjectPath;
                try
                {
                    if (Directory.Exists(_workspaceFolder))
                        displayPath = Path.GetRelativePath(_workspaceFolder, ProjectPath);
                }
                catch
                {
                }

                return $"{name} ({displayPath})";
            }
        }

        private void fileExplorerSetStartupProjectMenuItem_Click(object sender, EventArgs e)
        {
            string projectPath = GetProjectPathFromExplorerPath(
                (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string);
            if (!IsProjectFilePath(projectPath))
                return;

            _fileExplorerStartupProjectPath = projectPath;
            UpdateFileExplorerStartupProjectHighlight();
            ShowProjectStatus("Startup project set", projectPath, FindNearestBuildFile(
                Path.GetDirectoryName(projectPath), _fileExplorerRootPath, "*.sln"));
        }

        private void fileExplorerNewFileMenuItem_Click(object sender, EventArgs e)
        {
            string folderPath = GetExplorerContextFolderPath();
            if (string.IsNullOrEmpty(folderPath))
                return;

            string fileName = PromptForExplorerName("New C# File", "File name:", GetUniqueExplorerName(folderPath, "Class1.cs"));
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            if (!TryNormalizeExplorerFileName(fileName, ".cs", out fileName))
                return;

            string filePath = Path.Combine(folderPath, fileName);
            if (File.Exists(filePath) || Directory.Exists(filePath))
            {
                MessageBox.Show("An item with that name already exists.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                File.WriteAllText(filePath, BuildNewCSharpFileContent(fileName));
                RefreshAndExpandExplorerFolder(folderPath);
                SelectExplorerPath(filePath);
                OpenFileFromExplorer(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "New C# File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileExplorerNewFolderMenuItem_Click(object sender, EventArgs e)
        {
            string folderPath = GetExplorerContextFolderPath();
            if (string.IsNullOrEmpty(folderPath))
                return;

            string folderName = PromptForExplorerName("New Folder", "Folder name:", GetUniqueExplorerName(folderPath, "New Folder"));
            if (string.IsNullOrWhiteSpace(folderName))
                return;

            if (!TryValidateExplorerItemName(folderName, out folderName))
                return;

            string newFolderPath = Path.Combine(folderPath, folderName);
            if (Directory.Exists(newFolderPath) || File.Exists(newFolderPath))
            {
                MessageBox.Show("An item with that name already exists.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(newFolderPath);
                RefreshAndExpandExplorerFolder(folderPath);
                SelectExplorerPath(newFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "New Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileExplorerRenameMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = _fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || IsExplorerRootPath(path))
                return;

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
                return;

            if (!IsPathInsideExplorerRoot(path))
            {
                MessageBox.Show("This item is outside the opened explorer folder.", "Rename",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sourcePath = isDirectory
                ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : path;
            string currentName = Path.GetFileName(sourcePath);
            string newName = PromptForExplorerName("Rename", "Name:", currentName);
            if (string.IsNullOrWhiteSpace(newName))
                return;

            if (!TryValidateExplorerItemName(newName, out newName))
                return;

            if (string.Equals(currentName, newName, StringComparison.Ordinal))
                return;

            string parentPath = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(parentPath))
                return;

            string newPath = Path.Combine(parentPath, newName);
            bool samePathIgnoreCase = string.Equals(NormalizeCompletionPath(path),
                NormalizeCompletionPath(newPath), StringComparison.OrdinalIgnoreCase);
            if (!samePathIgnoreCase && (File.Exists(newPath) || Directory.Exists(newPath)))
            {
                MessageBox.Show("An item with that name already exists.", "Rename",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                MoveExplorerItem(path, newPath, isDirectory);
                UpdateStartupProjectAfterExplorerRename(path, newPath, isDirectory);
                UpdateProjectReferencesAfterExplorerRename(path, newPath, isDirectory);
                UpdateOpenTabsAfterExplorerRename(path, newPath, isDirectory);

                RefreshAndExpandExplorerFolder(parentPath);
                SelectExplorerPath(newPath);
                UpdateFileExplorerStartupProjectHighlight();

                InvalidateCompletionWorkspace();
                RealTimeChecker.InvalidateReferenceCache();
                RefreshProjectPackageContext(GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Rename", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileExplorerDeleteMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = _fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || IsExplorerRootPath(path))
                return;

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
                return;

            if (!IsPathInsideExplorerRoot(path))
            {
                MessageBox.Show("This item is outside the opened explorer folder.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string itemType = isDirectory ? "folder" : "file";
            DialogResult dialog = MessageBox.Show(
                $"Delete {itemType} '{Path.GetFileName(path)}'?",
                "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            string parentPath = Path.GetDirectoryName(path);
            List<string> deletedProjectPaths = GetExplorerDeletedProjectPaths(path, isDirectory);
            try
            {
                if (isDirectory)
                {
                    VBFileSystem.DeleteDirectory(path, VBUIOption.OnlyErrorDialogs,
                        VBRecycleOption.SendToRecycleBin);
                }
                else
                {
                    VBFileSystem.DeleteFile(path, VBUIOption.OnlyErrorDialogs,
                        VBRecycleOption.SendToRecycleBin);
                }

                RemoveProjectsFromWorkspaceSolutions(deletedProjectPaths, _fileExplorerRootPath);
                ClearStartupProjectAfterExplorerDelete(path, isDirectory);
                if (!string.IsNullOrEmpty(parentPath))
                    RefreshExplorerNodeForPath(parentPath);
                UpdateFileExplorerStartupProjectHighlight();
                RefreshProjectPackageContext(GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void MoveExplorerItem(string path, string newPath, bool isDirectory)
        {
            string normalizedPath = NormalizeCompletionPath(path);
            string normalizedNewPath = NormalizeCompletionPath(newPath);
            bool isCaseOnlyRename = string.Equals(normalizedPath, normalizedNewPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedPath, normalizedNewPath, StringComparison.Ordinal);

            if (!isCaseOnlyRename)
            {
                if (isDirectory)
                    Directory.Move(path, newPath);
                else
                    File.Move(path, newPath);
                return;
            }

            string parentPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrEmpty(parentPath))
                return;

            string tempPath;
            do
            {
                tempPath = Path.Combine(parentPath, ".ciare-rename-" + Guid.NewGuid().ToString("N"));
            }
            while (File.Exists(tempPath) || Directory.Exists(tempPath));

            if (isDirectory)
            {
                Directory.Move(path, tempPath);
                Directory.Move(tempPath, newPath);
            }
            else
            {
                File.Move(path, tempPath);
                File.Move(tempPath, newPath);
            }
        }

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

        private void UpdateProjectReferencesAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return;

            try
            {
                foreach (string projectPath in EnumerateBuildFiles(_fileExplorerRootPath, "*.csproj"))
                    UpdateProjectFileItemReferences(projectPath, oldPath, newPath, renamedDirectory);

                var projectPathPairs = GetRenamedProjectPathPairs(oldPath, newPath, renamedDirectory).ToList();
                if (projectPathPairs.Count == 0)
                    return;

                foreach (string solutionPath in EnumerateBuildFiles(_fileExplorerRootPath, "*.sln"))
                    UpdateSolutionProjectReferences(solutionPath, projectPathPairs);
            }
            catch
            {
            }
        }

        private static void UpdateProjectFileItemReferences(string projectPath, string oldPath, string newPath,
            bool renamedDirectory)
        {
            try
            {
                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (string.IsNullOrEmpty(projectDirectory))
                    return;

                XDocument document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                bool changed = false;
                foreach (XAttribute attribute in document.Descendants()
                    .SelectMany(element => element.Attributes())
                    .Where(IsProjectItemPathAttribute)
                    .ToList())
                {
                    string value = attribute.Value;
                    if (!TryGetRenamedProjectItemReference(projectDirectory, value, oldPath, newPath,
                            renamedDirectory, out string updatedValue))
                    {
                        continue;
                    }

                    if (string.Equals(value, updatedValue, StringComparison.Ordinal))
                        continue;

                    attribute.Value = updatedValue;
                    changed = true;
                }

                if (changed)
                    document.Save(projectPath, SaveOptions.DisableFormatting);
            }
            catch
            {
            }
        }

        private static bool IsProjectItemPathAttribute(XAttribute attribute)
        {
            string attributeName = attribute.Name.LocalName;
            if (!string.Equals(attributeName, "Include", StringComparison.Ordinal) &&
                !string.Equals(attributeName, "Update", StringComparison.Ordinal) &&
                !string.Equals(attributeName, "Remove", StringComparison.Ordinal))
            {
                return false;
            }

            switch (attribute.Parent?.Name.LocalName)
            {
                case "AdditionalFiles":
                case "Analyzer":
                case "ApplicationDefinition":
                case "Compile":
                case "Content":
                case "EmbeddedResource":
                case "None":
                case "Page":
                case "ProjectReference":
                case "Resource":
                case "SplashScreen":
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetRenamedProjectItemReference(string projectDirectory, string value, string oldPath,
            string newPath, bool renamedDirectory, out string updatedValue)
        {
            updatedValue = string.Empty;
            string candidate = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(candidate) ||
                candidate.Contains("$(") ||
                candidate.IndexOfAny(new[] { '*', '?', ';' }) >= 0)
            {
                return false;
            }

            try
            {
                string fullPath = Path.IsPathRooted(candidate)
                    ? Path.GetFullPath(candidate)
                    : Path.GetFullPath(Path.Combine(projectDirectory, candidate));
                string renamedPath = GetRenamedExplorerPath(fullPath, oldPath, newPath, renamedDirectory);
                if (string.IsNullOrEmpty(renamedPath))
                    return false;

                string projectPath = Path.IsPathRooted(candidate)
                    ? renamedPath
                    : Path.GetRelativePath(projectDirectory, renamedPath);
                updatedValue = PreserveProjectPathSeparators(value, projectPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string PreserveProjectPathSeparators(string originalValue, string path)
        {
            if ((originalValue ?? string.Empty).Contains("/") && !(originalValue ?? string.Empty).Contains("\\"))
                return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetRenamedProjectPathPairs(string oldPath,
            string newPath, bool renamedDirectory)
        {
            if (!renamedDirectory)
            {
                if (string.Equals(Path.GetExtension(oldPath), ".csproj", StringComparison.OrdinalIgnoreCase))
                    yield return new KeyValuePair<string, string>(oldPath, newPath);
                yield break;
            }

            if (!Directory.Exists(newPath))
                yield break;

            foreach (string newProjectPath in EnumerateBuildFiles(newPath, "*.csproj"))
            {
                string relativePath;
                try
                {
                    relativePath = Path.GetRelativePath(newPath, newProjectPath);
                }
                catch
                {
                    continue;
                }

                yield return new KeyValuePair<string, string>(
                    Path.Combine(oldPath, relativePath),
                    newProjectPath);
            }
        }

        private static void UpdateSolutionProjectReferences(string solutionPath,
            IEnumerable<KeyValuePair<string, string>> projectPathPairs)
        {
            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(solutionDirectory))
                    return;

                string content = File.ReadAllText(solutionPath);
                string updatedContent = content;
                foreach (var projectPathPair in projectPathPairs)
                {
                    string oldRelativePath = Path.GetRelativePath(solutionDirectory, projectPathPair.Key)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    string newRelativePath = Path.GetRelativePath(solutionDirectory, projectPathPair.Value)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    updatedContent = ReplaceOrdinalIgnoreCase(updatedContent, oldRelativePath, newRelativePath);
                    updatedContent = ReplaceOrdinalIgnoreCase(updatedContent, oldRelativePath.Replace('\\', '/'),
                        newRelativePath.Replace('\\', '/'));
                }

                if (!string.Equals(content, updatedContent, StringComparison.Ordinal))
                    File.WriteAllText(solutionPath, updatedContent);
            }
            catch
            {
            }
        }

        private static List<string> GetExplorerDeletedProjectPaths(string path, bool isDirectory)
        {
            if (isDirectory)
                return EnumerateBuildFiles(path, "*.csproj")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return IsProjectFilePath(path)
                ? new List<string> { path }
                : new List<string>();
        }

        private static void RemoveProjectsFromWorkspaceSolutions(IList<string> projectPaths, string workspaceFolder)
        {
            if (projectPaths == null || projectPaths.Count == 0 ||
                string.IsNullOrWhiteSpace(workspaceFolder) ||
                !Directory.Exists(workspaceFolder))
            {
                return;
            }

            foreach (string solutionPath in EnumerateBuildFiles(workspaceFolder, "*.sln"))
                RemoveProjectsFromSolution(solutionPath, projectPaths);
        }

        private static void RemoveProjectsFromSolution(string solutionPath, IList<string> projectPaths)
        {
            if (!IsSolutionFilePath(solutionPath) || projectPaths == null || projectPaths.Count == 0)
                return;

            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(solutionDirectory))
                    return;

                var deletedProjects = new HashSet<string>(
                    projectPaths.Select(NormalizeCompletionPath).Where(path => !string.IsNullOrEmpty(path)),
                    StringComparer.OrdinalIgnoreCase);
                if (deletedProjects.Count == 0)
                    return;

                var lines = File.ReadAllLines(solutionPath).ToList();
                var updatedLines = new List<string>(lines.Count);
                var removedProjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < lines.Count; i++)
                {
                    if (TryGetRemovedSolutionProjectGuid(lines[i], solutionDirectory, deletedProjects,
                        out string projectGuid))
                    {
                        if (!string.IsNullOrWhiteSpace(projectGuid))
                            removedProjectGuids.Add(projectGuid);

                        while (i + 1 < lines.Count &&
                            !lines[i].Trim().Equals("EndProject", StringComparison.OrdinalIgnoreCase))
                        {
                            i++;
                        }

                        continue;
                    }

                    updatedLines.Add(lines[i]);
                }

                if (removedProjectGuids.Count == 0)
                    return;

                updatedLines = updatedLines
                    .Where(line => !IsRemovedSolutionProjectConfigurationLine(line, removedProjectGuids))
                    .ToList();

                File.WriteAllLines(solutionPath, updatedLines);
            }
            catch
            {
            }
        }

        private static bool TryGetRemovedSolutionProjectGuid(string line, string solutionDirectory,
            HashSet<string> deletedProjects, out string projectGuid)
        {
            projectGuid = string.Empty;
            if (string.IsNullOrWhiteSpace(line) ||
                string.IsNullOrWhiteSpace(solutionDirectory) ||
                deletedProjects == null ||
                deletedProjects.Count == 0)
            {
                return false;
            }

            Match match = Regex.Match(line,
                @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)"",\s*""(\{[^}]+\})""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                return false;

            string projectPath = match.Groups[1].Value;
            try
            {
                if (!Path.IsPathRooted(projectPath))
                    projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));
            }
            catch
            {
                return false;
            }

            if (!deletedProjects.Contains(NormalizeCompletionPath(projectPath)))
                return false;

            projectGuid = match.Groups[2].Value;
            return true;
        }

        private static bool IsRemovedSolutionProjectConfigurationLine(string line,
            HashSet<string> removedProjectGuids)
        {
            if (string.IsNullOrWhiteSpace(line) || removedProjectGuids == null || removedProjectGuids.Count == 0)
                return false;

            string trimmed = line.TrimStart();
            return removedProjectGuids.Any(guid =>
                trimmed.StartsWith(guid + ".", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(guid + " =", StringComparison.OrdinalIgnoreCase));
        }

        private static string ReplaceOrdinalIgnoreCase(string value, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(oldValue))
                return value;

            var builder = new StringBuilder(value.Length);
            int startIndex = 0;
            while (true)
            {
                int matchIndex = value.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);
                if (matchIndex < 0)
                {
                    builder.Append(value, startIndex, value.Length - startIndex);
                    break;
                }

                builder.Append(value, startIndex, matchIndex - startIndex);
                builder.Append(newValue);
                startIndex = matchIndex + oldValue.Length;
            }

            return builder.ToString();
        }

        private static string GetRenamedExplorerPath(string path, string oldPath, string newPath, bool renamedDirectory)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                return string.Empty;

            try
            {
                string normalizedPath = NormalizeCompletionPath(path);
                string normalizedOldPath = NormalizeCompletionPath(oldPath);

                if (!renamedDirectory)
                    return string.Equals(normalizedPath, normalizedOldPath, StringComparison.OrdinalIgnoreCase)
                        ? newPath
                        : string.Empty;

                if (!IsSameOrChildDirectory(normalizedPath, normalizedOldPath))
                    return string.Empty;

                string relativePath = normalizedPath.Length == normalizedOldPath.Length
                    ? string.Empty
                    : normalizedPath.Substring(normalizedOldPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return string.IsNullOrEmpty(relativePath)
                    ? newPath
                    : Path.Combine(newPath, relativePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetExplorerContextFolderPath()
        {
            string path = (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string;
            return Directory.Exists(path) && IsPathInsideExplorerRoot(path) ? path : string.Empty;
        }

        private bool IsExplorerRootPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                Directory.Exists(_fileExplorerRootPath) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(_fileExplorerRootPath),
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPathInsideExplorerRoot(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                Directory.Exists(_fileExplorerRootPath) &&
                IsSameOrChildDirectory(path, _fileExplorerRootPath);
        }

        private void SelectExplorerPath(string path)
        {
            if (_fileExplorerTree == null || _fileExplorerTree.Nodes.Count == 0 || string.IsNullOrWhiteSpace(path))
                return;

            TreeNode node = FindTreeNodeByPath(_fileExplorerTree.Nodes[0], path);
            if (node != null)
            {
                _fileExplorerTree.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        private void RefreshAndExpandExplorerFolder(string folderPath)
        {
            if (_fileExplorerTree == null || _fileExplorerTree.Nodes.Count == 0)
                return;

            TreeNode node = FindTreeNodeByPath(_fileExplorerTree.Nodes[0], folderPath);
            if (node == null)
                return;

            PopulateDirectoryNode(node);
            if (!node.IsExpanded)
                node.Expand();
        }

        private static bool TryNormalizeExplorerFileName(string value, string extension, out string fileName)
        {
            if (!TryValidateExplorerItemName(value, out fileName))
                return false;

            if (!string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase))
                fileName = Path.ChangeExtension(fileName, extension);

            return true;
        }

        private static bool TryValidateExplorerItemName(string value, out string itemName)
        {
            itemName = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(itemName))
                return false;

            if (itemName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("The name contains characters that cannot be used in a file name.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static string GetUniqueExplorerName(string folderPath, string preferredName)
        {
            string candidate = preferredName;
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(preferredName);
            string extension = Path.GetExtension(preferredName);
            int count = 1;

            while (File.Exists(Path.Combine(folderPath, candidate)) ||
                   Directory.Exists(Path.Combine(folderPath, candidate)))
            {
                count++;
                candidate = string.IsNullOrEmpty(extension)
                    ? $"{preferredName} {count}"
                    : $"{nameWithoutExtension}{count}{extension}";
            }

            return candidate;
        }

        private static string BuildNewCSharpFileContent(string fileName)
        {
            string className = Path.GetFileNameWithoutExtension(fileName);
            className = ToSafeCSharpIdentifier(className);

            return "public class " + className + Environment.NewLine +
                   "{" + Environment.NewLine +
                   "}" + Environment.NewLine;
        }

        private static string ToSafeCSharpIdentifier(string value)
        {
            var builder = new StringBuilder();
            foreach (char ch in value ?? string.Empty)
                builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private string PromptForExplorerName(string title, string labelText, string defaultValue)
        {
            using (var form = new Form())
            using (var label = new Label())
            using (var textBox = new TextBox())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(360, 118);
                form.Font = Font;

                label.AutoSize = true;
                label.Text = labelText;
                label.Location = new Point(12, 14);

                textBox.Text = defaultValue;
                textBox.Location = new Point(12, 38);
                textBox.Width = 336;
                textBox.SelectAll();

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Location = new Point(156, 78);
                okButton.Width = 92;

                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.Location = new Point(256, 78);
                cancelButton.Width = 92;

                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                FrmColorMod.ToogleColorMode(form, GlobalVariables.darkColor);

                return form.ShowDialog(this) == DialogResult.OK
                    ? textBox.Text.Trim()
                    : string.Empty;
            }
        }

        private void fileExplorerTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OpenFileExplorerNode(e.Node);
        }

        private void fileExplorerTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || _fileExplorerTree.SelectedNode == null)
                return;

            OpenFileExplorerNode(_fileExplorerTree.SelectedNode);
            e.Handled = true;
        }

        private void OpenFileExplorerNode(TreeNode node)
        {
            string path = node?.Tag as string;
            if (string.IsNullOrEmpty(path))
                return;

            if (Directory.Exists(path))
            {
                if (node.IsExpanded)
                    node.Collapse();
                else
                    node.Expand();
                return;
            }

            if (!File.Exists(path))
                return;

            OpenFileFromExplorer(path);
        }

        private void OpenFileFromExplorer(string filePath)
        {
            try
            {
                if (TabControllerManage.IsFileOpenedInTab(EditorTabControl, filePath))
                {
                    ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                    return;
                }

                var editor = SelectedEditor.GetSelectedEditor();
                bool useCurrentBlankTab = editor != null &&
                    EditorTabControl.SelectedIndex > 0 &&
                    string.IsNullOrWhiteSpace(editor.Text) &&
                    string.IsNullOrWhiteSpace(EditorTabControl.SelectedTab.ToolTipText);

                if (!useCurrentBlankTab)
                    TabControllerManage.AddNewTab(EditorTabControl);

                FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), filePath);
                RefreshProjectPackageContext(GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScheduleCurrentTypeCheck(TextEditorControl editor)
        {
            if (editor == null)
                return;

            string filePath = GetActiveEditorFilePath();
            if (!IsCSharpFilePath(filePath))
            {
                RealTimeChecker.Cancel(editor, typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
                return;
            }

            if (Directory.Exists(_fileExplorerRootPath) &&
                !IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                InvalidateCompletionWorkspace();
                ClearProjectPackageCompletionReferences();
            }

            string workspaceFolder = GetActiveWorkspaceFolder();
            bool useProjectReferences = !string.IsNullOrEmpty(workspaceFolder) &&
                IsPathInsideFolder(filePath, workspaceFolder);

            RealTimeChecker.ScheduleCheck(editor.Text, editor, typeCheckLbl, errorsLV, errorsTabPage,
                warningsCheckLbl, workspaceFolder, filePath, useProjectReferences);
        }

        private string GetActiveEditorFilePath()
        {
            try
            {
                string path = EditorTabControl.SelectedTab?.ToolTipText?.Trim();
                return File.Exists(path) ? path : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetActiveWorkspaceFolder()
        {
            string filePath = GetActiveEditorFilePath();

            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            if (string.IsNullOrEmpty(filePath) || IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return _fileExplorerRootPath;

            return string.Empty;
        }

        public string GetActiveCompileProjectPath()
        {
            string filePath = GetActiveEditorFilePath();

            if (IsCSharpFilePath(filePath))
                return GetBuildTargetForActiveCSharpFile(filePath);

            if (Directory.Exists(_fileExplorerRootPath))
            {
                if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                    IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
                {
                    return _fileExplorerStartupProjectPath;
                }

                string activeFilePath = File.Exists(filePath) &&
                    IsPathInsideFolder(filePath, _fileExplorerRootPath)
                    ? filePath
                    : null;

                string openFolderTarget = FindBuildTargetFile(_fileExplorerRootPath, activeFilePath);
                if (!string.IsNullOrEmpty(openFolderTarget))
                    return openFolderTarget;
            }

            return string.Empty;
        }

        public string GetActiveRunProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
            {
                return _fileExplorerStartupProjectPath;
            }

            string filePath = GetActiveEditorFilePath();
            if (IsCSharpFilePath(filePath))
            {
                string sourceProject = FindProjectContainingSourceFile(filePath);
                if (string.IsNullOrEmpty(sourceProject))
                    return string.Empty;

                return sourceProject;
            }

            string activeProject = FindProjectFileForPath(GetActiveEditorFilePath(), _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(activeProject))
                return activeProject;

            string selectedPath = _fileExplorerTree?.SelectedNode?.Tag as string;
            string selectedProject = FindProjectFileForPath(selectedPath, _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(selectedProject))
                return selectedProject;

            string rootProject = FindTopLevelBuildFile(_fileExplorerRootPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            return FindSingleRecursiveBuildFile(_fileExplorerRootPath, "*.csproj");
        }

        private string GetBuildTargetForActiveCSharpFile(string filePath)
        {
            if (!Directory.Exists(_fileExplorerRootPath) ||
                !File.Exists(filePath) ||
                !IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                return string.Empty;
            }

            string activeProject = FindProjectContainingSourceFile(filePath);
            if (string.IsNullOrEmpty(activeProject))
                return string.Empty;

            if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath) &&
                ProjectContainsSourceFile(_fileExplorerStartupProjectPath, filePath))
            {
                return _fileExplorerStartupProjectPath;
            }

            string activeSolution = FindContainingSolutionForProjectInWorkspace(activeProject);
            return !string.IsNullOrEmpty(activeSolution) ? activeSolution : activeProject;
        }

        private string FindProjectContainingSourceFile(string filePath)
        {
            if (!IsCSharpFilePath(filePath) ||
                !Directory.Exists(_fileExplorerRootPath) ||
                !IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                return string.Empty;
            }

            foreach (string projectPath in GetProjectCandidatesForSourceFile(filePath))
            {
                if (ProjectContainsSourceFile(projectPath, filePath))
                    return projectPath;
            }

            return string.Empty;
        }

        private IEnumerable<string> GetProjectCandidatesForSourceFile(string filePath)
        {
            var candidates = new List<string>();
            string activeFolder = Path.GetDirectoryName(filePath);

            string activeSolution = FindNearestBuildFile(activeFolder, _fileExplorerRootPath, "*.sln");
            if (!string.IsNullOrEmpty(activeSolution))
                candidates.AddRange(ReadSolutionProjectFiles(activeSolution));

            candidates.AddRange(EnumerateBuildFiles(_fileExplorerRootPath, "*.csproj"));

            return candidates
                .Where(IsProjectFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(projectPath => GetCommonPathLength(filePath, Path.GetDirectoryName(projectPath)))
                .ThenBy(projectPath => projectPath, StringComparer.OrdinalIgnoreCase);
        }

        private string FindContainingSolutionForProjectInWorkspace(string projectPath)
        {
            if (!IsProjectFilePath(projectPath) || !Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string projectFolder = Path.GetDirectoryName(projectPath);
            string solutionPath = FindNearestBuildFile(projectFolder, _fileExplorerRootPath, "*.sln");
            return SolutionFileContainsProject(solutionPath, projectPath) ? solutionPath : string.Empty;
        }

        public string GetActivePackageProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string selectedPath = _fileExplorerTree?.SelectedNode?.Tag as string;
            string selectedProject = FindProjectFileForPath(selectedPath, _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(selectedProject))
                return selectedProject;

            string activeProject = FindProjectFileForPath(GetActiveEditorFilePath(), _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(activeProject))
                return activeProject;

            string rootProject = FindTopLevelBuildFile(_fileExplorerRootPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            return FindSingleRecursiveBuildFile(_fileExplorerRootPath, "*.csproj");
        }

        public string GetActivePackageInstallProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string filePath = GetActiveEditorFilePath();
            if (!File.Exists(filePath) || !IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return string.Empty;

            return FindProjectFileForPath(filePath, _fileExplorerRootPath);
        }

        private string GetActiveEditorPackageProjectPath()
        {
            string activeProject = GetActivePackageInstallProjectPath();
            return !string.IsNullOrEmpty(activeProject) ? activeProject : GetActivePackageProjectPath();
        }

        private static bool IsCSharpFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProjectFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                File.Exists(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSolutionFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                File.Exists(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".sln", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ProjectContainsSourceFile(string projectPath, string sourceFilePath)
        {
            if (!IsProjectFilePath(projectPath) || !IsCSharpFilePath(sourceFilePath) || !File.Exists(sourceFilePath))
                return false;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory) ||
                !IsSameOrChildDirectory(Path.GetDirectoryName(sourceFilePath), projectDirectory))
            {
                return false;
            }

            XDocument document;
            try
            {
                document = XDocument.Load(projectPath);
            }
            catch
            {
                return false;
            }

            if (ProjectItemMatchesSourceFile(document, projectDirectory, sourceFilePath, "Compile", "Remove"))
                return false;

            if (ProjectCompileIncludeMatchesSourceFile(document, projectDirectory, sourceFilePath))
                return true;

            if (!ProjectUsesSdkStyle(document) || !DefaultCompileItemsEnabled(document))
                return false;

            return IsDefaultCompileCandidate(projectDirectory, sourceFilePath);
        }

        private static bool ProjectUsesSdkStyle(XDocument document)
        {
            if (document?.Root == null)
                return false;

            return document.Root.Attribute("Sdk") != null ||
                document.Root.Elements()
                    .Any(element => string.Equals(element.Name.LocalName, "Import", StringComparison.Ordinal) &&
                        element.Attribute("Sdk") != null);
        }

        private static bool DefaultCompileItemsEnabled(XDocument document)
        {
            return !ProjectPropertyIsFalse(document, "EnableDefaultItems") &&
                !ProjectPropertyIsFalse(document, "EnableDefaultCompileItems");
        }

        private static bool ProjectPropertyIsFalse(XDocument document, string propertyName)
        {
            return document?.Root?
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))
                .Any(element => string.Equals((element.Value ?? string.Empty).Trim(), "false",
                    StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static bool IsDefaultCompileCandidate(string projectDirectory, string sourceFilePath)
        {
            if (!IsSameOrChildDirectory(sourceFilePath, projectDirectory))
                return false;

            string relativePath;
            try
            {
                relativePath = Path.GetRelativePath(projectDirectory, sourceFilePath);
            }
            catch
            {
                return false;
            }

            string normalizedRelativePath = relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            if (string.IsNullOrWhiteSpace(normalizedRelativePath) ||
                normalizedRelativePath == ".." ||
                normalizedRelativePath.StartsWith("../"))
            {
                return false;
            }

            string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !segments.Any(segment =>
                string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ProjectCompileIncludeMatchesSourceFile(XDocument document, string projectDirectory,
            string sourceFilePath)
        {
            if (document?.Root == null)
                return false;

            foreach (XElement item in document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal)))
            {
                string include = item.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include) ||
                    !ProjectItemSpecMatchesSourceFile(projectDirectory, include, sourceFilePath))
                {
                    continue;
                }

                string exclude = item.Attribute("Exclude")?.Value;
                if (!string.IsNullOrWhiteSpace(exclude) &&
                    ProjectItemSpecMatchesSourceFile(projectDirectory, exclude, sourceFilePath))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ProjectItemMatchesSourceFile(XDocument document, string projectDirectory,
            string sourceFilePath, string itemName, string attributeName)
        {
            if (document?.Root == null)
                return false;

            foreach (XElement item in document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal)))
            {
                string itemSpec = item.Attribute(attributeName)?.Value;
                if (string.IsNullOrWhiteSpace(itemSpec))
                    continue;

                if (ProjectItemSpecMatchesSourceFile(projectDirectory, itemSpec, sourceFilePath))
                    return true;
            }

            return false;
        }

        private static bool ProjectItemSpecMatchesSourceFile(string projectDirectory, string itemSpec,
            string sourceFilePath)
        {
            foreach (string spec in SplitProjectItemSpec(itemSpec))
            {
                if (spec.Contains("$"))
                    continue;

                bool hasWildcards = spec.IndexOfAny(new[] { '*', '?' }) >= 0;
                if (!hasWildcards)
                {
                    string itemPath = ResolveProjectItemPath(projectDirectory, spec);
                    if (string.Equals(NormalizeCompletionPath(itemPath), NormalizeCompletionPath(sourceFilePath),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    continue;
                }

                string pattern = NormalizeProjectItemPattern(spec);
                string candidate = Path.IsPathRooted(spec)
                    ? NormalizeCompletionPath(sourceFilePath).Replace(Path.DirectorySeparatorChar, '/')
                    : GetProjectRelativePath(projectDirectory, sourceFilePath);

                if (!string.IsNullOrEmpty(candidate) && GlobMatches(pattern, candidate))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> SplitProjectItemSpec(string itemSpec)
        {
            return (itemSpec ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(spec => spec.Trim())
                .Where(spec => !string.IsNullOrWhiteSpace(spec));
        }

        private static string ResolveProjectItemPath(string projectDirectory, string itemSpec)
        {
            try
            {
                return Path.IsPathRooted(itemSpec)
                    ? Path.GetFullPath(itemSpec)
                    : Path.GetFullPath(Path.Combine(projectDirectory, itemSpec));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetProjectRelativePath(string projectDirectory, string sourceFilePath)
        {
            try
            {
                return Path.GetRelativePath(projectDirectory, sourceFilePath)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(Path.AltDirectorySeparatorChar, '/');
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeProjectItemPattern(string itemSpec)
        {
            string pattern = (itemSpec ?? string.Empty)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            while (pattern.StartsWith("./", StringComparison.Ordinal))
                pattern = pattern.Substring(2);

            return pattern;
        }

        private static bool GlobMatches(string pattern, string candidate)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(candidate))
                return false;

            var regex = new StringBuilder("^");
            for (int i = 0; i < pattern.Length; i++)
            {
                char ch = pattern[i];
                if (ch == '*')
                {
                    bool recursive = i + 1 < pattern.Length && pattern[i + 1] == '*';
                    if (recursive && i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        regex.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        regex.Append(recursive ? ".*" : "[^/]*");
                        if (recursive)
                            i++;
                    }
                }
                else if (ch == '?')
                {
                    regex.Append("[^/]");
                }
                else
                {
                    regex.Append(Regex.Escape(ch.ToString()));
                }
            }

            regex.Append("$");
            return Regex.IsMatch(candidate, regex.ToString(),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static IEnumerable<string> ReadSolutionProjectFiles(string solutionPath)
        {
            if (!IsSolutionFilePath(solutionPath))
                yield break;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(solutionDirectory))
                yield break;

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadLines(solutionPath);
            }
            catch
            {
                yield break;
            }

            foreach (string line in lines)
            {
                Match match = Regex.Match(line,
                    @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                string projectPath = match.Groups[1].Value;
                if (!Path.IsPathRooted(projectPath))
                    projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));

                if (IsProjectFilePath(projectPath))
                    yield return projectPath;
            }
        }

        private static int GetCommonPathLength(string filePath, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(folderPath))
                return 0;

            string normalizedFile = NormalizeCompletionPath(filePath);
            string normalizedFolder = NormalizeCompletionPath(folderPath);
            if (string.IsNullOrEmpty(normalizedFile) || string.IsNullOrEmpty(normalizedFolder))
                return 0;

            return normalizedFile.StartsWith(normalizedFolder + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) ||
                normalizedFile.StartsWith(normalizedFolder + Path.AltDirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                ? normalizedFolder.Length
                : 0;
        }

        private string GetProjectPathFromExplorerPath(string path)
        {
            if (IsProjectFilePath(path))
                return path;

            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (File.Exists(path))
                return Directory.Exists(_fileExplorerRootPath)
                    ? FindProjectFileForPath(path, _fileExplorerRootPath)
                    : string.Empty;

            if (!Directory.Exists(path))
                return string.Empty;

            string childProject = FindTopLevelBuildFile(path, "*.csproj");
            if (!string.IsNullOrEmpty(childProject))
                return childProject;

            return Directory.Exists(_fileExplorerRootPath)
                ? FindNearestBuildFile(path, _fileExplorerRootPath, "*.csproj")
                : string.Empty;
        }

        private string GetSolutionPathFromExplorerPath(string path)
        {
            if (IsSolutionFilePath(path))
                return path;

            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string folder = Directory.Exists(path)
                ? path
                : File.Exists(path)
                    ? Path.GetDirectoryName(path)
                    : string.Empty;

            if (string.IsNullOrEmpty(folder))
                return string.Empty;

            if (Directory.Exists(_fileExplorerRootPath))
                return FindNearestBuildFile(folder, _fileExplorerRootPath, "*.sln");

            return FindTopLevelBuildFile(folder, "*.sln");
        }

        private static bool IsAddProjectToSolutionContext(string path, string solutionPath)
        {
            if (!IsSolutionFilePath(solutionPath) || string.IsNullOrWhiteSpace(path))
                return false;

            if (IsSolutionFilePath(path))
                return string.Equals(NormalizeCompletionPath(path),
                    NormalizeCompletionPath(solutionPath), StringComparison.OrdinalIgnoreCase);

            if (!Directory.Exists(path))
                return false;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            return !string.IsNullOrEmpty(solutionDirectory) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(solutionDirectory),
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string FindProjectFileForPath(string path, string workspaceFolder)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(workspaceFolder) ||
                !Directory.Exists(workspaceFolder))
            {
                return string.Empty;
            }

            if (IsProjectFilePath(path) && IsPathInsideFolder(path, workspaceFolder))
                return path;

            string folder = Directory.Exists(path)
                ? path
                : File.Exists(path)
                    ? Path.GetDirectoryName(path)
                    : string.Empty;

            if (string.IsNullOrEmpty(folder) || !IsSameOrChildDirectory(folder, workspaceFolder))
                return string.Empty;

            string childProject = FindTopLevelBuildFile(folder, "*.csproj");
            if (!string.IsNullOrEmpty(childProject))
                return childProject;

            return FindNearestBuildFile(folder, workspaceFolder, "*.csproj");
        }

        private static bool IsPathInsideFolder(string filePath, string folderPath)
        {
            try
            {
                string fullFolder = Path.GetFullPath(folderPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullPath = Path.GetFullPath(filePath);
                return fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string FindBuildTargetFile(string folderPath, string activeFilePath = null)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return string.Empty;

            string rootSolution = FindTopLevelBuildFile(folderPath, "*.sln");
            if (!string.IsNullOrEmpty(rootSolution))
                return rootSolution;

            if (IsCSharpFilePath(activeFilePath))
            {
                string activeFolder = Path.GetDirectoryName(activeFilePath);

                string activeSolution = FindNearestBuildFile(activeFolder, folderPath, "*.sln");
                if (!string.IsNullOrEmpty(activeSolution))
                    return activeSolution;

                string activeProject = FindNearestBuildFile(activeFolder, folderPath, "*.csproj");
                if (!string.IsNullOrEmpty(activeProject))
                    return activeProject;
            }

            string rootProject = FindTopLevelBuildFile(folderPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            string singleSolution = FindSingleRecursiveBuildFile(folderPath, "*.sln");
            if (!string.IsNullOrEmpty(singleSolution))
                return singleSolution;

            return FindSingleRecursiveBuildFile(folderPath, "*.csproj");
        }

        private static string FindBuildTargetFromActiveFile(string filePath)
        {
            if (!IsCSharpFilePath(filePath))
                return string.Empty;

            string activeFolder = Path.GetDirectoryName(filePath);
            string solution = FindNearestBuildFile(activeFolder, null, "*.sln");
            if (!string.IsNullOrEmpty(solution))
                return solution;

            return FindNearestBuildFile(activeFolder, null, "*.csproj");
        }

        private static string FindTopLevelBuildFile(string folderPath, string searchPattern)
        {
            try
            {
                return Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FindNearestBuildFile(string startFolder, string stopFolder, string searchPattern)
        {
            if (string.IsNullOrEmpty(startFolder) || !Directory.Exists(startFolder))
                return string.Empty;

            if (!string.IsNullOrEmpty(stopFolder) && !IsSameOrChildDirectory(startFolder, stopFolder))
                return string.Empty;

            string folder = startFolder;
            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                string buildFile = FindTopLevelBuildFile(folder, searchPattern);
                if (!string.IsNullOrEmpty(buildFile))
                    return buildFile;

                if (!string.IsNullOrEmpty(stopFolder) &&
                    string.Equals(NormalizeCompletionPath(folder), NormalizeCompletionPath(stopFolder), StringComparison.OrdinalIgnoreCase))
                    break;

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || parent == folder)
                    break;

                if (!string.IsNullOrEmpty(stopFolder) && !IsSameOrChildDirectory(parent, stopFolder))
                    break;

                folder = parent;
            }

            return string.Empty;
        }

        private static string FindSingleRecursiveBuildFile(string folderPath, string searchPattern)
        {
            try
            {
                var matches = EnumerateBuildFiles(folderPath, searchPattern)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Take(2)
                    .ToList();

                return matches.Count == 1 ? matches[0] : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IEnumerable<string> EnumerateBuildFiles(string folderPath, string searchPattern)
        {
            var pending = new Stack<string>();
            pending.Push(folderPath);

            while (pending.Count > 0)
            {
                string current = pending.Pop();

                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(current, searchPattern); }
                catch { files = Array.Empty<string>(); }

                foreach (string file in files)
                    yield return file;

                IEnumerable<string> directories;
                try { directories = Directory.EnumerateDirectories(current); }
                catch { directories = Array.Empty<string>(); }

                foreach (string directory in directories)
                {
                    string name = Path.GetFileName(directory);
                    if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase))
                        continue;

                    pending.Push(directory);
                }
            }
        }

        private static bool IsSameOrChildDirectory(string path, string folderPath)
        {
            try
            {
                string fullPath = NormalizeCompletionPath(path);
                string fullFolder = NormalizeCompletionPath(folderPath);

                return string.Equals(fullPath, fullFolder, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullFolder + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsStartupProjectPath(string projectPath)
        {
            return IsProjectFilePath(projectPath) &&
                IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                string.Equals(NormalizeCompletionPath(projectPath),
                    NormalizeCompletionPath(_fileExplorerStartupProjectPath),
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool IsStartupProjectNode(TreeNode node)
        {
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) ||
                !IsProjectFilePath(_fileExplorerStartupProjectPath))
                return false;

            string startupDirectory = Path.GetDirectoryName(_fileExplorerStartupProjectPath);
            return !string.IsNullOrEmpty(startupDirectory) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(startupDirectory),
                    StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyStartupProjectNodeStyle(TreeNode node)
        {
            if (node == null)
                return;

            if (IsStartupProjectNode(node))
            {
                node.BackColor = GlobalVariables.darkColor
                    ? Color.FromArgb(70, 72, 38)
                    : Color.FromArgb(255, 245, 189);
                node.ForeColor = GlobalVariables.darkColor
                    ? Color.FromArgb(255, 235, 150)
                    : Color.FromArgb(80, 63, 0);
                if (!node.ToolTipText.EndsWith("Startup project", StringComparison.Ordinal))
                    node.ToolTipText = node.ToolTipText + Environment.NewLine + "Startup project";
            }
            else
            {
                node.BackColor = Color.Empty;
                node.ForeColor = Color.Empty;
                string marker = Environment.NewLine + "Startup project";
                if (!string.IsNullOrEmpty(node.ToolTipText) &&
                    node.ToolTipText.EndsWith(marker, StringComparison.Ordinal))
                {
                    node.ToolTipText = node.ToolTipText.Substring(0, node.ToolTipText.Length - marker.Length);
                }
            }
        }

        private void UpdateFileExplorerStartupProjectHighlight()
        {
            if (_fileExplorerTree == null || _fileExplorerTree.IsDisposed)
                return;

            _fileExplorerTree.BeginUpdate();
            try
            {
                foreach (TreeNode node in _fileExplorerTree.Nodes)
                    ApplyStartupProjectNodeStyleRecursive(node);
            }
            finally
            {
                _fileExplorerTree.EndUpdate();
            }
        }

        private void ApplyStartupProjectNodeStyleRecursive(TreeNode node)
        {
            ApplyStartupProjectNodeStyle(node);
            foreach (TreeNode child in node.Nodes)
                ApplyStartupProjectNodeStyleRecursive(child);
        }

        private void UpdateStartupProjectAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (string.IsNullOrWhiteSpace(_fileExplorerStartupProjectPath) ||
                !string.Equals(Path.GetExtension(_fileExplorerStartupProjectPath), ".csproj",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string renamedStartupProject = GetRenamedExplorerPath(_fileExplorerStartupProjectPath, oldPath, newPath,
                renamedDirectory);
            if (!string.IsNullOrEmpty(renamedStartupProject))
                _fileExplorerStartupProjectPath = renamedStartupProject;
        }

        private void ClearStartupProjectAfterExplorerDelete(string deletedPath, bool deletedDirectory)
        {
            if (string.IsNullOrWhiteSpace(_fileExplorerStartupProjectPath) ||
                !string.Equals(Path.GetExtension(_fileExplorerStartupProjectPath), ".csproj",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool deletedStartupProject = deletedDirectory
                ? IsSameOrChildDirectory(_fileExplorerStartupProjectPath, deletedPath)
                : string.Equals(NormalizeCompletionPath(_fileExplorerStartupProjectPath),
                    NormalizeCompletionPath(deletedPath), StringComparison.OrdinalIgnoreCase);

            if (deletedStartupProject)
                _fileExplorerStartupProjectPath = string.Empty;
        }

        private void ApplyFileExplorerTheme(string highlight = null)
        {
            if (_fileExplorerPanel == null)
                return;

            var theme = ThemeManager.GetCompletionThemeColors(highlight);
            bool dark = GlobalVariables.darkColor;
            Color backColor = dark ? theme.BackColor : SystemColors.Window;
            Color headerColor = dark ? theme.RowAlternateColor : SystemColors.Control;
            Color foreColor = dark ? theme.ForeColor : Color.Black;
            Color borderColor = dark ? theme.BorderColor : SystemColors.ControlDark;
            Color buttonBackColor = dark ? theme.RowAlternateColor : SystemColors.Control;

            _editorWorkspacePanel.BackColor = backColor;
            _editorExplorerSplitContainer.BackColor = borderColor;
            _editorExplorerSplitContainer.Panel1.BackColor = backColor;
            _editorExplorerSplitContainer.Panel2.BackColor = backColor;
            _fileExplorerPanel.BackColor = backColor;
            _fileExplorerHeader.BackColor = headerColor;
            _fileExplorerTitleLabel.BackColor = headerColor;
            _fileExplorerTitleLabel.ForeColor = foreColor;
            _fileExplorerTree.BackColor = backColor;
            _fileExplorerTree.ForeColor = foreColor;
            _fileExplorerTree.LineColor = borderColor;
            _fileExplorerContentSplitContainer.BackColor = borderColor;
            _fileExplorerContentSplitContainer.Panel1.BackColor = backColor;
            _fileExplorerContentSplitContainer.Panel2.BackColor = backColor;
            _fileExplorerNuGetPanel.BackColor = backColor;
            _fileExplorerNuGetTitleLabel.BackColor = headerColor;
            _fileExplorerNuGetTitleLabel.ForeColor = foreColor;
            _fileExplorerNuGetList.BackColor = backColor;
            _fileExplorerNuGetList.ForeColor = foreColor;

            ApplyFileExplorerButtonTheme(_fileExplorerOpenFolderButton, buttonBackColor, foreColor, borderColor);
            ApplyFileExplorerButtonTheme(_fileExplorerHideButton, buttonBackColor, foreColor, borderColor);
            ApplyFileExplorerButtonTheme(_fileExplorerShowButton, buttonBackColor, foreColor, borderColor);
            UpdateFileExplorerStartupProjectHighlight();
        }

        private static void ApplyFileExplorerButtonTheme(Button button, Color backColor, Color foreColor, Color borderColor)
        {
            if (button == null)
                return;

            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = GlobalVariables.darkColor
                ? GlobalVariables.TabSelectedColor
                : SystemColors.ControlLight;
            button.FlatAppearance.MouseDownBackColor = GlobalVariables.darkColor
                ? GlobalVariables.TabBgColor
                : SystemColors.ControlDark;
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Hide();
            Instance = this;
            this.Text = $"CIARE {GlobalVariables.versionName}";
            TabControllerManage.CleanFileSizeStoreFile(GlobalVariables.tabsFilePath);
            ThemeManager.LoadExternalThemes();
            InitializeFileExplorerPane();
            Initiliaze();
            Console.SetOut(new ControlWriter(outputRBT));
            InitializeEditor.GenerateLiveSessionId();
            InitializeEditor.CleanNugetFolder(GlobalVariables.downloadNugetPath);
            CodeCompletion.CheckCodeCompletion(GlobalVariables.registryPath);
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
            this.Show();
            myProjectContent = new Dom.DefaultProjectContent();
            myProjectContent.Language = CurrentLanguageProperties;
            _apiConnectionEvents = new ApiConnectionEvents();
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;

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
            isLoaded = true;
            ReloadRef();

            // Get last opened tab MD5.
            if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                GlobalVariables.openedFileMD5 = FileHash.GetFileHash(SelectedEditor.GetSelectedEditor().Text);

            if (GlobalVariables.darkColor)
            {
                FrmColorMod.EnableDarkTitleBar(this.Handle);
                FrmColorMod.EnableDarkTitleBar(EditorTabControl.Handle);
            }

            // Run initial type check so errors are underlined from the first load.
            RestoreFileExplorerState();
            QueueFileExplorerLayoutApply();
            QueueEditorLayoutRefresh();
            var startEditor = SelectedEditor.GetSelectedEditor();
            if (startEditor != null && !string.IsNullOrWhiteSpace(startEditor.Text))
                ScheduleCurrentTypeCheck(startEditor);
        }

        private void SetCodeCompletion(int index)
        {
            if (GlobalVariables.OCodeCompletion)
            {
                HostCallbackImplementation.Register(this);
                CodeCompletionKeyHandler.Attach(this, SelectedEditor.GetSelectedEditor(index));
                ToolTipProvider.Attach(this, SelectedEditor.GetSelectedEditor(index));

                EnsureCompletionRegistry();
                SelectedEditor.GetSelectedEditor(index).ActiveTextAreaControl.Refresh();
            }
        }

        private void EnsureCompletionRegistry()
        {
            if (_completionRegistryInitialized && pcRegistry != null)
                return;

            pcRegistry = new Dom.ProjectContentRegistry();
            string completionCachePath = Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion");
            if (!Directory.Exists(completionCachePath))
                Directory.CreateDirectory(completionCachePath);

            pcRegistry.ActivatePersistence(completionCachePath);
            _completionRegistryInitialized = true;
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
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
            RealTimeChecker.Cancel(SelectedEditor.GetSelectedEditor(), typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.RunCode(outputRBT, runCodePb, SelectedEditor.GetSelectedEditor(), splitContainer1, true);
        }

        /// <summary>
        /// Exit Main application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        /// <summary>
        /// Open file on text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int indexTab = EditorTabControl.SelectedIndex;
            FileManage.OpenFileTab(EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
        }

        private void newProjectStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new NewProject())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                OpenCreatedProject(dialog.CreatedProject);
            }
        }

        private void openProjectStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenProjectOrSolutionDialog();
        }

        private void OpenCreatedProject(NewProjectResult project)
        {
            if (project == null)
                return;

            string projectOrSolutionPath = !string.IsNullOrEmpty(project.SolutionFilePath)
                ? project.SolutionFilePath
                : project.ProjectFilePath;

            if (!OpenProjectOrSolutionPath(projectOrSolutionPath, showMessage: false))
                return;

            if (File.Exists(project.StarterFilePath))
                OpenFileFromExplorer(project.StarterFilePath);

            ShowProjectStatus("Created project", project.ProjectFilePath, project.SolutionFilePath);
        }

        private void OpenProjectOrSolutionDialog()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter =
                    "Solution or C# Project (*.sln;*.csproj)|*.sln;*.csproj|Solution Files (*.sln)|*.sln|C# Project Files (*.csproj)|*.csproj|All Files (*.*)|*.*";
                dialog.Title = "Open Project or Solution";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.InitialDirectory = GetInitialProjectDialogDirectory();

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    OpenProjectOrSolutionPath(dialog.FileName, showMessage: true);
            }
        }

        private string GetInitialProjectDialogDirectory()
        {
            if (Directory.Exists(_fileExplorerRootPath))
                return _fileExplorerRootPath;

            string filePath = GetActiveEditorFilePath();
            if (File.Exists(filePath))
                return Path.GetDirectoryName(filePath);

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private bool OpenProjectOrSolutionPath(string filePath, bool showMessage)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            if (!string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Select a .sln or .csproj file.", "Open Project/Solution",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string folderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return false;

            string containingSolutionPath = string.Empty;
            if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                containingSolutionPath = FindContainingSolutionForProject(filePath);
                string solutionDirectory = Path.GetDirectoryName(containingSolutionPath);
                if (!string.IsNullOrEmpty(solutionDirectory) && Directory.Exists(solutionDirectory))
                    folderPath = solutionDirectory;
            }

            LoadFileExplorerFolder(folderPath);
            ToggleFileExplorer(true);

            if (showMessage)
            {
                string projectPath = string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                    ? filePath
                    : GetActivePackageProjectPath();
                string solutionPath = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
                    ? filePath
                    : containingSolutionPath;
                ShowProjectStatus("Opened project/solution", projectPath, solutionPath);
            }

            return true;
        }

        private static string FindContainingSolutionForProject(string projectPath)
        {
            if (!IsProjectFilePath(projectPath))
                return string.Empty;

            string folder = Path.GetDirectoryName(projectPath);
            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                try
                {
                    foreach (string solutionPath in Directory.GetFiles(folder, "*.sln", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    {
                        if (SolutionFileContainsProject(solutionPath, projectPath))
                            return solutionPath;
                    }
                }
                catch
                {
                }

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase))
                    break;

                folder = parent;
            }

            return string.Empty;
        }

        private static bool SolutionFileContainsProject(string solutionPath, string projectPath)
        {
            if (!IsSolutionFilePath(solutionPath) || !IsProjectFilePath(projectPath))
                return false;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(solutionDirectory))
                return false;

            string normalizedProject = NormalizeCompletionPath(projectPath);
            try
            {
                foreach (string line in File.ReadLines(solutionPath))
                {
                    Match match = Regex.Match(line,
                        @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (!match.Success)
                        continue;

                    string referencedProject = match.Groups[1].Value;
                    if (!Path.IsPathRooted(referencedProject))
                        referencedProject = Path.GetFullPath(Path.Combine(solutionDirectory, referencedProject));

                    if (string.Equals(NormalizeCompletionPath(referencedProject), normalizedProject,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private void ShowProjectStatus(string action, string projectPath, string solutionPath,
            string referencePath = null)
        {
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;

            var lines = new List<string> { action + "." };
            if (!string.IsNullOrEmpty(solutionPath))
                lines.Add("Solution: " + solutionPath);
            if (!string.IsNullOrEmpty(projectPath))
                lines.Add("Project: " + projectPath);
            if (!string.IsNullOrEmpty(referencePath))
                lines.Add("Reference: " + referencePath);

            outputRBT.Text = string.Join(Environment.NewLine, lines);
        }


        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) =>
            FileManage.SaveFileTab(EditorTabControl, selectedEditor);


        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.SaveAsDialog(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)  => TextDataChangedAction();

        /// <summary>
        /// Function for check if text is changed in editor.
        /// </summary>
        private void TextDataChangedAction()
        {
            var path = EditorTabControl.SelectedTab.ToolTipText;
            if (File.Exists(path))
            {
                var md5Txt = FileHash.GetFileHash(SelectedEditor.GetSelectedEditor().Text);

                //Remove * depende of file size in comparison text size.
                if (GlobalVariables.openedFileMD5 != md5Txt)
                {
                    var title = $"*{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (Text != title)
                    {
                        this.Text = title;
                        string curentTabTitle = EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        EditorTabControl.SelectedTab.Text = $"*{curentTabTitle}";
                    }
                }
                else
                {
                    var title = $"{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (Text != title)
                    {
                        this.Text = title;
                        string curentTabTitle = EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        EditorTabControl.SelectedTab.Text = $"{curentTabTitle}";
                    }
                }
            }

            LinesManage.GetTotalLinesCount(linesCountLbl);
            SelectedEditor.GetSelectedEditor().Document.FoldingManager.FoldingStrategy = new FoldingStrategy();
            SelectedEditor.GetSelectedEditor().Document.FoldingManager.UpdateFoldings(null, null);

            // Trigger real-time type checking after a short debounce.
            ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());

            // Send live share data to api.
            SendData();
        }

        /// <summary>
        /// Clear the editor and path for new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FileManage.NewFile(selectedEditor, outputRBT);
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
        /// Split window horizontaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitWindowHorizontally(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), true);
            });
        }

        /// <summary>
        /// Split window verticaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitWindowVertically(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), false);
            });
        }

        #region HotKeys Actions

        /// <summary>
        /// Toggle full screen mode: hides the menu bar and toolbar, leaving only the tab strip visible.
        /// </summary>
        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _savedBorderStyle = this.FormBorderStyle;
                _savedWindowState = this.WindowState;
                _markStartFileChkVisible = markStartFileChk.Visible;

                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;

                menuStrip1.Visible = false;
                runCodePb.Visible = false;
                label2.Visible = false;
                label3.Visible = false;
                linesCountLbl.Visible = false;
                linesPositionLbl.Visible = false;
                typeCheckLbl.Visible = false;
                warningsCheckLbl.Visible = false;
                liveStatusPb.Visible = false;
                markStartFileChk.Visible = false;

                fullScreenToolStripMenuItem.Checked = true;
                _isFullScreen = true;
            }
            else
            {
                this.FormBorderStyle = _savedBorderStyle;
                this.WindowState = _savedWindowState;

                menuStrip1.Visible = true;
                runCodePb.Visible = true;
                label2.Visible = true;
                label3.Visible = true;
                linesCountLbl.Visible = true;
                linesPositionLbl.Visible = true;
                typeCheckLbl.Visible = true;
                warningsCheckLbl.Visible = true;
                liveStatusPb.Visible = true;
                markStartFileChk.Visible = _markStartFileChkVisible;

                fullScreenToolStripMenuItem.Checked = false;
                _isFullScreen = false;
            }
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e) => ToggleFullScreen();

        /// <summary>
        /// Override the key combination listener for file management events.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.U | Keys.Control:
                    SwitchSplit.SwitchSplitWindow();
                    return true;
                case Keys.End | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        var liensCount = SelectedEditor.GetSelectedEditor().Document.TotalNumberOfLines;
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(liensCount);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = liensCount;
                    }
                    return true;
                case Keys.Home | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(0);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = 0;
                    }
                    return true;
                case Keys.PageDown | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, true);
                    return true;
                case Keys.PageUp | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, false);
                    return true;
                case Keys.Q | Keys.Control:
                    LiveShareHost liveShareHost = new LiveShareHost();
                    liveShareHost.ShowDialog();
                    return true;
                case Keys.Left | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, true);
                    return true;
                case Keys.Right | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, false);
                    return true;
                case Keys.Tab | Keys.Control:
                    worker = new BackgroundWorker();
                    worker.DoWork += NewHotKeyTab;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.N | Keys.Control:
                    FileManage.NewFile(SelectedEditor.GetSelectedEditor(), outputRBT);
                    return true;
                case Keys.N | Keys.Control | Keys.Shift:
                    newProjectStripMenuItem_Click(this, EventArgs.Empty);
                    return true;
                case Keys.H | Keys.Control:
                    GlobalVariables.findTabOpen = false;
                    FindAndReplace findAndReplace = new FindAndReplace();
                    findAndReplace.Show();
                    return true;
                case Keys.S | Keys.Control:
                    FileManage.SaveToFileDialog();
                    return true;
                case Keys.S | Keys.Control | Keys.Shift:
                    FileManage.SaveAsDialog(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.O | Keys.Control:
                    int indexTab = EditorTabControl.SelectedIndex;
                    FileManage.OpenFileTab(EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
                    return true;
                case Keys.O | Keys.Control | Keys.Shift:
                    OpenProjectOrSolutionDialog();
                    return true;
                case Keys.F | Keys.Control:
                    GlobalVariables.findTabOpen = true;
                    FindAndReplace find = new FindAndReplace();
                    find.ShowDialog();
                    return true;
                case Keys.F12 | Keys.Shift:
                    FindUsagesAtCaret();
                    return true;
                case Keys.F5:
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.RunCode(outputRBT, runCodePb, SelectedEditor.GetSelectedEditor(), splitContainer1, true);
                    return true;
                case Keys.T | Keys.Control:
                    FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.B | Keys.Control:
                    GlobalVariables.binaryPublish = false;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    GlobalVariables.binaryPublish = true;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.W | Keys.Control:
                    worker = new BackgroundWorker();
                    worker.DoWork += SplitWindowHorizontally;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.W | Keys.Control | Keys.Shift:
                    worker = new BackgroundWorker();
                    worker.DoWork += SplitWindowVertically;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.K | Keys.Control:
                    OutputWindowManage.SetOutputWindowState(outputRBT, splitContainer1);
                    return true;
                case Keys.E | Keys.Control:
                    ToggleFileExplorer();
                    return true;
                case Keys.G | Keys.Control:
                    GoToLine goToLine = new GoToLine();
                    goToLine.ShowDialog();
                    return true;
                case Keys.L | Keys.Control:
                    CmdLineArgs cmdLineArgs = new CmdLineArgs();
                    cmdLineArgs.ShowDialog();
                    return true;
                case Keys.P | Keys.Control | Keys.Shift:
                    AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
                    return true;
                case Keys.R | Keys.Control:
                    RefManager refManager = new RefManager();
                    if (!refManager.Visible)
                        refManager.ShowDialog();
                    var editorRef = SelectedEditor.GetSelectedEditor();
                    if (editorRef != null)
                        ScheduleCurrentTypeCheck(editorRef);
                    return true;
                case Keys.F11:
                    ToggleFullScreen();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        public void SetHighLighter(TextEditorControl textEditorControl, string highlight)
        {
            if (highlight.Length > 0)
            {
                textEditorControl.SetHighlighting(highlight);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlight);
            }
            var theme = CIARE.GUI.ThemeManager.GetCompletionThemeColors(highlight);
            ICSharpCode.TextEditor.Gui.CompletionWindow.CodeCompletionListView.ActiveTheme = theme;
            ICSharpCode.TextEditor.Gui.CompletionWindow.DeclarationViewWindow.ThemeBackColor = theme.BackColor;
            ICSharpCode.TextEditor.Gui.CompletionWindow.DeclarationViewWindow.ThemeForeColor = theme.ForeColor;

            if (highlight.StartsWith("C#-Dark") || CIARE.GUI.InitializeEditor.IsDarkTheme(highlight))
            {
                GlobalVariables.darkColor = true;
                GlobalVariables.isVStheme = highlight.EndsWith("VS");
                UpdateThemeColors(highlight);
                var darkBg = GlobalVariables.controlBgColor;
                var darkFg = Color.FromArgb(192, 215, 207);
                DarkModeMain.SetDarkModeMain(this, outputRBT, groupBox1, label2, label3,
                    menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator(), GlobalVariables.isVStheme);
                errorsLV.BackColor = darkBg;
                errorsLV.ForeColor = darkFg;
                ApplyTabControlDarkMode(EditorTabControl, darkBg);
                ApplyTabControlDarkMode(outputTabControl, darkBg);
                ApplyFileExplorerTheme(highlight);
                return;
            }
            GlobalVariables.darkColor = false;
            LightModeMain.SetLightModeMain(this, outputRBT, groupBox1,
                menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
            errorsLV.BackColor = SystemColors.Window;
            errorsLV.ForeColor = Color.Black;
            ApplyTabControlDarkMode(EditorTabControl, SystemColors.Window);
            ApplyTabControlDarkMode(outputTabControl, SystemColors.Window);
            ApplyFileExplorerTheme(highlight);
        }

        /// <summary>
        /// Sets <see cref="GlobalVariables.formBgColor"/> and <see cref="GlobalVariables.controlBgColor"/>
        /// to match the currently selected theme, including external .xshd themes.
        /// </summary>
        private static void UpdateThemeColors(string highlight)
        {
            if (highlight.EndsWith("VS"))
            {
                GlobalVariables.formBgColor = Color.FromArgb(51, 51, 51);
                GlobalVariables.controlBgColor = Color.FromArgb(30, 30, 30);
            }
            else if (highlight.StartsWith("C#-Dark"))
            {
                GlobalVariables.formBgColor = Color.FromArgb(0, 1, 10);
                GlobalVariables.controlBgColor = Color.FromArgb(2, 0, 10);
            }
            else
            {
                // External dark theme — derive background from the .xshd bgcolor.
                var extBg = CIARE.GUI.ThemeManager.GetExternalThemeBgColor(highlight);
                if (extBg.HasValue)
                {
                    GlobalVariables.formBgColor = extBg.Value;
                    GlobalVariables.controlBgColor = extBg.Value;
                }
                else
                {
                    GlobalVariables.formBgColor = Color.FromArgb(0, 1, 10);
                    GlobalVariables.controlBgColor = Color.FromArgb(2, 0, 10);
                }
            }
        }

        private static void ApplyTabControlDarkMode(TabControl tabControl, Color backColor)
        {
            if (tabControl == null)
                return;

            tabControl.BackColor = backColor;
            foreach (TabPage page in tabControl.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = backColor;
            }
            tabControl.Invalidate();
        }

        private void OutputTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= outputTabControl.TabPages.Count)
                return;

            bool dark = GlobalVariables.darkColor;
            var g = e.Graphics;
            var tp = outputTabControl.TabPages[e.Index];
            var tabBounds = outputTabControl.GetTabRect(e.Index);
            bool selected = e.Index == outputTabControl.SelectedIndex;

            Color tabBackColor = dark
                ? (selected ? GlobalVariables.TabSelectedColor : GlobalVariables.TabBgColor)
                : (selected ? SystemColors.Window : SystemColors.Control);
            Color textColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            Color borderColor = dark ? GlobalVariables.TabSelectedColor : SystemColors.ControlDark;

            using (var bgBrush = new SolidBrush(tabBackColor))
                g.FillRectangle(bgBrush, tabBounds);
            using (var borderPen = new Pen(borderColor))
                g.DrawRectangle(borderPen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);

            if (e.Index == outputTabControl.TabPages.Count - 1)
            {
                TabControllerManage.SetTransparentTabBar(outputTabControl, e,
                    GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);
            }

            Rectangle textBounds = Rectangle.Inflate(tabBounds, -8, 0);
            TextRenderer.DrawText(g, tp.Text, tp.Font ?? outputTabControl.Font, textBounds, textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine);
        }

        private void errorsLV_ItemActivate(object sender, EventArgs e)
        {
            if (errorsLV.SelectedItems.Count == 0) return;
            var item = errorsLV.SelectedItems[0];
            if (!int.TryParse(item.SubItems[1].Text, out int targetLine)) return;

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;

            GoToLineNumber.GoToLine(editor, targetLine);
            editor.Focus();
        }

        private string _clickedErrorLine = "";

        private void errorsLV_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = errorsLV.HitTest(e.Location);
            if (hit.Item == null)
            {
                _clickedErrorLine = "";
                return;
            }
            var item = hit.Item;
            _clickedErrorLine = $"{item.SubItems[2].Text}: {item.SubItems[3].Text}";
        }

        private void errorsLV_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (errorsLV.ListViewItemSorter is CIARE.GUI.ListViewColumnSorter sorter)
            {
                sorter.SortColumn = e.Column;
                errorsLV.Sorting = SortOrder.None;
                errorsLV.Sort();
            }
        }

        private void copyErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_clickedErrorLine))
                Clipboard.SetText(_clickedErrorLine);
        }

        private void askAiErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_clickedErrorLine)) return;
            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;
            AiManage.GetDataAIFromError(GlobalVariables.aiKey.ConvertSecureStringToString(), editor.Text, _clickedErrorLine);
        }

        /// <summary>
        /// Load predefined C# code sample for run with Roslyn!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
        }


        /// <summary>
        /// Open about window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        /// <summary>
        /// Show hotkeys info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void HotKeyToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            HotKeys hotKeys = new HotKeys();
            hotKeys.ShowDialog();
        }

        /// <summary>
        /// Compile code to binary exe file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = false;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = true;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
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

            if (_editorLayoutRefreshTimer != null)
            {
                _editorLayoutRefreshTimer.Stop();
                _editorLayoutRefreshTimer.Tick -= OnEditorLayoutRefreshTimer;
                _editorLayoutRefreshTimer.Dispose();
                _editorLayoutRefreshTimer = null;
            }
            _pendingEditorLayoutRefresh = false;

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
            try
            {
                FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath, GlobalVariables.savedFileNoMD5Check);
            }
            catch
            {
            }
        }


        #region Hotkeys for Edit Menu
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^z");
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^y");
        }

        private void cutStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^x");
        }

        private void copyStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^c");
        }

        private void pasteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^v");
        }

        private void deleteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("{DELETE}");
        }

        private void replaceStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^h");
        }

        private void selectAllStripMenuItem3_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^a");
        }

        private void splitEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^w");
        }

        private void splitVEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker = new BackgroundWorker();
            worker.DoWork += SplitWindowVertically;
            worker.RunWorkerAsync();
        }

        private void showHideSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^k");
        }

        private void showHideExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleFileExplorer();
        }

        private void goToLineStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^g");
        }

        private void cmdLinesArgsStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^l");
        }


        private void finStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^f");
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options options = new Options();
            options.ShowDialog();
        }

        private void chatGPTCTRLShiftPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
        }

        private void referenceAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^r");
        }

        #endregion

        /// <summary>
        /// Hide output richtextbox on textEditorControl1 focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Enter(object sender, EventArgs e)
        {
            if (!visibleSplitContainerAutoHide)
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer1);
                visibleSplitContainer = true;
            }
        }

        #region Code Completion settup.

        /// <summary>
        /// Relaod ref in texteditor control.
        /// </summary>
        public void ReloadRef()
        {
            if (GlobalVariables.OCodeCompletion)
            {
                if (parserThread != null && parserThread.IsAlive)
                    return;

                parserThread = new Thread(ParserThread);
                parserThread.IsBackground = true;
                parserThread.Start();
            }
        }
        private HashSet<string> _alreadyLoaded = new HashSet<string>();
        void ParserThread()
        {
            lock (_completionDataLock)
                myProjectContent.AddReferencedContent(pcRegistry.Mscorlib);
            ParseStep();

            Dom.IProjectContent[] total = pcRegistry.LoadAll();

            foreach (var item in total)
            {
                if (_alreadyLoaded.Contains(item.ToString()))
                    continue;

                _alreadyLoaded.Add(item.ToString());

                lock (_completionDataLock)
                {
                    myProjectContent.AddReferencedContent(item);

                    if (myProjectContent is Dom.ReflectionProjectContent myObj) myObj.InitializeReferences();
                }
            }

            while (!IsDisposed)
            {
                ParseStep();
                Thread.Sleep(2000);
            }
        }

        void ParseStep()
        {
            string code = null;
            CompletionScopeSnapshot completionScope = default;
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                code = SelectedEditor.GetSelectedEditor().Text;
                completionScope = RefreshCompletionScope(GetActiveEditorFilePath());
            }));
            var workspaceCompletionClasses = new List<WorkspaceCompletionClass>();
            AddRoslynCompletionClasses(workspaceCompletionClasses, code, completionScope.CurrentFilePath);
            var topLevelFunctions = new List<WorkspaceCompletionItem>();
            CollectTopLevelLocalFunctions(topLevelFunctions, code, completionScope.CurrentFilePath);
            Dom.ICompilationUnit activeCompilationUnit = ParseCompletionCompilationUnit(code,
                completionScope.CurrentFilePath);
            if (!IsCompletionScopeCurrent(completionScope))
                return;

            SetActiveCompletionCompilationUnit(activeCompilationUnit, completionScope.CurrentFilePath);
            ParseWorkspaceFilesForCompletion(completionScope, workspaceCompletionClasses);
            if (!IsCompletionScopeCurrent(completionScope))
                return;

            lock (_completionDataLock)
            {
                if (!IsCompletionScopeCurrent(completionScope))
                    return;

                _workspaceCompletionClasses.Clear();
                _workspaceCompletionClasses.AddRange(workspaceCompletionClasses);
                _topLevelLocalFunctions.Clear();
                _topLevelLocalFunctions.AddRange(topLevelFunctions);
            }
        }

        internal void RefreshActiveCompletionUnit(string code)
        {
            if (!GlobalVariables.OCodeCompletion || myProjectContent == null)
                return;

            CompletionScopeSnapshot completionScope = RefreshCompletionScope(GetActiveEditorFilePath());
            SetActiveCompletionCompilationUnit(ParseCompletionCompilationUnit(code, completionScope.CurrentFilePath),
                completionScope.CurrentFilePath);
        }

        private Dom.ICompilationUnit ParseCompletionCompilationUnit(string code, string currentFilePath)
        {
            string parsedCode = PrepareCodeForNRefactoryCompletion(code ?? string.Empty, currentFilePath,
                out _, out _, out _);
            NRefactory.SupportedLanguage supportedLanguage = IsVisualBasic
                ? NRefactory.SupportedLanguage.VBNet
                : NRefactory.SupportedLanguage.CSharp;

            using (TextReader textReader = new StringReader(parsedCode))
            using (NRefactory.IParser parser = NRefactory.ParserFactory.CreateParser(supportedLanguage, textReader))
            {
                parser.ParseMethodBodies = false;
                parser.Parse();
                return ConvertCompilationUnit(parser.CompilationUnit);
            }
        }

        private void SetActiveCompletionCompilationUnit(Dom.ICompilationUnit compilationUnit,
            string currentFilePath)
        {
            if (compilationUnit == null)
                return;

            if (compilationUnit is Dom.DefaultCompilationUnit dcuCurrent &&
                !string.IsNullOrEmpty(currentFilePath))
            {
                dcuCurrent.FileName = currentFilePath;
            }

            lock (_completionDataLock)
            {
                myProjectContent.UpdateCompilationUnit(lastCompilationUnit, compilationUnit, DummyFileName);
                lastCompilationUnit = compilationUnit;
                parseInformation.SetCompilationUnit(compilationUnit);
            }
        }

        private struct CompletionScopeSnapshot
        {
            public string CurrentFilePath;
            public string WorkspaceFolder;
            public List<string> SourceFolders;
            public string WorkspaceKey;
            public string FileKey;
            public int Version;
        }

        private CompletionScopeSnapshot RefreshCompletionScope(string currentFilePath)
        {
            string projectPath = GetCompletionProjectPath(currentFilePath);
            string workspaceFolder = GetCompletionWorkspaceFolder(currentFilePath);
            var projectPaths = GetCompletionProjectPaths(projectPath);
            var sourceFolders = GetCompletionSourceFolders(projectPaths);
            string workspaceKey = BuildCompletionWorkspaceKey(projectPaths);
            if (string.IsNullOrEmpty(workspaceKey))
                workspaceKey = NormalizeCompletionPath(workspaceFolder);
            string fileKey = NormalizeCompletionPath(currentFilePath);
            bool workspaceChanged = !string.Equals(workspaceKey, _completionWorkspaceKey,
                StringComparison.OrdinalIgnoreCase);
            bool fileChanged = !string.Equals(fileKey, _completionFileKey,
                StringComparison.OrdinalIgnoreCase);

            if (workspaceChanged || fileChanged)
            {
                _completionWorkspaceKey = workspaceKey;
                _completionFileKey = fileKey;
                Interlocked.Increment(ref _completionWorkspaceVersion);

                if (workspaceChanged)
                    ClearWorkspaceCompletionData();
            }

            return new CompletionScopeSnapshot
            {
                CurrentFilePath = currentFilePath,
                WorkspaceFolder = workspaceFolder,
                SourceFolders = sourceFolders,
                WorkspaceKey = workspaceKey,
                FileKey = fileKey,
                Version = Volatile.Read(ref _completionWorkspaceVersion)
            };
        }

        private bool IsCompletionScopeCurrent(CompletionScopeSnapshot scope)
        {
            return scope.Version == Volatile.Read(ref _completionWorkspaceVersion) &&
                string.Equals(scope.WorkspaceKey, _completionWorkspaceKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(scope.FileKey, _completionFileKey, StringComparison.OrdinalIgnoreCase);
        }

        private void InvalidateCompletionWorkspace()
        {
            _completionWorkspaceKey = string.Empty;
            _completionFileKey = string.Empty;
            Interlocked.Increment(ref _completionWorkspaceVersion);
            ClearWorkspaceCompletionData();
        }

        private void ParseWorkspaceFilesForCompletion(CompletionScopeSnapshot scope, List<WorkspaceCompletionClass> workspaceCompletionClasses)
        {
            List<string> sourceFolders = scope.SourceFolders ?? new List<string>();
            string currentFilePath = scope.CurrentFilePath;
            if (!IsCompletionScopeCurrent(scope))
                return;

            if (sourceFolders.Count == 0)
            {
                ClearWorkspaceCompletionData();
                return;
            }

            string normalizedCurrent = NormalizeCompletionPath(currentFilePath);
            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string sourceFolder in sourceFolders)
            {
                if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
                    continue;

                foreach (var filePath in GetWorkspaceCsFiles(sourceFolder))
                {
                    if (!IsCompletionScopeCurrent(scope))
                        return;

                    if (!string.IsNullOrEmpty(normalizedCurrent) &&
                        string.Equals(NormalizeCompletionPath(filePath), normalizedCurrent, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!visitedPaths.Add(filePath))
                        continue;

                    try
                    {
                        string originalFileCode = File.ReadAllText(filePath);
                        AddRoslynCompletionClasses(workspaceCompletionClasses, originalFileCode, filePath);
                        string fileCode = PrepareCodeForNRefactoryCompletion(originalFileCode, filePath,
                            out _, out _, out _);
                        Dom.ICompilationUnit newCU;
                        using (var reader = new StringReader(fileCode))
                        using (NRefactory.IParser p = NRefactory.ParserFactory.CreateParser(NRefactory.SupportedLanguage.CSharp, reader))
                        {
                            p.ParseMethodBodies = false;
                            p.Parse();
                            newCU = ConvertCompilationUnit(p.CompilationUnit);
                        }
                        if (newCU is Dom.DefaultCompilationUnit dcuW)
                            dcuW.FileName = filePath;
                        lock (_completionDataLock)
                        {
                            if (!IsCompletionScopeCurrent(scope))
                                return;

                            _workspaceCompilationUnits.TryGetValue(filePath, out var oldCU);
                            myProjectContent.UpdateCompilationUnit(oldCU, newCU, filePath);
                            _workspaceCompilationUnits[filePath] = newCU;
                        }
                    }
                    catch { }
                }
            }

            lock (_completionDataLock)
            {
                if (!IsCompletionScopeCurrent(scope))
                    return;

                var toRemove = _workspaceCompilationUnits.Keys.Where(p => !visitedPaths.Contains(p)).ToList();
                foreach (var path in toRemove)
                {
                    myProjectContent.RemoveCompilationUnit(_workspaceCompilationUnits[path]);
                    _workspaceCompilationUnits.Remove(path);
                }
            }
        }

        internal ArrayList GetWorkspaceMethodCompletionData(string prefix)
        {
            var result = new ArrayList();
            if (string.IsNullOrWhiteSpace(prefix))
                return result;

            if (!ShouldUseWorkspaceCompletionData())
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            lock (_completionDataLock)
            {
                AddCompilationUnitMethods(result, seen, parseInformation.MostRecentCompilationUnit, prefix);
                foreach (var unit in _workspaceCompilationUnits.Values)
                {
                    AddCompilationUnitMethods(result, seen, unit, prefix);
                    if (result.Count >= WorkspaceCompletionMethodLimit)
                        break;
                }

                foreach (var func in _topLevelLocalFunctions)
                {
                    if (func.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && seen.Add(func.Name))
                        result.Add(new DefaultCompletionData(func.Name, func.Description, func.ImageIndex));
                }
            }

            return result;
        }

        internal ArrayList GetWorkspaceMemberCompletionData(string expression)
        {
            var result = new ArrayList();
            string normalizedExpression = NormalizeCompletionExpression(expression);
            if (string.IsNullOrEmpty(normalizedExpression))
                return result;

            if (!ShouldUseWorkspaceCompletionData())
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            lock (_completionDataLock)
            {
                foreach (var completionClass in _workspaceCompletionClasses)
                {
                    if (!MatchesWorkspaceClass(completionClass, normalizedExpression))
                        continue;

                    AddWorkspaceClassCompletionItems(result, seen, completionClass);
                    if (result.Count >= WorkspaceCompletionMethodLimit)
                        return result;
                }

                if (result.Count == 0)
                    AddWorkspaceNamespaceCompletionItems(result, seen, normalizedExpression, _workspaceCompletionClasses);
            }

            return result;
        }

        internal ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText)
        {
            var result = new ArrayList();
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return result;

            try
            {
                var context = BuildRoslynCompletionContext(code);
                if (context == null)
                    return result;

                int position = Math.Max(0, Math.Min(caretOffset, code.Length));
                string completionExpression = NormalizeCompletionExpression(expressionText);
                if (string.IsNullOrEmpty(completionExpression))
                    completionExpression = GetMemberCompletionExpressionText(code, position);

                ExpressionSyntax expression = FindRoslynMemberAccessExpression(context.Root, position, completionExpression);
                if (expression == null)
                    return result;

                var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);
                var typeInfo = context.SemanticModel.GetTypeInfo(expression);
                ISymbol expressionSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                if (expressionSymbol is INamespaceSymbol namespaceSymbol)
                {
                    AddRoslynSymbolsToCompletionData(result, namespaceSymbol.GetMembers(), string.Empty,
                        staticOnly: false, instanceOnly: false);
                    return result;
                }

                bool staticOnly = expressionSymbol is INamedTypeSymbol;
                ITypeSymbol typeSymbol = staticOnly
                    ? expressionSymbol as ITypeSymbol
                    : typeInfo.Type ?? typeInfo.ConvertedType;
                if (typeSymbol == null)
                    return result;

                AddRoslynSymbolsToCompletionData(result,
                    GetRoslynTypeMembers(typeSymbol, staticOnly),
                    string.Empty,
                    staticOnly,
                    instanceOnly: !staticOnly);
            }
            catch
            {
            }

            return result;
        }

        private static string GetMemberCompletionExpressionText(string code, int caretOffset)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            int offset = Math.Max(0, Math.Min(caretOffset, code.Length));
            while (offset > 0 && char.IsWhiteSpace(code[offset - 1]))
                offset--;

            if (offset > 0 && code[offset - 1] == '.')
                offset--;

            while (offset > 0 && char.IsWhiteSpace(code[offset - 1]))
                offset--;

            int end = offset;
            while (offset > 0 && IsSimpleMemberExpressionChar(code[offset - 1]))
                offset--;

            return end > offset
                ? NormalizeCompletionExpression(code.Substring(offset, end - offset))
                : string.Empty;
        }

        private static bool IsSimpleMemberExpressionChar(char ch)
        {
            return char.IsLetterOrDigit(ch) ||
                   ch == '_' ||
                   ch == '@' ||
                   ch == '.';
        }

        internal ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix)
        {
            var result = new ArrayList();
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return result;

            try
            {
                var context = BuildRoslynCompletionContext(code);
                if (context == null)
                    return result;

                int position = Math.Max(0, Math.Min(caretOffset, code.Length));
                AddRoslynSymbolsToCompletionData(result,
                    context.SemanticModel.LookupSymbols(position),
                    prefix,
                    staticOnly: false,
                    instanceOnly: false);
            }
            catch
            {
            }

            return result;
        }

        private RoslynCompletionContext BuildRoslynCompletionContext(string code)
        {
            string currentFilePath = GetActiveEditorFilePath();
            string projectPath = GetCompletionProjectPath(currentFilePath);
            var parseOptions = BuildCompletionParseOptions();
            SyntaxTree activeTree;
            var syntaxTrees = BuildRoslynCompletionSyntaxTrees(code, currentFilePath, projectPath,
                parseOptions, out activeTree);
            if (activeTree == null)
                return null;

            var compilation = CSharpCompilation.Create(
                "__CiareCompletion",
                syntaxTrees,
                BuildCompletionReferences(projectPath),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: GlobalVariables.OUnsafeCode));

            var root = activeTree.GetCompilationUnitRoot();
            return new RoslynCompletionContext(
                activeTree,
                root,
                compilation,
                compilation.GetSemanticModel(activeTree, true));
        }

        private List<SyntaxTree> BuildRoslynCompletionSyntaxTrees(string code, string currentFilePath,
            string projectPath, CSharpParseOptions parseOptions, out SyntaxTree activeTree)
        {
            string activePath = string.IsNullOrWhiteSpace(currentFilePath) ? DummyFileName : currentFilePath;
            activeTree = CSharpSyntaxTree.ParseText(code ?? string.Empty, parseOptions, path: activePath);
            var syntaxTrees = new List<SyntaxTree> { activeTree };

            var projectPaths = GetCompletionProjectPaths(projectPath);
            AddRoslynCompletionGlobalUsings(syntaxTrees, projectPaths, parseOptions);

            var sourceFolders = GetCompletionSourceFolders(projectPaths);
            if (sourceFolders.Count == 0)
                return syntaxTrees;

            string normalizedCurrent = NormalizeCompletionPath(currentFilePath);
            var seenSourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(normalizedCurrent))
                seenSourceFiles.Add(normalizedCurrent);

            int sourceFileCount = 0;
            foreach (string sourceFolder in sourceFolders)
            {
                foreach (string filePath in GetWorkspaceCsFiles(sourceFolder))
                {
                    string normalizedFile = NormalizeCompletionPath(filePath);
                    if (string.IsNullOrEmpty(normalizedFile) || !seenSourceFiles.Add(normalizedFile))
                        continue;

                    if (++sourceFileCount > RoslynCompletionSourceFileLimit)
                        return syntaxTrees;

                    AddRoslynCompletionSyntaxTree(syntaxTrees, filePath, parseOptions);
                }
            }

            return syntaxTrees;
        }

        private static void AddRoslynCompletionGlobalUsings(List<SyntaxTree> syntaxTrees,
            IList<string> projectPaths, CSharpParseOptions parseOptions)
        {
            var addedGeneratedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string projectPath in projectPaths)
            {
                string globalUsingsPath = FindProjectGlobalUsingsFile(projectPath);
                if (!string.IsNullOrEmpty(globalUsingsPath) &&
                    addedGeneratedFiles.Add(NormalizeCompletionPath(globalUsingsPath)))
                {
                    AddRoslynCompletionSyntaxTree(syntaxTrees, globalUsingsPath, parseOptions);
                    continue;
                }

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(BuildCompletionImplicitUsingsCode(projectPath),
                    parseOptions, path: GetCompletionImplicitUsingsTreePath(projectPath)));
            }
        }

        private static string GetCompletionImplicitUsingsTreePath(string projectPath)
        {
            string normalizedProject = NormalizeCompletionPath(projectPath);
            string fileName = string.IsNullOrEmpty(normalizedProject)
                ? "Project"
                : Path.GetFileNameWithoutExtension(normalizedProject);
            return "__CIARE_ImplicitUsings_" + fileName + ".g.cs";
        }

        private static void AddRoslynCompletionSyntaxTree(List<SyntaxTree> syntaxTrees,
            string filePath, CSharpParseOptions parseOptions)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(filePath),
                    parseOptions, path: filePath));
            }
            catch
            {
            }
        }

        private static List<string> GetCompletionProjectPaths(string projectPath)
        {
            var projectPaths = new List<string>();
            AddCompletionProjectPath(projectPath, projectPaths,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return projectPaths;
        }

        private static void AddCompletionProjectPath(string projectPath, List<string> projectPaths,
            HashSet<string> visited)
        {
            if (!IsProjectFilePath(projectPath))
                return;

            string normalizedProject = NormalizeCompletionPath(projectPath);
            if (string.IsNullOrEmpty(normalizedProject) || !visited.Add(normalizedProject))
                return;

            projectPaths.Add(projectPath);
            foreach (string referencedProjectPath in ProjectReferenceManager.GetProjectReferences(projectPath))
                AddCompletionProjectPath(referencedProjectPath, projectPaths, visited);
        }

        private static List<string> GetCompletionSourceFolders(IList<string> projectPaths)
        {
            return projectPaths
                .Select(GetProjectDirectory)
                .Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetProjectDirectory(string projectPath)
        {
            try
            {
                return string.IsNullOrWhiteSpace(projectPath) ? string.Empty : Path.GetDirectoryName(projectPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildCompletionWorkspaceKey(IList<string> projectPaths)
        {
            if (projectPaths == null || projectPaths.Count == 0)
                return string.Empty;

            return string.Join("|", projectPaths
                .Select(NormalizeCompletionPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        private static List<string> GetCompletionFrameworkReferencePaths(string projectPath)
        {
            return GetCompletionProjectPaths(projectPath)
                .SelectMany(path => ProjectNuGetManager.GetFrameworkReferencePaths(path))
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetCompletionCompileReferencePaths(string projectPath)
        {
            return GetCompletionProjectPaths(projectPath)
                .SelectMany(path => ProjectNuGetManager.GetCompileReferencePaths(path))
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string GetCompletionWorkspaceFolder(string currentFilePath)
        {
            string projectPath = GetCompletionProjectPath(currentFilePath);
            if (string.IsNullOrEmpty(projectPath))
                return string.Empty;

            string projectFolder = Path.GetDirectoryName(projectPath);
            return !string.IsNullOrEmpty(projectFolder) && Directory.Exists(projectFolder)
                ? projectFolder
                : string.Empty;
        }

        private bool ShouldUseWorkspaceCompletionData()
        {
            string projectPath = GetCompletionProjectPath(GetActiveEditorFilePath());
            return GetCompletionSourceFolders(GetCompletionProjectPaths(projectPath)).Count > 0;
        }

        internal void ResetCompletionWorkspaceIfInactive()
        {
            CompletionScopeSnapshot completionScope = RefreshCompletionScope(GetActiveEditorFilePath());
            if (string.IsNullOrEmpty(completionScope.WorkspaceFolder))
            {
                InvalidateCompletionWorkspace();
                ClearProjectPackageCompletionReferences();
            }
        }

        private void ClearWorkspaceCompletionData()
        {
            if (myProjectContent == null)
                return;

            lock (_completionDataLock)
            {
                foreach (var pair in _workspaceCompilationUnits)
                    myProjectContent.RemoveCompilationUnit(pair.Value);

                _workspaceCompilationUnits.Clear();
                _workspaceCompletionClasses.Clear();
                _topLevelLocalFunctions.Clear();
            }
        }

        internal ArrayList FilterCompletionDataForActiveProject(ArrayList completionData)
        {
            if (completionData == null || completionData.Count == 0)
                return completionData;

            string activeFilePath = GetActiveEditorFilePath();
            var sourceFolders = GetCompletionSourceFolders(
                GetCompletionProjectPaths(GetCompletionProjectPath(activeFilePath)));
            var filtered = new ArrayList(completionData.Count);

            foreach (object item in completionData)
            {
                if (ShouldKeepCompletionDataItem(item, activeFilePath, sourceFolders))
                    filtered.Add(item);
            }

            return filtered;
        }

        private bool ShouldKeepCompletionDataItem(object item, string activeFilePath, IList<string> sourceFolders)
        {
            string sourcePath = GetCompletionDataSourcePath(item);
            if (string.IsNullOrWhiteSpace(sourcePath))
                return true;

            if (!string.Equals(Path.GetExtension(sourcePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return true;

            string normalizedSource = NormalizeCompletionPath(sourcePath);
            if (string.IsNullOrEmpty(normalizedSource))
                return true;

            string normalizedActive = NormalizeCompletionPath(activeFilePath);
            if (!string.IsNullOrEmpty(normalizedActive) &&
                string.Equals(normalizedSource, normalizedActive, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string sourceFolder in sourceFolders ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(sourceFolder) &&
                    Directory.Exists(sourceFolder) &&
                    IsPathInsideFolder(sourcePath, sourceFolder))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetCompletionDataSourcePath(object item)
        {
            try
            {
                if (item is Dom.IClass completionClass)
                    return completionClass.CompilationUnit?.FileName ?? string.Empty;

                if (item is Dom.IMember completionMember)
                    return completionMember.DeclaringType?.CompilationUnit?.FileName ?? string.Empty;
            }
            catch
            {
            }

            return string.Empty;
        }

        private static IEnumerable<MetadataReference> BuildCompletionReferences(string projectPath)
        {
            var references = new List<MetadataReference>();
            var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var referenceAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencePath in GetCompletionFrameworkReferencePaths(projectPath))
                AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);

            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.customRefList);
            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.filteredCustomRef);
            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.customRefAsm);

            foreach (var referencePath in GetCompletionCompileReferencePaths(projectPath))
                AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);

            string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrEmpty(trusted))
            {
                foreach (var referencePath in trusted.Split(Path.PathSeparator))
                    AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);
            }

            return references;
        }

        private static void AddCompletionReferencePaths(List<MetadataReference> references,
            HashSet<string> referencePaths, HashSet<string> referenceAssemblyNames,
            IEnumerable<string> storedReferences)
        {
            if (storedReferences == null)
                return;

            foreach (string storedReference in storedReferences)
                AddCompletionReference(references, referencePaths, referenceAssemblyNames,
                    GetCompletionReferencePath(storedReference));
        }

        private static void AddCompletionReference(List<MetadataReference> references,
            HashSet<string> referencePaths, HashSet<string> referenceAssemblyNames, string referencePath)
        {
            if (string.IsNullOrWhiteSpace(referencePath) || !File.Exists(referencePath))
                return;

            try
            {
                string normalizedPath = NormalizeCompletionPath(referencePath);
                if (!referencePaths.Add(normalizedPath))
                    return;

                string assemblyName = GetCompletionReferenceAssemblyName(referencePath);
                if (!string.IsNullOrEmpty(assemblyName) &&
                    !referenceAssemblyNames.Add(assemblyName))
                {
                    referencePaths.Remove(normalizedPath);
                    return;
                }

                references.Add(MetadataReference.CreateFromFile(referencePath));
            }
            catch
            {
            }
        }

        private static string GetCompletionReferenceAssemblyName(string referencePath)
        {
            try
            {
                return AssemblyName.GetAssemblyName(referencePath).Name ?? string.Empty;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(referencePath);
            }
        }

        private static string GetCompletionReferencePath(string storedReference)
        {
            if (string.IsNullOrWhiteSpace(storedReference))
                return string.Empty;

            string[] parts = storedReference.Split('|');
            return parts.Length >= 2 ? parts[1] : storedReference;
        }

        private static CSharpParseOptions BuildCompletionParseOptions()
        {
            string framework = GlobalVariables.Framework ?? string.Empty;
            var languageVersion = LanguageVersion.Default;
            if (framework.StartsWith("net6.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp10;
            else if (framework.StartsWith("net7.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp11;
            else if (framework.StartsWith("net8.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp12;
            else if (framework.StartsWith("net9.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp13;
            else if (framework.StartsWith("net10.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp14;

            return CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        }

        private static string BuildCompletionImplicitUsingsCode(string projectPath)
        {
            var namespaces = ProjectNuGetManager.GetImplicitUsingNamespaces(projectPath);
            if (namespaces.Count == 0)
                namespaces.AddRange(CompletionImplicitUsingNamespaces);

            return string.Join(Environment.NewLine,
                namespaces.Distinct(StringComparer.Ordinal).Select(ns => $"global using {ns};"));
        }

        private static ExpressionSyntax FindRoslynMemberAccessExpression(
            CompilationUnitSyntax root, int caretOffset, string expressionText)
        {
            if (root == null)
                return null;

            int lookupPosition = Math.Max(0, Math.Min(caretOffset - 1, root.FullSpan.End));
            SyntaxToken token = root.FindToken(lookupPosition);
            var memberAccess = token.Parent?.AncestorsAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(access => access.OperatorToken.SpanStart <= lookupPosition &&
                                 access.SpanStart <= lookupPosition)
                .OrderByDescending(access => access.SpanStart)
                .FirstOrDefault();
            if (memberAccess != null)
                return memberAccess.Expression;

            string normalizedExpression = NormalizeCompletionExpression(expressionText);
            if (string.IsNullOrEmpty(normalizedExpression))
                return null;

            return root.DescendantNodes()
                .OfType<ExpressionSyntax>()
                .Where(expression => expression.Span.End <= caretOffset &&
                                     string.Equals(NormalizeCompletionExpression(expression.ToString()),
                                         normalizedExpression, StringComparison.Ordinal))
                .OrderByDescending(expression => expression.Span.End)
                .FirstOrDefault();
        }

        private static IEnumerable<ISymbol> GetRoslynTypeMembers(ITypeSymbol typeSymbol, bool staticOnly)
        {
            if (typeSymbol == null)
                yield break;

            if (staticOnly)
            {
                foreach (var member in typeSymbol.GetMembers())
                    yield return member;
                yield break;
            }

            for (INamedTypeSymbol current = typeSymbol as INamedTypeSymbol;
                current != null;
                current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                    yield return member;
            }
        }

        private static void AddRoslynSymbolsToCompletionData(ArrayList result, IEnumerable<ISymbol> symbols,
            string prefix, bool staticOnly, bool instanceOnly)
        {
            if (result == null || symbols == null)
                return;

            string normalizedPrefix = prefix ?? string.Empty;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in symbols.OrderBy(symbol => symbol.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsRoslynCompletionSymbol(symbol, staticOnly, instanceOnly))
                    continue;

                string name = GetRoslynCompletionName(symbol);
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!string.IsNullOrEmpty(normalizedPrefix) &&
                    !name.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(name))
                    continue;

                result.Add(new DefaultCompletionData(name,
                    GetRoslynCompletionDescription(symbol),
                    GetRoslynCompletionImageIndex(symbol)));
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static bool IsRoslynCompletionSymbol(ISymbol symbol, bool staticOnly, bool instanceOnly)
        {
            if (symbol == null || symbol.IsImplicitlyDeclared || string.IsNullOrEmpty(symbol.Name))
                return false;

            if (symbol is IMethodSymbol method)
            {
                if (method.MethodKind != MethodKind.Ordinary &&
                    method.MethodKind != MethodKind.ReducedExtension)
                {
                    return false;
                }
            }

            if (staticOnly && symbol.Kind != SymbolKind.NamedType && !symbol.IsStatic)
                return false;

            if (instanceOnly && symbol.IsStatic && symbol.Kind != SymbolKind.NamedType)
                return false;

            return symbol.Kind == SymbolKind.Method ||
                   symbol.Kind == SymbolKind.Property ||
                   symbol.Kind == SymbolKind.Field ||
                   symbol.Kind == SymbolKind.Event ||
                   symbol.Kind == SymbolKind.NamedType ||
                   symbol.Kind == SymbolKind.Namespace ||
                   symbol.Kind == SymbolKind.Local ||
                   symbol.Kind == SymbolKind.Parameter;
        }

        private static string GetRoslynCompletionName(ISymbol symbol)
        {
            if (symbol == null)
                return string.Empty;

            if (symbol is INamedTypeSymbol namedType && !string.IsNullOrEmpty(namedType.Name))
                return namedType.Name;

            return symbol.Name ?? string.Empty;
        }

        private static string GetRoslynCompletionDescription(ISymbol symbol)
        {
            try
            {
                return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            catch
            {
                return symbol?.Name ?? string.Empty;
            }
        }

        private static int GetRoslynCompletionImageIndex(ISymbol symbol)
        {
            switch (symbol?.Kind)
            {
                case SymbolKind.Method:
                    return 1;
                case SymbolKind.Property:
                    return 2;
                case SymbolKind.Field:
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                    return 3;
                case SymbolKind.Event:
                    return 6;
                case SymbolKind.NamedType:
                    return symbol is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Enum ? 4 : 0;
                case SymbolKind.Namespace:
                    return 5;
                default:
                    return 0;
            }
        }

        private static void AddWorkspaceClassCompletionItems(ArrayList result, HashSet<string> seen, WorkspaceCompletionClass completionClass)
        {
            foreach (var nestedType in completionClass.NestedTypes)
            {
                AddWorkspaceCompletionItem(result, seen, nestedType);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }

            foreach (var member in completionClass.StaticMembers)
            {
                AddWorkspaceCompletionItem(result, seen, member);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddWorkspaceNamespaceCompletionItems(
            ArrayList result,
            HashSet<string> seen,
            string namespaceName,
            List<WorkspaceCompletionClass> completionClasses)
        {
            string namespacePrefix = namespaceName.Length == 0 ? string.Empty : namespaceName + ".";

            foreach (var completionClass in completionClasses)
            {
                if (completionClass.IsNested)
                    continue;

                if (string.Equals(completionClass.NamespaceName, namespaceName, StringComparison.Ordinal))
                {
                    AddWorkspaceCompletionItem(result, seen, completionClass.ToCompletionItem());
                }
                else if (completionClass.NamespaceName.StartsWith(namespacePrefix, StringComparison.Ordinal))
                {
                    string remainder = completionClass.NamespaceName.Substring(namespacePrefix.Length);
                    int separatorIndex = remainder.IndexOf('.');
                    string childNamespace = separatorIndex >= 0 ? remainder.Substring(0, separatorIndex) : remainder;
                    if (childNamespace.Length > 0)
                        AddWorkspaceCompletionItem(result, seen, new WorkspaceCompletionItem(childNamespace, "namespace " + childNamespace, 5));
                }

                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddWorkspaceCompletionItem(ArrayList result, HashSet<string> seen, WorkspaceCompletionItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Name))
                return;

            if (seen.Add(item.Name))
                result.Add(new DefaultCompletionData(item.Name, item.Description, item.ImageIndex));
        }

        private static bool MatchesWorkspaceClass(WorkspaceCompletionClass completionClass, string expression)
        {
            return string.Equals(completionClass.FullName, expression, StringComparison.Ordinal) ||
                   string.Equals(completionClass.Name, expression, StringComparison.Ordinal);
        }

        private static string NormalizeCompletionExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return string.Empty;

            expression = expression.Trim();
            while (expression.EndsWith(".", StringComparison.Ordinal))
                expression = expression.Substring(0, expression.Length - 1);

            const string globalPrefix = "global::";
            if (expression.StartsWith(globalPrefix, StringComparison.Ordinal))
                expression = expression.Substring(globalPrefix.Length);

            return expression;
        }

        /// <summary>
        /// Finds the first declaration of <paramref name="name"/> across all parsed compilation units.
        /// Returns (filePath, lineNumber) or (null, 0) if not found.
        /// </summary>
        internal (string FilePath, int Line) FindDefinition(string name, int offset)
        {
            var hit = FindDefinitionInActiveFile(name, offset, out string qualifier);
            if (hit.FilePath != null)
                return hit;

            hit = FindDefinitionInWorkspaceMembers(name, qualifier);
            return hit.FilePath != null ? hit : FindDefinition(name);
        }

        internal (string FilePath, int Line) FindDefinition(string name)
        {
            if (string.IsNullOrEmpty(name))
                return (null, 0);

            lock (_completionDataLock)
            {
                // Top-level local functions and variables in the active file take priority.
                foreach (var item in _topLevelLocalFunctions)
                {
                    if (string.Equals(item.Name, name, StringComparison.Ordinal)
                        && item.FilePath != null && item.Line > 0)
                        return (item.FilePath, item.Line);
                }

                var hit = FindDefinitionInUnit(parseInformation.MostRecentCompilationUnit, name);
                if (hit.FilePath != null) return hit;
                foreach (var unit in _workspaceCompilationUnits.Values)
                {
                    hit = FindDefinitionInUnit(unit, name);
                    if (hit.FilePath != null) return hit;
                }
            }
            return (null, 0);
        }

        private (string FilePath, int Line) FindDefinitionInActiveFile(string name, int offset, out string qualifier)
        {
            qualifier = string.Empty;
            if (IsVisualBasic || string.IsNullOrEmpty(name))
                return (null, 0);

            var editor = SelectedEditor.GetSelectedEditor();
            string code = editor?.Text;
            if (string.IsNullOrWhiteSpace(code) || offset < 0 || offset >= code.Length)
                return (null, 0);

            try
            {
                string filePath = GetActiveEditorFilePath();
                var tree = CSharpSyntaxTree.ParseText(code, path: filePath ?? string.Empty);
                var root = tree.GetCompilationUnitRoot();
                var token = root.FindToken(offset);
                if (!token.IsKind(SyntaxKind.IdentifierToken) || !string.Equals(token.ValueText, name, StringComparison.Ordinal))
                    return (null, 0);

                qualifier = GetMemberAccessQualifier(token);

                var compilation = CSharpCompilation.Create("__CiareGoToDefinition", new[] { tree });
                var semanticModel = compilation.GetSemanticModel(tree, true);

                ISymbol symbol = GetDeclaredSymbolForIdentifier(semanticModel, token);
                var hit = GetDefinitionLocation(symbol, filePath);
                if (hit.FilePath != null)
                    return hit;

                SyntaxNode node = token.Parent;
                if (node is IdentifierNameSyntax || node is GenericNameSyntax)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(node);
                    symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                    hit = GetDefinitionLocation(symbol, filePath);
                    if (hit.FilePath != null)
                        return hit;
                }
            }
            catch
            {
                // Fall back to the existing workspace-wide DOM lookup below.
            }

            return (null, 0);
        }

        private static string GetMemberAccessQualifier(SyntaxToken token)
        {
            if (token.Parent is SimpleNameSyntax simpleName &&
                simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName)
                return NormalizeCompletionExpression(memberAccess.Expression.ToString());

            return string.Empty;
        }

        private (string FilePath, int Line) FindDefinitionInWorkspaceMembers(string name, string qualifier)
        {
            if (string.IsNullOrEmpty(name))
                return (null, 0);

            lock (_completionDataLock)
            {
                return FindDefinitionInWorkspaceMembersCore(name, qualifier);
            }
        }

        private (string FilePath, int Line) FindDefinitionInWorkspaceMembersCore(string name, string qualifier)
        {
            string normalizedQualifier = NormalizeCompletionExpression(qualifier);
            if (!string.IsNullOrEmpty(normalizedQualifier))
            {
                foreach (var completionClass in _workspaceCompletionClasses)
                {
                    if (!MatchesWorkspaceClass(completionClass, normalizedQualifier))
                        continue;

                    var hit = FindDefinitionInWorkspaceItems(completionClass.StaticMembers, name);
                    if (hit.FilePath != null)
                        return hit;
                }
            }

            foreach (var completionClass in _workspaceCompletionClasses)
            {
                var hit = FindDefinitionInWorkspaceItems(completionClass.StaticMembers, name);
                if (hit.FilePath != null)
                    return hit;
            }

            return (null, 0);
        }

        private static (string FilePath, int Line) FindDefinitionInWorkspaceItems(IEnumerable<WorkspaceCompletionItem> items, string name)
        {
            foreach (var item in items)
            {
                if (item != null &&
                    string.Equals(item.Name, name, StringComparison.Ordinal) &&
                    item.FilePath != null && item.Line > 0)
                    return (item.FilePath, item.Line);
            }

            return (null, 0);
        }

        private static ISymbol GetDeclaredSymbolForIdentifier(SemanticModel semanticModel, SyntaxToken token)
        {
            SyntaxNode node = token.Parent;
            while (node != null)
            {
                switch (node)
                {
                    case VariableDeclaratorSyntax variable when variable.Identifier == token:
                    case SingleVariableDesignationSyntax designation when designation.Identifier == token:
                    case ParameterSyntax parameter when parameter.Identifier == token:
                    case LocalFunctionStatementSyntax localFunction when localFunction.Identifier == token:
                    case BaseTypeDeclarationSyntax typeDeclaration when typeDeclaration.Identifier == token:
                    case MethodDeclarationSyntax method when method.Identifier == token:
                    case PropertyDeclarationSyntax property when property.Identifier == token:
                    case EventDeclarationSyntax eventDeclaration when eventDeclaration.Identifier == token:
                    case EnumMemberDeclarationSyntax enumMember when enumMember.Identifier == token:
                    case ForEachStatementSyntax forEach when forEach.Identifier == token:
                    case CatchDeclarationSyntax catchDeclaration when catchDeclaration.Identifier == token:
                        return semanticModel.GetDeclaredSymbol(node);
                }

                node = node.Parent;
            }

            return null;
        }

        private static (string FilePath, int Line) GetDefinitionLocation(ISymbol symbol, string fallbackFilePath)
        {
            if (symbol == null)
                return (null, 0);

            ISymbol definition = symbol.OriginalDefinition ?? symbol;
            foreach (var location in definition.Locations)
            {
                var hit = GetDefinitionLocation(location, fallbackFilePath);
                if (hit.FilePath != null)
                    return hit;
            }

            foreach (var syntaxRef in definition.DeclaringSyntaxReferences)
            {
                var hit = GetDefinitionLocation(syntaxRef.GetSyntax().GetLocation(), fallbackFilePath);
                if (hit.FilePath != null)
                    return hit;
            }

            return (null, 0);
        }

        private static (string FilePath, int Line) GetDefinitionLocation(Location location, string fallbackFilePath)
        {
            if (location == null || !location.IsInSource)
                return (null, 0);

            var lineSpan = location.GetLineSpan();
            string filePath = string.IsNullOrEmpty(lineSpan.Path) ? fallbackFilePath : lineSpan.Path;
            if (string.IsNullOrEmpty(filePath))
                return (null, 0);

            return (filePath, lineSpan.StartLinePosition.Line + 1);
        }

        private static (string FilePath, int Line) FindDefinitionInUnit(Dom.ICompilationUnit unit, string name)
        {
            if (unit?.Classes == null) return (null, 0);
            foreach (var @class in unit.Classes)
            {
                var hit = FindDefinitionInClass(@class, name);
                if (hit.FilePath != null) return hit;
            }
            return (null, 0);
        }

        private static (string FilePath, int Line) FindDefinitionInClass(Dom.IClass @class, string name)
        {
            if (@class == null) return (null, 0);
            if (string.Equals(@class.Name, name, StringComparison.Ordinal)
                && @class.CompilationUnit?.FileName != null && @class.Region.BeginLine > 0)
                return (@class.CompilationUnit.FileName, @class.Region.BeginLine);

            foreach (var m in @class.Methods)
            {
                if (m != null && !m.IsConstructor
                    && string.Equals(m.Name, name, StringComparison.Ordinal)
                    && m.DeclaringType?.CompilationUnit?.FileName != null && m.Region.BeginLine > 0)
                    return (m.DeclaringType.CompilationUnit.FileName, m.Region.BeginLine);
            }
            foreach (var p in @class.Properties)
            {
                if (p != null && string.Equals(p.Name, name, StringComparison.Ordinal)
                    && p.DeclaringType?.CompilationUnit?.FileName != null && p.Region.BeginLine > 0)
                    return (p.DeclaringType.CompilationUnit.FileName, p.Region.BeginLine);
            }
            foreach (var f in @class.Fields)
            {
                if (f != null && string.Equals(f.Name, name, StringComparison.Ordinal)
                    && f.DeclaringType?.CompilationUnit?.FileName != null && f.Region.BeginLine > 0)
                    return (f.DeclaringType.CompilationUnit.FileName, f.Region.BeginLine);
            }
            foreach (var inner in @class.InnerClasses)
            {
                var hit = FindDefinitionInClass(inner, name);
                if (hit.FilePath != null) return hit;
            }
            return (null, 0);
        }

        private static void AddCompilationUnitMethods(ArrayList result, HashSet<string> seen, Dom.ICompilationUnit unit, string prefix)
        {
            if (unit == null || unit.Classes == null)
                return;

            foreach (var @class in unit.Classes)
            {
                AddClassMethods(result, seen, @class, prefix);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddClassMethods(ArrayList result, HashSet<string> seen, Dom.IClass @class, string prefix)
        {
            if (@class == null)
                return;

            foreach (var method in @class.Methods)
            {
                if (method == null || method.IsConstructor)
                    continue;
                if (!method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = method.FullyQualifiedName + "#" + method.Parameters.Count + "#" + method.Region.BeginLine + "#" + method.Region.BeginColumn;
                if (seen.Add(key))
                    result.Add(method);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }

            foreach (var innerClass in @class.InnerClasses)
            {
                AddClassMethods(result, seen, innerClass, prefix);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static string NormalizeCompletionPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch { return path; }
        }

        private static readonly Regex FileScopedNamespaceRegex = new Regex(
            @"^[ \t]*namespace[ \t]+([\w.]+)[ \t]*;",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Converts file-scoped namespace declarations (namespace Foo.Bar;) to
        /// block-scoped (namespace Foo.Bar { ... }) so NRefactory can parse them.
        /// </summary>
        private static string ConvertFileScopedNamespace(string code)
        {
            var match = FileScopedNamespaceRegex.Match(code);
            if (!match.Success)
                return code;
            string nsName = match.Groups[1].Value;
            string before = code.Substring(0, match.Index);
            string after = code.Substring(match.Index + match.Length);
            return before + "namespace " + nsName + "\n{" + after + "\n}";
        }

        internal string PrepareCodeForNRefactoryCompletion(string code, out int prefixLineOffset,
            out int wrapLineOffset, out int bodyStartLine)
        {
            return PrepareCodeForNRefactoryCompletion(code, GetActiveEditorFilePath(),
                out prefixLineOffset, out wrapLineOffset, out bodyStartLine);
        }

        private string PrepareCodeForNRefactoryCompletion(string code, string filePath,
            out int prefixLineOffset, out int wrapLineOffset, out int bodyStartLine)
        {
            prefixLineOffset = 0;
            wrapLineOffset = 0;
            bodyStartLine = 1;

            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return code;

            string globalUsings = GetProjectGlobalUsingsForCompletion(filePath);
            string codeWithGlobalUsings = code;
            if (!string.IsNullOrEmpty(globalUsings))
            {
                prefixLineOffset = CountLineBreaks(globalUsings);
                codeWithGlobalUsings = globalUsings + code;
            }

            return WrapTopLevelStatementsForNRefactory(ConvertFileScopedNamespace(codeWithGlobalUsings),
                out wrapLineOffset, out bodyStartLine);
        }

        private string GetProjectGlobalUsingsForCompletion(string sourceFilePath)
        {
            return ReadGlobalUsingsAsRegularDirectives(GetCompletionProjectPath(sourceFilePath));
        }

        private string GetCompletionProjectPath(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                return string.Empty;

            if (Directory.Exists(_fileExplorerRootPath))
            {
                return IsPathInsideFolder(sourceFilePath, _fileExplorerRootPath)
                    ? FindProjectFileForPath(sourceFilePath, _fileExplorerRootPath)
                    : string.Empty;
            }

            return string.Empty;
        }

        private static string ReadGlobalUsingsAsRegularDirectives(string projectPath)
        {
            var directives = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string globalUsingsPath = FindProjectGlobalUsingsFile(projectPath);
            if (!string.IsNullOrEmpty(globalUsingsPath))
                AddGlobalUsingDirectivesFromFile(globalUsingsPath, directives, seen);

            try
            {
                string projectDirectory = string.IsNullOrWhiteSpace(projectPath)
                    ? string.Empty
                    : Path.GetDirectoryName(projectPath);
                if (!string.IsNullOrEmpty(projectDirectory) && Directory.Exists(projectDirectory))
                {
                    foreach (string filePath in GetWorkspaceCsFiles(projectDirectory))
                        AddGlobalUsingDirectivesFromFile(filePath, directives, seen);
                }

                foreach (string namespaceName in ProjectNuGetManager.GetImplicitUsingNamespaces(projectPath))
                    AddRegularUsingDirective("using " + namespaceName + ";", directives, seen);

                return directives.Count == 0
                    ? string.Empty
                    : string.Join(Environment.NewLine, directives) + Environment.NewLine;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AddGlobalUsingDirectivesFromFile(string filePath, List<string> directives,
            HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                foreach (string line in File.ReadAllLines(filePath))
                    AddRegularUsingDirective(ConvertGlobalUsingLine(line), directives, seen);
            }
            catch
            {
            }
        }

        private static void AddRegularUsingDirective(string directive, List<string> directives,
            HashSet<string> seen)
        {
            if (!string.IsNullOrWhiteSpace(directive) && seen.Add(directive))
                directives.Add(directive);
        }

        private static string FindProjectGlobalUsingsFile(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return string.Empty;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
                return string.Empty;

            string objDirectory = Path.Combine(projectDirectory, "obj");
            if (!Directory.Exists(objDirectory))
                return string.Empty;

            try
            {
                string targetFramework = ProjectNuGetManager.GetProjectTargetFramework(projectPath);
                var files = Directory.EnumerateFiles(objDirectory, "*GlobalUsings.g.cs",
                        SearchOption.AllDirectories)
                    .Where(File.Exists)
                    .OrderByDescending(path => IsPreferredGlobalUsingsPath(path, targetFramework))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return files.FirstOrDefault(path => IsPreferredGlobalUsingsPath(path, targetFramework)) ??
                    files.FirstOrDefault() ??
                    string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsPreferredGlobalUsingsPath(string filePath, string targetFramework)
        {
            string framework = string.IsNullOrWhiteSpace(targetFramework)
                ? GlobalVariables.Framework
                : targetFramework;
            if (string.IsNullOrWhiteSpace(framework))
                return false;

            return filePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith(framework, StringComparison.OrdinalIgnoreCase));
        }

        private static string ConvertGlobalUsingLine(string line)
        {
            string trimmed = (line ?? string.Empty).Trim();
            const string prefix = "global using ";
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                return string.Empty;

            string body = trimmed.Substring(prefix.Length);
            if (body.StartsWith("static ", StringComparison.Ordinal))
                return string.Empty;

            string directive = "using " + body.Replace("global::", string.Empty);
            return directive.EndsWith(";", StringComparison.Ordinal) ? directive : directive + ";";
        }

        private static int CountLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int count = 0;
            foreach (char ch in text)
                if (ch == '\n')
                    count++;
            return count;
        }

        /// <summary>
        /// When code uses top-level statements (C# 9+), wraps the body in a synthetic
        /// class and method so the NRefactory resolver can find a method scope and
        /// provide proper code-completion. Using-directives remain at the top so the
        /// resolver still sees them in their original positions.
        /// </summary>
        /// <param name="code">Source code to inspect and possibly wrap.</param>
        /// <param name="lineOffset">
        /// Number of wrapper lines inserted between the using-directives and the body.
        /// 0 when no wrapping was needed.
        /// </param>
        /// <param name="bodyStartLine">
        /// First 1-based line in the <paramref name="code"/> that belongs to the body
        /// (i.e. the first line that will be shifted by <paramref name="lineOffset"/>).
        /// </param>
        /// <returns>Wrapped code, or the original code unchanged when no wrapping is needed.</returns>
        internal static string WrapTopLevelStatementsForNRefactory(string code, out int lineOffset, out int bodyStartLine)
        {
            lineOffset = 0;
            bodyStartLine = 1;

            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return code;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                if (!root.Members.OfType<GlobalStatementSyntax>().Any())
                    return code;

                // Keep using-directives outside the wrapper so they stay at the same lines.
                int splitPos = 0;
                if (root.Usings.Count > 0)
                    splitPos = root.Usings.Last().FullSpan.End;

                // Compute the 1-based line number of the first body line in the original code.
                int linesBeforeSplit = 0;
                for (int i = 0; i < splitPos && i < code.Length; i++)
                    if (code[i] == '\n') linesBeforeSplit++;
                bodyStartLine = linesBeforeSplit + 1;

                string head = code.Substring(0, splitPos);
                string body = code.Substring(splitPos);

                lineOffset = 2; // "class __TopLevel__ {\n" + "void __Main__() {\n"
                return head + "class __TopLevel__ {\nvoid __Main__() {" + body + "\n}\n}";
            }
            catch
            {
                return code;
            }
        }

        private static void AddRoslynCompletionClasses(List<WorkspaceCompletionClass> result, string code, string filePath)
        {
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                foreach (var member in root.Members)
                    AddRoslynCompletionMember(result, member, string.Empty, string.Empty, null, filePath);
            }
            catch
            {
                // The NRefactory parser still handles the primary completion path.
            }
        }

        private static void CollectTopLevelLocalFunctions(List<WorkspaceCompletionItem> result, string code, string filePath)
        {
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                foreach (var globalStmt in root.Members.OfType<GlobalStatementSyntax>())
                {
                    if (globalStmt.Statement is LocalFunctionStatementSyntax localFunc)
                    {
                        string name = localFunc.Identifier.ValueText;
                        if (string.IsNullOrEmpty(name))
                            continue;
                        string parameters = string.Join(", ", localFunc.ParameterList.Parameters.Select(p => p.ToString()));
                        string returnType = localFunc.ReturnType.ToString();
                        string description = returnType + " " + name + "(" + parameters + ")";
                        int line = localFunc.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        result.Add(new WorkspaceCompletionItem(name, description, 1, filePath, line));
                    }
                    else if (globalStmt.Statement is LocalDeclarationStatementSyntax localDecl)
                    {
                        int line = localDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        string typeStr = localDecl.Declaration.Type.ToString();
                        foreach (var declarator in localDecl.Declaration.Variables)
                        {
                            string name = declarator.Identifier.ValueText;
                            if (string.IsNullOrEmpty(name))
                                continue;
                            result.Add(new WorkspaceCompletionItem(name, typeStr + " " + name, 0, filePath, line));
                        }
                    }
                }
            }
            catch { }
        }

        private static void AddRoslynCompletionMember(
            List<WorkspaceCompletionClass> result,
            MemberDeclarationSyntax member,
            string namespaceName,
            string containingTypeName,
            WorkspaceCompletionClass parentClass,
            string filePath)
        {
            if (member is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                string childNamespace = CombineName(namespaceName, namespaceDeclaration.Name.ToString());
                foreach (var childMember in namespaceDeclaration.Members)
                    AddRoslynCompletionMember(result, childMember, childNamespace, containingTypeName, parentClass, filePath);
                return;
            }

            if (member is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                string childNamespace = CombineName(namespaceName, fileScopedNamespace.Name.ToString());
                foreach (var childMember in fileScopedNamespace.Members)
                    AddRoslynCompletionMember(result, childMember, childNamespace, containingTypeName, parentClass, filePath);
                return;
            }

            if (member is BaseTypeDeclarationSyntax typeDeclaration)
            {
                bool isNested = parentClass != null;
                if (!IsWorkspaceVisibleType(typeDeclaration.Modifiers, isNested))
                    return;

                string typeName = typeDeclaration.Identifier.ValueText;
                if (string.IsNullOrEmpty(typeName))
                    return;

                string typePrefix = string.IsNullOrEmpty(containingTypeName) ? namespaceName : containingTypeName;
                var completionClass = new WorkspaceCompletionClass(
                    typeName,
                    CombineName(typePrefix, typeName),
                    namespaceName,
                    GetTypeKeyword(typeDeclaration),
                    GetTypeImageIndex(typeDeclaration),
                    isNested,
                    filePath);

                result.Add(completionClass);
                if (parentClass != null)
                    parentClass.NestedTypes.Add(completionClass.ToCompletionItem());

                if (typeDeclaration is TypeDeclarationSyntax typeWithMembers)
                {
                    foreach (var childMember in typeWithMembers.Members)
                    {
                        if (childMember is BaseTypeDeclarationSyntax || childMember is NamespaceDeclarationSyntax || childMember is FileScopedNamespaceDeclarationSyntax)
                            AddRoslynCompletionMember(result, childMember, namespaceName, completionClass.FullName, completionClass, filePath);
                        else
                            AddRoslynStaticMember(completionClass, childMember);
                    }
                }
                else if (typeDeclaration is EnumDeclarationSyntax enumDeclaration)
                {
                    foreach (var enumMember in enumDeclaration.Members)
                    {
                        string enumMemberName = enumMember.Identifier.ValueText;
                        if (enumMemberName.Length > 0)
                            completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                                enumMemberName,
                                completionClass.FullName + "." + enumMemberName,
                                3,
                                completionClass.FilePath,
                                GetSyntaxLine(enumMember)));
                    }
                }
            }
        }

        private static void AddRoslynStaticMember(WorkspaceCompletionClass completionClass, MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax fieldDeclaration)
            {
                if (!IsWorkspaceVisibleMember(fieldDeclaration.Modifiers) || !IsStaticOrConst(fieldDeclaration.Modifiers))
                    return;

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    string name = variable.Identifier.ValueText;
                    if (name.Length == 0)
                        continue;

                    string description = JoinDeclarationParts(GetModifierText(fieldDeclaration.Modifiers), fieldDeclaration.Declaration.Type.ToString(), name);
                    completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                        name,
                        description,
                        3,
                        completionClass.FilePath,
                        GetSyntaxLine(variable)));
                }
                return;
            }

            if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                if (!IsWorkspaceVisibleMember(propertyDeclaration.Modifiers) || !HasModifier(propertyDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = propertyDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string description = JoinDeclarationParts(GetModifierText(propertyDeclaration.Modifiers), propertyDeclaration.Type.ToString(), name);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    2,
                    completionClass.FilePath,
                    GetSyntaxLine(propertyDeclaration)));
                return;
            }

            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                if (!IsWorkspaceVisibleMember(methodDeclaration.Modifiers) || !HasModifier(methodDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = methodDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string parameters = string.Join(", ", methodDeclaration.ParameterList.Parameters.Select(parameter => parameter.ToString()));
                string signature = name + "(" + parameters + ")";
                string description = JoinDeclarationParts(GetModifierText(methodDeclaration.Modifiers), methodDeclaration.ReturnType.ToString(), signature);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    1,
                    completionClass.FilePath,
                    GetSyntaxLine(methodDeclaration)));
                return;
            }

            if (member is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                if (!IsWorkspaceVisibleMember(eventFieldDeclaration.Modifiers) || !HasModifier(eventFieldDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                foreach (var variable in eventFieldDeclaration.Declaration.Variables)
                {
                    string name = variable.Identifier.ValueText;
                    if (name.Length == 0)
                        continue;

                    string description = JoinDeclarationParts(GetModifierText(eventFieldDeclaration.Modifiers), "event", eventFieldDeclaration.Declaration.Type.ToString(), name);
                    completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                        name,
                        description,
                        6,
                        completionClass.FilePath,
                        GetSyntaxLine(variable)));
                }
                return;
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                if (!IsWorkspaceVisibleMember(eventDeclaration.Modifiers) || !HasModifier(eventDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = eventDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string description = JoinDeclarationParts(GetModifierText(eventDeclaration.Modifiers), "event", eventDeclaration.Type.ToString(), name);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    6,
                    completionClass.FilePath,
                    GetSyntaxLine(eventDeclaration)));
            }
        }

        private static int GetSyntaxLine(SyntaxNode node)
        {
            return node?.GetLocation().GetLineSpan().StartLinePosition.Line + 1 ?? 0;
        }

        private static bool IsWorkspaceVisibleType(SyntaxTokenList modifiers, bool isNested)
        {
            if (HasModifier(modifiers, SyntaxKind.PrivateKeyword) || HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
                return false;

            return !isNested || HasModifier(modifiers, SyntaxKind.PublicKeyword) || HasModifier(modifiers, SyntaxKind.InternalKeyword);
        }

        private static bool IsWorkspaceVisibleMember(SyntaxTokenList modifiers)
        {
            return HasModifier(modifiers, SyntaxKind.PublicKeyword) || HasModifier(modifiers, SyntaxKind.InternalKeyword);
        }

        private static bool IsStaticOrConst(SyntaxTokenList modifiers)
        {
            return HasModifier(modifiers, SyntaxKind.StaticKeyword) || HasModifier(modifiers, SyntaxKind.ConstKeyword);
        }

        private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier.IsKind(kind))
                    return true;
            }
            return false;
        }

        private static string CombineName(string prefix, string name)
        {
            if (string.IsNullOrEmpty(prefix))
                return name ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                return prefix;
            return prefix + "." + name;
        }

        private static string GetModifierText(SyntaxTokenList modifiers)
        {
            return string.Join(" ", modifiers.Select(modifier => modifier.ValueText));
        }

        private static string JoinDeclarationParts(params string[] parts)
        {
            return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string GetTypeKeyword(BaseTypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration is InterfaceDeclarationSyntax)
                return "interface";
            if (typeDeclaration is StructDeclarationSyntax)
                return "struct";
            if (typeDeclaration is EnumDeclarationSyntax)
                return "enum";
            if (typeDeclaration is RecordDeclarationSyntax)
                return "record";
            return "class";
        }

        private static int GetTypeImageIndex(BaseTypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration is EnumDeclarationSyntax ? 4 : 0;
        }

        private sealed class WorkspaceCompletionClass
        {
            public WorkspaceCompletionClass(string name, string fullName, string namespaceName, string kindKeyword, int imageIndex, bool isNested, string filePath)
            {
                Name = name;
                FullName = fullName;
                NamespaceName = namespaceName ?? string.Empty;
                KindKeyword = kindKeyword;
                ImageIndex = imageIndex;
                IsNested = isNested;
                FilePath = filePath;
            }

            public string Name { get; }
            public string FullName { get; }
            public string NamespaceName { get; }
            public string KindKeyword { get; }
            public int ImageIndex { get; }
            public bool IsNested { get; }
            public string FilePath { get; }
            public List<WorkspaceCompletionItem> StaticMembers { get; } = new List<WorkspaceCompletionItem>();
            public List<WorkspaceCompletionItem> NestedTypes { get; } = new List<WorkspaceCompletionItem>();

            public WorkspaceCompletionItem ToCompletionItem()
            {
                return new WorkspaceCompletionItem(Name, KindKeyword + " " + FullName, ImageIndex);
            }
        }

        private sealed class RoslynCompletionContext
        {
            public RoslynCompletionContext(SyntaxTree activeTree, CompilationUnitSyntax root,
                CSharpCompilation compilation, SemanticModel semanticModel)
            {
                ActiveTree = activeTree;
                Root = root;
                Compilation = compilation;
                SemanticModel = semanticModel;
            }

            public SyntaxTree ActiveTree { get; }
            public CompilationUnitSyntax Root { get; }
            public CSharpCompilation Compilation { get; }
            public SemanticModel SemanticModel { get; }
        }

        private sealed class WorkspaceCompletionItem
        {
            public WorkspaceCompletionItem(string name, string description, int imageIndex, string filePath = null, int line = 0)
            {
                Name = name;
                Description = description;
                ImageIndex = imageIndex;
                FilePath = filePath;
                Line = line;
            }

            public string Name { get; }
            public string Description { get; }
            public int ImageIndex { get; }
            public string FilePath { get; }
            public int Line { get; }
        }

        private const string CurrentFileUsageDisplayName = "<current file>";

        private struct OpenTabInfo
        {
            public readonly string FilePath;
            public readonly string Text;
            public readonly bool IsActive;
            public OpenTabInfo(string filePath, string text, bool isActive)
            {
                FilePath = filePath;
                Text = text;
                IsActive = isActive;
            }
        }

        private sealed class UsageDocument
        {
            private readonly object _linesLock = new object();
            private string[] _lines;

            public UsageDocument(string filePath, string text, SyntaxTree syntaxTree, CompilationUnitSyntax root, bool isActive)
            {
                FilePath = filePath ?? string.Empty;
                Text = text ?? string.Empty;
                SyntaxTree = syntaxTree;
                Root = root;
                IsActive = isActive;
            }

            public string FilePath { get; }
            public string Text { get; }
            public SyntaxTree SyntaxTree { get; }
            public CompilationUnitSyntax Root { get; }
            public bool IsActive { get; }
            public string DisplayPath => string.IsNullOrEmpty(FilePath) ? CurrentFileUsageDisplayName : FilePath;

            public string GetLineText(int lineNumber)
            {
                int index = lineNumber - 1;
                var lines = GetLines();
                return index >= 0 && index < lines.Length ? lines[index] : string.Empty;
            }

            private string[] GetLines()
            {
                if (_lines != null)
                    return _lines;

                lock (_linesLock)
                {
                    if (_lines == null)
                    {
                        _lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    }

                    return _lines;
                }
            }
        }

        private sealed class UsageDocumentCacheEntry
        {
            public UsageDocumentCacheEntry(UsageDocument document, long lastWriteUtcTicks, long length)
            {
                Document = document;
                LastWriteUtcTicks = lastWriteUtcTicks;
                Length = length;
                LastAccessUtc = DateTime.UtcNow;
            }

            public UsageDocument Document { get; }
            public long LastWriteUtcTicks { get; }
            public long Length { get; }
            public DateTime LastAccessUtc { get; private set; }

            public void Touch()
            {
                LastAccessUtc = DateTime.UtcNow;
            }
        }

        private sealed class UsageLocation
        {
            public UsageLocation(string filePath, int line, int column, string text)
            {
                FilePath = filePath;
                Line = line;
                Column = column;
                Text = text;
            }

            public string FilePath { get; }
            public int Line { get; }
            public int Column { get; }
            public string Text { get; }
        }

        private async void FindUsagesAtCaret()
        {
            if (IsVisualBasic)
            {
                ShowFindUsagesMessage("Find Usages is available for C# files.");
                return;
            }

            if (!TryGetIdentifierAtCaret(out string identifier, out int identifierOffset))
            {
                ShowFindUsagesMessage("Put the caret on an identifier to find usages.");
                return;
            }

            var previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                // Capture all UI-thread state before going async.
                var openTabs = CollectOpenTabInfo(identifier);
                var workspaceFolders = GetUsageWorkspaceFolders(GetActiveEditorFilePath()).ToList();
                List<string> compilationUnitKeys;
                lock (_completionDataLock)
                    compilationUnitKeys = _workspaceCompilationUnits.Keys.ToList();

                List<UsageLocation> usages = await Task.Run(() =>
                {
                    var documents = BuildUsageDocuments(identifier, openTabs, workspaceFolders, compilationUnitKeys);
                    if (documents.Count == 0)
                        return null;

                    var semanticUsages = FindSemanticUsages(identifier, identifierOffset, documents, out bool targetResolved);
                    return targetResolved ? semanticUsages : FindSyntaxUsages(identifier, documents);
                });

                if (usages == null)
                    ShowFindUsagesMessage("No C# files found to search.");
                else
                    ShowFindUsagesResults(identifier, usages);
            }
            catch (Exception ex)
            {
                ShowFindUsagesMessage("Find Usages failed: " + ex.Message);
            }
            finally
            {
                Cursor.Current = previousCursor;
            }
        }

        private bool TryGetIdentifierAtCaret(out string identifier, out int offset)
        {
            identifier = string.Empty;
            offset = -1;

            var editor = SelectedEditor.GetSelectedEditor();
            var textArea = editor?.ActiveTextAreaControl?.TextArea;
            var document = textArea?.Document;
            if (document == null || document.TextLength == 0)
                return false;

            if (textArea.SelectionManager.HasSomethingSelected &&
                textArea.SelectionManager.SelectionCollection.Count == 1)
            {
                string selectedText = textArea.SelectionManager.SelectedText?.Trim();
                if (IsValidIdentifierText(selectedText))
                {
                    identifier = NormalizeIdentifierText(selectedText);
                    offset = textArea.SelectionManager.SelectionCollection[0].Offset;
                    return true;
                }
            }

            int caretOffset = Math.Max(0, Math.Min(textArea.Caret.Offset, document.TextLength));
            int lookupOffset = caretOffset == document.TextLength && caretOffset > 0 ? caretOffset - 1 : caretOffset;
            int wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(document, lookupOffset);
            int wordEnd = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(document, lookupOffset);

            if (wordEnd <= wordStart && caretOffset > 0)
            {
                lookupOffset = caretOffset - 1;
                wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(document, lookupOffset);
                wordEnd = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(document, lookupOffset);
            }

            if (wordEnd <= wordStart)
                return false;

            string word = document.GetText(wordStart, wordEnd - wordStart);
            if (!IsValidIdentifierText(word))
                return false;

            identifier = NormalizeIdentifierText(word);
            offset = wordStart;
            return true;
        }

        private List<OpenTabInfo> CollectOpenTabInfo(string identifier)
        {
            var tabs = new List<OpenTabInfo>();
            bool hasActive = false;
            for (int i = 0; i < EditorTabControl.TabPages.Count; i++)
            {
                var tabPage = EditorTabControl.TabPages[i];
                var editor = tabPage.Controls.Count > 0 ? tabPage.Controls[0] as TextEditorControl : null;
                if (editor == null)
                    continue;

                bool isActive = i == EditorTabControl.SelectedIndex;
                string filePath = tabPage.ToolTipText?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(filePath))
                {
                    if (isActive)
                    {
                        tabs.Add(new OpenTabInfo(string.Empty, editor.Text ?? string.Empty, true));
                        hasActive = true;
                    }
                    continue;
                }

                if (!IsCSharpFilePath(filePath))
                    continue;

                if (!isActive && !TextContainsIdentifier(editor.Text, identifier))
                    continue;

                tabs.Add(new OpenTabInfo(filePath, editor.Text ?? string.Empty, isActive));
                if (isActive)
                    hasActive = true;
            }

            if (!hasActive)
            {
                var editor = SelectedEditor.GetSelectedEditor();
                if (editor != null)
                    tabs.Add(new OpenTabInfo(GetActiveEditorFilePath(), editor.Text ?? string.Empty, true));
            }

            return tabs;
        }

        private static List<UsageDocument> BuildUsageDocuments(
            string identifier,
            List<OpenTabInfo> openTabs,
            List<string> workspaceFolders,
            List<string> compilationUnitFilePaths)
        {
            var documents = new List<UsageDocument>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parseOptions = CSharpParseOptions.Default;

            // Parse open-tab documents first (already in memory).
            foreach (var tab in openTabs)
                AddUsageDocument(documents, seen, tab.FilePath, tab.Text, tab.IsActive, parseOptions);

            // Gather all candidate file paths from workspace folders and compilation units.
            var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in workspaceFolders)
            {
                if (!Directory.Exists(folder))
                    continue;
                foreach (var f in GetWorkspaceCsFiles(folder))
                    allPaths.Add(f);
            }
            foreach (var f in compilationUnitFilePaths)
                allPaths.Add(f);

            // Snapshot already-seen paths so the parallel scan can filter without locking.
            var seenSnapshot = new HashSet<string>(seen, StringComparer.OrdinalIgnoreCase);
            var pathsToScan = allPaths
                .Where(f => !seenSnapshot.Contains(NormalizeCompletionPath(f)))
                .ToList();

            // Scan and parse files in parallel (disk I/O + Roslyn parse are the bottleneck).
            var newDocs = new ConcurrentDictionary<string, UsageDocument>(StringComparer.OrdinalIgnoreCase);
            Parallel.ForEach(pathsToScan, CreateUsageParallelOptions(), filePath =>
            {
                if (TryGetUsageDocumentFromFile(filePath, identifier, parseOptions, out UsageDocument document))
                    newDocs.TryAdd(NormalizeCompletionPath(filePath), document);
            });

            foreach (var kvp in newDocs)
            {
                if (seen.Add(kvp.Key))
                    documents.Add(kvp.Value);
            }

            return documents;
        }

        private static ParallelOptions CreateUsageParallelOptions()
        {
            return new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };
        }

        private static bool TryGetUsageDocumentFromFile(string filePath, string identifier,
            CSharpParseOptions parseOptions, out UsageDocument document)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrEmpty(identifier))
                return false;

            string normalizedPath = NormalizeCompletionPath(filePath);
            if (string.IsNullOrEmpty(normalizedPath))
                return false;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return false;
            }
            catch
            {
                return false;
            }

            if (TryGetCachedUsageDocument(normalizedPath, fileInfo, identifier, out document))
                return true;

            try
            {
                if (!TryReadUsageFileTextIfContainsIdentifier(filePath, fileInfo, identifier, out string text))
                    return false;

                var syntaxTree = CSharpSyntaxTree.ParseText(text, parseOptions, path: filePath);
                var root = syntaxTree.GetCompilationUnitRoot();
                document = new UsageDocument(filePath, text, syntaxTree, root, false);
                CacheUsageDocument(normalizedPath, document, fileInfo);
                return true;
            }
            catch
            {
                document = null;
                return false;
            }
        }

        private static bool TryReadUsageFileTextIfContainsIdentifier(string filePath, FileInfo fileInfo,
            string identifier, out string text)
        {
            text = null;
            try
            {
                if (fileInfo.Length <= UsageFastReadMaxFileBytes)
                {
                    text = File.ReadAllText(filePath);
                    if (TextContainsIdentifier(text, identifier))
                        return true;

                    text = null;
                    return false;
                }

                if (!FileContainsIdentifier(filePath, identifier))
                    return false;

                text = File.ReadAllText(filePath);
                return true;
            }
            catch
            {
                text = null;
                return false;
            }
        }

        private static bool TryGetCachedUsageDocument(string normalizedPath, FileInfo fileInfo,
            string identifier, out UsageDocument document)
        {
            document = null;
            UsageDocumentCacheEntry entry;
            lock (_usageDocumentCacheLock)
            {
                if (!_usageDocumentCache.TryGetValue(normalizedPath, out entry))
                    return false;

                if (entry.LastWriteUtcTicks != fileInfo.LastWriteTimeUtc.Ticks ||
                    entry.Length != fileInfo.Length)
                {
                    _usageDocumentCache.Remove(normalizedPath);
                    return false;
                }
            }

            if (!TextContainsIdentifier(entry.Document.Text, identifier))
                return false;

            lock (_usageDocumentCacheLock)
            {
                entry.Touch();
            }

            document = entry.Document;
            return true;
        }

        private static void CacheUsageDocument(string normalizedPath, UsageDocument document, FileInfo fileInfo)
        {
            if (document == null || fileInfo.Length > UsageDocumentCacheMaxFileBytes)
                return;

            lock (_usageDocumentCacheLock)
            {
                _usageDocumentCache[normalizedPath] = new UsageDocumentCacheEntry(
                    document,
                    fileInfo.LastWriteTimeUtc.Ticks,
                    fileInfo.Length);
                TrimUsageDocumentCache();
            }
        }

        private static void TrimUsageDocumentCache()
        {
            if (_usageDocumentCache.Count <= UsageDocumentCacheLimit)
                return;

            var keysToRemove = _usageDocumentCache
                .OrderBy(kvp => kvp.Value.LastAccessUtc)
                .Take(_usageDocumentCache.Count - UsageDocumentCacheLimit)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in keysToRemove)
                _usageDocumentCache.Remove(key);
        }

        private static void AddUsageDocument(List<UsageDocument> documents, HashSet<string> seen,
            string filePath, string text, bool isActive, CSharpParseOptions parseOptions)
        {
            string normalizedPath = NormalizeCompletionPath(filePath);
            if (!string.IsNullOrEmpty(normalizedPath) && !seen.Add(normalizedPath))
                return;

            string syntaxPath = string.IsNullOrEmpty(filePath) ? CurrentFileUsageDisplayName : filePath;
            var syntaxTree = CSharpSyntaxTree.ParseText(text ?? string.Empty, parseOptions, path: syntaxPath);
            var root = syntaxTree.GetCompilationUnitRoot();
            documents.Add(new UsageDocument(filePath, text, syntaxTree, root, isActive));
        }

        private static bool FileContainsIdentifier(string filePath, string identifier)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(identifier))
                return false;

            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    if (TextContainsIdentifier(line, identifier))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TextContainsIdentifier(string text, string identifier)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(identifier))
                return false;

            int index = -1;
            while ((index = text.IndexOf(identifier, index + 1, StringComparison.Ordinal)) >= 0)
            {
                int beforeIndex = index - 1;
                int afterIndex = index + identifier.Length;
                bool startsAtIdentifierBoundary = beforeIndex < 0 ||
                    text[beforeIndex] == '@' ||
                    !IsIdentifierPart(text[beforeIndex]);
                bool endsAtIdentifierBoundary = afterIndex >= text.Length ||
                    !IsIdentifierPart(text[afterIndex]);

                if (startsAtIdentifierBoundary && endsAtIdentifierBoundary)
                    return true;
            }

            return false;
        }

        private static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private IEnumerable<string> GetUsageWorkspaceFolders(string activeFilePath)
        {
            var folders = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddFolder(string folder)
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    return;

                string normalized = NormalizeCompletionPath(folder);
                if (seen.Add(normalized))
                    folders.Add(folder);
            }

            AddFolder(GetActiveWorkspaceFolder());
            AddUsageFoldersForFile(activeFilePath, AddFolder);

            foreach (TabPage tabPage in EditorTabControl.TabPages)
            {
                string tabPath = tabPage.ToolTipText?.Trim();
                if (IsCSharpFilePath(tabPath))
                    AddUsageFoldersForFile(tabPath, AddFolder);
            }

            lock (_completionDataLock)
            {
                foreach (var filePath in _workspaceCompilationUnits.Keys)
                    AddUsageFoldersForFile(filePath, AddFolder);
            }

            return folders;
        }

        private string GetUsageWorkspaceFolder(string activeFilePath)
        {
            return GetUsageWorkspaceFolders(activeFilePath).FirstOrDefault() ?? string.Empty;
        }

        private static void AddUsageFoldersForFile(string filePath, Action<string> addFolder)
        {
            if (!IsCSharpFilePath(filePath))
                return;

            string activeFolder = Path.GetDirectoryName(filePath);
            addFolder(FindWorkspaceRoot(activeFolder));
            addFolder(FindSolutionOrRepositoryRoot(activeFolder));
        }

        private static string FindSolutionOrRepositoryRoot(string startDir)
        {
            if (string.IsNullOrEmpty(startDir))
                return startDir;

            string dir = startDir;
            string projectRoot = string.Empty;
            for (int i = 0; i < 10; i++)
            {
                if (!Directory.Exists(dir))
                    break;

                try
                {
                    if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.Exists(Path.Combine(dir, ".git")))
                        return dir;

                    if (string.IsNullOrEmpty(projectRoot) &&
                        Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
                        projectRoot = dir;
                }
                catch { }

                string parent = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(parent) || parent == dir)
                    break;
                dir = parent;
            }

            return projectRoot;
        }

        private List<UsageLocation> FindSemanticUsages(string identifier, int identifierOffset,
            List<UsageDocument> documents, out bool targetResolved)
        {
            targetResolved = false;
            try
            {
                var activeDocument = documents.FirstOrDefault(document => document.IsActive);
                if (activeDocument == null)
                    return null;

                var compilation = CSharpCompilation.Create(
                    "__CiareFindUsages",
                    syntaxTrees: documents.Select(document => document.SyntaxTree),
                    references: BuildUsageReferences(),
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: GlobalVariables.OUnsafeCode));

                var activeModel = compilation.GetSemanticModel(activeDocument.SyntaxTree, true);
                ISymbol targetSymbol = GetIdentifierSymbolAtOffset(activeModel, activeDocument.Root, identifierOffset, identifier);
                if (targetSymbol == null)
                    return null;

                targetResolved = true;
                targetSymbol = NormalizeUsageSymbol(targetSymbol);

                // Search each document's tokens in parallel (Roslyn compilation/model is thread-safe for reads).
                var rawUsages = new ConcurrentBag<UsageLocation>();
                Parallel.ForEach(documents, CreateUsageParallelOptions(), document =>
                {
                    var semanticModel = compilation.GetSemanticModel(document.SyntaxTree, true);
                    foreach (var token in GetIdentifierTokens(document.Root, identifier))
                    {
                        if (IsDeclarationIdentifier(token))
                            continue;

                        ISymbol symbol = GetReferenceSymbolForIdentifier(semanticModel, token);
                        if (SymbolsMatch(targetSymbol, symbol))
                        {
                            rawUsages.Add(CreateUsageLocation(document, token));
                            continue;
                        }

                        if (symbol == null && IsPotentialUnresolvedUsage(token, targetSymbol))
                            rawUsages.Add(CreateUsageLocation(document, token));
                    }
                });

                // Deduplicate and sort on a single thread.
                var usages = new List<UsageLocation>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var usage in rawUsages)
                    AddUsageLocation(usages, seen, usage);

                return SortUsageLocations(usages);
            }
            catch
            {
                targetResolved = false;
                return null;
            }
        }

        private static List<UsageLocation> FindSyntaxUsages(string identifier, List<UsageDocument> documents)
        {
            var rawUsages = new ConcurrentBag<UsageLocation>();
            Parallel.ForEach(documents, CreateUsageParallelOptions(), document =>
            {
                foreach (var token in GetIdentifierTokens(document.Root, identifier))
                {
                    if (!IsDeclarationIdentifier(token))
                        rawUsages.Add(CreateUsageLocation(document, token));
                }
            });

            return SortUsageLocations(rawUsages.ToList());
        }

        private static IEnumerable<SyntaxToken> GetIdentifierTokens(CompilationUnitSyntax root, string identifier)
        {
            return root.DescendantTokens()
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken) &&
                    string.Equals(token.ValueText, identifier, StringComparison.Ordinal));
        }

        private static ISymbol GetIdentifierSymbolAtOffset(SemanticModel semanticModel,
            CompilationUnitSyntax root, int offset, string identifier)
        {
            if (semanticModel == null || root == null || offset < 0)
                return null;

            int safeOffset = Math.Max(0, Math.Min(offset, Math.Max(0, root.FullSpan.End - 1)));
            SyntaxToken token = root.FindToken(safeOffset);
            if (!token.IsKind(SyntaxKind.IdentifierToken) ||
                !string.Equals(token.ValueText, identifier, StringComparison.Ordinal))
                return null;

            return GetDeclaredSymbolForIdentifier(semanticModel, token) ??
                GetReferenceSymbolForIdentifier(semanticModel, token);
        }

        private static ISymbol GetReferenceSymbolForIdentifier(SemanticModel semanticModel, SyntaxToken token)
        {
            if (semanticModel == null || !(token.Parent is SimpleNameSyntax simpleName))
                return null;

            var symbolInfo = semanticModel.GetSymbolInfo(simpleName);
            return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        }

        private static bool SymbolsMatch(ISymbol targetSymbol, ISymbol candidateSymbol)
        {
            if (targetSymbol == null || candidateSymbol == null)
                return false;

            return SymbolEqualityComparer.Default.Equals(
                NormalizeUsageSymbol(targetSymbol),
                NormalizeUsageSymbol(candidateSymbol));
        }

        private static ISymbol NormalizeUsageSymbol(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method && method.ReducedFrom != null)
                symbol = method.ReducedFrom;

            return symbol?.OriginalDefinition ?? symbol;
        }

        private static bool IsPotentialUnresolvedUsage(SyntaxToken token, ISymbol targetSymbol)
        {
            if (targetSymbol is IMethodSymbol method)
            {
                var invocation = GetInvocationForIdentifier(token);
                if (invocation == null || !ArgumentCountMatches(method, invocation.ArgumentList.Arguments.Count))
                    return false;

                string qualifier = GetInvocationQualifier(token);
                if (string.IsNullOrEmpty(qualifier))
                    return true;

                string containingTypeName = method.ContainingType?.Name;
                string containingTypeFullName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    ?.Replace("global::", string.Empty);
                return string.Equals(qualifier, containingTypeName, StringComparison.Ordinal) ||
                    string.Equals(qualifier, containingTypeFullName, StringComparison.Ordinal) ||
                    qualifier.EndsWith("." + containingTypeName, StringComparison.Ordinal);
            }

            return token.Parent is IdentifierNameSyntax;
        }

        private static InvocationExpressionSyntax GetInvocationForIdentifier(SyntaxToken token)
        {
            if (!(token.Parent is SimpleNameSyntax simpleName))
                return null;

            if (simpleName.Parent is InvocationExpressionSyntax directInvocation &&
                directInvocation.Expression == simpleName)
                return directInvocation;

            if (simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName &&
                memberAccess.Parent is InvocationExpressionSyntax memberInvocation &&
                memberInvocation.Expression == memberAccess)
                return memberInvocation;

            return null;
        }

        private static string GetInvocationQualifier(SyntaxToken token)
        {
            if (token.Parent is SimpleNameSyntax simpleName &&
                simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName)
                return NormalizeCompletionExpression(memberAccess.Expression.ToString());

            return string.Empty;
        }

        private static bool ArgumentCountMatches(IMethodSymbol method, int argumentCount)
        {
            if (method == null)
                return false;

            int requiredCount = method.Parameters.Count(parameter => !parameter.HasExplicitDefaultValue && !parameter.IsParams);
            int totalCount = method.Parameters.Length;
            bool hasParams = method.Parameters.Length > 0 && method.Parameters[method.Parameters.Length - 1].IsParams;

            return argumentCount >= requiredCount && (hasParams || argumentCount <= totalCount);
        }

        private static bool IsDeclarationIdentifier(SyntaxToken token)
        {
            SyntaxNode node = token.Parent;
            while (node != null)
            {
                switch (node)
                {
                    case VariableDeclaratorSyntax variable when variable.Identifier == token:
                    case SingleVariableDesignationSyntax designation when designation.Identifier == token:
                    case ParameterSyntax parameter when parameter.Identifier == token:
                    case TypeParameterSyntax typeParameter when typeParameter.Identifier == token:
                    case LocalFunctionStatementSyntax localFunction when localFunction.Identifier == token:
                    case BaseTypeDeclarationSyntax typeDeclaration when typeDeclaration.Identifier == token:
                    case DelegateDeclarationSyntax delegateDeclaration when delegateDeclaration.Identifier == token:
                    case MethodDeclarationSyntax method when method.Identifier == token:
                    case ConstructorDeclarationSyntax constructor when constructor.Identifier == token:
                    case DestructorDeclarationSyntax destructor when destructor.Identifier == token:
                    case PropertyDeclarationSyntax property when property.Identifier == token:
                    case EventDeclarationSyntax eventDeclaration when eventDeclaration.Identifier == token:
                    case EnumMemberDeclarationSyntax enumMember when enumMember.Identifier == token:
                    case ForEachStatementSyntax forEach when forEach.Identifier == token:
                    case CatchDeclarationSyntax catchDeclaration when catchDeclaration.Identifier == token:
                        return true;
                }

                node = node.Parent;
            }

            return false;
        }

        private static UsageLocation CreateUsageLocation(UsageDocument document, SyntaxToken token)
        {
            var lineSpan = token.GetLocation().GetLineSpan();
            int line = lineSpan.StartLinePosition.Line + 1;
            int column = lineSpan.StartLinePosition.Character + 1;
            return new UsageLocation(document.DisplayPath, line, column, document.GetLineText(line).Trim());
        }

        private static void AddUsageLocation(List<UsageLocation> usages, HashSet<string> seen, UsageLocation usage)
        {
            if (usage == null)
                return;

            string key = $"{usage.FilePath}|{usage.Line}|{usage.Column}";
            if (seen.Add(key))
                usages.Add(usage);
        }

        private static List<UsageLocation> SortUsageLocations(List<UsageLocation> usages)
        {
            return usages
                .OrderBy(usage => usage.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(usage => usage.Line)
                .ThenBy(usage => usage.Column)
                .ToList();
        }

        private static IEnumerable<MetadataReference> BuildUsageReferences()
        {
            lock (_usageReferencesLock)
            {
                if (_usagePlatformReferences == null)
                {
                    _usagePlatformReferences = new List<MetadataReference>();
                    string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
                    if (!string.IsNullOrEmpty(trusted))
                    {
                        foreach (var referencePath in trusted.Split(Path.PathSeparator))
                        {
                            if (!string.IsNullOrEmpty(referencePath) && File.Exists(referencePath))
                                _usagePlatformReferences.Add(MetadataReference.CreateFromFile(referencePath));
                        }
                    }
                }

                var customReferences = GlobalVariables.customRefList ?? new List<string>();
                string customReferenceKey = string.Join("|", customReferences);
                if (!string.Equals(_usageCustomReferenceKey, customReferenceKey, StringComparison.Ordinal))
                {
                    _usageCustomReferenceKey = customReferenceKey;
                    _usageCustomReferences = new List<MetadataReference>();
                    foreach (var customReference in customReferences)
                    {
                        try
                        {
                            var parts = customReference.Split('|');
                            string referencePath = parts.Length >= 2 ? parts[1] : customReference;
                            if (!string.IsNullOrEmpty(referencePath) && File.Exists(referencePath))
                                _usageCustomReferences.Add(MetadataReference.CreateFromFile(referencePath));
                        }
                        catch { }
                    }
                }

                return _usagePlatformReferences.Concat(_usageCustomReferences).ToList();
            }
        }

        private void ShowFindUsagesResults(string identifier, List<UsageLocation> usages)
        {
            CloseFindUsagesWindow();

            var form = new Form
            {
                Text = "Find Usages - " + identifier,
                Icon = this.Icon,
                Size = new Size(900, 480),
                MinimumSize = new Size(640, 320),
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                ShowIcon = true,
                KeyPreview = true,
            };
            form.Location = new Point(
                Math.Max(0, Left + 80),
                Math.Max(0, Top + 80));

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(10, 9, 10, 0),
                Text = $"Usages of '{identifier}' ({usages.Count})"
            };

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                Activation = ItemActivation.Standard
            };
            list.Columns.Add("File", 340);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Column", 70);
            list.Columns.Add("Code", 400);

            string workspaceFolder = GetUsageWorkspaceFolder(GetActiveEditorFilePath());
            bool hasWorkspaceFolder = Directory.Exists(workspaceFolder);
            var usageItems = new List<ListViewItem>(Math.Max(1, usages.Count));
            foreach (var usage in usages)
            {
                var item = new ListViewItem(GetUsageWindowDisplayPath(usage.FilePath, workspaceFolder, hasWorkspaceFolder))
                {
                    Tag = usage,
                    ToolTipText = usage.FilePath
                };
                item.SubItems.Add(usage.Line.ToString());
                item.SubItems.Add(usage.Column.ToString());
                item.SubItems.Add(usage.Text);
                usageItems.Add(item);
            }

            if (usages.Count == 0)
                usageItems.Add(new ListViewItem(new[] { "No usages found.", string.Empty, string.Empty, string.Empty }));

            list.BeginUpdate();
            try
            {
                list.Items.AddRange(usageItems.ToArray());
            }
            finally
            {
                list.EndUpdate();
            }

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                WrapContents = false
            };
            var closeButton = new Button
            {
                Text = "Close",
                Width = 90,
                Height = 26
            };
            var openButton = new Button
            {
                Text = "Open",
                Width = 90,
                Height = 26,
                Enabled = usages.Count > 0
            };
            footer.Controls.Add(closeButton);
            footer.Controls.Add(openButton);

            void OpenSelectedUsage()
            {
                if (list.SelectedItems.Count == 0)
                    return;

                if (list.SelectedItems[0].Tag is UsageLocation usage)
                    NavigateToUsageLocation(usage.FilePath, usage.Line, usage.Column);
            }

            openButton.Click += (sender, e) => OpenSelectedUsage();
            closeButton.Click += (sender, e) => form.Close();
            list.ItemActivate += (sender, e) => OpenSelectedUsage();
            list.SelectedIndexChanged += (sender, e) =>
                openButton.Enabled = list.SelectedItems.Count > 0 && list.SelectedItems[0].Tag is UsageLocation;
            list.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OpenSelectedUsage();
                    e.Handled = true;
                }
            };
            form.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    form.Close();
            };
            list.Resize += (sender, e) => ResizeFindUsagesColumns(list);

            ApplyFindUsagesWindowTheme(form, header, list, footer, openButton, closeButton);
            form.HandleCreated += (sender, e) => ApplyFindUsagesDarkTitleBar(form);

            form.Controls.Add(list);
            form.Controls.Add(footer);
            form.Controls.Add(header);
            form.Shown += (sender, e) =>
            {
                ApplyFindUsagesDarkTitleBar(form);
                ResizeFindUsagesColumns(list);
                if (list.Items.Count > 0 && list.Items[0].Tag is UsageLocation)
                    list.Items[0].Selected = true;
                list.Focus();
            };
            form.FormClosed += (sender, e) =>
            {
                if (ReferenceEquals(_findUsagesWindow, form))
                    _findUsagesWindow = null;
            };

            _findUsagesWindow = form;
            form.Show(this);
        }

        private void ShowFindUsagesMessage(string message)
        {
            MessageBox.Show(this, message, "Find Usages", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CloseFindUsagesWindow()
        {
            if (_findUsagesWindow != null && !_findUsagesWindow.IsDisposed)
                _findUsagesWindow.Close();
            _findUsagesWindow = null;
        }

        private string GetUsageWindowDisplayPath(string filePath)
        {
            string workspaceFolder = GetUsageWorkspaceFolder(GetActiveEditorFilePath());
            return GetUsageWindowDisplayPath(filePath, workspaceFolder, Directory.Exists(workspaceFolder));
        }

        private static string GetUsageWindowDisplayPath(string filePath, string workspaceFolder, bool hasWorkspaceFolder)
        {
            if (string.IsNullOrEmpty(filePath) || string.Equals(filePath, CurrentFileUsageDisplayName, StringComparison.Ordinal))
                return CurrentFileUsageDisplayName;

            try
            {
                if (hasWorkspaceFolder && IsPathInsideFolder(filePath, workspaceFolder))
                    return Path.GetRelativePath(workspaceFolder, filePath);
            }
            catch { }

            return filePath;
        }

        private static void ResizeFindUsagesColumns(ListView list)
        {
            if (list == null || list.Columns.Count < 4)
                return;

            int width = Math.Max(480, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
            int lineWidth = 70;
            int columnWidth = 70;
            int fileWidth = Math.Max(220, width * 40 / 100);
            int codeWidth = Math.Max(180, width - fileWidth - lineWidth - columnWidth);

            list.Columns[0].Width = fileWidth;
            list.Columns[1].Width = lineWidth;
            list.Columns[2].Width = columnWidth;
            list.Columns[3].Width = codeWidth;
        }

        private static void ApplyFindUsagesWindowTheme(Form form, Label header, ListView list,
            FlowLayoutPanel footer, params Button[] buttons)
        {
            if (!GlobalVariables.darkColor)
                return;

            Color background = GlobalVariables.controlBgColor;
            Color foreground = Color.FromArgb(192, 215, 207);
            form.BackColor = background;
            form.ForeColor = foreground;
            header.BackColor = background;
            header.ForeColor = foreground;
            list.BackColor = Color.FromArgb(30, 30, 30);
            list.ForeColor = foreground;
            footer.BackColor = background;

            foreach (var button in buttons)
            {
                button.BackColor = Color.FromArgb(45, 45, 48);
                button.ForeColor = foreground;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
            }
        }

        private static void ApplyFindUsagesDarkTitleBar(Form form)
        {
            if (!GlobalVariables.darkColor || form == null || form.IsDisposed)
                return;

            try
            {
                FrmColorMod.EnableDarkTitleBar(form.Handle);
            }
            catch
            {
                // The usages window should still work if DWM dark title bars are unavailable.
            }
        }

        private static string NormalizeIdentifierText(string identifier)
        {
            identifier = identifier?.Trim() ?? string.Empty;
            return identifier.StartsWith("@", StringComparison.Ordinal) ? identifier.Substring(1) : identifier;
        }

        private static bool IsValidIdentifierText(string identifier)
        {
            identifier = NormalizeIdentifierText(identifier);
            return identifier.Length > 0 && SyntaxFacts.IsValidIdentifier(identifier);
        }

        /// <summary>
        /// Navigates the editor to the given file and line number. Opens the file in a new
        /// tab when it is not the currently active file.
        /// </summary>
        internal void NavigateToDefinition(string filePath, int lineNumber)
        {
            if (string.IsNullOrEmpty(filePath) || lineNumber <= 0 || !File.Exists(filePath))
                return;
            string normalizedPath = NormalizeCompletionPath(filePath);
            string currentFilePath = NormalizeCompletionPath(GetActiveEditorFilePath());
            if (!string.Equals(normalizedPath, currentFilePath, StringComparison.OrdinalIgnoreCase))
                OpenFileFromExplorer(filePath);
            var editor = SelectedEditor.GetSelectedEditor();
            if (editor != null)
                editor.ActiveTextAreaControl.JumpTo(lineNumber - 1);
        }

        private void NavigateToUsageLocation(string filePath, int lineNumber, int columnNumber)
        {
            if (lineNumber <= 0)
                return;

            if (!string.Equals(filePath, CurrentFileUsageDisplayName, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                string normalizedPath = NormalizeCompletionPath(filePath);
                string currentFilePath = NormalizeCompletionPath(GetActiveEditorFilePath());
                if (!string.Equals(normalizedPath, currentFilePath, StringComparison.OrdinalIgnoreCase))
                    OpenFileFromExplorer(filePath);
            }

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null || editor.Document.TotalNumberOfLines == 0)
                return;

            int lineIndex = Math.Max(0, Math.Min(lineNumber - 1, editor.Document.TotalNumberOfLines - 1));
            var lineSegment = editor.Document.GetLineSegment(lineIndex);
            int columnIndex = Math.Max(0, Math.Min(columnNumber - 1, lineSegment.Length));
            editor.ActiveTextAreaControl.JumpTo(lineIndex, columnIndex);
            editor.Focus();
        }

        /// <summary>
        /// Walks up the directory tree from <paramref name="startDir"/> looking for a project
        /// root marker (.sln, .csproj, .git, .vs). Falls back to the parent directory of
        /// <paramref name="startDir"/> when no marker is found (so sibling folders are still included).
        /// </summary>
        private static string FindWorkspaceRoot(string startDir)
        {
            if (string.IsNullOrEmpty(startDir)) return startDir;
            string dir = startDir;
            for (int i = 0; i < 8; i++)
            {
                if (!Directory.Exists(dir)) break;
                try
                {
                    if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.Exists(Path.Combine(dir, ".git")) ||
                        Directory.Exists(Path.Combine(dir, ".vs")))
                        return dir;
                }
                catch { }
                string parent = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(parent) || parent == dir) break;
                dir = parent;
            }
            // No project marker found — go one level up so sibling directories are included.
            string oneLevelUp = Path.GetDirectoryName(startDir);
            return string.IsNullOrEmpty(oneLevelUp) ? startDir : oneLevelUp;
        }

        private static IEnumerable<string> GetWorkspaceCsFiles(string folder)
        {
            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                IEnumerable<string> dirs;
                try { dirs = Directory.EnumerateDirectories(current); }
                catch { dirs = Array.Empty<string>(); }
                foreach (var d in dirs)
                {
                    string name = Path.GetFileName(d);
                    if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase) ||
                        DirectoryContainsProjectFile(d))
                    {
                        continue;
                    }

                    try
                    {
                        var attrs = File.GetAttributes(d);
                        if ((attrs & FileAttributes.Hidden) == 0 && (attrs & FileAttributes.System) == 0)
                            pending.Push(d);
                    }
                    catch { pending.Push(d); }
                }
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(current, "*.cs"); }
                catch { files = Array.Empty<string>(); }
                foreach (var f in files)
                    yield return f;
            }
        }

        private static bool DirectoryContainsProjectFile(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                return false;
            }
        }

        private static bool DirectoryContainsSolutionFile(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*.sln", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                return false;
            }
        }

        Dom.ICompilationUnit ConvertCompilationUnit(NRefactory.Ast.CompilationUnit cu)
        {
            Dom.NRefactoryResolver.NRefactoryASTConvertVisitor converter;
            converter = new Dom.NRefactoryResolver.NRefactoryASTConvertVisitor(myProjectContent);
            cu.AcceptVisitor(converter, null);
            return converter.Cu;
        }
        #endregion


        /// <summary>
        /// Form resize event used to store window size in registry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            if (this.WindowState != FormWindowState.Maximized)
            {
                InitializeEditor.SetEditorWindowSize(GlobalVariables.registryPath, this.Width, this.Height);
                InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath, false);
            }
            else
            {
                InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath, true);
            }
            QueueEditorLayoutRefresh();
        }


        /// <summary>
        /// Mark file for auto start event on Windows reboot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void markStartFileChk_CheckedChanged(object sender, EventArgs e)
        {
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.SetFilePath(markStartFileChk);
            if (GlobalVariables.OWinLoginState)
                autoStartFile.SetRegistryRunApp();
        }

        /// <summary>
        /// Set realtime spliter position controler event to middle on texteditor resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Resize(object sender, EventArgs e)
        {
            if (sender is TextEditorControl editor)
            {
                ConfigureEditorScrollBars(editor);
                if (editor.secondaryTextArea != null)
                    SplitEditorWindow.SetSplitWindowSize(editor, GlobalVariables.splitWindowPosition);
            }
        }

        /// <summary>
        /// Start live share host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void liveShareHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LiveShareHost liveShareHost = new LiveShareHost();
            liveShareHost.ShowDialog();
        }

        /// <summary>
        /// Send data to remote client.
        /// </summary>
        private async void SendData()
        {
            GlobalVariables.codeWriter = false;

            if (!GlobalVariables.connected)
                return;

            await Task.Delay(10);
            await _apiConnectionEvents.SendData(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
        }

        /// <summary>
        /// Manage live hub disconection event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void liveStatusPb_Paint(object sender, PaintEventArgs e)
        {
            if (GlobalVariables.connected && GlobalVariables.liveDisconnected)
            {
                if (GlobalVariables.apiRemoteConnected || GlobalVariables.apiConnected)
                    ApiConnectionEvents.ManageHubDisconnection(hubConnection, new Button());
            }
        }

        /// <summary>
        /// RichtextBox mouse wheel event for store zoomfactor value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outputRBT_MouseWheel(object sender, MouseEventArgs e) => GlobalVariables.zoomFactor = outputRBT.ZoomFactor;

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
            string titleTab = EditorTabControl.SelectedTab.Text.Trim();
            string filePath = EditorTabControl.SelectedTab.ToolTipText.Trim();

            if (isLoaded)
            {
                try
                {
                    GlobalVariables.textAreaFirst = SelectedEditor.GetSelectedEditor().primaryTextArea;
                    GlobalVariables.textAreaSecond = SelectedEditor.GetSelectedEditor().secondaryTextArea;
                    InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, SelectedEditor.GetSelectedEditor());
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
            var tabCount = this.EditorTabControl.TabCount;
            if (tabCount != _countTabs)
            {
                _countTabs = tabCount;
                dynamicTextEdtior = new TextEditorControl();
                TabPage tabPage = EditorTabControl.TabPages[EditorTabControl.SelectedIndex];
                ConfigureEditorTabPageLayout(tabPage);
                SetDesignEditor(ref dynamicTextEdtior);
                tabPage.Controls.Add(dynamicTextEdtior);
                QueueEditorLayoutRefresh();
                Initiliaze(EditorTabControl.SelectedIndex);
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
        /// Set design for every new editor controler.
        /// </summary>
        /// <param name="dynamicTextEdtior"></param>
        private void SetDesignEditor(ref TextEditorControl dynamicTextEdtior)
        {
            var tabCount = this.EditorTabControl.TabCount;
            var tabIndex = this.EditorTabControl.SelectedIndex;
            dynamicTextEdtior.Name = $"textEditorControl{tabCount}";
            dynamicTextEdtior.Anchor = AnchorStyles.None;
            dynamicTextEdtior.Dock = DockStyle.Fill;
            dynamicTextEdtior.BackColor = SystemColors.Window;
            dynamicTextEdtior.BorderStyle = BorderStyle.FixedSingle;
            dynamicTextEdtior.Font = new Font("Consolas", 10F);
            dynamicTextEdtior.Highlighting = null;
            dynamicTextEdtior.Location = new Point(0, 0);
            dynamicTextEdtior.Margin = Padding.Empty;
            dynamicTextEdtior.TabIndex = tabIndex;
            dynamicTextEdtior.VRulerRow = 0;
            dynamicTextEdtior.TextChanged += textEditorControl1_TextChanged;
            dynamicTextEdtior.Enter += textEditorControl1_Enter;
            dynamicTextEdtior.Resize += textEditorControl1_Resize;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragDrop += DynamicTextEdtior_DragDrop;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragOver += DynamicTextEdtior_DragEnter;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.AllowDrop = true;
            dynamicTextEdtior.ActiveTextAreaControl.HScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.VScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.KeyPress += CurlyBraket.TextArea_KeyPress;
            dynamicTextEdtior.ActiveTextAreaControl.AutoHideScrollbars = false;
            dynamicTextEdtior.ActiveTextAreaControl.TextEditorProperties.AutoInsertCurlyBracket = true;
            dynamicTextEdtior.ActiveTextAreaControl.VerticalScroll.Enabled = true;
            dynamicTextEdtior.ActiveTextAreaControl.HorizontalScroll.Enabled = true;
            dynamicTextEdtior.TextEditorProperties.StoreZoomSize = true;
            dynamicTextEdtior.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            ConfigureEditorControlLayout(dynamicTextEdtior);
            dynamicTextEdtior.Focus();
            HookEditorAskAI(dynamicTextEdtior);
        }

        /// <summary>
        /// Drag enter event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicTextEdtior_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// OpenFile in drag drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicTextEdtior_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Count() > 1)
            {
                MessageBox.Show("Only one file can be opened with drag & drop!", "CIARE", MessageBoxButtons.OK,
           MessageBoxIcon.Warning);
                return;
            }
            _filesDrag = files;
            worker = new BackgroundWorker();
            worker.DoWork += AddTabOnDop;
            worker.RunWorkerCompleted += DragDropWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void DragDropWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (sender is BackgroundWorker completedWorker)
            {
                completedWorker.DoWork -= AddTabOnDop;
                completedWorker.RunWorkerCompleted -= DragDropWorkerCompleted;
                completedWorker.Dispose();

                if (ReferenceEquals(worker, completedWorker))
                    worker = null;
            }
        }

        /// <summary>
        /// Open data from drag&drop to new tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTabOnDop(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                foreach (var file in _filesDrag)
                {
                    var isFileOpenedInTab = TabControllerManage.IsFileOpenedInTab(MainForm.Instance.EditorTabControl, file);
                    if (isFileOpenedInTab) return;
                    TabControllerManage.AddNewTab(EditorTabControl);
                    FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), file);
                }
            });
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

        /// <summary>
        /// Refresh top most.
        /// </summary>
        public void RefreshTopMost()
        {
            TopMost = true;
            TopMost = false;
        }
    }
}
