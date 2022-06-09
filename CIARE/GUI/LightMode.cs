using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CIARE.GUI
{
    /// <summary>
    /// Light mode theme class.
    /// </summary>
    public class LightMode
    {
        /// <summary>
        /// Set light mode for main form.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="richTextBox"></param>
        /// <param name="groupBox"></param>
        /// <param name="highlight"></param>
        /// <param name="comboBox"></param>
        /// <param name="menuStrip"></param>
        /// <param name="find"></param>
        /// <param name="toolStripMenuList"></param>
        /// <param name="toolStripSeparatorList"></param>
        /// <param name="findButton"></param>
        public static void SetLightModeMain(Form form, RichTextBox richTextBox, GroupBox groupBox, MenuStrip menuStrip,
           List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList)
        {
            form.BackColor = SystemColors.Window;
            form.ForeColor = Color.Black;
            richTextBox.BackColor = SystemColors.Window;
            richTextBox.ForeColor = Color.Black;
            groupBox.ForeColor = Color.Black;
            menuStrip.BackColor = SystemColors.Window;
            menuStrip.ForeColor = Color.Black;
            menuStrip.Renderer = null;
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = SystemColors.Window;
                toolStripMenu.ForeColor = Color.Black;
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintLight;
            }
        }

        public static void OptionsLightMode(Form form, Button cancelBtn, Label highlightLbl, ComboBox highLightName,
          CheckBox codeCompletion, CheckBox lineNumber, CheckBox codeFolding)
        {
            form.BackColor = SystemColors.Window;
            form.ForeColor = Color.Black;
            cancelBtn.BackColor = SystemColors.Window;
            cancelBtn.ForeColor = Color.Black;
            highlightLbl.ForeColor = Color.Black;
            highLightName.BackColor = SystemColors.Window;
            highLightName.ForeColor = Color.Black;
            codeCompletion.ForeColor = Color.Black;
            lineNumber.ForeColor = Color.Black;
            codeFolding.ForeColor = Color.Black;
        }
    }
}
