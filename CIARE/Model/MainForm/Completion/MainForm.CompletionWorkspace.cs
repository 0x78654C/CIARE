using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CIARE.Utils;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Dom = ICSharpCode.SharpDevelop.Dom;
using NRefactory = ICSharpCode.NRefactory;

namespace CIARE
{
    public partial class MainForm
    {
        private const int WorkspaceCompletionMethodLimit = 300;
        private int _completionWorkspaceVersion;
        private string _completionWorkspaceKey = string.Empty;
        private string _completionFileKey = string.Empty;
        private readonly Dictionary<string, Dom.ICompilationUnit> _workspaceCompilationUnits
            = new Dictionary<string, Dom.ICompilationUnit>(StringComparer.OrdinalIgnoreCase);
        private readonly List<WorkspaceCompletionClass> _workspaceCompletionClasses
            = new List<WorkspaceCompletionClass>();
        private readonly List<WorkspaceCompletionItem> _topLevelLocalFunctions
            = new List<WorkspaceCompletionItem>();

        private struct CompletionScopeSnapshot
        {
            public string CurrentFilePath;
            public string WorkspaceFolder;
            public List<string> SourceFolders;
            public List<string> ProjectPaths;
            public string WorkspaceKey;
            public string FileKey;
            public int Version;
        }

        private CompletionScopeSnapshot RefreshCompletionScope(string currentFilePath)
        {
            string projectPath = GetCompletionProjectPath(currentFilePath);
            string workspaceFolder = GetCompletionWorkspaceFolder(currentFilePath);
            var projectPaths = GetCompletionProjectPaths(projectPath);
            var sourceFolders = GetCompletionSourceFolders(projectPaths);
            string workspaceKey = BuildCompletionWorkspaceKey(projectPaths);
            if (string.IsNullOrEmpty(workspaceKey))
                workspaceKey = NormalizeCompletionPath(workspaceFolder);
            string fileKey = NormalizeCompletionPath(currentFilePath);
            bool workspaceChanged = !string.Equals(workspaceKey, _completionWorkspaceKey,
                StringComparison.OrdinalIgnoreCase);
            bool fileChanged = !string.Equals(fileKey, _completionFileKey,
                StringComparison.OrdinalIgnoreCase);

            if (workspaceChanged || fileChanged)
            {
                _completionWorkspaceKey = workspaceKey;
                _completionFileKey = fileKey;
                Interlocked.Increment(ref _completionWorkspaceVersion);

                if (workspaceChanged)
                    ClearWorkspaceCompletionData();
            }

            return new CompletionScopeSnapshot
            {
                CurrentFilePath = currentFilePath,
                WorkspaceFolder = workspaceFolder,
                SourceFolders = sourceFolders,
                ProjectPaths = projectPaths,
                WorkspaceKey = workspaceKey,
                FileKey = fileKey,
                Version = Volatile.Read(ref _completionWorkspaceVersion)
            };
        }

        private bool IsCompletionScopeCurrent(CompletionScopeSnapshot scope)
        {
            return scope.Version == Volatile.Read(ref _completionWorkspaceVersion) &&
                string.Equals(scope.WorkspaceKey, _completionWorkspaceKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(scope.FileKey, _completionFileKey, StringComparison.OrdinalIgnoreCase);
        }

        private void InvalidateCompletionWorkspace()
        {
            _completionWorkspaceKey = string.Empty;
            _completionFileKey = string.Empty;
            Interlocked.Increment(ref _completionWorkspaceVersion);
            ClearWorkspaceCompletionData();
        }

        private void ParseWorkspaceFilesForCompletion(CompletionScopeSnapshot scope, List<WorkspaceCompletionClass> workspaceCompletionClasses)
        {
            List<string> sourceFolders = scope.SourceFolders ?? new List<string>();
            string currentFilePath = scope.CurrentFilePath;
            if (!IsCompletionScopeCurrent(scope))
                return;

            if (sourceFolders.Count == 0)
            {
                ClearWorkspaceCompletionData();
                return;
            }

            string normalizedCurrent = NormalizeCompletionPath(currentFilePath);
            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string sourceFolder in sourceFolders)
            {
                if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
                    continue;

                foreach (var filePath in GetWorkspaceCsFiles(sourceFolder))
                {
                    if (!IsCompletionScopeCurrent(scope))
                        return;

                    if (!string.IsNullOrEmpty(normalizedCurrent) &&
                        string.Equals(NormalizeCompletionPath(filePath), normalizedCurrent, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!visitedPaths.Add(filePath))
                        continue;

                    try
                    {
                        string originalFileCode = File.ReadAllText(filePath);
                        AddRoslynCompletionClasses(workspaceCompletionClasses, originalFileCode, filePath);
                        string fileCode = PrepareCodeForNRefactoryCompletion(originalFileCode, filePath,
                            out _, out _, out _);
                        Dom.ICompilationUnit newCU;
                        using (var reader = new StringReader(fileCode))
                        using (NRefactory.IParser p = NRefactory.ParserFactory.CreateParser(NRefactory.SupportedLanguage.CSharp, reader))
                        {
                            p.ParseMethodBodies = false;
                            p.Parse();
                            newCU = ConvertCompilationUnit(p.CompilationUnit);
                        }
                        if (newCU is Dom.DefaultCompilationUnit dcuW)
                            dcuW.FileName = filePath;
                        lock (_completionDataLock)
                        {
                            if (!IsCompletionScopeCurrent(scope))
                                return;

                            _workspaceCompilationUnits.TryGetValue(filePath, out var oldCU);
                            myProjectContent.UpdateCompilationUnit(oldCU, newCU, filePath);
                            _workspaceCompilationUnits[filePath] = newCU;
                        }
                    }
                    catch { }
                }
            }

            lock (_completionDataLock)
            {
                if (!IsCompletionScopeCurrent(scope))
                    return;

                var toRemove = _workspaceCompilationUnits.Keys.Where(p => !visitedPaths.Contains(p)).ToList();
                foreach (var path in toRemove)
                {
                    myProjectContent.RemoveCompilationUnit(_workspaceCompilationUnits[path]);
                    _workspaceCompilationUnits.Remove(path);
                }
            }
        }

        internal ArrayList GetWorkspaceMethodCompletionData(string prefix)
        {
            var result = new ArrayList();
            if (string.IsNullOrWhiteSpace(prefix))
                return result;

            if (!ShouldUseWorkspaceCompletionData())
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            lock (_completionDataLock)
            {
                AddCompilationUnitMethods(result, seen, parseInformation.MostRecentCompilationUnit, prefix);
                foreach (var unit in _workspaceCompilationUnits.Values)
                {
                    AddCompilationUnitMethods(result, seen, unit, prefix);
                    if (result.Count >= WorkspaceCompletionMethodLimit)
                        break;
                }

                foreach (var func in _topLevelLocalFunctions)
                {
                    if (func.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && seen.Add(func.Name))
                        result.Add(new DefaultCompletionData(func.Name, func.Description, func.ImageIndex));
                }
            }

            return result;
        }

        internal ArrayList GetWorkspaceMemberCompletionData(string expression)
        {
            var result = new ArrayList();
            string normalizedExpression = NormalizeCompletionExpression(expression);
            if (string.IsNullOrEmpty(normalizedExpression))
                return result;

            if (!ShouldUseWorkspaceCompletionData())
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            lock (_completionDataLock)
            {
                foreach (var completionClass in _workspaceCompletionClasses)
                {
                    if (!MatchesWorkspaceClass(completionClass, normalizedExpression))
                        continue;

                    AddWorkspaceClassCompletionItems(result, seen, completionClass);
                    if (result.Count >= WorkspaceCompletionMethodLimit)
                        return result;
                }

                if (result.Count == 0)
                    AddWorkspaceNamespaceCompletionItems(result, seen, normalizedExpression, _workspaceCompletionClasses);
            }

            return result;
        }

        private static List<string> GetCompletionProjectPaths(string projectPath)
        {
            var projectPaths = new List<string>();
            AddCompletionProjectPath(projectPath, projectPaths,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return projectPaths;
        }

        private static void AddCompletionProjectPath(string projectPath, List<string> projectPaths,
            HashSet<string> visited)
        {
            if (!IsProjectFilePath(projectPath))
                return;

            string normalizedProject = NormalizeCompletionPath(projectPath);
            if (string.IsNullOrEmpty(normalizedProject) || !visited.Add(normalizedProject))
                return;

            projectPaths.Add(projectPath);
            foreach (string referencedProjectPath in ProjectReferenceManager.GetProjectReferences(projectPath))
                AddCompletionProjectPath(referencedProjectPath, projectPaths, visited);
        }

        private static List<string> GetCompletionSourceFolders(IList<string> projectPaths)
        {
            return projectPaths
                .Select(GetProjectDirectory)
                .Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetProjectDirectory(string projectPath)
        {
            try
            {
                return string.IsNullOrWhiteSpace(projectPath) ? string.Empty : Path.GetDirectoryName(projectPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildCompletionWorkspaceKey(IList<string> projectPaths)
        {
            if (projectPaths == null || projectPaths.Count == 0)
                return string.Empty;

            return string.Join("|", projectPaths
                .Select(NormalizeCompletionPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        private string GetCompletionWorkspaceFolder(string currentFilePath)
        {
            string projectPath = GetCompletionProjectPath(currentFilePath);
            if (string.IsNullOrEmpty(projectPath))
                return string.Empty;

            string projectFolder = Path.GetDirectoryName(projectPath);
            return !string.IsNullOrEmpty(projectFolder) && Directory.Exists(projectFolder)
                ? projectFolder
                : string.Empty;
        }

        private bool ShouldUseWorkspaceCompletionData()
        {
            string projectPath = GetCompletionProjectPath(GetActiveEditorFilePath());
            return GetCompletionSourceFolders(GetCompletionProjectPaths(projectPath)).Count > 0;
        }

        internal void ResetCompletionWorkspaceIfInactive()
        {
            ResetCompletionWorkspaceIfInactive(GetActiveEditorFilePath());
        }

        internal void ResetCompletionWorkspaceIfInactive(string currentFilePath)
        {
            CompletionScopeSnapshot completionScope = RefreshCompletionScope(currentFilePath);
            if (string.IsNullOrEmpty(completionScope.WorkspaceFolder))
            {
                InvalidateCompletionWorkspace();
                ClearProjectPackageCompletionReferences();
            }
        }

        private void ClearWorkspaceCompletionData()
        {
            if (myProjectContent == null)
                return;

            lock (_completionDataLock)
            {
                foreach (var pair in _workspaceCompilationUnits)
                    myProjectContent.RemoveCompilationUnit(pair.Value);

                _workspaceCompilationUnits.Clear();
                _workspaceCompletionClasses.Clear();
                _topLevelLocalFunctions.Clear();
            }
        }

        internal ArrayList FilterCompletionDataForActiveProject(ArrayList completionData)
        {
            return FilterCompletionDataForActiveProject(completionData, GetActiveEditorFilePath());
        }

        internal ArrayList FilterCompletionDataForActiveProject(ArrayList completionData, string activeFilePath)
        {
            if (completionData == null || completionData.Count == 0)
                return completionData;

            var sourceFolders = GetCompletionSourceFolders(
                GetCompletionProjectPaths(GetCompletionProjectPath(activeFilePath)));
            var filtered = new ArrayList(completionData.Count);

            foreach (object item in completionData)
            {
                if (ShouldKeepCompletionDataItem(item, activeFilePath, sourceFolders))
                    filtered.Add(item);
            }

            return filtered;
        }

        private bool ShouldKeepCompletionDataItem(object item, string activeFilePath, IList<string> sourceFolders)
        {
            string sourcePath = GetCompletionDataSourcePath(item);
            if (string.IsNullOrWhiteSpace(sourcePath))
                return true;

            if (!string.Equals(Path.GetExtension(sourcePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return true;

            string normalizedSource = NormalizeCompletionPath(sourcePath);
            if (string.IsNullOrEmpty(normalizedSource))
                return true;

            string normalizedActive = NormalizeCompletionPath(activeFilePath);
            if (!string.IsNullOrEmpty(normalizedActive) &&
                string.Equals(normalizedSource, normalizedActive, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string sourceFolder in sourceFolders ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(sourceFolder) &&
                    Directory.Exists(sourceFolder) &&
                    IsPathInsideFolder(sourcePath, sourceFolder))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetCompletionDataSourcePath(object item)
        {
            try
            {
                if (item is Dom.IClass completionClass)
                    return completionClass.CompilationUnit?.FileName ?? string.Empty;

                if (item is Dom.IMember completionMember)
                    return completionMember.DeclaringType?.CompilationUnit?.FileName ?? string.Empty;
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void AddWorkspaceClassCompletionItems(ArrayList result, HashSet<string> seen, WorkspaceCompletionClass completionClass)
        {
            foreach (var nestedType in completionClass.NestedTypes)
            {
                AddWorkspaceCompletionItem(result, seen, nestedType);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }

            foreach (var member in completionClass.StaticMembers)
            {
                AddWorkspaceCompletionItem(result, seen, member);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddWorkspaceNamespaceCompletionItems(
            ArrayList result,
            HashSet<string> seen,
            string namespaceName,
            List<WorkspaceCompletionClass> completionClasses)
        {
            string namespacePrefix = namespaceName.Length == 0 ? string.Empty : namespaceName + ".";

            foreach (var completionClass in completionClasses)
            {
                if (completionClass.IsNested)
                    continue;

                if (string.Equals(completionClass.NamespaceName, namespaceName, StringComparison.Ordinal))
                {
                    AddWorkspaceCompletionItem(result, seen, completionClass.ToCompletionItem());
                }
                else if (completionClass.NamespaceName.StartsWith(namespacePrefix, StringComparison.Ordinal))
                {
                    string remainder = completionClass.NamespaceName.Substring(namespacePrefix.Length);
                    int separatorIndex = remainder.IndexOf('.');
                    string childNamespace = separatorIndex >= 0 ? remainder.Substring(0, separatorIndex) : remainder;
                    if (childNamespace.Length > 0)
                        AddWorkspaceCompletionItem(result, seen, new WorkspaceCompletionItem(childNamespace, "namespace " + childNamespace, 5));
                }

                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddWorkspaceCompletionItem(ArrayList result, HashSet<string> seen, WorkspaceCompletionItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Name))
                return;

            if (seen.Add(item.Name))
                result.Add(new DefaultCompletionData(item.Name, item.Description, item.ImageIndex));
        }

        private static bool MatchesWorkspaceClass(WorkspaceCompletionClass completionClass, string expression)
        {
            return string.Equals(completionClass.FullName, expression, StringComparison.Ordinal) ||
                   string.Equals(completionClass.Name, expression, StringComparison.Ordinal);
        }

        private static string NormalizeCompletionExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return string.Empty;

            expression = expression.Trim();
            while (expression.EndsWith(".", StringComparison.Ordinal))
                expression = expression.Substring(0, expression.Length - 1);

            const string globalPrefix = "global::";
            if (expression.StartsWith(globalPrefix, StringComparison.Ordinal))
                expression = expression.Substring(globalPrefix.Length);

            return expression;
        }

        private static void AddCompilationUnitMethods(ArrayList result, HashSet<string> seen, Dom.ICompilationUnit unit, string prefix)
        {
            if (unit == null || unit.Classes == null)
                return;

            foreach (var @class in unit.Classes)
            {
                AddClassMethods(result, seen, @class, prefix);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static void AddClassMethods(ArrayList result, HashSet<string> seen, Dom.IClass @class, string prefix)
        {
            if (@class == null)
                return;

            foreach (var method in @class.Methods)
            {
                if (method == null || method.IsConstructor)
                    continue;
                if (!method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = method.FullyQualifiedName + "#" + method.Parameters.Count + "#" + method.Region.BeginLine + "#" + method.Region.BeginColumn;
                if (seen.Add(key))
                    result.Add(method);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }

            foreach (var innerClass in @class.InnerClasses)
            {
                AddClassMethods(result, seen, innerClass, prefix);
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static string NormalizeCompletionPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch { return path; }
        }
    }
}
