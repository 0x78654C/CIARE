using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("Windows")]
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
    <TargetFramework>" + GlobalVariables.Framework + @"</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <Nullable>enable</Nullable>
<AllowUnsafeBlocks>" + GlobalVariables.OUnsafeCode.ToString()+@"</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='" + StateCompile+@"|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
" + SetReference(GlobalVariables.filteredCustomRef, GlobalVariables.nugetNames) + @"
</Project>
";
        private string CsProjTemplateDll = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>" + GlobalVariables.Framework + @"</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <Nullable>enable</Nullable>
<AllowUnsafeBlocks>" + GlobalVariables.OUnsafeCode.ToString()+@"</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='" + StateCompile+@"|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
" + SetReference(GlobalVariables.filteredCustomRef, GlobalVariables.nugetNames) + @"
</Project>
";

        private string CsProjTemplatePublish = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>"+GlobalVariables.binarytypeTemplate +@"</OutputType>
    <TargetFramework>" + GlobalVariables.Framework + @"</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <PublishAot>true</PublishAot>
    <Nullable>enable</Nullable>
<AllowUnsafeBlocks>" + GlobalVariables.OUnsafeCode.ToString() + @"</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='" + StateCompile + @"|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
" + SetReference(GlobalVariables.filteredCustomRef, GlobalVariables.nugetNames) + @"
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
        /// Set optimization type.
        /// </summary>
        private static string StateCompile => (GlobalVariables.configParam.Contains("Release")) ? "Release" : "Debug";

        /// <summary>
        /// Set reference link template for csproj file extension from the loaded list in reference manager.
        /// </summary>
        /// <param name="refList"></param>
        /// <returns></returns>
        private static string SetReference(List<string> refList, List<string> nugetNames)
        {
            string outRef = string.Empty;
            refList = refList.Distinct().ToList();
            nugetNames = nugetNames.Distinct().ToList();

            foreach(var nameVersion in nugetNames)
            {
                if (!string.IsNullOrEmpty(nameVersion))
                {
                    var name = nameVersion.Split('|')[0];
                    var version = nameVersion.Split('|')[1];
                    string format = $@"<ItemGroup>
    <PackageReference Include=""" + name + @""" Version="""+version+@"""/>
</ItemGroup>";
                    outRef += Environment.NewLine + format;
                }
            }

            foreach (var item in refList)
            {
                var assemblyName = CustomRef.GetAssemblyNamespace(item);
                if (item != assemblyName && !string.IsNullOrEmpty(assemblyName))
                {
                    string format = $@"<ItemGroup>
  <Reference Include=""" + assemblyName + @""">
    <HintPath>" + item + @"</HintPath>
  </Reference>
</ItemGroup>";
                outRef += Environment.NewLine + format;
                }
            }
            return outRef;
        }

        /// <summary>
        /// Build the Project
        /// </summary>
        /// <param name="logOutput"></param>
        public void Build(RichTextBox logOutput)
        {
            try
            {
                if (!Directory.Exists(BinaryPath))
                    RichExtColor.ErrorDisplay(logOutput, $"ERROR: Directory does not exist -> {BinaryPath}");

                string exeName = FileName.Substring(0, FileName.Length - 4);
                string projectDir = BinaryPath + exeName;
                if (!Directory.Exists(projectDir))
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
                    string framework = GlobalVariables.Framework.Split('-')[0];
                    PathExe(projectDir, exeName, framework, GlobalVariables.platformParam);
                    if (!string.IsNullOrEmpty(_exeFilePath))
                        logOutput.Text += $"Build succeeded.\n\n  {exeName} -> {_exeFilePath}";
                }
                logOutput.SelectionStart = logOutput.Text.Length;
                logOutput.ScrollToCaret();
            }
            catch (UnauthorizedAccessException uae)
            {
                RichExtColor.ErrorDisplay(logOutput, $"ERROR: {uae.Message}. Process may be running!");
                GlobalVariables.compileTime = true;
            }
            catch (Exception e)
            {
                RichExtColor.ErrorDisplay(logOutput, $"ERROR: {e.Message}");
                GlobalVariables.compileTime = true;
            }
        }

        /// <summary>
        /// Get compiled exe file path from project directory.
        /// </summary>
        /// <param name="pathProject"></param>
        /// <param name="projectName"></param>
        private void PathExe(string pathProject, string projectName, string framework, string platform)
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
                        int pathSplit = fileInfo.FullName.Split(Path.DirectorySeparatorChar).Count();
                        string frameworkPath = fileInfo.FullName.Split(Path.DirectorySeparatorChar)[pathSplit - 2];
                        string parsePlatform = platform.Split('"')[1];
                        if (fileInfo.FullName.EndsWith($"{projectName}.exe") && frameworkPath.Contains(framework) 
                            && fileInfo.FullName.Contains(parsePlatform) && GetState(fileInfo.FullName).Contains(StateCompile))
                        {
                            _exeFilePath = fileInfo.FullName;
                            break;
                        }
                        if (fileInfo.FullName.EndsWith($"{projectName}.dll") && frameworkPath.Contains(framework)
                            && fileInfo.FullName.Contains(parsePlatform) && GetState(fileInfo.FullName).Contains(StateCompile))
                        {
                            _exeFilePath = fileInfo.FullName;
                        }
                    }
                    PathExe(dir, projectName, framework, platform);
                }
            }
        }

        /// <summary>
        /// Get optimization level state from full path after compile.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetState(string path)
        {
            var splitCount = path.Split(Path.DirectorySeparatorChar).Count();
            var getType = path.Split(Path.DirectorySeparatorChar)[splitCount - 3];
            return getType;
        }
    }
}
