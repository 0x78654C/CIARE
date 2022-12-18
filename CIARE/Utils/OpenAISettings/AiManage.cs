using ICSharpCode.TextEditor;
using OpenAI.Api.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Utils.OpenAISettings
{
    public class AiManage
    {
        private string ApiKey = string.Empty;
        private string Qestion = string.Empty;
        private static HttpClient HttpClient = new HttpClient();

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
            if (string.IsNullOrEmpty(ApiKey))
                return "";

            if (string.IsNullOrEmpty(Qestion))
                return "";
            OpenAiApiV1Client client = new OpenAiApiV1Client(HttpClient, ApiKey);

            var resu = await client.PostCompletion(new OpenAI.Api.Client.Models.CompletionRequest
            {
                MaxTokens = Int32.Parse(GlobalVariables.aiMaxTokens),
                Temperature = 0.8m,
                Model = "text-davinci-003",
                Prompt = Qestion
            });
            return resu.Choices.First().Text;
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
            {
                outPut += $"{Environment.NewLine}{line}";
            }
            textEditorControl.Text += $"{Environment.NewLine}/* {outPut} */";
        }
    }
}
