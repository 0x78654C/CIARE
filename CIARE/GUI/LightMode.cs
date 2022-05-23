﻿using System.Collections.Generic;
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
        public static void SetLightModeMain(Form form, RichTextBox richTextBox, GroupBox groupBox, Label highlight, ComboBox comboBox, MenuStrip menuStrip, TextBox find,
           List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList, Button findButton)
        {
            form.BackColor = SystemColors.Window;
            form.ForeColor = Color.Black;
            richTextBox.BackColor = SystemColors.Window;
            richTextBox.ForeColor = Color.Black;
            groupBox.ForeColor = Color.Black;
            highlight.ForeColor = Color.Black;
            comboBox.BackColor = SystemColors.Window;
            comboBox.ForeColor = Color.Black;
            menuStrip.BackColor = SystemColors.Window;
            menuStrip.ForeColor = Color.Black;
            find.BackColor = SystemColors.Window;
            find.ForeColor = Color.Black;
            findButton.BackColor = SystemColors.Window;
            findButton.ForeColor = Color.Black;
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
    }
}