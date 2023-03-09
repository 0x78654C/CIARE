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

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("windows")]
    public class RoslynRun
    {
        /* Class for compile and run C# code using Roslyn */
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static string[] s_commandLineArguments = null;

        /// <summary>
        /// Compile and run C# using Roslyn.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="param"></param>
        /// <param name="richTextBox"></param>
        public static void CompileAndRun(string code, RichTextBox richTextBox)
        {
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
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                string assemblyName = Path.GetRandomFileName();
                string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: References(false),
                    options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

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
                            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                            ErrorDisplay(richTextBox, diagnostic.Id, diagnostic.GetMessage(), line);
                        }
                    }
                    else
                    {
                        richTextBox.Clear();
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }
                    ms.Close();
                }
                MethodInfo myMethod = assembly.EntryPoint;
                myMethod.Invoke(null, new object[] { s_commandLineArguments });
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                if (richTextBox.Text.EndsWith("\n"))
                    richTextBox.Text += $"---------------------------------\nCompile and code execution time: {s_timeSpan.Milliseconds} milliseconds";
                else
                    richTextBox.Text += $"\n---------------------------------\nCompile and code execution time: {s_timeSpan.Milliseconds} milliseconds";
            }
            catch (DivideByZeroException dbze)
            {
                richTextBox.Text += dbze.StackTrace;
            }
            catch (Exception st)
            {
                if (!richTextBox.Text.StartsWith("ERROR"))
                {
                    if (richTextBox.Text.EndsWith("\n"))
                        richTextBox.Text += $"---------------Stack Trace------------------\n";
                    else
                        richTextBox.Text += $"\n--------------Stack Trace-------------------\n";
                    richTextBox.Text += st.Message;
                }
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
        public static void BinaryCompile(string code, bool exeFile, string outPut, RichTextBox richTextBox)
        {
            string pathOutput = Application.StartupPath + "binary\\";
            string roslynDir = Application.StartupPath + "roslyn\\";
            try
            {
                if (!Directory.Exists(roslynDir))
                {
                    richTextBox.Text = $"ERROR: Directory does not exist -> {roslynDir}";
                    return;
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
                if (exeFile)
                    richTextBox.Text = "Compile EXE binary file ...";
                else
                    richTextBox.Text = "Compile DLL binary file ...";
                string Output = pathOutput + outPut;
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();
                s_stopWatch.Start();
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                string assemblyName = Path.GetRandomFileName();
                CSharpCompilation compilation;
                if (exeFile)
                {
                    compilation = CSharpCompilation.Create(
                      assemblyName,
                      syntaxTrees: new[] { syntaxTree },
                      references: References(true),
                      options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, true, null, null,
                      null, null, OptimizationLevel.Debug, false, false, null, null,
                      ImmutableArray.Create<byte>(new byte[] { }), false, Platform.AnyCpu));;
                }
                else
                {
                    compilation = CSharpCompilation.Create(
                     assemblyName,
                     syntaxTrees: new[] { syntaxTree },
                     references: References(true),
                     options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, true, null, null,
                     null, null, OptimizationLevel.Debug, false, false, null, null,
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
                            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                            ErrorDisplay(richTextBox, diagnostic.Id, diagnostic.GetMessage(), line);
                        }
                    }
                    else
                    {
                        richTextBox.Clear();
                        CsProjCompile projCompile = new CsProjCompile(outPut, pathOutput, code, !exeFile);
                        projCompile.Build(richTextBox);
                        s_stopWatch.Stop();
                        s_timeSpan = s_stopWatch.Elapsed;
                        if (!GlobalVariables.compileTime)
                            richTextBox.Text += $"\n---------------------------------\nCompile execution time: {s_timeSpan.Milliseconds} milliseconds";
                        GlobalVariables.compileTime = false;
                    }
                    s_stopWatch.Stop();
                    ms.Close();
                }
                GlobalVariables.binaryName = string.Empty;
            }
            catch (DivideByZeroException dbze)
            {
                richTextBox.Text += dbze.StackTrace;
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
        }


        /// <summary>
        /// Compile and run C# code and controlers handle.
        /// </summary>
        public static void RunCode(RichTextBox outLogRtb, PictureBox runCodePb, TextEditorControl textEditor, SplitContainer splitContainer, bool runner)
        {
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            if (GlobalVariables.darkColor)
                outLogRtb.ForeColor = Color.FromArgb(192, 215, 207);
            else
                outLogRtb.ForeColor = Color.Black;

            outLogRtb.Clear();
            outLogRtb.Text = "Compile and Runing..\n";
            runCodePb.Image = Properties.Resources.runButton_gray;
            runCodePb.Enabled = false;
            CompileAndRun(textEditor.Text, outLogRtb);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            runCodePb.Image = Properties.Resources.runButton21;
            runCodePb.Enabled = true;
            GC.Collect();
        }

        /// <summary>
        /// Compile code to EXE binary file method.
        /// </summary>
        public static void CompileBinaryExe(TextEditorControl textEditor, SplitContainer splitContainer, RichTextBox outLogRtb, bool runner)
        {
            GlobalVariables.exeName = true;
            BinaryName binaryName = new BinaryName();
            if (!GlobalVariables.checkFormOpen)
                binaryName.ShowDialog();
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            BinaryCompile(textEditor.Text, true, GlobalVariables.binaryName, outLogRtb);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            GC.Collect();
        }

        /// <summary>
        /// Compile code to DLL binary file method.
        /// </summary>
        public static void CompileBinaryDll(TextEditorControl textEditor, SplitContainer splitContainer, RichTextBox outLogRtb, bool runner)
        {
            GlobalVariables.exeName = false;
            BinaryName binaryName = new BinaryName();
            if (!GlobalVariables.checkFormOpen)
                binaryName.ShowDialog();
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            BinaryCompile(textEditor.Text, false, GlobalVariables.binaryName, outLogRtb);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            GC.Collect();
        }

        /// <summary>
        /// Get binary reference list.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<MetadataReference> References(bool isCompiled)
        {
            var refList = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(refs => MetadataReference.CreateFromFile(refs)).Cast<MetadataReference>().ToList();
            var customRefList = GlobalVariables.customRefAsm;
            foreach (var libPath in customRefList)
            {
                if (!isCompiled && !string.IsNullOrEmpty(libPath))
                {
                    var existAsm = LibLoaded.CheckLoadedAssembly(libPath);
                    if (existAsm) continue;
                }
                var stream = File.OpenRead(libPath);
                AssemblyLoadContext.Default.LoadFromStream(stream);
                var r = MetadataReference.CreateFromFile(libPath);
                refList.Add(r);
            }
            return refList;
        }
    }
}
