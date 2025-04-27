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
            string regOllamaAIModel = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.ollamModel);
            string regAiType = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.aiType);
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

            if (regAiType.Length > 0)
                GlobalVariables.aiTypeVar = regAiType;
            else
                GlobalVariables.aiTypeVar = "OpenAI";

            if (regAiType.StartsWith("Ollama"))
            {
                GlobalVariables.modelOllamaVar = regOllamaAIModel;
                GlobalVariables.aiTypeVar = regAiType;
            }
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
        public static void SetOpenAIData(TextBox openAIApiKey, TextBox aiMaxTokens, TextBox openModel, ComboBox aitype, ComboBox ollamaModel, string regKeyAPI, string regKeyTokens, string regModel, string regOllamModel, string regAiType)
        {
            var trimKey = openAIApiKey.Text.Trim();
            var trimTokens = aiMaxTokens.Text.Trim();
            var trimModel = openModel.Text.Trim();
            var trimAyType = aitype.Text.Trim();
            var trimOllamaModel = ollamaModel.Text.Trim();
            if (trimAyType.StartsWith("Ollama"))
            {
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regAiType, "Ollama(Local)");
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regOllamModel, trimOllamaModel);
                GlobalVariables.aiKey = trimKey;
                GlobalVariables.aiMaxTokens = trimTokens;
                GlobalVariables.modelOllamaVar = trimOllamaModel;
                GlobalVariables.aiTypeVar = trimAyType;
                return;
            }
            if (string.IsNullOrWhiteSpace(trimAyType))
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regAiType, "OpenAI");
            else
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regAiType, trimAyType);



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
            GlobalVariables.aiType = trimAyType;
        }
    }
}
