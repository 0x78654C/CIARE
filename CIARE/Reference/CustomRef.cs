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
        public static string GetAssemblyNamespace(string libPath)
        {
            var listNamespaces = Assembly.LoadFile(libPath).GetTypes().Select(t => t.Namespace);

            var counts = new Dictionary<string, int>();
            listNamespaces = listNamespaces.OrderBy(i => i);
            foreach (var ns in listNamespaces)
            {
                var foundRoot = false;
                if (ns is "Microsoft.CodeAnalysis" or "System.Runtime.CompilerServices" or null) continue;
                foreach (var (key, count) in counts)
                {
                    if (ns.StartsWith(key))
                    {
                        counts[key] = count + 1;
                        foundRoot = true;
                        break;
                    }
                }

                if (!foundRoot)
                {
                    counts.Add(ns, 0);
                }
            }

            var rootNamespace = counts.OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault();

            return rootNamespace;
        }

        /// <summary>
        /// Populate the listview with reference lib path and namespace.
        /// </summary>
        /// <param name="libPath"></param>
        /// <param name="refList"></param>
        public static void PopulateList(List<string> libPath, ref ListView refList)
        {
            try
            {
                if (libPath == null)
                    return;
                foreach (var lib in libPath)
                {
                    // check here

                    string assemblyNamespace = GetAssemblyNamespace(lib);
                    var foundItem = refList.FindItemWithText(assemblyNamespace);
                    if (foundItem != null)
                        return;
                    ListViewItem item = new ListViewItem(new[] { assemblyNamespace, lib });
                    if (string.IsNullOrEmpty(assemblyNamespace))
                        return;
                    var foundItemIn = refList.FindItemWithText(assemblyNamespace);
                    if (foundItemIn == null)
                        refList.Items.Add(item);
                }
            }
            catch
            {
                // Ignore.
            }
        }
    }
}
