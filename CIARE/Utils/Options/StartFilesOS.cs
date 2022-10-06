using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class StartFilesOS
    {
        /// <summary>
        /// Check state of file open on startup option.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckOSStartFile(string regKeyName)
        {
            string regOSStartFile = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.startUp);
            if (regOSStartFile.Length > 0)
            {
                bool regParse = bool.Parse(regOSStartFile);
                GlobalVariables.OStartUp = bool.Parse(regOSStartFile);
                Form1.Instance.markStartFileChk.Visible = regParse;
            }
        }

        /// <summary>
        /// Store state of file open on startup option.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetOSStartStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            Form1.Instance.markStartFileChk.Visible = status.Checked;
            GlobalVariables.OStartUp = status.Checked;
        }
    }
}
