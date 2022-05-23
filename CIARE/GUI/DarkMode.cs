using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CIARE.GUI
{
    /// <summary>
    /// Dark mode theme class.
    /// </summary>
    public class DarkMode
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
        public static void SetDarkModeMain(Form form, RichTextBox richTextBox, GroupBox groupBox, Label separator1,
            Label separator2, Label separator3, Label highlight, ComboBox comboBox, MenuStrip menuStrip, TextBox find, 
            List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList, Button findButton)
        {
            form.BackColor = Color.FromArgb(51,51,51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            richTextBox.BackColor = Color.FromArgb(36, 36, 36);
            richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor= Color.FromArgb(192, 215, 207);
            separator1.ForeColor = Color.FromArgb(192, 215, 207);
            separator2.ForeColor = Color.FromArgb(192, 215, 207);
            separator3.ForeColor = Color.FromArgb(192, 215, 207);
            highlight.ForeColor = Color.FromArgb(192, 215, 207);
            comboBox.BackColor = Color.FromArgb(36, 36, 36);
            comboBox.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.BackColor = Color.FromArgb(51, 51, 51);
            menuStrip.ForeColor = Color.FromArgb(192, 215, 207);
            find.BackColor = Color.FromArgb(36, 36, 36);
            find.ForeColor = Color.FromArgb(192, 215, 207);
            findButton.BackColor = Color.FromArgb(36, 36, 36);
            findButton.ForeColor = Color.FromArgb(192, 215, 207);
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = Color.FromArgb(51, 51, 51);
                toolStripMenu.ForeColor = Color.FromArgb(192, 215, 207);
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
            }
        }
    }
}
