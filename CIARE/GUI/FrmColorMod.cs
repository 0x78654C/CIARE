using CIARE.Utils;
using System.Drawing;
using System.Windows.Forms;

namespace CIARE.GUI
{
    public static class FrmColorMod
    {
        private static Color ForeColor;
        private static Color BackGroundColor;
        private static Color ForeColorForm;
        private static Color BackGroundColorForm;

        /// <summary>
        /// Toogle color mode on form (dark/light)
        /// </summary>
        /// <param name="form"></param>
        /// <param name="dark"></param>
        public static void ToogleColorMode(this Form form, bool dark)
        {
            ForeColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            BackGroundColor = dark ? Color.FromArgb(30, 30, 30) : SystemColors.Window;
            ForeColorForm = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            if(GlobalVariables.isVStheme)
                BackGroundColorForm = dark ? Color.FromArgb(51, 51, 51) : SystemColors.Window;
            else
                BackGroundColorForm = dark ? Color.FromArgb(0, 1, 10) : SystemColors.Window;
            ApplyColorMode(form, dark);
            form.BackColor = BackGroundColorForm;
            form.ForeColor = ForeColorForm;
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
    }
}
