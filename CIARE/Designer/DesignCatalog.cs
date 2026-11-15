using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CIARE.Designer;

internal sealed record ControlSpec(string Kind, string Category, int Width, int Height, string Glyph);
internal sealed record EventSpec(string Name, string ArgumentType);

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class DesignCatalog
{
    internal static readonly ControlSpec[] Controls =
    {
        new("Button", "Common Controls", 104, 32, "▣"), new("Label", "Common Controls", 104, 24, "A"),
        new("TextBox", "Common Controls", 160, 28, "ab"), new("CheckBox", "Common Controls", 136, 24, "☑"),
        new("RadioButton", "Common Controls", 136, 24, "◉"), new("ComboBox", "Common Controls", 160, 28, "▾"),
        new("ListBox", "Common Controls", 160, 112, "≡"), new("DateTimePicker", "Common Controls", 216, 28, "▦"),
        new("NumericUpDown", "Common Controls", 120, 28, "↕"), new("PictureBox", "Common Controls", 128, 96, "▧"),
        new("RichTextBox", "Common Controls", 200, 112, "¶"), new("LinkLabel", "Common Controls", 120, 24, "↗"),
        new("Panel", "Containers", 256, 176, "□"), new("GroupBox", "Containers", 256, 176, "▤"),
        new("TabControl", "Containers", 320, 220, "▤"), new("TabPage", "Containers", 300, 190, "□"),
        new("FlowLayoutPanel", "Containers", 256, 176, "⇢"), new("TableLayoutPanel", "Containers", 256, 176, "▦"),
        new("ProgressBar", "Display", 200, 24, "▬"), new("TrackBar", "Display", 200, 48, "↔"),
        new("TreeView", "Display", 176, 160, "⊞"), new("ListView", "Display", 224, 160, "▥")
    };
    internal static DesignNode Create(string kind, string name, Point location)
    {
        var spec = Controls.First(c => c.Kind == kind);
        return new DesignNode
        {
            Kind = kind, Name = name, Text = kind is "TextBox" or "RichTextBox" or "ComboBox" ? "" : name,
            Location = location, Width = spec.Width, Height = spec.Height,
            BackColor = kind is "TextBox" or "RichTextBox" or "ComboBox" or "ListBox" or "TreeView" or "ListView" or "NumericUpDown"
                ? SystemColors.Window : SystemColors.Control
        };
    }
    internal static Type ControlType(string kind) => typeof(Control).Assembly.GetType("System.Windows.Forms." + kind) ?? typeof(Control);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, EventSpec[]> EventCache = new();
    internal static IEnumerable<EventSpec> EventsFor(string kind) => EventCache.GetOrAdd(kind, key =>
        ControlType(key).GetEvents().Where(e => !Attribute.IsDefined(e, typeof(ObsoleteAttribute)))
            .Select(e => (Event: e, Parameters: e.EventHandlerType?.GetMethod("Invoke")?.GetParameters()))
            .Where(e => e.Parameters?.Length == 2 && e.Parameters[0].ParameterType == typeof(object) &&
                typeof(EventArgs).IsAssignableFrom(e.Parameters[1].ParameterType) && !e.Parameters[1].ParameterType.IsGenericType)
            .Select(e => new EventSpec(e.Event.Name, e.Parameters[1].ParameterType.FullName.Replace('+', '.')))
            .OrderBy(e => e.Name).ToArray());
    internal static string DefaultEvent(string kind) => kind switch
    {
        "Form" => "Load", "TextBox" or "RichTextBox" => "TextChanged", "CheckBox" or "RadioButton" => "CheckedChanged",
        "ComboBox" or "ListBox" or "ListView" => "SelectedIndexChanged", "TreeView" => "AfterSelect", "LinkLabel" => "LinkClicked",
        "NumericUpDown" or "DateTimePicker" or "TrackBar" => "ValueChanged", _ => "Click"
    };

    internal static Control CreateControl(DesignNode node, bool designing)
    {
        Control control = node.Kind switch
        {
            "Form" => new Panel(), "Button" => new Button(), "Label" => new Label(), "TextBox" => new TextBox { AutoSize = false, Multiline = node.Multiline },
            "CheckBox" => new CheckBox { Checked = node.Checked }, "RadioButton" => new RadioButton { Checked = node.Checked },
            "ComboBox" => new ComboBox(), "ListBox" => new ListBox { IntegralHeight = false }, "DateTimePicker" => new DateTimePicker { Value = node.Date },
            "NumericUpDown" => new NumericUpDown { Minimum = node.Minimum, Maximum = node.Maximum, Value = node.Value },
            "PictureBox" => new PictureBox { BorderStyle = BorderStyle.FixedSingle }, "RichTextBox" => new RichTextBox(),
            "LinkLabel" => new LinkLabel(), "Panel" => new Panel(), "GroupBox" => new GroupBox(),
            "TabControl" => new TabControl(), "TabPage" => new TabPage(),
            "FlowLayoutPanel" => new FlowLayoutPanel(), "TableLayoutPanel" => new TableLayoutPanel(),
            "ProgressBar" => new ProgressBar { Minimum = node.Minimum, Maximum = node.Maximum, Value = node.Value },
            "TrackBar" => new TrackBar { Minimum = node.Minimum, Maximum = node.Maximum, Value = node.Value, AutoSize = false },
            "TreeView" => new TreeView(), "ListView" => new ListView(), _ => throw new InvalidOperationException("Unsupported control.")
        };
        try
        {
            control.SuspendLayout();
            control.Name = node.Name;
            if (node.Kind is not ("DateTimePicker" or "NumericUpDown")) control.Text = node.Text ?? "";
            control.Bounds = node.Bounds;
            control.BackColor = node.BackColor;
            control.ForeColor = node.ForeColor;
            var font = new Font(node.FontFamily, node.FontSize, node.FontStyle);
            control.Font = font;
            control.Disposed += (_, _) => font.Dispose();
            control.TabIndex = node.TabIndex;
            control.Enabled = designing || node.Enabled;
            control.Visible = designing || node.Visible;
            if (designing && node.SourceType.Length > 0)
            {
                control.Paint += (_, e) =>
                {
                    using var pen = new Pen(Color.Gray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                    e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, control.Width - 1), Math.Max(0, control.Height - 1));
                    TextRenderer.DrawText(e.Graphics, node.SourceType, control.Font, control.ClientRectangle, Color.DimGray,
                        TextFormatFlags.Top | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                };
            }
            if (control is ComboBox combo) combo.Items.AddRange(node.Items);
            if (control is ListBox list) list.Items.AddRange(node.Items);
            DesignOptions.Apply(node, control);
            foreach (var child in node.Children) control.Controls.Add(CreateControl(child, designing));
            control.ResumeLayout(true);
            control.Anchor = node.Anchor;
            control.Dock = node.Dock;
            return control;
        }
        catch { control.Dispose(); throw; }
    }
    internal static Form CreatePreview(DesignDocument document)
    {
        var root = document.Form;
        var form = new Form { Text = root.Text, Name = root.Name, ClientSize = root.Size, BackColor = root.BackColor,
            ForeColor = root.ForeColor, Enabled = root.Enabled, StartPosition = FormStartPosition.CenterParent, AutoScaleMode = AutoScaleMode.None };
        var font = new Font(root.FontFamily, root.FontSize, root.FontStyle);
        form.Font = font;
        form.Disposed += (_, _) => font.Dispose();
        DesignOptions.Apply(root, form);
        foreach (var child in root.Children) form.Controls.Add(CreateControl(child, false));
        return form;
    }
}
