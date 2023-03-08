using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.IO;
using System;
using CIARE.Roslyn;

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
        public static void SetCustomRefDirective(List<string> refList)
        {
            try
            {
                foreach (var libPath in refList)
                {
                    var checkASM = LibLoaded.CheckLoadedAssembly(libPath);

                    if (checkASM) continue;

                    if (IsManaged(libPath))
                        Task.Run(() => MainForm.pcRegistry.LoadCustomAssembly(libPath));
                }
                MainForm.Instance.ReloadRef();
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Get reference assembly namesapce.
        /// </summary>
        /// <param name="libPath"></param>
        /// <returns></returns>
        public static string GetAssemblyNamespace(string libPath)
        {
            IEnumerable<string> listNamespaces = null;
            try
            {
                listNamespaces = Assembly.LoadFile(libPath).GetTypes().Select(t => t.Namespace);
            }
            catch
            {
                // Ignore
            }
            if (listNamespaces == null)
            {
                var fileInfo = new FileInfo(libPath);
                var fileName = fileInfo.Name[..^4];
                listNamespaces = new string[] { fileName };
            }
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
        public static void PopulateList(List<string> libPath, ListView refList)
        {
            try
            {
                foreach (var lib in libPath)
                {
                    string assemblyNamespace = GetAssemblyNamespace(lib);
                    ListViewItem item = new ListViewItem(new[] { assemblyNamespace, lib });
                    if (string.IsNullOrEmpty(assemblyNamespace))
                        continue;
                    if (!CheckItem(refList,assemblyNamespace) && (IsManaged(lib)))
                        refList.Items.Add(item);
                }
            }
            catch
            {
                // Ignore
            }
        }

        /// <summary>
        /// Check if listview contins string item.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool CheckItem(ListView listView, string text)
        {
            for (int i = 0; i < listView.Items.Count; i++)
                if (listView.Items[i].SubItems[0].Text == text)
                   return true;
            return false;
        }
    }
}
