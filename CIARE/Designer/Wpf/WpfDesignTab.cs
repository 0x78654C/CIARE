using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfDesignTab : IDisposable
{
    private readonly MainForm _main;
    internal readonly TabPage Page;
    internal readonly TextEditorControl SourceEditor;
    internal TabPage XamlPage { get; private set; }
    private TextEditorControl _xamlEditor;
    internal WpfDesignerControl Designer { get; private set; }
    internal string XamlPath { get; private set; }
    private string _saved, _savedDisk, _codePath;
    private bool _syncing, _saving;
    private readonly ToolStrip _views = new() { Dock = DockStyle.Bottom, GripStyle = ToolStripGripStyle.Hidden };
    private readonly ToolStripButton _design = new("Design"), _xaml = new("XAML"), _code = new("Code-behind");
    private readonly ToolStripLabel _message = new();
    internal bool IsDesignView => _design.Checked;
    internal bool IsDirty => SourceEditor.Text != _saved;
    internal WpfDesignTab(MainForm main, TabPage page, string path)
    {
        _main = main; Page = page; XamlPath = path; _codePath = path + ".cs";
        SourceEditor = page.Controls.OfType<TextEditorControl>().First(); _saved = SourceEditor.Text; _savedDisk = File.ReadAllText(path);
        _views.Items.AddRange(new ToolStripItem[] { _design, _xaml, _code, new ToolStripSeparator(), _message }); page.Controls.Add(_views);
        _design.Click += (_, _) => ShowDesign(); _xaml.Click += (_, _) => ShowXaml(); _code.Click += (_, _) => Attempt(ShowCode);
        SourceEditor.TextChanged += SourceChanged; ApplyTheme();
    }
    private void Attempt(Action action) { try { action(); } catch (Exception error) { _message.Text = error.Message; _message.ToolTipText = error.Message; } }
    internal void ShowDesign()
    {
        try
        {
            _main.EditorTabControl.SelectedTab = Page;
            if (Designer == null)
            {
                Designer = new WpfDesignerControl(_main, SourceEditor.Text) { Host = this };
                Designer.Session.Changed += DesignChanged; Page.Controls.Add(Designer); Page.Controls.SetChildIndex(Designer, 1);
            }
            else if (Designer.Session.Document.Serialize() != SourceEditor.Text) Designer.Session.Reset(SourceEditor.Text);
            SourceEditor.Visible = false; Designer.Visible = true; _design.Checked = true; _xaml.Checked = false; _message.Text = "";
            CanvasFocus(); RefreshCaption();
        }
        catch (Exception error) { ShowInlineXaml(); _message.Text = error.Message; _message.ToolTipText = error.Message; }
    }
    private void CanvasFocus() { Page.PerformLayout(); Designer.Canvas.Focus(); }
    internal void ShowInlineXaml()
    {
        if (Designer != null) Designer.Visible = false;
        SourceEditor.Visible = true; _design.Checked = false; _xaml.Checked = true; RefreshCaption();
    }
    internal void ShowXaml()
    {
        if (XamlPage == null || XamlPage.IsDisposed)
        {
            TabControllerManage.AddNewTab(_main.EditorTabControl, initialText: SourceEditor.Text);
            XamlPage = _main.EditorTabControl.SelectedTab; XamlPage.ToolTipText = XamlPath;
            if (XamlPage is EditorTabPage editorPage) editorPage.WpfXamlView = true;
            _xamlEditor = XamlPage.Controls.OfType<TextEditorControl>().First();
            _xamlEditor.SetHighlighting("XML"); _xamlEditor.TextChanged += XamlChanged; XamlPage.Disposed += XamlClosed;
        }
        _main.EditorTabControl.SelectedTab = XamlPage; _main.TabsFeature.UpdateActiveEditorPathFromSelectedTab(); RefreshCaption(); _xamlEditor.Focus();
    }
    internal void ShowCode()
    {
        var document = new WpfDocument(SourceEditor.Text);
        string code = ReadCode(document);
        var page = CodePage();
        if (page == null)
        {
            TabControllerManage.AddNewTab(_main.EditorTabControl, initialText: code, filePath: _codePath);
            page = _main.EditorTabControl.SelectedTab;
        }
        _main.EditorTabControl.SelectedTab = page; _main.TabsFeature.UpdateActiveEditorPathFromSelectedTab();
        if (!File.Exists(_codePath)) page.Text = "*" + Path.GetFileName(_codePath) + "               ";
        page.Controls.OfType<TextEditorControl>().First().Focus();
    }
    internal void TryShowCode() => Attempt(ShowCode);
    private TabPage CodePage() => _main.EditorTabControl.TabPages.Cast<TabPage>().FirstOrDefault(p => string.Equals(p.ToolTipText, _codePath, StringComparison.OrdinalIgnoreCase));
    private string ReadCode(WpfDocument document) => CodePage()?.Controls.OfType<TextEditorControl>().FirstOrDefault()?.Text ?? (File.Exists(_codePath) ? File.ReadAllText(_codePath) : WpfCodeBehind.Create(document));
    internal void BindEvent(string name, string handler, bool navigate)
    {
        if (Designer == null) return;
        var evt = WpfCatalog.Events(Designer.Session.Selected).FirstOrDefault(e => e.Name == name) ?? throw new InvalidDataException("This event is unavailable for the selected control.");
        string code = handler.Length == 0 ? null : WpfCodeBehind.EnsureHandler(ReadCode(Designer.Session.Document), Designer.Session.Document, evt, handler);
        Designer.Session.Set(name, handler);
        if (code != null)
        {
            var selected = _main.EditorTabControl.SelectedTab;
            ShowCode(); var editor = SelectedEditor.GetSelectedEditor(); editor.Text = code;
            if (navigate)
            {
                var lines = editor.Document.LineSegmentCollection.Cast<ICSharpCode.TextEditor.Document.LineSegment>();
                var line = lines.FirstOrDefault(l => editor.Document.GetText(l).Contains("void " + handler + "("));
                if (line != null) { editor.ActiveTextAreaControl.Caret.Position = new TextLocation(0, line.LineNumber); editor.ActiveTextAreaControl.ScrollTo(line.LineNumber); }
            }
            else _main.EditorTabControl.SelectedTab = selected;
        }
    }
    private void DesignChanged(object sender, EventArgs e)
    {
        if (_syncing) return;
        SetSource(Designer.Session.Document.Serialize()); RefreshCaption();
    }
    private void SetSource(string text)
    {
        _syncing = true;
        try { if (SourceEditor.Text != text) SourceEditor.Text = text; if (_xamlEditor != null && _xamlEditor.Text != text) _xamlEditor.Text = text; }
        finally { _syncing = false; }
    }
    private void SourceChanged(object sender, EventArgs e)
    {
        if (_syncing) return;
        SetSource(SourceEditor.Text); RefreshCaption();
    }
    private void XamlChanged(object sender, EventArgs e)
    {
        if (_syncing) return;
        SetSource(_xamlEditor.Text); RefreshCaption();
    }
    internal void DetachXamlPage()
    {
        if (XamlPage == null) return;
        _xamlEditor.TextChanged -= XamlChanged; XamlPage.Disposed -= XamlClosed;
        if (XamlPage is EditorTabPage p) p.WpfXamlView = false;
        XamlPage = null; _xamlEditor = null;
    }
    private void XamlClosed(object sender, EventArgs e) => DetachXamlPage();
    internal bool Save(bool saveAs)
    {
        if (_saving || Designer != null && !Designer.CommitEditors()) return false;
        _saving = true;
        try
        {
            var document = new WpfDocument(SourceEditor.Text);
            string target = XamlPath;
            if (saveAs)
            {
                using var dialog = new SaveFileDialog { Filter = "WPF XAML (*.xaml)|*.xaml", FileName = Path.GetFileName(XamlPath), InitialDirectory = Path.GetDirectoryName(XamlPath) };
                if (dialog.ShowDialog(_main) != DialogResult.OK) return false;
                target = dialog.FileName;
                if (!string.Equals(target, XamlPath, StringComparison.OrdinalIgnoreCase) && (File.Exists(target) || File.Exists(target + ".cs"))) throw new InvalidDataException("Choose a new filename to preserve existing files.");
            }
            if (File.Exists(XamlPath) && File.ReadAllText(XamlPath) != _savedDisk) throw new InvalidDataException("The XAML file changed on disk. Reopen it before saving these changes.");
            var files = new Dictionary<string, string> { [target] = SourceEditor.Text };
            if (document.ClassName.Length > 0) files[target + ".cs"] = ReadCode(document);
            FormDesignTab.WriteFiles(files);
            var oldCodePage = CodePage(); bool moved = !string.Equals(target, XamlPath, StringComparison.OrdinalIgnoreCase);
            if (moved) { Page.ToolTipText = target; UpdatePath(target); }
            if (moved && oldCodePage != null) oldCodePage.ToolTipText = target + ".cs";
            foreach (var file in files) _main.FormDesignerFeature.RefreshOpenFile(file.Key, file.Value);
            _saved = _savedDisk = SourceEditor.Text; Designer?.Session.MarkSaved(); RefreshCaption(); _message.Text = "Saved";
            _main.TabsFeature.UpdateActiveEditorPathFromSelectedTab(); return true;
        }
        catch (Exception error) { _message.Text = error.Message; MessageBox.Show(_main, error.Message, "Save WPF document", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        finally { _saving = false; }
    }
    internal bool ConfirmClose()
    {
        if (Designer != null && !Designer.CommitEditors()) return false;
        if (!IsDirty) return true;
        var answer = MessageBox.Show(_main, "Save changes to " + Path.GetFileName(XamlPath) + "?", "CIARE", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer == DialogResult.No || answer == DialogResult.Yes && Save(false);
    }
    internal void UpdatePath(string path)
    {
        XamlPath = path; _codePath = path + ".cs";
        if (XamlPage != null) XamlPage.ToolTipText = path;
        RefreshCaption();
    }
    internal void RefreshCaption()
    {
        if (Page.IsDisposed) return;
        Page.Text = (IsDirty ? "*" : "") + Path.GetFileName(XamlPath) + (IsDesignView ? " [Design]" : "") + "               ";
        if (XamlPage != null) XamlPage.Text = (IsDirty ? "*" : "") + Path.GetFileName(XamlPath) + "               ";
        _main.LiveShareEventsFeature.RefreshAvailability();
    }
    internal void ApplyTheme() { DesignerTheme.For(_main).ApplyStrip(_views); Designer?.ApplyTheme(); }
    public void Dispose()
    {
        DetachXamlPage(); SourceEditor.TextChanged -= SourceChanged;
        if (Designer != null) { Designer.Session.Changed -= DesignChanged; Designer.Dispose(); }
        _views.Dispose(); if (!SourceEditor.IsDisposed) SourceEditor.Visible = true;
    }
}
