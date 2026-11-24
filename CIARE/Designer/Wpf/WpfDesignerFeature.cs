using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CIARE.GUI;
using ICSharpCode.TextEditor;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfDesignerFeature(MainForm main)
{
    private readonly Dictionary<TabPage, WpfDesignTab> _tabs = new();
    internal WpfDesignTab Get(TabPage page) => page == null ? null : _tabs.GetValueOrDefault(page) ?? _tabs.Values.FirstOrDefault(t => t.XamlPage == page);
    internal void Initialize()
    {
        var create = new ToolStripMenuItem("New WPF Window…") { Name = "newWpfWindowMenuItem" }; create.Click += (_, _) => Open(newWindow: true);
        main.fIleToolStripMenuItem.DropDownItems.Insert(4, create);
        var view = new ToolStripMenuItem("WPF Designer") { Name = "wpfDesignerMenuItem", ShortcutKeys = Keys.Control | Keys.Alt | Keys.D }; view.Click += (_, _) => Open();
        main.viewToolStripMenuItem.DropDownItems.Add(view);
        main.Disposed += (_, _) => { foreach (var tab in _tabs.Values.ToArray()) tab.Dispose(); _tabs.Clear(); };
    }
    internal void Attach(TabPage page, bool showDesign)
    {
        if (page == null || page.IsDisposed || page is EditorTabPage { WpfXamlView: true }) return;
        if (Get(page) is { } prior)
        { if (string.Equals(prior.XamlPath, page.ToolTipText, StringComparison.OrdinalIgnoreCase)) return; Detach(page); }
        string path = page.ToolTipText?.Trim();
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return;
        var editor = page.Controls.OfType<TextEditorControl>().FirstOrDefault(); if (editor == null) return;
        try { _ = new WpfDocument(editor.Text); } catch { return; } // Resource dictionaries and App.xaml remain ordinary source files.
        editor.SetHighlighting("XML");
        var tab = new WpfDesignTab(main, page, path); _tabs.Add(page, tab); page.Disposed += (_, _) => Detach(page);
        if (showDesign) tab.ShowDesign(); else tab.ShowInlineXaml();
    }
    internal void Detach(TabPage page)
    {
        if (_tabs.Remove(page, out var tab)) tab.Dispose(); else Get(page)?.DetachXamlPage();
    }
    internal void Open(string path = null, bool newWindow = false)
    {
        try
        {
            if (newWindow) { CreateNew(); return; }
            if (Get(main.EditorTabControl.SelectedTab) is { } active && path == null) { active.ShowDesign(); return; }
            path ??= main.EditorTabControl.SelectedTab?.ToolTipText;
            if (path?.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) == true) path = path[..^3];
            if (path?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) != true || !File.Exists(path))
            {
                using var dialog = new OpenFileDialog { Filter = "WPF XAML (*.xaml)|*.xaml", Title = "Open WPF document" };
                if (dialog.ShowDialog(main) != DialogResult.OK) return; path = dialog.FileName;
            }
            var existing = _tabs.Values.FirstOrDefault(t => string.Equals(t.XamlPath, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null) { existing.ShowDesign(); return; }
            main.ExplorerTreeFeature.OpenFileFromExplorer(path); Attach(main.EditorTabControl.SelectedTab, true);
            if (Get(main.EditorTabControl.SelectedTab) is { } tab) tab.ShowDesign();
            else MessageBox.Show(main, "Select a WPF Window, Page, or UserControl XAML file.", "WPF Designer");
        }
        catch (Exception error) { MessageBox.Show(main, error.Message, "WPF Designer", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void CreateNew()
    {
        using var dialog = new SaveFileDialog { Filter = "WPF XAML (*.xaml)|*.xaml", FileName = "MainWindow.xaml", Title = "New WPF Window" };
        if (dialog.ShowDialog(main) != DialogResult.OK) return;
        string name = Path.GetFileNameWithoutExtension(dialog.FileName), ns = "MyApplication";
        if (!DesignDocument.IsIdentifier(name)) throw new InvalidDataException("Use a C# class name for the XAML filename.");
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(dialog.FileName)); directory != null; directory = directory.Parent)
        {
            var project = directory.GetFiles("*.csproj").FirstOrDefault(); if (project == null) continue;
            ns = XDocument.Load(project.FullName).Descendants().FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value ?? Path.GetFileNameWithoutExtension(project.Name); break;
        }
        CreateFiles(dialog.FileName, WpfDocument.Create(ns + "." + name, name)); Open(dialog.FileName);
    }
    internal static void CreateFiles(string path, WpfDocument document)
    {
        if (File.Exists(path) || File.Exists(path + ".cs")) throw new InvalidDataException("A document with that name already exists.");
        FormDesignTab.WriteFiles(new Dictionary<string, string> { [path] = document.Serialize(), [path + ".cs"] = WpfCodeBehind.Create(document) });
    }
    internal bool TrySaveActive(bool saveAs) { var tab = Get(main.EditorTabControl.SelectedTab); if (tab == null) return false; tab.Save(saveAs); return true; }
    internal void ApplyTheme() { foreach (var tab in _tabs.Values) tab.ApplyTheme(); }
}
