using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class LiveShare
    {

        /// <summary>
        /// Check stored data for LiveShare
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckApiLiveShare(string regKeyName)
        {
            string regLiveShare = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.liveShare);
            if (TryNormalizeApiUrl(regLiveShare, out string normalizedUrl))
                GlobalVariables.apiUrl = normalizedUrl;
        }

        /// <summary>
        /// Set stored data for LiveShare
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetApiLiveShare(TextBox liveShareApi, string regKeyName)
        {
            var trimLink = liveShareApi.Text.Trim();
            if (!TryNormalizeApiUrl(trimLink, out string normalizedUrl))
            {
                MessageBox.Show("Use an HTTPS Live Share URL. HTTP is allowed only for local development.",
                    "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, normalizedUrl);
            GlobalVariables.apiUrl = normalizedUrl;
        }

        internal static bool TryNormalizeApiUrl(string value, out string normalizedUrl)
        {
            normalizedUrl = string.Empty;
            if (!System.Uri.TryCreate(value, System.UriKind.Absolute, out System.Uri uri) ||
                !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            bool isHttps = string.Equals(uri.Scheme, System.Uri.UriSchemeHttps,
                System.StringComparison.OrdinalIgnoreCase);
            bool isLocalHttp = string.Equals(uri.Scheme, System.Uri.UriSchemeHttp,
                System.StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
            if (!isHttps && !isLocalHttp)
                return false;

            normalizedUrl = uri.AbsoluteUri.TrimEnd('/');
            return true;
        }
    }
}
