using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Class for change Menu items selection color.
    /// </summary>
    public class ColorTableSet : ToolStripProfessionalRenderer
    {
        public ColorTableSet() : base(new MyColors()) { }
        private class MyColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return Color.FromArgb(45, 45, 48); }
            }

            public override Color MenuItemSelectedGradientBegin
            {
                get { return Color.FromArgb(45, 45, 48); }
            }
            public override Color MenuItemSelectedGradientEnd
            {
                get { return Color.FromArgb(45, 45, 48); }
            }
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(45, 45, 48);

        }
    }
}
