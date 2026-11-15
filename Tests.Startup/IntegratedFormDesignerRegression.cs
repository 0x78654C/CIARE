using CIARE.Designer;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CIARE;

internal static class IntegratedFormDesignerRegression
{
    internal static void Run(MainForm main, Action<bool, string> assert)
    {
        string directory = Path.Combine(GlobalVariables.userProfileDirectory, "IntegratedForms");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "CustomerForm.cs");
        File.WriteAllText(path, Code);
        File.WriteAllText(FormSource.DesignerPath(path), DesignerCode);
        string resources = FormDesignTab.EmptyResources.Replace("</root>", "  <data name=\"Greeting\" xml:space=\"preserve\"><value>Keep this resource</value></data>\n</root>");
        File.WriteAllText(Path.ChangeExtension(path, ".resx"), resources);
        var imported = new FormSource(DesignerCode);
        assert(imported.Document.AllNodes.Single(n => n.Name == "saveButton").Options["FlatStyle"] == "Flat", "Native appearance settings are imported into Properties");
        assert(imported.Document.Form.Children.Count == 2, "Standard Visual Studio designer source imports without a CIARE layout");
        assert(imported.Document.AllNodes.Single(n => n.Name == "saveButton").Events["Click"] == "Save_Click", "Existing event bindings are imported");
        assert(imported.Update(DesignDocument.Parse(imported.Document.Serialize())) == DesignerCode, "An unchanged imported form retains its exact source text");
        var edited = DesignDocument.Parse(imported.Document.Serialize());
        edited.AllNodes.Single(n => n.Name == "saveButton").Text = "Save changes";
        string patched = imported.Update(edited);
        assert(patched.Contains("saveButton.FlatStyle = FlatStyle.Flat;") && patched.Contains("// Preserve custom setup."), "Unchanged native properties and comments survive editing");
        assert(new FormSource(patched).Document.AllNodes.Single(n => n.Name == "saveButton").Text == "Save changes", "Edited properties round trip through designer source");
        Compile(Code, patched, assert);

        main.ClientSize = new Size(1420, 880);
        main.ExplorerTreeFeature.LoadFileExplorerFolder(directory);
        main.ExplorerTreeFeature.OpenFileFromExplorer(path);
        Application.DoEvents();
        var page = main.EditorTabControl.SelectedTab;
        var tab = main.FormDesignerFeature.Get(page);
        assert(tab != null && tab.IsDesignView, "Opening a form automatically displays its embedded design view");
        assert(tab.Designer.Parent == page && tab.Designer.FindForm() == main && main.OwnedForms.Length == 0, "The designer is inside the main editor without a separate window");
        assert(ReferenceEquals(page.Controls[0], tab.CodeEditor) && ReferenceEquals(SelectedEditor.GetSelectedEditor(), tab.CodeEditor), "Editor services retain the form's code editor");
        var tree = main.ExplorerFeature._fileExplorerTree;
        TreeNode formNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(n => (string)n.Tag == path);
        assert(formNode.Nodes.Cast<TreeNode>().Select(n => Path.GetFileName((string)n.Tag)).SequenceEqual(new[] { "CustomerForm.Designer.cs", "CustomerForm.resx" }), "Explorer nests designer and resources under the main form like Visual Studio");
        assert(tree.Nodes[0].Nodes.Count == 1, "Nested form files are not duplicated at folder level");
        formNode.Expand();

        tab.Designer.Controls.OfType<ToolStrip>().SelectMany(s => s.Items.OfType<ToolStripButton>()).Single(b => b.Text == "View Code").PerformClick();
        var codePage = main.EditorTabControl.SelectedTab;
        var sourceEditor = SelectedEditor.GetSelectedEditor();
        assert(codePage != page && codePage == tab.CodePage && codePage.ToolTipText == path && sourceEditor.Visible && sourceEditor.Text == Code,
            "View Code opens the form source in a separate editor tab");
        assert(tab.IsDesignView && page.Text.Contains("[Design]") && !codePage.Text.Contains("[Design]") && tab.Designer.Parent == page,
            "The original tab retains its designer and a distinct caption");
        int tabCount = main.EditorTabControl.TabCount;
        main.EditorTabControl.SelectedTab = page;
        main.viewToolStripMenuItem.DropDownItems["formCodeMenuItem"].PerformClick();
        assert(main.EditorTabControl.SelectedTab == codePage && main.EditorTabControl.TabCount == tabCount,
            "View Code and F7 reuse the existing source tab");
        sourceEditor.Document.Insert(sourceEditor.Document.TextLength, "\r\n// Separate code tab edit");
        assert(tab.CodeDirty && tab.CodeEditor.Text == sourceEditor.Text && codePage.Text.StartsWith("*"), "Source edits synchronize with the form document");
        sourceEditor.Document.UndoStack.Undo();
        assert(!tab.CodeDirty && tab.CodeEditor.Text == Code, "Undo in the code tab also updates the form document");
        main.FormDesignerFeature.Open();
        assert(main.EditorTabControl.SelectedTab == page && tab.IsDesignView, "Form Designer returns from the code tab to the existing designer");

        var session = tab.Designer.Session;
        var button = session.Document.AllNodes.Single(n => n.Name == "saveButton");
        session.Select(button.Id);
        session.Change(() => { button.Text = "Save customer"; button.Location = new Point(400, 304); });
        session.CreateHandler("MouseDown");
        var panel = session.Document.AllNodes.Single(n => n.Name == "detailsPanel");
        var check = session.Add("CheckBox", panel, new Point(24, 108));
        session.Change(() => { check.Text = "Send email updates"; check.Width = 220; check.Checked = true; });
        assert(tab.DesignDirty && page.Text.StartsWith("*"), "Design changes mark the owning document dirty");
        assert(main.Text.StartsWith("*"), "Design changes also trigger the main editor's save-before-run behavior");
        tab.EditHandler("saveButton_MouseDown");
        assert(tab.IsDesignView && main.EditorTabControl.SelectedTab == codePage && sourceEditor.Text.Contains("void saveButton_MouseDown"), "Edit code creates the handler in the separate source tab");
        assert(sourceEditor.Document.GetText(sourceEditor.Document.GetLineSegment(sourceEditor.ActiveTextAreaControl.Caret.Line)).Contains("void saveButton_MouseDown"), "Handler navigation positions the caret at its implementation");
        sourceEditor.Text = sourceEditor.Text.Replace("// Add your event-handler code here.", "Text = \"New handler body\";");
        tab.ShowDesign();
        assert(tab.DesignDirty && session.Document.AllNodes.Any(n => n.Kind == "CheckBox"), "Switching between code and design preserves pending changes");
        tab.ShowCode();
        FileManage.SaveToFileDialog();
        assert(!tab.DesignDirty && !tab.CodeDirty && !page.Text.StartsWith("*") && !codePage.Text.StartsWith("*"), "Save from the separate code tab saves both code and designer changes");
        tab.ShowDesign();
        string savedCode = File.ReadAllText(path), savedDesigner = File.ReadAllText(FormSource.DesignerPath(path));
        assert(savedCode.Contains("Text = \"Existing handler body\";") && savedCode.Contains("Text = \"New handler body\";"), "Existing and edited handler implementations survive Save");
        assert(File.ReadAllText(Path.ChangeExtension(path, ".resx")) == resources, "Existing resources remain byte-for-byte unchanged");
        var saved = new FormSource(savedDesigner).Document;
        assert(saved.AllNodes.Single(n => n.Name == "saveButton").Location == new Point(400, 304) && saved.AllNodes.Any(n => n.Kind == "CheckBox"), "Saved source contains moved and newly created controls");
        Compile(savedCode, savedDesigner, assert);
        assert(session.CanUndo, "Saving preserves designer undo history");
        session.Undo(); assert(tab.DesignDirty, "Undo remains available after Save");
        session.Redo(); assert(!tab.DesignDirty, "Redo returns to the saved layout");

        // Delete and collection edits also operate on imported statements, without regenerating unrelated code.
        var choices = session.Add("ComboBox", session.Document.Form, new Point(32, 350));
        session.Change(() => choices.Items = new[] { "Alpha", "Beta \"quoted\"" });
        assert(tab.Save(false), "Adding a collection control can be saved");
        var loadedChoices = tab.Designer.Session.Document.AllNodes.Single(n => n.Kind == "ComboBox");
        assert(loadedChoices.Items.Length == 2, "Collection items survive a source round trip");
        tab.Designer.Session.Select(loadedChoices.Id); tab.Designer.Session.Delete();
        assert(tab.Save(false), "Deleting a control can be saved");
        Compile(File.ReadAllText(path), File.ReadAllText(FormSource.DesignerPath(path)), assert);

        tab.OpenRelated(".Designer.cs");
        var designerEditor = SelectedEditor.GetSelectedEditor();
        assert(main.EditorTabControl.SelectedTab.ToolTipText == FormSource.DesignerPath(path), "Designer Source opens the related source file");
        designerEditor.Text = designerEditor.Text.Replace("Save customer", "Updated in source");
        main.EditorTabControl.SelectedTab = page; tab.ShowDesign();
        assert(tab.Designer.Session.Document.AllNodes.Single(n => n.Name == "saveButton").Text == "Updated in source", "Design view reloads edits from an open designer-source buffer");
        assert(tab.Save(false), "Saving the form also saves the current designer-source buffer");
        main.EditorTabControl.SelectedTab = page;
        tab.OpenRelated(".resx");
        assert(main.EditorTabControl.SelectedTab.ToolTipText == Path.ChangeExtension(path, ".resx"), "Resources opens the associated resx document");
        main.EditorTabControl.SelectedTab = page; tab.ShowDesign();
        session = tab.Designer.Session;
        session.Select(session.Document.AllNodes.Single(n => n.Name == "saveButton").Id);
        typeof(FormDesignerControl).GetMethod("CreateEvent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tab.Designer, new object[] { "Click" });
        Application.DoEvents();
        Capture(main, "form-designer-integrated.png");
        string originalTheme = main.WindowThemeFeature._appliedTheme;
        foreach (string themeName in new[] { "C#-Light", "C#-DarkVS", "C#-NordWave" })
        {
            main.SetHighLighter(tab.CodeEditor, themeName, false); Application.DoEvents();
            var palette = DesignerTheme.For(main);
            var inspector = (DesignerInspectorTabs)tab.Designer.EventGrid.Parent.Parent;
            assert(inspector.Theme == palette && tab.Designer.Properties.ViewBackColor == palette.Field &&
                tab.Designer.Properties.HelpForeColor == palette.Text && tab.Designer.EventGrid.DefaultCellStyle.ForeColor == palette.Text,
                themeName + " updates both inspector tabs without reopening the form");
            using (var tabImage = new Bitmap(inspector.Width, inspector.Height))
            {
                inspector.DrawToBitmap(tabImage, inspector.ClientRectangle);
                assert(tabImage.GetPixel(inspector.Width - 8, inspector.GetTabRect(0).Top + 5).ToArgb() == palette.Panel.ToArgb(),
                    themeName + " paints the unused tab strip with the active background");
            }
            inspector.SelectedIndex = 0; Capture(main, "form-designer-properties-" + themeName + ".png");
            inspector.SelectedIndex = 1; Capture(main, "form-designer-events-" + themeName + ".png");
        }
        main.SetHighLighter(tab.CodeEditor, originalTheme, false);
        main.ClientSize = new Size(1000, 700); Application.DoEvents();
        assert(tab.Designer.Width > 400 && tab.Designer.EventGrid.Width >= 200, "Embedded designer remains usable in the normal editor window");

        // The application's own complex form is parsed without constructing or running it.
        string ownDesigner = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Model", "MainForm.Designer.cs"));
        string ownText = File.ReadAllText(ownDesigner);
        var own = new FormSource(ownText);
        assert(own.Document.Form.Name == "MainForm" && own.Document.AllNodes.Count() > 20, "CIARE's existing MainForm imports its control hierarchy");
        assert(own.Update(DesignDocument.Parse(own.Document.Serialize())) == ownText, "Complex designer source is preserved exactly when unchanged");
        using (var ownPreview = DesignCatalog.CreatePreview(own.Document))
            assert(ownPreview.Controls.Count == own.Document.Form.Children.Count, "Complex forms preview without executing application code");

        string second = Path.Combine(directory, "SecondForm.cs");
        var secondDocument = new DesignDocument(); secondDocument.Form.Name = "SecondForm";
        FormDesignerFeature.CreateFiles(second, secondDocument);
        main.ExplorerTreeFeature.OpenFileFromExplorer(second);
        var secondPage = main.EditorTabControl.SelectedTab;
        assert(main.FormDesignerFeature.Get(secondPage)?.IsDesignView == true && !ReferenceEquals(secondPage, page), "Multiple form tabs have independent designers");
        var secondTab = main.FormDesignerFeature.Get(secondPage);
        var secondSession = secondTab.Designer.Session;
        secondSession.Add("Button", secondSession.Document.Form, new Point(24, 24));
        assert(AnswerDialogs(2, () => TabControllerManage.CloseTabEvent(main.EditorTabControl, SelectedEditor.GetSelectedEditor())) == 1 && !secondPage.IsDisposed,
            "Cancelling the form save prompt keeps its designer tab open");
        assert(AnswerDialogs(2, () => main.Close()) == 1 && !main.IsDisposed && secondSession.IsDirty,
            "Cancelling application close preserves unsaved form edits");
        assert(AnswerDialogs(2, () => secondTab.Save(true)) == 1 && secondTab.CodePath == second && secondSession.IsDirty,
            "Cancelling Save As keeps the form path and dirty state");
        secondSession.Undo();
        secondSession.Add("Button", secondSession.Document.Form, new Point(24, 24));
        string renamed = Path.Combine(directory, "RenamedForm.cs");
        secondTab.ShowCode();
        var secondCodePage = main.EditorTabControl.SelectedTab;
        var renames = Utils.Explorer.ExplorerActions.GetFormFileRenames(second, renamed, false);
        Utils.Explorer.ExplorerActions.MoveFormFiles(renames);
        foreach (var rename in renames) main.TabsFeature.UpdateOpenTabsAfterExplorerRename(rename.Source, rename.Target, false);
        assert(secondTab.CodePath == renamed && secondTab.DesignDirty && File.Exists(Path.ChangeExtension(renamed, ".resx")) && !File.Exists(second),
            "Renaming a form moves its companion files and preserves pending designer edits");
        assert(secondCodePage.ToolTipText == renamed && GlobalVariables.openedFilePath == renamed && secondCodePage.Text.Trim() == "RenamedForm.cs",
            "Renaming a form updates its separate source tab and active file path");
        assert(secondTab.Save(false), "A renamed form saves to its new source paths");
        main.EditorTabControl.SelectedTab = page;
        assert(main.FormDesignerFeature.Get(page) == tab && tab.IsDesignView, "Switching tabs restores the previous form view");
        main.EditorTabControl.SelectedTab = secondPage;
        TabControllerManage.CloseTabEvent(main.EditorTabControl, SelectedEditor.GetSelectedEditor());
        assert(secondPage.IsDisposed && main.FormDesignerFeature.Get(secondPage) == null, "Closing a form tab disposes its designer and registry entry");
        main.EditorTabControl.SelectedTab = secondCodePage;
        TabControllerManage.CloseTabEvent(main.EditorTabControl, SelectedEditor.GetSelectedEditor());
        main.EditorTabControl.SelectedTab = page;
        tab.ShowCode();
        sourceEditor.Document.Insert(sourceEditor.Document.TextLength, "\r\n// Keep unsaved source while closing its view");
        TabControllerManage.CloseTabEvent(main.EditorTabControl, SelectedEditor.GetSelectedEditor());
        assert(codePage.IsDisposed && !page.IsDisposed && tab.CodeDirty && tab.CodePage == null, "Closing the source tab keeps unsaved code in the designer document");
        tab.ShowCode(); codePage = main.EditorTabControl.SelectedTab; sourceEditor = SelectedEditor.GetSelectedEditor();
        assert(sourceEditor.Text.Contains("// Keep unsaved source while closing its view"), "Reopening the source tab restores pending edits");
        FileManage.SaveToFileDialog();
        main.EditorTabControl.SelectedTab = page;
        TabControllerManage.CloseTabEvent(main.EditorTabControl, SelectedEditor.GetSelectedEditor());
        main.EditorTabControl.SelectedTab = codePage;
        assert(page.IsDisposed && !codePage.IsDisposed && main.FormDesignerFeature.Get(codePage) == null && sourceEditor.Visible,
            "Closing the designer leaves the standalone source tab usable");
        sourceEditor.Document.Insert(sourceEditor.Document.TextLength, "\r\n// Preserved when reopening the designer");
        main.FormDesignerFeature.Open();
        var reopened = main.FormDesignerFeature.Get(main.EditorTabControl.SelectedTab);
        assert(reopened != null && reopened.Page != codePage && reopened.CodePage == codePage && reopened.CodeDirty && reopened.CodeEditor.Text == sourceEditor.Text,
            "Reopening Design from a standalone source tab preserves unsaved code and separate views");
        assert(reopened.Save(false), "Reopened linked views save successfully");
        Capture(main, "form-designer-separate-code-tabs.png");
        reopened.ShowCode();
        string pendingSource = reopened.CodeEditor.Text;
        FileManage.NewFile(SelectedEditor.GetSelectedEditor(), main.outputRBT);
        assert(reopened.CodePage == null && reopened.CodeEditor.Text == pendingSource && SelectedEditor.GetSelectedEditor().Text == "",
            "Replacing a source view with New File leaves the designer's code intact");
        reopened.ShowCode();
        string notesPath = Path.Combine(directory, "notes.txt"); File.WriteAllText(notesPath, "A different document");
        FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), notesPath);
        assert(reopened.CodePage == null && reopened.CodeEditor.Text == pendingSource && SelectedEditor.GetSelectedEditor().Text == "A different document",
            "Opening a different file in the source tab detaches it before replacing its text");
        reopened.ShowDesign(); reopened.ShowCode();
        Capture(main, "form-designer-separate-code-view.png");
    }
    private static void Compile(string code, string designer, Action<bool, string> assert)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Distinct(StringComparer.OrdinalIgnoreCase).Select(SharedMetadataReferences.Get);
        var compilation = CSharpCompilation.Create("IntegratedForm" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(code), CSharpSyntaxTree.ParseText(designer) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        assert(result.Success, "Edited imported form compiles: " + string.Join("; ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
    }
    private static int AnswerDialogs(int command, Action action)
    {
        int count = 0;
        uint thread = GetCurrentThreadId();
        using var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) => EnumThreadWindows(thread, (handle, _) =>
        {
            var name = new System.Text.StringBuilder(64);
            GetClassName(handle, name, name.Capacity);
            if (name.ToString() == "#32770") { count++; SendMessage(handle, 0x0111, new IntPtr(command), IntPtr.Zero); }
            return true;
        }, IntPtr.Zero);
        timer.Start(); action(); timer.Stop(); return count;
    }
    private delegate bool WindowCallback(IntPtr handle, IntPtr argument);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, IntPtr argument);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, System.Text.StringBuilder name, int length);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    private static void Capture(Form main, string file)
    {
        using var snapshot = new Bitmap(main.Width, main.Height);
        main.DrawToBitmap(snapshot, new Rectangle(Point.Empty, main.Size));
        snapshot.Save(Path.Combine(AppContext.BaseDirectory, file));
    }
    private const string Code = """
        using System;
        using System.Windows.Forms;
        namespace ExistingProject;
        public partial class CustomerForm : Form
        {
            public CustomerForm() { InitializeComponent(); }
            private void Save_Click(object sender, EventArgs e)
            {
                Text = "Existing handler body";
            }
        }
        """;
    private const string DesignerCode = """
        using System.Drawing;
        using System.Windows.Forms;
        namespace ExistingProject
        {
            partial class CustomerForm
            {
                private Button saveButton;
                private Panel detailsPanel;
                private Label titleLabel;
                private TextBox nameText;
                private void InitializeComponent()
                {
                    saveButton = new Button();
                    detailsPanel = new Panel();
                    titleLabel = new Label();
                    nameText = new TextBox();
                    SuspendLayout();
                    detailsPanel.Location = new Point(24, 24);
                    detailsPanel.Size = new Size(560, 240);
                    titleLabel.Location = new Point(24, 24);
                    titleLabel.Size = new Size(400, 40);
                    titleLabel.Text = "Customer details";
                    titleLabel.Font = new Font("Segoe UI", 18F);
                    nameText.Location = new Point(24, 72);
                    nameText.Size = new Size(400, 28);
                    nameText.Text = "Alex Morgan";
                    detailsPanel.Controls.AddRange(new Control[] { titleLabel, nameText });
                    saveButton.Location = new Point(440, 300);
                    saveButton.Size = new Size(136, 32);
                    saveButton.Text = "Save";
                    // Preserve custom setup.
                    saveButton.FlatStyle = FlatStyle.Flat;
                    saveButton.Tag = typeof(CustomerForm);
                    saveButton.Click += new System.EventHandler(this.Save_Click);
                    ClientSize = new Size(640, 420);
                    Text = "Customer form";
                    Controls.AddRange(new Control[] { detailsPanel, saveButton });
                    ResumeLayout(false);
                }
            }
        }
        """;
}
