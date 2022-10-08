using CIARE.GUI;
using CIARE.Utils;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CIARE.Roslyn
{
    //TODO: implement /clp:ErrorsOnly as GUI option
    //TODO1: implement param /p:Platform=
    public class CsProjCompile
    {
        private string FileName { get; set; }
        private string BinaryPath { get; set; }
        private string Code { get; set; }
        private bool Library { get; set; } = false;
        private string _exeFilePath;
        private string CsProjTemplateExe = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>"+GlobalVariables.Framework+@"</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
</Project>
";
        private string CsProjTemplateDll = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>"+GlobalVariables.Framework+@"</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
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
        public CsProjCompile(string binaryName, string binaryPath, string code, bool library)
        {
            FileName = binaryName;
            BinaryPath = binaryPath;
            Code = code;
            Library = library;
        }

        /// <summary>
        /// Build the Project
        /// </summary>
        /// <param name="logOutput"></param>
        public void Build(RichTextBox logOutput)
        {
            if (!Directory.Exists(BinaryPath))
               RichExtColor.ErrorDisplay(logOutput, $"ERROR: Directory does not exist -> {BinaryPath}");

            string exeName = FileName.Substring(0, FileName.Length - 4);
            string projectDir = BinaryPath + exeName;
            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, true);

            Directory.CreateDirectory(projectDir);
            if (Library)
                File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplateDll);
            else
                File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplateExe);
            File.WriteAllText($"{projectDir}\\{exeName}.cs", Code);
            string param = $"build {GlobalVariables.configParam} {GlobalVariables.platformParam}";
            ProcessRun processRun = new ProcessRun("dotnet", param, projectDir);
            string build = processRun.Run();
            if (build.Contains("error"))
                logOutput.Text = build;
            else
            {
                if (GlobalVariables.OWarnings)
                    logOutput.Text = build;

                PathExe(projectDir, exeName);
                if (!string.IsNullOrEmpty(_exeFilePath))
                    logOutput.Text += $"Build succeeded.\n\n  {exeName} -> {_exeFilePath}";
            }
            logOutput.SelectionStart = logOutput.Text.Length;
            logOutput.ScrollToCaret();
        }

        /// <summary>
        /// Get compiled exe file path from project directory.
        /// </summary>
        /// <param name="pathProject"></param>
        /// <param name="projectName"></param>
        private void PathExe(string pathProject, string projectName)
        {
            var directories = Directory.GetDirectories(pathProject);
            foreach (var dir in directories)
            {
                if (!dir.EndsWith("obj"))
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.FullName.EndsWith($"{projectName}.exe"))
                            _exeFilePath = fileInfo.FullName;
                    }
                    PathExe(dir, projectName);
                }
            }
        }
    }
}
