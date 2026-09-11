using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private string _fileExplorerSolutionPath = string.Empty;

        private string GetActiveWorkspaceFolder()
        {
            string filePath = GetActiveEditorFilePath();

            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            if (string.IsNullOrEmpty(filePath) || IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return _fileExplorerRootPath;

            return string.Empty;
        }

        public string GetActiveCompileProjectPath()
        {
            string filePath = GetActiveEditorFilePath();

            if (IsActiveUntitledEditorPage())
                return string.Empty;

            if (IsCSharpFilePath(filePath))
                return GetBuildTargetForActiveCSharpFile(filePath);

            if (Directory.Exists(_fileExplorerRootPath))
            {
                if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                    IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
                {
                    return _fileExplorerStartupProjectPath;
                }

                string activeFilePath = File.Exists(filePath) &&
                    IsPathInsideFolder(filePath, _fileExplorerRootPath)
                    ? filePath
                    : null;

                string openFolderTarget = FindBuildTargetFile(_fileExplorerRootPath, activeFilePath);
                if (!string.IsNullOrEmpty(openFolderTarget))
                    return openFolderTarget;
            }

            return string.Empty;
        }

        public string GetActiveRunProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string filePath = GetActiveEditorFilePath();
            if (IsActiveUntitledEditorPage() || string.IsNullOrEmpty(filePath))
                return string.Empty;

            if (!IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return string.Empty;

            if (IsCSharpFilePath(filePath))
            {
                string sourceProject = FindProjectContainingSourceFile(filePath);
                if (string.IsNullOrEmpty(sourceProject))
                    return string.Empty;

                if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                    IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
                {
                    return _fileExplorerStartupProjectPath;
                }

                return sourceProject;
            }

            if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath))
            {
                return _fileExplorerStartupProjectPath;
            }

            string activeProject = FindProjectFileForPath(filePath, _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(activeProject))
                return activeProject;

            string selectedPath = _fileExplorerTree?.SelectedNode?.Tag as string;
            string selectedProject = FindProjectFileForPath(selectedPath, _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(selectedProject))
                return selectedProject;

            string rootProject = FindTopLevelBuildFile(_fileExplorerRootPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            return FindSingleRecursiveBuildFile(_fileExplorerRootPath, "*.csproj");
        }

        private string GetBuildTargetForActiveCSharpFile(string filePath)
        {
            if (!Directory.Exists(_fileExplorerRootPath) ||
                !File.Exists(filePath) ||
                !IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                return string.Empty;
            }

            string activeProject = FindProjectContainingSourceFile(filePath);
            if (string.IsNullOrEmpty(activeProject))
                return string.Empty;

            if (IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                IsPathInsideFolder(_fileExplorerStartupProjectPath, _fileExplorerRootPath) &&
                ProjectContainsSourceFile(_fileExplorerStartupProjectPath, filePath))
            {
                return _fileExplorerStartupProjectPath;
            }

            string activeSolution = FindContainingSolutionForProjectInWorkspace(activeProject);
            return !string.IsNullOrEmpty(activeSolution) ? activeSolution : activeProject;
        }

        private string FindProjectContainingSourceFile(string filePath)
        {
            if (!IsCSharpFilePath(filePath) ||
                !Directory.Exists(_fileExplorerRootPath) ||
                !IsPathInsideFolder(filePath, _fileExplorerRootPath))
            {
                return string.Empty;
            }

            foreach (string projectPath in GetProjectCandidatesForSourceFile(filePath))
            {
                if (ProjectContainsSourceFile(projectPath, filePath))
                    return projectPath;
            }

            return string.Empty;
        }

        private IEnumerable<string> GetProjectCandidatesForSourceFile(string filePath)
        {
            var candidates = new List<string>();
            string activeFolder = Path.GetDirectoryName(filePath);

            string activeSolution = FindNearestBuildFile(activeFolder, _fileExplorerRootPath, "*.sln");
            if (!string.IsNullOrEmpty(activeSolution))
                candidates.AddRange(ReadSolutionProjectFiles(activeSolution));

            candidates.AddRange(EnumerateBuildFiles(_fileExplorerRootPath, "*.csproj"));

            return candidates
                .Where(IsProjectFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(projectPath => GetCommonPathLength(filePath, Path.GetDirectoryName(projectPath)))
                .ThenBy(projectPath => projectPath, StringComparer.OrdinalIgnoreCase);
        }

        private string FindContainingSolutionForProjectInWorkspace(string projectPath)
        {
            if (!IsProjectFilePath(projectPath) || !Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string projectFolder = Path.GetDirectoryName(projectPath);
            string solutionPath = FindNearestBuildFile(projectFolder, _fileExplorerRootPath, "*.sln");
            return SolutionFileContainsProject(solutionPath, projectPath) ? solutionPath : string.Empty;
        }

        public string GetActivePackageProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string selectedPath = _fileExplorerTree?.SelectedNode?.Tag as string;
            string selectedProject = FindProjectFileForPath(selectedPath, _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(selectedProject))
                return selectedProject;

            string activeProject = FindProjectFileForPath(GetActiveEditorFilePath(), _fileExplorerRootPath);
            if (!string.IsNullOrEmpty(activeProject))
                return activeProject;

            string rootProject = FindTopLevelBuildFile(_fileExplorerRootPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            return FindSingleRecursiveBuildFile(_fileExplorerRootPath, "*.csproj");
        }

        public string GetActivePackageInstallProjectPath()
        {
            if (!Directory.Exists(_fileExplorerRootPath))
                return string.Empty;

            string filePath = GetActiveEditorFilePath();
            if (!File.Exists(filePath) || !IsPathInsideFolder(filePath, _fileExplorerRootPath))
                return string.Empty;

            return FindProjectFileForPath(filePath, _fileExplorerRootPath);
        }

        private string GetActiveEditorPackageProjectPath()
        {
            string activeProject = GetActivePackageInstallProjectPath();
            return !string.IsNullOrEmpty(activeProject) ? activeProject : GetActivePackageProjectPath();
        }

        private static bool IsCSharpFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase);
        }

        private string GetProjectPathFromExplorerPath(string path)
        {
            if (IsProjectFilePath(path))
                return path;

            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (File.Exists(path))
                return Directory.Exists(_fileExplorerRootPath)
                    ? FindProjectFileForPath(path, _fileExplorerRootPath)
                    : string.Empty;

            if (!Directory.Exists(path))
                return string.Empty;

            string childProject = FindTopLevelBuildFile(path, "*.csproj");
            if (!string.IsNullOrEmpty(childProject))
                return childProject;

            return Directory.Exists(_fileExplorerRootPath)
                ? FindNearestBuildFile(path, _fileExplorerRootPath, "*.csproj")
                : string.Empty;
        }

        private string GetSolutionPathFromExplorerPath(string path)
        {
            if (IsSolutionFilePath(path))
                return path;

            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string folder = Directory.Exists(path)
                ? path
                : File.Exists(path)
                    ? Path.GetDirectoryName(path)
                    : string.Empty;

            if (string.IsNullOrEmpty(folder))
                return string.Empty;

            if (IsSolutionFilePath(_fileExplorerSolutionPath) &&
                IsSameOrChildDirectory(folder, Path.GetDirectoryName(_fileExplorerSolutionPath)))
            {
                return _fileExplorerSolutionPath;
            }

            if (Directory.Exists(_fileExplorerRootPath))
                return FindNearestBuildFile(folder, _fileExplorerRootPath, "*.sln");

            return FindTopLevelBuildFile(folder, "*.sln");
        }

        private static string ResolveSolutionPathForLoadedFolder(string folderPath, string solutionPath)
        {
            if (solutionPath != null)
                return IsSolutionFilePath(solutionPath) ? Path.GetFullPath(solutionPath) : string.Empty;

            string topLevelSolution = FindTopLevelBuildFile(folderPath, "*.sln");
            if (!string.IsNullOrEmpty(topLevelSolution))
                return topLevelSolution;

            return FindSingleRecursiveBuildFile(folderPath, "*.sln");
        }

        private static bool IsAddProjectToSolutionContext(string path, string solutionPath)
        {
            if (!IsSolutionFilePath(solutionPath) || string.IsNullOrWhiteSpace(path))
                return false;

            if (IsSolutionFilePath(path))
                return string.Equals(NormalizeCompletionPath(path),
                    NormalizeCompletionPath(solutionPath), StringComparison.OrdinalIgnoreCase);

            if (!Directory.Exists(path))
                return false;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            return !string.IsNullOrEmpty(solutionDirectory) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(solutionDirectory),
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string FindProjectFileForPath(string path, string workspaceFolder)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(workspaceFolder) ||
                !Directory.Exists(workspaceFolder))
            {
                return string.Empty;
            }

            if (IsProjectFilePath(path) && IsPathInsideFolder(path, workspaceFolder))
                return path;

            string folder = Directory.Exists(path)
                ? path
                : File.Exists(path)
                    ? Path.GetDirectoryName(path)
                    : string.Empty;

            if (string.IsNullOrEmpty(folder) || !IsSameOrChildDirectory(folder, workspaceFolder))
                return string.Empty;

            string childProject = FindTopLevelBuildFile(folder, "*.csproj");
            if (!string.IsNullOrEmpty(childProject))
                return childProject;

            return FindNearestBuildFile(folder, workspaceFolder, "*.csproj");
        }

        private static bool IsPathInsideFolder(string filePath, string folderPath)
        {
            try
            {
                string fullFolder = Path.GetFullPath(folderPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullPath = Path.GetFullPath(filePath);
                return fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string FindBuildTargetFile(string folderPath, string activeFilePath = null)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return string.Empty;

            string rootSolution = FindTopLevelBuildFile(folderPath, "*.sln");
            if (!string.IsNullOrEmpty(rootSolution))
                return rootSolution;

            if (IsCSharpFilePath(activeFilePath))
            {
                string activeFolder = Path.GetDirectoryName(activeFilePath);

                string activeSolution = FindNearestBuildFile(activeFolder, folderPath, "*.sln");
                if (!string.IsNullOrEmpty(activeSolution))
                    return activeSolution;

                string activeProject = FindNearestBuildFile(activeFolder, folderPath, "*.csproj");
                if (!string.IsNullOrEmpty(activeProject))
                    return activeProject;
            }

            string rootProject = FindTopLevelBuildFile(folderPath, "*.csproj");
            if (!string.IsNullOrEmpty(rootProject))
                return rootProject;

            string singleSolution = FindSingleRecursiveBuildFile(folderPath, "*.sln");
            if (!string.IsNullOrEmpty(singleSolution))
                return singleSolution;

            return FindSingleRecursiveBuildFile(folderPath, "*.csproj");
        }

        private static string FindBuildTargetFromActiveFile(string filePath)
        {
            if (!IsCSharpFilePath(filePath))
                return string.Empty;

            string activeFolder = Path.GetDirectoryName(filePath);
            string solution = FindNearestBuildFile(activeFolder, null, "*.sln");
            if (!string.IsNullOrEmpty(solution))
                return solution;

            return FindNearestBuildFile(activeFolder, null, "*.csproj");
        }

        private static string FindTopLevelBuildFile(string folderPath, string searchPattern)
        {
            try
            {
                return Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FindNearestBuildFile(string startFolder, string stopFolder, string searchPattern)
        {
            if (string.IsNullOrEmpty(startFolder) || !Directory.Exists(startFolder))
                return string.Empty;

            if (!string.IsNullOrEmpty(stopFolder) && !IsSameOrChildDirectory(startFolder, stopFolder))
                return string.Empty;

            string folder = startFolder;
            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                string buildFile = FindTopLevelBuildFile(folder, searchPattern);
                if (!string.IsNullOrEmpty(buildFile))
                    return buildFile;

                if (!string.IsNullOrEmpty(stopFolder) &&
                    string.Equals(NormalizeCompletionPath(folder), NormalizeCompletionPath(stopFolder), StringComparison.OrdinalIgnoreCase))
                    break;

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || parent == folder)
                    break;

                if (!string.IsNullOrEmpty(stopFolder) && !IsSameOrChildDirectory(parent, stopFolder))
                    break;

                folder = parent;
            }

            return string.Empty;
        }

        private static string FindSingleRecursiveBuildFile(string folderPath, string searchPattern)
        {
            try
            {
                var matches = EnumerateBuildFiles(folderPath, searchPattern)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Take(2)
                    .ToList();

                return matches.Count == 1 ? matches[0] : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IEnumerable<string> EnumerateBuildFiles(string folderPath, string searchPattern)
        {
            var pending = new Stack<string>();
            pending.Push(folderPath);

            while (pending.Count > 0)
            {
                string current = pending.Pop();

                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(current, searchPattern); }
                catch { files = Array.Empty<string>(); }

                foreach (string file in files)
                    yield return file;

                IEnumerable<string> directories;
                try { directories = Directory.EnumerateDirectories(current); }
                catch { directories = Array.Empty<string>(); }

                foreach (string directory in directories)
                {
                    string name = Path.GetFileName(directory);
                    if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase))
                        continue;

                    pending.Push(directory);
                }
            }
        }

        private static bool IsSameOrChildDirectory(string path, string folderPath)
        {
            try
            {
                string fullPath = NormalizeCompletionPath(path);
                string fullFolder = NormalizeCompletionPath(folderPath);

                return string.Equals(fullPath, fullFolder, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullFolder + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
