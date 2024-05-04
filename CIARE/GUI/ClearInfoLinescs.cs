using CIARE.Utils;
using System.Runtime.Versioning;

namespace CIARE.GUI
{
    public class ClearInfoLinescs
    {
        [SupportedOSPlatform("windows")]
        public static void ClearLinesInfo()
        {
            MainForm.Instance.linesPositionLbl.Text = string.Empty;
            MainForm.Instance.linesCountLbl.Text = string.Empty;
            GlobalVariables.linePos = 0;
        }
    }
}
