using ICSharpCode.TextEditor;
using OpenAI.Api.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using OpenAI.Api.Client.Models;
using System.Runtime.Versioning;
using OpenRouter;
using OllamaInt;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Drawing;
using CIARE.GUI;
using CIARE.Model;

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
                    var response = await openRouterClient.SendPromptAsync(Qestion);
                    result = response;
                }else if (aiType.StartsWith("Ollama"))
                {
                    OllamaLLM ollamaClient = new OllamaLLM();
                    ollamaClient.Model = GlobalVariables.modelOllamaVar;
                    ollamaClient.Promt = Qestion;
                    ollamaClient.Uri = GlobalVariables.ollamaUri;
                    ollamaClient.ChatHistory = GlobalVariables.chatHistory;
                    var response = Task.Run(ollamaClient.AskOllama).Result;
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
            if (string.IsNullOrEmpty(apiAi))
            {
                MessageBox.Show("OpenAI API key was not found!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string question = string.Empty;
            if (textEditorControl.Text.Contains("/*[") && textEditorControl.Text.Contains("]*/"))
            {
                try
                {
                    question = textEditorControl.Text.MiddleString("/*[", "]*/");
                    if (string.IsNullOrWhiteSpace(question))
                    {
                        MessageBox.Show("No question provided!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                catch
                {
                    // Do nothing.
                }
            }
            else
            {
                MessageBox.Show("Wrong question format!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            AiManage openAI = new AiManage(apiAi, question.Trim());
            StringReader reader = new StringReader(await openAI.AskOpenAI());
            string line = "";
            string outPut = string.Empty;
            while ((line = reader.ReadLine()) != null)
                outPut += $"{Environment.NewLine}{line}";
            outPut = $"//---------------- {{Result}} ----------------\n{outPut}\n//----------------------------------------";
            var newAIData = InsertData(textEditorControl.Text, "]*/", outPut).Replace($"/*[{question}]*/", "");
            textEditorControl.Document.Replace(0, textEditorControl.Text.Length, newAIData);
            GoToLineNumber.GoToLine(textEditorControl, s_line);
            MainForm.Instance.progressBar.Visible = false;
            MainForm.Instance.aiLabel.Visible = false;
        }

        /// <summary>
        /// Display error message solution from OpenAI.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="apiAi"></param>
        /// <param name="lineError"></param>
        /// <param name="errorMessage"></param>
        public static async void GetDataAIERR(TextEditorControl textEditorControl, string apiAi, string code, string errorMessage,string lineNumber, RichTextBox richTextBox)
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
            //outPut = $"\n\n---------------- {{AI respond on error message}} ----------------\n{outPut}\n-------------------------------------------------------------";
            //richTextBox.Text += outPut;
            MainForm.Instance.progressBar.Visible = false;
            MainForm.Instance.aiLabel.Visible = false;
            GlobalVariables.errorAiResponse = outPut;
            AiResponseError aiResponseError = new AiResponseError();
            aiResponseError.Show();
        }


        /// <summary>
        /// Instert data in string by a specfic patern
        /// </summary>
        /// <param name="data"></param>
        /// <param name="patern"></param>
        /// <param name="insertedData"></param>
        /// <returns></returns>
        private static string InsertData(string data, string patern, string insertedData)
        {
            string outData = string.Empty;
            List<string> dataList = new List<string>();
            StringReader reader = new StringReader(data);
            string line = string.Empty;
            int result = 0, count = 0;

            while ((line = reader.ReadLine()) != null)
            {
                count++;
                if (line.Contains("]*/"))
                    result = count;
                dataList.Add(line);
            }
            dataList.Insert(result, insertedData);
            outData = string.Join("\n", dataList);
            s_line = result + insertedData.Split('\n').Count() + 10;
            return outData;
        }

        /// <summary>
        /// Load progress bar.
        /// </summary>
        public static void LoadProgressBar()
        {
            MainForm.Instance.progressBar.Visible = true;
            MainForm.Instance.aiLabel.Visible = true;
            int centerX = (MainForm.Instance.Width - MainForm.Instance.progressBar.Width) / 2;
            int centerY = (MainForm.Instance.Height - MainForm.Instance.progressBar.Height) / 2;
            MainForm.Instance.progressBar.Location = new Point(centerX, centerY);
            MainForm.Instance.aiLabel.Location = new Point(centerX - 4, centerY - 20);
            MainForm.Instance.progressBar.BringToFront();
            MainForm.Instance.aiLabel.BringToFront();
        }
    }
}
