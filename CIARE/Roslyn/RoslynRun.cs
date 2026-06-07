using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Diagnostics;
using CIARE.Utils;
using CIARE.GUI;
using ICSharpCode.TextEditor;
using System.Runtime.Versioning;
using Path = System.IO.Path;
using System.Collections.Immutable;
using System.Runtime.Loader;
using System.Drawing.Text;
using CIARE.Utils.OpenAISettings;
using CIARE.Utils.Options;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("windows")]
    public class RoslynRun
    {
        /* Class for compile and run C# code using Roslyn */
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static string[] s_commandLineArguments = null;
        private static string s_errorCode = "";

        // Cached platform references (never change at runtime).
        private static ImmutableArray<MetadataReference> _platformRefsCompile;
        private static readonly object _compileRefLock = new object();
        private static string s_errorMessage = "";
        private static string s_codeAI = "";
        private static string s_line = "";

        private sealed class ProjectBuildCommand
        {
            public ProjectBuildCommand(string processName, string arguments, string displayName, bool usesFullFrameworkMsBuild)
            {
                ProcessName = processName;
                Arguments = arguments;
                DisplayName = displayName;
                UsesFullFrameworkMsBuild = usesFullFrameworkMsBuild;
            }

            public string ProcessName { get; }
            public string Arguments { get; }
            public string DisplayName { get; }
            public bool UsesFullFrameworkMsBuild { get; }
        }

        /// <summary>
        /// Compile and run C# using Roslyn.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="param"></param>
        /// <param name="richTextBox"></param>
        public static void CompileAndRun(string code, RichTextBox richTextBox, bool allowUnsafe)
        {
            AssemblyLoadContext runContext = null;
            try
            {

                if (string.IsNullOrEmpty(code))
                {
                    richTextBox.Text = "ERROR: There is no code in the editor to run!";
                    return;
                }
                s_commandLineArguments = SplitArguments.CommandLineToArgs(GlobalVariables.commandLineArguments) ?? Array.Empty<string>();
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();
                s_stopWatch.Start();
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, SetLanguageVersion(GlobalVariables.Framework));
                string assemblyName = Path.GetRandomFileName();
                string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: References(false),
                    options: new CSharpCompilationOptions(OutputKind.WindowsApplication, true, null, null,
                     null, null, OptimizationLevelState(), false, allowUnsafe, null, null,
                     ImmutableArray.Create<byte>(new byte[] { }), false, Platform.AnyCpu));
                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        foreach (Diagnostic diagnostic in failures)
                        {
                            richTextBox.Clear();
                            richTextBox.ForeColor = Color.Red;
                            richTextBox.ScrollToEnd();
                            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                            ErrorDisplay(richTextBox, diagnostic.Id, diagnostic.GetMessage(), line);
                        }
                    }
                    else
                    {
                        richTextBox.Clear();
                        ms.Seek(0, SeekOrigin.Begin);
                        runContext = new AssemblyLoadContext(null, isCollectible: true);
                        assembly = runContext.LoadFromStream(ms);
                    }
                    ms.Close();
                }
                MethodInfo myMethod = assembly.EntryPoint;
                myMethod.Invoke(null, new object[] { s_commandLineArguments });
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                if (richTextBox.Text.EndsWith("\n"))
                {
                    richTextBox.Text += $"---------------------------------\nCompile and code execution time: {s_timeSpan.Milliseconds} milliseconds";
                    richTextBox.ScrollToEnd();
                }
                else
                {
                    richTextBox.Text += $"\n---------------------------------\nCompile and code execution time: {s_timeSpan.Milliseconds} milliseconds";
                    richTextBox.ScrollToEnd();
                }
                richTextBox.ScrollToEnd();
            }
            catch (DivideByZeroException dbze)
            {
                richTextBox.Clear();
                richTextBox.Text += dbze.StackTrace;
            }
            catch (Exception st)
            {
                if (!richTextBox.Text.StartsWith("ERROR"))
                {
                    if (richTextBox.Text.EndsWith("\n"))
                    {
                        richTextBox.Text += $"---------------Stack Trace------------------\n";
                    }
                    else
                    {
                        richTextBox.Text += $"\n--------------Stack Trace-------------------\n";
                    }
                    richTextBox.Text += st.ToString();
                }
            }
            finally
            {
                runContext?.Unload();
            }
        }

        /// <summary>
        /// Binary C# code Compile.
        /// https://docs.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/compile-code-using-compiler
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exeFile"></param>
        /// <param name="outPut"></param>
        /// <param name="richTextBox"></param>
        public static void BinaryCompile(string code, bool exeFile, string outPut, RichTextBox richTextBox, bool allowUnsafe, OutputKind outputKind = OutputKind.ConsoleApplication)
        {
            string pathOutput = Application.StartupPath + "binary\\";
            string roslynDir = Application.StartupPath + "roslyn\\";
            try
            {
                if (!Directory.Exists(roslynDir))
                {
                    Directory.CreateDirectory(roslynDir);
                }
                if (string.IsNullOrEmpty(code))
                {
                    richTextBox.Text = "ERROR: There is no code in the editor to compile!";
                    return;
                }
                if (string.IsNullOrEmpty(GlobalVariables.binaryName))
                    return;

                if (string.IsNullOrEmpty(outPut))
                    return;

                if (GlobalVariables.darkColor)
                    richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
                else
                    richTextBox.ForeColor = Color.Black;

                if (!Directory.Exists(pathOutput))
                    Directory.CreateDirectory(pathOutput);
                if (GlobalVariables.OPublishNative && !exeFile)
                {
                    richTextBox.Text = "ERROR: Native AOT publish requires an executable output.";
                    return;
                }
                if (GlobalVariables.OPublishNative)
                    richTextBox.Text = "Publish Native AOT EXE binary file ...";
                else if (exeFile)
                    richTextBox.Text = "Compile EXE binary file ...";
                else
                    richTextBox.Text = "Compile DLL binary file ...";
                string Output = pathOutput + outPut;
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();
                s_stopWatch.Start();
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, SetLanguageVersion(GlobalVariables.Framework));
                string assemblyName = Path.GetRandomFileName();
                CSharpCompilation compilation;
                if (exeFile)
                {
                    compilation = CSharpCompilation.Create(
                      assemblyName,
                      syntaxTrees: new[] { syntaxTree },
                      references: References(true),
                      options: new CSharpCompilationOptions(outputKind, true, null, null,
                      null, null, OptimizationLevelState(), false, allowUnsafe, null, null,
                      ImmutableArray.Create<byte>(new byte[] { }), false, Platform.AnyCpu)); ;
                }
                else
                {
                    compilation = CSharpCompilation.Create(
                     assemblyName,
                     syntaxTrees: new[] { syntaxTree },
                     references: References(true),
                     options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, true, null, null,
                     null, null, OptimizationLevelState(), false, allowUnsafe, null, null,
                     ImmutableArray.Create<byte>(new byte[] { }), false, Platform.AnyCpu));
                }


                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        foreach (Diagnostic diagnostic in failures)
                        {
                            richTextBox.Clear();
                            richTextBox.ForeColor = Color.Red;
                            richTextBox.ScrollToEnd();
                            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                            ErrorDisplay(richTextBox, diagnostic.Id, diagnostic.GetMessage(), line);
                        }
                    }
                    else
                    {
                        richTextBox.Clear();
                        bool nativeAot = GlobalVariables.OPublishNative;
                        bool publish = GlobalVariables.binaryPublish || nativeAot;
                        CsProjCompile projCompile = new CsProjCompile(outPut, pathOutput, code, !exeFile, publish, nativeAot);
                        projCompile.Build(richTextBox);
                        s_stopWatch.Stop();
                        s_timeSpan = s_stopWatch.Elapsed;
                        richTextBox.ScrollToEnd();
                    }
                    s_stopWatch.Stop();
                    ms.Close();
                }
                GlobalVariables.binaryName = string.Empty;
            }
            catch (DivideByZeroException dbze)
            {
                richTextBox.Text += $"Error: {dbze.Message}\n";
                GlobalVariables.binaryName = string.Empty;
            }
            catch (Exception ex)
            {
                GlobalVariables.binaryName = string.Empty;
                richTextBox.Text = $"Error:{ex.Message}\n";
            }
        }

        /// <summary>
        /// Output Error message to richtextbox.
        /// </summary>
        /// <param name="richTextBox"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="lineNumber"></param>
        private static void ErrorDisplay(RichTextBox richTextBox, string errorId, string errorMessage, int lineNumber)
        {
            richTextBox.Text = $"ERROR: (Line {lineNumber}) | ID: {errorId} -> {errorMessage}";
            s_errorCode = errorId;
            s_errorMessage = $"{errorId}: {errorMessage}";
            s_line = (lineNumber - 1).ToString();
            GoToLineNumber.GoToLine(SelectedEditor.GetSelectedEditor(), lineNumber + 20);
            GoToLineNumber.GoToLine(SelectedEditor.GetSelectedEditor(), lineNumber);
            SendKeys.Send("{END}");
            var screenPosition = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.ScreenPosition;
            var colPos = screenPosition.Y;
            var start = new TextLocation(0, lineNumber - 1);
            var end = new TextLocation(colPos, lineNumber - 1);
            SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.SelectionManager.SetSelection(start, end);
            var lineSegment = SelectedEditor.GetSelectedEditor().Document.GetLineSegment(lineNumber - 1);
            s_codeAI = SelectedEditor.GetSelectedEditor().Text;
            screenPosition = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.ScreenPosition;
            var X = screenPosition.X;
            var Y = screenPosition.Y + 23;
            var pos = new Point(X, Y);
            var contextMenuStrip = new ContextMenuStrip();
            var itemMenu = new ToolStripMenuItem();
            var errorMesasgeSplited = DataManage.SplitTextByWordsInLine($"\u2196\n{errorId} -> {errorMessage}", 6);
            itemMenu.Text = errorMesasgeSplited;
            contextMenuStrip.Name = "Error Notification";
            itemMenu.BackColor = GlobalVariables.controlBgColor;
            itemMenu.ForeColor = Color.IndianRed;
            itemMenu.Font = new Font(new FontFamily(GenericFontFamilies.Monospace), 11.28f, FontStyle.Italic | FontStyle.Bold);
            itemMenu.Click += ItemMenu_Click;
            var itemMenuAI = new ToolStripMenuItem();
            var separator = new ToolStripSeparator();
            separator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDarkAI_Error;
            itemMenuAI.Text = "[ Ask AI for help you with this error? ]";
            itemMenuAI.BackColor = GlobalVariables.controlBgColor;
            itemMenuAI.ForeColor = Color.White;
            itemMenuAI.Font = new Font(new FontFamily(GenericFontFamilies.Monospace), 11.28f, FontStyle.Italic | FontStyle.Bold);
            itemMenuAI.Click += AskAI_Click;
            contextMenuStrip.Items.Add(itemMenu);
            if (!string.IsNullOrEmpty(GlobalVariables.aiKey.ConvertSecureStringToString()))
            {
                contextMenuStrip.Items.Add(separator);
                contextMenuStrip.Items.Add(itemMenuAI);
            }
            contextMenuStrip.Show(SelectedEditor.GetSelectedEditor().ActiveTextAreaControl, pos);
        }

        /// <summary>
        /// Open link to error documentation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ItemMenu_Click(object sender, EventArgs e)
        {
            var url = $"https://learn.microsoft.com/en-us/search/?terms={Uri.EscapeDataString(s_errorCode)}&category=Documentation";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// Get result from AI for error message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void AskAI_Click(object sender, EventArgs e)
        {
            AiManage.LoadProgressBar();
            AiManage.GetDataAIERR(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString(), s_codeAI, s_errorMessage, s_line, MainForm.Instance.outputRBT);
        }


        /// <summary>
        /// Compile and run C# code and controlers handle.
        /// </summary>
        public static void RunCode(RichTextBox outLogRtb, PictureBox runCodePb, TextEditorControl textEditor, SplitContainer splitContainer, bool runner)
        {
            if (GlobalVariables.darkColor)
                outLogRtb.ForeColor = Color.FromArgb(192, 215, 207);
            else
                outLogRtb.ForeColor = Color.Black;

            outLogRtb.Text = "Compile and Running..\n";
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            runCodePb.Image = Properties.Resources.runButton_gray;
            runCodePb.Enabled = false;
            try
            {
                if (!TryRunActiveProject(outLogRtb))
                    CompileAndRun(textEditor.Text, outLogRtb, GlobalVariables.OUnsafeCode);
            }
            finally
            {
                RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
                runCodePb.Image = Properties.Resources.runButton21;
                runCodePb.Enabled = true;
                GC.Collect();
            }
        }

        /// <summary>
        /// Compile code to EXE binary file method.
        /// </summary>
        public static void CompileBinary(TextEditorControl textEditor, SplitContainer splitContainer, RichTextBox outLogRtb, bool runner, OutputKind outputKind = OutputKind.ConsoleApplication)
        {
            if (TryCompileActiveProject(splitContainer, outLogRtb, runner))
                return;

            BinaryName binaryName = new BinaryName();
            var code = textEditor.Text;
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("There is no code in the editor to compile!", "CIARE", MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
                return;
            }
            if (!GlobalVariables.checkFormOpen)
                binaryName.ShowDialog();
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            if (GlobalVariables.binarytype == ".exe")
                BinaryCompile(textEditor.Text, true, GlobalVariables.binaryName, outLogRtb, GlobalVariables.OUnsafeCode, outputKind);
            else
                BinaryCompile(textEditor.Text, false, GlobalVariables.binaryName, outLogRtb, GlobalVariables.OUnsafeCode);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            GC.Collect();
        }

        private static bool TryCompileActiveProject(SplitContainer splitContainer, RichTextBox outLogRtb, bool runner)
        {
            string projectPath = string.Empty;

            try
            {
                projectPath = MainForm.Instance?.GetActiveCompileProjectPath() ?? string.Empty;
            }
            catch
            {
                projectPath = string.Empty;
            }

            if (string.IsNullOrEmpty(projectPath))
                return false;

            projectPath = PreferActiveNativeAotProject(projectPath);

            if (GlobalVariables.darkColor)
                outLogRtb.ForeColor = Color.FromArgb(192, 215, 207);
            else
                outLogRtb.ForeColor = Color.Black;

            OutputWindowManage.ShowOutputWindow(splitContainer, outLogRtb);
            BuildExistingProject(projectPath, outLogRtb, GlobalVariables.binaryPublish);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            GC.Collect();
            return true;
        }

        private static bool TryRunActiveProject(RichTextBox outLogRtb)
        {
            string projectPath = string.Empty;

            try
            {
                projectPath = MainForm.Instance?.GetActiveRunProjectPath() ?? string.Empty;
            }
            catch
            {
                projectPath = string.Empty;
            }

            if (string.IsNullOrEmpty(projectPath))
                return false;

            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            RunExistingProject(projectPath, outLogRtb);
            return true;
        }

        private static string PreferActiveNativeAotProject(string buildTargetPath)
        {
            if (!GlobalVariables.OPublishNative ||
                !string.Equals(Path.GetExtension(buildTargetPath), ".sln", StringComparison.OrdinalIgnoreCase))
            {
                return buildTargetPath;
            }

            try
            {
                string activeProject = MainForm.Instance?.GetActivePackageInstallProjectPath() ?? string.Empty;
                return ProjectHasPublishAotEnabled(activeProject) ? activeProject : buildTargetPath;
            }
            catch
            {
                return buildTargetPath;
            }
        }

        private static void RunExistingProject(string projectPath, RichTextBox logOutput)
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    RichExtColor.ErrorDisplay(logOutput, $"ERROR: Project file does not exist -> {projectPath}");
                    return;
                }

                string workingDirectory = Path.GetDirectoryName(projectPath);
                string configuration = ExistingProjectConfiguration();
                string platform = ExistingProjectPlatformDotnetArgument(projectPath);
                string arguments = $"run --project {QuoteProcessArgument(projectPath)} --configuration {configuration} {platform}".Trim();

                logOutput.Text = $"Run project ...\n{projectPath}";
                ProcessRunResult run = RunBuildCommand(
                    new ProjectBuildCommand("dotnet", arguments, ".NET SDK", false), workingDirectory);
                string runOutput = FormatBuildOutput(run);
                if (!run.Success || BuildHasErrors(runOutput))
                    logOutput.Text = runOutput + $"\nRun completed with errors! (exit code {run.ExitCode})";
                else
                    logOutput.Text = runOutput + "\nDone!";

                logOutput.SelectionStart = logOutput.Text.Length;
                logOutput.ScrollToCaret();
            }
            catch (Exception e)
            {
                RichExtColor.ErrorDisplay(logOutput, $"ERROR: {e.Message}");
            }
        }

        private static void BuildExistingProject(string projectPath, RichTextBox logOutput, bool publish)
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    RichExtColor.ErrorDisplay(logOutput, $"ERROR: Project file does not exist -> {projectPath}");
                    return;
                }

                bool nativeAot = GlobalVariables.OPublishNative &&
                    BuildTargetHasPublishAotEnabled(projectPath);
                bool effectivePublish = publish || nativeAot;
                string nativeAotRuntimeIdentifier = nativeAot
                    ? ExistingProjectNativeAotRuntimeIdentifier()
                    : string.Empty;
                if (nativeAot && string.IsNullOrEmpty(nativeAotRuntimeIdentifier))
                {
                    RichExtColor.ErrorDisplay(logOutput, "ERROR: Native AOT publish works only with x64 architecture!");
                    return;
                }

                string action = nativeAot ? "Publish Native AOT" : effectivePublish ? "Publish" : "Build";
                logOutput.Text = $"{action} project ...\n{projectPath}";

                string workingDirectory = Path.GetDirectoryName(projectPath);
                ProjectBuildCommand command = CreateBuildCommand(projectPath, effectivePublish,
                    BuildTargetPrefersFullFrameworkMsBuild(projectPath), nativeAotRuntimeIdentifier);
                ProcessRunResult build = RunBuildCommand(command, workingDirectory);

                if (!command.UsesFullFrameworkMsBuild && RequiresFullFrameworkMsBuild(build.Output))
                {
                    ProjectBuildCommand msBuildCommand = CreateBuildCommand(projectPath, effectivePublish, true,
                        nativeAotRuntimeIdentifier);
                    if (msBuildCommand.UsesFullFrameworkMsBuild)
                    {
                        command = msBuildCommand;
                        ProcessRunResult retryBuild = RunBuildCommand(command, workingDirectory);
                        build = new ProcessRunResult(retryBuild.ExitCode,
                            "dotnet build cannot handle this project because it requires ResolveComReference. " +
                            $"Retrying with {command.DisplayName}...\n\n" + retryBuild.Output);
                    }
                    else
                    {
                        build = new ProcessRunResult(build.ExitCode,
                            build.Output.Trim() + "\n\nThis project requires the .NET Framework version of MSBuild because it uses COM references. " +
                            "Install Visual Studio or Build Tools with the .NET desktop build workload, then retry.");
                    }
                }

                string buildOutput = FormatBuildOutput(build);
                if (!build.Success || BuildHasErrors(buildOutput))
                    logOutput.Text = buildOutput + $"\nCompleted task with errors! ({command.DisplayName}, exit code {build.ExitCode})";
                else
                    logOutput.Text = buildOutput + $"\nDone! ({command.DisplayName})";

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

        private static ProjectBuildCommand CreateBuildCommand(string projectPath, bool publish,
            bool preferFullFrameworkMsBuild, string nativeAotRuntimeIdentifier)
        {
            if (preferFullFrameworkMsBuild && TryFindFullFrameworkMsBuild(out string msBuildPath))
            {
                return new ProjectBuildCommand(msBuildPath,
                    BuildExistingProjectMsBuildArguments(projectPath, publish, nativeAotRuntimeIdentifier),
                    "Visual Studio MSBuild", true);
            }

            return new ProjectBuildCommand("dotnet",
                BuildExistingProjectDotnetArguments(projectPath, publish, nativeAotRuntimeIdentifier),
                ".NET SDK MSBuild", false);
        }

        private static ProcessRunResult RunBuildCommand(ProjectBuildCommand command, string workingDirectory)
        {
            ProcessRun processRun = new ProcessRun(command.ProcessName, command.Arguments, workingDirectory);
            return processRun.RunWithResult();
        }

        private static string BuildExistingProjectDotnetArguments(string projectPath, bool publish,
            string nativeAotRuntimeIdentifier)
        {
            string quotedProjectPath = QuoteProcessArgument(projectPath);
            string configuration = ExistingProjectConfiguration();
            string platform = ExistingProjectPlatformDotnetArgument(projectPath);
            string runtime = string.IsNullOrEmpty(nativeAotRuntimeIdentifier)
                ? string.Empty
                : $"--runtime {nativeAotRuntimeIdentifier}";
            string publishAotOverride = publish
                ? $"--property:PublishAot={(string.IsNullOrEmpty(nativeAotRuntimeIdentifier) ? "false" : "true")}"
                : string.Empty;

            if (publish)
                return $"publish {quotedProjectPath} --configuration {configuration} {platform} {publishAotOverride} {runtime}".Trim();

            return $"build {quotedProjectPath} --configuration {configuration} {platform}".Trim();
        }

        private static string BuildExistingProjectMsBuildArguments(string projectPath, bool publish,
            string nativeAotRuntimeIdentifier)
        {
            string quotedProjectPath = QuoteProcessArgument(projectPath);
            string configuration = ExistingProjectConfiguration();
            string platform = ExistingProjectPlatformMsBuildArgument(projectPath);
            string target = publish ? "Publish" : "Build";
            string runtime = string.IsNullOrEmpty(nativeAotRuntimeIdentifier)
                ? string.Empty
                : $"/p:RuntimeIdentifier={nativeAotRuntimeIdentifier}";
            string publishAotOverride = publish
                ? $"/p:PublishAot={(string.IsNullOrEmpty(nativeAotRuntimeIdentifier) ? "false" : "true")}"
                : string.Empty;

            return $"{quotedProjectPath} /restore /t:{target} /p:Configuration={configuration} {platform} {publishAotOverride} {runtime}".Trim();
        }

        private static string ExistingProjectConfiguration() =>
            GlobalVariables.configParam.Contains("Release") ? "Release" : "Debug";

        private static string ExistingProjectPlatformDotnetArgument(string buildTargetPath)
        {
            string platform = ExistingProjectPlatform(buildTargetPath);
            return string.IsNullOrEmpty(platform) ? string.Empty : $"--property:Platform={QuoteProcessArgument(platform)}";
        }

        private static string ExistingProjectPlatformMsBuildArgument(string buildTargetPath)
        {
            string platform = ExistingProjectPlatform(buildTargetPath);
            return string.IsNullOrEmpty(platform) ? string.Empty : $"/p:Platform={QuoteProcessArgument(platform)}";
        }

        private static string ExistingProjectPlatform(string buildTargetPath)
        {
            string platform = ExistingProjectRequestedPlatform();
            if (string.IsNullOrEmpty(platform))
                return string.Empty;

            string extension = Path.GetExtension(buildTargetPath);
            if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase))
            {
                string configuration = ExistingProjectConfiguration();
                return FindMatchingSolutionPlatform(buildTargetPath, configuration, platform) ??
                    FindFallbackSolutionPlatform(buildTargetPath, configuration) ??
                    string.Empty;
            }

            if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                string configuration = ExistingProjectConfiguration();
                return FindMatchingProjectOutputPlatform(buildTargetPath, configuration, platform) ??
                    FindFallbackProjectOutputPlatform(buildTargetPath, configuration) ??
                    platform;
            }

            return platform;
        }

        private static string ExistingProjectRequestedPlatform()
        {
            if (string.IsNullOrWhiteSpace(GlobalVariables.platformParam))
                return string.Empty;

            const string platformKey = "Platform=";
            int platformIndex = GlobalVariables.platformParam.IndexOf(platformKey, StringComparison.OrdinalIgnoreCase);
            if (platformIndex < 0)
                return string.Empty;

            return GlobalVariables.platformParam
                .Substring(platformIndex + platformKey.Length)
                .Trim()
                .Trim('"');
        }

        private static string ExistingProjectNativeAotRuntimeIdentifier()
        {
            return string.Equals(ExistingProjectRequestedPlatform(), "x64", StringComparison.OrdinalIgnoreCase)
                ? "win-x64"
                : string.Empty;
        }

        private static bool BuildTargetHasPublishAotEnabled(string buildTargetPath)
        {
            return string.Equals(Path.GetExtension(buildTargetPath), ".csproj",
                    StringComparison.OrdinalIgnoreCase) &&
                ProjectHasPublishAotEnabled(buildTargetPath);
        }

        private static bool ProjectHasPublishAotEnabled(string projectPath)
        {
            if (!File.Exists(projectPath))
                return false;

            try
            {
                XDocument project = XDocument.Load(projectPath);
                return project.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "PublishAot",
                        StringComparison.OrdinalIgnoreCase))
                    .Any(element => string.Equals(element.Value.Trim(), "true",
                        StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private static string FindMatchingSolutionPlatform(string solutionPath, string configuration, string platform)
        {
            return ReadSolutionPlatforms(solutionPath, configuration)
                .FirstOrDefault(item => string.Equals(NormalizePlatformName(item), NormalizePlatformName(platform),
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string FindFallbackSolutionPlatform(string solutionPath, string configuration)
        {
            List<string> platforms = ReadSolutionPlatforms(solutionPath, configuration).ToList();
            foreach (string preferredPlatform in new[] { "AnyCPU", "x64", "x86" })
            {
                string platform = platforms.FirstOrDefault(item =>
                    string.Equals(NormalizePlatformName(item), preferredPlatform, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(platform))
                    return platform;
            }

            return platforms.FirstOrDefault();
        }

        private static string FindMatchingProjectOutputPlatform(string projectPath, string configuration,
            string platform)
        {
            return ReadProjectOutputPlatforms(projectPath, configuration)
                .FirstOrDefault(item => string.Equals(NormalizePlatformName(item), NormalizePlatformName(platform),
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string FindFallbackProjectOutputPlatform(string projectPath, string configuration)
        {
            List<string> platforms = ReadProjectOutputPlatforms(projectPath, configuration).ToList();
            foreach (string preferredPlatform in new[] { "AnyCPU", "x64", "x86" })
            {
                string platform = platforms.FirstOrDefault(item =>
                    string.Equals(NormalizePlatformName(item), preferredPlatform, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(platform))
                    return platform;
            }

            return platforms.FirstOrDefault();
        }

        private static IEnumerable<string> ReadProjectOutputPlatforms(string projectPath, string configuration)
        {
            var platforms = new List<string>();
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return platforms;

            try
            {
                XDocument project = XDocument.Load(projectPath);
                foreach (XElement outputPath in project.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "OutputPath",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    XElement propertyGroup = outputPath.Parent;
                    if (propertyGroup == null ||
                        !string.Equals(propertyGroup.Name.LocalName, "PropertyGroup",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (string condition in new[]
                    {
                        propertyGroup.Attribute("Condition")?.Value,
                        outputPath.Attribute("Condition")?.Value
                    })
                    {
                        if (!TryReadConfigurationPlatformCondition(condition, out string conditionConfiguration,
                                out string conditionPlatform) ||
                            !string.Equals(conditionConfiguration, configuration, StringComparison.OrdinalIgnoreCase) ||
                            platforms.Any(item => string.Equals(NormalizePlatformName(item),
                                NormalizePlatformName(conditionPlatform), StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        platforms.Add(conditionPlatform);
                    }
                }
            }
            catch
            {
                return Array.Empty<string>();
            }

            return platforms;
        }

        private static bool TryReadConfigurationPlatformCondition(string condition, out string configuration,
            out string platform)
        {
            configuration = string.Empty;
            platform = string.Empty;
            if (string.IsNullOrWhiteSpace(condition) ||
                condition.IndexOf("$(Configuration)", StringComparison.OrdinalIgnoreCase) < 0 ||
                condition.IndexOf("$(Platform)", StringComparison.OrdinalIgnoreCase) < 0 ||
                condition.IndexOf("==", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            foreach (Match match in Regex.Matches(condition,
                @"['""](?<configuration>[^'""|]+)\|(?<platform>[^'""]+)['""]",
                RegexOptions.CultureInvariant))
            {
                string matchedConfiguration = match.Groups["configuration"].Value.Trim();
                string matchedPlatform = match.Groups["platform"].Value.Trim();
                if (matchedConfiguration.IndexOf("$(", StringComparison.Ordinal) >= 0 ||
                    matchedPlatform.IndexOf("$(", StringComparison.Ordinal) >= 0)
                {
                    continue;
                }

                configuration = matchedConfiguration;
                platform = matchedPlatform;
                return !string.IsNullOrEmpty(configuration) && !string.IsNullOrEmpty(platform);
            }

            return false;
        }

        private static IEnumerable<string> ReadSolutionPlatforms(string solutionPath, string configuration)
        {
            var platforms = new List<string>();
            if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
                return platforms;

            bool inSolutionConfigurationSection = false;
            foreach (string rawLine in File.ReadLines(solutionPath))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("GlobalSection(SolutionConfigurationPlatforms)",
                        StringComparison.OrdinalIgnoreCase))
                {
                    inSolutionConfigurationSection = true;
                    continue;
                }

                if (inSolutionConfigurationSection &&
                    line.StartsWith("EndGlobalSection", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!inSolutionConfigurationSection)
                    continue;

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex <= 0)
                    continue;

                string configurationPlatform = line.Substring(0, equalsIndex).Trim();
                int separatorIndex = configurationPlatform.IndexOf('|');
                if (separatorIndex <= 0 || separatorIndex >= configurationPlatform.Length - 1)
                    continue;

                string configurationName = configurationPlatform.Substring(0, separatorIndex).Trim();
                string platformName = configurationPlatform.Substring(separatorIndex + 1).Trim();
                if (string.Equals(configurationName, configuration, StringComparison.OrdinalIgnoreCase) &&
                    !platforms.Any(item => string.Equals(item, platformName, StringComparison.OrdinalIgnoreCase)))
                {
                    platforms.Add(platformName);
                }
            }

            return platforms;
        }

        private static string NormalizePlatformName(string platform)
        {
            return (platform ?? string.Empty).Replace(" ", string.Empty);
        }

        private static string QuoteProcessArgument(string argument) =>
            "\"" + argument.Replace("\"", "\\\"") + "\"";

        private static bool BuildTargetPrefersFullFrameworkMsBuild(string buildTargetPath)
        {
            try
            {
                string extension = Path.GetExtension(buildTargetPath);
                if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
                    return ProjectFilePrefersFullFrameworkMsBuild(buildTargetPath);

                if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase))
                    return SolutionPrefersFullFrameworkMsBuild(buildTargetPath);
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool SolutionPrefersFullFrameworkMsBuild(string solutionPath)
        {
            foreach (string projectPath in GetSolutionProjectPaths(solutionPath))
            {
                if (ProjectFilePrefersFullFrameworkMsBuild(projectPath))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> GetSolutionProjectPaths(string solutionPath)
        {
            List<string> projectPaths = new List<string>();
            string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? string.Empty;

            try
            {
                foreach (string line in File.ReadLines(solutionPath))
                {
                    Match match = Regex.Match(line,
                        @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    if (!match.Success)
                        continue;

                    string projectPath = match.Groups[1].Value;
                    if (!Path.IsPathRooted(projectPath))
                        projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));

                    projectPaths.Add(projectPath);
                }
            }
            catch
            {
                return Array.Empty<string>();
            }

            return projectPaths;
        }

        private static bool ProjectFilePrefersFullFrameworkMsBuild(string projectPath)
        {
            if (!File.Exists(projectPath))
                return false;

            string projectXml = File.ReadAllText(projectPath);
            return projectXml.IndexOf("<COMReference", StringComparison.OrdinalIgnoreCase) >= 0 ||
                projectXml.IndexOf("<COMFileReference", StringComparison.OrdinalIgnoreCase) >= 0 ||
                projectXml.IndexOf("<TargetFrameworkVersion>v4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                Regex.IsMatch(projectXml, @"<TargetFrameworks?>\s*net4", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool RequiresFullFrameworkMsBuild(string buildOutput)
        {
            if (string.IsNullOrEmpty(buildOutput))
                return false;

            return buildOutput.IndexOf("MSB4803", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (buildOutput.IndexOf("ResolveComReference", StringComparison.OrdinalIgnoreCase) >= 0 &&
                buildOutput.IndexOf(".NET Core version of MSBuild", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool TryFindFullFrameworkMsBuild(out string msBuildPath)
        {
            msBuildPath = FindMsBuildWithVsWhere();
            if (!string.IsNullOrEmpty(msBuildPath))
                return true;

            foreach (string candidate in GetKnownMsBuildPaths())
            {
                if (File.Exists(candidate))
                {
                    msBuildPath = candidate;
                    return true;
                }
            }

            msBuildPath = string.Empty;
            return false;
        }

        private static string FindMsBuildWithVsWhere()
        {
            string vsWherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio", "Installer", "vswhere.exe");

            if (!File.Exists(vsWherePath))
                return string.Empty;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(vsWherePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Arguments = "-latest -products * -requires Microsoft.Component.MSBuild -find \"MSBuild\\**\\Bin\\MSBuild.exe\""
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        return string.Empty;

                    if (!process.WaitForExit(5000))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    return output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(path => path.Trim())
                        .FirstOrDefault(File.Exists) ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IEnumerable<string> GetKnownMsBuildPaths()
        {
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            string[] versions = { "2022", "2019", "2017" };
            string[] editions = { "BuildTools", "Community", "Professional", "Enterprise" };

            foreach (string root in roots.Where(root => !string.IsNullOrEmpty(root)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (string version in versions)
                {
                    foreach (string edition in editions)
                    {
                        string msBuildRoot = Path.Combine(root, "Microsoft Visual Studio", version, edition, "MSBuild");
                        yield return Path.Combine(msBuildRoot, "Current", "Bin", "MSBuild.exe");
                        yield return Path.Combine(msBuildRoot, "Current", "Bin", "amd64", "MSBuild.exe");
                    }
                }
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                yield return Path.Combine(programFilesX86, "MSBuild", "14.0", "Bin", "MSBuild.exe");
                yield return Path.Combine(programFilesX86, "MSBuild", "12.0", "Bin", "MSBuild.exe");
            }
        }

        private static string FormatBuildOutput(ProcessRunResult build)
        {
            string output = build.Output?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(output))
                return output;

            return build.Success
                ? "Build completed with no output."
                : $"Build failed with exit code {build.ExitCode}.";
        }

        private static bool BuildHasErrors(string buildOutput)
        {
            if (string.IsNullOrEmpty(buildOutput))
                return false;

            return buildOutput.IndexOf(" error ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                buildOutput.IndexOf(": error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                buildOutput.IndexOf("Build FAILED.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                buildOutput.StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get binary reference list.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<MetadataReference> References(bool isCompiled)
        {
            lock (_compileRefLock)
            {
                if (_platformRefsCompile.IsDefault)
                {
                    _platformRefsCompile = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                        .Split(Path.PathSeparator)
                        .Select(r => (MetadataReference)MetadataReference.CreateFromFile(r))
                        .ToImmutableArray();
                }
            }

            var refList = _platformRefsCompile.ToList();
            var customRefList = GlobalVariables.customRefList;
            foreach (var libPath in customRefList)
            {
                var lib = libPath.Split('|')[1];
                if (!isCompiled && !string.IsNullOrEmpty(lib))
                {
                    var existAsm = LibLoaded.CheckLoadedAssembly(lib);
                    if (existAsm) continue;
                }
                using (var stream = File.OpenRead(lib))
                {
                    AssemblyLoadContext.Default.LoadFromStream(stream);
                }
                refList.Add(MetadataReference.CreateFromFile(lib));
            }
            return refList;
        }

        /// <summary>
        /// Set optimizaiton level acording to options data.
        /// </summary>
        /// <returns></returns>
        private static OptimizationLevel OptimizationLevelState() => (GlobalVariables.configParam.Contains("Release")) ? OptimizationLevel.Release : OptimizationLevel.Debug;

        /// <summary>
        /// Set C# language version.
        /// </summary>
        /// <param name="framework"></param>
        /// <returns></returns>
        private static CSharpParseOptions SetLanguageVersion(string framework)
        {
            var languageVersion = LanguageVersion.Default;
            switch (framework)
            {
                case "net6.0-windows":
                    languageVersion = LanguageVersion.CSharp10;
                    break;
                case "net7.0-windows":
                    languageVersion = LanguageVersion.CSharp11;
                    break;
                case "net8.0-windows":
                    languageVersion = LanguageVersion.CSharp12;
                    break;
            }
            return CSharpParseOptions.Default.WithLanguageVersion(languageVersion);

        }
    }
}
