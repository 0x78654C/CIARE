using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class Warnings
    {

        /// <summary>
        /// Check stored status for warnings display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckWarnings(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.warnings);
            if (regHighlight.Length > 0)
                GlobalVariables.OWarnings = bool.Parse(regHighlight);
        }

        /// <summary>
        /// Store in registry warinings display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetWarnings(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            GlobalVariables.OWarnings = status.Checked;
        }
    }
}
