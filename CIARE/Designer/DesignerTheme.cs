using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;

namespace CIARE.Designer;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed record DesignerTheme(Color Panel, Color Field, Color Text, Color Border, Color Accent, Color Selection, Color SelectionText)
{
    internal static DesignerTheme For(MainForm main)
    {
        string name = main.WindowThemeFeature._appliedTheme ?? "C#-Dark";
        if (GlobalVariables.darkColor != InitializeEditor.IsDarkTheme(name)) name = GlobalVariables.darkColor ? "C#-Dark" : "C#-Light";
        var colors = ThemeManager.GetCompletionThemeColors(name);
        return new(GlobalVariables.darkColor ? GlobalVariables.formBgColor : SystemColors.Control,
            GlobalVariables.darkColor ? GlobalVariables.controlBgColor : colors.BackColor,
            colors.ForeColor, colors.BorderColor, colors.AccentColor, colors.SelectionStartColor, colors.SelectionForeColor);
    }
    internal void ApplyStrip(ToolStrip strip)
    {
        strip.BackColor = Panel; strip.ForeColor = Text;
        strip.Renderer = new Renderer(this);
        foreach (ToolStripItem item in strip.Items) item.ForeColor = Text;
    }
    private sealed class Renderer(DesignerTheme theme) : ToolStripProfessionalRenderer(new Colors(theme))
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = !e.Item.Enabled ? SystemColors.GrayText : e.Item.Selected || e.Item.Pressed || e.Item is ToolStripButton { Checked: true } ? theme.SelectionText : theme.Text;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = theme.Text; base.OnRenderArrow(e); }
    }
    private sealed class Colors(DesignerTheme theme) : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => theme.Panel;
        public override Color ToolStripGradientMiddle => theme.Panel;
        public override Color ToolStripGradientEnd => theme.Panel;
        public override Color StatusStripGradientBegin => theme.Panel;
        public override Color StatusStripGradientEnd => theme.Panel;
        public override Color ToolStripBorder => theme.Border;
        public override Color SeparatorDark => theme.Border;
        public override Color SeparatorLight => theme.Panel;
        public override Color ButtonSelectedHighlight => theme.Selection;
        public override Color ButtonSelectedBorder => theme.Accent;
        public override Color ButtonSelectedGradientBegin => theme.Selection;
        public override Color ButtonSelectedGradientMiddle => theme.Selection;
        public override Color ButtonSelectedGradientEnd => theme.Selection;
        public override Color ButtonCheckedHighlight => theme.Selection;
        public override Color ButtonCheckedHighlightBorder => theme.Accent;
        public override Color ButtonCheckedGradientBegin => theme.Selection;
        public override Color ButtonCheckedGradientMiddle => theme.Selection;
        public override Color ButtonCheckedGradientEnd => theme.Selection;
        public override Color ButtonPressedBorder => theme.Accent;
        public override Color ButtonPressedGradientBegin => theme.Selection;
        public override Color ButtonPressedGradientMiddle => theme.Selection;
        public override Color ButtonPressedGradientEnd => theme.Selection;
        public override Color ToolStripDropDownBackground => theme.Panel;
        public override Color ImageMarginGradientBegin => theme.Panel;
        public override Color ImageMarginGradientMiddle => theme.Panel;
        public override Color ImageMarginGradientEnd => theme.Panel;
    }
}

// Native TabControl paints its header and frame using Windows colors even when BackColor is set.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class DesignerInspectorTabs : TabControl
{
    internal DesignerTheme Theme;
    internal DesignerInspectorTabs()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DrawMode = TabDrawMode.OwnerDrawFixed;
        Padding = new Point(14, 7);
    }
    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Theme?.Panel ?? SystemColors.Control);
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Theme == null) return;
        for (int i = 0; i < TabCount; i++)
        {
            var bounds = GetTabRect(i);
            bool selected = i == SelectedIndex;
            using var brush = new SolidBrush(selected ? Theme.Field : Theme.Panel);
            e.Graphics.FillRectangle(brush, bounds);
            TextRenderer.DrawText(e.Graphics, TabPages[i].Text, Font, bounds, Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (selected)
            {
                using var accent = new Pen(Theme.Accent, 2);
                e.Graphics.DrawLine(accent, bounds.Left + 2, bounds.Top + 1, bounds.Right - 2, bounds.Top + 1);
                if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), Theme.Text, Theme.Field);
            }
        }
        if (TabCount > 0)
        {
            var border = DisplayRectangle; border.Inflate(1, 1);
            using var pen = new Pen(Theme.Border); e.Graphics.DrawRectangle(pen, border);
        }
    }
    protected override void OnSelectedIndexChanged(System.EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
    protected override void OnGotFocus(System.EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(System.EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
