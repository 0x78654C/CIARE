using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Drawing;
using System.Collections.Generic;

namespace CIARE.Reference
{
    [SupportedOSPlatform("Windows")]
    public class CustomRef
    {
        /// <summary>
        /// Check if  a library is managed.
        /// </summary>
        /// <param name="libName"></param>
        /// <returns></returns>
        public static bool IsManaged(string libName)
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
        public static void SetCustomRefDirective(List<string> refList, RichTextBox logOutput)
        {
            try
            {
                foreach (var libPath in refList)
                {
                  Task.Run(() => MainForm.pcRegistry.LoadCustomAssembly(libPath));
                }
                MainForm.Instance.ReloadRef();
            }
            catch (Exception ex)
            {
                logOutput.ForeColor = Color.Red;
                logOutput.Text = $"Error: {ex.Message}";
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
