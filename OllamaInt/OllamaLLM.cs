using Microsoft.Extensions.AI;
using System.Diagnostics;
namespace OllamaInt
{
    public class OllamaLLM
    {
        public string Model { get; set; }
        public string Uri { get; set; }

        /// <summary>
        /// Constructor for Ollama.
        /// </summary>
        public OllamaLLM()
        {
        }


        /// <summary>
        /// Check if ollama is installed.
        /// </summary>
        /// <returns></returns>
        public bool IsOllamaInstalled()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
            startInfo.Arguments = "/c ollama -h";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            var outData = process.StandardOutput.ReadToEnd();
            return outData.Contains("ollama [flags]");
        }

        /// <summary>
        /// List models from ollama
        /// </summary>
        /// <returns></returns>
        public List<string> LocalModels()
        {
            List<string> modelList = new List<string>();
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
            startInfo.UseShellExecute = false;
            startInfo.Arguments = "/c ollama list";
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            var outData = process.StandardOutput.ReadToEnd();
            using (var reader = new StringReader(outData))
            {
                string line;
                while (null != (line = reader.ReadLine()))
                {
                    if (!line.StartsWith("NAME"))
                    {
                        var model = line.Split(' ')[0].Trim();
                        if (!string.IsNullOrEmpty(model))
                        {
                            modelList.Add(model);
                        }
                    }
                }
            }
            return modelList;
        }
    }
}
