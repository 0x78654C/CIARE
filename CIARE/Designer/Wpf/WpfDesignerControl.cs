using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfDesignerControl : UserControl
{
    internal readonly WpfSession Session;
    internal readonly WpfCanvas Canvas;
    internal readonly PropertyGrid Properties = new() { Dock = DockStyle.Fill, HelpVisible = true, PropertySort = PropertySort.Categorized };
    internal new readonly DataGridView Events = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None, EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2 };
    private readonly MainForm _main;
    private readonly TreeView _toolbox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ItemHeight = 27, HideSelection = false };
    private readonly TreeView _outline = new() { Dock = DockStyle.Top, Height = 170, HideSelection = false, BorderStyle = BorderStyle.None };
    private readonly TextBox _toolSearch = new() { Dock = DockStyle.Top, PlaceholderText = "Search Toolbox" };
    internal readonly TextBox PropertySearch = new() { Dock = DockStyle.Top, PlaceholderText = "Search properties" };
    internal readonly TextBox EventSearch = new() { Dock = DockStyle.Top, PlaceholderText = "Search events or handlers" };
    private readonly DesignerInspectorTabs _inspector = new() { Dock = DockStyle.Fill };
    private readonly ToolStrip _commands = new() { GripStyle = ToolStripGripStyle.Hidden };
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _message = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripButton _undo, _redo;
    private bool _refreshing;
    internal WpfDesignTab Host;
    internal WpfDesignerControl(MainForm main, string source)
    {
        _main = main; Session = new WpfSession(source); Canvas = new WpfCanvas(Session);
        Dock = DockStyle.Fill; Size = new Size(1200, 700); Font = new Font("Segoe UI", 9); Name = "WpfDesignerControl";
        var workspace = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1200, 700), SplitterDistance = 180, Panel1MinSize = 110, Panel2MinSize = 350, FixedPanel = FixedPanel.Panel1 };
        var inspector = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1000, 700), SplitterDistance = 730, Panel1MinSize = 100, Panel2MinSize = 220, FixedPanel = FixedPanel.Panel2 };
        workspace.Panel1.Controls.Add(_toolbox); workspace.Panel1.Controls.Add(Search(_toolSearch)); workspace.Panel1.Controls.Add(Heading("WPF TOOLBOX"));
        workspace.Panel2.Controls.Add(inspector);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true }; scroll.Controls.Add(Canvas);
        inspector.Panel1.Controls.Add(scroll);
        var propertyPage = new TabPage("Properties"); propertyPage.Controls.Add(Properties); propertyPage.Controls.Add(Search(PropertySearch));
        var eventPage = new TabPage("⚡ Events");
        Events.Columns.Add(new DataGridViewTextBoxColumn { Name = "Event", HeaderText = "Event", ReadOnly = true, FillWeight = 45 });
        Events.Columns.Add(new DataGridViewTextBoxColumn { Name = "Handler", HeaderText = "Event handler", FillWeight = 55 });
        Events.RowTemplate.Height = 26;
        eventPage.Controls.Add(Events); eventPage.Controls.Add(Search(EventSearch));
        eventPage.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 44, Text = "Double-click to create or open a handler.\r\nClear its name to disconnect." });
        _inspector.TabPages.Add(propertyPage); _inspector.TabPages.Add(eventPage);
        inspector.Panel2.Controls.Add(_inspector); inspector.Panel2.Controls.Add(_outline); inspector.Panel2.Controls.Add(Heading("DOCUMENT OUTLINE"));
        Command("Save", () => Host?.Save(false)); _undo = Command("Undo", Session.Undo); _redo = Command("Redo", Session.Redo);
        Command("Copy", Session.Copy); Command("Paste", Session.Paste); Command("Delete", Session.Delete);
        _commands.Items.Add(new ToolStripSeparator());
        Command("▶ Preview", Preview); Command("View XAML", () => Host?.ShowXaml()); Command("View Code", () => Host?.ShowCode());
        var snap = Command("Snap to Grid", () => { }); snap.CheckOnClick = true; snap.Checked = true; snap.CheckedChanged += (_, _) => Canvas.SnapToGrid = snap.Checked;
        _status.Items.Add(_message); Controls.Add(workspace); Controls.Add(_commands); Controls.Add(_status);
        _toolSearch.TextChanged += (_, _) => PopulateToolbox(); PropertySearch.TextChanged += (_, _) => RefreshSelection(); EventSearch.TextChanged += (_, _) => RefreshSelection();
        _toolbox.AfterSelect += (_, e) => Canvas.ToolKind = e.Node.Tag as string;
        _toolbox.ItemDrag += (_, e) => { if (e.Item is TreeNode { Tag: string kind }) _toolbox.DoDragDrop(new DataObject(WpfCanvas.ToolboxFormat, kind), DragDropEffects.Copy); };
        _toolbox.NodeMouseDoubleClick += (_, e) =>
        {
            if (e.Node.Tag is not string kind) return;
            Attempt(() => { Session.Add(kind, WpfCatalog.IsContainer(Session.Selected) ? Session.Selected : WpfDocument.VisualParent(Session.Selected)); Canvas.ToolKind = null; _toolbox.SelectedNode = _toolbox.Nodes[0]; });
        };
        _outline.AfterSelect += (_, e) => { if (!_refreshing && e.Node.Tag is XElement element) Session.Select(element); };
        Session.Changed += (_, _) => RefreshDocument(); Session.SelectionChanged += (_, _) => RefreshSelection();
        Canvas.OperationFailed += error => SetStatus(error.Message);
        Canvas.ToolPlaced += (_, _) => _toolbox.SelectedNode = _toolbox.Nodes[0];
        Canvas.DefaultEventRequested += (_, _) => CreateHandler(WpfCatalog.DefaultEvent(Session.Selected));
        Properties.PropertyValueChanged += (_, _) => RefreshSelection();
        Events.CellEndEdit += (_, e) =>
        {
            if (_refreshing || e.RowIndex < 0) return;
            string name = (string)Events.Rows[e.RowIndex].Cells[0].Value;
            string handler = Convert.ToString(Events.Rows[e.RowIndex].Cells[1].Value)?.Trim() ?? "";
            Attempt(() => Host?.BindEvent(name, handler, false)); RefreshSelection();
        };
        Events.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) CreateHandler((string)Events.Rows[e.RowIndex].Cells[0].Value); };
        Events.EditingControlShowing += (_, e) => { var theme = DesignerTheme.For(_main); e.Control.BackColor = theme.Field; e.Control.ForeColor = theme.Text; };
        PopulateToolbox(); RefreshDocument(); ApplyTheme();
    }
    private static Label Heading(string text) => new() { Dock = DockStyle.Top, Height = 30, Text = text, Padding = new Padding(8, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
    private static Panel Search(TextBox box) { var panel = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6) }; panel.Controls.Add(box); return panel; }
    private ToolStripButton Command(string text, Action action)
    {
        var button = new ToolStripButton(text); button.Click += (_, _) => Attempt(() => { if (CommitEditors()) action(); }); _commands.Items.Add(button); return button;
    }
    internal bool CommitEditors() { if (!ValidateChildren()) return false; return Events.EndEdit(); }
    private void Attempt(Action action) { try { action(); } catch (Exception error) { SetStatus(error.Message); } }
    internal void SetStatus(string text) { _message.Text = text; _message.ToolTipText = text; }
    private void PopulateToolbox()
    {
        _toolbox.BeginUpdate(); _toolbox.Nodes.Clear(); _toolbox.Nodes.Add("↖ Pointer");
        foreach (var group in WpfCatalog.Controls.Where(c => c.Kind.Contains(_toolSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)).GroupBy(c => c.Category))
        { var category = _toolbox.Nodes.Add(group.Key); foreach (var item in group) category.Nodes.Add(new TreeNode(item.Kind) { Tag = item.Kind }); category.Expand(); }
        _toolbox.EndUpdate(); Canvas.ToolKind = null;
    }
    private void RefreshDocument()
    {
        _refreshing = true; _outline.BeginUpdate(); _outline.Nodes.Clear();
        TreeNode Node(XElement e) { var n = new TreeNode((WpfDocument.Name(e) is { Length: > 0 } name ? name + " : " : "") + e.Name.LocalName) { Tag = e }; foreach (var c in WpfDocument.Children(e)) n.Nodes.Add(Node(c)); return n; }
        _outline.Nodes.Add(Node(Session.Document.Root)); _outline.ExpandAll(); _outline.EndUpdate(); _refreshing = false;
        _undo.Enabled = Session.CanUndo; _redo.Enabled = Session.CanRedo;
        SetStatus(Canvas.Renderer.Approximations == 0 ? "Drag controls from the Toolbox. Ctrl+S saves XAML. Double-click a control to edit its event." :
            "Bindings, resources, images, and custom markup are preserved. Some values use simplified previews.");
        RefreshSelection();
    }
    private void RefreshSelection()
    {
        _refreshing = true;
        TreeNode Find(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes) { if (node.Tag == Session.Selected) return node; var nested = Find(node.Nodes); if (nested != null) return nested; }
            return null;
        }
        _outline.SelectedNode = Find(_outline.Nodes);
        Properties.SelectedObject = new WpfPropertyView(Session, PropertySearch.Text);
        Events.Rows.Clear(); string filter = EventSearch.Text.Trim();
        foreach (var evt in WpfCatalog.Events(Session.Selected))
        {
            string handler = (string)Session.Selected.Attribute(evt.Name) ?? "";
            if (filter.Length == 0 || evt.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || handler.Contains(filter, StringComparison.OrdinalIgnoreCase)) Events.Rows.Add(evt.Name, handler);
        }
        _refreshing = false;
    }
    internal void CreateHandler(string name) => Attempt(() =>
    {
        string handler = (string)Session.Selected.Attribute(name);
        if (string.IsNullOrEmpty(handler)) handler = (WpfDocument.Name(Session.Selected) is { Length: > 0 } controlName ? controlName : Session.Selected.Name.LocalName) + "_" + name;
        Host?.BindEvent(name, handler, true);
    });
    private void Preview()
    {
        using var preview = new Form { Text = "WPF Preview", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(820, 500) };
        using var host = new System.Windows.Forms.Integration.ElementHost { Dock = DockStyle.Fill };
        host.Child = new WpfRenderer().Build(Session.Document, designMode: false); preview.Controls.Add(host); preview.ShowDialog(this); host.Child = null;
    }
    internal void ApplyTheme()
    {
        var theme = DesignerTheme.For(_main);
        void Apply(Control control)
        {
            control.ForeColor = theme.Text; control.BackColor = control is TextBox or TreeView or PropertyGrid ? theme.Field : theme.Panel;
            foreach (Control child in control.Controls) Apply(child);
        }
        Apply(this); theme.ApplyStrip(_commands); theme.ApplyStrip(_status);
        Properties.ViewBackColor = theme.Field; Properties.ViewForeColor = theme.Text; Properties.HelpBackColor = theme.Panel; Properties.HelpForeColor = theme.Text;
        Properties.CategoryForeColor = theme.Text; Properties.CategorySplitterColor = theme.Border; Properties.LineColor = theme.Border; Properties.CommandsBackColor = theme.Panel; Properties.CommandsForeColor = theme.Text;
        Events.EnableHeadersVisualStyles = false; Events.BackgroundColor = theme.Field; Events.GridColor = theme.Border;
        Events.DefaultCellStyle.BackColor = theme.Field; Events.DefaultCellStyle.ForeColor = theme.Text;
        Events.DefaultCellStyle.SelectionBackColor = theme.Selection; Events.DefaultCellStyle.SelectionForeColor = theme.SelectionText;
        Events.ColumnHeadersDefaultCellStyle.BackColor = theme.Panel; Events.ColumnHeadersDefaultCellStyle.ForeColor = theme.Text;
        _inspector.Theme = theme; _inspector.Invalidate(); Canvas.Accent = theme.Accent; Canvas.Invalidate();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S)) { Host?.Save(false); return true; }
        if (keyData == Keys.F7) { Host?.TryShowCode(); return true; }
        if (keyData == Keys.F5) { Attempt(Preview); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
