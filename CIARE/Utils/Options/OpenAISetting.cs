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
            string regOpenAIModel = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.openModel);
            if (regOpenAIKey.Length > 0)
                GlobalVariables.aiKey = regOpenAIKey;
            if (regOpenAITokens.Length > 0)
                GlobalVariables.aiMaxTokens = regOpenAITokens;
            else
                GlobalVariables.aiMaxTokens = "999";

            if (regOpenAIModel.Length > 0)
                GlobalVariables.model = regOpenAIModel;
            else
                GlobalVariables.model = "text-davinci-003";
        }

        /// <summary>
        /// Set stored data for OpenAI.
        /// </summary>
        /// <param name="openAIApiKey"></param>
        /// <param name="aiMaxTokens"></param>
        /// <param name="openModel"></param>
        /// <param name="regKeyAPI"></param>
        /// <param name="regKeyTokens"></param>
        /// <param name="regModel"></param>
        public static void SetOpenAIData(TextBox openAIApiKey, TextBox aiMaxTokens, TextBox openModel, string regKeyAPI, string regKeyTokens, string regModel)
        {
            var trimKey = openAIApiKey.Text.Trim();
            var trimTokens = aiMaxTokens.Text.Trim();
            var trimModel = openModel.Text.Trim();
            if (!string.IsNullOrWhiteSpace(trimModel))
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regModel, trimModel);
            else
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regModel, "text-davinci-003");

            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyAPI, trimKey);
            if (!string.IsNullOrWhiteSpace(trimTokens))
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyTokens, trimTokens);
            else
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyTokens, "999"); // default 999
            GlobalVariables.aiKey = trimKey;
            GlobalVariables.aiMaxTokens = trimTokens;
            GlobalVariables.model = trimModel;
        }
    }
}
