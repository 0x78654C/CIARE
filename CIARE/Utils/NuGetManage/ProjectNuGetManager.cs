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
        private static readonly string[] SdkImplicitUsingNamespaces =
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks"
        };

        private static readonly string[] AspNetCoreImplicitUsingNamespaces =
        {
            "System.Net.Http.Json",
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging"
        };

        private static readonly string[] WindowsFormsImplicitUsingNamespaces =
        {
            "System.Drawing",
            "System.Windows.Forms"
        };

        private static readonly string[] WpfImplicitUsingNamespaces =
        {
            "System.Windows",
            "System.Windows.Controls",
            "System.Windows.Data",
            "System.Windows.Documents",
            "System.Windows.Media",
            "System.Windows.Media.Imaging",
            "System.Windows.Navigation",
            "System.Windows.Shapes"
        };

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
            var packagesRequiredByOtherPackages = ReadPackagesRequiredByOtherPackages(assetsPath);
            foreach (var package in packages)
            {
                if (packagesRequiredByOtherPackages.Contains(package.Name))
                    continue;

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

        public static List<string> GetAssemblyReferencePaths(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return new List<string>();

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
                return new List<string>();

            try
            {
                var document = XDocument.Load(projectPath);
                return document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "Reference",
                        StringComparison.Ordinal))
                    .SelectMany(reference => ResolveAssemblyReferencePaths(reference, projectDirectory))
                    .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static List<string> GetFrameworkReferencePaths(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return new List<string>();

            var projectFrameworkReferences = ReadFrameworkReferencesFromProjectFile(projectPath);
            var frameworkReferences = new HashSet<string>(projectFrameworkReferences,
                StringComparer.OrdinalIgnoreCase);
            string assetsPath = GetProjectAssetsPath(projectPath);
            frameworkReferences.UnionWith(ReadFrameworkReferencesFromAssets(assetsPath));

            if (frameworkReferences.Count == 0)
                return new List<string>();

            string targetFramework = GetProjectTargetFramework(projectPath);
            if (string.IsNullOrWhiteSpace(targetFramework))
                targetFramework = ReadTargetFrameworkFromAssets(assetsPath);

            bool projectUsesWindowsDesktop = projectFrameworkReferences.Any(IsWindowsDesktopFrameworkReference);
            var paths = new List<string>();
            foreach (string frameworkReference in frameworkReferences)
            {
                if (IsWindowsDesktopFrameworkReference(frameworkReference) &&
                    (!TargetFrameworkTargetsWindows(targetFramework) || !projectUsesWindowsDesktop))
                {
                    continue;
                }

                paths.AddRange(ResolveFrameworkReferencePaths(frameworkReference, targetFramework));
            }

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> GetImplicitUsingNamespaces(string projectPath)
        {
            if (!IsProjectFile(projectPath) || !ProjectHasImplicitUsingsEnabled(projectPath))
                return new List<string>();

            var namespaces = new List<string>();
            namespaces.AddRange(SdkImplicitUsingNamespaces);

            var projectFrameworkReferences = ReadFrameworkReferencesFromProjectFile(projectPath);
            var frameworkReferences = new HashSet<string>(projectFrameworkReferences,
                StringComparer.OrdinalIgnoreCase);
            string assetsPath = GetProjectAssetsPath(projectPath);
            frameworkReferences.UnionWith(ReadFrameworkReferencesFromAssets(assetsPath));

            if (frameworkReferences.Any(IsAspNetCoreFrameworkReference) ||
                ProjectSdkContains(projectPath, "Microsoft.NET.Sdk.Web"))
            {
                namespaces.AddRange(AspNetCoreImplicitUsingNamespaces);
            }

            string targetFramework = GetProjectTargetFramework(projectPath);
            if (string.IsNullOrWhiteSpace(targetFramework))
                targetFramework = ReadTargetFrameworkFromAssets(assetsPath);
            bool targetsWindows = TargetFrameworkTargetsWindows(targetFramework);

            if (targetsWindows &&
                (ProjectUsesWindowsForms(projectPath) ||
                projectFrameworkReferences.Any(IsWindowsFormsFrameworkReference)))
            {
                namespaces.AddRange(WindowsFormsImplicitUsingNamespaces);
            }

            if (targetsWindows &&
                (ProjectUsesWpf(projectPath) ||
                projectFrameworkReferences.Any(IsWpfFrameworkReference)))
            {
                namespaces.AddRange(WpfImplicitUsingNamespaces);
            }

            return namespaces
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public static string GetProjectTargetFramework(string projectPath)
        {
            if (!IsProjectFile(projectPath))
                return string.Empty;

            try
            {
                var document = XDocument.Load(projectPath);
                string targetFramework = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFramework",
                        StringComparison.Ordinal))?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(targetFramework))
                    return targetFramework;

                string targetFrameworks = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFrameworks",
                        StringComparison.Ordinal))?.Value;
                targetFramework = SplitTargetFrameworks(targetFrameworks).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(targetFramework))
                    return targetFramework;

                string targetFrameworkVersion = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFrameworkVersion",
                        StringComparison.Ordinal))?.Value;
                return NormalizeNetFrameworkVersion(targetFrameworkVersion);
            }
            catch
            {
                return string.Empty;
            }
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

        private static IEnumerable<string> ResolveAssemblyReferencePaths(XElement reference,
            string projectDirectory)
        {
            foreach (string hintPath in reference.Elements()
                .Where(element => string.Equals(element.Name.LocalName, "HintPath",
                    StringComparison.Ordinal))
                .Select(element => element.Value))
            {
                string resolvedPath = ResolveProjectReferencePath(hintPath, projectDirectory);
                if (!string.IsNullOrWhiteSpace(resolvedPath))
                    yield return resolvedPath;
            }

            string include = reference.Attribute("Include")?.Value;
            if (LooksLikeAssemblyPath(include))
            {
                string resolvedPath = ResolveProjectReferencePath(include, projectDirectory);
                if (!string.IsNullOrWhiteSpace(resolvedPath))
                    yield return resolvedPath;
            }
        }

        private static bool LooksLikeAssemblyPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveProjectReferencePath(string path, string projectDirectory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(projectDirectory))
                return string.Empty;

            try
            {
                string resolvedPath = path.Trim().Trim('"');
                resolvedPath = resolvedPath.Replace("$(MSBuildProjectDirectory)", projectDirectory);
                resolvedPath = resolvedPath.Replace("$(MSBuildThisFileDirectory)",
                    EnsureTrailingDirectorySeparator(projectDirectory));
                resolvedPath = resolvedPath.Replace("$(ProjectDir)",
                    EnsureTrailingDirectorySeparator(projectDirectory));
                resolvedPath = Environment.ExpandEnvironmentVariables(resolvedPath);

                if (resolvedPath.Contains("$("))
                    return string.Empty;

                return Path.IsPathRooted(resolvedPath)
                    ? Path.GetFullPath(resolvedPath)
                    : Path.GetFullPath(Path.Combine(projectDirectory, resolvedPath));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path) ||
                path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static HashSet<string> ReadFrameworkReferencesFromProjectFile(string projectPath)
        {
            var frameworkReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!IsProjectFile(projectPath))
                return frameworkReferences;

            try
            {
                var document = XDocument.Load(projectPath);
                string sdkList = document.Root?.Attribute("Sdk")?.Value;
                if (SdkListContains(sdkList, "Microsoft.NET.Sdk.Web"))
                    frameworkReferences.Add("Microsoft.AspNetCore.App");
                if (SdkListContains(sdkList, "Microsoft.NET.Sdk.WindowsDesktop"))
                    frameworkReferences.Add("Microsoft.WindowsDesktop.App");

                foreach (var sdkElement in document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "Sdk", StringComparison.Ordinal)))
                {
                    string sdkName = sdkElement.Attribute("Name")?.Value ?? sdkElement.Attribute("Sdk")?.Value;
                    if (SdkListContains(sdkName, "Microsoft.NET.Sdk.Web"))
                        frameworkReferences.Add("Microsoft.AspNetCore.App");
                    if (SdkListContains(sdkName, "Microsoft.NET.Sdk.WindowsDesktop"))
                        frameworkReferences.Add("Microsoft.WindowsDesktop.App");
                }

                if (ProjectUsesWindowsForms(document))
                    frameworkReferences.Add("Microsoft.WindowsDesktop.App.WindowsForms");
                if (ProjectUsesWpf(document))
                    frameworkReferences.Add("Microsoft.WindowsDesktop.App.WPF");
                if (ProjectReferencesWindowsDesktopAssemblies(document))
                    frameworkReferences.Add("Microsoft.WindowsDesktop.App");

                foreach (var frameworkReference in document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "FrameworkReference",
                        StringComparison.Ordinal)))
                {
                    string name = frameworkReference.Attribute("Include")?.Value ??
                        frameworkReference.Attribute("Update")?.Value;
                    if (!string.IsNullOrWhiteSpace(name))
                        frameworkReferences.Add(name.Trim());
                }
            }
            catch
            {
            }

            return frameworkReferences;
        }

        private static HashSet<string> ReadFrameworkReferencesFromAssets(string assetsPath)
        {
            var frameworkReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(assetsPath) || !File.Exists(assetsPath))
                return frameworkReferences;

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
                var root = document.RootElement;
                if (!root.TryGetProperty("project", out var project) ||
                    !project.TryGetProperty("frameworks", out var frameworks))
                {
                    return frameworkReferences;
                }

                foreach (var framework in frameworks.EnumerateObject())
                {
                    if (!framework.Value.TryGetProperty("frameworkReferences", out var references))
                        continue;

                    foreach (var reference in references.EnumerateObject())
                    {
                        if (!string.IsNullOrWhiteSpace(reference.Name))
                            frameworkReferences.Add(reference.Name);
                    }
                }
            }
            catch
            {
            }

            return frameworkReferences;
        }

        private static IEnumerable<string> ResolveFrameworkReferencePaths(string frameworkReference,
            string targetFramework)
        {
            string packName = GetFrameworkReferencePackName(frameworkReference);
            string sharedName = GetSharedFrameworkName(frameworkReference);
            string refTargetFramework = GetReferenceTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(packName) ||
                string.IsNullOrEmpty(sharedName) ||
                string.IsNullOrEmpty(refTargetFramework))
            {
                return Enumerable.Empty<string>();
            }

            if (IsNetFrameworkTargetFramework(refTargetFramework) &&
                IsWindowsDesktopFrameworkReference(frameworkReference))
            {
                return ResolveNetFrameworkReferencePaths(refTargetFramework);
            }

            string versionPrefix = GetVersionPrefixFromTargetFramework(refTargetFramework);
            if (string.IsNullOrEmpty(versionPrefix))
                return Enumerable.Empty<string>();

            foreach (string dotnetRoot in GetDotnetRootCandidates())
            {
                string packRoot = Path.Combine(dotnetRoot, "packs", packName);
                string packVersionDirectory = FindBestFrameworkVersionDirectory(packRoot, versionPrefix,
                    versionDirectory => Directory.Exists(Path.Combine(versionDirectory, "ref",
                        refTargetFramework)));
                if (!string.IsNullOrEmpty(packVersionDirectory))
                {
                    string refDirectory = Path.Combine(packVersionDirectory, "ref", refTargetFramework);
                    try
                    {
                        return Directory.EnumerateFiles(refDirectory, "*.dll").ToList();
                    }
                    catch
                    {
                    }
                }
            }

            foreach (string dotnetRoot in GetDotnetRootCandidates())
            {
                string sharedRoot = Path.Combine(dotnetRoot, "shared", sharedName);
                string sharedVersionDirectory = FindBestFrameworkVersionDirectory(sharedRoot, versionPrefix,
                    Directory.Exists);
                if (!string.IsNullOrEmpty(sharedVersionDirectory))
                {
                    try
                    {
                        return Directory.EnumerateFiles(sharedVersionDirectory, "*.dll").ToList();
                    }
                    catch
                    {
                    }
                }
            }

            return Enumerable.Empty<string>();
        }

        private static string GetFrameworkReferencePackName(string frameworkReference)
        {
            if (IsAspNetCoreFrameworkReference(frameworkReference))
                return "Microsoft.AspNetCore.App.Ref";

            if (!string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.StartsWith("Microsoft.WindowsDesktop.App",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Microsoft.WindowsDesktop.App.Ref";
            }

            return string.Empty;
        }

        private static string GetSharedFrameworkName(string frameworkReference)
        {
            if (IsAspNetCoreFrameworkReference(frameworkReference))
                return "Microsoft.AspNetCore.App";

            if (!string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.StartsWith("Microsoft.WindowsDesktop.App",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Microsoft.WindowsDesktop.App";
            }

            return string.Empty;
        }

        private static bool IsAspNetCoreFrameworkReference(string frameworkReference)
        {
            return !string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.StartsWith("Microsoft.AspNetCore.App",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWindowsDesktopFrameworkReference(string frameworkReference)
        {
            return !string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.StartsWith("Microsoft.WindowsDesktop.App",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWindowsFormsFrameworkReference(string frameworkReference)
        {
            return !string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.IndexOf("WindowsForms", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsWpfFrameworkReference(string frameworkReference)
        {
            return !string.IsNullOrWhiteSpace(frameworkReference) &&
                frameworkReference.IndexOf("WPF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TargetFrameworkTargetsWindows(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
                return false;

            string normalized = NormalizeTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(normalized))
                normalized = targetFramework.Trim();

            if (normalized.IndexOf("-windows", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string version = normalized.Substring(3);
            int digitCount = 0;
            while (digitCount < version.Length && char.IsDigit(version[digitCount]))
                digitCount++;

            if (digitCount == 0)
                return false;

            return IsNetFrameworkTargetFramework(normalized);
        }

        private static bool IsNetFrameworkTargetFramework(string targetFramework)
        {
            string normalized = NormalizeTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(normalized))
                normalized = targetFramework?.Trim() ?? string.Empty;

            if (!normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string version = normalized.Substring(3);
            if (version.Length == 0)
                return false;

            return version[0] >= '1' && version[0] <= '4' &&
                (version.IndexOf('.') < 0 ||
                version.StartsWith("1.", StringComparison.Ordinal) ||
                version.StartsWith("2.", StringComparison.Ordinal) ||
                version.StartsWith("3.", StringComparison.Ordinal) ||
                version.StartsWith("4.", StringComparison.Ordinal));
        }

        private static IEnumerable<string> ResolveNetFrameworkReferencePaths(string targetFramework)
        {
            string frameworkFolderName = GetNetFrameworkReferenceFolderName(targetFramework);
            if (string.IsNullOrEmpty(frameworkFolderName))
                return Enumerable.Empty<string>();

            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            foreach (string root in roots.Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string referenceDirectory = Path.Combine(root, "Reference Assemblies", "Microsoft",
                    "Framework", ".NETFramework", frameworkFolderName);
                if (!Directory.Exists(referenceDirectory))
                    continue;

                try
                {
                    return Directory.EnumerateFiles(referenceDirectory, "*.dll").ToList();
                }
                catch
                {
                }
            }

            return Enumerable.Empty<string>();
        }

        private static string GetNetFrameworkReferenceFolderName(string targetFramework)
        {
            string normalized = NormalizeTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(normalized))
                normalized = targetFramework?.Trim() ?? string.Empty;

            if (!normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string version = normalized.Substring(3);
            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                version = version.Substring(1);

            if (version.Length == 0)
                return string.Empty;

            if (version.IndexOf('.') >= 0)
                return "v" + version;

            if (version.Length == 2)
                return "v" + version[0] + "." + version[1];

            if (version.Length == 3)
                return "v" + version[0] + "." + version[1] + "." + version[2];

            return "v" + version;
        }

        private static string ReadTargetFrameworkFromAssets(string assetsPath)
        {
            if (string.IsNullOrWhiteSpace(assetsPath) || !File.Exists(assetsPath))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
                var root = document.RootElement;
                if (root.TryGetProperty("project", out var project) &&
                    project.TryGetProperty("frameworks", out var frameworks))
                {
                    foreach (var framework in frameworks.EnumerateObject())
                    {
                        string normalized = NormalizeTargetFramework(framework.Name);
                        if (!string.IsNullOrEmpty(normalized))
                            return normalized;
                    }
                }

                if (root.TryGetProperty("targets", out var targets))
                {
                    foreach (var target in targets.EnumerateObject())
                    {
                        string normalized = NormalizeTargetFramework(target.Name);
                        if (!string.IsNullOrEmpty(normalized))
                            return normalized;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string NormalizeTargetFramework(string frameworkName)
        {
            if (string.IsNullOrWhiteSpace(frameworkName))
                return string.Empty;

            string name = frameworkName.Trim();
            int slashIndex = name.IndexOf('/');
            if (slashIndex >= 0)
                name = name.Substring(0, slashIndex);

            if (name.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return name;

            const string netCoreAppPrefix = ".NETCoreApp,Version=v";
            if (name.StartsWith(netCoreAppPrefix, StringComparison.OrdinalIgnoreCase))
                return "net" + name.Substring(netCoreAppPrefix.Length);

            const string netFrameworkPrefix = ".NETFramework,Version=v";
            if (name.StartsWith(netFrameworkPrefix, StringComparison.OrdinalIgnoreCase))
                return NormalizeNetFrameworkVersion(name.Substring(netFrameworkPrefix.Length));

            return string.Empty;
        }

        private static string NormalizeNetFrameworkVersion(string targetFrameworkVersion)
        {
            if (string.IsNullOrWhiteSpace(targetFrameworkVersion))
                return string.Empty;

            string version = targetFrameworkVersion.Trim();
            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                version = version.Substring(1);

            return string.IsNullOrWhiteSpace(version)
                ? string.Empty
                : "net" + version.Replace(".", string.Empty);
        }

        private static string GetReferenceTargetFramework(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
                return string.Empty;

            string normalized = NormalizeTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(normalized))
                normalized = targetFramework.Trim();

            int dashIndex = normalized.IndexOf('-');
            if (dashIndex > 0)
                normalized = normalized.Substring(0, dashIndex);

            return normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : string.Empty;
        }

        private static string GetVersionPrefixFromTargetFramework(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework) ||
                !targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string version = targetFramework.Substring(3);
            int dashIndex = version.IndexOf('-');
            if (dashIndex > 0)
                version = version.Substring(0, dashIndex);

            return version;
        }

        private static IEnumerable<string> GetDotnetRootCandidates()
        {
            var roots = new List<string>();
            AddDirectoryCandidate(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT"));
            AddDirectoryCandidate(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)"));

            try
            {
                string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
                string dotnetRoot = Directory.GetParent(runtimeDirectory)?.Parent?.Parent?.FullName;
                AddDirectoryCandidate(roots, dotnetRoot);
            }
            catch
            {
            }

            AddDirectoryCandidate(roots, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"));
            AddDirectoryCandidate(roots, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet"));

            return roots.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static void AddDirectoryCandidate(List<string> candidates, string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                candidates.Add(path);
        }

        private static string FindBestFrameworkVersionDirectory(string frameworkRoot, string versionPrefix,
            Func<string, bool> predicate)
        {
            if (string.IsNullOrWhiteSpace(frameworkRoot) || !Directory.Exists(frameworkRoot))
                return string.Empty;

            try
            {
                return Directory.EnumerateDirectories(frameworkRoot)
                    .Where(directory => VersionMatchesPrefix(Path.GetFileName(directory), versionPrefix))
                    .Where(directory => predicate == null || predicate(directory))
                    .OrderByDescending(directory => ParseFrameworkVersion(Path.GetFileName(directory)))
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool VersionMatchesPrefix(string version, string versionPrefix)
        {
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(versionPrefix))
                return false;

            return string.Equals(version, versionPrefix, StringComparison.OrdinalIgnoreCase) ||
                version.StartsWith(versionPrefix + ".", StringComparison.OrdinalIgnoreCase);
        }

        private static Version ParseFrameworkVersion(string version)
        {
            return Version.TryParse(version, out var parsed)
                ? parsed
                : new Version(0, 0);
        }

        private static bool ProjectHasImplicitUsingsEnabled(string projectPath)
        {
            try
            {
                var document = XDocument.Load(projectPath);
                string value = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "ImplicitUsings",
                        StringComparison.Ordinal))?.Value?.Trim();

                return string.Equals(value, "enable", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool ProjectSdkContains(string projectPath, string sdkName)
        {
            try
            {
                var document = XDocument.Load(projectPath);
                if (SdkListContains(document.Root?.Attribute("Sdk")?.Value, sdkName))
                    return true;

                return document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "Sdk", StringComparison.Ordinal))
                    .Any(element => SdkListContains(
                        element.Attribute("Name")?.Value ?? element.Attribute("Sdk")?.Value,
                        sdkName));
            }
            catch
            {
                return false;
            }
        }

        private static bool SdkListContains(string sdkList, string sdkName)
        {
            if (string.IsNullOrWhiteSpace(sdkList) || string.IsNullOrWhiteSpace(sdkName))
                return false;

            return sdkList
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(sdk => sdk.Trim().StartsWith(sdkName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ProjectUsesWindowsForms(string projectPath)
        {
            try
            {
                return ProjectUsesWindowsForms(XDocument.Load(projectPath));
            }
            catch
            {
                return false;
            }
        }

        private static bool ProjectUsesWindowsForms(XDocument document)
        {
            return ProjectPropertyIsTrue(document, "UseWindowsForms");
        }

        private static bool ProjectUsesWpf(string projectPath)
        {
            try
            {
                return ProjectUsesWpf(XDocument.Load(projectPath));
            }
            catch
            {
                return false;
            }
        }

        private static bool ProjectUsesWpf(XDocument document)
        {
            return ProjectPropertyIsTrue(document, "UseWPF");
        }

        private static bool ProjectReferencesWindowsDesktopAssemblies(XDocument document)
        {
            return document?.Root != null &&
                document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "Reference",
                        StringComparison.Ordinal))
                    .Select(element => element.Attribute("Include")?.Value)
                    .Any(IsWindowsDesktopAssemblyReference);
        }

        private static bool IsWindowsDesktopAssemblyReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return false;

            string assemblyName = reference.Split(',')[0].Trim();
            return string.Equals(assemblyName, "PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "PresentationUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "ReachFramework", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "System.Xaml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "System.Windows.Forms", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "WindowsBase", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ProjectPropertyIsTrue(XDocument document, string propertyName)
        {
            if (document?.Root == null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            string value = document.Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName,
                    StringComparison.OrdinalIgnoreCase))?.Value?.Trim();

            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "enable", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> SplitTargetFrameworks(string targetFrameworks)
        {
            if (string.IsNullOrWhiteSpace(targetFrameworks))
                return Enumerable.Empty<string>();

            return targetFrameworks
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(framework => framework.Trim())
                .Where(framework => framework.Length > 0);
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

        private static HashSet<string> ReadPackagesRequiredByOtherPackages(string assetsPath)
        {
            var packageDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
                if (!document.RootElement.TryGetProperty("targets", out var targets))
                    return packageDependencies;

                foreach (var target in targets.EnumerateObject())
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        string[] libraryParts = library.Name.Split('/');
                        if (libraryParts.Length != 2 ||
                            !library.Value.TryGetProperty("type", out var libraryType) ||
                            libraryType.ValueKind != JsonValueKind.String ||
                            !string.Equals(libraryType.GetString(), "package", StringComparison.OrdinalIgnoreCase) ||
                            !library.Value.TryGetProperty("dependencies", out var dependencies) ||
                            dependencies.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        foreach (var dependency in dependencies.EnumerateObject())
                        {
                            if (!string.Equals(libraryParts[0], dependency.Name,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                packageDependencies.Add(dependency.Name);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return packageDependencies;
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
