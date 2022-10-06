using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class BuildConfig
    {
        #region Config registry events. 
        /// <summary>
        /// Check stored status for configuration parameters in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckConfig(string regKeyName)
        {
            string regConfig = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.OConfigParam);
            if (regConfig.Length > 0)
                GlobalVariables.configParam = regConfig;
        }

        /// <summary>
        /// Set index for configuration comboBox.
        /// </summary>
        /// <param name="comboBox"></param>
        public static void SetConfigControl(ComboBox comboBox)
        {
            if (GlobalVariables.configParam.Contains("Release"))
                comboBox.SelectedIndex = 1;
            else
                comboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Store in registry configuration parameters status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void StoreConfig(string regKeyName, string configParam)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, configParam);
        }
        #endregion

        #region Platform reistry store events.

        /// <summary>
        /// Check stored status for platform parameters in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckPlatform(string regKeyName)
        {
            string regPlat = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.OPlatformParam);
            if (regPlat.Length > 0)
                GlobalVariables.platformParam = regPlat;
        }

        /// <summary>
        /// Set index for platform comboBox.
        /// </summary>
        /// <param name="comboBox"></param>
        public static void SetPlatformControl(ComboBox comboBox)
        {
            if (GlobalVariables.platformParam.Contains("x64"))
                comboBox.SelectedIndex = 1;
            else
                comboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Store in registry platform parameters status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void StorePlatform(string regKeyName, string configParam)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, configParam);
        }
        #endregion
    }
}
