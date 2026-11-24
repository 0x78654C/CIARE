using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfPropertyView(WpfSession session, string filter = "") : CustomTypeDescriptor
{
    public override object GetPropertyOwner(PropertyDescriptor pd) => session;
    public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
    public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        var element = session.Selected;
        var properties = WpfCatalog.Properties(element).Select(p => new AttributeProperty(session, p.Name, p.Category, p.Type, p.Default)).ToList();
        properties.Insert(0, new AttributeProperty(session, "Name", "Design", typeof(string), ""));
        foreach (string property in new[] { "Canvas.Left", "Canvas.Top", "Grid.Row", "Grid.Column", "Grid.RowSpan", "Grid.ColumnSpan", "DockPanel.Dock" })
            if (element != session.Document.Root) properties.Add(new AttributeProperty(session, property, "Layout", typeof(string), property.EndsWith("Span") ? "1" : property == "DockPanel.Dock" ? "Left" : "0"));
        return new PropertyDescriptorCollection(properties.Where(p => p.Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase)).Cast<PropertyDescriptor>().ToArray());
    }
    private sealed class AttributeProperty : PropertyDescriptor
    {
        private readonly WpfSession _session;
        private readonly Type _type;
        private readonly object _default;
        internal AttributeProperty(WpfSession session, string name, string category, Type type, object fallback)
            : base(name, new Attribute[] { new CategoryAttribute(category), new DescriptionAttribute("WPF " + name + ". XAML expressions are preserved; clear or reset to use the default.") })
        { _session = session; _type = type; _default = fallback; }
        private XAttribute Attribute => _session.Selected.Attribute(Name == "Name" ? WpfDocument.Xaml + "Name" : XName.Get(Name)) ?? (Name == "Name" ? _session.Selected.Attribute("Name") : null);
        public override Type ComponentType => typeof(WpfSession);
        public override Type PropertyType => Attribute?.Value.StartsWith('{') == true ? typeof(string) : _type.IsEnum || _type == typeof(bool) || _type == typeof(int) ? _type : typeof(string);
        public override bool IsReadOnly => WpfDocument.PropertyElement(_session.Selected, Name) != null || Name is "Content" or "Text" && WpfDocument.Children(_session.Selected).Any();
        public override object GetValue(object component)
        {
            if (IsReadOnly && PropertyType == typeof(string)) return "(Edit in XAML)";
            string content = Name is "Content" or "Text" && !_session.Selected.HasElements ? _session.Selected.Value : "";
            string value = Attribute?.Value ?? (!string.IsNullOrWhiteSpace(content) ? content : WpfCatalog.Format(_default));
            return PropertyType == typeof(string) ? value : WpfCatalog.ConvertValue(PropertyType, value);
        }
        public override void SetValue(object component, object value)
        {
            string text = WpfCatalog.Format(value);
            if (text.Length > 0 && !text.StartsWith('{') && Name != "Name")
            {
                if (Name.StartsWith("Grid.")) { int n = int.Parse(text, CultureInfo.InvariantCulture); if (n < (Name.EndsWith("Span") ? 1 : 0) || n > 1000) throw new ArgumentException("Use a valid grid index or span."); }
                else if (Name.StartsWith("Canvas.")) { if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) || !double.IsFinite(n)) throw new ArgumentException("Use a numeric canvas position."); }
                else if (Name == "DockPanel.Dock") Enum.Parse<System.Windows.Controls.Dock>(text);
                else
                {
                    var parsed = WpfCatalog.ConvertValue(_type, text);
                    var controlType = WpfCatalog.TypeFor(_session.Selected);
                    var property = controlType?.GetProperty(Name);
                    if (property != null)
                    {
                        var instance = Activator.CreateInstance(controlType);
                        try { property.SetValue(instance, parsed); }
                        catch (System.Reflection.TargetInvocationException error) { throw new ArgumentException(error.InnerException?.Message ?? error.Message); }
                        finally { if (instance is System.Windows.Window window) window.Close(); }
                    }
                }
            }
            _session.Change(() =>
            {
                WpfDocument.Set(_session.Selected, Name, text);
            });
        }
        public override bool CanResetValue(object component) => Attribute != null;
        public override void ResetValue(object component) => SetValue(component, "");
        public override bool ShouldSerializeValue(object component) => Attribute != null;
    }
}
