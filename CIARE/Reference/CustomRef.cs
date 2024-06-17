using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.IO;
using CIARE.Roslyn;
using CIARE.Utils;
using System.Threading;
using System;

namespace CIARE.Reference
{
    [SupportedOSPlatform("Windows")]
    public class CustomRef
    {
        private static bool s_isInList = false;

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
        public static void SetCustomRefDirective(List<string> refList, bool isNuget = false)
        {
            try
            {
                foreach (var libPath in refList)
                {
                    if (s_isInList) continue;

                    //var checkASM = LibLoaded.CheckLoadedAssembly(libPath);

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
        public static void PopulateList(List<string> libPath, string pathNugetFile, bool isFormLoading = false)
        {
            try
            {
                ListView lstRef = new ListView();
                if (!isFormLoading)
                {
                    foreach (var lib in libPath)
                    {
                        string assemblyNamespace = GetAssemblyNamespace(lib);

                        ListViewItem item = new ListViewItem(new[] { assemblyNamespace, lib });
                        if (string.IsNullOrEmpty(assemblyNamespace))
                            continue;
                        FileInfo fileInfo = new FileInfo(lib);
                        var libFile = $"{assemblyNamespace}|{lib}";


                        if (!File.Exists(pathNugetFile))
                            File.WriteAllText(pathNugetFile, "");

                        if (!CheckItem(lstRef, fileInfo.Name))
                        {
                            lstRef.Items.Add(item);
                            if (!GlobalVariables.customRefList.Contains(libFile))
                            {
                                GlobalVariables.customRefList.Add(libFile);
                                var libsFile = File.ReadAllText(pathNugetFile);
                                if (!libsFile.Contains(lib) && !GlobalVariables.customRefList.Contains(lib))
                                    File.AppendAllText(pathNugetFile, $"{lib}\n");
                            }
                        }
                        else
                            s_isInList = true;
                    }
                }
                else
                {
                    foreach (var dItem in GlobalVariables.customRefList)
                    {
                        var asmName = dItem.Split('|')[0];
                        var lib = dItem.Split('|')[1];
                        ListViewItem item = new ListViewItem(new[] { asmName, lib });
                        lstRef.Items.Add(item);
                    }
                }
            }
            catch
            {
                // Ignore 
            }
        }
        /// <summary>
        /// Populate the listview with downloaded nuget packages.
        /// </summary>
        /// <param name="libPath"></param>
        /// <param name="refList"></param>
        public static void PopulateListNuget(List<string> nugetList, ListView refList)
        {
            foreach (var nuget in nugetList)
            {
                var name = nuget.Split('|')[0];
                var version = nuget.Split('|')[1];
                ListViewItem item = new ListViewItem(new[] { name, version });
                if (!CheckItem(refList, name, true))
                    refList.Items.Add(item);
            }
        }

        /// <summary>
        /// Populate the listview with local libs.
        /// </summary>
        /// <param name="nugetList"></param>
        /// <param name="refList"></param>
        public static void PopulateListLocal(List<string> nugetList, ListView refList)
        {
            foreach (var lib in nugetList)
            {
                if (string.IsNullOrEmpty(lib))
                    continue;
                string assemblyNamespace = GetAssemblyNamespace(lib);
                ListViewItem item = new ListViewItem(new[] { assemblyNamespace, lib });
                if (!CheckItem(refList, lib) && (IsManaged(lib)))
                    refList.Items.Add(item);
            }
        }

        /// <summary>
        /// Check if listview contins string item.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool CheckItem(ListView listView, string text, bool isNuget = false)
        {
            bool isPresent = false;
            if (!isNuget)
            {
                for (int i = 0; i < listView.Items.Count; i++)
                    if (listView.Items[i].SubItems[1].Text.EndsWith(text))
                        isPresent = true;
            }
            else
            {
                for (int i = 0; i < listView.Items.Count; i++)
                    if (listView.Items[i].SubItems[0].Text.Contains(text))
                        isPresent = true;
            }
            return isPresent;
        }

        /// <summary>
        /// Function for delete zip file from nuget forlder.
        /// </summary>
        /// <param name="pathNugetDir"></param>
        public static void DelDownloadedPackage(string pathNugetDir)
        {
            Thread.Sleep(3000);
            try
            {
                if (!Directory.Exists(pathNugetDir)) return;

                var files = Directory.GetFiles(pathNugetDir);
                foreach (var file in files)
                {
                    if (file.EndsWith(".zip"))
                        File.Delete(file);
                }
            }
            catch { }
        }

        /// <summary>
        /// Function for delete zip file from nuget forlder.
        /// </summary>
        /// <param name="pathNugetDir"></param>
        public static void DeleteNuGetLibs(string pathNugetDir, string libFile)
        {
            Thread.Sleep(3000);
            try
            {
                if (!Directory.Exists(pathNugetDir)) return;

                var files = Directory.GetFiles(pathNugetDir);
                foreach (var file in files)
                {
                    if (file.EndsWith(libFile))
                        File.Delete(file);
                }
            }
            catch { }
        }
    }
}
