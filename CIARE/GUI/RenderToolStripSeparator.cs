using CIARE.Utils;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Render ToolStripSeparator backgound/foreground color used on toolstrip menu in main form.
    /// </summary>
    public class RenderToolStripSeparator
    {
        /// <summary>
        /// Render ToolStripSeparator backgound/foreground color for light
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static  void RenderToolStripSeparator_PaintDark(object sender, PaintEventArgs e)
        {
            ToolStripSeparator toolStripSeparator = (ToolStripSeparator)sender;
            int width = toolStripSeparator.Width;
            int height = toolStripSeparator.Height;
            Color foreColor = Color.FromArgb(192, 215, 207);
            Color backColor =(GlobalVariables.isVStheme) ? Color.FromArgb(51, 51, 51) : Color.FromArgb(0, 1, 10);
            e.Graphics.FillRectangle(new SolidBrush(backColor), 0, 0, width, height);
            e.Graphics.DrawLine(new Pen(foreColor), 4, height / 2, width - 4, height / 2);
        }

        /// <summary>
        /// Render ToolStripSeparator backgound/foreground color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RenderToolStripSeparator_PaintLight(object sender, PaintEventArgs e)
        {
            ToolStripSeparator toolStripSeparator = (ToolStripSeparator)sender;
            int width = toolStripSeparator.Width;
            int height = toolStripSeparator.Height;
            Color foreColor = SystemColors.ControlDark;
            Color backColor = SystemColors.Window;
            e.Graphics.FillRectangle(new SolidBrush(backColor), 0, 0, width, height);
            e.Graphics.DrawLine(new Pen(foreColor), 4, height / 2, width - 4, height / 2);
        }
    }
}
