using System.Runtime.Versioning;
using System.Windows.Forms;

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
                //MainForm.Instance.markStartFileChk.Visible = regParse; //TODO: remove on refactor and cleanup
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
           // MainForm.Instance.markStartFileChk.Visible = status.Checked;
            GlobalVariables.OStartUp = status.Checked;
        }

        /// <summary>
        /// Store state of windows login registry.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetWinLoginState(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            GlobalVariables.OWinLoginState = status.Checked;
        }
    }
}
