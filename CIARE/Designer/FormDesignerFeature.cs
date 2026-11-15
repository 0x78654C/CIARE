using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CIARE.GUI;
using ICSharpCode.TextEditor;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class FormDesignerFeature(MainForm main)
{
    private readonly Dictionary<TabPage, FormDesignTab> _tabs = new();
    internal FormDesignTab Get(TabPage page) => page == null ? null : _tabs.TryGetValue(page, out var tab) ? tab : _tabs.Values.FirstOrDefault(t => t.CodePage == page);
    internal void Initialize()
    {
        var create = new ToolStripMenuItem("New Windows Form…") { Name = "newWindowsFormMenuItem" };
        create.Click += (_, _) => Open(newWindow: true);
        var open = new ToolStripMenuItem("Open Windows Form…") { Name = "openFormLayoutMenuItem" };
        open.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Windows Forms (*.cs;*.ciareform)|*.cs;*.ciareform", Title = "Open Windows Form" };
            if (dialog.ShowDialog(main) == DialogResult.OK) Open(dialog.FileName);
        };
        main.fIleToolStripMenuItem.DropDownItems.Insert(2, create);
        main.fIleToolStripMenuItem.DropDownItems.Insert(3, open);
        var view = new ToolStripMenuItem("Form Designer") { Name = "formDesignerMenuItem", ShortcutKeys = Keys.Control | Keys.Shift | Keys.D };
        view.Click += (_, _) => Open();
        main.viewToolStripMenuItem.DropDownItems.Add(view);
        var code = new ToolStripMenuItem("View Code") { Name = "formCodeMenuItem", ShortcutKeys = Keys.F7 };
        code.Click += (_, _) => Get(main.EditorTabControl.SelectedTab)?.ShowCode();
        main.viewToolStripMenuItem.DropDownItems.Add(code);
        main.EditorTabControl.SelectedIndexChanged += (_, _) => Attach(main.EditorTabControl.SelectedTab, false);
        main.Disposed += (_, _) => { foreach (var tab in _tabs.Values.ToArray()) tab.Dispose(); _tabs.Clear(); };
    }
    internal void Attach(TabPage page, bool showDesign = true)
    {
        if (page == null || page.IsDisposed || !page.Controls.OfType<TextEditorControl>().Any()) return;
        if (page is EditorTabPage { FormCodeView: true }) return;
        string path = page.ToolTipText?.Trim();
        var prior = Get(page);
        if (prior != null)
        {
            if (string.Equals(prior.CodePath, path, StringComparison.OrdinalIgnoreCase)) return;
            Detach(page);
        }
        if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(FormSource.DesignerPath(path))) return;
        string text = page.Controls.OfType<TextEditorControl>().First().Text;
        if (!text.Contains("InitializeComponent", StringComparison.Ordinal)) return;
        var tab = new FormDesignTab(main, page, path); _tabs.Add(page, tab);
        page.Disposed += (_, _) => Detach(page);
        if (showDesign) tab.ShowDesign(false); else tab.ShowInlineCode();
    }
    internal void Detach(TabPage page)
    {
        if (_tabs.Remove(page, out var tab)) tab.Dispose();
        else Get(page)?.DetachCodePage();
        if (page is EditorTabPage editorPage) editorPage.FormCodeView = false;
    }
    internal void Open(string path = null, bool newWindow = false)
    {
        try
        {
            if (newWindow) { CreateNew(); return; }
            path ??= main.EditorTabControl.SelectedTab?.ToolTipText?.Trim();
            if (string.IsNullOrWhiteSpace(path)) { CreateNew(); return; }
            if (path.EndsWith(".ciareform", StringComparison.OrdinalIgnoreCase))
            {
                string code = Path.ChangeExtension(path, ".cs");
                if (!File.Exists(code) && !File.Exists(FormSource.DesignerPath(code))) CreateFiles(code, DesignDocument.Load(path));
                path = code;
            }
            path = FormSource.CodePath(path);
            var existing = _tabs.Values.FirstOrDefault(t => string.Equals(t.CodePath, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null) { existing.ShowDesign(); return; }
            main.ExplorerTreeFeature.OpenFileFromExplorer(path);
            if (main.EditorTabControl.SelectedTab is EditorTabPage { FormCodeView: true } codePage && File.Exists(FormSource.DesignerPath(path)))
            {
                string codeText = codePage.Controls.OfType<TextEditorControl>().First().Text;
                TabControllerManage.AddNewTab(main.EditorTabControl, initialText: codeText, filePath: path);
                Get(main.EditorTabControl.SelectedTab)?.LinkCodePage(codePage);
            }
            Attach(main.EditorTabControl.SelectedTab, false);
            var tab = Get(main.EditorTabControl.SelectedTab);
            if (tab != null) tab.ShowDesign();
            else MessageBox.Show(main, "Open a Windows Form .cs file with its matching .Designer.cs file to use Design view.", "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error) { MessageBox.Show(main, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void CreateNew()
    {
        string active = main.EditorTabControl.SelectedTab?.ToolTipText?.Trim();
        using var dialog = new SaveFileDialog { Filter = "Windows Form (*.cs)|*.cs", Title = "New Windows Form", FileName = "Form1.cs",
            InitialDirectory = File.Exists(active) ? Path.GetDirectoryName(active) : "" };
        if (dialog.ShowDialog(main) != DialogResult.OK) return;
        string path = dialog.FileName, name = Path.GetFileNameWithoutExtension(dialog.FileName);
        if (!DesignDocument.IsIdentifier(name) || path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Use a C# class name such as CustomerForm.cs.");
        var document = new DesignDocument(); document.Form.Name = document.Form.Text = name;
        for (var folder = new DirectoryInfo(Path.GetDirectoryName(path)); folder != null; folder = folder.Parent)
        {
            var project = folder.GetFiles("*.csproj").FirstOrDefault();
            if (project == null) continue;
            string ns = XDocument.Load(project.FullName).Descendants().FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value ?? Path.GetFileNameWithoutExtension(project.Name);
            if (ns.Split('.').All(DesignDocument.IsIdentifier)) document.Form.Namespace = ns;
            break;
        }
        CreateFiles(path, document); Open(path);
    }
    internal static void CreateFiles(string path, DesignDocument document)
    {
        var files = new Dictionary<string, string>
        {
            [path] = DesignCodeGenerator.GenerateCodeBehind(document),
            [FormSource.DesignerPath(path)] = DesignCodeGenerator.GenerateDesigner(document),
            [Path.ChangeExtension(path, ".resx")] = FormDesignTab.EmptyResources
        };
        if (files.Keys.Any(File.Exists)) throw new InvalidDataException("A form file with this name already exists. Choose another name.");
        FormDesignTab.WriteFiles(files);
    }
    internal string ReadText(string path)
    {
        var page = main.EditorTabControl.TabPages.Cast<TabPage>().FirstOrDefault(p => string.Equals(p.ToolTipText.Trim(), path, StringComparison.OrdinalIgnoreCase));
        return page?.Controls.OfType<TextEditorControl>().FirstOrDefault()?.Text ?? File.ReadAllText(path);
    }
    internal void RefreshOpenFile(string path, string text)
    {
        foreach (TabPage page in main.EditorTabControl.TabPages)
        {
            if (!string.Equals(page.ToolTipText.Trim(), path, StringComparison.OrdinalIgnoreCase)) continue;
            var editor = page.Controls.OfType<TextEditorControl>().FirstOrDefault();
            if (editor != null && editor.Text != text) editor.Text = text;
            page.Text = Path.GetFileName(path) + "               ";
            TabControllerManage.StoreFileMD5(path, Utils.GlobalVariables.userProfileDirectory, Utils.GlobalVariables.tabsFilePath, main.EditorTabControl.TabPages.IndexOf(page));
        }
    }
    internal bool TrySaveActive(bool saveAs = false)
    {
        var tab = Get(main.EditorTabControl.SelectedTab);
        if (tab == null) return false;
        tab.Save(saveAs); return true;
    }
    internal void ApplyTheme() { foreach (var tab in _tabs.Values) tab.ApplyTheme(); }
}
