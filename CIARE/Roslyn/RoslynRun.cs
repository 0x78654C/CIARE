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

namespace CIARE.Roslyn
{
    public class RoslynRun
    {
        /* Class for compile and run C# code using Roslyn */

        public static void CompileAndRun(string code, string param, RichTextBox richTextBox)
        {
            try
            {
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
                            richTextBox.Text += $"Error: {diagnostic.Id} -> {diagnostic.GetMessage()}"+Environment.NewLine;
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
                myMethod.Invoke(null, new object[] { new string[0]});
            }
            catch
            {
                //TODO: maybe log it?
            }
        }
    }
}
