using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CIARE.Roslyn;
using CIARE.Utils;
using CIARE.Utils.NuGetManage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Dom = ICSharpCode.SharpDevelop.Dom;

namespace CIARE
{
    public partial class MainForm
    {
        private readonly Dictionary<string, Dom.IProjectContent> _projectPackageCompletionContents
            = new Dictionary<string, Dom.IProjectContent>(StringComparer.OrdinalIgnoreCase);

        private bool ShouldUseProjectPackageCompletionReferences(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return false;

            string activeProjectPath = GetCompletionProjectPath(GetActiveEditorFilePath());
            return !string.IsNullOrEmpty(activeProjectPath) &&
                string.Equals(NormalizeCompletionPath(activeProjectPath),
                    NormalizeCompletionPath(projectPath), StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshProjectPackageCompletionReferences(string projectPath)
        {
            if (!GlobalVariables.OCodeCompletion || pcRegistry == null || myProjectContent == null)
                return;

            var referencePaths = GetCompletionCompileReferencePaths(projectPath);
            var activeReferences = new HashSet<string>(referencePaths, StringComparer.OrdinalIgnoreCase);

            lock (_completionDataLock)
            {
                var removedReferences = _projectPackageCompletionContents.Keys
                    .Where(path => !activeReferences.Contains(path))
                    .ToList();

                foreach (var referencePath in removedReferences)
                {
                    Dom.IProjectContent projectContent = _projectPackageCompletionContents[referencePath];
                    myProjectContent.ReferencedContents.Remove(projectContent);
                    projectContent.Dispose();
                    _projectPackageCompletionContents.Remove(referencePath);
                }

                foreach (var referencePath in referencePaths)
                {
                    if (_projectPackageCompletionContents.ContainsKey(referencePath))
                        continue;

                    Dom.IProjectContent projectContent = LoadProjectPackageCompletionContent(referencePath);
                    if (projectContent == null)
                        continue;

                    _projectPackageCompletionContents[referencePath] = projectContent;
                    myProjectContent.AddReferencedContent(projectContent);
                }
            }
        }

        private void ClearProjectPackageCompletionReferences()
        {
            if (myProjectContent == null)
                return;

            lock (_completionDataLock)
            {
                foreach (var projectContent in _projectPackageCompletionContents.Values)
                {
                    myProjectContent.ReferencedContents.Remove(projectContent);
                    projectContent.Dispose();
                }

                _projectPackageCompletionContents.Clear();
            }
        }

        private Dom.IProjectContent LoadProjectPackageCompletionContent(string referencePath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFile(referencePath);
                var projectContent = new Dom.ReflectionProjectContent(assembly, pcRegistry);
                projectContent.InitializeReferences();
                return projectContent;
            }
            catch
            {
                return null;
            }
        }

        private static List<string> GetCompletionFrameworkReferencePaths(string projectPath)
        {
            return GetCompletionProjectPaths(projectPath)
                .SelectMany(path => ProjectNuGetManager.GetFrameworkReferencePaths(path))
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetCompletionCompileReferencePaths(string projectPath)
        {
            var paths = new List<string>();
            foreach (string path in GetCompletionProjectPaths(projectPath))
            {
                paths.AddRange(ProjectNuGetManager.GetAssemblyReferencePaths(path));
                paths.AddRange(ProjectNuGetManager.GetCompileReferencePaths(path));
            }

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<MetadataReference> BuildCompletionReferences(string projectPath)
        {
            var references = new List<MetadataReference>();
            var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var referenceAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencePath in GetCompletionFrameworkReferencePaths(projectPath))
                AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);

            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.customRefList);
            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.filteredCustomRef);
            AddCompletionReferencePaths(references, referencePaths, referenceAssemblyNames,
                GlobalVariables.customRefAsm);

            foreach (var referencePath in GetCompletionCompileReferencePaths(projectPath))
                AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);

            string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrEmpty(trusted))
            {
                foreach (var referencePath in trusted.Split(Path.PathSeparator))
                    AddCompletionReference(references, referencePaths, referenceAssemblyNames, referencePath);
            }

            return references;
        }

        private static void AddCompletionReferencePaths(List<MetadataReference> references,
            HashSet<string> referencePaths, HashSet<string> referenceAssemblyNames,
            IEnumerable<string> storedReferences)
        {
            if (storedReferences == null)
                return;

            foreach (string storedReference in storedReferences)
                AddCompletionReference(references, referencePaths, referenceAssemblyNames,
                    GetCompletionReferencePath(storedReference));
        }

        private static void AddCompletionReference(List<MetadataReference> references,
            HashSet<string> referencePaths, HashSet<string> referenceAssemblyNames, string referencePath)
        {
            if (string.IsNullOrWhiteSpace(referencePath) || !File.Exists(referencePath))
                return;

            try
            {
                string normalizedPath = NormalizeCompletionPath(referencePath);
                if (!referencePaths.Add(normalizedPath))
                    return;

                string assemblyName = GetCompletionReferenceAssemblyName(referencePath);
                if (!string.IsNullOrEmpty(assemblyName) &&
                    !referenceAssemblyNames.Add(assemblyName))
                {
                    referencePaths.Remove(normalizedPath);
                    return;
                }

                references.Add(SharedMetadataReferences.Get(referencePath));
            }
            catch
            {
            }
        }

        private static string GetCompletionReferenceAssemblyName(string referencePath)
        {
            try
            {
                return AssemblyName.GetAssemblyName(referencePath).Name ?? string.Empty;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(referencePath);
            }
        }

        private static string GetCompletionReferencePath(string storedReference)
        {
            if (string.IsNullOrWhiteSpace(storedReference))
                return string.Empty;

            string[] parts = storedReference.Split('|');
            return parts.Length >= 2 ? parts[1] : storedReference;
        }
    }
}
