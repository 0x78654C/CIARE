using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignSession
{
    internal DesignDocument Document { get; private set; } = new();
    internal string SelectedId { get; private set; }
    internal DesignNode Selected => Document.Find(SelectedId) ?? Document.Form;
    private readonly List<string> _undo = new();
    private readonly List<string> _redo = new();
    private string _committed;
    private string _saved;
    private string _clipboard;
    internal event EventHandler Changed;
    internal event EventHandler SelectionChanged;
    internal bool IsDirty => _committed != _saved;
    internal bool CanUndo => _undo.Count > 0;
    internal bool CanRedo => _redo.Count > 0;
    internal bool CanPaste => _clipboard != null;
    internal DesignSession() { Reset(Document); }
    internal void Reset(DesignDocument document)
    {
        document.Validate();
        Document = document; SelectedId = document.Form.Id;
        _undo.Clear(); _redo.Clear(); _committed = _saved = document.Serialize();
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void MarkSaved() { _saved = _committed; Changed?.Invoke(this, EventArgs.Empty); }
    internal void Select(string id)
    {
        SelectedId = Document.Find(id)?.Id ?? Document.Form.Id;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    internal void Change(Action action)
    {
        try { action(); Commit(); }
        catch { Document = DesignDocument.Parse(_committed); Changed?.Invoke(this, EventArgs.Empty); throw; }
    }
    internal void Commit()
    {
        try { Document.Validate(); }
        catch { Document = DesignDocument.Parse(_committed); Changed?.Invoke(this, EventArgs.Empty); throw; }
        string next = Document.Serialize();
        if (next == _committed) return;
        _undo.Add(_committed);
        if (_undo.Count > 100) _undo.RemoveAt(0);
        _redo.Clear(); _committed = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void Undo() => Restore(_undo, _redo);
    internal void Redo() => Restore(_redo, _undo);
    private void Restore(List<string> source, List<string> destination)
    {
        if (source.Count == 0) return;
        destination.Add(_committed);
        _committed = source[^1]; source.RemoveAt(source.Count - 1);
        Document = DesignDocument.Parse(_committed);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal DesignNode Add(string kind, DesignNode parent, Point location)
    {
        var node = DesignCatalog.Create(kind, Document.UniqueName(kind), location);
        Change(() => { node.TabIndex = parent.Children.Count; parent.Children.Insert(0, node); SelectedId = node.Id; });
        return node;
    }
    internal void Delete()
    {
        var selected = Selected;
        if (selected == Document.Form) return;
        var parent = Document.ParentOf(selected);
        Change(() => { parent.Children.Remove(selected); SelectedId = parent.Id; });
    }
    internal void Copy()
    {
        if (Selected == Document.Form) return;
        _clipboard = JsonSerializer.Serialize(Selected, DesignDocument.JsonOptions);
    }
    internal void Paste()
    {
        if (_clipboard == null) return;
        var parent = Selected.IsContainer ? Selected : Document.ParentOf(Selected);
        Change(() =>
        {
            var clone = JsonSerializer.Deserialize<DesignNode>(_clipboard, DesignDocument.JsonOptions);
            clone.Location = new Point(Math.Min(clone.X + 16, Math.Max(0, parent.Width - clone.Width)), Math.Min(clone.Y + 16, Math.Max(0, parent.Height - clone.Height)));
            // Add first so each subsequent generated name sees earlier descendants' new names.
            parent.Children.Insert(0, clone);
            foreach (var node in DesignDocument.Enumerate(clone))
            {
                node.Id = Guid.NewGuid().ToString("N");
                node.Name = Document.UniqueName(node.Kind);
                node.Events.Clear();
            }
            SelectedId = clone.Id;
        });
    }
    internal void Reorder(bool front)
    {
        var selected = Selected;
        if (selected == Document.Form) return;
        var parent = Document.ParentOf(selected);
        Change(() => { parent.Children.Remove(selected); parent.Children.Insert(front ? 0 : parent.Children.Count, selected); });
    }
    internal void BindEvent(string eventName, string handler)
    {
        Change(() =>
        {
            if (string.IsNullOrWhiteSpace(handler)) Selected.Events.Remove(eventName);
            else Selected.Events[eventName] = handler.Trim();
        });
    }
    internal string CreateHandler(string eventName)
    {
        if (Selected.Events.TryGetValue(eventName, out var existing)) return existing;
        string name = Selected.Name + "_" + eventName;
        string candidate = name;
        int index = 1;
        while (Document.AllNodes.Any(n => n.Name == candidate || n.Events.Values.Contains(candidate))) candidate = name + index++;
        BindEvent(eventName, candidate);
        return candidate;
    }
}
