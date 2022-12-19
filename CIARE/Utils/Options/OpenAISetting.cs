using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class OpenAISetting
    {
        /// <summary>
        /// Check stored data for OpenAI.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckOpenAIData(string regKeyName)
        {
            string regOpenAIKey = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.openAIKey);
            string regOpenAITokens = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.openAIMaxTokens);
            if (regOpenAIKey.Length > 0)
                GlobalVariables.aiKey = regOpenAIKey;
            if (regOpenAITokens.Length > 0)
                GlobalVariables.aiMaxTokens = regOpenAITokens;
            else
                GlobalVariables.aiMaxTokens = "999";
        }

        /// <summary>
        /// Set stored data for OpenAI.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetOpenAIData(TextBox openAIApiKey, TextBox aiMaxTokens, string regKeyAPI, string regKeyTokens)
        {
            var trimKey = openAIApiKey.Text.Trim();
            var trimTokens = aiMaxTokens.Text.Trim();
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyAPI, trimKey);
            if (!string.IsNullOrWhiteSpace(trimTokens))
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyTokens, trimTokens);
            else
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyTokens, "999"); // default 999
            GlobalVariables.aiKey = trimKey;
            GlobalVariables.aiMaxTokens = trimTokens;
        }
    }
}
