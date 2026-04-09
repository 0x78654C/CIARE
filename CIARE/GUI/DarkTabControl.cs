using CIARE.Utils;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class DarkTabControl : TabControl
    {
        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;

        protected override void WndProc(ref Message m)
        {
            // In dark mode, fill the tab strip background ourselves so the
            // OS default light erase does not show through gaps between tabs.
            if (m.Msg == WM_ERASEBKGND && GlobalVariables.darkColor)
            {
                using (Graphics g = Graphics.FromHdc(m.WParam))
                {
                    Color bgColor = GlobalVariables.isVStheme
                        ? Color.FromArgb(51, 51, 51)
                        : Color.FromArgb(0, 1, 10);
                    using (SolidBrush brush = new SolidBrush(bgColor))
                        g.FillRectangle(brush, ClientRectangle);
                }
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            // After the system has drawn everything (including calling DrawItem),
            // overdraw the OS-drawn light border around the page content area.
            if (m.Msg == WM_PAINT && GlobalVariables.darkColor)
                OverdrawPageBorder();
        }

        private void OverdrawPageBorder()
        {
            if (TabCount == 0) return;
            using (Graphics g = Graphics.FromHwnd(Handle))
            {
                Color borderColor = GlobalVariables.isVStheme
                    ? Color.FromArgb(63, 63, 70)
                    : Color.FromArgb(20, 20, 35);

                int tabStripBottom = GetTabRect(0).Bottom;

                // Cover the system-drawn border around the tab page content area.
                Rectangle pageRect = new Rectangle(0, tabStripBottom, Width - 1, Height - tabStripBottom - 1);
                using (Pen pen = new Pen(borderColor))
                    g.DrawRectangle(pen, pageRect);
            }
        }
    }
}
