using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class FormDesignerWindow : Form
{
    internal readonly DesignSession Session = new();
    internal readonly DesignCanvas Canvas;
    internal readonly PropertyGrid Properties = new() { Dock = DockStyle.Fill, HelpVisible = true, PropertySort = PropertySort.Categorized };
    internal readonly DataGridView EventGrid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.None, EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2 };
    private readonly MainForm _main;
    private readonly TreeView _toolbox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, HideSelection = false, ShowLines = false, FullRowSelect = true, ItemHeight = 29 };
    private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "Search Toolbox", Margin = new Padding(8) };
    private readonly TreeView _outline = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, HideSelection = false };
    private readonly Label _selection = new() { Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
    private readonly Label _documentTab = new() { Dock = DockStyle.Top, Height = 34, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
    private readonly TabControl _inspector = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _coordinates = new();
    private readonly ToolStrip _commands = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 4, 6, 4) };
    private readonly ToolStrip _layout = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 2, 6, 2) };
    private ToolStripButton _undo, _redo;
    private bool _refreshing;
    private string _path;
    private string _exportPath;
    internal string LayoutPath => _path;

    internal FormDesignerWindow(MainForm main, string path = null)
    {
        _main = main;
        Font = new Font("Segoe UI", 9F);
        Text = "Form Designer — CIARE"; Name = "FormDesignerWindow";
        StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(1280, 780); MinimumSize = new Size(960, 640);
        AutoScaleMode = AutoScaleMode.Dpi; KeyPreview = true;
        if (main != null) Icon = main.Icon;
        Canvas = new DesignCanvas(Session);
        BuildLayout(); BuildCommands(); PopulateToolbox(); ApplyTheme();
        Session.Changed += (_, _) => RefreshDocument();
        Session.SelectionChanged += (_, _) => RefreshSelection();
        Canvas.DefaultEventRequested += (_, _) => CreateEvent(DesignCatalog.DefaultEvent(Session.Selected.Kind));
        Canvas.OperationFailed += error => Attempt(() => throw error);
        Canvas.ToolPlaced += (_, _) => { _toolbox.SelectedNode = _toolbox.Nodes[0]; SetStatus("Control added. Drag to move; use the handles to resize."); };
        Properties.PropertyValueChanged += (_, _) => Attempt(Session.Commit);
        _search.TextChanged += (_, _) => PopulateToolbox();
        _toolbox.AfterSelect += (_, e) => { Canvas.ToolKind = e.Node.Tag as string; SetStatus(Canvas.ToolKind == null ? "Select a control on the form or in Document Outline." : "Click the form to place a " + Canvas.ToolKind + ", or drag it from the Toolbox."); };
        _toolbox.ItemDrag += (_, e) => { if (e.Item is TreeNode { Tag: string kind }) _toolbox.DoDragDrop(new DataObject(DesignCanvas.ToolboxFormat, kind), DragDropEffects.Copy); };
        _toolbox.NodeMouseDoubleClick += (_, e) =>
        {
            if (e.Node.Tag is not string kind) return;
            Attempt(() =>
            {
                var parent = Session.Selected.IsContainer ? Session.Selected : Session.Document.ParentOf(Session.Selected);
                Session.Add(kind, parent, new Point(24, parent.Kind == "GroupBox" ? 32 : 24));
                Canvas.ToolKind = null; _toolbox.SelectedNode = _toolbox.Nodes[0]; Canvas.Focus();
            });
        };
        _outline.AfterSelect += (_, e) => { if (!_refreshing && e.Node.Tag is string id) Session.Select(id); };
        EventGrid.CellEndEdit += (_, e) =>
        {
            if (_refreshing || e.RowIndex < 0) return;
            string eventName = (string)EventGrid.Rows[e.RowIndex].Cells[0].Value;
            string handler = Convert.ToString(EventGrid.Rows[e.RowIndex].Cells[1].Value);
            Attempt(() => Session.BindEvent(eventName, handler));
        };
        EventGrid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) CreateEvent((string)EventGrid.Rows[e.RowIndex].Cells[0].Value); };
        FormClosing += (_, e) => { if (!e.Cancel && !ConfirmSave()) e.Cancel = true; };
        Shown += (_, _) => Canvas.Focus();
        if (path != null) LoadLayout(path); else RefreshDocument();
    }
    private static Label Heading(string text) => new() { Text = text, Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
    private void BuildLayout()
    {
        var workspace = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, Size = new Size(1280, 680), SplitterDistance = 190, Panel1MinSize = 150, Panel2MinSize = 600 };
        var inspectorSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, Size = new Size(1080, 680), SplitterDistance = 755, Panel1MinSize = 300, Panel2MinSize = 280 };
        workspace.Panel2.Controls.Add(inspectorSplit);
        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 39, Padding = new Padding(8) };
        searchPanel.Controls.Add(_search);
        workspace.Panel1.Controls.Add(_toolbox); workspace.Panel1.Controls.Add(searchPanel); workspace.Panel1.Controls.Add(Heading("TOOLBOX"));
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "DesignWorkspace" };
        scroll.Controls.Add(Canvas);
        inspectorSplit.Panel1.Controls.Add(scroll); inspectorSplit.Panel1.Controls.Add(_documentTab);
        var outlinePanel = new Panel { Dock = DockStyle.Top, Height = 170 };
        outlinePanel.Controls.Add(_outline); outlinePanel.Controls.Add(Heading("DOCUMENT OUTLINE"));
        var propertyPage = new TabPage("Properties"); propertyPage.Controls.Add(Properties);
        var eventPage = new TabPage("⚡ Events");
        EventGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Event", HeaderText = "Event", ReadOnly = true, FillWeight = 44 });
        EventGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Handler", HeaderText = "Event handler", FillWeight = 56 });
        EventGrid.ColumnHeadersHeight = 30; EventGrid.RowTemplate.Height = 27;
        var eventFooter = new Panel { Dock = DockStyle.Bottom, Height = 92, Padding = new Padding(6) };
        var eventHint = new Label { Dock = DockStyle.Top, Height = 42, Text = "Double-click an event to create a handler.\r\nClear its name to disconnect the event." };
        var eventButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34, WrapContents = false };
        var create = new Button { Text = "Create handler", AutoSize = true };
        create.Click += (_, _) => CreateEvent(CurrentEvent());
        var edit = new Button { Text = "Edit code", AutoSize = true };
        edit.Click += (_, _) => EditHandler();
        eventButtons.Controls.Add(create); eventButtons.Controls.Add(edit);
        eventFooter.Controls.Add(eventHint); eventFooter.Controls.Add(eventButtons);
        eventPage.Controls.Add(EventGrid); eventPage.Controls.Add(eventFooter);
        _inspector.TabPages.Add(propertyPage); _inspector.TabPages.Add(eventPage);
        inspectorSplit.Panel2.Controls.Add(_inspector); inspectorSplit.Panel2.Controls.Add(_selection); inspectorSplit.Panel2.Controls.Add(outlinePanel);
        var status = new StatusStrip(); status.Items.Add(_status); status.Items.Add(_coordinates);
        Controls.Add(workspace); Controls.Add(_layout); Controls.Add(_commands); Controls.Add(status);
    }
    private ToolStripButton Button(ToolStrip strip, string text, Action action, string tooltip = null)
    {
        var button = new ToolStripButton(text) { ToolTipText = tooltip ?? text, DisplayStyle = ToolStripItemDisplayStyle.Text, Margin = new Padding(2, 1, 2, 1) };
        button.Click += (_, _) => Attempt(() => { if (CommitEditors()) action(); }); strip.Items.Add(button); return button;
    }
    private void BuildCommands()
    {
        Button(_commands, "New", () => { if (ConfirmSave()) { _path = _exportPath = null; Session.Reset(new()); } });
        Button(_commands, "Open…", OpenLayout, "Open a CIARE form layout (Ctrl+O)");
        Button(_commands, "Save", () => Save(false), "Save layout (Ctrl+S)");
        Button(_commands, "Save As…", () => Save(true));
        _commands.Items.Add(new ToolStripSeparator());
        _undo = Button(_commands, "Undo", Session.Undo, "Undo (Ctrl+Z)");
        _redo = Button(_commands, "Redo", Session.Redo, "Redo (Ctrl+Y)");
        Button(_commands, "Delete", Session.Delete);
        _commands.Items.Add(new ToolStripSeparator());
        Button(_commands, "▶ Preview", Preview, "Preview controls without running event-handler code (F5)");
        Button(_commands, "View Code", ViewCode);
        Button(_commands, "Export C#…", () => Export(), "Export .cs and .Designer.cs; existing handler bodies are preserved");
        var snap = Button(_layout, "Snap to Grid", () => { }); snap.CheckOnClick = true; snap.Checked = true;
        snap.CheckedChanged += (_, _) => Canvas.SnapToGrid = snap.Checked;
        var grid = Button(_layout, "Show Grid", () => { }); grid.CheckOnClick = true; grid.Checked = true;
        grid.CheckedChanged += (_, _) => { Canvas.ShowGrid = grid.Checked; Canvas.RefreshDesign(); };
        _layout.Items.Add(new ToolStripSeparator());
        Button(_layout, "Copy", () => { Session.Copy(); SetStatus("Control copied. Paste duplicates the control and its children."); });
        Button(_layout, "Paste", Session.Paste);
        Button(_layout, "Bring to Front", () => Session.Reorder(true));
        Button(_layout, "Send to Back", () => Session.Reorder(false));
        _layout.Items.Add(new ToolStripSeparator());
        _layout.Items.Add(new ToolStripLabel("Arrow keys: move   •   Shift+Arrow: resize   •   Alt: bypass grid"));
    }
    private void PopulateToolbox()
    {
        _toolbox.BeginUpdate(); _toolbox.Nodes.Clear();
        _toolbox.Nodes.Add(new TreeNode("↖   Pointer"));
        foreach (var group in DesignCatalog.Controls.Where(c => c.Kind.Contains(_search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).GroupBy(c => c.Category))
        {
            var category = new TreeNode(group.Key);
            foreach (var item in group) category.Nodes.Add(new TreeNode(item.Glyph + "   " + item.Kind) { Tag = item.Kind });
            _toolbox.Nodes.Add(category); category.Expand();
        }
        _toolbox.EndUpdate(); Canvas.ToolKind = null;
    }
    private void RefreshDocument()
    {
        _refreshing = true;
        _outline.BeginUpdate(); _outline.Nodes.Clear();
        TreeNode Build(DesignNode node)
        {
            var tree = new TreeNode(node.ToString()) { Tag = node.Id };
            foreach (var child in node.Children) tree.Nodes.Add(Build(child));
            return tree;
        }
        _outline.Nodes.Add(Build(Session.Document.Form)); _outline.ExpandAll(); _outline.EndUpdate();
        _refreshing = false;
        _undo.Enabled = Session.CanUndo; _redo.Enabled = Session.CanRedo;
        Text = $"{Session.Document.Form.Name}{(Session.IsDirty ? " *" : "")} — Form Designer — CIARE";
        _documentTab.Text = $"{Session.Document.Form.Name} [Design]{(Session.IsDirty ? " *" : "")}    •    {Session.Document.AllNodes.Count() - 1} controls";
        RefreshSelection();
    }
    private void RefreshSelection()
    {
        _refreshing = true;
        var selected = Session.Selected;
        _selection.Text = selected.ToString();
        Properties.SelectedObject = new DesignPropertyView(selected);
        string activeEvent = EventGrid.CurrentRow?.Cells[0].Value as string;
        EventGrid.Rows.Clear();
        string defaultEvent = DesignCatalog.DefaultEvent(selected.Kind);
        foreach (var spec in DesignCatalog.EventsFor(selected.Kind).OrderBy(e => e.Name != defaultEvent).ThenBy(e => e.Name))
        {
            int row = EventGrid.Rows.Add(spec.Name, selected.Events.GetValueOrDefault(spec.Name, ""));
            EventGrid.Rows[row].Cells[1].ToolTipText = $"void handler(object sender, {spec.ArgumentType} e)";
            if (spec.Name == activeEvent) EventGrid.CurrentCell = EventGrid.Rows[row].Cells[1];
        }
        void SelectNode(TreeNode node)
        {
            if ((string)node.Tag == selected.Id) _outline.SelectedNode = node;
            foreach (TreeNode child in node.Nodes) SelectNode(child);
        }
        foreach (TreeNode node in _outline.Nodes) SelectNode(node);
        _coordinates.Text = $"{selected.X}, {selected.Y}    |    {selected.Width} × {selected.Height}";
        _refreshing = false;
    }
    private string CurrentEvent() => EventGrid.CurrentRow?.Cells[0].Value as string ?? DesignCatalog.DefaultEvent(Session.Selected.Kind);
    private void CreateEvent(string eventName)
    {
        Attempt(() =>
        {
            EventGrid.EndEdit();
            string handler = Session.CreateHandler(eventName);
            _inspector.SelectedIndex = 1;
            foreach (DataGridViewRow row in EventGrid.Rows) if ((string)row.Cells[0].Value == eventName) EventGrid.CurrentCell = row.Cells[1];
            SetStatus($"{eventName} → {handler}. Use Edit code to export and implement this handler.");
        });
    }
    private void EditHandler()
    {
        Attempt(() =>
        {
            string eventName = CurrentEvent(); CreateEvent(eventName);
            if (!Export()) return;
            _main?.ExplorerTreeFeature.OpenFileFromExplorer(_exportPath);
            var editor = SelectedEditor.GetSelectedEditor();
            string handler = Session.Selected.Events.GetValueOrDefault(eventName);
            int offset = handler == null ? -1 : editor?.Text.IndexOf("void " + handler + "(", StringComparison.Ordinal) ?? -1;
            if (offset >= 0)
            {
                int line = editor.Document.OffsetToPosition(offset).Line;
                editor.ActiveTextAreaControl.Caret.Line = line;
                editor.ActiveTextAreaControl.TextArea.ScrollTo(line);
            }
            // Owned windows stay above their owner; make the editor visible for implementation.
            if (_main != null) { WindowState = FormWindowState.Minimized; _main.Activate(); }
        });
    }
    private void SetStatus(string text) => _status.Text = text;
    private void Attempt(Action action)
    {
        try { action(); }
        catch (Exception error) { SetStatus(error.Message); MessageBox.Show(this, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    internal void LoadLayout(string path)
    {
        var document = DesignDocument.Load(path);
        _path = Path.GetFullPath(path); _exportPath = null; Session.Reset(document);
        SetStatus("Opened " + Path.GetFileName(path));
    }
    private void OpenLayout()
    {
        using var dialog = new OpenFileDialog { Filter = "CIARE form layouts (*.ciareform)|*.ciareform", Title = "Open Form Layout" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        // Validate before asking to discard the current layout.
        var document = DesignDocument.Load(dialog.FileName);
        if (!ConfirmSave()) return;
        _path = dialog.FileName; _exportPath = null; Session.Reset(document);
        SetStatus("Opened " + Path.GetFileName(_path));
    }
    internal bool Save(bool saveAs)
    {
        if (!CommitEditors()) return false;
        string path = _path;
        if (path == null || saveAs)
        {
            using var dialog = new SaveFileDialog { Filter = "CIARE form layout (*.ciareform)|*.ciareform", DefaultExt = "ciareform",
                FileName = path == null ? Session.Document.Form.Name + ".ciareform" : Path.GetFileName(path), InitialDirectory = path == null ? "" : Path.GetDirectoryName(path) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            path = dialog.FileName;
        }
        try
        {
            DesignDocument.WriteAtomic(path, Session.Document.Serialize());
            _path = path; Session.MarkSaved(); SetStatus("Layout saved: " + Path.GetFileName(path)); return true;
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Save Form Layout", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
    }
    private bool CommitEditors()
    {
        if (!ValidateChildren()) return false;
        if (!EventGrid.EndEdit()) return false;
        try { Session.Commit(); return true; }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }
    internal bool ConfirmSave()
    {
        if (!CommitEditors()) return false;
        if (!Session.IsDirty) return true;
        var answer = MessageBox.Show(this, "Save changes to " + Session.Document.Form.Name + "?", "Form Designer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer == DialogResult.No || answer == DialogResult.Yes && Save(false);
    }
    private bool Export()
    {
        if (!CommitEditors()) return false;
        using var dialog = new SaveFileDialog { Filter = "C# form code (*.cs)|*.cs", DefaultExt = "cs", Title = "Export Form — handler code is preserved",
            FileName = _exportPath == null ? Session.Document.Form.Name + ".cs" : Path.GetFileName(_exportPath),
            InitialDirectory = Path.GetDirectoryName(_exportPath ?? _path ?? "") ?? "", OverwritePrompt = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        string path = dialog.FileName;
        if (path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose the code-behind name, such as Form1.cs. The .Designer.cs file is created alongside it.");
        string designerPath = Path.ChangeExtension(path, ".Designer.cs");
        var openEditors = _main?.EditorTabControl.TabPages.Cast<TabPage>().Where(p =>
            string.Equals(p.ToolTipText.Trim(), path, StringComparison.OrdinalIgnoreCase) || string.Equals(p.ToolTipText.Trim(), designerPath, StringComparison.OrdinalIgnoreCase))
            .Select(p => (Page: p, Editor: p.Controls.OfType<TextEditorControl>().FirstOrDefault())).Where(p => p.Editor != null).ToArray();
        if (openEditors != null)
            foreach (var item in openEditors)
                if (!File.Exists(item.Page.ToolTipText.Trim()) || item.Editor.Text != File.ReadAllText(item.Page.ToolTipText.Trim()))
                    throw new InvalidDataException("Save changes in the open C# editor before exporting this form.");
        DesignCodeGenerator.Export(Session.Document, path); _exportPath = path;
        if (openEditors != null)
            foreach (var item in openEditors) item.Editor.Text = File.ReadAllText(item.Page.ToolTipText.Trim());
        SetStatus("Exported " + Path.GetFileName(path) + " and " + Path.GetFileName(designerPath) + ". Save the layout to continue designing later.");
        return true;
    }
    private void Preview()
    {
        if (!CommitEditors()) return;
        using var preview = DesignCatalog.CreatePreview(Session.Document);
        preview.ShowDialog(this);
    }
    private void ViewCode()
    {
        if (!CommitEditors()) return;
        using var viewer = new Form { Text = Session.Document.Form.Name + " — Generated C#", ClientSize = new Size(900, 650), StartPosition = FormStartPosition.CenterParent, Icon = Icon };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        void Add(string name, string source)
        {
            var page = new TabPage(name);
            var editor = new TextEditorControl { Dock = DockStyle.Fill };
            editor.SetHighlighting("C#"); editor.Text = source; editor.Document.ReadOnly = true;
            page.Controls.Add(editor); tabs.TabPages.Add(page);
        }
        Add(Session.Document.Form.Name + ".Designer.cs", DesignCodeGenerator.GenerateDesigner(Session.Document));
        Add(Session.Document.Form.Name + ".cs", DesignCodeGenerator.GenerateCodeBehind(Session.Document, _exportPath != null && File.Exists(_exportPath) ? File.ReadAllText(_exportPath) : null));
        viewer.Controls.Add(tabs); viewer.ShowDialog(this);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S)) { Save(false); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.S)) { Save(true); return true; }
        if (keyData == (Keys.Control | Keys.O)) { Attempt(OpenLayout); return true; }
        if (keyData == Keys.F5) { Attempt(Preview); return true; }
        if (keyData == Keys.Escape && Canvas.ContainsFocus) { Canvas.ToolKind = null; Canvas.CancelDrag(); return true; }
        // Let text fields, the property grid and event cells handle their own editing shortcuts.
        if (Canvas.ContainsFocus || _outline.ContainsFocus)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Z: Session.Undo(); return true;
                case Keys.Control | Keys.Y: Session.Redo(); return true;
                case Keys.Control | Keys.C: Session.Copy(); return true;
                case Keys.Control | Keys.X: Session.Copy(); Session.Delete(); return true;
                case Keys.Control | Keys.V: Attempt(Session.Paste); return true;
                case Keys.Delete: Session.Delete(); return true;
            }
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down && (keyData & Keys.Control) == 0)
            {
                var node = Session.Selected;
                int step = Canvas.SnapToGrid && (keyData & Keys.Alt) == 0 ? 8 : 1;
                int dx = key == Keys.Left ? -step : key == Keys.Right ? step : 0;
                int dy = key == Keys.Up ? -step : key == Keys.Down ? step : 0;
                bool resize = (keyData & Keys.Shift) != 0;
                if (node.Dock == DockStyle.None)
                    Attempt(() => Session.Change(() =>
                    {
                        if (resize) node.Size = new Size(Math.Clamp(node.Width + dx, 16, 2400), Math.Clamp(node.Height + dy, 16, 1800));
                        else if (node != Session.Document.Form) node.Location = new Point(Math.Clamp(node.X + dx, 0, 2400), Math.Clamp(node.Y + dy, 0, 1800));
                    }));
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private void ApplyTheme()
    {
        bool dark = GlobalVariables.darkColor;
        Color panel = dark ? Color.FromArgb(37, 37, 38) : Color.FromArgb(246, 246, 248);
        Color field = dark ? Color.FromArgb(30, 30, 30) : Color.White;
        Color text = dark ? Color.FromArgb(225, 225, 228) : Color.FromArgb(35, 35, 40);
        void Apply(Control control)
        {
            control.BackColor = control is TextBox or TreeView or PropertyGrid ? field : panel;
            control.ForeColor = text;
            foreach (Control child in control.Controls) Apply(child);
        }
        Apply(this);
        Canvas.BackColor = dark ? Color.FromArgb(24, 24, 26) : Color.FromArgb(220, 223, 228);
        Canvas.Parent.BackColor = Canvas.BackColor;
        _documentTab.BackColor = dark ? Color.FromArgb(45, 45, 48) : Color.White;
        Properties.ViewBackColor = field; Properties.ViewForeColor = text; Properties.HelpBackColor = panel; Properties.HelpForeColor = text;
        Properties.CategoryForeColor = text; Properties.CategorySplitterColor = panel; Properties.LineColor = panel;
        Properties.CommandsBackColor = panel; Properties.CommandsForeColor = text;
        EventGrid.BackgroundColor = field; EventGrid.DefaultCellStyle.BackColor = field; EventGrid.DefaultCellStyle.ForeColor = text;
        EventGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204); EventGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        EventGrid.GridColor = panel; EventGrid.EnableHeadersVisualStyles = false;
        EventGrid.ColumnHeadersDefaultCellStyle.BackColor = panel; EventGrid.ColumnHeadersDefaultCellStyle.ForeColor = text;
        foreach (var strip in new[] { _commands, _layout })
        {
            strip.Renderer = new ToolStripProfessionalRenderer(new DesignerColorTable(panel));
            foreach (ToolStripItem item in strip.Items) item.ForeColor = text;
        }
        HandleCreated += (_, _) => FrmColorMod.SetDarkTitleBar(Handle, dark);
    }
    private sealed class DesignerColorTable(Color panel) : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => panel;
        public override Color ToolStripGradientMiddle => panel;
        public override Color ToolStripGradientEnd => panel;
        public override Color ButtonSelectedHighlight => Color.FromArgb(0, 122, 204);
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(0, 122, 204);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(0, 122, 204);
        public override Color ButtonCheckedGradientBegin => Color.FromArgb(0, 90, 155);
        public override Color ButtonCheckedGradientEnd => Color.FromArgb(0, 90, 155);
        public override Color ButtonCheckedGradientMiddle => Color.FromArgb(0, 90, 155);
    }
}
