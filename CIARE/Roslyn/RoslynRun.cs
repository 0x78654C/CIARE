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

        private static string _namesapce; 
        private static string _className;
        public static void CompileAndRun(string code, string param, RichTextBox richTextBox)
        {
            try
            {
                ParseCode(code);
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
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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
                Type type = assembly.GetType($"{_namesapce}.{_className}");
                object obj = Activator.CreateInstance(type);
                type.InvokeMember("Main",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                    null,
                    obj,
                    new object[] { param });
                _className = "";
                _namesapce = "";
                obj = null;
                references = null;
                compilation = null;
                assembly = null;
                type = null;
            }
            catch 
            {
                //TODO: null the internal errors or will in future add to log system.
            }
        }

        private static void ParseCode(string code)
        {
            try
            {
                string line;
                using (var reader = new StringReader(code))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.ContainsText("namespace"))
                        {
                            _namesapce = line.Split(' ').ParameterAfter("namespace");
                        }

                        if (line.ContainsText("class"))
                        {
                            _className = line.Split(' ').ParameterAfter("class");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
