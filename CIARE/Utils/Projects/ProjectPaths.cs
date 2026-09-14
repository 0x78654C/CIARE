using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectFiles;

namespace CIARE.Utils.Projects
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ProjectPaths
    {
        private readonly MainForm _mainForm;

        internal ProjectPaths(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal void UpdateProjectReferencesAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (!Directory.Exists(_mainForm.ExplorerTreeFeature._fileExplorerRootPath))
                return;

            try
            {
                foreach (string projectPath in EnumerateBuildFiles(_mainForm.ExplorerTreeFeature._fileExplorerRootPath, "*.csproj"))
                    UpdateProjectFileItemReferences(projectPath, oldPath, newPath, renamedDirectory);

                var projectPathPairs = GetRenamedProjectPathPairs(oldPath, newPath, renamedDirectory).ToList();
                if (projectPathPairs.Count == 0)
                    return;

                foreach (string solutionPath in EnumerateBuildFiles(_mainForm.ExplorerTreeFeature._fileExplorerRootPath, "*.sln"))
                    UpdateSolutionProjectReferences(solutionPath, projectPathPairs);
            }
            catch
            {
            }
        }

        private static void UpdateProjectFileItemReferences(string projectPath, string oldPath, string newPath,
            bool renamedDirectory)
        {
            try
            {
                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (string.IsNullOrEmpty(projectDirectory))
                    return;

                XDocument document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                bool changed = false;
                foreach (XAttribute attribute in document.Descendants()
                    .SelectMany(element => element.Attributes())
                    .Where(IsProjectItemPathAttribute)
                    .ToList())
                {
                    string value = attribute.Value;
                    if (!TryGetRenamedProjectItemReference(projectDirectory, value, oldPath, newPath,
                            renamedDirectory, out string updatedValue))
                    {
                        continue;
                    }

                    if (string.Equals(value, updatedValue, StringComparison.Ordinal))
                        continue;

                    attribute.Value = updatedValue;
                    changed = true;
                }

                if (changed)
                    document.Save(projectPath, SaveOptions.DisableFormatting);
            }
            catch
            {
            }
        }

        private static bool IsProjectItemPathAttribute(XAttribute attribute)
        {
            string attributeName = attribute.Name.LocalName;
            if (!string.Equals(attributeName, "Include", StringComparison.Ordinal) &&
                !string.Equals(attributeName, "Update", StringComparison.Ordinal) &&
                !string.Equals(attributeName, "Remove", StringComparison.Ordinal))
            {
                return false;
            }

            switch (attribute.Parent?.Name.LocalName)
            {
                case "AdditionalFiles":
                case "Analyzer":
                case "ApplicationDefinition":
                case "Compile":
                case "Content":
                case "EmbeddedResource":
                case "None":
                case "Page":
                case "ProjectReference":
                case "Resource":
                case "SplashScreen":
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetRenamedProjectItemReference(string projectDirectory, string value, string oldPath,
            string newPath, bool renamedDirectory, out string updatedValue)
        {
            updatedValue = string.Empty;
            string candidate = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(candidate) ||
                candidate.Contains("$(") ||
                candidate.IndexOfAny(new[] { '*', '?', ';' }) >= 0)
            {
                return false;
            }

            try
            {
                string fullPath = Path.IsPathRooted(candidate)
                    ? Path.GetFullPath(candidate)
                    : Path.GetFullPath(Path.Combine(projectDirectory, candidate));
                string renamedPath = GetRenamedExplorerPath(fullPath, oldPath, newPath, renamedDirectory);
                if (string.IsNullOrEmpty(renamedPath))
                    return false;

                string projectPath = Path.IsPathRooted(candidate)
                    ? renamedPath
                    : Path.GetRelativePath(projectDirectory, renamedPath);
                updatedValue = PreserveProjectPathSeparators(value, projectPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string PreserveProjectPathSeparators(string originalValue, string path)
        {
            if ((originalValue ?? string.Empty).Contains("/") && !(originalValue ?? string.Empty).Contains("\\"))
                return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetRenamedProjectPathPairs(string oldPath,
            string newPath, bool renamedDirectory)
        {
            if (!renamedDirectory)
            {
                if (string.Equals(Path.GetExtension(oldPath), ".csproj", StringComparison.OrdinalIgnoreCase))
                    yield return new KeyValuePair<string, string>(oldPath, newPath);
                yield break;
            }

            if (!Directory.Exists(newPath))
                yield break;

            foreach (string newProjectPath in EnumerateBuildFiles(newPath, "*.csproj"))
            {
                string relativePath;
                try
                {
                    relativePath = Path.GetRelativePath(newPath, newProjectPath);
                }
                catch
                {
                    continue;
                }

                yield return new KeyValuePair<string, string>(
                    Path.Combine(oldPath, relativePath),
                    newProjectPath);
            }
        }

        private static void UpdateSolutionProjectReferences(string solutionPath,
            IEnumerable<KeyValuePair<string, string>> projectPathPairs)
        {
            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(solutionDirectory))
                    return;

                string content = File.ReadAllText(solutionPath);
                string updatedContent = content;
                foreach (var projectPathPair in projectPathPairs)
                {
                    string oldRelativePath = Path.GetRelativePath(solutionDirectory, projectPathPair.Key)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    string newRelativePath = Path.GetRelativePath(solutionDirectory, projectPathPair.Value)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    updatedContent = ReplaceOrdinalIgnoreCase(updatedContent, oldRelativePath, newRelativePath);
                    updatedContent = ReplaceOrdinalIgnoreCase(updatedContent, oldRelativePath.Replace('\\', '/'),
                        newRelativePath.Replace('\\', '/'));
                }

                if (!string.Equals(content, updatedContent, StringComparison.Ordinal))
                    File.WriteAllText(solutionPath, updatedContent);
            }
            catch
            {
            }
        }

        internal static List<string> GetExplorerDeletedProjectPaths(string path, bool isDirectory)
        {
            if (isDirectory)
                return EnumerateBuildFiles(path, "*.csproj")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return IsProjectFilePath(path)
                ? new List<string> { path }
                : new List<string>();
        }

        internal static void RemoveProjectsFromWorkspaceSolutions(IList<string> projectPaths, string workspaceFolder)
        {
            if (projectPaths == null || projectPaths.Count == 0 ||
                string.IsNullOrWhiteSpace(workspaceFolder) ||
                !Directory.Exists(workspaceFolder))
            {
                return;
            }

            foreach (string solutionPath in EnumerateBuildFiles(workspaceFolder, "*.sln"))
                RemoveProjectsFromSolution(solutionPath, projectPaths);
        }

        private static void RemoveProjectsFromSolution(string solutionPath, IList<string> projectPaths)
        {
            if (!IsSolutionFilePath(solutionPath) || projectPaths == null || projectPaths.Count == 0)
                return;

            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(solutionDirectory))
                    return;

                var deletedProjects = new HashSet<string>(
                    projectPaths.Select(NormalizeCompletionPath).Where(path => !string.IsNullOrEmpty(path)),
                    StringComparer.OrdinalIgnoreCase);
                if (deletedProjects.Count == 0)
                    return;

                var lines = File.ReadAllLines(solutionPath).ToList();
                var updatedLines = new List<string>(lines.Count);
                var removedProjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < lines.Count; i++)
                {
                    if (TryGetRemovedSolutionProjectGuid(lines[i], solutionDirectory, deletedProjects,
                        out string projectGuid))
                    {
                        if (!string.IsNullOrWhiteSpace(projectGuid))
                            removedProjectGuids.Add(projectGuid);

                        while (i + 1 < lines.Count &&
                            !lines[i].Trim().Equals("EndProject", StringComparison.OrdinalIgnoreCase))
                        {
                            i++;
                        }

                        continue;
                    }

                    updatedLines.Add(lines[i]);
                }

                if (removedProjectGuids.Count == 0)
                    return;

                updatedLines = updatedLines
                    .Where(line => !IsRemovedSolutionProjectConfigurationLine(line, removedProjectGuids))
                    .ToList();

                File.WriteAllLines(solutionPath, updatedLines);
            }
            catch
            {
            }
        }

        private static bool TryGetRemovedSolutionProjectGuid(string line, string solutionDirectory,
            HashSet<string> deletedProjects, out string projectGuid)
        {
            projectGuid = string.Empty;
            if (string.IsNullOrWhiteSpace(line) ||
                string.IsNullOrWhiteSpace(solutionDirectory) ||
                deletedProjects == null ||
                deletedProjects.Count == 0)
            {
                return false;
            }

            Match match = Regex.Match(line,
                @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)"",\s*""(\{[^}]+\})""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                return false;

            string projectPath = match.Groups[1].Value;
            try
            {
                if (!Path.IsPathRooted(projectPath))
                    projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));
            }
            catch
            {
                return false;
            }

            if (!deletedProjects.Contains(NormalizeCompletionPath(projectPath)))
                return false;

            projectGuid = match.Groups[2].Value;
            return true;
        }

        private static bool IsRemovedSolutionProjectConfigurationLine(string line,
            HashSet<string> removedProjectGuids)
        {
            if (string.IsNullOrWhiteSpace(line) || removedProjectGuids == null || removedProjectGuids.Count == 0)
                return false;

            string trimmed = line.TrimStart();
            return removedProjectGuids.Any(guid =>
                trimmed.StartsWith(guid + ".", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(guid + " =", StringComparison.OrdinalIgnoreCase));
        }

        private static string ReplaceOrdinalIgnoreCase(string value, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(oldValue))
                return value;

            var builder = new StringBuilder(value.Length);
            int startIndex = 0;
            while (true)
            {
                int matchIndex = value.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);
                if (matchIndex < 0)
                {
                    builder.Append(value, startIndex, value.Length - startIndex);
                    break;
                }

                builder.Append(value, startIndex, matchIndex - startIndex);
                builder.Append(newValue);
                startIndex = matchIndex + oldValue.Length;
            }

            return builder.ToString();
        }

        internal static string GetRenamedExplorerPath(string path, string oldPath, string newPath, bool renamedDirectory)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                return string.Empty;

            try
            {
                string normalizedPath = NormalizeCompletionPath(path);
                string normalizedOldPath = NormalizeCompletionPath(oldPath);

                if (!renamedDirectory)
                    return string.Equals(normalizedPath, normalizedOldPath, StringComparison.OrdinalIgnoreCase)
                        ? newPath
                        : string.Empty;

                if (!IsSameOrChildDirectory(normalizedPath, normalizedOldPath))
                    return string.Empty;

                string relativePath = normalizedPath.Length == normalizedOldPath.Length
                    ? string.Empty
                    : normalizedPath.Substring(normalizedOldPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return string.IsNullOrEmpty(relativePath)
                    ? newPath
                    : Path.Combine(newPath, relativePath);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
