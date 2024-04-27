using CIARE.Utils;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Dark mode theme class.
    /// </summary>
    public class DarkModeMain
    {
        /// <summary>
        /// Set dark mode for main form. 
        /// </summary>
        /// <param name="form"></param>
        /// <param name="richTextBox"></param>
        /// <param name="groupBox"></param>
        /// <param name="separator1"></param>
        /// <param name="separator2"></param>
        /// <param name="separator3"></param>
        /// <param name="highlight"></param>
        /// <param name="comboBox"></param>
        /// <param name="menuStrip"></param>
        /// <param name="find"></param>
        /// <param name="toolStripMenuList"></param>
        /// <param name="toolStripSeparatorList"></param>
        /// <param name="findButton"></param>
        public static void SetDarkModeMain(Form form, RichTextBox richTextBox, GroupBox groupBox,
            Label separator2, Label separator3, MenuStrip menuStrip,
            List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList, bool isVsTheme)
        {
            form.BackColor = (isVsTheme)? Color.FromArgb(51, 51, 51): Color.FromArgb(0, 1, 10);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            richTextBox.BackColor = (isVsTheme) ? Color.FromArgb(30, 30, 30) : Color.FromArgb(2, 0, 10);
            if (GlobalVariables.isRed)
            {
                richTextBox.ForeColor = Color.Red;
                GlobalVariables.isRed = false;
            }
            else
                richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
            separator2.ForeColor = Color.FromArgb(192, 215, 207);
            separator3.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.BackColor = (isVsTheme) ? Color.FromArgb(51, 51, 51): Color.FromArgb(0, 1, 10);
            menuStrip.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.Renderer = new ColorTableSet();
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = (isVsTheme) ? Color.FromArgb(51, 51, 51) : Color.FromArgb(0, 1, 10);
                toolStripMenu.ForeColor = Color.FromArgb(192, 215, 207);
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
            }
        }
    }
}