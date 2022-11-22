using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class LiveShare
    {

        /// <summary>
        /// Check stored status for warnings display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckApiLiveShare(string regKeyName)
        {
            string regLiveShare = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.liveShare);
            if (regLiveShare.Length > 0)
                GlobalVariables.apiUrl = regLiveShare;
        }

        /// <summary>
        /// Store in registry warinings display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetApiLiveShare(TextBox liveShareApi, string regKeyName)
        {
            var trimLink = liveShareApi.Text.Trim();
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, trimLink);
            GlobalVariables.apiUrl = trimLink;
        }
    }
}
