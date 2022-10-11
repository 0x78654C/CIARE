using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class CodeCompletion
    {
        /// <summary>
        /// Check stored status for code completion display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckCodeCompletion(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.codeCompletionKey);
            if (regHighlight.Length > 0)
                GlobalVariables.OCodeCompletion = bool.Parse(regHighlight);
        }

        /// <summary>
        /// Store in registry code completion display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetCodeCompletionStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            GlobalVariables.OCodeCompletion = status.Checked;
        }
    }
}
