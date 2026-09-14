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
            List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList, bool isVsTheme,
            Color? foreColor = null)
        {

            var textColor = foreColor ?? Color.FromArgb(192, 215, 207);
            FrmColorMod.EnableDarkTitleBar(form.Handle);
            form.BackColor = GlobalVariables.formBgColor;
            form.ForeColor = textColor;
            richTextBox.BackColor = GlobalVariables.controlBgColor;
            if (GlobalVariables.isRed)
            {
                richTextBox.ForeColor = Color.Red;
                GlobalVariables.isRed = false;
            }
            else
                richTextBox.ForeColor = textColor;
            groupBox.ForeColor = textColor;
            separator2.ForeColor = textColor;
            separator3.ForeColor = textColor;
            menuStrip.BackColor = GlobalVariables.formBgColor;
            menuStrip.ForeColor = textColor;
            menuStrip.Renderer = new ColorTableSet();
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = GlobalVariables.formBgColor;
                toolStripMenu.ForeColor = textColor;
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint -= RenderToolStripSeparator.RenderToolStripSeparator_PaintLight;
                toolStripSeparator.Paint -= RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
            }
        }
    }
}
