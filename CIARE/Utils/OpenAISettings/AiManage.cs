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

namespace CIARE.Utils.OpenAISettings
{
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
            var result =string.Empty;
            try
            {
                if (string.IsNullOrEmpty(ApiKey))
                    return "";

                if (string.IsNullOrEmpty(Qestion))
                    return "";
                OpenAiApiV1Client client = new OpenAiApiV1Client(HttpClient, ApiKey);

                var resu = await client.PostCompletion(new CompletionRequest
                {
                    Max_Tokens = Int32.Parse(GlobalVariables.aiMaxTokens),
                    Temperature = 0.8m,
                    Model = GlobalVariables.model,
                    Prompt = Qestion
                });

                result= resu.Choices.First().Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
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
            outPut = $"//----------{question}----------{outPut}\n//-----------------------------------";
            textEditorControl.Text = InsertData(textEditorControl.Text, "]*/",outPut).Replace($"/*[{question}]*/","");
            GoToLineNumber.GoToLine(textEditorControl, s_line);
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
            int result = 0 ,count = 0;
            
            while ((line = reader.ReadLine()) != null)
            {
                count++;
                if (line.Contains("]*/"))
                    result = count;
                dataList.Add(line);
            }
            dataList.Insert(result, insertedData);
            outData = string.Join("\n", dataList);
            s_line = result + insertedData.Length;
            return outData;
        }
    }
}
