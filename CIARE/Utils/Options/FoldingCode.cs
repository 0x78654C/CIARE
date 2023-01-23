using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class FoldingCode
    {
        /// <summary>
        /// Check stored status for folding code display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckFoldingCodeStatus(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.foldingCodeKey);
            if (regHighlight.Length > 0)
            {
                GlobalVariables.OFoldingCode = bool.Parse(regHighlight);
                MainForm.Instance.textEditorControl1.EnableFolding = GlobalVariables.OFoldingCode;
            }
        }

        /// <summary>
        /// Store in registry folding code display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetFoldingCodeStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            MainForm.Instance.textEditorControl1.EnableFolding = status.Checked;
            GlobalVariables.OFoldingCode = status.Checked;
        }
    }
}
