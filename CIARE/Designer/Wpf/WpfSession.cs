using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfSession
{
    private sealed record Snapshot(string Source, string Selection);
    private readonly Stack<Snapshot> _undo = new(), _redo = new();
    private string _saved, _clipboard;
    internal WpfDocument Document { get; private set; }
    internal XElement Selected { get; private set; }
    internal bool IsDirty => Document.Serialize() != _saved;
    internal bool CanUndo => _undo.Count > 0;
    internal bool CanRedo => _redo.Count > 0;
    internal event EventHandler Changed, SelectionChanged;
    internal WpfSession(string source) => Reset(source);
    internal void Reset(string source)
    {
        Document = new WpfDocument(source); Selected = Document.Root; _saved = source; _undo.Clear(); _redo.Clear(); Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void Select(XElement e) { Selected = e ?? Document.Root; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    private Snapshot Current() => new(Document.Serialize(), WpfDocument.PathOf(Selected));
    private void Restore(Snapshot state) { Document = new WpfDocument(state.Source); Selected = Document.Find(state.Selection) ?? Document.Root; Changed?.Invoke(this, EventArgs.Empty); }
    internal void Change(Action action)
    {
        var before = Current();
        try { action(); Document.Validate(); }
        catch { Restore(before); throw; }
        if (Document.Serialize() != before.Source) { _undo.Push(before); _redo.Clear(); Changed?.Invoke(this, EventArgs.Empty); }
    }
    internal void Set(string property, string value) => Change(() => WpfDocument.Set(Selected, property, value));
    internal void Add(string kind, XElement parent, double x = 24, double y = 24) => Change(() => Selected = Document.Add(kind, parent, x, y));
    internal void Undo() { if (!CanUndo) return; _redo.Push(Current()); Restore(_undo.Pop()); }
    internal void Redo() { if (!CanRedo) return; _undo.Push(Current()); Restore(_redo.Pop()); }
    internal void MarkSaved() => _saved = Document.Serialize();
    internal void Delete() { if (Selected == Document.Root) return; Change(() => { var parent = WpfDocument.VisualParent(Selected); Selected.Remove(); Selected = Document.Elements.FirstOrDefault(e => e == parent) ?? Document.Root; }); }
    internal void Copy() { if (Selected != Document.Root) _clipboard = Selected.ToString(); }
    internal void Paste()
    {
        if (_clipboard == null) return;
        Change(() =>
        {
            var copy = XElement.Parse(_clipboard);
            var parent = WpfDocument.InsertionParent(WpfCatalog.IsContainer(Selected) ? Selected : WpfDocument.VisualParent(Selected), copy.Name.LocalName);
            WpfDocument.ContentTarget(parent).Add(copy);
            foreach (var e in copy.DescendantsAndSelf().Where(e => !e.Name.LocalName.Contains('.')))
            {
                if (WpfDocument.Name(e).Length > 0) { e.SetAttributeValue("Name", null); e.SetAttributeValue(WpfDocument.Xaml + "Name", Document.UniqueName(e.Name.LocalName)); }
                foreach (var evt in WpfCatalog.Events(e)) e.SetAttributeValue(evt.Name, null);
            }
            Selected = copy;
        });
    }
}
