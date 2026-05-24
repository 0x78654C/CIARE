using System.Runtime.Versioning;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    /*
     Show/Hide split container second Panel.
     */
    public class SplitContainerHideShow
    {
        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Hide split container second panel.
        /// </summary>
        /// <param name="splitContainer"></param>
        public static void HideSplitContainer(SplitContainer splitContainer)
        {
            if (splitContainer == null || splitContainer.IsDisposed || splitContainer.Panel2Collapsed)
                return;

            SetRedraw(splitContainer, false);
            splitContainer.SuspendLayout();
            try
            {
                splitContainer.Panel2Collapsed = true;
                splitContainer.Panel2.Hide();
            }
            finally
            {
                splitContainer.ResumeLayout(true);
                SetRedraw(splitContainer, true);
                splitContainer.Invalidate(true);
            }
        }

        /// <summary>
        /// Show split container second panel.
        /// </summary>
        /// <param name="splitContainer"></param>
        public static void ShowSplitContainer(SplitContainer splitContainer)
        {
            if (splitContainer == null || splitContainer.IsDisposed)
                return;

            if (!splitContainer.Panel2Collapsed && splitContainer.Panel2.Visible)
                return;

            SetRedraw(splitContainer, false);
            splitContainer.SuspendLayout();
            try
            {
                splitContainer.Panel2Collapsed = false;
                splitContainer.Panel2.Show();
            }
            finally
            {
                splitContainer.ResumeLayout(true);
                SetRedraw(splitContainer, true);
                splitContainer.Invalidate(true);
            }
        }

        private static void SetRedraw(Control control, bool enabled)
        {
            if (control == null || control.IsDisposed || !control.IsHandleCreated)
                return;

            SendMessage(control.Handle, WM_SETREDRAW, enabled ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
        }
    }
}
