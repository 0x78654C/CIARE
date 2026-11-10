using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignNode
{
    [Browsable(false)] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Category("Design"), ReadOnly(true)] public string Kind { get; set; } = "Button";
    [Category("Design"), DisplayName("(Name)")] public string Name { get; set; } = "button1";
    [Category("Design")] public string Namespace { get; set; } = "MyApplication";
    [Category("Appearance")] public string Text { get; set; } = "Button";
    [Browsable(false)] public int X { get; set; } = 24;
    [Browsable(false)] public int Y { get; set; } = 24;
    [Browsable(false)] public int Width { get; set; } = 100;
    [Browsable(false)] public int Height { get; set; } = 32;
    [JsonIgnore, Category("Layout")] public Point Location { get => new(X, Y); set { X = value.X; Y = value.Y; } }
    [JsonIgnore, Category("Layout")] public Size Size { get => new(Width, Height); set { Width = value.Width; Height = value.Height; } }
    [Category("Layout")] public AnchorStyles Anchor { get; set; } = AnchorStyles.Top | AnchorStyles.Left;
    [Category("Layout")] public DockStyle Dock { get; set; }
    [Category("Behavior")] public bool Enabled { get; set; } = true;
    [Category("Behavior")] public bool Visible { get; set; } = true;
    [Category("Behavior")] public int TabIndex { get; set; }
    [Browsable(false)] public int BackColorArgb { get; set; } = SystemColors.Control.ToArgb();
    [Browsable(false)] public int ForeColorArgb { get; set; } = SystemColors.ControlText.ToArgb();
    [JsonIgnore, Category("Appearance")] public Color BackColor { get => Color.FromArgb(BackColorArgb); set => BackColorArgb = value.ToArgb(); }
    [JsonIgnore, Category("Appearance")] public Color ForeColor { get => Color.FromArgb(ForeColorArgb); set => ForeColorArgb = value.ToArgb(); }
    [Category("Appearance")] public float FontSize { get; set; } = 9;
    [Category("Behavior")] public bool Checked { get; set; }
    [Category("Behavior")] public bool Multiline { get; set; }
    [Category("Data"), Description("One item per line.")]
    public string[] Items { get; set; } = Array.Empty<string>();
    [Category("Data")] public int Minimum { get; set; }
    [Category("Data")] public int Maximum { get; set; } = 100;
    [Category("Data")] public int Value { get; set; }
    [Category("Data"), DisplayName("Value")] public DateTime Date { get; set; } = DateTime.Today;
    [Browsable(false)] public List<DesignNode> Children { get; set; } = new();
    [Browsable(false)] public Dictionary<string, string> Events { get; set; } = new();
    [JsonIgnore, Browsable(false)] public bool IsContainer => Kind is "Form" or "Panel" or "GroupBox";
    internal Rectangle Bounds { get => new(Location, Size); set { Location = value.Location; Size = value.Size; } }
    public override string ToString() => $"{Name}  :  {Kind}";
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignDocument
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        MaxDepth = 48
    };
    public int Version { get; set; } = 1;
    public DesignNode Form { get; set; } = new() { Kind = "Form", Name = "Form1", Text = "Form1", X = 0, Y = 0, Width = 640, Height = 420 };
    internal IEnumerable<DesignNode> AllNodes => Enumerate(Form);
    internal static IEnumerable<DesignNode> Enumerate(DesignNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Enumerate(child)) yield return descendant;
    }
    internal DesignNode Find(string id) => AllNodes.FirstOrDefault(n => n.Id == id);
    internal DesignNode ParentOf(DesignNode child) => AllNodes.FirstOrDefault(n => n.Children.Contains(child));
    internal string Serialize() => JsonSerializer.Serialize(this, JsonOptions);
    internal static DesignDocument Parse(string json)
    {
        var document = JsonSerializer.Deserialize<DesignDocument>(json, JsonOptions) ?? throw new InvalidDataException("The form layout is empty.");
        document.Validate();
        return document;
    }
    internal static DesignDocument Load(string path)
    {
        if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("The form layout is too large (maximum 4 MB).");
        return Parse(File.ReadAllText(path));
    }
    internal static void WriteAtomic(string path, string content)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, content, new System.Text.UTF8Encoding(false)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    internal string UniqueName(string kind)
    {
        string prefix = char.ToLowerInvariant(kind[0]) + kind.Substring(1);
        int index = 1;
        while (AllNodes.Any(n => n.Name == prefix + index || n.Events.Values.Contains(prefix + index))) index++;
        return prefix + index;
    }
    internal static bool IsIdentifier(string name) => !string.IsNullOrWhiteSpace(name) &&
        SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && !name.StartsWith("@");
    private static readonly HashSet<string> Reserved = typeof(Form).GetMembers().Select(m => m.Name)
        .Concat(new[] { "InitializeComponent", "Dispose" }).ToHashSet(StringComparer.Ordinal);

    internal void Validate()
    {
        if (Version != 1 || Form == null || Form.Kind != "Form") throw new InvalidDataException("This is not a supported CIARE form layout.");
        if (string.IsNullOrWhiteSpace(Form.Namespace) || !Form.Namespace.Split('.').All(IsIdentifier))
            throw new InvalidDataException("Namespace must contain valid C# identifiers separated by dots.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        void Check(DesignNode node, int depth)
        {
            if (node == null || ++count > 250 || depth > 16) throw new InvalidDataException("Layouts support up to 250 components and 16 nesting levels.");
            if (!IsIdentifier(node.Name) || Reserved.Contains(node.Name) || !names.Add(node.Name))
                throw new InvalidDataException("Component names must be unique C# identifiers and must not hide Form members.");
            if (string.IsNullOrEmpty(node.Id) || !ids.Add(node.Id)) throw new InvalidDataException("Component IDs must be unique.");
            if (node != Form && !DesignCatalog.Controls.Any(c => c.Kind == node.Kind)) throw new InvalidDataException("Unsupported control: " + node.Kind);
            if (node.Width < 16 || node.Height < 16 || node.Width > 2400 || node.Height > 1800 || node.X < 0 || node.Y < 0 || node.X > 2400 || node.Y > 1800)
                throw new InvalidDataException("Use a size from 16 × 16 to 2400 × 1800 and a nonnegative location within 2400 × 1800.");
            if (!float.IsFinite(node.FontSize) || node.FontSize < 6 || node.FontSize > 72 || node.TabIndex < 0 || node.TabIndex > 32767)
                throw new InvalidDataException("Font size must be 6–72 pt and TabIndex must be 0–32767.");
            if (node.BackColor.A != 255 || node.ForeColor.A != 255) throw new InvalidDataException("Use opaque colors for controls.");
            if (!Enum.IsDefined(node.Dock) || ((int)node.Anchor & ~15) != 0) throw new InvalidDataException("Invalid layout setting.");
            if (node.Minimum < 0 || node.Maximum < node.Minimum || node.Value < node.Minimum || node.Value > node.Maximum || node.Maximum > 1000000)
                throw new InvalidDataException("Use 0 ≤ Minimum ≤ Value ≤ Maximum ≤ 1,000,000.");
            if (node.Kind == "DateTimePicker" && (node.Date < DateTimePicker.MinimumDateTime || node.Date > DateTimePicker.MaximumDateTime))
                throw new InvalidDataException("Date is outside the supported calendar range.");
            if (node.Children == null || node.Events == null || node.Items == null || node.Items.Length > 1000 || node.Items.Any(i => i == null))
                throw new InvalidDataException("Invalid control data.");
            if (node.Children.Count > 0 && !node.IsContainer) throw new InvalidDataException("Only forms, panels, and group boxes can contain controls.");
            foreach (var child in node.Children) Check(child, depth + 1);
        }
        Check(Form, 0);
        var signatures = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in AllNodes)
        {
            foreach (var binding in node.Events)
            {
                var spec = DesignCatalog.EventsFor(node.Kind).FirstOrDefault(e => e.Name == binding.Key);
                if (spec == null || !IsIdentifier(binding.Value) || names.Contains(binding.Value) || Reserved.Contains(binding.Value))
                    throw new InvalidDataException("Event handlers must be valid C# names, distinct from component and Form members.");
                if (signatures.TryGetValue(binding.Value, out var type) && type != spec.ArgumentType)
                    throw new InvalidDataException("A shared event handler must use the same event argument type.");
                signatures[binding.Value] = spec.ArgumentType;
            }
        }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignPropertyView : CustomTypeDescriptor
{
    private readonly DesignNode _node;
    internal DesignPropertyView(DesignNode node) { _node = node; }
    public override object GetPropertyOwner(PropertyDescriptor pd) => _node;
    public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
    public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        bool Show(PropertyDescriptor p) => p.IsBrowsable && (p.Name switch
        {
            "Namespace" => _node.Kind == "Form",
            "Visible" => _node.Kind != "Form",
            "Text" => _node.Kind is "Form" or "Button" or "Label" or "TextBox" or "CheckBox" or "RadioButton" or "ComboBox" or "GroupBox" or "RichTextBox" or "LinkLabel",
            "Location" or "Anchor" or "Dock" or "TabIndex" => _node.Kind != "Form",
            "Checked" => _node.Kind is "CheckBox" or "RadioButton",
            "Multiline" => _node.Kind == "TextBox",
            "Items" => _node.Kind is "ComboBox" or "ListBox",
            "Minimum" or "Maximum" or "Value" => _node.Kind is "NumericUpDown" or "ProgressBar" or "TrackBar",
            "Date" => _node.Kind == "DateTimePicker",
            _ => true
        });
        return new PropertyDescriptorCollection(TypeDescriptor.GetProperties(typeof(DesignNode)).Cast<PropertyDescriptor>().Where(Show).ToArray());
    }
}
