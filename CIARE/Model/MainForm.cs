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
using CIARE.Utils.Encryption;
using System.Collections;
using System.Collections.Concurrent;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CIARE.Properties;


namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
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
        private const string FileExplorerLoadingTag = "__loading__";
        private const string FileExplorerPathKey = "fileExplorerPath";
        private const string FileExplorerVisibleKey = "fileExplorerVisible";
        private static readonly string FileExplorerExpandedPathsFilePath =
            Path.Combine(GlobalVariables.userProfileDirectory, "fileExplorerExpandedPaths.cDat");
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
        private ContextMenuStrip _fileExplorerNuGetContextMenu;
        private ToolStripMenuItem _fileExplorerNuGetRemoveMenuItem;
        private ImageList _fileExplorerImageList;
        private string _fileExplorerRootPath = string.Empty;
        private int _fileExplorerWidth = FileExplorerDefaultWidth;
        private FileSystemWatcher _fileExplorerWatcher;
        private System.Windows.Forms.Timer _fileExplorerRefreshTimer;
        private System.Windows.Forms.Timer _fileExplorerNuGetRefreshTimer;
        private string _pendingProjectPackageRefreshPath = string.Empty;
        private bool _pendingProjectPackageRestore;
        private bool _pendingProjectPackageShowRestoreFailure;
        private int _projectPackageRefreshVersion;
        private bool _suppressFileExplorerExpandedStateSave;
        private readonly HashSet<string> _pendingRefreshPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const int WorkspaceCompletionMethodLimit = 300;
        private readonly object _completionDataLock = new object();
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

            splitContainer1.Panel1.SuspendLayout();
            EditorTabControl.SuspendLayout();

            splitContainer1.Panel1.Controls.Remove(EditorTabControl);

            _editorWorkspacePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };

            _editorExplorerSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 5
            };
            _editorExplorerSplitContainer.Panel1.Padding = Padding.Empty;
            _editorExplorerSplitContainer.Panel2.Padding = Padding.Empty;
            _editorExplorerSplitContainer.Panel1.Resize += (sender, args) => RefreshEditorLayoutBounds();
            _editorExplorerSplitContainer.SplitterMoved += (sender, args) => RefreshEditorLayoutBounds();

            _fileExplorerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };

            _fileExplorerContentSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 4,
                Panel2MinSize = 90
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
            _fileExplorerTree.NodeMouseDoubleClick += fileExplorerTree_NodeMouseDoubleClick;
            _fileExplorerTree.KeyDown += fileExplorerTree_KeyDown;

            _fileExplorerNuGetPackageColumn = new ColumnHeader { Text = "Package", Width = 130 };
            _fileExplorerNuGetVersionColumn = new ColumnHeader { Text = "Version", Width = 80 };

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
                Columns = { _fileExplorerNuGetPackageColumn, _fileExplorerNuGetVersionColumn },
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

            _fileExplorerNuGetRemoveMenuItem = new ToolStripMenuItem
            {
                Text = "Remove from Project"
            };
            _fileExplorerNuGetRemoveMenuItem.Click += fileExplorerNuGetRemoveMenuItem_Click;

            _fileExplorerNuGetContextMenu = new ContextMenuStrip(components);
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

            ConfigureEditorTabControlLayout();

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

            splitContainer1.Panel1.Controls.Add(_editorWorkspacePanel);
            splitContainer1.Panel1.ResumeLayout();
            EditorTabControl.ResumeLayout();
            BeginInvoke((Action)(() =>
            {
                ApplyEditorExplorerMinimumWidths();
                SetFileExplorerWidth(FileExplorerDefaultWidth);
                SetFileExplorerNuGetHeight(350);
                PositionFileExplorerShowButton();
                RefreshEditorLayoutBounds();
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
            _fileExplorerImageList.Images.Add("file", DrawFileIcon(Color.FromArgb(128, 128, 128), false));
            _fileExplorerImageList.Images.Add("cs", DrawFileIcon(Color.FromArgb(72, 133, 237), true));
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

            if (show)
            {
                _editorExplorerSplitContainer.Panel2Collapsed = false;
                ApplyEditorExplorerMinimumWidths();
                SetFileExplorerWidth(_fileExplorerWidth);
                _fileExplorerShowButton.Visible = false;
            }
            else
            {
                if (saveWidth)
                    _fileExplorerWidth = Math.Max(_editorExplorerSplitContainer.Panel2.Width, FileExplorerMinWidth);
                _editorExplorerSplitContainer.Panel2Collapsed = true;
                _fileExplorerShowButton.Visible = true;
                PositionFileExplorerShowButton();
                SelectedEditor.GetSelectedEditor()?.Focus();
            }

            RefreshEditorLayoutBounds();
            EditorTabControl.Invalidate();
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerVisibleKey, show.ToString());
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
                RefreshEditorLayoutBounds();
                return;
            }

            int minDist = _editorExplorerSplitContainer.Panel1MinSize;
            int maxDist = Math.Max(minDist, availableWidth - _editorExplorerSplitContainer.Panel2MinSize);
            int explorerWidth = Math.Max(_editorExplorerSplitContainer.Panel2MinSize,
                Math.Min(width, Math.Max(0, availableWidth - minDist)));
            int dist = Math.Max(minDist, Math.Min(maxDist, availableWidth - explorerWidth));

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
                _editorExplorerSplitContainer.SplitterDistance = dist;

            RefreshEditorLayoutBounds();
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

            int minPanel1 = 120;
            int nuGetHeight = Math.Max(_fileExplorerContentSplitContainer.Panel2MinSize,
                Math.Min(height, Math.Max(_fileExplorerContentSplitContainer.Panel2MinSize,
                    availableHeight - minPanel1)));
            _fileExplorerContentSplitContainer.SplitterDistance = Math.Max(0, availableHeight - nuGetHeight);
        }

        private int GetEditorExplorerSplitWidth()
        {
            if (_editorExplorerSplitContainer == null)
                return 0;

            return _editorExplorerSplitContainer.ClientSize.Width > 0
                ? _editorExplorerSplitContainer.ClientSize.Width
                : _editorExplorerSplitContainer.Width;
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

        private void ConfigureEditorTabControlLayout()
        {
            if (EditorTabControl == null)
                return;

            EditorTabControl.SuspendLayout();
            EditorTabControl.Anchor = AnchorStyles.None;
            EditorTabControl.Dock = DockStyle.Fill;
            EditorTabControl.Location = Point.Empty;
            EditorTabControl.Margin = Padding.Empty;

            foreach (TabPage tabPage in EditorTabControl.TabPages)
                ConfigureEditorTabPageLayout(tabPage);

            EditorTabControl.ResumeLayout(true);
        }

        private void ConfigureEditorTabPageLayout(TabPage tabPage)
        {
            if (tabPage == null)
                return;

            tabPage.AutoScroll = false;
            tabPage.Margin = Padding.Empty;
            tabPage.Padding = Padding.Empty;

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
            editor.Anchor = AnchorStyles.None;
            editor.Dock = DockStyle.Fill;
            editor.Location = Point.Empty;
            editor.Margin = Padding.Empty;
            ConfigureEditorScrollBars(editor);
            editor.ResumeLayout(true);
        }

        private void ConfigureEditorScrollBars(TextEditorControl editor)
        {
            var textAreaControl = editor?.ActiveTextAreaControl;
            if (textAreaControl == null)
                return;

            textAreaControl.AutoHideScrollbars = false;
            textAreaControl.VScrollBar.Visible = true;
            textAreaControl.HScrollBar.Visible = true;
            textAreaControl.ResizeTextArea();
            textAreaControl.AdjustScrollBars();
        }

        private void RefreshEditorLayoutBounds()
        {
            if (EditorTabControl == null || EditorTabControl.IsDisposed)
                return;

            ConfigureEditorTabControlLayout();
            EditorTabControl.PerformLayout();

            foreach (TabPage tabPage in EditorTabControl.TabPages)
                tabPage.PerformLayout();
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

            string savedPath = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerPathKey);
            string savedVisible = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerVisibleKey);

            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                LoadFileExplorerFolder(savedPath);

            if (savedVisible == "False")
                ToggleFileExplorer(false, saveWidth: false);
        }

        private void LoadFileExplorerFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath) || _fileExplorerTree == null)
                return;

            _fileExplorerRootPath = folderPath;
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
            var node = new TreeNode(text)
            {
                Tag = directory.FullName,
                ImageKey = "folder",
                SelectedImageKey = "folder-open",
                ToolTipText = directory.FullName
            };
            node.Nodes.Add(new TreeNode("Loading...") { Tag = FileExplorerLoadingTag });
            return node;
        }

        private TreeNode CreateFileNode(FileInfo file)
        {
            string imageKey = GetFileExplorerImageKey(file.FullName);
            return new TreeNode(file.Name)
            {
                Tag = file.FullName,
                ImageKey = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText = file.FullName
            };
        }

        private static string GetFileExplorerImageKey(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return "cs";
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

                var packages = ProjectNuGetManager.GetPackageReferences(projectPath);
                if (packages.Count == 0)
                {
                    AddFileExplorerNuGetPlaceholder("No PackageReference items");
                    return;
                }

                foreach (var package in packages)
                {
                    var item = new ListViewItem(new[] { package.Name, package.Version })
                    {
                        Tag = package,
                        ToolTipText = $"{package.Name} {package.Version}"
                    };
                    _fileExplorerNuGetList.Items.Add(item);
                }
            }
            finally
            {
                _fileExplorerNuGetList.EndUpdate();
                ResizeFileExplorerNuGetColumns();
            }
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
            RefreshExplorerNuGetPackages();

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                RealTimeChecker.InvalidateReferenceCache();
                ClearProjectPackageCompletionReferences();
                ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                return;
            }

            RealTimeChecker.InvalidateReferenceCache();
            Task.Run(() =>
            {
                ProcessRunResult restoreResult = null;
                if (restoreProject)
                    restoreResult = ProjectNuGetManager.RestoreProject(projectPath);

                RealTimeChecker.InvalidateReferenceCache();
                RefreshProjectPackageCompletionReferences(projectPath);
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

        private void RefreshProjectPackageCompletionReferences(string projectPath)
        {
            if (!GlobalVariables.OCodeCompletion || pcRegistry == null || myProjectContent == null)
                return;

            var referencePaths = ProjectNuGetManager.GetCompileReferencePaths(projectPath);
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

        private void AddFileExplorerNuGetPlaceholder(string text)
        {
            var item = new ListViewItem(new[] { text, string.Empty })
            {
                ForeColor = GlobalVariables.darkColor ? Color.FromArgb(150, 170, 165) : SystemColors.GrayText
            };
            _fileExplorerNuGetList.Items.Add(item);
        }

        private void ResizeFileExplorerNuGetColumns()
        {
            if (_fileExplorerNuGetList == null ||
                _fileExplorerNuGetList.IsDisposed ||
                _fileExplorerNuGetList.Columns.Count < 2)
            {
                return;
            }

            int availableWidth = _fileExplorerNuGetList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            if (availableWidth <= 0)
                return;

            int versionWidth = Math.Max(70, Math.Min(92, availableWidth / 3));
            _fileExplorerNuGetVersionColumn.Width = versionWidth;
            _fileExplorerNuGetPackageColumn.Width = Math.Max(90, availableWidth - versionWidth);
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
            _fileExplorerNuGetRemoveMenuItem.Tag = package;
            _fileExplorerNuGetRemoveMenuItem.Text = $"Remove {package.Name} from Project";
            _fileExplorerNuGetContextMenu.Show(_fileExplorerNuGetList, e.Location);
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
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string filePath = GetActiveEditorFilePath();
            if (string.IsNullOrEmpty(filePath) || IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return _fileExplorerRootPath;

            return string.Empty;
        }

        public string GetActiveCompileProjectPath()
        {
            string filePath = GetActiveEditorFilePath();

            if (Directory.Exists(_fileExplorerRootPath) &&
                File.Exists(filePath) &&
                IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                string openFolderTarget = FindBuildTargetFile(_fileExplorerRootPath, filePath);
                if (!string.IsNullOrEmpty(openFolderTarget))
                    return openFolderTarget;
            }

            return string.Empty;
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
            _fileExplorerPanel.BackColor = backColor;
            _fileExplorerHeader.BackColor = headerColor;
            _fileExplorerTitleLabel.BackColor = headerColor;
            _fileExplorerTitleLabel.ForeColor = foreColor;
            _fileExplorerTree.BackColor = backColor;
            _fileExplorerTree.ForeColor = foreColor;
            _fileExplorerTree.LineColor = borderColor;
            _fileExplorerContentSplitContainer.BackColor = borderColor;
            _fileExplorerNuGetPanel.BackColor = backColor;
            _fileExplorerNuGetTitleLabel.BackColor = headerColor;
            _fileExplorerNuGetTitleLabel.ForeColor = foreColor;
            _fileExplorerNuGetList.BackColor = backColor;
            _fileExplorerNuGetList.ForeColor = foreColor;

            ApplyFileExplorerButtonTheme(_fileExplorerOpenFolderButton, buttonBackColor, foreColor, borderColor);
            ApplyFileExplorerButtonTheme(_fileExplorerHideButton, buttonBackColor, foreColor, borderColor);
            ApplyFileExplorerButtonTheme(_fileExplorerShowButton, buttonBackColor, foreColor, borderColor);
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
            RefreshEditorLayoutBounds();
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

                pcRegistry = new Dom.ProjectContentRegistry();
                if (!Directory.Exists((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"))))
                    Directory.CreateDirectory((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion")));

                pcRegistry.ActivatePersistence(Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"));
                SelectedEditor.GetSelectedEditor(index).ActiveTextAreaControl.Refresh();
            }
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
                ApplyTabControlDarkMode(outputTabControl, darkBg);
                ApplyFileExplorerTheme(highlight);
                return;
            }
            GlobalVariables.darkColor = false;
            LightModeMain.SetLightModeMain(this, outputRBT, groupBox1,
                menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
            errorsLV.BackColor = SystemColors.Window;
            errorsLV.ForeColor = Color.Black;
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
            tabControl.BackColor = backColor;
            foreach (TabPage page in tabControl.TabPages)
                page.BackColor = backColor;
            tabControl.Invalidate();
        }

        private void OutputTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            bool dark = GlobalVariables.darkColor;
            var g = e.Graphics;
            var tp = outputTabControl.TabPages[e.Index];
            var rt = e.Bounds;

            // Fill tab background in dark mode (same colours as the editor tabs)
            if (dark)
            {
                Color tabBg = GlobalVariables.TabBgColor;
                using (var bgBrush = new SolidBrush(tabBg))
                    g.FillRectangle(bgBrush, rt);
            }

            // Fill the empty strip area beyond the last tab (same helper as editor tabs)
            TabControllerManage.SetTransparentTabBar(outputTabControl, e,
                GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);

            // Draw text centred in the FULL tab rect — no close-button indentation for output tabs
            Color textColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            using (var textBrush = new SolidBrush(textColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            })
            {
                g.DrawString(tp.Text, tp.Font ?? Control.DefaultFont, textBrush, (RectangleF)rt, sf);
            }
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
                var parserThread = new Thread(ParserThread);
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
            string workspaceFolder = null;
            string currentFilePath = null;
            Invoke(new MethodInvoker(delegate
            {
                code = SelectedEditor.GetSelectedEditor().Text;
                workspaceFolder = !string.IsNullOrEmpty(_fileExplorerRootPath)
                    ? _fileExplorerRootPath
                    : (!string.IsNullOrEmpty(GetActiveEditorFilePath())
                        ? FindWorkspaceRoot(Path.GetDirectoryName(GetActiveEditorFilePath()))
                        : null);
                currentFilePath = GetActiveEditorFilePath();
            }));
            var workspaceCompletionClasses = new List<WorkspaceCompletionClass>();
            AddRoslynCompletionClasses(workspaceCompletionClasses, code, currentFilePath);
            var topLevelFunctions = new List<WorkspaceCompletionItem>();
            CollectTopLevelLocalFunctions(topLevelFunctions, code, currentFilePath);
            string parsedCode = IsVisualBasic ? code : WrapTopLevelStatementsForNRefactory(ConvertFileScopedNamespace(code), out _, out _);
            TextReader textReader = new StringReader(parsedCode);
            Dom.ICompilationUnit newCompilationUnit;
            NRefactory.SupportedLanguage supportedLanguage;
            if (IsVisualBasic)
                supportedLanguage = NRefactory.SupportedLanguage.VBNet;
            else
                supportedLanguage = NRefactory.SupportedLanguage.CSharp;
            using (NRefactory.IParser p = NRefactory.ParserFactory.CreateParser(supportedLanguage, textReader))
            {
                p.ParseMethodBodies = false;
                p.Parse();
                newCompilationUnit = ConvertCompilationUnit(p.CompilationUnit);
            }
            if (newCompilationUnit is Dom.DefaultCompilationUnit dcuCurrent && !string.IsNullOrEmpty(currentFilePath))
                dcuCurrent.FileName = currentFilePath;
            lock (_completionDataLock)
            {
                myProjectContent.UpdateCompilationUnit(lastCompilationUnit, newCompilationUnit, DummyFileName);
                lastCompilationUnit = newCompilationUnit;
                parseInformation.SetCompilationUnit(newCompilationUnit);
            }
            ParseWorkspaceFilesForCompletion(workspaceFolder, currentFilePath, workspaceCompletionClasses);
            lock (_completionDataLock)
            {
                _workspaceCompletionClasses.Clear();
                _workspaceCompletionClasses.AddRange(workspaceCompletionClasses);
                _topLevelLocalFunctions.Clear();
                _topLevelLocalFunctions.AddRange(topLevelFunctions);
            }
        }

        private void ParseWorkspaceFilesForCompletion(string workspaceFolder, string currentFilePath, List<WorkspaceCompletionClass> workspaceCompletionClasses)
        {
            if (string.IsNullOrEmpty(workspaceFolder) || !Directory.Exists(workspaceFolder))
            {
                lock (_completionDataLock)
                {
                    foreach (var pair in _workspaceCompilationUnits)
                        myProjectContent.RemoveCompilationUnit(pair.Value);
                    _workspaceCompilationUnits.Clear();
                }
                return;
            }

            string normalizedCurrent = NormalizeCompletionPath(currentFilePath);
            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in GetWorkspaceCsFiles(workspaceFolder))
            {
                if (!string.IsNullOrEmpty(normalizedCurrent) &&
                    string.Equals(NormalizeCompletionPath(filePath), normalizedCurrent, StringComparison.OrdinalIgnoreCase))
                    continue;

                visitedPaths.Add(filePath);
                try
                {
                    string originalFileCode = File.ReadAllText(filePath);
                    AddRoslynCompletionClasses(workspaceCompletionClasses, originalFileCode, filePath);
                    string fileCode = ConvertFileScopedNamespace(originalFileCode);
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
                        _workspaceCompilationUnits.TryGetValue(filePath, out var oldCU);
                        myProjectContent.UpdateCompilationUnit(oldCU, newCU, filePath);
                        _workspaceCompilationUnits[filePath] = newCU;
                    }
                }
                catch { }
            }

            lock (_completionDataLock)
            {
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
            private readonly string[] _lines;

            public UsageDocument(string filePath, string text, SyntaxTree syntaxTree, CompilationUnitSyntax root, bool isActive)
            {
                FilePath = filePath ?? string.Empty;
                Text = text ?? string.Empty;
                SyntaxTree = syntaxTree;
                Root = root;
                IsActive = isActive;
                _lines = Regex.Split(Text, "\r\n|\r|\n");
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
                return index >= 0 && index < _lines.Length ? _lines[index] : string.Empty;
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
            Parallel.ForEach(pathsToScan, filePath =>
            {
                try
                {
                    if (!FileContainsIdentifier(filePath, identifier))
                        return;
                    string text = File.ReadAllText(filePath);
                    var syntaxTree = CSharpSyntaxTree.ParseText(text, parseOptions, path: filePath);
                    var root = syntaxTree.GetCompilationUnitRoot();
                    string normalizedPath = NormalizeCompletionPath(filePath);
                    if (!string.IsNullOrEmpty(normalizedPath))
                        newDocs.TryAdd(normalizedPath, new UsageDocument(filePath, text, syntaxTree, root, false));
                }
                catch { }
            });

            foreach (var kvp in newDocs)
            {
                if (seen.Add(kvp.Key))
                    documents.Add(kvp.Value);
            }

            return documents;
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
                Parallel.ForEach(documents, document =>
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
            Parallel.ForEach(documents, document =>
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

            foreach (var usage in usages)
            {
                var item = new ListViewItem(GetUsageWindowDisplayPath(usage.FilePath))
                {
                    Tag = usage,
                    ToolTipText = usage.FilePath
                };
                item.SubItems.Add(usage.Line.ToString());
                item.SubItems.Add(usage.Column.ToString());
                item.SubItems.Add(usage.Text);
                list.Items.Add(item);
            }

            if (usages.Count == 0)
                list.Items.Add(new ListViewItem(new[] { "No usages found.", string.Empty, string.Empty, string.Empty }));

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
            if (string.IsNullOrEmpty(filePath) || string.Equals(filePath, CurrentFileUsageDisplayName, StringComparison.Ordinal))
                return CurrentFileUsageDisplayName;

            try
            {
                string workspaceFolder = GetUsageWorkspaceFolder(GetActiveEditorFilePath());
                if (Directory.Exists(workspaceFolder) && IsPathInsideFolder(filePath, workspaceFolder))
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
                        string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase))
                        continue;
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
            RefreshEditorLayoutBounds();
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
                ConfigureEditorScrollBars(editor);
            SplitEditorWindow.SetSplitWindowSize(SelectedEditor.GetSelectedEditor(), GlobalVariables.splitWindowPosition);
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
                RefreshEditorLayoutBounds();
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
            worker.RunWorkerAsync();
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
