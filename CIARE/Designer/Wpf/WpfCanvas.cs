using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfCanvas : Control
{
    internal const string ToolboxFormat = "CIARE.WpfDesigner.Control";
    internal static readonly Point Origin = new(32, 48);
    internal readonly WpfRenderer Renderer = new();
    private readonly WpfSession _session;
    private Bitmap _bitmap;
    private bool _pending, _dragging;
    private Point _start;
    private Rectangle _original, _ghost;
    private int _handle = -1;
    internal string ToolKind;
    internal bool SnapToGrid = true;
    internal Color Accent = Color.DodgerBlue;
    internal event Action<Exception> OperationFailed;
    internal event EventHandler DefaultEventRequested, ToolPlaced;
    internal WpfCanvas(WpfSession session)
    {
        _session = session; AllowDrop = true; TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        session.Changed += Refresh; session.SelectionChanged += Refresh; RefreshDesign();
    }
    private void Refresh(object sender, EventArgs e) => RefreshDesign();
    internal Rectangle NodeBounds(XElement element) => Renderer.Bounds.TryGetValue(element, out var bounds)
        ? new Rectangle(bounds.Left + Origin.X, bounds.Top + Origin.Y, bounds.Width, bounds.Height) : Rectangle.Empty;
    internal void RefreshDesign()
    {
        CancelDrag(); _bitmap?.Dispose(); _bitmap = Renderer.Render(_session.Document, _session.Selected);
        Size = new Size(_bitmap.Width + 64, _bitmap.Height + 96); Invalidate();
    }
    private static Rectangle[] Handles(Rectangle r) => new[]
    {
        new Point(r.Left, r.Top), new Point(r.Left + r.Width / 2, r.Top), new Point(r.Right, r.Top), new Point(r.Right, r.Top + r.Height / 2),
        new Point(r.Right, r.Bottom), new Point(r.Left + r.Width / 2, r.Bottom), new Point(r.Left, r.Bottom), new Point(r.Left, r.Top + r.Height / 2)
    }.Select(p => new Rectangle(p.X - 3, p.Y - 3, 6, 6)).ToArray();
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_bitmap == null) return;
        e.Graphics.DrawImageUnscaled(_bitmap, Origin);
        TextRenderer.DrawText(e.Graphics, (string)_session.Document.Root.Attribute("Title") ?? _session.Document.Root.Name.LocalName,
            Font, new Point(Origin.X, 20), ForeColor);
        var bounds = NodeBounds(_session.Selected);
        if (!bounds.IsEmpty)
        {
            using var pen = new Pen(Accent); var outline = bounds; outline.Inflate(2, 2); e.Graphics.DrawRectangle(pen, outline);
            foreach (var handle in Handles(bounds)) { e.Graphics.FillRectangle(Brushes.White, handle); e.Graphics.DrawRectangle(pen, handle); }
        }
        if (_dragging) { using var pen = new Pen(Accent) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }; e.Graphics.DrawRectangle(pen, _ghost); }
    }
    private XElement Hit(Point p, bool containers = false) => _session.Document.Elements.Reverse()
        .FirstOrDefault(e => NodeBounds(e).Contains(p) && (!containers || WpfCatalog.IsContainer(e)));
    internal void AddAt(string kind, Point p)
    {
        var parent = Hit(p, true) ?? _session.Document.Root;
        var bounds = NodeBounds(parent);
        _session.Add(kind, parent, Math.Max(0, Snap(p.X - bounds.Left)), Math.Max(0, Snap(p.Y - bounds.Top)));
        ToolKind = null; ToolPlaced?.Invoke(this, EventArgs.Empty);
    }
    private int Snap(int value) => SnapToGrid && (ModifierKeys & Keys.Alt) == 0 ? (int)Math.Round(value / 8d) * 8 : value;
    private void Attempt(Action action) { try { action(); } catch (Exception error) { OperationFailed?.Invoke(error); } }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); Focus(); if (e.Button != MouseButtons.Left) return;
        CancelDrag();
        if (ToolKind != null) { Attempt(() => AddAt(ToolKind, e.Location)); return; }
        _handle = Array.FindIndex(Handles(NodeBounds(_session.Selected)), h => h.Contains(e.Location));
        if (_handle < 0) _session.Select(Hit(e.Location) ?? _session.Document.Root);
        if (_session.Selected == _session.Document.Root && _handle is not (3 or 4 or 5)) return;
        _original = NodeBounds(_session.Selected); _ghost = _original; _start = e.Location;
        if (_original.IsEmpty) return;
        _pending = true; Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_pending || (e.Button & MouseButtons.Left) == 0) return;
        if (!_dragging && new Rectangle(_start.X - SystemInformation.DragSize.Width / 2, _start.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width, SystemInformation.DragSize.Height).Contains(e.Location)) return;
        _dragging = true;
        int dx = Snap(e.X - _start.X), dy = Snap(e.Y - _start.Y);
        int left = _original.Left, top = _original.Top, right = _original.Right, bottom = _original.Bottom;
        if (_handle < 0) { left += dx; right += dx; top += dy; bottom += dy; }
        else
        {
            if (_handle is 0 or 6 or 7) left = Math.Min(right - 8, left + dx);
            if (_handle is 2 or 3 or 4) right = Math.Max(left + 8, right + dx);
            if (_handle is 0 or 1 or 2) top = Math.Min(bottom - 8, top + dy);
            if (_handle is 4 or 5 or 6) bottom = Math.Max(top + 8, bottom + dy);
        }
        _ghost = Rectangle.FromLTRB(left, top, right, bottom); Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); if (e.Button != MouseButtons.Left) return;
        if (_dragging)
        {
            var bounds = _ghost; var before = _original; int handle = _handle;
            Attempt(() => _session.Change(() =>
            {
                int dx = bounds.Left - before.Left, dy = bounds.Top - before.Top;
                if (_session.Selected != _session.Document.Root && (dx != 0 || dy != 0)) MoveSelection(dx, dy);
                if (handle >= 0)
                {
                    WpfDocument.Set(_session.Selected, "Width", WpfCatalog.Format(bounds.Width));
                    WpfDocument.Set(_session.Selected, "Height", WpfCatalog.Format(bounds.Height));
                }
            }));
        }
        CancelDrag();
    }
    private void MoveSelection(int dx, int dy)
    {
        var element = _session.Selected;
        var parent = WpfDocument.VisualParent(element);
        if (parent?.Name.LocalName == "Canvas")
        {
            if (new[] { "Canvas.Left", "Canvas.Top", "Canvas.Right", "Canvas.Bottom" }.Any(p => ((string)element.Attribute(p))?.StartsWith('{') == true))
                throw new InvalidOperationException("Edit the bound Canvas position in XAML before moving this control.");
            var bounds = NodeBounds(element); var parentBounds = NodeBounds(parent);
            element.SetAttributeValue("Canvas.Left", WpfCatalog.Format(WpfDocument.Number(element, "Canvas.Left", bounds.Left - parentBounds.Left) + dx));
            element.SetAttributeValue("Canvas.Top", WpfCatalog.Format(WpfDocument.Number(element, "Canvas.Top", bounds.Top - parentBounds.Top) + dy));
        }
        else
        {
            string marginText = (string)element.Attribute("Margin") ?? "0";
            if (marginText.StartsWith('{')) throw new InvalidOperationException("Edit the bound Margin in XAML before moving this control.");
            var margin = (System.Windows.Thickness)WpfCatalog.ConvertValue(typeof(System.Windows.Thickness), marginText);
            WpfDocument.Set(element, "Margin", string.Join(",", new[] { margin.Left + dx, margin.Top + dy, margin.Right, margin.Bottom }.Select(n => WpfCatalog.Format(n))));
        }
    }
    private void CancelDrag() { _pending = _dragging = false; _handle = -1; Capture = false; Invalidate(); }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) { _pending = _dragging = false; Invalidate(); } }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { base.OnMouseDoubleClick(e); CancelDrag(); DefaultEventRequested?.Invoke(this, EventArgs.Empty); }
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) { CancelDrag(); ToolKind = null; return; }
        Attempt(() =>
        {
            if (e.Control && e.KeyCode == Keys.Z) _session.Undo();
            else if (e.Control && e.KeyCode == Keys.Y) _session.Redo();
            else if (e.Control && e.KeyCode == Keys.C) _session.Copy();
            else if (e.Control && e.KeyCode == Keys.V) _session.Paste();
            else if (e.KeyCode == Keys.Delete) _session.Delete();
            else if (_session.Selected != _session.Document.Root && e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
            {
                int step = SnapToGrid && !e.Alt ? 8 : 1;
                int dx = e.KeyCode == Keys.Left ? -step : e.KeyCode == Keys.Right ? step : 0;
                int dy = e.KeyCode == Keys.Up ? -step : e.KeyCode == Keys.Down ? step : 0;
                _session.Change(() =>
                {
                    if (!e.Shift) MoveSelection(dx, dy);
                    else { var bounds = NodeBounds(_session.Selected); WpfDocument.Set(_session.Selected, "Width", WpfCatalog.Format(Math.Max(8, bounds.Width + dx))); WpfDocument.Set(_session.Selected, "Height", WpfCatalog.Format(Math.Max(8, bounds.Height + dy))); }
                });
            }
            else return;
            e.Handled = true; e.SuppressKeyPress = true;
        });
    }
    protected override void OnDragEnter(DragEventArgs e) { base.OnDragEnter(e); e.Effect = e.Data.GetDataPresent(ToolboxFormat) ? DragDropEffects.Copy : DragDropEffects.None; }
    protected override void OnDragDrop(DragEventArgs e) { base.OnDragDrop(e); Attempt(() => AddAt(e.Data.GetData(ToolboxFormat) as string, PointToClient(new Point(e.X, e.Y)))); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _session.Changed -= Refresh; _session.SelectionChanged -= Refresh; _bitmap?.Dispose(); }
        base.Dispose(disposing);
    }
}
