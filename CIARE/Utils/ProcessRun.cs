using System.Diagnostics;

namespace CIARE.Utils
{
    public class ProcessRun
    {
        private string ProcessToRun { get; set; }
        private string Arguments { get; set; }
        private string WorkingDirectory { get; set; }

        /// <summary>
        /// Run process with arguments and diferent working directory.
        /// </summary>
        /// <param name="processToRun">Process name to run.</param>
        /// <param name="arguments">Arguments passed to process.</param>
        /// <param name="workingDirectory">Working directory</param>
        public ProcessRun(string processToRun, string arguments, string workingDirectory)
        {
            ProcessToRun = processToRun;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// Run procces.
        /// </summary>
        /// <returns></returns>
        public string Run()
        {
            string outData = string.Empty;
            if (string.IsNullOrEmpty(ProcessToRun) || string.IsNullOrEmpty(Arguments) || string.IsNullOrEmpty(WorkingDirectory))
                return "Error: Check process parameters!";

            ProcessStartInfo startInfo = new ProcessStartInfo(ProcessToRun);
            startInfo.UseShellExecute = false;
            startInfo.Arguments = Arguments;
            startInfo.WorkingDirectory = WorkingDirectory;
            startInfo.StandardOutputEncoding = true;
            startInfo.StandardInputEncoding = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(startInfo);
            outData += startInfo.StandardErrorEncoding;
            outData += startInfo.StandardOutputEncoding;
            return outData;
        }
    }
}
