using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class UnsafeCode
    {
        /// <summary>
        /// Check stored status for unsafe code usage in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckUnsafeStatus(string regKeyName)
        {
            string regUnsafe = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.unsafeCode);
            if (regUnsafe.Length > 0)
                GlobalVariables.OUnsafeCode = bool.Parse(regUnsafe);
        }

        /// <summary>
        /// Store in registry unsafe code usage status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetUnsafeStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            GlobalVariables.OUnsafeCode = status.Checked;
        }
    }
}
