using CIARE.Designer;
using CIARE.Roslyn;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows.Forms;

namespace CIARE;

internal static class FormDesignerRegression
{
    internal static void Run(MainForm main, Action<bool, string> assert)
    {
        assert(main.fIleToolStripMenuItem.DropDownItems.ContainsKey("newWindowsFormMenuItem"), "New form command is discoverable");
        assert(main.viewToolStripMenuItem.DropDownItems.ContainsKey("formDesignerMenuItem"), "Designer command is discoverable");
        using var window = new FormDesignerWindow(main) { Opacity = 0, ShowInTaskbar = false };
        window.Show(main);
        Application.DoEvents();
        var session = window.Session;
        var root = session.Document.Form;
        session.Change(() => { root.Text = "Customer details"; root.Name = "CustomerForm"; root.Namespace = "DesignerTest"; });
        var title = session.Add("Label", root, new Point(24, 20));
        session.Change(() => { title.Text = "Customer details"; title.Size = new Size(420, 36); title.FontSize = 18; });
        var group = session.Add("GroupBox", root, new Point(24, 72));
        session.Change(() => { group.Text = "Contact information"; group.Size = new Size(568, 240); });
        var label = session.Add("Label", group, new Point(20, 32));
        session.Change(() => { label.Text = "Full name"; });
        var name = session.Add("TextBox", group, new Point(20, 60));
        session.Change(() => { name.Text = "Alex Morgan"; name.Width = 240; });
        var emailLabel = session.Add("Label", group, new Point(288, 32));
        session.Change(() => { emailLabel.Text = "Email address"; emailLabel.Width = 160; });
        var email = session.Add("TextBox", group, new Point(288, 60));
        session.Change(() => { email.Text = "alex@example.com"; email.Width = 240; });
        var check = session.Add("CheckBox", group, new Point(20, 108));
        session.Change(() => { check.Text = "Send email updates"; check.Checked = true; check.Width = 216; });
        var notes = session.Add("TextBox", group, new Point(20, 148));
        session.Change(() => { notes.Text = "Customer notes…"; notes.Multiline = true; notes.Size = new Size(508, 64); });
        var button = session.Add("Button", root, new Point(480, 340));
        session.Change(() => { button.Text = "Save customer"; button.Width = 112; });
        string buttonId = button.Id, groupId = group.Id;
        session.BindEvent("Click", "SaveCustomer_Click");
        assert(window.EventGrid.Rows.Cast<DataGridViewRow>().Any(r => (string)r.Cells[0].Value == "Click" && (string)r.Cells[1].Value == "SaveCustomer_Click"), "Events panel displays assigned handler");
        assert(window.Properties.SelectedObject != null, "Properties are bound to the selection");
        ((TabControl)window.EventGrid.Parent.Parent).SelectedTab = (TabPage)window.EventGrid.Parent;
        var eventRow = window.EventGrid.Rows.Cast<DataGridViewRow>().Single(r => (string)r.Cells[0].Value == "MouseUp");
        window.EventGrid.CurrentCell = eventRow.Cells[1];
        window.EventGrid.BeginEdit(true);
        ((TextBox)window.EventGrid.EditingControl).Text = "Save_MouseUp";
        window.EventGrid.EndEdit();
        assert(session.Selected.Events["MouseUp"] == "Save_MouseUp", "Editing an event cell commits its binding");
        assert(window.Canvas.NodeBounds(name.Id).Location == new Point(76, 196), "Nested control bounds include their container");

        var before = button.Bounds;
        var visible = window.Canvas.NodeBounds(buttonId);
        Mouse(window.Canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, visible.X + 20, visible.Y + 12, 0));
        Mouse(window.Canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, visible.X + 36, visible.Y + 32, 0));
        Mouse(window.Canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, visible.X + 36, visible.Y + 32, 0));
        assert(session.Selected.X == 496 && session.Selected.Y == 360, "Mouse drag moves controls with grid snapping");
        session.Undo();
        assert(session.Selected.Bounds == before, "Undo restores drag bounds");
        session.Redo();
        assert(session.Selected.X == 496, "Redo reapplies drag");
        session.Undo();
        visible = window.Canvas.NodeBounds(buttonId);
        Mouse(window.Canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, visible.Right, visible.Bottom, 0));
        Mouse(window.Canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, visible.Right + 16, visible.Bottom + 8, 0));
        Mouse(window.Canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, visible.Right + 16, visible.Bottom + 8, 0));
        assert(session.Selected.Size == new Size(128, 40), "Resize handles update dimensions");
        session.Undo();
        session.Copy(); session.Paste();
        assert(session.Selected.Name != session.Document.Find(buttonId).Name && session.Selected.Events.Count == 0, "Paste creates a unique control without accidentally sharing handlers");
        session.Delete();
        session.Select(groupId); session.Copy(); session.Paste();
        assert(session.Selected.Children.Count == 6, "Nested controls copy with their container");
        session.Undo();
        session.Select(buttonId);
        string valid = session.Document.Serialize();
        bool rejected = false;
        try { session.Change(() => session.Selected.Name = "Controls"); } catch (InvalidDataException) { rejected = true; }
        assert(rejected && session.Document.Serialize() == valid, "Invalid property changes roll back atomically");
        rejected = false;
        try { session.BindEvent("MouseDown", "SaveCustomer_Click"); } catch (InvalidDataException) { rejected = true; }
        assert(rejected, "Incompatible event signatures cannot share a handler");

        string directory = Path.Combine(GlobalVariables.userProfileDirectory, "Designer");
        Directory.CreateDirectory(directory);
        string layoutPath = Path.Combine(directory, "CustomerForm.ciareform");
        DesignDocument.WriteAtomic(layoutPath, session.Document.Serialize());
        session.MarkSaved();
        var loaded = DesignDocument.Load(layoutPath);
        assert(loaded.Serialize() == session.Document.Serialize(), "Layout round trip preserves properties, nesting and events");
        assert(!session.IsDirty, "Saving clears dirty state");
        session.Change(() => session.Selected.Text = "Save now");
        assert(session.IsDirty, "Edits mark the layout dirty");
        session.Undo(); assert(!session.IsDirty, "Undo back to the saved layout clears dirty state");
        string codePath = Path.Combine(directory, "CustomerForm.cs");
        var exported = DesignCodeGenerator.Export(session.Document, codePath);
        string originalCode = File.ReadAllText(codePath).Replace("// Add your event-handler code here.", "this.Text = \"Handler executed\"; // Keep this user code.");
        File.WriteAllText(codePath, originalCode);
        session.CreateHandler("MouseDown");
        DesignCodeGenerator.Export(session.Document, codePath);
        string updated = File.ReadAllText(codePath);
        assert(updated.Contains("this.Text = \"Handler executed\"; // Keep this user code."), "Export preserves user handler bodies");
        assert(updated.Split("private void SaveCustomer_Click").Length == 2, "Export does not duplicate existing handlers");
        assert(updated.Contains("MouseEventArgs"), "New handlers use typed event arguments");
        string repeated = DesignCodeGenerator.GenerateCodeBehind(session.Document, updated);
        assert(repeated == updated, "Repeated export is idempotent");
        CompileAndRun(session.Document, File.ReadAllText(exported.DesignerPath), updated, assert, true);

        string unrelatedPath = Path.Combine(directory, "Unrelated.cs");
        string unrelatedDesigner = Path.Combine(directory, "Unrelated.Designer.cs");
        File.WriteAllText(unrelatedDesigner, "// User-owned designer");
        rejected = false;
        try { DesignCodeGenerator.Export(session.Document, unrelatedPath); } catch (InvalidDataException) { rejected = true; }
        assert(rejected && !File.Exists(unrelatedPath) && File.ReadAllText(unrelatedDesigner) == "// User-owned designer", "Unrelated designer files are never overwritten");
        var everyControl = new DesignDocument();
        foreach (var spec in DesignCatalog.Controls)
        {
            var node = DesignCatalog.Create(spec.Kind, everyControl.UniqueName(spec.Kind), new Point(8, 8));
            everyControl.Form.Children.Add(node);
            if (node.Kind is "ComboBox" or "ListBox") node.Items = new[] { "Quotes \" and slash " + (char)92, "Unicode: café\r\nsecond line" };
            foreach (var evt in DesignCatalog.EventsFor(node.Kind)) node.Events[evt.Name] = "On" + evt.ArgumentType.Split('.').Last();
        }
        foreach (var evt in DesignCatalog.EventsFor("Form")) everyControl.Form.Events[evt.Name] = "On" + evt.ArgumentType.Split('.').Last();
        CompileAndRun(everyControl, DesignCodeGenerator.GenerateDesigner(everyControl), DesignCodeGenerator.GenerateCodeBehind(everyControl), assert, false);
        using (var allPreview = DesignCatalog.CreatePreview(everyControl))
            assert(allPreview.Controls.Count == DesignCatalog.Controls.Length, "Every toolbox control can be previewed");
        using (var projectDialog = new NewProject())
        {
            T Field<T>(string field) => (T)typeof(NewProject).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(projectDialog);
            Field<TextBox>("_projectNameText").Text = "DesignerProject";
            Field<TextBox>("_locationText").Text = directory;
            Field<CheckBox>("_createSolutionCheckBox").Checked = false;
            var templates = Field<ListBox>("_templateList");
            templates.SelectedItem = templates.Items.Cast<object>().Single(t => t.ToString() == "Windows Forms App");
            Field<ComboBox>("_targetFrameworkCombo").Text = "net10.0-windows";
            var project = (NewProjectResult)typeof(NewProject).GetMethod("CreateProject", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(projectDialog, null);
            string projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
            var projectLayout = DesignDocument.Load(Path.Combine(projectDirectory, "MainForm.ciareform"));
            assert(projectLayout.Form.Name == "MainForm" && projectLayout.Form.Namespace == "DesignerProject", "New Windows Forms projects include a matching layout");
            CompileAndRun(projectLayout, File.ReadAllText(Path.Combine(projectDirectory, "MainForm.Designer.cs")),
                File.ReadAllText(Path.Combine(projectDirectory, "MainForm.cs")), assert, false, File.ReadAllText(project.StarterFilePath));
        }
        using (var preview = DesignCatalog.CreatePreview(session.Document))
        {
            preview.ShowInTaskbar = false; preview.Opacity = 0; preview.Show(window); Application.DoEvents();
            assert(preview.Controls.Find("textBox1", true).Single().Text == "Alex Morgan", "Preview uses the designed control properties");
            Capture(preview, "form-designer-preview.png"); preview.Close();
        }
        session.Select(buttonId);
        typeof(FormDesignerWindow).GetMethod("CreateEvent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, new object[] { "Click" });
        Application.DoEvents();
        Capture(window, "form-designer-dark.png");
        window.Size = new Size(960, 640); Application.DoEvents();
        assert(window.EventGrid.Width >= 260 && window.Properties.Width >= 260, "Inspector remains usable at minimum window size");
        session.MarkSaved(); window.Close();
        bool wasDark = GlobalVariables.darkColor;
        try
        {
            GlobalVariables.darkColor = false;
            using var light = new FormDesignerWindow(main, layoutPath) { Opacity = 0, ShowInTaskbar = false };
            light.Show(main); Application.DoEvents();
            assert(light.EventGrid.DefaultCellStyle.ForeColor.GetBrightness() < 0.3, "Light theme has dark event text");
            Capture(light, "form-designer-light.png"); light.Close();
        }
        finally { GlobalVariables.darkColor = wasDark; }
    }
    private static void Mouse(Control control, string method, MouseEventArgs args) =>
        control.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(control, new object[] { args });
    private static void Capture(Form form, string name)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory, name));
    }
    private static void CompileAndRun(DesignDocument document, string designer, string code, Action<bool, string> assert, bool click, string extraSource = "")
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Distinct(StringComparer.OrdinalIgnoreCase).Select(SharedMetadataReferences.Get);
        var compilation = CSharpCompilation.Create("DesignedForm" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(designer), CSharpSyntaxTree.ParseText(code), CSharpSyntaxTree.ParseText(extraSource) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        assert(result.Success, "Exported form compiles: " + string.Join("; ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        if (!result.Success) return;
        stream.Position = 0;
        var context = new AssemblyLoadContext("Designer regression", true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            using var instance = (Form)Activator.CreateInstance(assembly.GetType(document.Form.Namespace + "." + document.Form.Name));
            assert(instance.Controls.Count == document.Form.Children.Count, "Exported form creates its controls");
            if (click)
            {
                typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance.Controls.Find("button1", true).Single(), new object[] { EventArgs.Empty });
                assert(instance.Text == "Handler executed", "Generated event wiring invokes the preserved handler");
            }
        }
        finally { context.Unload(); }
    }
}
