using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class FormDesignerFeature(MainForm main)
{
    internal void Initialize()
    {
        var create = new ToolStripMenuItem("New Windows Form…") { Name = "newWindowsFormMenuItem" };
        create.Click += (_, _) => Open(newWindow: true);
        var open = new ToolStripMenuItem("Open Form Layout…") { Name = "openFormLayoutMenuItem" };
        open.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "CIARE form layouts (*.ciareform)|*.ciareform", Title = "Open Form Layout" };
            if (dialog.ShowDialog(main) == DialogResult.OK) Open(dialog.FileName);
        };
        main.fIleToolStripMenuItem.DropDownItems.Insert(2, create);
        main.fIleToolStripMenuItem.DropDownItems.Insert(3, open);
        var view = new ToolStripMenuItem("Form Designer…") { Name = "formDesignerMenuItem", ShortcutKeys = Keys.Control | Keys.Shift | Keys.D };
        view.Click += (_, _) =>
        {
            string active = main.EditorFeature.GetActiveEditorFilePath();
            string layout = string.IsNullOrEmpty(active) ? null : Path.ChangeExtension(active.Replace(".Designer.cs", ".cs"), ".ciareform");
            Open(layout != null && File.Exists(layout) ? layout : null);
        };
        main.viewToolStripMenuItem.DropDownItems.Add(view);
    }
    internal void Open(string path = null, bool newWindow = false)
    {
        try
        {
            var existing = main.OwnedForms.OfType<FormDesignerWindow>().FirstOrDefault(f => !f.IsDisposed &&
                (path == null ? !newWindow : string.Equals(f.LayoutPath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)));
            if (existing != null) { existing.Show(); existing.WindowState = FormWindowState.Normal; existing.Activate(); return; }
            // Validate before allocating a native window.
            if (path != null) DesignDocument.Load(path);
            var window = new FormDesignerWindow(main, path);
            window.Show(main);
        }
        catch (Exception error) { MessageBox.Show(main, error.Message, "Form Designer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
