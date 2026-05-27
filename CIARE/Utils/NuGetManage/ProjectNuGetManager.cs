using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml.Linq;
using CIARE.Utils;

namespace CIARE.Utils.NuGetManage
{
    [SupportedOSPlatform("windows")]
    public sealed class ProjectNuGetPackageReference
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
    }

    [SupportedOSPlatform("windows")]
    public static class ProjectNuGetManager
    {
        public static List<ProjectNuGetPackageReference> GetPackageReferences(string projectPath)
        {
            var packages = new List<ProjectNuGetPackageReference>();
            if (!IsProjectFile(projectPath))
                return packages;

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                foreach (var packageReference in FindPackageReferenceElements(document))
                {
                    string name = GetPackageName(packageReference);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    string version = GetPackageVersion(packageReference);
                    if (string.IsNullOrWhiteSpace(version))
                        version = ResolveCentralPackageVersion(projectPath, name);

                    packages.Add(new ProjectNuGetPackageReference
                    {
                        Name = name,
                        Version = string.IsNullOrWhiteSpace(version) ? "(not specified)" : version,
                        ProjectPath = projectPath
                    });
                }
            }
            catch
            {
            }

            return packages
                .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool HasPackageReference(string projectPath, string packageName)
        {
            if (!IsProjectFile(projectPath) || string.IsNullOrWhiteSpace(packageName))
                return false;

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                return FindPackageReferenceElements(document)
                    .Any(packageReference => string.Equals(GetPackageName(packageReference), packageName,
                        StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public static bool AddPackageReference(string projectPath, string packageName, string version, out string message)
        {
            message = string.Empty;
            if (!IsProjectFile(projectPath))
            {
                message = "No valid .csproj file was found for the opened explorer project.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(version))
            {
                message = "The NuGet package name or version is empty.";
                return false;
            }

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                if (FindPackageReferenceElements(document)
                    .Any(packageReference => string.Equals(GetPackageName(packageReference), packageName,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    message = $"NuGet package {packageName} is already installed in {Path.GetFileName(projectPath)}.";
                    return false;
                }

                XElement itemGroup = FindPackageItemGroup(document) ?? CreatePackageItemGroup(document);
                AddPackageReferenceElement(itemGroup, packageName, version);
                document.Save(projectPath, SaveOptions.DisableFormatting);
                message = $"NuGet package {packageName} {version} was added to {Path.GetFileName(projectPath)}.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool RemovePackageReference(string projectPath, string packageName, out string message)
        {
            message = string.Empty;
            if (!IsProjectFile(projectPath))
            {
                message = "No valid .csproj file was found for the opened explorer project.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(packageName))
            {
                message = "The NuGet package name is empty.";
                return false;
            }

            try
            {
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                XElement packageReference = FindPackageReferenceElements(document)
                    .FirstOrDefault(element => string.Equals(GetPackageName(element), packageName,
                        StringComparison.OrdinalIgnoreCase));

                if (packageReference == null)
                {
                    message = $"NuGet package {packageName} is not installed in {Path.GetFileName(projectPath)}.";
                    return false;
                }

                XElement itemGroup = packageReference.Parent;
                RemoveElementWithLeadingWhitespace(packageReference);
                RemoveItemGroupIfEmpty(itemGroup);
                document.Save(projectPath, SaveOptions.DisableFormatting);
                message = $"NuGet package {packageName} was removed from {Path.GetFileName(projectPath)}.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static ProcessRunResult RestoreProject(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return new ProcessRunResult(-1, "No valid .csproj file was found for the opened explorer project.");

            try
            {
                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
                    return new ProcessRunResult(-1, "The project directory was not found.");

                var processRun = new ProcessRun("dotnet",
                    "restore " + QuoteArgument(projectPath) + " --force-evaluate",
                    projectDirectory);
                return processRun.RunWithResult();
            }
            catch (Exception ex)
            {
                return new ProcessRunResult(-1, ex.GetBaseException()?.Message ?? ex.Message);
            }
        }

        public static List<string> GetCompileReferencePaths(string projectPath)
        {
            string assetsPath = GetProjectAssetsPath(projectPath);
            if (string.IsNullOrEmpty(assetsPath))
                return new List<string>();

            return ReadCompileReferencesFromAssets(assetsPath)
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetProjectAssetsPath(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return string.Empty;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
                return string.Empty;

            string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
            return File.Exists(assetsPath) ? assetsPath : string.Empty;
        }

        private static bool IsProjectFile(string projectPath)
        {
            return !string.IsNullOrWhiteSpace(projectPath) &&
                File.Exists(projectPath) &&
                string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static IEnumerable<XElement> FindPackageReferenceElements(XDocument document)
        {
            if (document?.Root == null)
                return Enumerable.Empty<XElement>();

            return document.Root
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "PackageReference",
                    StringComparison.Ordinal));
        }

        private static XElement FindPackageItemGroup(XDocument document)
        {
            return document?.Root?
                .Elements()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "ItemGroup", StringComparison.Ordinal) &&
                    element.Elements().Any(child => string.Equals(child.Name.LocalName, "PackageReference",
                        StringComparison.Ordinal)));
        }

        private static XElement CreatePackageItemGroup(XDocument document)
        {
            XNamespace ns = document.Root.GetDefaultNamespace();
            var itemGroup = new XElement(ns + "ItemGroup",
                new XText(Environment.NewLine + "    "));

            document.Root.Add(
                new XText(Environment.NewLine + "  "),
                itemGroup,
                new XText(Environment.NewLine));

            return itemGroup;
        }

        private static void AddPackageReferenceElement(XElement itemGroup, string packageName, string version)
        {
            XNamespace ns = itemGroup.Name.Namespace;
            string elementIndent = DetectElementIndent(itemGroup);
            string closingIndent = DetectClosingIndent(itemGroup);

            XNode trailingWhitespace = itemGroup.Nodes()
                .LastOrDefault(node => node is XText text && string.IsNullOrWhiteSpace(text.Value));
            trailingWhitespace?.Remove();

            var packageReference = new XElement(ns + "PackageReference",
                new XAttribute("Include", packageName.Trim()),
                new XAttribute("Version", version.Trim()));

            itemGroup.Add(new XText(elementIndent), packageReference, new XText(closingIndent));
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

        private static string DetectElementIndent(XElement itemGroup)
        {
            foreach (var node in itemGroup.Nodes())
            {
                if (node is XElement element &&
                    string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal) &&
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

        private static string GetPackageName(XElement packageReference)
        {
            return ((string)packageReference.Attribute("Include") ??
                    (string)packageReference.Attribute("Update") ??
                    string.Empty).Trim();
        }

        private static string GetPackageVersion(XElement packageReference)
        {
            string version = ((string)packageReference.Attribute("Version") ??
                              packageReference.Elements()
                                  .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Version",
                                      StringComparison.Ordinal))?.Value ??
                              string.Empty).Trim();
            return version;
        }

        private static string ResolveCentralPackageVersion(string projectPath, string packageName)
        {
            try
            {
                string folder = Path.GetDirectoryName(projectPath);
                while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    string propsPath = Path.Combine(folder, "Directory.Packages.props");
                    string version = ReadCentralPackageVersion(propsPath, packageName);
                    if (!string.IsNullOrWhiteSpace(version))
                        return version;

                    string parent = Path.GetDirectoryName(folder);
                    if (string.IsNullOrEmpty(parent) || string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase))
                        break;

                    folder = parent;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string ReadCentralPackageVersion(string propsPath, string packageName)
        {
            if (!File.Exists(propsPath))
                return string.Empty;

            try
            {
                var document = XDocument.Load(propsPath, LoadOptions.PreserveWhitespace);
                foreach (var packageVersion in document.Root.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "PackageVersion",
                        StringComparison.Ordinal)))
                {
                    string name = ((string)packageVersion.Attribute("Include") ??
                                   (string)packageVersion.Attribute("Update") ??
                                   string.Empty).Trim();
                    if (!string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return ((string)packageVersion.Attribute("Version") ??
                            packageVersion.Elements()
                                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Version",
                                    StringComparison.Ordinal))?.Value ??
                            string.Empty).Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static IEnumerable<string> ReadCompileReferencesFromAssets(string assetsPath)
        {
            var references = new List<string>();
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
                var root = document.RootElement;
                var packageFolders = ReadPackageFolders(root);

                if (!root.TryGetProperty("targets", out var targets))
                    return references;

                foreach (var target in targets.EnumerateObject())
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        if (!library.Name.Contains("/"))
                            continue;

                        if (!library.Value.TryGetProperty("compile", out var compileAssets))
                            continue;

                        foreach (var compileAsset in compileAssets.EnumerateObject())
                        {
                            foreach (var referencePath in ResolvePackageAssetPaths(library.Name,
                                compileAsset.Name, packageFolders))
                            {
                                references.Add(referencePath);
                            }
                        }
                    }
                }
            }
            catch
            {
                return references;
            }

            return references;
        }

        private static List<string> ReadPackageFolders(JsonElement root)
        {
            var packageFolders = new List<string>();
            if (root.TryGetProperty("packageFolders", out var folders))
            {
                foreach (var folder in folders.EnumerateObject())
                {
                    if (!string.IsNullOrWhiteSpace(folder.Name) && Directory.Exists(folder.Name))
                        packageFolders.Add(folder.Name);
                }
            }

            if (packageFolders.Count == 0)
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile))
                {
                    string globalPackages = Path.Combine(userProfile, ".nuget", "packages");
                    if (Directory.Exists(globalPackages))
                        packageFolders.Add(globalPackages);
                }
            }

            return packageFolders;
        }

        private static IEnumerable<string> ResolvePackageAssetPaths(string libraryName, string assetPath,
            List<string> packageFolders)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                assetPath.EndsWith("/_._", StringComparison.OrdinalIgnoreCase) ||
                assetPath.EndsWith("\\_._", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            var libraryParts = libraryName.Split('/');
            if (libraryParts.Length != 2)
                yield break;

            var packageIds = new[] { libraryParts[0], libraryParts[0].ToLowerInvariant() }
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var packageVersions = new[] { libraryParts[1], libraryParts[1].ToLowerInvariant() }
                .Distinct(StringComparer.OrdinalIgnoreCase);
            string normalizedAssetPath = assetPath.Replace('/', Path.DirectorySeparatorChar);

            foreach (var folder in packageFolders)
            {
                foreach (var packageId in packageIds)
                {
                    foreach (var packageVersion in packageVersions)
                    {
                        string referencePath = Path.Combine(folder, packageId, packageVersion, normalizedAssetPath);
                        if (File.Exists(referencePath))
                            yield return referencePath;
                    }
                }
            }
        }
    }
}
