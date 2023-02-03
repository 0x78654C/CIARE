using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using System.Diagnostics.Eventing.Reader;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace CIARE.Reference
{
    [SupportedOSPlatform("Windows")]
    public class CustomRef
    {
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
                            var asmName = Assembly.LoadFile(libPath).GetTypes().Select(t => t.Namespace).Last();
                            var refList = GlobalVariables.customRefAsm;
                            if (!refList.Contains(libPath))
                            {
                                refList.Add(libPath);
                                MainForm.pcRegistry.LoadCustomAssembly(libPath);
                            }
                            textData = textData.Replace(line, $"using {asmName}; // Custom ref: {libPath}");
                        }
                    }
                }
                textEditorControl.Text = textData;
            }
            catch (Exception ex)// Exception is for tests at this point
            {
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
                var asmName = Assembly.LoadFile(libPath).GetTypes().Select(t => t.Namespace).Last();

                var refList = GlobalVariables.customRefAsm;
                if (!refList.Contains(libPath))
                    refList.Add(libPath);
            }
            catch (Exception ex)// Exception is for tests at this point
            {
                logOutput.Text = ex.ToString();
            }
        }

    }
}
