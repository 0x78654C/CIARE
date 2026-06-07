using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CIARE.Utils
{
    internal static class SolutionStartupProjectStore
    {
        private const string SettingsDirectoryName = ".ciare";
        private const string StartupProjectFileName = "cSu.xdat";

        public static string Ensure(string solutionPath, string preferredProjectPath = null)
        {
            if (!IsSolutionFile(solutionPath))
                return string.Empty;

            List<string> solutionProjects = ReadSolutionProjectFiles(solutionPath)
                .Where(IsProjectFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string storedValue = ReadStoredValue(solutionPath);
            string startupProjectPath = ResolveStoredProjectPath(solutionPath, storedValue);
            if (!ContainsPath(solutionProjects, startupProjectPath))
            {
                startupProjectPath = ContainsPath(solutionProjects, preferredProjectPath)
                    ? Path.GetFullPath(preferredProjectPath)
                    : solutionProjects.FirstOrDefault() ?? string.Empty;
            }

            string normalizedStoredValue = GetStoredValue(solutionPath, startupProjectPath);
            if (!File.Exists(GetStartupProjectFilePath(solutionPath)) ||
                !string.Equals(storedValue, normalizedStoredValue, StringComparison.Ordinal))
            {
                Save(solutionPath, startupProjectPath);
            }

            return startupProjectPath;
        }

        public static bool Save(string solutionPath, string projectPath)
        {
            if (!IsSolutionFile(solutionPath))
                return false;

            if (!string.IsNullOrWhiteSpace(projectPath) && !IsProjectFile(projectPath))
                return false;

            try
            {
                string startupProjectFilePath = GetStartupProjectFilePath(solutionPath);
                Directory.CreateDirectory(Path.GetDirectoryName(startupProjectFilePath));
                File.WriteAllText(startupProjectFilePath, GetStoredValue(solutionPath, projectPath), Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<string> ReadSolutionProjectFiles(string solutionPath)
        {
            if (!IsSolutionFile(solutionPath))
                yield break;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(solutionDirectory))
                yield break;

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadLines(solutionPath).ToArray();
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
                string resolvedProjectPath;
                try
                {
                    if (!Path.IsPathRooted(projectPath))
                        projectPath = Path.Combine(solutionDirectory, projectPath);

                    resolvedProjectPath = Path.GetFullPath(projectPath);
                }
                catch
                {
                    continue;
                }

                yield return resolvedProjectPath;
            }
        }

        private static string ReadStoredValue(string solutionPath)
        {
            try
            {
                string startupProjectFilePath = GetStartupProjectFilePath(solutionPath);
                return File.Exists(startupProjectFilePath)
                    ? File.ReadAllText(startupProjectFilePath).Trim()
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveStoredProjectPath(string solutionPath, string storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return string.Empty;

            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                string projectPath = Path.IsPathRooted(storedValue)
                    ? storedValue
                    : Path.Combine(solutionDirectory, storedValue);
                return Path.GetFullPath(projectPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetStoredValue(string solutionPath, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return string.Empty;

            try
            {
                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                return Path.GetRelativePath(solutionDirectory, Path.GetFullPath(projectPath));
            }
            catch
            {
                return projectPath;
            }
        }

        private static string GetStartupProjectFilePath(string solutionPath)
        {
            string solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
            return Path.Combine(solutionDirectory, SettingsDirectoryName, StartupProjectFileName);
        }

        private static bool ContainsPath(IEnumerable<string> paths, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
                return false;

            string normalizedCandidate;
            try
            {
                normalizedCandidate = Path.GetFullPath(candidatePath);
            }
            catch
            {
                return false;
            }

            return paths.Any(path => string.Equals(path, normalizedCandidate, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSolutionFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".sln", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProjectFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase);
        }
    }
}
