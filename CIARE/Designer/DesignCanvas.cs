using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignCanvas : Control
{
    internal const string ToolboxFormat = "CIARE.FormDesigner.Control";
    internal static readonly Point Origin = new(32, 64);
    private readonly DesignSession _session;
    private readonly Dictionary<string, Rectangle> _bounds = new();
    private Bitmap _render;
    private bool _dragging;
    private Point _start;
    private Rectangle _original, _ghost;
    private int _handle = -1;
    internal bool SnapToGrid = true;
    internal bool ShowGrid = true;
    internal string ToolKind;
    internal event EventHandler DefaultEventRequested;
    internal event EventHandler ToolPlaced;
    internal event Action<Exception> OperationFailed;

    internal DesignCanvas(DesignSession session)
    {
        _session = session;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = true; AllowDrop = true;
        _session.Changed += SessionChanged;
        _session.SelectionChanged += SelectionChanged;
        RefreshDesign();
    }
    private void SessionChanged(object sender, EventArgs e) => RefreshDesign();
    private void SelectionChanged(object sender, EventArgs e) => Invalidate();
    internal Rectangle NodeBounds(string id) => _bounds.TryGetValue(id, out var bounds) ? bounds : Rectangle.Empty;
    internal void RefreshDesign()
    {
        CancelDrag();
        _render?.Dispose(); _render = null; _bounds.Clear();
        var form = _session.Document.Form;
        Size = new Size(form.Width + 64, form.Height + 100);
        using var root = DesignCatalog.CreateControl(form, true);
        root.Dock = DockStyle.None; root.Location = Point.Empty;
        root.Paint += (_, e) =>
        {
            if (!ShowGrid) return;
            using var brush = new SolidBrush(Color.FromArgb(55, form.ForeColor));
            for (int x = 0; x < root.Width; x += 8)
                for (int y = 0; y < root.Height; y += 8) e.Graphics.FillRectangle(brush, x, y, 1, 1);
        };
        root.CreateControl(); root.PerformLayout();
        _render = new Bitmap(form.Width, form.Height);
        root.DrawToBitmap(_render, new Rectangle(Point.Empty, form.Size));
        void Collect(DesignNode node, Control control, Point offset)
        {
            _bounds[node.Id] = new Rectangle(offset, control.Size);
            foreach (var child in node.Children)
            {
                var view = control.Controls.Cast<Control>().First(c => c.Name == child.Name);
                Collect(child, view, new Point(offset.X + view.Left, offset.Y + view.Top));
            }
        }
        Collect(form, root, Origin);
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var form = _session.Document.Form;
        using var shadow = new SolidBrush(Color.FromArgb(45, Color.Black));
        g.FillRectangle(shadow, Origin.X + 5, Origin.Y - 29 + 5, form.Width, form.Height + 29);
        using var chrome = new SolidBrush(Color.FromArgb(246, 246, 248));
        g.FillRectangle(chrome, Origin.X, Origin.Y - 29, form.Width, 29);
        TextRenderer.DrawText(g, form.Text, Font, new Rectangle(Origin.X + 12, Origin.Y - 25, form.Width - 85, 24), Color.FromArgb(35, 35, 40), TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "—  □  ×", Font, new Rectangle(Origin.X + form.Width - 73, Origin.Y - 25, 68, 24), Color.DimGray, TextFormatFlags.VerticalCenter);
        if (_render != null) g.DrawImageUnscaled(_render, Origin);
        using var border = new Pen(Color.FromArgb(130, 130, 140));
        g.DrawRectangle(border, Origin.X - 1, Origin.Y - 30, form.Width + 1, form.Height + 30);
        var selection = NodeBounds(_session.Selected.Id);
        using var accent = new Pen(Color.FromArgb(0, 122, 204), 1);
        if (!selection.IsEmpty)
        {
            selection.Inflate(2, 2); g.DrawRectangle(accent, selection);
            using var fill = new SolidBrush(Color.White);
            foreach (var handle in Handles(NodeBounds(_session.Selected.Id))) { g.FillRectangle(fill, handle); g.DrawRectangle(accent, handle); }
        }
        if (_dragging)
        {
            using var ghost = new Pen(Color.FromArgb(0, 122, 204)) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(ghost, _ghost);
        }
        TextRenderer.DrawText(g, "Double-click a control to create its default event handler", Font,
            new Point(Origin.X, form.Height + Origin.Y + 16), ForeColor);
    }
    private static Rectangle[] Handles(Rectangle r)
    {
        var points = new[] { new Point(r.Left, r.Top), new Point(r.Left + r.Width / 2, r.Top), new Point(r.Right, r.Top),
            new Point(r.Right, r.Top + r.Height / 2), new Point(r.Right, r.Bottom), new Point(r.Left + r.Width / 2, r.Bottom),
            new Point(r.Left, r.Bottom), new Point(r.Left, r.Top + r.Height / 2) };
        return points.Select(p => new Rectangle(p.X - 3, p.Y - 3, 6, 6)).ToArray();
    }
    private DesignNode Hit(Point point, bool containersOnly = false)
    {
        DesignNode Visit(DesignNode node)
        {
            if (!NodeBounds(node.Id).Contains(point)) return null;
            foreach (var child in node.Children)
            {
                var result = Visit(child);
                if (result != null) return result;
            }
            return !containersOnly || node.IsContainer ? node : null;
        }
        return Visit(_session.Document.Form);
    }
    private int Snap(int value) => SnapToGrid && (ModifierKeys & Keys.Alt) == 0 ? (int)Math.Round(value / 8d) * 8 : value;
    internal void AddAt(string kind, Point point)
    {
        if (!DesignCatalog.Controls.Any(c => c.Kind == kind)) return;
        var parent = Hit(point, true);
        if (parent == null) return;
        var bounds = NodeBounds(parent.Id);
        _session.Add(kind, parent, new Point(Math.Max(0, Snap(point.X - bounds.X)), Math.Max(0, Snap(point.Y - bounds.Y))));
        ToolKind = null; ToolPlaced?.Invoke(this, EventArgs.Empty); Focus();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); Focus();
        if (e.Button != MouseButtons.Left) return;
        if (ToolKind != null) { TryChange(() => AddAt(ToolKind, e.Location)); return; }
        _handle = Array.FindIndex(Handles(NodeBounds(_session.Selected.Id)), r => r.Contains(e.Location));
        if (_handle < 0)
        {
            var hit = Hit(e.Location) ?? _session.Document.Form;
            _session.Select(hit.Id);
        }
        if (_session.Selected == _session.Document.Form && _handle is not (3 or 4 or 5)) return;
        if (_session.Selected.Dock != DockStyle.None && _session.Selected != _session.Document.Form) return;
        _original = NodeBounds(_session.Selected.Id); _ghost = _original; _start = e.Location;
        _dragging = true; Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            int handle = Array.FindIndex(Handles(NodeBounds(_session.Selected.Id)), r => r.Contains(e.Location));
            Cursor = ToolKind != null ? Cursors.Cross : handle switch { 0 or 4 => Cursors.SizeNWSE, 2 or 6 => Cursors.SizeNESW, 1 or 5 => Cursors.SizeNS, 3 or 7 => Cursors.SizeWE, _ => Cursors.Default };
            return;
        }
        int dx = e.X - _start.X, dy = e.Y - _start.Y;
        int left = _original.Left, top = _original.Top, right = _original.Right, bottom = _original.Bottom;
        var parent = _session.Document.ParentOf(_session.Selected);
        var parentBounds = parent == null ? new Rectangle(Origin, _session.Document.Form.Size) : NodeBounds(parent.Id);
        if (_handle < 0)
        {
            left = parentBounds.X + Math.Clamp(Snap(left + dx - parentBounds.X), 0, Math.Max(0, parentBounds.Width - _original.Width));
            top = parentBounds.Y + Math.Clamp(Snap(top + dy - parentBounds.Y), 0, Math.Max(0, parentBounds.Height - _original.Height));
            _ghost = new Rectangle(left, top, _original.Width, _original.Height);
        }
        else
        {
            if (_handle is 0 or 6 or 7) left = Math.Clamp(parentBounds.X + Snap(left + dx - parentBounds.X), parentBounds.X, right - 16);
            if (_handle is 0 or 1 or 2) top = Math.Clamp(parentBounds.Y + Snap(top + dy - parentBounds.Y), parentBounds.Y, bottom - 16);
            if (_handle is 2 or 3 or 4) right = left + Math.Clamp(Snap(_original.Right + dx - left), 16, 2400);
            if (_handle is 4 or 5 or 6) bottom = top + Math.Clamp(Snap(_original.Bottom + dy - top), 16, 1800);
            _ghost = Rectangle.FromLTRB(left, top, right, bottom);
        }
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging) return;
        var bounds = _ghost;
        CancelDrag();
        var node = _session.Selected;
        var parent = _session.Document.ParentOf(node);
        var offset = parent == null ? Origin : NodeBounds(parent.Id).Location;
        bounds.Offset(-offset.X, -offset.Y);
        TryChange(() => _session.Change(() => node.Bounds = bounds));
    }
    internal void CancelDrag() { _dragging = false; Capture = false; Invalidate(); }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) { _dragging = false; Invalidate(); } }
    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e); CancelDrag();
        if (e.Button == MouseButtons.Left) DefaultEventRequested?.Invoke(this, EventArgs.Empty);
    }
    protected override void OnDragEnter(DragEventArgs e) { base.OnDragEnter(e); e.Effect = e.Data.GetDataPresent(ToolboxFormat) ? DragDropEffects.Copy : DragDropEffects.None; }
    protected override void OnDragOver(DragEventArgs e) { base.OnDragOver(e); e.Effect = Hit(PointToClient(new Point(e.X, e.Y)), true) != null && e.Data.GetDataPresent(ToolboxFormat) ? DragDropEffects.Copy : DragDropEffects.None; }
    protected override void OnDragDrop(DragEventArgs e) { base.OnDragDrop(e); TryChange(() => AddAt(e.Data.GetData(ToolboxFormat) as string, PointToClient(new Point(e.X, e.Y)))); }
    private void TryChange(Action action)
    {
        try { action(); }
        catch (Exception error) { OperationFailed?.Invoke(error); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _session.Changed -= SessionChanged; _session.SelectionChanged -= SelectionChanged; _render?.Dispose(); }
        base.Dispose(disposing);
    }
}
