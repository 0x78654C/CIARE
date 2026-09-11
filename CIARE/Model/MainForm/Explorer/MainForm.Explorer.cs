using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using Button = System.Windows.Forms.Button;

namespace CIARE
{
    public partial class MainForm
    {
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
        private ToolStripMenuItem _fileExplorerBuildProjectMenuItem;
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

            _fileExplorerBuildProjectMenuItem = new ToolStripMenuItem { Text = "Build Project" };
            _fileExplorerBuildProjectMenuItem.Click += fileExplorerBuildProjectMenuItem_Click;

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
            _fileExplorerContextMenu.Items.Add(_fileExplorerBuildProjectMenuItem);
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
            ApplyEditorExplorerMinimumWidths();
            PositionFileExplorerShowButton();

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

        private void ApplyFileExplorerTheme(string highlight = null)
        {
            if (_fileExplorerPanel == null)
                return;

            var theme = ThemeManager.GetCompletionThemeColors(highlight ?? _appliedTheme);
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
    }
}
