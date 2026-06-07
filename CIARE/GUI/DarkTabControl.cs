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

        public DarkTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            UpdateStyles();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                using (Graphics g = Graphics.FromHdc(m.WParam))
                {
                    Color background = GlobalVariables.darkColor ? GlobalVariables.formBgColor : BackColor;
                    using (SolidBrush brush = new SolidBrush(background))
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
                int tabStripBottom = GetTabRect(0).Bottom;

                // Cover the system-drawn border around the tab page content area.
                Rectangle pageRect = new Rectangle(0, tabStripBottom, Width - 1, Height - tabStripBottom - 1);
                using (Pen pen = new Pen(GlobalVariables.TabSelectedColor))
                    g.DrawRectangle(pen, pageRect);
            }
        }
    }
}
