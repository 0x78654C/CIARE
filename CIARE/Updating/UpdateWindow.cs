using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Updating;

[SupportedOSPlatform("windows")]
internal class UpdateWindow : Form
{
    private static readonly Color Surface = Color.FromArgb(26, 30, 40);
    private static readonly Color Muted = Color.FromArgb(167, 179, 199);
    public Label Heading { get; } = new();
    public Label Summary { get; } = new();
    public Label Versions { get; } = new();
    public Label Status { get; } = new();
    public Label Transfer { get; } = new();
    public TextBox Notes { get; } = new();
    public UpdateProgressBar Progress { get; } = new();
    public Button Primary { get; } = new();
    public Button Secondary { get; } = new();

    public UpdateWindow()
    {
        Text = "CIARE Update";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(17, 20, 28);
        ForeColor = Color.FromArgb(238, 242, 250);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(680, 580);
        MinimumSize = new Size(630, 570);
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(32, 24, 32, 24), ColumnCount = 1, RowCount = 10 };
        float[] heights = { 30, 50, 58, 64, 28, 0, 32, 12, 30, 54 };
        for (int i = 0; i < heights.Length; i++)
            layout.RowStyles.Add(i == 5 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, heights[i]));
        Controls.Add(layout);
        var brand = new Label { Text = "CIARE   /   SOFTWARE UPDATE", ForeColor = Color.FromArgb(108, 194, 255), Font = new Font(Font, FontStyle.Bold) };
        Heading.Font = new Font("Segoe UI Semibold", 23);
        Heading.Text = "A fresh version awaits";
        Summary.ForeColor = Muted;
        Summary.Text = "Get the latest improvements, directly from GitHub.";
        Versions.BackColor = Surface;
        Versions.Padding = new Padding(16, 0, 8, 0);
        Versions.TextAlign = ContentAlignment.MiddleLeft;
        Versions.Font = new Font("Segoe UI Semibold", 12);
        var notesTitle = new Label { Text = "RELEASE NOTES", ForeColor = Muted, Font = new Font(Font.FontFamily, 8, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
        Notes.Multiline = true;
        Notes.ReadOnly = true;
        Notes.TabStop = false;
        Notes.ScrollBars = ScrollBars.Vertical;
        Notes.BorderStyle = BorderStyle.None;
        Notes.BackColor = Surface;
        Notes.ForeColor = ForeColor;
        Notes.Text = "Checking the latest CIARE releases…";
        Status.TextAlign = ContentAlignment.BottomLeft;
        Transfer.ForeColor = Muted;
        Transfer.Font = new Font(Font.FontFamily, 9);
        Transfer.TextAlign = ContentAlignment.MiddleLeft;
        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        StyleButton(Primary, "Update & restart", Color.FromArgb(101, 188, 255), Color.FromArgb(14, 24, 38));
        StyleButton(Secondary, "Not now", Surface, ForeColor);
        actions.Controls.Add(Primary);
        actions.Controls.Add(Secondary);
        Control[] rows = { brand, Heading, Summary, Versions, notesTitle, Notes, Status, Progress, Transfer, actions };
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].Dock = DockStyle.Fill;
            rows[i].Margin = new Padding(0, 0, 0, i == 3 ? 8 : 0);
            layout.Controls.Add(rows[i], 0, i);
        }
        AcceptButton = Primary;
        Shown += (_, _) =>
        {
            Notes.Select(0, 0);
            if (Primary.Visible && Primary.Enabled) Primary.Focus(); else Secondary.Focus();
            int enabled = 1;
            try { DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)); } catch { }
        };
    }

    public void ShowRelease(Version installed, UpdateRelease release)
    {
        Versions.Text = $"{installed}   →   {release.Version}     /     {release.Architecture.ToUpperInvariant()}";
        Notes.Text = string.IsNullOrWhiteSpace(release.Notes) ? "This release includes the latest CIARE improvements." : release.Notes.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }

    public void ShowProgress(string status, int percent, string detail = "")
    {
        if (IsDisposed) return;
        Status.Text = status;
        Progress.Value = Math.Clamp(percent, 0, 100);
        Transfer.Text = detail;
    }

    private static void StyleButton(Button button, string text, Color background, Color foreground)
    {
        button.Text = text;
        button.Size = new Size(164, 38);
        button.Margin = new Padding(10, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = background;
        button.ForeColor = foreground;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.UseMnemonic = false;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}

[SupportedOSPlatform("windows")]
internal sealed class UpdateProgressBar : Control
{
    private int _value;
    [DefaultValue(0)]
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            AccessibleDescription = $"{_value}% complete";
            Invalidate();
        }
    }

    public UpdateProgressBar()
    {
        DoubleBuffered = true;
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleName = "Update progress";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var track = new SolidBrush(Color.FromArgb(38, 45, 59));
        using var fill = new SolidBrush(Color.FromArgb(101, 188, 255));
        e.Graphics.FillRectangle(track, ClientRectangle);
        e.Graphics.FillRectangle(fill, 0, 0, ClientSize.Width * Value / 100, ClientSize.Height);
    }
}
