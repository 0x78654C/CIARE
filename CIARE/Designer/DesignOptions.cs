using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE.Designer;

// A curated set of native properties, with the native converters and editors used by PropertyGrid.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class DesignOptions
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "AutoSize", "AutoSizeMode", "TabStop", "AllowDrop", "CausesValidation", "RightToLeft", "Cursor",
        "AccessibleName", "AccessibleDescription", "AccessibleRole", "MinimumSize", "MaximumSize", "Margin", "Padding",
        "AutoEllipsis", "TextAlign", "ImageAlign", "FlatStyle", "BorderStyle", "UseVisualStyleBackColor",
        "ReadOnly", "MaxLength", "UseSystemPasswordChar", "PasswordChar", "AcceptsReturn", "AcceptsTab", "ScrollBars", "WordWrap", "CharacterCasing",
        "DropDownStyle", "MaxDropDownItems", "DropDownWidth", "Sorted", "IntegralHeight", "SelectionMode",
        "CheckBoxes", "View", "FullRowSelect", "GridLines", "HideSelection", "MultiSelect", "LabelEdit", "ShowLines", "ShowRootLines", "ShowPlusMinus",
        "DecimalPlaces", "Increment", "ThousandsSeparator", "Hexadecimal", "TickFrequency", "LargeChange", "SmallChange", "TickStyle", "Orientation",
        "SizeMode", "Style", "MarqueeAnimationSpeed", "AutoScroll", "FlowDirection", "WrapContents", "RowCount", "ColumnCount",
        "Format", "CustomFormat", "ShowCheckBox", "ShowUpDown", "DetectUrls", "ZoomFactor",
        "FormBorderStyle", "StartPosition", "ControlBox", "MaximizeBox", "MinimizeBox", "ShowIcon", "ShowInTaskbar", "KeyPreview", "TopMost"
    };
    private sealed record Option(PropertyDescriptor Property, object Default);
    private static readonly ConcurrentDictionary<string, Option[]> Cache = new();
    private static Option[] For(string kind) => Cache.GetOrAdd(kind, key =>
    {
        using var control = (Control)Activator.CreateInstance(DesignCatalog.ControlType(key));
        return TypeDescriptor.GetProperties(control).Cast<PropertyDescriptor>()
            .Where(p => Names.Contains(p.Name) && !p.IsReadOnly && p.IsBrowsable)
            .Where(p => p.PropertyType.IsEnum || p.PropertyType == typeof(string) || p.PropertyType == typeof(bool) ||
                p.PropertyType == typeof(int) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(float) ||
                p.PropertyType == typeof(char) || p.PropertyType == typeof(Size) || p.PropertyType == typeof(Padding) || p.PropertyType == typeof(Cursor))
            .Select(p => new Option(p, DefaultValue(key, p, control))).ToArray();
    });
    private static object DefaultValue(string kind, PropertyDescriptor property, Control control) => property.Name switch
    {
        "AutoSize" => false,
        "IntegralHeight" when kind == "ListBox" => false,
        "BorderStyle" when kind == "PictureBox" => BorderStyle.FixedSingle,
        "StartPosition" when kind == "Form" => FormStartPosition.CenterParent,
        _ => property.GetValue(control)
    };
    internal static IEnumerable<PropertyDescriptor> Descriptors(DesignNode node) => For(node.Kind).Select(o => new OptionDescriptor(o));
    private static object Read(DesignNode node, Option option) => node.Options.TryGetValue(option.Property.Name, out string value)
        ? option.Property.Converter.ConvertFromInvariantString(value) : option.Default;
    private static string Encode(Option option, object value) => option.Property.Converter.ConvertToInvariantString(value) ?? "";
    internal static void Validate(DesignNode node)
    {
        if (node.Options == null) throw new InvalidDataException("Control options cannot be empty.");
        foreach (var entry in node.Options)
        {
            var option = For(node.Kind).FirstOrDefault(p => p.Property.Name == entry.Key);
            if (option == null || entry.Value == null || entry.Value.Length > 8192) throw new InvalidDataException("Unsupported control property: " + entry.Key);
            var value = Read(node, option);
            Type type = option.Property.PropertyType;
            if (type.IsEnum && !Enum.IsDefined(type, value)) throw new InvalidDataException("Choose a valid " + entry.Key + " value.");
            if (value is int number)
            {
                int minimum = entry.Key is "MaxDropDownItems" or "DropDownWidth" ? 1 : 0;
                int maximum = entry.Key switch { "MaxLength" => int.MaxValue, "MaxDropDownItems" => 100, "DecimalPlaces" => 99, "RowCount" or "ColumnCount" => 1000, _ => 1000000 };
                if (number < minimum || number > maximum) throw new InvalidDataException($"{entry.Key} must be between {minimum} and {maximum}.");
            }
            if (value is decimal increment && (increment < 0 || increment > 1000000)) throw new InvalidDataException("Increment must be between 0 and 1,000,000.");
            if (value is float zoom && (!float.IsFinite(zoom) || zoom < 0.015625F || zoom > 64)) throw new InvalidDataException("ZoomFactor must be between 0.015625 and 64.");
            if (value is Size size && (size.Width < 0 || size.Height < 0 || size.Width > 4000 || size.Height > 3000)) throw new InvalidDataException("Minimum/maximum size must be within 4000 × 3000.");
            if (value is Padding padding && new[] { padding.Left, padding.Top, padding.Right, padding.Bottom }.Any(n => n < 0 || n > 1024))
                throw new InvalidDataException("Margins and padding must be between 0 and 1024.");
        }
        var minimumSize = For(node.Kind).FirstOrDefault(o => o.Property.Name == "MinimumSize");
        var maximumSize = For(node.Kind).FirstOrDefault(o => o.Property.Name == "MaximumSize");
        if (minimumSize != null && maximumSize != null)
        {
            var min = (Size)Read(node, minimumSize); var max = (Size)Read(node, maximumSize);
            if (max.Width > 0 && min.Width > max.Width || max.Height > 0 && min.Height > max.Height)
                throw new InvalidDataException("MinimumSize cannot exceed MaximumSize.");
        }
    }
    internal static void Apply(DesignNode node, Control control)
    {
        foreach (var option in For(node.Kind))
        {
            if (!node.Options.ContainsKey(option.Property.Name)) continue;
            // A form is represented by a panel on the drawing surface; window-only settings apply to Preview.
            var actual = TypeDescriptor.GetProperties(control)[option.Property.Name];
            if (actual != null && !actual.IsReadOnly && actual.PropertyType == option.Property.PropertyType) actual.SetValue(control, Read(node, option));
        }
    }
    internal static Dictionary<string, string> Expressions(DesignNode node, bool includeDefaults = false)
    {
        return For(node.Kind).Where(o => includeDefaults || node.Options.ContainsKey(o.Property.Name))
            .ToDictionary(o => o.Property.Name, o => Expression(o.Property.PropertyType, Read(node, o)), StringComparer.Ordinal);
    }
    private static string Expression(Type type, object value)
    {
        if (type.IsEnum) return $"global::{type.FullName}.{value}";
        return value switch
        {
            null => "null",
            string text => SymbolDisplay.FormatLiteral(text, true),
            char character => SymbolDisplay.FormatLiteral(character, true),
            bool flag => flag ? "true" : "false",
            int number => number.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture) + "M",
            float number => number.ToString(CultureInfo.InvariantCulture) + "F",
            Size size => $"new global::System.Drawing.Size({size.Width}, {size.Height})",
            Padding p => $"new global::System.Windows.Forms.Padding({p.Left}, {p.Top}, {p.Right}, {p.Bottom})",
            Cursor cursor => $"global::System.Windows.Forms.Cursors.{TypeDescriptor.GetConverter(typeof(Cursor)).ConvertToInvariantString(cursor)}",
            _ => throw new InvalidDataException("Unsupported property value.")
        };
    }
    internal static bool TryRead(DesignNode node, string name, ExpressionSyntax syntax)
    {
        var option = For(node.Kind).FirstOrDefault(o => o.Property.Name == name);
        if (option == null) return false;
        while (syntax is CastExpressionSyntax cast) syntax = cast.Expression;
        string text;
        if (syntax is LiteralExpressionSyntax literal) text = literal.Token.Value is IFormattable formatted
            ? formatted.ToString(null, CultureInfo.InvariantCulture) : literal.Token.Value?.ToString() ?? "";
        else if (syntax is MemberAccessExpressionSyntax member && (option.Property.PropertyType.IsEnum || option.Property.PropertyType == typeof(Cursor)))
            text = member.Name.Identifier.ValueText;
        else if (syntax is BaseObjectCreationExpressionSyntax creation && creation.ArgumentList != null &&
            (option.Property.PropertyType == typeof(Size) || option.Property.PropertyType == typeof(Padding)) &&
            creation.ArgumentList.Arguments.All(a => a.Expression is LiteralExpressionSyntax))
            text = string.Join(", ", creation.ArgumentList.Arguments.Select(a => ((LiteralExpressionSyntax)a.Expression).Token.ValueText));
        else return false;
        try
        {
            object value = option.Property.Converter.ConvertFromInvariantString(text);
            node.Options[name] = Encode(option, value);
            return true;
        }
        catch (Exception error) when (error is ArgumentException or FormatException or NotSupportedException) { return false; }
    }
    private sealed class OptionDescriptor : PropertyDescriptor
    {
        private readonly Option _option;
        internal OptionDescriptor(Option option) : base(option.Property.Name, option.Property.Attributes.Cast<Attribute>().ToArray()) { _option = option; }
        public override Type ComponentType => typeof(DesignNode);
        public override Type PropertyType => _option.Property.PropertyType;
        public override bool IsReadOnly => false;
        public override TypeConverter Converter => _option.Property.Converter;
        public override object GetEditor(Type editorBaseType) => _option.Property.GetEditor(editorBaseType);
        public override object GetValue(object component) => Read((DesignNode)component, _option);
        public override void SetValue(object component, object value)
        {
            var node = (DesignNode)component;
            node.Options[Name] = Encode(_option, value);
            OnValueChanged(component, EventArgs.Empty);
        }
        public override bool CanResetValue(object component) => ((DesignNode)component).Options.ContainsKey(Name);
        public override void ResetValue(object component) { ((DesignNode)component).Options.Remove(Name); OnValueChanged(component, EventArgs.Empty); }
        public override bool ShouldSerializeValue(object component) => CanResetValue(component);
    }
}
