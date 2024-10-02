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
        private bool Publish { get; set; } = false;
        private string _exeFilePath;
        private string _pathNative;
        private string CsProjTemplateExe = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>" + GlobalVariables.Framework + @"</TargetFramework>
	  <UseWindowsForms>"+GlobalVariables.winForms.ToString() + @"</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <Nullable>enable</Nullable>
<AllowUnsafeBlocks>" + GlobalVariables.OUnsafeCode.ToString() +@"</AllowUnsafeBlocks>
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
	  <UseWindowsForms>"+GlobalVariables.winForms.ToString() + @"</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>
    <Nullable>enable</Nullable>
<AllowUnsafeBlocks>" + GlobalVariables.OUnsafeCode.ToString() + @"</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='" + StateCompile+@"|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
" + SetReference(GlobalVariables.filteredCustomRef, GlobalVariables.nugetNames) + @"
</Project>
";

        private string CsProjTemplatePublish = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>"+GlobalVariables.binarytypeTemplate + @"</OutputType>
    <UseWindowsForms>"+GlobalVariables.winForms.ToString() + @"</UseWindowsForms>
    <TargetFramework>" + GlobalVariables.Framework + @"</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningLevel>0</WarningLevel>"+GlobalVariables.publishAot+@"
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
        public CsProjCompile(string binaryName, string binaryPath, string code, bool library, bool publish)
        {
            FileName = binaryName;
            BinaryPath = binaryPath;
            Code = code;
            Library = library;
            Publish = publish;
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
                File.Delete($"{projectDir}\\{exeName}.csproj");
                if (Publish)
                    File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplatePublish);
                else if (Library)
                    File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplateDll);
                else
                    File.WriteAllText($"{projectDir}\\{exeName}.csproj", CsProjTemplateExe);
                File.WriteAllText($"{projectDir}\\{exeName}.cs", Code);
                var param = "";
                if (Publish)
                {
                    if (!GlobalVariables.platformParam.Contains("x64"))
                    {
                        RichExtColor.ErrorDisplay(logOutput, $"ERROR: Native AOT publish works only with x64 arhitecture!");
                        return;
                    }
                    param = $"publish -r win-x64 -c {StateCompile}";
                }
                else
                    param = $"build {GlobalVariables.configParam} {GlobalVariables.platformParam}";
                ProcessRun processRun = new ProcessRun("dotnet", param, projectDir);
                string build = processRun.Run();
                if (build.Contains("error"))
                    logOutput.Text = build.Trim();
                else
                {
                    logOutput.Text = build.Trim()+"\nDone!";
                }
                logOutput.SelectionStart = logOutput.Text.Length;
                logOutput.ScrollToCaret();
            }
            catch (UnauthorizedAccessException uae)
            {
                RichExtColor.ErrorDisplay(logOutput, $"ERROR: {uae.Message}. Process may be running!");
            }
            catch (Exception e)
            {
                RichExtColor.ErrorDisplay(logOutput, $"ERROR: {e.Message}");
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
