using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CIARE.GUI
{
    public class TitleBar
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        public static void UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (enabled)
            {
                if (DwmSetWindowAttribute(handle, 19, new[] { 1 }, 4) != 0)
                    DwmSetWindowAttribute(handle, 20, new[] { 1 }, 4);
            }
            else
            {
                if (DwmSetWindowAttribute(handle, 19, new[] { 0 }, 4) == 0)
                    DwmSetWindowAttribute(handle, 20, new[] { 0 }, 4);
            }
            ShowWindow(handle, 0); //hide
            ShowWindow(handle, 8); //show
        }
    }
}
