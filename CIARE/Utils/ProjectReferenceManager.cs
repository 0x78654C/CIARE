using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CIARE.Utils
{
    public static class ProjectReferenceManager
    {
        private static readonly string[] SkippedFolders =
        {
            ".git",
            ".vs",
            "bin",
            "obj",
            "node_modules",
            "packages"
        };

        public static List<string> GetReferenceableProjects(string projectPath, string solutionPath,
            string workspaceFolder)
        {
            if (!IsProjectFile(projectPath))
                return new List<string>();

            string normalizedProject = NormalizePath(projectPath);
            return EnumerateCandidateProjects(solutionPath, workspaceFolder)
                .Where(path => IsProjectFile(path))
                .Where(path => !string.Equals(NormalizePath(path), normalizedProject,
                    StringComparison.OrdinalIgnoreCase))
                .Where(path => !HasProjectReference(projectPath, path))
                .Where(path => !ProjectReferencesProject(path, projectPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool AddProjectReference(string projectPath, string referencedProjectPath,
            out string message)
        {
            message = string.Empty;
            if (!IsProjectFile(projectPath))
            {
                message = "No valid target .csproj file was found.";
                return false;
            }

            if (!IsProjectFile(referencedProjectPath))
            {
                message = "No valid referenced .csproj file was found.";
                return false;
            }

            if (string.Equals(NormalizePath(projectPath), NormalizePath(referencedProjectPath),
                StringComparison.OrdinalIgnoreCase))
            {
                message = "A project cannot reference itself.";
                return false;
            }

            if (HasProjectReference(projectPath, referencedProjectPath))
            {
                message = $"{Path.GetFileNameWithoutExtension(projectPath)} already references " +
                    $"{Path.GetFileNameWithoutExtension(referencedProjectPath)}.";
                return false;
            }

            if (ProjectReferencesProject(referencedProjectPath, projectPath))
            {
                message = "Adding that project reference would create a circular reference.";
                return false;
            }

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                if (document.Root == null)
                {
                    message = "The project file is empty.";
                    return false;
                }

                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (string.IsNullOrEmpty(projectDirectory))
                {
                    message = "The target project directory was not found.";
                    return false;
                }

                string includePath = Path.GetRelativePath(projectDirectory, referencedProjectPath)
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                XElement itemGroup = FindProjectReferenceItemGroup(document) ??
                    CreateProjectReferenceItemGroup(document);
                AddProjectReferenceElement(itemGroup, includePath);
                document.Save(projectPath, SaveOptions.DisableFormatting);

                message = $"{Path.GetFileNameWithoutExtension(referencedProjectPath)} was added as a project reference.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static List<string> GetProjectReferences(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return new List<string>();

            return ReadProjectReferenceFiles(projectPath)
                .Where(IsProjectFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool RemoveProjectReference(string projectPath, string referencedProjectPath,
            out string message)
        {
            message = string.Empty;
            if (!IsProjectFile(projectPath))
            {
                message = "No valid target .csproj file was found.";
                return false;
            }

            if (!IsProjectFile(referencedProjectPath))
            {
                message = "No valid referenced .csproj file was found.";
                return false;
            }

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                if (document.Root == null)
                {
                    message = "The project file is empty.";
                    return false;
                }

                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (string.IsNullOrEmpty(projectDirectory))
                {
                    message = "The target project directory was not found.";
                    return false;
                }

                string normalizedReference = NormalizePath(referencedProjectPath);
                XElement projectReference = FindProjectReferenceElements(document)
                    .FirstOrDefault(element =>
                    {
                        string include = element.Attribute("Include")?.Value;
                        string referencePath = ResolveProjectReferencePath(projectDirectory, include);
                        return string.Equals(NormalizePath(referencePath), normalizedReference,
                            StringComparison.OrdinalIgnoreCase);
                    });

                if (projectReference == null)
                {
                    message = $"{Path.GetFileNameWithoutExtension(projectPath)} does not reference " +
                        $"{Path.GetFileNameWithoutExtension(referencedProjectPath)}.";
                    return false;
                }

                XElement itemGroup = projectReference.Parent;
                RemoveElementWithLeadingWhitespace(projectReference);
                RemoveItemGroupIfEmpty(itemGroup);
                document.Save(projectPath, SaveOptions.DisableFormatting);

                message = $"{Path.GetFileNameWithoutExtension(referencedProjectPath)} was removed from project references.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool HasProjectReference(string projectPath, string referencedProjectPath)
        {
            string normalizedReference = NormalizePath(referencedProjectPath);
            if (string.IsNullOrEmpty(normalizedReference))
                return false;

            return ReadProjectReferenceFiles(projectPath)
                .Any(path => string.Equals(NormalizePath(path), normalizedReference,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> EnumerateCandidateProjects(string solutionPath, string workspaceFolder)
        {
            var projectPaths = new List<string>();
            if (IsSolutionFile(solutionPath))
                projectPaths.AddRange(ReadSolutionProjectFiles(solutionPath));

            if (projectPaths.Count == 0 && Directory.Exists(workspaceFolder))
                projectPaths.AddRange(EnumerateProjectFiles(workspaceFolder));

            return projectPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ReadSolutionProjectFiles(string solutionPath)
        {
            if (!IsSolutionFile(solutionPath))
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

                if (IsProjectFile(projectPath))
                    yield return projectPath;
            }
        }

        private static IEnumerable<string> EnumerateProjectFiles(string workspaceFolder)
        {
            var pending = new Stack<string>();
            pending.Push(workspaceFolder);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                IEnumerable<string> projectFiles;
                try
                {
                    projectFiles = Directory.EnumerateFiles(current, "*.csproj", SearchOption.TopDirectoryOnly)
                        .ToList();
                }
                catch
                {
                    continue;
                }

                foreach (string projectFile in projectFiles)
                    yield return projectFile;

                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(current).ToList();
                }
                catch
                {
                    continue;
                }

                foreach (string directory in directories)
                {
                    string name = Path.GetFileName(directory);
                    if (SkippedFolders.Any(skip => string.Equals(skip, name, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    pending.Push(directory);
                }
            }
        }

        private static bool ProjectReferencesProject(string projectPath, string targetProjectPath)
        {
            string normalizedTarget = NormalizePath(targetProjectPath);
            if (string.IsNullOrEmpty(normalizedTarget))
                return false;

            return ProjectReferencesProject(projectPath, normalizedTarget,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static bool ProjectReferencesProject(string projectPath, string normalizedTarget,
            HashSet<string> visited)
        {
            string normalizedProject = NormalizePath(projectPath);
            if (string.IsNullOrEmpty(normalizedProject) || !visited.Add(normalizedProject))
                return false;

            foreach (string referencedProject in ReadProjectReferenceFiles(projectPath))
            {
                string normalizedReference = NormalizePath(referencedProject);
                if (string.Equals(normalizedReference, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (ProjectReferencesProject(referencedProject, normalizedTarget, visited))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> ReadProjectReferenceFiles(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                yield break;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
                yield break;

            XDocument document;
            try
            {
                document = XDocument.Load(projectPath);
            }
            catch
            {
                yield break;
            }

            foreach (XElement projectReference in FindProjectReferenceElements(document))
            {
                string include = projectReference.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include) || include.Contains("$"))
                    continue;

                string referencedProject = ResolveProjectReferencePath(projectDirectory, include);
                if (string.IsNullOrEmpty(referencedProject))
                    continue;

                if (IsProjectFile(referencedProject))
                    yield return referencedProject;
            }
        }

        private static IEnumerable<XElement> FindProjectReferenceElements(XDocument document)
        {
            if (document?.Root == null)
                return Enumerable.Empty<XElement>();

            return document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "ProjectReference",
                    StringComparison.Ordinal));
        }

        private static XElement FindProjectReferenceItemGroup(XDocument document)
        {
            return document?.Root?
                .Elements()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "ItemGroup", StringComparison.Ordinal) &&
                    element.Elements().Any(child => string.Equals(child.Name.LocalName, "ProjectReference",
                        StringComparison.Ordinal)));
        }

        private static XElement CreateProjectReferenceItemGroup(XDocument document)
        {
            XNamespace ns = document.Root.GetDefaultNamespace();
            var itemGroup = new XElement(ns + "ItemGroup", new XText(Environment.NewLine + "    "));
            document.Root.Add(
                new XText(Environment.NewLine + "  "),
                itemGroup,
                new XText(Environment.NewLine));
            return itemGroup;
        }

        private static void AddProjectReferenceElement(XElement itemGroup, string includePath)
        {
            XNamespace ns = itemGroup.Name.Namespace;
            string elementIndent = DetectElementIndent(itemGroup);
            string closingIndent = DetectClosingIndent(itemGroup);

            XNode trailingWhitespace = itemGroup.Nodes()
                .LastOrDefault(node => node is XText text && string.IsNullOrWhiteSpace(text.Value));
            trailingWhitespace?.Remove();

            var projectReference = new XElement(ns + "ProjectReference",
                new XAttribute("Include", includePath));
            itemGroup.Add(new XText(elementIndent), projectReference, new XText(closingIndent));
        }

        private static void RemoveElementWithLeadingWhitespace(XElement element)
        {
            XNode leadingWhitespace = element.PreviousNode;
            if (leadingWhitespace is XText text &&
                string.IsNullOrWhiteSpace(text.Value) &&
                text.Value.Contains("\n"))
            {
                leadingWhitespace.Remove();
            }

            element.Remove();
        }

        private static void RemoveItemGroupIfEmpty(XElement itemGroup)
        {
            if (itemGroup == null || itemGroup.Elements().Any())
                return;

            bool hasContent = itemGroup.Nodes()
                .OfType<XText>()
                .Any(text => !string.IsNullOrWhiteSpace(text.Value));
            if (hasContent)
                return;

            XNode leadingWhitespace = itemGroup.PreviousNode;
            if (leadingWhitespace is XText text &&
                string.IsNullOrWhiteSpace(text.Value) &&
                text.Value.Contains("\n"))
            {
                leadingWhitespace.Remove();
            }

            itemGroup.Remove();
        }

        private static string ResolveProjectReferencePath(string projectDirectory, string includePath)
        {
            if (string.IsNullOrWhiteSpace(projectDirectory) ||
                string.IsNullOrWhiteSpace(includePath) ||
                includePath.Contains("$"))
            {
                return string.Empty;
            }

            try
            {
                return Path.IsPathRooted(includePath)
                    ? Path.GetFullPath(includePath)
                    : Path.GetFullPath(Path.Combine(projectDirectory, includePath));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DetectElementIndent(XElement itemGroup)
        {
            foreach (XNode node in itemGroup.Nodes())
            {
                if (node is XElement element &&
                    element.PreviousNode is XText previousText &&
                    previousText.Value.Contains("\n"))
                {
                    return previousText.Value;
                }
            }

            return Environment.NewLine + "    ";
        }

        private static string DetectClosingIndent(XElement itemGroup)
        {
            XNode lastNode = itemGroup.Nodes().LastOrDefault();
            if (lastNode is XText text && string.IsNullOrWhiteSpace(text.Value) && text.Value.Contains("\n"))
                return text.Value;

            return Environment.NewLine + "  ";
        }

        private static bool IsProjectFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSolutionFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".sln", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return string.IsNullOrWhiteSpace(path)
                    ? string.Empty
                    : Path.GetFullPath(path)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path ?? string.Empty;
            }
        }
    }
}
