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
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Diagnostics;

namespace CIARE.Roslyn
{
    public class RoslynRun
    {
        /* Class for compile and run C# code using Roslyn */
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        /// <summary>
        /// Compile and run C# using Roslyn.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="param"></param>
        /// <param name="richTextBox"></param>
        public static void CompileAndRun(string code, string param, RichTextBox richTextBox)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    richTextBox.ForeColor = Color.Red;
                    richTextBox.Text = "ERROR: There is no code in the editor to run!";
                    return;
                }
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();
                s_stopWatch.Start();
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                string assemblyName = Path.GetRandomFileName();
                string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
                var references = Directory.GetFiles(assemblyPath).Where(t => t.EndsWith(".dll"))
        .Where(t => ManageCheck.IsManaged(t))
        .Select(t => MetadataReference.CreateFromFile(t)).ToArray();

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
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
                            richTextBox.Text += $"Error: {diagnostic.Id} -> {diagnostic.GetMessage()}" + Environment.NewLine;
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
                myMethod.Invoke(null, new object[] { new string[0] });
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                richTextBox.Text += $"---------------------------------\nCompile and code execution time: {s_timeSpan.Milliseconds} milliseconds";
            }
            catch
            {
                //TODO: maybe log it?
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
        [Obsolete]
        public static void BinaryCompile(string code, bool exeFile, string outPut, RichTextBox richTextBox)
        {
            string pathOutput = Application.StartupPath + "\\binary\\";
            richTextBox.Clear();
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
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();
            string Output = pathOutput + outPut;
            richTextBox.Clear();
            CompilerParameters parameters = new CompilerParameters();
            //Make sure we generate an EXE, not a DLL
            parameters.GenerateExecutable = exeFile;
            parameters.OutputAssembly = Output;
            parameters.ReferencedAssemblies.Clear();
            parameters.ReferencedAssemblies.AddRange(Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(a => a.Name + ".dll").ToArray());
            CompilerResults results = icc.CompileAssemblyFromSource(parameters, code);
            if (results.Errors.Count > 0)
            {
                richTextBox.ForeColor = Color.Red;
                foreach (CompilerError CompErr in results.Errors)
                {
                    richTextBox.Text =
                                "ERROR: Line number " + CompErr.Line +
                                ", ID: " + CompErr.ErrorNumber +
                                " -> " + CompErr.ErrorText;
                }
            }
            else
            {
                //Successful Compile
                richTextBox.Text = $"Success!\nBinary saved in: {pathOutput + outPut}";
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                richTextBox.Text += $"\n\n---------------------------------\nCompile execution time: {s_timeSpan.Milliseconds} milliseconds";
            }
            Utils.GlobalVariables.binaryName = string.Empty;
        }
    }
}
