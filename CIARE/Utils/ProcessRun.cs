using System.Diagnostics;
using System.Threading.Tasks;

namespace CIARE.Utils
{
    public sealed class ProcessRunResult
    {
        public ProcessRunResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output ?? string.Empty;
        }

        public int ExitCode { get; }
        public string Output { get; }
        public bool Success => ExitCode == 0;

        public override string ToString() => Output;
    }

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
            return RunWithResult().Output;
        }

        /// <summary>
        /// Run process with console output redirect and return the process exit code.
        /// </summary>
        public ProcessRunResult RunWithResult()
        {
            if (string.IsNullOrEmpty(ProcessToRun) || string.IsNullOrEmpty(Arguments) || string.IsNullOrEmpty(WorkingDirectory))
                return new ProcessRunResult(-1, "Error: Check process parameters!");

            ProcessStartInfo startInfo = new ProcessStartInfo(ProcessToRun);
            startInfo.UseShellExecute = false;
            startInfo.Arguments = Arguments;
            startInfo.WorkingDirectory = WorkingDirectory;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                Task.WaitAll(outputTask, errorTask);

                string outData = CombineOutput(outputTask.Result, errorTask.Result);
                if (string.IsNullOrWhiteSpace(outData) && process.ExitCode != 0)
                    outData = $"{ProcessToRun} exited with code {process.ExitCode}.";

                return new ProcessRunResult(process.ExitCode, outData);
            }
        }

        private static string CombineOutput(string standardOutput, string standardError)
        {
            if (string.IsNullOrWhiteSpace(standardOutput))
                return standardError ?? string.Empty;

            if (string.IsNullOrWhiteSpace(standardError))
                return standardOutput;

            return standardOutput.TrimEnd() + "\n" + standardError;
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
            startInfo.Arguments = "\"" + Arguments + "\"";
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
