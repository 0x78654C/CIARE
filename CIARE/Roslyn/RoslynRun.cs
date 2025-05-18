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

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("windows")]
    public class RoslynRun
    {
        /* Class for compile and run C# code using Roslyn */
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static string[] s_commandLineArguments = null;
        private static AssemblyLoadContext s_assemblyLoad;
        private static string s_errorCode = "";
        private static string s_errorMessage = "";
        private static string s_codeAI = "";
        private static string s_line = "";
        /// <summary>
        /// Compile and run C# using Roslyn.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="param"></param>
        /// <param name="richTextBox"></param>
        public static void CompileAndRun(string code, RichTextBox richTextBox, bool allowUnsafe)
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
                        assembly = Assembly.Load(ms.ToArray());
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
                if (exeFile)
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
                        CsProjCompile projCompile = new CsProjCompile(outPut, pathOutput, code, !exeFile, GlobalVariables.binaryPublish);
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
            itemMenu.BackColor = Color.FromArgb(30, 30, 31);
            itemMenu.ForeColor = Color.IndianRed;
            itemMenu.Font = new Font(new FontFamily(GenericFontFamilies.Monospace), 11.28f, FontStyle.Italic | FontStyle.Bold);
            itemMenu.Click += ItemMenu_Click;
            var itemMenuAI = new ToolStripMenuItem();
            var separator = new ToolStripSeparator();
            separator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDarkAI_Error;
            itemMenuAI.Text = "Ask AI for help you with this error?";
            itemMenuAI.BackColor = Color.FromArgb(30, 30, 31);
            itemMenuAI.ForeColor = Color.White;
            itemMenuAI.Font = new Font(new FontFamily(GenericFontFamilies.Monospace), 11.28f, FontStyle.Italic | FontStyle.Bold);
            itemMenuAI.Click += AskAI_Click;
            contextMenuStrip.Items.Add(itemMenu);
            contextMenuStrip.Items.Add(separator);
            contextMenuStrip.Items.Add(itemMenuAI);
            contextMenuStrip.Show(SelectedEditor.GetSelectedEditor().ActiveTextAreaControl, pos);
        }

        /// <summary>
        /// Open link to error documentation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ItemMenu_Click(object sender, EventArgs e)
        {
            var url = $"https://learn.microsoft.com/en-us/search/?terms={s_errorCode}&category=Documentation";
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
            OutputWindowManage.ShowOutputOnCompileRun(runner, splitContainer, outLogRtb);
            if (GlobalVariables.darkColor)
                outLogRtb.ForeColor = Color.FromArgb(192, 215, 207);
            else
                outLogRtb.ForeColor = Color.Black;

            outLogRtb.Clear();
            outLogRtb.Text = "Compile and Runing..\n";
            runCodePb.Image = Properties.Resources.runButton_gray;
            runCodePb.Enabled = false;
            CompileAndRun(textEditor.Text, outLogRtb, GlobalVariables.OUnsafeCode);
            RtbZoom.RichTextBoxZoom(outLogRtb, GlobalVariables.zoomFactor);
            runCodePb.Image = Properties.Resources.runButton21;
            runCodePb.Enabled = true;
            GC.Collect();
        }

        /// <summary>
        /// Compile code to EXE binary file method.
        /// </summary>
        public static void CompileBinary(TextEditorControl textEditor, SplitContainer splitContainer, RichTextBox outLogRtb, bool runner, OutputKind outputKind = OutputKind.ConsoleApplication)
        {
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

        /// <summary>
        /// Get binary reference list.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<MetadataReference> References(bool isCompiled)
        {
            var refList = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(refs => MetadataReference.CreateFromFile(refs)).Cast<MetadataReference>().ToList();
            var customRefList = GlobalVariables.customRefList;
            s_assemblyLoad = AssemblyLoadContext.Default;
            foreach (var libPath in customRefList)
            {
                var lib = libPath.Split('|')[1];
                if (!isCompiled && !string.IsNullOrEmpty(lib))
                {
                    var existAsm = LibLoaded.CheckLoadedAssembly(lib);
                    if (existAsm) continue;
                }
                var stream = File.OpenRead(lib);
                s_assemblyLoad.LoadFromStream(stream);
                var r = MetadataReference.CreateFromFile(lib);
                refList.Add(r);
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
