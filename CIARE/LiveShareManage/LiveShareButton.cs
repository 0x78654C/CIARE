using System.Drawing;
using System.Windows.Forms;

namespace CIARE.LiveShareManage;

// WinForms' disabled flat-button renderer ignores ForeColor and can draw black text on dark themes.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class LiveShareButton : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Enabled) { base.OnPaint(e); return; }
        e.Graphics.Clear(BackColor);
        using var border = new Pen(FlatAppearance.BorderColor);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
