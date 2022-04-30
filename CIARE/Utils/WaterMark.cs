using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CIARE.Utils
{
    public class WaterMark
    {
        //Grab the user32.dll function for textbox hint text
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        /// <summary>
        /// Set textbox watermark.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="message"></param>
        public static void TextBoxWaterMark(TextBox textBox, string message)
        {
            SendMessage(textBox.Handle, 0x1501, 1, message);
        }
    }
}
