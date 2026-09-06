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
        public DarkTabControl()
        {
            // TabControl's native WM_PAINT bypasses WinForms double buffering.
            // Paint the complete strip (including its border) in one buffer.
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            DrawMode = TabDrawMode.OwnerDrawFixed;
            UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color background = GlobalVariables.darkColor ? GlobalVariables.formBgColor : BackColor;
            using var brush = new SolidBrush(background);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            for (int index = 0; index < TabCount; index++)
            {
                if (index != SelectedIndex) PaintTab(e, index);
            }
            if (SelectedIndex >= 0) PaintTab(e, SelectedIndex);

            if (TabCount > 0)
            {
                var border = DisplayRectangle;
                border.Inflate(1, 1);
                using var pen = new Pen(GlobalVariables.darkColor
                    ? GlobalVariables.TabSelectedColor : SystemColors.ControlDark);
                e.Graphics.DrawRectangle(pen, border);
            }
            base.OnPaint(e);
        }

        private void PaintTab(PaintEventArgs e, int index)
        {
            Rectangle bounds = GetTabRect(index);
            if (!bounds.IntersectsWith(e.ClipRectangle)) return;
            DrawItemState state = index == SelectedIndex ? DrawItemState.Selected : DrawItemState.None;
            if (Focused && index == SelectedIndex) state |= DrawItemState.Focus;
            OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, bounds, index, state));
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }
    }
}
