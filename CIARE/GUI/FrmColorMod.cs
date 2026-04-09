using CIARE.Utils;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public static class FrmColorMod
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
          IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private static Color ForeColor;
        private static Color BackGroundColor;
        private static Color ForeColorForm;
        private static Color BackGroundColorForm;
        public static Color ButtonDarkColor = Color.FromArgb(2, 0, 10);
        public static Color ButtonDarkVSColor = Color.FromArgb(51, 51, 51);

        /// <summary>
        /// Toogle color mode on form (dark/light)
        /// </summary>
        /// <param name="form"></param>
        /// <param name="dark"></param>
        public static void ToogleColorMode(this Form form, bool dark)
        {
            EnableDarkTitleBar(form.Handle);
            ForeColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            if (GlobalVariables.isVStheme)
                BackGroundColor = dark ? Color.FromArgb(30, 30, 30) : SystemColors.Window;
            else
                BackGroundColor = dark ? Color.FromArgb(2, 0, 10) : SystemColors.Window;
            ForeColorForm = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            if (GlobalVariables.isVStheme)
                BackGroundColorForm = dark ? Color.FromArgb(51, 51, 51) : SystemColors.Window;
            else
                BackGroundColorForm = dark ? Color.FromArgb(0, 1, 10) : SystemColors.Window;
            ApplyColorMode(form, dark);
            form.BackColor = BackGroundColorForm;
            form.ForeColor = ForeColorForm;
        }

        /// <summary>
        /// Enable dark title bar on windows 10 and 11. (Only for main form, child forms will inherit the title bar color)
        /// </summary>
        /// <param name="handle"></param>
        public static void EnableDarkTitleBar(IntPtr handle)
        {
            int value = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }


        /// <summary>
        /// Apply color mode on form (dark/light) 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="dark"></param>
        private static void ApplyColorMode(Control control, bool dark)
        {
            if (control.GetType().Name != "TextBox" || control.GetType().Name != "ComboBox" && dark)
                control.BackColor = BackGroundColor;
            else
                control.BackColor = BackGroundColor;

            if (control.GetType().Name == "GroupBox" || control.GetType().Name == "Label" || control.GetType().Name == "CheckBox"
                || control.GetType().Name == "PictureBox" || control.GetType().Name == "TableLayoutPanel"
                || control.GetType().Name == "TabPage" && dark)
                control.BackColor = BackGroundColorForm;


            control.ForeColor = ForeColor;

            foreach (Control control2 in control.Controls)
                ApplyColorMode(control2, dark);
        }

        /// <summary>
        /// Set color for buttons on dark theme by text box.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="textBox"></param>
        /// <param name="isDark"></param>
        public static void SetButtonColorDisable(Button button, TextBox textBox, bool isDark, bool isVsTheme)
        {
            if (isDark)
            {
                if (string.IsNullOrEmpty(textBox.Text) && isDark)
                    button.BackColor = (isVsTheme) ? ButtonDarkVSColor : Color.Gray;
                else

                    button.BackColor = (isVsTheme) ? ButtonDarkVSColor : ButtonDarkColor;
            }
        }

        /// <summary>
        ///  Set color for buttons on dark theme by combobox.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="comboBox"></param>
        /// <param name="isDark"></param>
        /// <param name="isVsTheme"></param>
        public static void SetButtonColorDisableCombo(Button button, ComboBox comboBox, bool isDark, bool isVsTheme)
        {
            if (isDark)
            {
                if (string.IsNullOrEmpty(comboBox.Text) && isDark)
                    button.BackColor = (isVsTheme) ? ButtonDarkVSColor : Color.Gray;
                else

                    button.BackColor = (isVsTheme) ? ButtonDarkVSColor : ButtonDarkColor;
            }
        }
    }
}
