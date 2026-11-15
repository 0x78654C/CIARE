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
internal sealed class FormDesignTab : IDisposable
{
    private readonly MainForm _main;
    internal readonly TabPage Page;
    internal readonly TextEditorControl CodeEditor;
    internal TabPage CodePage { get; private set; }
    private TextEditorControl _codeViewEditor;
    internal string CodePath { get; private set; }
    internal FormDesignerControl Designer { get; private set; }
    private readonly ToolStrip _views = new() { Dock = DockStyle.Bottom, GripStyle = ToolStripGripStyle.Hidden, Height = 29 };
    private readonly ToolStripButton _code = new("Code"), _design = new("Design");
    private readonly ToolStripLabel _message = new() { AutoSize = false, Width = 280 };
    private FormSource _source;
    private string _savedCode;
    private bool _saving, _syncingCode;
    internal bool IsDesignView => _design.Checked;
    internal bool CodeDirty => CodeEditor.Text != _savedCode;
    internal bool DesignDirty => Designer?.Session.IsDirty == true;
    internal FormDesignTab(MainForm main, TabPage page, string codePath)
    {
        _main = main; Page = page; CodePath = codePath;
        CodeEditor = page.Controls.OfType<TextEditorControl>().First();
        _savedCode = File.Exists(codePath) ? File.ReadAllText(codePath) : CodeEditor.Text;
        _views.Items.AddRange(new ToolStripItem[] { _code, _design, new ToolStripSeparator() });
        _code.Click += (_, _) => ShowCode(); _design.Click += (_, _) => ShowDesign();
        AddRelated("Designer.cs", ".Designer.cs"); AddRelated("Resources", ".resx");
        _views.Items.Add(_message); page.Controls.Add(_views);
        CodeEditor.TextChanged += CodeChanged; ApplyTheme();
    }
    private void AddRelated(string text, string extension)
    {
        var button = new ToolStripButton(text); button.Click += (_, _) => OpenRelated(extension); _views.Items.Add(button);
    }
    internal void ShowDesign(bool reportError = true)
    {
        try
        {
            _main.EditorTabControl.SelectedTab = Page;
            string sourceText = _main.FormDesignerFeature.ReadText(FormSource.DesignerPath(CodePath));
            if (Designer == null)
            {
                _source = new FormSource(sourceText);
                Designer = new FormDesignerControl(_main) { Host = this, Visible = false };
                Designer.Session.Reset(DesignDocument.Parse(_source.Document.Serialize()));
                Designer.Session.Changed += DesignChanged;
                Page.Controls.Add(Designer);
                // Existing editor services expect the code editor at index zero.
                Page.Controls.SetChildIndex(Designer, 1);
            }
            else if (_source.Text != sourceText)
            {
                if (DesignDirty) throw new InvalidDataException("Designer source changed while this layout has unsaved edits. Undo the layout edits before reloading Design view.");
                _source = new FormSource(sourceText); Designer.Session.Reset(DesignDocument.Parse(_source.Document.Serialize()));
            }
            CodeEditor.Visible = false; Designer.Visible = true;
            _code.Checked = false; _design.Checked = true; _message.Text = "";
            Designer.Canvas.Focus();
            int approximations = Designer.Session.Document.AllNodes.Count(n => n.SourceType.Length > 0);
            Designer.SetStatus(approximations == 0 ? "Drag controls from the Toolbox. Ctrl+S saves this form's source files." :
                $"{approximations} custom or unsupported controls use placeholder previews; their source and resources are preserved.");
            Page.PerformLayout(); RefreshCaption();
        }
        catch (Exception error)
        {
            ShowInlineCode(); _message.Text = "Designer: " + error.Message; _message.ToolTipText = error.Message;
            if (reportError) MessageBox.Show(_main, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    internal void ShowCode()
    {
        if (Designer != null && !Designer.CommitEditors()) return;
        if (CodePage == null || CodePage.IsDisposed)
        {
            TabControllerManage.AddNewTab(_main.EditorTabControl, initialText: CodeEditor.Text);
            var page = _main.EditorTabControl.SelectedTab;
            if (page == Page) return;
            page.ToolTipText = CodePath;
            LinkCodePage(page);
        }
        _main.EditorTabControl.SelectedTab = CodePage;
        _main.TabsFeature.UpdateActiveEditorPathFromSelectedTab();
        RefreshCaption(); _codeViewEditor.Focus();
        _main.EditorFeature.ScheduleCurrentTypeCheck(_codeViewEditor);
    }
    internal void LinkCodePage(TabPage page)
    {
        DetachCodePage();
        CodePage = page;
        if (page is EditorTabPage editorPage) editorPage.FormCodeView = true;
        _codeViewEditor = page.Controls.OfType<TextEditorControl>().First();
        _codeViewEditor.TextChanged += CodeViewChanged;
        page.Disposed += CodePageDisposed;
        RefreshCaption();
    }
    internal void DetachCodePage()
    {
        if (CodePage == null) return;
        _codeViewEditor.TextChanged -= CodeViewChanged;
        CodePage.Disposed -= CodePageDisposed;
        CodePage = null; _codeViewEditor = null;
    }
    private void CodePageDisposed(object sender, EventArgs e) => DetachCodePage();
    private void CopyCode(TextEditorControl source, TextEditorControl target)
    {
        if (_syncingCode || target == null || target.IsDisposed || target.Text == source.Text) return;
        try { _syncingCode = true; target.Text = source.Text; }
        finally { _syncingCode = false; }
    }
    private void CodeViewChanged(object sender, EventArgs e) { CopyCode(_codeViewEditor, CodeEditor); RefreshCaption(); }
    internal void ShowInlineCode()
    {
        if (Designer != null) Designer.Visible = false;
        CodeEditor.Visible = true; _code.Checked = true; _design.Checked = false;
        CodeEditor.Focus(); Page.PerformLayout(); RefreshCaption();
    }
    internal void OpenRelated(string extension)
    {
        if (DesignDirty && !Save(false)) return;
        string path = Path.ChangeExtension(CodePath, extension);
        if (!File.Exists(path))
        {
            if (extension != ".resx") return;
            DesignDocument.WriteAtomic(path, EmptyResources);
        }
        _main.ExplorerTreeFeature.OpenFileFromExplorer(path);
    }
    private string MergeHandlers()
    {
        if (Designer == null) return CodeEditor.Text;
        var pending = DesignDocument.Parse(Designer.Session.Document.Serialize());
        // Existing handlers may live in other partials; only new bindings require stubs here.
        foreach (var node in pending.AllNodes)
        {
            var before = _source.Document.Find(node.Id);
            foreach (string key in node.Events.Keys.ToArray())
                if (before?.Events.GetValueOrDefault(key) == node.Events[key]) node.Events.Remove(key);
        }
        return DesignCodeGenerator.GenerateCodeBehind(pending, CodeEditor.Text);
    }
    internal void EditHandler(string handler)
    {
        if (Designer == null || !Designer.CommitEditors()) return;
        CodeEditor.Text = MergeHandlers(); ShowCode();
        var editor = _codeViewEditor ?? CodeEditor;
        int offset = handler == null ? -1 : editor.Text.IndexOf("void " + handler + "(", StringComparison.Ordinal);
        if (offset >= 0)
        {
            var position = editor.Document.OffsetToPosition(offset);
            editor.ActiveTextAreaControl.Caret.Position = position;
            editor.ActiveTextAreaControl.TextArea.ScrollTo(position.Line);
        }
    }
    internal bool Save(bool saveAs)
    {
        if (_saving || Designer != null && !Designer.CommitEditors()) return false;
        _saving = true;
        try
        {
            string target = CodePath;
            if (saveAs)
            {
                using var dialog = new SaveFileDialog { Title = "Save Form As", Filter = "C# form (*.cs)|*.cs", FileName = Path.GetFileName(CodePath), InitialDirectory = Path.GetDirectoryName(CodePath) };
                if (dialog.ShowDialog(_main) != DialogResult.OK) return false;
                target = dialog.FileName;
                if (target.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose the form's main .cs filename.");
                if (!string.Equals(target, CodePath, StringComparison.OrdinalIgnoreCase) &&
                    (File.Exists(target) || File.Exists(FormSource.DesignerPath(target)) || File.Exists(Path.ChangeExtension(target, ".resx"))))
                    throw new InvalidDataException("Choose a new form filename so existing form files are preserved.");
            }
            string currentSource = _main.FormDesignerFeature.ReadText(FormSource.DesignerPath(CodePath));
            if (DesignDirty && currentSource != _source.Text)
                throw new InvalidDataException("Designer source changed after this layout was loaded. Undo the layout edits and reopen Design view to load those source changes.");
            string code = MergeHandlers();
            string designer = DesignDirty ? _source.Update(Designer.Session.Document) : currentSource;
            var verified = new FormSource(designer);
            if (Designer != null)
                foreach (var node in verified.Document.AllNodes)
                {
                    var previous = Designer.Session.Document.AllNodes.FirstOrDefault(n => n.Name == node.Name);
                    if (previous != null) node.Id = previous.Id;
                }
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            { [target] = code, [FormSource.DesignerPath(target)] = designer };
            string resources = Path.ChangeExtension(CodePath, ".resx"), targetResources = Path.ChangeExtension(target, ".resx");
            if (!File.Exists(targetResources)) files[targetResources] = File.Exists(resources) ? File.ReadAllText(resources) : EmptyResources;
            WriteFiles(files);
            if (!string.Equals(target, CodePath, StringComparison.OrdinalIgnoreCase))
            {
                string previousPath = CodePath;
                Page.ToolTipText = target; UpdatePath(target);
                if (GlobalVariables.OStartUp) TabControllerManage.StoreDeleteTabs(previousPath, target, GlobalVariables.userProfileDirectory,
                    GlobalVariables.tabsFilePathAll, _main.EditorTabControl.TabPages.IndexOf(Page));
                if (Page == _main.EditorTabControl.SelectedTab)
                {
                    GlobalVariables.openedFilePath = target; GlobalVariables.openedFileName = Path.GetFileName(target);
                }
            }
            foreach (var file in files) _main.FormDesignerFeature.RefreshOpenFile(file.Key, file.Value);
            _savedCode = code; _source = verified;
            if (Designer != null)
            {
                string selected = Designer.Session.Selected.Name;
                Designer.Session.AcceptSavedDocument(DesignDocument.Parse(verified.Document.Serialize()));
                Designer.Session.Select(verified.Document.AllNodes.FirstOrDefault(n => n.Name == selected)?.Id);
                Designer.SetStatus("Saved " + Path.GetFileName(target) + " and its designer source.");
            }
            RefreshCaption();
            if (File.Exists(_main.EditorTabControl.SelectedTab?.ToolTipText)) FileManage.SetFileMD5(_main.EditorTabControl.SelectedTab.ToolTipText);
            return true;
        }
        catch (Exception error) { MessageBox.Show(_main, error.Message, "Save Form", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        finally { _saving = false; }
    }
    internal bool ConfirmClose()
    {
        if (Designer != null && !Designer.CommitEditors()) return false;
        if (!CodeDirty && !DesignDirty) return true;
        var answer = MessageBox.Show(_main, "Save changes to " + Path.GetFileName(CodePath) + " and its designer?", "CIARE", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer == DialogResult.No || answer == DialogResult.Yes && Save(false);
    }
    internal void RefreshCaption()
    {
        if (Page.IsDisposed) return;
        bool dirty = CodeDirty || DesignDirty;
        Page.Text = (dirty ? "*" : "") + Path.GetFileName(CodePath) + (IsDesignView ? " [Design]" : "") + "               ";
        if (CodePage != null && !CodePage.IsDisposed) CodePage.Text = (CodeDirty ? "*" : "") + Path.GetFileName(CodePath) + "               ";
        if (Page == _main.EditorTabControl.SelectedTab || CodePage != null && CodePage == _main.EditorTabControl.SelectedTab)
            _main.Text = (dirty ? "*" : "") + _main.Text.TrimStart('*');
        _main.LiveShareEventsFeature.RefreshAvailability();
    }
    internal void UpdatePath(string path)
    {
        CodePath = path;
        if (CodePage != null)
        {
            CodePage.ToolTipText = path;
            if (_main.EditorTabControl.SelectedTab == CodePage) _main.TabsFeature.UpdateActiveEditorPathFromSelectedTab();
        }
        RefreshCaption();
    }
    private void CodeChanged(object sender, EventArgs e) { CopyCode(CodeEditor, _codeViewEditor); RefreshCaption(); }
    private void DesignChanged(object sender, EventArgs e) => RefreshCaption();
    internal void ApplyTheme()
    {
        DesignerTheme.For(_main).ApplyStrip(_views);
        Designer?.ApplyTheme();
    }
    public void Dispose()
    {
        DetachCodePage();
        CodeEditor.TextChanged -= CodeChanged;
        if (Designer != null) { Designer.Session.Changed -= DesignChanged; Designer.Dispose(); }
        _views.Dispose(); if (!CodeEditor.IsDisposed) CodeEditor.Visible = true;
    }
    internal static void WriteFiles(Dictionary<string, string> files)
    {
        var previous = files.Keys.ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllText(p) : null, StringComparer.OrdinalIgnoreCase);
        var written = new List<string>();
        try { foreach (var file in files) { DesignDocument.WriteAtomic(file.Key, file.Value); written.Add(file.Key); } }
        catch
        {
            foreach (string path in written.AsEnumerable().Reverse())
                if (previous[path] == null) File.Delete(path); else DesignDocument.WriteAtomic(path, previous[path]);
            throw;
        }
    }
    internal const string EmptyResources = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n" +
        "  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>\n" +
        "  <resheader name=\"version\"><value>2.0</value></resheader>\n" +
        "  <resheader name=\"reader\"><value>System.Resources.ResXResourceReader, System.Windows.Forms</value></resheader>\n" +
        "  <resheader name=\"writer\"><value>System.Resources.ResXResourceWriter, System.Windows.Forms</value></resheader>\n</root>\n";
}
