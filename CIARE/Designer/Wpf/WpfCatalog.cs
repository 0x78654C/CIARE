using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

internal sealed record WpfControlSpec(string Kind, string Category, Type Type, double Width, double Height, string Content = "");
internal sealed record WpfPropertySpec(string Name, string Category, Type Type, object Default);

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class WpfCatalog
{
    internal static readonly WpfControlSpec[] Controls =
    {
        new("Button", "Common Controls", typeof(Button), 110, 32, "Button"),
        new("TextBlock", "Common Controls", typeof(TextBlock), 160, 28, "Text"),
        new("Label", "Common Controls", typeof(Label), 120, 30, "Label"),
        new("TextBox", "Common Controls", typeof(TextBox), 180, 30),
        new("PasswordBox", "Common Controls", typeof(PasswordBox), 180, 30),
        new("CheckBox", "Common Controls", typeof(CheckBox), 160, 28, "CheckBox"),
        new("RadioButton", "Common Controls", typeof(RadioButton), 160, 28, "RadioButton"),
        new("ComboBox", "Common Controls", typeof(ComboBox), 180, 30),
        new("ListBox", "Common Controls", typeof(ListBox), 200, 130),
        new("ListView", "Common Controls", typeof(ListView), 240, 150),
        new("TreeView", "Common Controls", typeof(TreeView), 200, 170),
        new("DatePicker", "Common Controls", typeof(DatePicker), 180, 30),
        new("Calendar", "Common Controls", typeof(Calendar), 230, 190),
        new("Slider", "Common Controls", typeof(Slider), 200, 30),
        new("ProgressBar", "Common Controls", typeof(ProgressBar), 200, 24),
        new("Image", "Display", typeof(Image), 150, 110),
        new("Separator", "Display", typeof(Separator), 200, 8),
        new("DataGrid", "Data", typeof(DataGrid), 320, 180),
        new("Grid", "Containers", typeof(Grid), 300, 200),
        new("Canvas", "Containers", typeof(Canvas), 300, 200),
        new("StackPanel", "Containers", typeof(StackPanel), 260, 180),
        new("WrapPanel", "Containers", typeof(WrapPanel), 260, 180),
        new("DockPanel", "Containers", typeof(DockPanel), 300, 200),
        new("Border", "Containers", typeof(Border), 280, 180),
        new("GroupBox", "Containers", typeof(GroupBox), 280, 180, "Group"),
        new("ScrollViewer", "Containers", typeof(ScrollViewer), 280, 180),
        new("TabControl", "Containers", typeof(TabControl), 320, 220),
        new("TabItem", "Items", typeof(TabItem), double.NaN, double.NaN, "Tab"),
        new("ComboBoxItem", "Items", typeof(ComboBoxItem), double.NaN, double.NaN, "Item"),
        new("ListBoxItem", "Items", typeof(ListBoxItem), double.NaN, double.NaN, "Item"),
        new("TreeViewItem", "Items", typeof(TreeViewItem), double.NaN, double.NaN, "Item")
    };
    internal static Type TypeFor(XElement element) => element.Name.Namespace != WpfDocument.Presentation ? null : element.Name.LocalName switch
    {
        "Window" => typeof(Window), "UserControl" => typeof(UserControl), "Page" => typeof(Page),
        var kind => Controls.FirstOrDefault(s => s.Kind == kind)?.Type
    };
    internal static bool IsContainer(XElement element) => element?.Name.Namespace == WpfDocument.Presentation && element.Name.LocalName is
        "Window" or "UserControl" or "Page" or "Grid" or "Canvas" or "StackPanel" or "WrapPanel" or "DockPanel" or "Border" or "GroupBox" or "ScrollViewer" or "TabControl" or "TabItem" or "ComboBox" or "ListBox" or "ListView" or "TreeView" or "TreeViewItem";
    internal static bool SingleContent(XElement e) => e.Name.LocalName is "Window" or "UserControl" or "Page" or "Border" or "GroupBox" or "ScrollViewer" or "TabItem";
    internal static IEnumerable<EventSpec> Events(XElement e) => (TypeFor(e)?.GetEvents() ?? Array.Empty<System.Reflection.EventInfo>())
        .Where(v => !Attribute.IsDefined(v, typeof(ObsoleteAttribute)))
        .Select(v => (Event: v, Args: v.EventHandlerType?.GetMethod("Invoke")?.GetParameters()))
        .Where(v => v.Args?.Length == 2 && v.Args[0].ParameterType == typeof(object) && typeof(EventArgs).IsAssignableFrom(v.Args[1].ParameterType))
        .Select(v => new EventSpec(v.Event.Name, TypeName(v.Args[1].ParameterType))).OrderBy(v => v.Name);
    private static string TypeName(Type type) => type.IsGenericType
        ? type.GetGenericTypeDefinition().FullName.Split('`')[0].Replace('+', '.') + "<" + string.Join(", ", type.GetGenericArguments().Select(t => "global::" + TypeName(t))) + ">"
        : type.FullName.Replace('+', '.');
    internal static string DefaultEvent(XElement e) => e.Name.LocalName switch
    {
        "TextBox" => "TextChanged", "PasswordBox" => "PasswordChanged", "CheckBox" or "RadioButton" => "Checked",
        "ComboBox" or "ListBox" or "ListView" or "TabControl" or "DataGrid" => "SelectionChanged", "TreeView" => "SelectedItemChanged",
        "Slider" => "ValueChanged", "DatePicker" => "SelectedDateChanged", "Calendar" => "SelectedDatesChanged",
        "Button" => "Click", _ => "Loaded"
    };
    private static readonly string[] Editable =
    {
        "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight", "Margin", "Padding", "HorizontalAlignment", "VerticalAlignment",
        "HorizontalContentAlignment", "VerticalContentAlignment", "Background", "Foreground", "BorderBrush", "BorderThickness", "CornerRadius",
        "FontFamily", "FontSize", "FontWeight", "FontStyle", "Text", "Content", "Header", "Title", "IsEnabled", "Visibility", "Opacity", "ToolTip",
        "TextWrapping", "TextAlignment", "AcceptsReturn", "AcceptsTab", "IsReadOnly", "MaxLength", "IsChecked", "IsThreeState", "IsDefault", "IsCancel",
        "Orientation", "LastChildFill", "ItemWidth", "ItemHeight", "Minimum", "Maximum", "Value", "SmallChange", "LargeChange",
        "IsIndeterminate", "IsEditable", "SelectedIndex", "SelectionMode", "AutoGenerateColumns", "CanUserAddRows", "CanUserDeleteRows", "CanUserSortColumns",
        "HorizontalScrollBarVisibility", "VerticalScrollBarVisibility", "Stretch", "FlowDirection", "WindowStartupLocation", "ResizeMode", "ShowInTaskbar"
    };
    private static readonly Dictionary<Type, WpfPropertySpec[]> PropertyCache = new();
    internal static WpfPropertySpec[] Properties(XElement element)
    {
        Type type = TypeFor(element);
        if (type == null) return Array.Empty<WpfPropertySpec>();
        if (PropertyCache.TryGetValue(type, out var cached)) return cached;
        object instance = Activator.CreateInstance(type);
        try
        {
            cached = Editable.Select(name => type.GetProperty(name)).Where(p => p?.CanWrite == true && p.GetIndexParameters().Length == 0)
                .Where(p => p.PropertyType.IsEnum || p.PropertyType == typeof(string) || p.PropertyType == typeof(object) || p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?) ||
                    p.PropertyType == typeof(int) || p.PropertyType == typeof(double) || p.PropertyType == typeof(Thickness) || p.PropertyType == typeof(CornerRadius) ||
                    p.PropertyType == typeof(Brush) || p.PropertyType == typeof(FontFamily) || p.PropertyType == typeof(FontWeight) || p.PropertyType == typeof(FontStyle))
                .Select(p => new WpfPropertySpec(p.Name,
                    p.Name.Contains("Width") || p.Name.Contains("Height") || p.Name.Contains("Alignment") || p.Name is "Margin" or "Padding" or "Orientation" ? "Layout" :
                    p.Name.StartsWith("Is") || p.Name.StartsWith("Can") || p.Name is "Visibility" or "SelectedIndex" or "SelectionMode" ? "Behavior" : "Appearance",
                    p.PropertyType, p.GetValue(instance))).ToArray();
            PropertyCache[type] = cached; return cached;
        }
        finally { if (instance is Window window) window.Close(); }
    }
    internal static object ConvertValue(Type type, string value)
    {
        if (type == typeof(string) || type == typeof(object)) return value;
        if (type == typeof(bool?)) return bool.Parse(value);
        if (type == typeof(double) && value == "Auto") return double.NaN;
        return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);
    }
    internal static string Format(object value) => value switch
    {
        null => "", double number when double.IsNaN(number) => "Auto", bool boolean => boolean ? "True" : "False",
        IFormattable formatted => formatted.ToString(null, System.Globalization.CultureInfo.InvariantCulture), _ => value.ToString()
    };
}
