using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList)
        {
            form.BackColor = Color.FromArgb(0, 1, 10);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            richTextBox.BackColor = Color.FromArgb(30, 30, 30);
            richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
            separator2.ForeColor = Color.FromArgb(192, 215, 207);
            separator3.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.BackColor = Color.FromArgb(0, 1, 10);
            menuStrip.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.Renderer = new ColorTableSet();
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = Color.FromArgb(0, 1, 10);
                toolStripMenu.ForeColor = Color.FromArgb(192, 215, 207);
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
            }
        }
    }
}