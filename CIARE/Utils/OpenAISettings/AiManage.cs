using ICSharpCode.TextEditor;
using OpenAI.Api.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenAI.Api.Client.Models;
using System.Runtime.Versioning;
using OpenRouter;
using OllamaInt;
using System.Drawing;
using CIARE.Model;
using CIARE.GUI;

namespace CIARE.Utils.OpenAISettings
{
    [SupportedOSPlatform("windows")]
    public class AiManage
    {
        private string ApiKey = string.Empty;
        private string Qestion = string.Empty;
        private static HttpClient HttpClient = new HttpClient();
        private static int s_line = 0;

        public AiManage(string apiKey, string question)
        {
            ApiKey = apiKey;
            Qestion = question;
        }

        /// <summary>
        /// OPenAI API call
        /// </summary>
        /// <returns></returns>
        private async Task<string> AskOpenAI()
        {
            var result = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(ApiKey))
                    return "";

                if (string.IsNullOrEmpty(Qestion))
                    return "";
                var aiType = GlobalVariables.aiTypeVar;

                if (aiType == "OpenAI")
                {
                    OpenAiApiV1Client client = new OpenAiApiV1Client(HttpClient, ApiKey);

                    var resu = await client.PostCompletion(new CompletionRequest
                    {
                        Max_Tokens = Int32.Parse(GlobalVariables.aiMaxTokens),
                        Temperature = 0.8m,
                        Model = GlobalVariables.model,
                        Prompt = Qestion
                    });
                    result = resu.Choices.FirstOrDefault()?.Text;
                }
                else if (aiType == "OpenRouter")
                {
                    OpenRouterClient openRouterClient = new OpenRouterClient(ApiKey);
                    var response = await openRouterClient.SendPromptAsync(Qestion, GlobalVariables.model);
                    result = response;
                }
                else if (aiType.StartsWith("Ollama"))
                {
                    OllamaLLM ollamaClient = new OllamaLLM();
                    ollamaClient.Model = GlobalVariables.modelOllamaVar;
                    ollamaClient.Promt = Qestion;
                    ollamaClient.Uri = GlobalVariables.ollamaUri;
                    ollamaClient.ChatHistory = GlobalVariables.chatHistory;
                    var response = await ollamaClient.AskOllama();
                    result = response;
                }
                else
                {
                    MessageBox.Show("Wrong AI type!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}. Key period or credit could be expired!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        /// <summary>
        /// Load models from local Ollama.
        /// </summary>
        /// <param name="comboBox"></param>
        public static void LoadOllamaModels(ref ComboBox comboBox)
        {
            try
            {
                var client = new OllamaLLM();
                var models = client.LocalModels();
                comboBox.Items.Clear();
                foreach (var model in models)
                    comboBox.Items.Add(model);
                comboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// Get data from OpenAI
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="apiAi"></param>
        public static async void GetDataAI(TextEditorControl textEditorControl, string apiAi)
        {
            GlobalVariables.aiQuestion = "";
            if (string.IsNullOrEmpty(apiAi))
            {
                MessageBox.Show("OpenAI API key was not found!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CancelProgressBar();
                return;
            }

            var askAI = new AskAI();
            askAI.ShowDialog();
            LoadProgressBar();
            if (string.IsNullOrWhiteSpace(GlobalVariables.aiQuestion))
            {
                CancelProgressBar();
                return;
            }
            AiManage openAI = new AiManage(apiAi, GlobalVariables.aiQuestion.Trim());
            StringReader reader = new StringReader(await openAI.AskOpenAI());
            string line = "";
            string outPut = string.Empty;
            while ((line = reader.ReadLine()) != null)
                outPut += $"{Environment.NewLine}{line}";

            CancelProgressBar();
            GlobalVariables.aiQuestion = "";
            GlobalVariables.aiResponse = outPut;
            AiResponse aiResponse = new AiResponse();
            aiResponse.Show();
        }

        /// <summary>
        /// Hides the progress bar and associated label in the application's main form.
        /// </summary>
        /// <remarks>This method disables the visibility of the progress bar and the AI label. It is
        /// typically used to indicate that a background operation has completed or been canceled.</remarks>
        private static void CancelProgressBar()
        {
            MainForm.Instance.EditorTabControl.Visible = true;
            MainForm.Instance.EditorTabControl.Enabled = true;
            MainForm.Instance.progressBar.Visible = false;
        }

        /// <summary>
        /// Display error message solution from OpenAI.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="apiAi"></param>
        /// <param name="lineError"></param>
        /// <param name="errorMessage"></param>
        public static async void GetDataAIERR(TextEditorControl textEditorControl, string apiAi, string code, string errorMessage, string lineNumber, RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(apiAi))
            {
                MessageBox.Show("OpenAI API key was not found!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lineErrSplit = errorMessage.Split(':');
            var question = $"{code}\nHow to fix the error '{errorMessage}' at line {lineNumber} for the code above? Thank you!";
            AiManage openAI = new AiManage(apiAi, question); //for test now
            StringReader reader = new StringReader(await openAI.AskOpenAI());
            string line = "";
            string outPut = string.Empty;
            while ((line = reader.ReadLine()) != null)
                outPut += $"{Environment.NewLine}{line}";
            CancelProgressBar(); 
            GlobalVariables.aiResponse = outPut;
            AiResponse aiResponseError = new AiResponse();
            aiResponseError.Show();
        }

        /// <summary>
        /// Load progress bar.
        /// </summary>
        public static void LoadProgressBar()
        {
            const string regName = "highlight";
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", regName);
            MainForm.Instance.progressBar.Visible = true;
            MainForm.Instance.EditorTabControl.Visible = false;
            MainForm.Instance.EditorTabControl.Enabled = false;
            int centerX = (MainForm.Instance.Width - MainForm.Instance.progressBar.Width) / 2;
            int centerY = (MainForm.Instance.Height - MainForm.Instance.progressBar.Height) / 2;
            MainForm.Instance.progressBar.Location = new Point(centerX, centerY);
            MainForm.Instance.progressBar.BringToFront();
        }
    }
}
