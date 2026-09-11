using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private static bool IsProjectFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                File.Exists(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSolutionFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                File.Exists(filePath) &&
                string.Equals(Path.GetExtension(filePath), ".sln", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ProjectContainsSourceFile(string projectPath, string sourceFilePath)
        {
            if (!IsProjectFilePath(projectPath) || !IsCSharpFilePath(sourceFilePath) || !File.Exists(sourceFilePath))
                return false;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory) ||
                !IsSameOrChildDirectory(Path.GetDirectoryName(sourceFilePath), projectDirectory))
            {
                return false;
            }

            XDocument document;
            try
            {
                document = XDocument.Load(projectPath);
            }
            catch
            {
                return false;
            }

            if (ProjectItemMatchesSourceFile(document, projectDirectory, sourceFilePath, "Compile", "Remove"))
                return false;

            if (ProjectCompileIncludeMatchesSourceFile(document, projectDirectory, sourceFilePath))
                return true;

            if (!ProjectUsesSdkStyle(document) || !DefaultCompileItemsEnabled(document))
                return false;

            return IsDefaultCompileCandidate(projectDirectory, sourceFilePath);
        }

        private static bool ProjectUsesSdkStyle(XDocument document)
        {
            if (document?.Root == null)
                return false;

            return document.Root.Attribute("Sdk") != null ||
                document.Root.Elements()
                    .Any(element => string.Equals(element.Name.LocalName, "Import", StringComparison.Ordinal) &&
                        element.Attribute("Sdk") != null);
        }

        private static bool DefaultCompileItemsEnabled(XDocument document)
        {
            return !ProjectPropertyIsFalse(document, "EnableDefaultItems") &&
                !ProjectPropertyIsFalse(document, "EnableDefaultCompileItems");
        }

        private static bool ProjectPropertyIsFalse(XDocument document, string propertyName)
        {
            return document?.Root?
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))
                .Any(element => string.Equals((element.Value ?? string.Empty).Trim(), "false",
                    StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static bool IsDefaultCompileCandidate(string projectDirectory, string sourceFilePath)
        {
            if (!IsSameOrChildDirectory(sourceFilePath, projectDirectory))
                return false;

            string relativePath;
            try
            {
                relativePath = Path.GetRelativePath(projectDirectory, sourceFilePath);
            }
            catch
            {
                return false;
            }

            string normalizedRelativePath = relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            if (string.IsNullOrWhiteSpace(normalizedRelativePath) ||
                normalizedRelativePath == ".." ||
                normalizedRelativePath.StartsWith("../"))
            {
                return false;
            }

            string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !segments.Any(segment =>
                string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ProjectCompileIncludeMatchesSourceFile(XDocument document, string projectDirectory,
            string sourceFilePath)
        {
            if (document?.Root == null)
                return false;

            foreach (XElement item in document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal)))
            {
                string include = item.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include) ||
                    !ProjectItemSpecMatchesSourceFile(projectDirectory, include, sourceFilePath))
                {
                    continue;
                }

                string exclude = item.Attribute("Exclude")?.Value;
                if (!string.IsNullOrWhiteSpace(exclude) &&
                    ProjectItemSpecMatchesSourceFile(projectDirectory, exclude, sourceFilePath))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ProjectItemMatchesSourceFile(XDocument document, string projectDirectory,
            string sourceFilePath, string itemName, string attributeName)
        {
            if (document?.Root == null)
                return false;

            foreach (XElement item in document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal)))
            {
                string itemSpec = item.Attribute(attributeName)?.Value;
                if (string.IsNullOrWhiteSpace(itemSpec))
                    continue;

                if (ProjectItemSpecMatchesSourceFile(projectDirectory, itemSpec, sourceFilePath))
                    return true;
            }

            return false;
        }

        private static bool ProjectItemSpecMatchesSourceFile(string projectDirectory, string itemSpec,
            string sourceFilePath)
        {
            foreach (string spec in SplitProjectItemSpec(itemSpec))
            {
                if (spec.Contains("$"))
                    continue;

                bool hasWildcards = spec.IndexOfAny(new[] { '*', '?' }) >= 0;
                if (!hasWildcards)
                {
                    string itemPath = ResolveProjectItemPath(projectDirectory, spec);
                    if (string.Equals(NormalizeCompletionPath(itemPath), NormalizeCompletionPath(sourceFilePath),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    continue;
                }

                string pattern = NormalizeProjectItemPattern(spec);
                string candidate = Path.IsPathRooted(spec)
                    ? NormalizeCompletionPath(sourceFilePath).Replace(Path.DirectorySeparatorChar, '/')
                    : GetProjectRelativePath(projectDirectory, sourceFilePath);

                if (!string.IsNullOrEmpty(candidate) && GlobMatches(pattern, candidate))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> SplitProjectItemSpec(string itemSpec)
        {
            return (itemSpec ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(spec => spec.Trim())
                .Where(spec => !string.IsNullOrWhiteSpace(spec));
        }

        private static string ResolveProjectItemPath(string projectDirectory, string itemSpec)
        {
            try
            {
                return Path.IsPathRooted(itemSpec)
                    ? Path.GetFullPath(itemSpec)
                    : Path.GetFullPath(Path.Combine(projectDirectory, itemSpec));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetProjectRelativePath(string projectDirectory, string sourceFilePath)
        {
            try
            {
                return Path.GetRelativePath(projectDirectory, sourceFilePath)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(Path.AltDirectorySeparatorChar, '/');
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeProjectItemPattern(string itemSpec)
        {
            string pattern = (itemSpec ?? string.Empty)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            while (pattern.StartsWith("./", StringComparison.Ordinal))
                pattern = pattern.Substring(2);

            return pattern;
        }

        private static bool GlobMatches(string pattern, string candidate)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(candidate))
                return false;

            var regex = new StringBuilder("^");
            for (int i = 0; i < pattern.Length; i++)
            {
                char ch = pattern[i];
                if (ch == '*')
                {
                    bool recursive = i + 1 < pattern.Length && pattern[i + 1] == '*';
                    if (recursive && i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        regex.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        regex.Append(recursive ? ".*" : "[^/]*");
                        if (recursive)
                            i++;
                    }
                }
                else if (ch == '?')
                {
                    regex.Append("[^/]");
                }
                else
                {
                    regex.Append(Regex.Escape(ch.ToString()));
                }
            }

            regex.Append("$");
            return Regex.IsMatch(candidate, regex.ToString(),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static IEnumerable<string> ReadSolutionProjectFiles(string solutionPath)
        {
            if (!IsSolutionFilePath(solutionPath))
                yield break;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(solutionDirectory))
                yield break;

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadLines(solutionPath);
            }
            catch
            {
                yield break;
            }

            foreach (string line in lines)
            {
                Match match = Regex.Match(line,
                    @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                string projectPath = match.Groups[1].Value;
                if (!Path.IsPathRooted(projectPath))
                    projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));

                if (IsProjectFilePath(projectPath))
                    yield return projectPath;
            }
        }

        private static int GetCommonPathLength(string filePath, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(folderPath))
                return 0;

            string normalizedFile = NormalizeCompletionPath(filePath);
            string normalizedFolder = NormalizeCompletionPath(folderPath);
            if (string.IsNullOrEmpty(normalizedFile) || string.IsNullOrEmpty(normalizedFolder))
                return 0;

            return normalizedFile.StartsWith(normalizedFolder + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) ||
                normalizedFile.StartsWith(normalizedFolder + Path.AltDirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                ? normalizedFolder.Length
                : 0;
        }
    }
}
