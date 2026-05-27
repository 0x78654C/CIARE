using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CIARE.Utils;
using Mono.Cecil;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace CIARE.Utils.NuGetManage
{
    [SupportedOSPlatform("windows")]
    public sealed class ProjectNuGetPackageReference
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool HasUpdate { get; set; }
        public bool UnusedCheckCompleted { get; set; }
        public bool IsUnused { get; set; }
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

        public static bool UpdatePackageReference(string projectPath, string packageName, string version,
            out string message)
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
                string cleanVersion = version.Trim();
                var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
                XElement packageReference = FindPackageReferenceElements(document)
                    .FirstOrDefault(element => string.Equals(GetPackageName(element), packageName,
                        StringComparison.OrdinalIgnoreCase));

                if (packageReference == null)
                {
                    message = $"NuGet package {packageName} is not installed in {Path.GetFileName(projectPath)}.";
                    return false;
                }

                if (TrySetPackageVersion(packageReference, cleanVersion))
                {
                    document.Save(projectPath, SaveOptions.DisableFormatting);
                    message = $"NuGet package {packageName} was updated to {cleanVersion}.";
                    return true;
                }

                string centralVersionPath = FindCentralPackageVersionFile(projectPath, packageName);
                if (!string.IsNullOrWhiteSpace(centralVersionPath))
                {
                    var centralDocument = XDocument.Load(centralVersionPath, LoadOptions.PreserveWhitespace);
                    XElement packageVersion = FindPackageVersionElement(centralDocument, packageName);
                    if (packageVersion != null)
                    {
                        if (!TrySetPackageVersion(packageVersion, cleanVersion))
                            packageVersion.SetAttributeValue("Version", cleanVersion);

                        centralDocument.Save(centralVersionPath, SaveOptions.DisableFormatting);
                        message = $"NuGet package {packageName} was updated to {cleanVersion}.";
                        return true;
                    }
                }

                packageReference.SetAttributeValue("Version", cleanVersion);
                document.Save(projectPath, SaveOptions.DisableFormatting);
                message = $"NuGet package {packageName} was updated to {cleanVersion}.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static void PopulateLatestPackageVersions(IList<ProjectNuGetPackageReference> packages)
        {
            if (packages == null || packages.Count == 0 || string.IsNullOrWhiteSpace(GlobalVariables.nugetApi))
                return;

            try
            {
                ILogger logger = NullLogger.Instance;
                using var cache = new SourceCacheContext();
                SourceRepository repository = Repository.Factory.GetCoreV3(GlobalVariables.nugetApi);
                FindPackageByIdResource resource =
                    Task.Run(() => repository.GetResourceAsync<FindPackageByIdResource>()).Result;

                foreach (var package in packages)
                {
                    string latestVersion = GetLatestStablePackageVersion(resource, cache, logger, package.Name);
                    package.LatestVersion = latestVersion;
                    package.HasUpdate = IsNewerPackageVersion(package.Version, latestVersion);
                }
            }
            catch
            {
            }
        }

        public static void PopulateUnusedPackageStatus(string projectPath,
            IList<ProjectNuGetPackageReference> packages)
        {
            if (packages == null || packages.Count == 0)
                return;

            foreach (var package in packages)
            {
                package.UnusedCheckCompleted = true;
                package.IsUnused = false;
            }

            if (!IsProjectFile(projectPath))
                return;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            string assetsPath = GetProjectAssetsPath(projectPath);
            if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(assetsPath))
                return;

            var sourceTexts = ReadProjectSourceTexts(projectDirectory);
            if (sourceTexts.Count == 0)
                return;

            var referencesByPackage = ReadCompileReferencesByPackage(assetsPath);
            foreach (var package in packages)
            {
                if (!referencesByPackage.TryGetValue(package.Name, out var referencePaths) ||
                    referencePaths.Count == 0)
                {
                    continue;
                }

                var namespaces = ReadAssemblyNamespaces(referencePaths);
                if (namespaces.Count == 0)
                    continue;

                package.IsUnused = !SourceTextsContainAnyNamespace(sourceTexts, namespaces);
            }
        }

        public static bool IsNewerPackageVersion(string installedVersion, string latestVersion)
        {
            if (!NuGetVersion.TryParse((installedVersion ?? string.Empty).Trim(), out var installed))
                return false;

            if (!NuGetVersion.TryParse((latestVersion ?? string.Empty).Trim(), out var latest))
                return false;

            return latest.CompareTo(installed) > 0;
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

        private static string GetLatestStablePackageVersion(FindPackageByIdResource resource,
            SourceCacheContext cache, ILogger logger, string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                return string.Empty;

            try
            {
                var versions = Task.Run(() => resource.GetAllVersionsAsync(packageName, cache, logger,
                    CancellationToken.None)).Result;
                return versions
                    .Where(version => !version.IsPrerelease)
                    .OrderBy(version => version)
                    .LastOrDefault()
                    ?.ToNormalizedString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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

        private static bool TrySetPackageVersion(XElement packageElement, string version)
        {
            XAttribute versionAttribute = packageElement.Attribute("Version");
            if (versionAttribute != null)
            {
                versionAttribute.Value = version;
                return true;
            }

            XElement versionElement = packageElement.Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Version",
                    StringComparison.Ordinal));
            if (versionElement != null)
            {
                versionElement.Value = version;
                return true;
            }

            return false;
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

        private static string FindCentralPackageVersionFile(string projectPath, string packageName)
        {
            string folder = Path.GetDirectoryName(projectPath);
            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                string propsPath = Path.Combine(folder, "Directory.Packages.props");
                if (File.Exists(propsPath))
                {
                    try
                    {
                        var document = XDocument.Load(propsPath, LoadOptions.PreserveWhitespace);
                        if (FindPackageVersionElement(document, packageName) != null)
                            return propsPath;
                    }
                    catch
                    {
                    }
                }

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase))
                    break;

                folder = parent;
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
                XElement packageVersion = FindPackageVersionElement(document, packageName);
                return packageVersion == null ? string.Empty : GetPackageVersion(packageVersion);
            }
            catch
            {
            }

            return string.Empty;
        }

        private static XElement FindPackageVersionElement(XDocument document, string packageName)
        {
            if (document?.Root == null)
                return null;

            return document.Root.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "PackageVersion",
                    StringComparison.Ordinal))
                .FirstOrDefault(element => string.Equals(GetPackageName(element), packageName,
                    StringComparison.OrdinalIgnoreCase));
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

        private static Dictionary<string, List<string>> ReadCompileReferencesByPackage(string assetsPath)
        {
            var referencesByPackage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
                var root = document.RootElement;
                var packageFolders = ReadPackageFolders(root);

                if (!root.TryGetProperty("targets", out var targets))
                    return referencesByPackage;

                foreach (var target in targets.EnumerateObject())
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        string[] libraryParts = library.Name.Split('/');
                        if (libraryParts.Length != 2)
                            continue;

                        if (!library.Value.TryGetProperty("compile", out var compileAssets))
                            continue;

                        foreach (var compileAsset in compileAssets.EnumerateObject())
                        {
                            foreach (var referencePath in ResolvePackageAssetPaths(library.Name,
                                compileAsset.Name, packageFolders))
                            {
                                if (!referencesByPackage.TryGetValue(libraryParts[0], out var packageReferences))
                                {
                                    packageReferences = new List<string>();
                                    referencesByPackage[libraryParts[0]] = packageReferences;
                                }

                                if (!packageReferences.Contains(referencePath, StringComparer.OrdinalIgnoreCase))
                                    packageReferences.Add(referencePath);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return referencesByPackage;
        }

        private static List<string> ReadProjectSourceTexts(string projectDirectory)
        {
            var sourceTexts = new List<string>();
            try
            {
                foreach (string filePath in Directory.EnumerateFiles(projectDirectory, "*.cs",
                    SearchOption.AllDirectories))
                {
                    if (IsPathInIgnoredBuildDirectory(filePath))
                        continue;

                    try
                    {
                        sourceTexts.Add(File.ReadAllText(filePath));
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return sourceTexts;
        }

        private static bool IsPathInIgnoredBuildDirectory(string path)
        {
            string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string separator = Path.DirectorySeparatorChar.ToString();
            return normalized.IndexOf(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static HashSet<string> ReadAssemblyNamespaces(IEnumerable<string> referencePaths)
        {
            var namespaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (string referencePath in referencePaths)
            {
                try
                {
                    using var assembly = AssemblyDefinition.ReadAssembly(referencePath);
                    foreach (var module in assembly.Modules)
                        CollectTypeNamespaces(module.Types, namespaces);
                }
                catch
                {
                }
            }

            return namespaces;
        }

        private static void CollectTypeNamespaces(IEnumerable<TypeDefinition> types, HashSet<string> namespaces)
        {
            foreach (var type in types)
            {
                if (!string.IsNullOrWhiteSpace(type.Namespace))
                    namespaces.Add(type.Namespace);

                if (type.HasNestedTypes)
                    CollectTypeNamespaces(type.NestedTypes, namespaces);
            }
        }

        private static bool SourceTextsContainAnyNamespace(List<string> sourceTexts, HashSet<string> namespaces)
        {
            foreach (string sourceText in sourceTexts)
            {
                foreach (string namespaceName in namespaces)
                {
                    if (sourceText.Contains(namespaceName, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
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
