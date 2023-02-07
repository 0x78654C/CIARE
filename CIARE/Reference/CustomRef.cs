using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using System.Runtime.Versioning;
using System.Drawing;

namespace CIARE.Reference
{
    [SupportedOSPlatform("Windows")]
    public class CustomRef
    {
        private static string s_libPath = string.Empty;
        private static bool IsManaged(string libName)
        {
            try
            {
                var b = AssemblyName.GetAssemblyName(libName);
                return true;
            }
            catch
            {
                // Ignore
            }
            return false;
        }

        /// <summary>
        /// Loads custom managed library and sets namespace as directive in editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="logOutput"></param>
        public static void SetCustomRefDirective(TextEditorControl textEditorControl, RichTextBox logOutput)
        {
            try
            {
                if (string.IsNullOrEmpty(textEditorControl.Text))
                    return;
                var textData = textEditorControl.Text;
                var reader = new StringReader(textData);
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("using [\"") && line.EndsWith("\"]"))
                    {
                        var libPath = line.Replace("using [\"", string.Empty).Trim();
                        libPath = libPath.Replace("\"]", string.Empty).Trim();

                        if (!File.Exists(libPath))
                            logOutput.Text += $"Reference library not found: {libPath}\n";

                        if (IsManaged(libPath))
                        {
                            var asmName = GetAssemblyNamespace(libPath);
                            var refList = GlobalVariables.customRefAsm;
                            if (!refList.Contains(libPath))
                            {
                                refList.Add(libPath);
                                s_libPath = libPath;
                                Task.Run(()=> MainForm.pcRegistry.LoadCustomAssembly(libPath));
                            }
                            textData = textData.Replace(line, $"using {asmName};");
                        }
                    }
                }
                MainForm.Instance.ReloadRef();
                textEditorControl.Text = textData;
            }
            catch (Exception ex)// Exception is for tests at this point
            {
                logOutput.ForeColor = Color.Red;
                logOutput.Text = $"Error: {ex.Message}";
            }
        }

        public static void LoadCustomAssembly(string libPath, RichTextBox logOutput)
        {
            if (!File.Exists(libPath))
                logOutput.Text = $"Reference library not found: {libPath}";
            if (!IsManaged(libPath))
                return;
            try
            {
                var asmName = GetAssemblyNamespace(libPath);

                var refList = GlobalVariables.customRefAsm;
                if (!refList.Contains(libPath))
                    refList.Add(libPath);
                MessageBox.Show("Reference added o list!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Information);
            }
            catch (Exception ex)// Exception is for tests at this point
            {
                MessageBox.Show($"Error: {ex.Message}", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Get reference assembly namesapce.
        /// </summary>
        /// <param name="libPath"></param>
        /// <returns></returns>
        public static string GetAssemblyNamespace(string libPath) => Assembly.LoadFile(libPath).GetTypes().Select(t => t.Namespace).Last();
    }
}
