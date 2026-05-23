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
using Button = System.Windows.Forms.Button;
using CIARE.Model;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.ComponentModel;
using CIARE.Utils.Encryption;
using System.Collections;


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
        private Panel _editorWorkspacePanel;
        private SplitContainer _editorExplorerSplitContainer;
        private Panel _fileExplorerPanel;
        private TableLayoutPanel _fileExplorerHeader;
        private Label _fileExplorerTitleLabel;
        private Button _fileExplorerOpenFolderButton;
        private Button _fileExplorerHideButton;
        private Button _fileExplorerShowButton;
        private TreeView _fileExplorerTree;
        private ImageList _fileExplorerImageList;
        private string _fileExplorerRootPath = string.Empty;
        private int _fileExplorerWidth = FileExplorerDefaultWidth;
        private const int WorkspaceCompletionMethodLimit = 300;
        private readonly object _completionDataLock = new object();
        private readonly Dictionary<string, Dom.ICompilationUnit> _workspaceCompilationUnits
            = new Dictionary<string, Dom.ICompilationUnit>(StringComparer.OrdinalIgnoreCase);


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
                menu.AskAIAction = () => AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
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
            _fileExplorerTree.NodeMouseDoubleClick += fileExplorerTree_NodeMouseDoubleClick;
            _fileExplorerTree.KeyDown += fileExplorerTree_KeyDown;

            _fileExplorerHeader.Controls.Add(_fileExplorerTitleLabel, 0, 0);
            _fileExplorerHeader.Controls.Add(_fileExplorerOpenFolderButton, 1, 0);
            _fileExplorerHeader.Controls.Add(_fileExplorerHideButton, 2, 0);
            _fileExplorerPanel.Controls.Add(_fileExplorerTree);
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
                PositionFileExplorerShowButton();
                RefreshEditorLayoutBounds();
            }));

            ApplyFileExplorerTheme();
        }

        private ImageList CreateFileExplorerImageList()
        {
            _fileExplorerImageList = new ImageList
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
            _fileExplorerTree.BeginUpdate();
            _fileExplorerTree.Nodes.Clear();
            var root = CreateDirectoryNode(new DirectoryInfo(folderPath));
            _fileExplorerTree.Nodes.Add(root);
            PopulateDirectoryNode(root);
            root.Expand();
            _fileExplorerTree.EndUpdate();
            _fileExplorerTitleLabel.Text = "Explorer";
            toolTip1.SetToolTip(_fileExplorerTitleLabel, folderPath);

            ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
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
                RealTimeChecker.Cancel(editor, typeCheckLbl, errorsRTB, errorsTabPage, warningsCheckLbl);
                return;
            }

            RealTimeChecker.ScheduleCheck(editor.Text, editor, typeCheckLbl, errorsRTB, errorsTabPage,
                warningsCheckLbl, GetActiveWorkspaceFolder(), filePath);
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

        private static bool IsCSharpFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase);
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
            RealTimeChecker.Cancel(SelectedEditor.GetSelectedEditor(), typeCheckLbl, errorsRTB, errorsTabPage, warningsCheckLbl);
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
                errorsRTB.BackColor = darkBg;
                errorsRTB.ForeColor = darkFg;
                ApplyTabControlDarkMode(outputTabControl, darkBg);
                ApplyFileExplorerTheme(highlight);
                return;
            }
            GlobalVariables.darkColor = false;
            LightModeMain.SetLightModeMain(this, outputRBT, groupBox1,
                menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
            errorsRTB.BackColor = SystemColors.Window;
            errorsRTB.ForeColor = Color.Black;
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

        private void errorsRTB_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int charIndex = errorsRTB.GetCharIndexFromPosition(e.Location);
            int rtbLine = errorsRTB.GetLineFromCharIndex(charIndex);
            if (rtbLine < 0 || rtbLine >= errorsRTB.Lines.Length) return;

            var match = Regex.Match(
                errorsRTB.Lines[rtbLine], @"Line\s+(\d+)");
            if (!match.Success) return;

            if (!int.TryParse(match.Groups[1].Value, out int targetLine)) return;

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;

            GoToLineNumber.GoToLine(editor, targetLine);
            editor.Focus();
        }

        private string _clickedErrorLine = "";

        private void errorsRTB_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int charIndex = errorsRTB.GetCharIndexFromPosition(e.Location);
            int rtbLine = errorsRTB.GetLineFromCharIndex(charIndex);
            _clickedErrorLine = (rtbLine >= 0 && rtbLine < errorsRTB.Lines.Length)
                ? errorsRTB.Lines[rtbLine].Trim()
                : "";
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
            string parsedCode = IsVisualBasic ? code : ConvertFileScopedNamespace(code);
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
            ParseWorkspaceFilesForCompletion(workspaceFolder, currentFilePath);
        }

        private void ParseWorkspaceFilesForCompletion(string workspaceFolder, string currentFilePath)
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
                    string fileCode = ConvertFileScopedNamespace(File.ReadAllText(filePath));
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
            }

            return result;
        }

        /// <summary>
        /// Finds the first declaration of <paramref name="name"/> across all parsed compilation units.
        /// Returns (filePath, lineNumber) or (null, 0) if not found.
        /// </summary>
        internal (string FilePath, int Line) FindDefinition(string name)
        {
            if (string.IsNullOrEmpty(name))
                return (null, 0);

            lock (_completionDataLock)
            {
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
                GoToLineNumber.GoToLine(editor, lineNumber);
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
