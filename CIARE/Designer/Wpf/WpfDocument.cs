using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfDocument
{
    internal static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    internal static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    internal readonly XDocument Xml;
    private readonly string _original;
    private bool _changed;
    internal XElement Root => Xml.Root;
    internal string ClassName => (string)Root.Attribute(Xaml + "Class") ?? "";
    internal IEnumerable<XElement> Elements => Walk(Root);
    internal WpfDocument(string source)
    {
        using var reader = XmlReader.Create(new StringReader(source), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2_000_000 });
        Xml = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        _original = source;
        Validate(); Xml.Changed += (_, _) => _changed = true;
    }
    internal static WpfDocument Create(string className, string title = "WPF Window") => new(
        $"<Window x:Class=\"{className}\" xmlns=\"{Presentation}\" xmlns:x=\"{Xaml}\"\n        Title=\"{System.Security.SecurityElement.Escape(title)}\" Width=\"800\" Height=\"450\">\n    <Grid>\n    </Grid>\n</Window>\n");
    internal string Serialize() => !_changed ? _original : (Xml.Declaration == null ? "" : Xml.Declaration + Environment.NewLine) + Xml.ToString(SaveOptions.DisableFormatting);
    internal static IEnumerable<XElement> Children(XElement e) => e.Elements().SelectMany(child =>
        child.Name.LocalName.EndsWith(".Children", StringComparison.Ordinal) || child.Name.LocalName.EndsWith(".Content", StringComparison.Ordinal) || child.Name.LocalName.EndsWith(".Items", StringComparison.Ordinal)
            ? child.Elements() : child.Name.LocalName.Contains('.') ? Enumerable.Empty<XElement>() : new[] { child });
    private static IEnumerable<XElement> Walk(XElement e) { yield return e; foreach (var child in Children(e)) foreach (var item in Walk(child)) yield return item; }
    internal static string Name(XElement e) => (string)e.Attribute(Xaml + "Name") ?? (string)e.Attribute("Name") ?? "";
    internal static string PathOf(XElement e) => e.Parent == null ? "" : PathOf(e.Parent) + "/" + e.Parent.Elements().TakeWhile(s => s != e).Count();
    internal XElement Find(string path)
    {
        var e = Root;
        foreach (string part in (path ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries))
        { if (!int.TryParse(part, out int index)) return null; e = e.Elements().ElementAtOrDefault(index); if (e == null) return null; }
        return e;
    }
    internal string UniqueName(string kind)
    {
        string prefix = char.ToLowerInvariant(kind[0]) + kind[1..]; int suffix = 1;
        var names = Elements.Select(Name).ToHashSet(StringComparer.Ordinal);
        while (names.Contains(prefix + suffix)) suffix++;
        return prefix + suffix;
    }
    internal static double Number(XElement e, string property, double fallback) =>
        double.TryParse((string)e.Attribute(property), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value) ? value : fallback;
    internal void Validate()
    {
        if (Root?.Name.Namespace != Presentation || Root.Name.LocalName is not ("Window" or "UserControl" or "Page"))
            throw new InvalidDataException("Design view supports WPF Window, UserControl, and Page XAML files.");
        if (ClassName.Length > 0 && !ClassName.Split('.').All(DesignDocument.IsIdentifier)) throw new InvalidDataException("x:Class must be a valid C# type name.");
        var names = new HashSet<string>(StringComparer.Ordinal); int count = 0;
        foreach (var e in Elements)
        {
            if (++count > 250 || e.Ancestors().Count() > 24) throw new InvalidDataException("Design view supports up to 250 controls and 24 nesting levels.");
            string name = Name(e);
            if (name.Length > 0 && (!DesignDocument.IsIdentifier(name) || !names.Add(name))) throw new InvalidDataException("Control names must be unique C# identifiers.");
            foreach (string property in new[] { "Width", "Height" })
                if (e.Attribute(property) is { } dimension && !dimension.Value.StartsWith('{') && dimension.Value != "Auto" &&
                    (!double.TryParse(dimension.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n) || n < 0 || n > 4000))
                    throw new InvalidDataException(property + " must be Auto or a number from 0 to 4000.");
            foreach (var evt in WpfCatalog.Events(e))
                if (e.Attribute(evt.Name) is { } handler && !DesignDocument.IsIdentifier(handler.Value)) throw new InvalidDataException("Use a C# method name for " + evt.Name + ".");
        }
    }
    internal static XElement VisualParent(XElement element) => element.Parent?.Name.LocalName.Contains('.') == true ? element.Parent.Parent : element.Parent;
    internal static XElement PropertyElement(XElement e, string name) => e.Elements().FirstOrDefault(c => c.Name.LocalName == e.Name.LocalName + "." + name);
    internal static void Set(XElement e, string name, string value)
    {
        if (PropertyElement(e, name) != null) throw new InvalidDataException("Edit the " + name + " property element in XAML.");
        if (name is "Content" or "Text")
        {
            if (Children(e).Any()) throw new InvalidDataException("Edit this control's nested content in XAML.");
            foreach (var text in e.Nodes().OfType<XText>().Where(t => !string.IsNullOrWhiteSpace(t.Value)).ToArray()) text.Remove();
        }
        if (name == "Name") e.SetAttributeValue("Name", null);
        e.SetAttributeValue(name == "Name" ? Xaml + "Name" : XName.Get(name), string.IsNullOrEmpty(value) ? null : value);
    }
    internal static XElement InsertionParent(XElement parent, string kind)
    {
        if (!WpfCatalog.IsContainer(parent)) throw new InvalidDataException("Select a container or the document root to add a control.");
        while (WpfCatalog.SingleContent(parent) && Children(parent).Any())
        {
            parent = Children(parent).First();
            if (!WpfCatalog.IsContainer(parent)) throw new InvalidDataException("This container already has content. Add a panel in XAML to hold multiple controls.");
        }
        if (SingleItemParent(parent, kind)) throw new InvalidDataException("Choose a matching item for this container, or select its content panel.");
        if (WpfCatalog.SingleContent(parent) && (parent.Attribute("Content") != null || parent.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value))))
            throw new InvalidDataException("This container already has content. Edit its content in XAML first.");
        if (parent.Attribute("ItemsSource") != null) throw new InvalidDataException("This control uses ItemsSource. Edit its items or binding in XAML.");
        return parent;
    }
    internal static XElement ContentTarget(XElement parent) => parent.Elements().FirstOrDefault(e =>
        e.Name.LocalName is "Panel.Children" || e.Name.LocalName == parent.Name.LocalName + ".Children" ||
        e.Name.LocalName == parent.Name.LocalName + ".Items" || e.Name.LocalName == parent.Name.LocalName + ".Content") ?? parent;
    internal XElement Add(string kind, XElement parent, double x, double y)
    {
        var spec = WpfCatalog.Controls.First(s => s.Kind == kind);
        parent = InsertionParent(parent, kind);
        var element = new XElement(Presentation + kind, new XAttribute(Xaml + "Name", UniqueName(kind)));
        if (double.IsFinite(spec.Width)) element.SetAttributeValue("Width", WpfCatalog.Format(spec.Width));
        if (double.IsFinite(spec.Height)) element.SetAttributeValue("Height", WpfCatalog.Format(spec.Height));
        if (spec.Content.Length > 0) element.SetAttributeValue(kind == "TextBlock" ? "Text" : kind is "GroupBox" or "TabItem" or "TreeViewItem" ? "Header" : "Content", spec.Content);
        if (parent.Name.LocalName == "Canvas") { element.SetAttributeValue("Canvas.Left", WpfCatalog.Format(x)); element.SetAttributeValue("Canvas.Top", WpfCatalog.Format(y)); }
        else if (parent.Name.LocalName is "Grid" or "Border" or "Window" or "Page" or "UserControl")
        { element.SetAttributeValue("HorizontalAlignment", "Left"); element.SetAttributeValue("VerticalAlignment", "Top"); element.SetAttributeValue("Margin", $"{WpfCatalog.Format(x)},{WpfCatalog.Format(y)},0,0"); }
        if (kind == "TabControl") element.Add(new XElement(Presentation + "TabItem", new XAttribute("Header", "Tab 1"), new XElement(Presentation + "Grid")));
        else if (kind is "GroupBox" or "ScrollViewer" or "TabItem") element.Add(new XElement(Presentation + "Grid"));
        else if (kind is "ComboBox" or "ListBox") element.Add(new XElement(Presentation + (kind + "Item"), new XAttribute("Content", "Item 1")));
        ContentTarget(parent).Add(new XText(Environment.NewLine + new string(' ', parent.Ancestors().Count() * 4 + 4)), element, new XText(Environment.NewLine + new string(' ', parent.Ancestors().Count() * 4)));
        return element;
    }
    private static bool SingleItemParent(XElement parent, string kind) => parent.Name.LocalName switch
    {
        "TabControl" => kind != "TabItem", "ComboBox" => kind != "ComboBoxItem", "ListBox" => kind != "ListBoxItem", "TreeView" or "TreeViewItem" => kind != "TreeViewItem", _ => false
    };
}
