using CIARE.Designer;
using CIARE.Designer.Wpf;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CIARE;

internal static class WpfDesignerRegression
{
    internal static void Run(MainForm main, Action<bool, string> assert)
    {
        var document = new WpfDocument(Xaml);
        assert(document.Serialize() == Xaml, "Unedited WPF XAML is preserved byte for byte");
        assert(document.Elements.Count() == 7, "Resources and grid definitions are not treated as visual controls");
        using (var rendered = new WpfRenderer().Render(document))
            assert(rendered.Width == 760 && rendered.Height == 480, "WPF renders its layout into the design surface");
        foreach (var spec in WpfCatalog.Controls)
        {
            var sample = WpfDocument.Create("WpfTest.Sample");
            var control = sample.Add(spec.Kind, sample.Root, 20, 20);
            var renderer = new WpfRenderer();
            using var image = renderer.Render(sample);
            assert(renderer.Views[control].GetType() == spec.Type, spec.Kind + " previews as a native WPF control");
            assert(WpfCatalog.Events(control).Any(e => e.Name == WpfCatalog.DefaultEvent(control)), spec.Kind + " has a valid default event");
        }
        bool rejected = false;
        try { _ = new WpfDocument("<!DOCTYPE Window [<!ENTITY ext SYSTEM 'file:///missing'>]><Window xmlns=\"" + WpfDocument.Presentation + "\">&ext;</Window>"); }
        catch (System.Xml.XmlException) { rejected = true; }
        assert(rejected, "XAML parsing never resolves external XML entities");
        foreach (string rootKind in new[] { "UserControl", "Page" })
        {
            var alternate = new WpfDocument(WpfDocument.Create("Test.View").Serialize().Replace("<Window", "<" + rootKind).Replace("</Window>", "</" + rootKind + ">"));
            using var image = new WpfRenderer().Render(alternate);
            assert(image.Width == 800 && WpfCodeBehind.Create(alternate).Contains("System.Windows.Controls." + rootKind), rootKind + " supports design view and matching code-behind");
        }
        var nested = new WpfSession("<Window xmlns=\"" + WpfDocument.Presentation + "\"><Border><ScrollViewer><Grid><Grid.Children/></Grid></ScrollViewer></Border></Window>");
        nested.Add("Button", nested.Document.Root);
        assert(nested.Selected.Parent.Name.LocalName == "Grid.Children", "Adding through nested single-content containers preserves explicit Children property syntax");
        nested.Copy(); nested.Paste();
        assert(nested.Document.Elements.Count(e => e.Name.LocalName == "Button") == 2, "Pasting into an explicit Children collection preserves valid XAML");
        var restricted = new WpfSession(WpfDocument.Create("Test.View").Serialize()); restricted.Add("TabControl", restricted.Document.Root);
        restricted.Copy(); string restrictedBefore = restricted.Document.Serialize(); rejected = false;
        try { restricted.Paste(); } catch (InvalidDataException) { rejected = true; }
        assert(rejected && restricted.Document.Serialize() == restrictedBefore, "Invalid item containers reject paste without changing the document");
        var inline = new WpfSession("<Window xmlns=\"" + WpfDocument.Presentation + "\"><Button>Save</Button></Window>");
        inline.Select(WpfDocument.Children(inline.Document.Root).Single());
        var contentProperty = new WpfPropertyView(inline).GetProperties()["Content"];
        assert((string)contentProperty.GetValue(inline) == "Save", "Properties read literal inline WPF content");
        contentProperty.SetValue(inline, "Save draft");
        assert(inline.Selected.Value == "" && (string)inline.Selected.Attribute("Content") == "Save draft", "Editing inline content does not assign Content twice");
        inline.Undo(); assert(inline.Selected.Value == "Save", "Undo preserves the original inline content syntax");
        var sliderElement = new XElement(WpfDocument.Presentation + "Slider");
        var valueEvent = WpfCatalog.Events(sliderElement).Single(e => e.Name == "ValueChanged");
        string genericCode = Code.Replace("    public MainWindow()", "    private void Volume_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) {}\n    public MainWindow()");
        assert(WpfCodeBehind.EnsureHandler(genericCode, document, valueEvent, "Volume_Changed") == genericCode, "Existing generic routed event handlers accept C# type aliases");

        string folder = Path.Combine(GlobalVariables.userProfileDirectory, "WpfDesigner"); Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "MainWindow.xaml"); File.WriteAllText(path, Xaml); File.WriteAllText(path + ".cs", Code);
        main.ClientSize = new Size(1480, 900);
        main.ExplorerTreeFeature.LoadFileExplorerFolder(folder); main.ExplorerTreeFeature.OpenFileFromExplorer(path); Application.DoEvents();
        var page = main.EditorTabControl.SelectedTab;
        var tab = main.WpfDesignerFeature.Get(page);
        assert(tab?.Designer != null && tab.IsDesignView, "Opening a WPF window automatically opens its integrated designer");
        assert(tab.Designer.Parent == page && main.OwnedForms.Length == 0, "WPF design view is embedded in the editor tab");
        assert(!main.liveShareToolStripMenuItem.Enabled, "Live Share cannot start on the WPF design surface");
        var session = tab.Designer.Session;
        XElement Named(string name) => session.Document.Elements.Single(e => WpfDocument.Name(e) == name);
        var button = Named("saveButton"); session.Select(button);
        var properties = new WpfPropertyView(session).GetProperties();
        properties["FontSize"].SetValue(session, "18");
        assert((string)session.Selected.Attribute("FontSize") == "18", "Properties update native WPF formatting"); session.Undo();
        string propertyBefore = session.Document.Serialize(); rejected = false;
        try { properties["Name"].SetValue(session, "input"); } catch (InvalidDataException) { rejected = true; }
        assert(rejected && session.Document.Serialize() == propertyBefore, "Duplicate names roll back without damaging XAML");
        rejected = false;
        try { properties["FontSize"].SetValue(session, "-5"); } catch (ArgumentException) { rejected = true; }
        assert(rejected && session.Document.Serialize() == propertyBefore, "Property edits honor native WPF validation");
        button = Named("saveButton");
        assert(tab.Designer.Events.Rows.Cast<DataGridViewRow>().Any(r => (string)r.Cells[0].Value == "Click" && (string)r.Cells[1].Value == "Save_Click"), "Events show existing WPF handlers");
        assert(tab.Designer.Canvas.Renderer.Approximations > 0, "Bindings and custom controls use previews without executing project code");
        string before = session.Document.Serialize();
        var canvas = tab.Designer.Canvas;
        var bounds = canvas.NodeBounds(button); var click = new Point(bounds.Left + 20, bounds.Top + 12);
        Mouse(canvas, "OnMouseDown", click); Mouse(canvas, "OnMouseUp", click);
        assert(session.Document.Serialize() == before && !session.CanUndo, "Clicking a WPF control does not move it or create undo history");
        Mouse(canvas, "OnMouseDown", click); Mouse(canvas, "OnMouseMove", new Point(click.X + 1, click.Y)); Mouse(canvas, "OnMouseUp", new Point(click.X + 1, click.Y));
        assert(session.Document.Serialize() == before, "Pointer jitter remains a selection click");
        Mouse(canvas, "OnMouseDown", click); Mouse(canvas, "OnMouseMove", new Point(click.X + 16, click.Y + 24)); Mouse(canvas, "OnMouseUp", new Point(click.X + 16, click.Y + 24));
        assert((string)session.Selected.Attribute("Canvas.Left") == "29" && (string)session.Selected.Attribute("Canvas.Top") == "45", "Dragging edits WPF attached positions");
        session.Undo(); assert(session.Document.Serialize() == before, "Undo restores exact XAML after moving");
        session.Redo(); assert((string)session.Selected.Attribute("Canvas.Left") == "29", "Redo restores the WPF move"); session.Undo();
        bounds = canvas.NodeBounds(session.Selected); var handle = new Point(bounds.Right, bounds.Bottom);
        Mouse(canvas, "OnMouseDown", handle); Mouse(canvas, "OnMouseMove", new Point(handle.X + 16, handle.Y + 8)); Mouse(canvas, "OnMouseUp", new Point(handle.X + 16, handle.Y + 8));
        assert((string)session.Selected.Attribute("Width") == "116" && (string)session.Selected.Attribute("Height") == "40", "WPF resize handles update Width and Height"); session.Undo();
        session.Add("CheckBox", Named("body"), 24, 130);
        string checkName = WpfDocument.Name(session.Selected);
        tab.BindEvent("Checked", "optIn_Checked", true);
        var codePage = main.EditorTabControl.SelectedTab; var codeEditor = SelectedEditor.GetSelectedEditor();
        assert(codePage != page && codePage.ToolTipText == path + ".cs" && codeEditor.Text.Contains("global::System.Windows.RoutedEventArgs"), "WPF event creation opens a separate C# tab with the correct signature");
        assert(codeEditor.Text.Contains("Title = \"Saved\"; // Keep my handler."), "Existing code-behind handler bodies are preserved");
        assert(main.liveShareToolStripMenuItem.Enabled, "The separate code-behind tab can use Live Share");
        tab.ShowDesign(); session.Select(Named(checkName)); tab.BindEvent("Checked", "optIn_Checked", true);
        assert(codeEditor.Text.Split("void optIn_Checked").Length == 2, "Repeated event editing does not duplicate handlers");
        tab.ShowDesign(); session.Select(Named(checkName));
        var inspector = (TabControl)typeof(WpfDesignerControl).GetField("_inspector", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tab.Designer); inspector.SelectedIndex = 1;
        var uncheckedRow = tab.Designer.Events.Rows.Cast<DataGridViewRow>().Single(r => (string)r.Cells[0].Value == "Unchecked");
        tab.Designer.Events.CurrentCell = uncheckedRow.Cells[1]; tab.Designer.Events.BeginEdit(true);
        ((TextBox)tab.Designer.Events.EditingControl).Text = "optIn_Unchecked"; tab.Designer.Events.EndEdit();
        assert((string)session.Selected.Attribute("Unchecked") == "optIn_Unchecked" && codeEditor.Text.Contains("void optIn_Unchecked"), "Editing an Events cell commits the binding and typed handler");
        tab.ShowDesign(); session.Select(Named(checkName)); session.Copy(); session.Select(Named("body")); session.Paste();
        assert(WpfDocument.Name(session.Selected) != checkName && session.Selected.Attribute("Checked") == null, "Pasting creates a unique name without copying event bindings"); session.Undo();
        tab.ShowXaml(); var xamlPage = main.EditorTabControl.SelectedTab; var xamlEditor = SelectedEditor.GetSelectedEditor();
        assert(xamlPage != page && xamlPage == tab.XamlPage && xamlEditor.Text == tab.SourceEditor.Text, "View XAML opens a synchronized source tab");
        string validXaml = xamlEditor.Text; xamlEditor.Text = "<Window"; tab.ShowDesign();
        assert(!tab.IsDesignView && tab.SourceEditor.Visible && !tab.Designer.Visible, "Invalid XAML returns to source instead of displaying a stale designer");
        xamlEditor.Text = validXaml; tab.ShowDesign();
        assert(tab.IsDesignView, "Correcting XAML restores Design view");
        xamlEditor.Text = xamlEditor.Text.Replace("Content=\"Save\"", "Content=\"Save draft\""); tab.ShowDesign();
        session = tab.Designer.Session;
        assert((string)Named("saveButton").Attribute("Content") == "Save draft", "XAML source edits reload into the design surface");
        assert(tab.Save(false) && !tab.IsDirty, "Saving writes XAML and code-behind together");
        var saved = new WpfDocument(File.ReadAllText(path));
        assert(saved.Elements.Single(e => WpfDocument.Name(e) == "input").Attribute("Text").Value == "{Binding Name, Mode=TwoWay}", "Bindings survive design and source edits");
        assert(saved.Root.Element(WpfDocument.Presentation + "Window.Resources").ToString() == document.Root.Element(WpfDocument.Presentation + "Window.Resources").ToString(), "Resources remain intact");
        assert(saved.Elements.Single(e => WpfDocument.Name(e) == "customBadge").Attribute("Secret").Value == "keep", "Custom control markup is preserved");
        assert(File.ReadAllText(path + ".cs").Contains("void optIn_Checked"), "Generated event stubs are saved to the code-behind");
        foreach (string theme in new[] { "C#-DarkVS", "C#-Light", "C#-NordWave" })
        {
            main.WindowThemeFeature.SetHighLighter(tab.SourceEditor, theme, persistSetting: false); tab.ApplyTheme();
            assert(tab.Designer.Properties.ViewBackColor == DesignerTheme.For(main).Field && tab.Designer.Events.DefaultCellStyle.ForeColor == DesignerTheme.For(main).Text,
                theme + " applies to WPF Properties and Events");
            Capture(main, "wpf-designer-" + theme + ".png");
        }
        tab.ShowXaml(); xamlEditor.Document.Insert(xamlEditor.Document.TextLength, "\n<!-- source view edit -->");
        main.EditorTabControl.TabPages.Remove(page); page.Dispose();
        assert(!xamlPage.IsDisposed && xamlEditor.Text.Contains("source view edit") && main.WpfDesignerFeature.Get(xamlPage) == null, "Closing the designer leaves its separate XAML editor usable");

        using var projectDialog = new NewProject();
        T Field<T>(string name) => (T)typeof(NewProject).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(projectDialog);
        Field<TextBox>("_projectNameText").Text = "WpfSample"; Field<TextBox>("_locationText").Text = folder; Field<CheckBox>("_createSolutionCheckBox").Checked = false;
        var templates = Field<ListBox>("_templateList"); templates.SelectedItem = templates.Items.Cast<object>().Single(t => t.ToString() == "WPF App");
        Field<ComboBox>("_targetFrameworkCombo").Text = "net10.0-windows";
        var project = (NewProjectResult)typeof(NewProject).GetMethod("CreateProject", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectDialog, null);
        assert(File.ReadAllText(project.ProjectFilePath).Contains("<UseWPF>true</UseWPF>") && project.StarterFilePath.EndsWith("MainWindow.xaml") && File.Exists(project.StarterFilePath + ".cs"), "The WPF project template includes an editable window and matching code-behind");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "wpf-project-path.txt"), project.ProjectFilePath);
        string probeFolder = Path.Combine(Path.GetDirectoryName(project.ProjectFilePath), "Controls"); Directory.CreateDirectory(probeFolder);
        foreach (var spec in WpfCatalog.Controls)
        {
            string className = spec.Kind + "Probe";
            var sample = WpfDocument.Create("WpfSample.Controls." + className);
            var control = sample.Add(spec.Kind, sample.Root, 20, 20);
            string generated = WpfCodeBehind.Create(sample);
            // Compile each distinct event argument type as well as every toolbox control.
            foreach (var evt in WpfCatalog.Events(control).GroupBy(e => e.ArgumentType).Select(g => g.First()))
            {
                string handler = spec.Kind + "_" + evt.Name; control.SetAttributeValue(evt.Name, handler);
                generated = WpfCodeBehind.EnsureHandler(generated, sample, evt, handler);
            }
            File.WriteAllText(Path.Combine(probeFolder, className + ".xaml"), sample.Serialize());
            File.WriteAllText(Path.Combine(probeFolder, className + ".xaml.cs"), generated);
        }
    }
    private static void Mouse(Control control, string method, Point p) => control.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(control, new object[] { new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0) });
    private static void Capture(Form form, string name) { using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size)); image.Save(Path.Combine(AppContext.BaseDirectory, name)); }
    private const string Xaml = """
<Window x:Class="DesignerWpfTest.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:custom="clr-namespace:Example.Controls;assembly=UnavailableCustomControls"
        Title="WPF customer form" Width="760" Height="480">
    <Window.Resources>
        <!-- Preserve project resources -->
        <SolidColorBrush x:Key="Accent">#336699</SolidColorBrush>
    </Window.Resources>
    <Grid x:Name="layoutRoot" Background="White">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <TextBlock x:Name="heading" Text="{Binding Title}" Height="32" Margin="24,12,24,12"/>
        <Canvas x:Name="body" Grid.Row="1" Width="700" Height="380" HorizontalAlignment="Left" VerticalAlignment="Top">
            <Button x:Name="saveButton" Content="Save" Canvas.Left="13" Canvas.Top="21" Width="100" Height="32" Click="Save_Click"/>
            <TextBox x:Name="input" Text="{Binding Name, Mode=TwoWay}" Canvas.Left="13" Canvas.Top="75" Width="220" Height="28"/>
            <custom:Badge x:Name="customBadge" Canvas.Left="300" Canvas.Top="21" Width="120" Height="44" Secret="keep"/>
        </Canvas>
    </Grid>
</Window>
""";
    private const string Code = """
namespace DesignerWpfTest;
public partial class MainWindow : System.Windows.Window
{
    public MainWindow() { InitializeComponent(); }
    private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Title = "Saved"; // Keep my handler.
    }
}
""";
}
