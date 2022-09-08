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
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;
using ICSharpCode.NRefactory.Ast;

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
                    richTextBox.ForeColor = Color.Red;
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
                //        var references = Directory.GetFiles(assemblyPath).Where(t => t.EndsWith(".dll"))
                //.Where(t => ManageCheck.IsManaged(t))
                //.Select(t => MetadataReference.CreateFromFile(t)).ToArray();

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: References(),
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
            catch { }
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
                    richTextBox.ForeColor = Color.Red;
                    richTextBox.Text = $"ERROR: Directory does not exist -> {roslynDir}";
                    return;
                }
                if (string.IsNullOrEmpty(code))
                {
                    richTextBox.ForeColor = Color.Red;
                    richTextBox.Text = "ERROR: There is no code in the editor to compile!";
                    return;
                }
                if (string.IsNullOrEmpty(Utils.GlobalVariables.binaryName))
                    return;

                if (string.IsNullOrEmpty(outPut))
                    return;

                if (GlobalVariables.darkColor)
                    richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
                else
                    richTextBox.ForeColor = Color.Black;
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();
                s_stopWatch.Start();
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
                //Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                //string assemblyName = Path.GetRandomFileName();
                //string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
                //var references = Directory.GetFiles(assemblyPath).Where(t => t.EndsWith(".dll"))
                //.Where(t => ManageCheck.IsManaged(t))
                //.Select(t => MetadataReference.CreateFromFile(t)).ToArray();

                CSharpCompilation compilation;
                if (exeFile)
                {
                    compilation = CSharpCompilation.Create(
                      outPut,
                      syntaxTrees: new[] { syntaxTree },
                      references: References(),
                      options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, true, null, null,
                      null, null, OptimizationLevel.Release, false, false, null, null,
                      ImmutableArray.Create<byte>(new byte[] { }), false, Platform.X64));
                    string noExe = Output.Substring(0, Output.Length - 3);
                    File.WriteAllText($"{noExe}runtimeconfig.json", GenerateRuntimeConfig());
                }
                else
                {
                    compilation = CSharpCompilation.Create(
                    outPut,
                    syntaxTrees: new[] { syntaxTree },
                    references: References(),
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, true, null, null, null, null, OptimizationLevel.Release,
                    false, false, null, null,
                      ImmutableArray.Create<byte>(new byte[] { }), false, Platform.AnyCpu));
                }

                EmitResult result = compilation.Emit(pathOutput + outPut);
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
                    //Successful Compile
                    richTextBox.Text = $"Success!\nBinary saved in: {pathOutput + outPut}";
                    s_stopWatch.Stop();
                    s_timeSpan = s_stopWatch.Elapsed;
                    richTextBox.Text += $"\n\n---------------------------------\nCompile execution time: {s_timeSpan.Milliseconds} milliseconds";
                }
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;

                GlobalVariables.binaryName = string.Empty;
            }
            catch (DivideByZeroException dbze)
            {

                richTextBox.Text += dbze.StackTrace;
                GlobalVariables.binaryName = string.Empty;
            }
            catch { GlobalVariables.binaryName = string.Empty; }
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
            GUI.OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            BinaryCompile(textEditor.Text, true, GlobalVariables.binaryName, outLogRtb);
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
            GC.Collect();
        }

        /// <summary>
        /// Get binary reference list.
        /// </summary>
        /// <returns></returns>
        private static List<MetadataReference> References()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (var refs in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
                references.Add(MetadataReference.CreateFromFile(refs));
            return references;
        }

        /// <summary>
        /// Generate {exeName}.rutimeOptions.json file.
        /// </summary>
        /// <returns></returns>
        private static string GenerateRuntimeConfig()
        {
            string net6runtimeJson = $@"{{
  ""runtimeOptions"": {{
    ""tfm"": ""net6.0"",
    ""frameworks"": [
      {{
        ""name"": ""Microsoft.NETCore.App"",
        ""version"": ""6.0.0""
      }},
      {{
        ""name"": ""Microsoft.WindowsDesktop.App"",
        ""version"": ""6.0.0""
      }}
    ]
  }}
}}
";
            return net6runtimeJson;
        }
    }
}
