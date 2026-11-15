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
internal sealed class FormDesignerControl : UserControl
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
    private readonly DesignerInspectorTabs _inspector = new() { Dock = DockStyle.Fill };
    internal readonly TextBox PropertySearch = new() { Dock = DockStyle.Top, PlaceholderText = "Search properties" };
    internal readonly TextBox EventSearch = new() { Dock = DockStyle.Top, PlaceholderText = "Search events or handlers" };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _coordinates = new();
    private readonly ToolStrip _commands = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 4, 6, 4) };
    private readonly ToolStrip _layout = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 2, 6, 2) };
    private ToolStripButton _undo, _redo;
    private bool _refreshing;
    internal FormDesignTab Host;

    internal FormDesignerControl(MainForm main)
    {
        _main = main;
        Font = new Font("Segoe UI", 9F);
        Name = "FormDesignerControl"; Size = new Size(1000, 680); Dock = DockStyle.Fill;
        AutoScaleMode = AutoScaleMode.Dpi;
        Canvas = new DesignCanvas(Session);
        BuildLayout(); BuildCommands(); PopulateToolbox(); ApplyTheme();
        Session.Changed += (_, _) => RefreshDocument();
        Session.SelectionChanged += (_, _) => RefreshSelection();
        Canvas.DefaultEventRequested += (_, _) => CreateEvent(DesignCatalog.DefaultEvent(Session.Selected.Kind));
        Canvas.OperationFailed += error => Attempt(() => throw error);
        Canvas.ToolPlaced += (_, _) => { _toolbox.SelectedNode = _toolbox.Nodes[0]; SetStatus("Control added. Drag to move; use the handles to resize."); };
        Properties.PropertyValueChanged += (_, _) => Attempt(Session.Commit);
        _search.TextChanged += (_, _) => PopulateToolbox();
        PropertySearch.TextChanged += (_, _) => Properties.SelectedObject = new DesignPropertyView(Session.Selected, PropertySearch.Text);
        EventSearch.TextChanged += (_, _) => RefreshSelection();
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
        EventGrid.EditingControlShowing += (_, e) =>
        {
            var theme = DesignerTheme.For(_main);
            e.Control.BackColor = theme.Field; e.Control.ForeColor = theme.Text;
        };
        RefreshDocument();
    }
    private static Label Heading(string text) => new() { Text = text, Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
    private void BuildLayout()
    {
        var workspace = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, Size = new Size(1280, 680), SplitterDistance = 170, Panel1MinSize = 110, Panel2MinSize = 320 };
        var inspectorSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, Size = new Size(1080, 680), SplitterDistance = 820, Panel1MinSize = 100, Panel2MinSize = 210 };
        workspace.Panel2.Controls.Add(inspectorSplit);
        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 39, Padding = new Padding(8) };
        searchPanel.Controls.Add(_search);
        workspace.Panel1.Controls.Add(_toolbox); workspace.Panel1.Controls.Add(searchPanel); workspace.Panel1.Controls.Add(Heading("TOOLBOX"));
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "DesignWorkspace" };
        scroll.Controls.Add(Canvas);
        inspectorSplit.Panel1.Controls.Add(scroll); inspectorSplit.Panel1.Controls.Add(_documentTab);
        var outlinePanel = new Panel { Dock = DockStyle.Top, Height = 170 };
        outlinePanel.Controls.Add(_outline); outlinePanel.Controls.Add(Heading("DOCUMENT OUTLINE"));
        var propertyPage = new TabPage("Properties"); propertyPage.Controls.Add(Properties); propertyPage.Controls.Add(SearchPanel(PropertySearch));
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
        eventPage.Controls.Add(EventGrid); eventPage.Controls.Add(SearchPanel(EventSearch)); eventPage.Controls.Add(eventFooter);
        _inspector.TabPages.Add(propertyPage); _inspector.TabPages.Add(eventPage);
        inspectorSplit.Panel2.Controls.Add(_inspector); inspectorSplit.Panel2.Controls.Add(_selection); inspectorSplit.Panel2.Controls.Add(outlinePanel);
        var status = new StatusStrip(); status.Items.Add(_status); status.Items.Add(_coordinates);
        Controls.Add(workspace); Controls.Add(_layout); Controls.Add(_commands); Controls.Add(status);
    }
    private static Panel SearchPanel(TextBox search)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6) };
        panel.Controls.Add(search); return panel;
    }
    private ToolStripButton Button(ToolStrip strip, string text, Action action, string tooltip = null)
    {
        var button = new ToolStripButton(text) { ToolTipText = tooltip ?? text, DisplayStyle = ToolStripItemDisplayStyle.Text, Margin = new Padding(2, 1, 2, 1) };
        button.Click += (_, _) => Attempt(() => { if (CommitEditors()) action(); }); strip.Items.Add(button); return button;
    }
    private void BuildCommands()
    {
        Button(_commands, "Save", () => Host?.Save(false), "Save form and designer source (Ctrl+S)");
        _commands.Items.Add(new ToolStripSeparator());
        _undo = Button(_commands, "Undo", Session.Undo, "Undo (Ctrl+Z)");
        _redo = Button(_commands, "Redo", Session.Redo, "Redo (Ctrl+Y)");
        Button(_commands, "Delete", Session.Delete);
        _commands.Items.Add(new ToolStripSeparator());
        Button(_commands, "▶ Preview", Preview, "Preview controls without running event-handler code (F5)");
        Button(_commands, "View Code", () => Host?.ShowCode());
        Button(_commands, "Designer Source", () => Host?.OpenRelated(".Designer.cs"));
        Button(_commands, "Resources", () => Host?.OpenRelated(".resx"));
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
        Properties.SelectedObject = new DesignPropertyView(selected, PropertySearch.Text);
        string activeEvent = EventGrid.CurrentRow?.Cells[0].Value as string;
        EventGrid.Rows.Clear();
        string defaultEvent = DesignCatalog.DefaultEvent(selected.Kind);
        foreach (var spec in DesignCatalog.EventsFor(selected.Kind).OrderBy(e => e.Name != defaultEvent).ThenBy(e => e.Name))
        {
            string filter = EventSearch.Text.Trim();
            if (filter.Length > 0 && !spec.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !selected.Events.GetValueOrDefault(spec.Name, "").Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
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
            SetStatus($"{eventName} → {handler}. Use Edit code to implement this handler in the Code view.");
        });
    }
    private void EditHandler()
    {
        Attempt(() =>
        {
            string eventName = CurrentEvent(); CreateEvent(eventName);
            Host?.EditHandler(Session.Selected.Events.GetValueOrDefault(eventName));
        });
    }
    internal void SetStatus(string text) => _status.Text = text;
    private void Attempt(Action action)
    {
        try { action(); }
        catch (Exception error) { SetStatus(error.Message); MessageBox.Show(this, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    internal bool CommitEditors()
    {
        if (!ValidateChildren()) return false;
        if (!EventGrid.EndEdit()) return false;
        try { Session.Commit(); return true; }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }
    private void Preview()
    {
        if (!CommitEditors()) return;
        using var preview = DesignCatalog.CreatePreview(Session.Document);
        preview.ShowDialog(this);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S)) { Host?.Save(false); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.S)) { Host?.Save(true); return true; }
        if (keyData == Keys.F5) { Attempt(Preview); return true; }
        if (keyData == Keys.Escape && Canvas.ContainsFocus) { Canvas.ToolKind = null; Canvas.CancelDrag(); return true; }
        // Let text fields, the property grid and event cells handle their own editing shortcuts.
        if (Canvas.ContainsFocus || _outline.ContainsFocus)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Z: Session.Undo(); return true;
                case Keys.Control | Keys.Y: Session.Redo(); return true;
                case Keys.Control | Keys.C: Attempt(Session.Copy); return true;
                case Keys.Control | Keys.X: Attempt(() => { Session.Copy(); Session.Delete(); }); return true;
                case Keys.Control | Keys.V: Attempt(Session.Paste); return true;
                case Keys.Delete: Attempt(Session.Delete); return true;
            }
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down && (keyData & Keys.Control) == 0)
            {
                var node = Session.Selected;
                int step = Canvas.SnapToGrid && (keyData & Keys.Alt) == 0 ? 8 : 1;
                int dx = key == Keys.Left ? -step : key == Keys.Right ? step : 0;
                int dy = key == Keys.Up ? -step : key == Keys.Down ? step : 0;
                bool resize = (keyData & Keys.Shift) != 0;
                if (node.Dock == DockStyle.None && !node.SourceMember.Contains('.'))
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
    internal void ApplyTheme()
    {
        var theme = DesignerTheme.For(_main);
        Color panel = theme.Panel, field = theme.Field, text = theme.Text;
        void Apply(Control control)
        {
            control.BackColor = control is TextBox or TreeView or PropertyGrid ? field : panel;
            control.ForeColor = text;
            if (control is ToolStrip strip) theme.ApplyStrip(strip);
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = theme.Border;
                button.FlatAppearance.MouseOverBackColor = field; button.UseVisualStyleBackColor = false;
            }
            foreach (Control child in control.Controls) Apply(child);
        }
        Apply(this);
        _inspector.Theme = theme; _inspector.Invalidate();
        Canvas.BackColor = field;
        Canvas.SelectionColor = theme.Accent; Canvas.Invalidate();
        Canvas.Parent.BackColor = Canvas.BackColor;
        _documentTab.BackColor = panel;
        Properties.ViewBackColor = field; Properties.ViewForeColor = text; Properties.HelpBackColor = panel; Properties.HelpForeColor = text;
        Properties.CategoryForeColor = text; Properties.CategorySplitterColor = panel; Properties.LineColor = panel;
        Properties.CommandsBackColor = panel; Properties.CommandsForeColor = text;
        Properties.ViewBorderColor = theme.Border; Properties.HelpBorderColor = theme.Border; Properties.CommandsBorderColor = theme.Border;
        Properties.SelectedItemWithFocusBackColor = theme.Selection; Properties.SelectedItemWithFocusForeColor = theme.SelectionText;
        Properties.DisabledItemForeColor = SystemColors.GrayText;
        EventGrid.BackgroundColor = field; EventGrid.DefaultCellStyle.BackColor = field; EventGrid.DefaultCellStyle.ForeColor = text;
        EventGrid.DefaultCellStyle.SelectionBackColor = theme.Selection; EventGrid.DefaultCellStyle.SelectionForeColor = theme.SelectionText;
        EventGrid.GridColor = theme.Border; EventGrid.EnableHeadersVisualStyles = false;
        EventGrid.ColumnHeadersDefaultCellStyle.BackColor = panel; EventGrid.ColumnHeadersDefaultCellStyle.ForeColor = text;
        EventGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = panel; EventGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = text;
        if (EventGrid.EditingControl != null) { EventGrid.EditingControl.BackColor = field; EventGrid.EditingControl.ForeColor = text; }
    }
}
