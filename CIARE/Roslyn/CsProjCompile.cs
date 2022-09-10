using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Roslyn
{
    public class CsProjCompile
    {
        private string FileName { get; set; }
        private string BinaryPath { get; set; }
        private string Code { get; set; }

        private string CsProjTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
</Project>
";
        /// <summary>
        /// Compile csproject.
        /// </summary>
        /// <param name="binaryName">Binary file name</param>
        /// <param name="binaryPath">CIARE binary path</param>
        /// <param name="code">Provided code for compile.</param>
        public CsProjCompile(string binaryName, string binaryPath, string code)
        {
            FileName = binaryName;
            BinaryPath = binaryPath;
            Code = code;
        }

        /// <summary>
        /// Build the Project
        /// </summary>
        /// <param name="logOutput"></param>
        public void Build(RichTextBox logOutput)
        {
            if (!Directory.Exists(BinaryPath))
                ErrorDisplay(logOutput, $"ERROR: Directory does not exist -> {BinaryPath}");

            string exeName = FileName.Substring(0, FileName.Length - 4);
            string projectDir = BinaryPath + exeName;
            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, true);

            Directory.CreateDirectory(projectDir);
            File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplate);
            File.WriteAllText($"{projectDir}\\{exeName}.cs", Code);
            ProcessRun processRun = new ProcessRun("dotnet", "build", projectDir);
            string build = processRun.Run();
            if (build.Contains("error"))
                logOutput.Text = build;
            else
            {
                logOutput.Text = build;
                logOutput.Text += $"pro -> {projectDir}\\bin\\Debug\\net6.0-windows\\{FileName}";
            }
            logOutput.SelectionStart = logOutput.Text.Length;
            logOutput.ScrollToCaret();
        }

        /// <summary>
        /// Display error messages with red color.
        /// </summary>
        /// <param name="logOutput"></param>
        /// <param name="message"></param>
        private void ErrorDisplay(RichTextBox logOutput, string message)
        {
            logOutput.ForeColor = Color.Red;
            logOutput.Text = message;
            logOutput.ForeColor = Color.White;
        }
    }
}
