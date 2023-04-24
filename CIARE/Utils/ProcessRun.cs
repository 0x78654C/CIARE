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
        /// Run procces with console output redirect.
        /// </summary>
        /// <returns></returns>
        public string Run()
        {
            string outData = string.Empty;
            if (string.IsNullOrEmpty(ProcessToRun) || string.IsNullOrEmpty(Arguments) || string.IsNullOrEmpty(WorkingDirectory))
                return "Error: Check process parameters!";

            ProcessStartInfo startInfo = new ProcessStartInfo(ProcessToRun);
            startInfo.UseShellExecute = false;
            startInfo.Arguments =Arguments;
            startInfo.WorkingDirectory = WorkingDirectory;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = new Process();
            process.OutputDataReceived += (sender2, args) =>
            {
                outData +=args.Data + " \n";
            };
            process.ErrorDataReceived += (sender2, args) =>
            {
                outData += args.Data + " \n";
            };
            process.StartInfo = startInfo;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return outData;
        }

        /// <summary>
        /// Run process in visible mode without wait for exit.
        /// </summary>
        public void RunVisible()
        {
            if (string.IsNullOrEmpty(ProcessToRun) || string.IsNullOrEmpty(Arguments))
                return;
            ProcessStartInfo startInfo = new ProcessStartInfo(ProcessToRun);
            startInfo.UseShellExecute = false;
            startInfo.Arguments = "\""+Arguments+"\"";
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
        }

        /// <summary>
        /// Count the active processes by name.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static int CheckActiveProcessCount(string processName) => Process.GetProcessesByName(processName).Length;
    }
}
