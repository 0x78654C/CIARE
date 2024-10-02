using System.Runtime.Versioning;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class Publish
    {
        /// <summary>
        /// Check stored status for publish usage in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckPublishStatus(string regKeyName)
        {
            string regPublish = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.publish);
            if (regPublish.Length > 0)
                GlobalVariables.OPublishNative = bool.Parse(regPublish);
            GlobalVariables.publishAot = (GlobalVariables.OPublishNative) ? "\n<PublishAot>true</PublishAot>\n" : string.Empty;
        }

        /// <summary>
        /// Store in registry publish usage status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetPublishStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            GlobalVariables.OPublishNative = status.Checked;
            GlobalVariables.publishAot = (status.Checked)? "\n<PublishAot>true</PublishAot>\n": string.Empty;
        }
    }
}
