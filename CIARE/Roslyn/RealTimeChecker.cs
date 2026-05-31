using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Xml.Linq;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Reflection;
using CIARE.Utils;
using CIARE.Utils.NuGetManage;

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("windows")]
    public static class RealTimeChecker
    {
        private const int DebounceMs = 800;
        private static System.Threading.Timer _debounceTimer;
        private static CancellationTokenSource _cts;
        private static readonly object _lock = new object();

        private static readonly Color ErrorColor = Color.Red;
        private static readonly Color WarningColor = Color.Orange;
        private static readonly HashSet<string> SuppressedDiagnosticIds = new HashSet<string>
        {
            "CS1701",
            "CS1702",
            "CS8632"
        };

        // Cached platform references (never change at runtime).
        private static ImmutableArray<MetadataReference> _platformRefs;
        private static readonly object _refLock = new object();

        // Snapshot of custom refs used for the last build, so we only rebuild when they change.
        private static List<string> _lastCustomRefSnapshot;
        private static List<MetadataReference> _customRefs = new List<MetadataReference>();
        private static List<string> _lastNuGetRefSnapshot;
        private static List<MetadataReference> _nuGetRefs = new List<MetadataReference>();
        private static List<string> _lastFrameworkRefSnapshot;
        private static List<MetadataReference> _frameworkRefs = new List<MetadataReference>();

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

        /// <summary>
        /// Schedule a real-time type check after a short debounce delay.
        /// Resets the timer on every call so it only fires once typing pauses.
        /// </summary>
        public static void ScheduleCheck(string code, TextEditorControl editor, Label statusLabel,
            ListView errorsLV = null, TabPage errorsTabPage = null, Label warningsLabel = null,
            string workspaceFolder = null, string currentFilePath = null, bool useProjectReferences = false)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new System.Threading.Timer(
                    _ => RunCheck(code, editor, statusLabel, errorsLV, errorsTabPage, warningsLabel,
                        workspaceFolder, currentFilePath, useProjectReferences),
                    null, DebounceMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Cancel any pending or running check and clear all markers.
        /// </summary>
        public static void Cancel(TextEditorControl editor = null, Label statusLabel = null,
            ListView errorsLV = null, TabPage errorsTabPage = null, Label warningsLabel = null)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }

            if (editor != null)
            {
                editor.BeginInvoke((Action)(() =>
                {
                    editor.Document.MarkerStrategy.RemoveAll(_ => true);
                    InvalidateEditorTextArea(editor);
                    if (statusLabel != null)
                        statusLabel.Text = string.Empty;
                    if (warningsLabel != null)
                        warningsLabel.Text = string.Empty;
                    ClearErrorsPanel(errorsLV, errorsTabPage);
                }));
            }
        }

        public static void InvalidateReferenceCache()
        {
            lock (_refLock)
            {
                _lastCustomRefSnapshot = null;
                _customRefs = new List<MetadataReference>();
                _lastNuGetRefSnapshot = null;
                _nuGetRefs = new List<MetadataReference>();
                _lastFrameworkRefSnapshot = null;
                _frameworkRefs = new List<MetadataReference>();
            }
        }

        private static void RunCheck(string code, TextEditorControl editor, Label statusLabel,
            ListView errorsLV, TabPage errorsTabPage, Label warningsLabel,
            string workspaceFolder, string currentFilePath, bool useProjectReferences)
        {
            CancellationTokenSource cts;
            lock (_lock)
            {
                var old = _cts;
                old?.Cancel();
                old?.Dispose();
                cts = new CancellationTokenSource();
                _cts = cts;
            }

            Task.Run(() =>
            {
                try
                {
                    if (cts.IsCancellationRequested) return;

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        ClearMarkersAndStatus(editor, statusLabel, warningsLabel);
                        ClearErrorsPanel(errorsLV, errorsTabPage);
                        return;
                    }

                    var diagnostics = GetDiagnostics(code, cts.Token, workspaceFolder, currentFilePath,
                        useProjectReferences);
                    if (cts.IsCancellationRequested) return;

                    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                    var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

                    editor.BeginInvoke((Action)(() =>
                    {
                        if (cts.IsCancellationRequested) return;
                        ApplyMarkers(editor, errors, warnings);
                        UpdateErrorLabel(statusLabel, errors.Count);
                        UpdateWarningLabel(warningsLabel, warnings.Count);
                        UpdateErrorsPanel(errorsLV, errorsTabPage, errors, warnings);
                    }));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested) return;
                    ShowCheckerFailure(editor, statusLabel, warningsLabel, errorsLV, errorsTabPage, ex);
                }
            }, cts.Token);
        }

        private static List<Diagnostic> GetDiagnostics(string code, CancellationToken ct,
            string workspaceFolder = null, string currentFilePath = null, bool useProjectReferences = false)
        {
            var parseOptions = BuildParseOptions(GlobalVariables.Framework);
            string activeFilePath = string.IsNullOrWhiteSpace(currentFilePath) ? "edited.cs" : currentFilePath;
            var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions, path: activeFilePath, cancellationToken: ct);
            var syntaxTrees = new List<SyntaxTree> { syntaxTree };
            bool hasProjectContext = useProjectReferences &&
                HasOpenedProjectContext(workspaceFolder, currentFilePath);
            string projectFile = hasProjectContext
                ? FindNearestProjectFile(currentFilePath, workspaceFolder)
                : string.Empty;
            bool hasAspNetCoreContext = hasProjectContext &&
                ProjectUsesAspNetCore(workspaceFolder, currentFilePath, hasProjectContext);
            if (hasProjectContext)
            {
                var generatedGlobalUsings = BuildProjectGlobalUsingsSyntaxTrees(workspaceFolder,
                    currentFilePath, parseOptions, ct);
                if (generatedGlobalUsings.Count > 0)
                    syntaxTrees.AddRange(generatedGlobalUsings);
                else
                    syntaxTrees.Add(BuildImplicitUsingsSyntaxTree(parseOptions, ct,
                        hasAspNetCoreContext, projectFile));

                syntaxTrees.AddRange(BuildWorkspaceSyntaxTrees(workspaceFolder, currentFilePath, projectFile,
                    parseOptions, ct));
            }

            var outputKind = syntaxTrees.Any(tree => HasTopLevelStatements(tree, ct))
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary;

            try
            {
                return CompileAndFilterDiagnostics(syntaxTrees, syntaxTree,
                    BuildReferences(workspaceFolder, currentFilePath, hasProjectContext), outputKind, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch when (!hasProjectContext)
            {
                return CompileAndFilterDiagnostics(syntaxTrees, syntaxTree,
                    BuildPlatformReferences(), outputKind, ct);
            }
        }

        private static List<Diagnostic> CompileAndFilterDiagnostics(IEnumerable<SyntaxTree> syntaxTrees,
            SyntaxTree activeSyntaxTree, IEnumerable<MetadataReference> references, OutputKind outputKind,
            CancellationToken ct)
        {
            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(
                    outputKind,
                    reportSuppressedDiagnostics: true,
                    allowUnsafe: GlobalVariables.OUnsafeCode,
                    optimizationLevel: OptimizationLevel.Debug,
                    platform: Platform.AnyCpu));

            return compilation.GetDiagnostics(ct)
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Where(d => ShouldReportDiagnostic(d, activeSyntaxTree))
                .ToList();
        }

        private static SyntaxTree BuildImplicitUsingsSyntaxTree(CSharpParseOptions parseOptions, CancellationToken ct,
            bool includeAspNetCoreUsings = false, string projectFile = null)
        {
            IEnumerable<string> namespaces = ProjectNuGetManager.GetImplicitUsingNamespaces(projectFile);
            if (!namespaces.Any())
            {
                namespaces = includeAspNetCoreUsings
                    ? SdkImplicitUsingNamespaces.Concat(AspNetCoreImplicitUsingNamespaces)
                    : SdkImplicitUsingNamespaces;
            }
            else if (includeAspNetCoreUsings)
            {
                namespaces = namespaces.Concat(AspNetCoreImplicitUsingNamespaces);
            }

            string code = string.Join(Environment.NewLine,
                namespaces.Distinct(StringComparer.Ordinal).Select(ns => $"global using {ns};"));

            return CSharpSyntaxTree.ParseText(code, parseOptions, path: "__CIARE_ImplicitUsings.g.cs",
                cancellationToken: ct);
        }

        private static List<SyntaxTree> BuildProjectGlobalUsingsSyntaxTrees(string workspaceFolder,
            string currentFilePath, CSharpParseOptions parseOptions, CancellationToken ct)
        {
            var trees = new List<SyntaxTree>();
            foreach (string filePath in FindProjectGlobalUsingsFiles(workspaceFolder, currentFilePath))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), parseOptions,
                        path: filePath, cancellationToken: ct));
                }
                catch
                {
                    // Fall back to the built-in implicit usings when generated files are unreadable.
                }
            }

            return trees;
        }

        private static List<string> FindProjectGlobalUsingsFiles(string workspaceFolder, string currentFilePath)
        {
            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            if (string.IsNullOrWhiteSpace(projectFile))
                return new List<string>();

            string projectDirectory = Path.GetDirectoryName(projectFile);
            if (string.IsNullOrEmpty(projectDirectory))
                return new List<string>();

            string objDirectory = Path.Combine(projectDirectory, "obj");
            if (!Directory.Exists(objDirectory))
                return new List<string>();

            try
            {
                var files = Directory.EnumerateFiles(objDirectory, "*GlobalUsings.g.cs",
                        SearchOption.AllDirectories)
                    .Where(path => File.Exists(path))
                    .OrderByDescending(path => IsPreferredGlobalUsingsFile(path))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var preferredFiles = files
                    .Where(IsPreferredGlobalUsingsFile)
                    .ToList();
                if (preferredFiles.Count > 0)
                    return preferredFiles.Take(1).ToList();

                return files.Take(1).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static bool IsPreferredGlobalUsingsFile(string filePath)
        {
            string framework = GlobalVariables.Framework;
            if (string.IsNullOrWhiteSpace(framework))
                return false;

            return filePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith(framework, StringComparison.OrdinalIgnoreCase));
        }

        private static List<SyntaxTree> BuildWorkspaceSyntaxTrees(string workspaceFolder, string currentFilePath,
            string projectFile, CSharpParseOptions parseOptions, CancellationToken ct)
        {
            var trees = new List<SyntaxTree>();
            string sourceFolder = GetProjectSourceFolder(workspaceFolder, projectFile);
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
                return trees;

            string normalizedCurrentFile = NormalizePath(currentFilePath);
            foreach (var filePath in EnumerateWorkspaceCsFiles(sourceFolder, ct, true))
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(normalizedCurrentFile) &&
                    string.Equals(NormalizePath(filePath), normalizedCurrentFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var code = File.ReadAllText(filePath);
                    trees.Add(CSharpSyntaxTree.ParseText(code, parseOptions, path: filePath, cancellationToken: ct));
                }
                catch
                {
                    // Ignore unreadable workspace files; the active editor should still be checked.
                }
            }

            return trees;
        }

        private static string GetProjectSourceFolder(string workspaceFolder, string projectFile)
        {
            if (!string.IsNullOrWhiteSpace(projectFile) && File.Exists(projectFile))
            {
                string projectDirectory = Path.GetDirectoryName(projectFile);
                if (!string.IsNullOrEmpty(projectDirectory))
                    return projectDirectory;
            }

            return workspaceFolder;
        }

        private static IEnumerable<string> EnumerateWorkspaceCsFiles(string workspaceFolder, CancellationToken ct,
            bool skipNestedProjectDirectories = false)
        {
            var pending = new Stack<string>();
            pending.Push(workspaceFolder);

            while (pending.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var folder = pending.Pop();

                List<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(folder).ToList();
                }
                catch
                {
                    directories = new List<string>();
                }

                foreach (var directory in directories)
                {
                    if (!ShouldSkipWorkspaceDirectory(directory) &&
                        !(skipNestedProjectDirectories && DirectoryContainsProjectFile(directory)))
                    {
                        pending.Push(directory);
                    }
                }

                List<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, "*.cs").ToList();
                }
                catch
                {
                    files = new List<string>();
                }

                foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    yield return file;
            }
        }

        private static bool DirectoryContainsProjectFile(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldSkipWorkspaceDirectory(string directory)
        {
            string name = Path.GetFileName(directory);
            if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var attributes = File.GetAttributes(directory);
                return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                       (attributes & FileAttributes.System) == FileAttributes.System;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }

        private static bool ShouldReportDiagnostic(Diagnostic diagnostic, SyntaxTree activeSyntaxTree)
        {
            if (SuppressedDiagnosticIds.Contains(diagnostic.Id))
                return false;

            return !diagnostic.Location.IsInSource || diagnostic.Location.SourceTree == activeSyntaxTree;
        }

        private static bool HasTopLevelStatements(SyntaxTree syntaxTree, CancellationToken ct)
        {
            return syntaxTree.GetRoot(ct)
                .DescendantNodes()
                .OfType<GlobalStatementSyntax>()
                .Any();
        }

        private static IEnumerable<MetadataReference> BuildReferences(string workspaceFolder, string currentFilePath,
            bool hasProjectContext)
        {
            lock (_refLock)
            {
                EnsurePlatformReferences();

                // Rebuild custom references only when the list actually changes.
                var currentCustom = ResolveCustomReferencePaths(!hasProjectContext);
                if (_lastCustomRefSnapshot == null || !_lastCustomRefSnapshot.SequenceEqual(currentCustom))
                {
                    _lastCustomRefSnapshot = currentCustom.ToList();
                    _customRefs = new List<MetadataReference>();
                    foreach (var lib in currentCustom)
                    {
                        if (TryCreateMetadataReference(lib, out var reference))
                            _customRefs.Add(reference);
                    }
                }

                if (hasProjectContext)
                {
                    var currentFrameworkRefs = ResolveFrameworkReferencePaths(workspaceFolder, currentFilePath,
                        hasProjectContext);
                    if (_lastFrameworkRefSnapshot == null ||
                        !_lastFrameworkRefSnapshot.SequenceEqual(currentFrameworkRefs))
                    {
                        _lastFrameworkRefSnapshot = currentFrameworkRefs.ToList();
                        _frameworkRefs = new List<MetadataReference>();
                        foreach (var lib in currentFrameworkRefs)
                        {
                            if (TryCreateMetadataReference(lib, out var reference))
                                _frameworkRefs.Add(reference);
                        }
                    }

                    var currentNuGetRefs = ResolveNuGetReferencePaths(workspaceFolder, currentFilePath,
                        hasProjectContext);
                    if (_lastNuGetRefSnapshot == null || !_lastNuGetRefSnapshot.SequenceEqual(currentNuGetRefs))
                    {
                        _lastNuGetRefSnapshot = currentNuGetRefs.ToList();
                        _nuGetRefs = new List<MetadataReference>();
                        foreach (var lib in currentNuGetRefs)
                        {
                            if (TryCreateMetadataReference(lib, out var reference))
                                _nuGetRefs.Add(reference);
                        }
                    }
                }
                else
                {
                    _lastFrameworkRefSnapshot = new List<string>();
                    _frameworkRefs = new List<MetadataReference>();
                    _lastNuGetRefSnapshot = new List<string>();
                    _nuGetRefs = new List<MetadataReference>();
                }

                var references = new List<MetadataReference>();
                var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var assemblyIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var sourceAssemblyNames = ResolveActiveProjectAssemblyNames(workspaceFolder, currentFilePath,
                    hasProjectContext);

                AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames, _platformRefs, false);
                if (hasProjectContext)
                    AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames,
                        _frameworkRefs, true);
                if (hasProjectContext)
                {
                    var projectReferenceRefs = new List<MetadataReference>();
                    foreach (var lib in ResolveProjectReferenceOutputPaths(workspaceFolder, currentFilePath,
                        hasProjectContext))
                    {
                        if (TryCreateMetadataReference(lib, out var reference))
                            projectReferenceRefs.Add(reference);
                    }
                    AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames,
                        projectReferenceRefs, true);
                }
                AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames, _customRefs, true);
                if (hasProjectContext)
                    AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames, _nuGetRefs, true);
                return references;
            }
        }

        private static ImmutableArray<MetadataReference> BuildPlatformReferences()
        {
            lock (_refLock)
            {
                EnsurePlatformReferences();
                return _platformRefs;
            }
        }

        private static void EnsurePlatformReferences()
        {
            if (!_platformRefs.IsDefault)
                return;

            var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            // Exclude the entry assembly so that workspace source files don't produce
            // duplicate type definitions (which would cause false CS0121 ambiguity errors).
            var entryLocation = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
            IEnumerable<string> assemblyPaths;
            if (string.IsNullOrEmpty(trusted))
            {
                // Fallback: load all DLLs from the runtime directory when the host
                // doesn't set TRUSTED_PLATFORM_ASSEMBLIES (e.g. some test runners).
                var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
                assemblyPaths = Directory.Exists(runtimeDir)
                    ? Directory.GetFiles(runtimeDir, "*.dll")
                    : Enumerable.Empty<string>();
            }
            else
            {
                assemblyPaths = trusted.Split(Path.PathSeparator);
            }

            var references = new List<MetadataReference>();
            foreach (var assemblyPath in assemblyPaths
                .Where(r => !string.IsNullOrEmpty(r)
                         && !string.Equals(r, entryLocation, StringComparison.OrdinalIgnoreCase)
                         && File.Exists(r)))
            {
                if (TryCreateMetadataReference(assemblyPath, out var reference))
                    references.Add(reference);
            }

            _platformRefs = references.ToImmutableArray();
        }

        private static List<string> ResolveCustomReferencePaths(bool includeStandaloneNuGetReferences)
        {
            var activeNuGetReferencePaths = includeStandaloneNuGetReferences
                ? ReadActivePersistedNuGetReferencePaths()
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var paths = new List<string>();
            AddStoredReferencePaths(paths, GlobalVariables.customRefList);
            AddStoredReferencePaths(paths, GlobalVariables.filteredCustomRef);
            AddStoredReferencePaths(paths, GlobalVariables.customRefAsm);

            if (includeStandaloneNuGetReferences)
                paths.AddRange(activeNuGetReferencePaths);

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Where(path => ShouldIncludeCustomReferencePath(path, activeNuGetReferencePaths))
                .Where(path => !IsBlacklistedReference(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AddStoredReferencePaths(List<string> paths, IEnumerable<string> storedReferences)
        {
            if (storedReferences == null)
                return;

            foreach (var storedReference in storedReferences)
            {
                string path = GetStoredReferencePath(storedReference);
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }
        }

        private static HashSet<string> ReadActivePersistedNuGetReferencePaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!Directory.Exists(GlobalVariables.downloadNugetPath))
                    return paths;

                var activePackageNames = GetActiveNuGetPackageNames();
                if (activePackageNames.Count == 0)
                    return paths;

                foreach (var packageDataFile in Directory.EnumerateFiles(GlobalVariables.downloadNugetPath, "*.ddb"))
                {
                    string packageName = Path.GetFileNameWithoutExtension(packageDataFile);
                    if (!activePackageNames.Contains(packageName))
                        continue;

                    foreach (var line in File.ReadLines(packageDataFile))
                    {
                        string path = GetStoredReferencePath(line);
                        if (!string.IsNullOrWhiteSpace(path))
                            paths.Add(NormalizePath(path));
                    }
                }
            }
            catch
            {
            }

            return paths;
        }

        private static HashSet<string> GetActiveNuGetPackageNames()
        {
            var packageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (GlobalVariables.nugetNames == null)
                return packageNames;

            foreach (var package in GlobalVariables.nugetNames)
            {
                if (string.IsNullOrWhiteSpace(package))
                    continue;

                string packageName = package.Split('|')[0].Trim();
                if (!string.IsNullOrEmpty(packageName))
                    packageNames.Add(packageName);
            }

            return packageNames;
        }

        private static bool ShouldIncludeCustomReferencePath(string path, HashSet<string> activeNuGetReferencePaths)
        {
            if (!IsDownloadedNuGetReference(path))
                return true;

            return activeNuGetReferencePaths.Contains(NormalizePath(path));
        }

        private static bool IsDownloadedNuGetReference(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(GlobalVariables.downloadNugetPath))
                return false;

            return IsSameOrChildDirectory(path, GlobalVariables.downloadNugetPath);
        }

        private static bool IsBlacklistedReference(string path)
        {
            return GlobalVariables.blackRefList != null &&
                GlobalVariables.blackRefList.Any(blacklisted =>
                    !string.IsNullOrWhiteSpace(blacklisted) &&
                    (string.Equals(GetStoredReferencePath(blacklisted), path, StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(blacklisted, StringComparison.OrdinalIgnoreCase)));
        }

        private static string GetStoredReferencePath(string storedReference)
        {
            if (string.IsNullOrWhiteSpace(storedReference))
                return string.Empty;

            var parts = storedReference.Split('|');
            return parts.Length >= 2 ? parts[1] : storedReference;
        }

        private static bool TryCreateMetadataReference(string filePath, out MetadataReference reference)
        {
            reference = null;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                reference = MetadataReference.CreateFromFile(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void AddReferences(List<MetadataReference> references, HashSet<string> referencePaths,
            Dictionary<string, int> assemblyIndexes, HashSet<string> sourceAssemblyNames,
            IEnumerable<MetadataReference> newReferences, bool replaceExisting)
        {
            foreach (var reference in newReferences)
            {
                string filePath = GetReferenceFilePath(reference);
                if (!string.IsNullOrEmpty(filePath) && referencePaths.Contains(filePath))
                    continue;

                string assemblyName = GetReferenceAssemblyName(filePath);
                if (!string.IsNullOrEmpty(assemblyName) && sourceAssemblyNames.Contains(assemblyName))
                    continue;

                if (!string.IsNullOrEmpty(assemblyName) &&
                    assemblyIndexes.TryGetValue(assemblyName, out int existingIndex))
                {
                    if (replaceExisting && CanReplaceReference(GetReferenceFilePath(references[existingIndex]), filePath))
                    {
                        string existingPath = GetReferenceFilePath(references[existingIndex]);
                        if (!string.IsNullOrEmpty(existingPath))
                            referencePaths.Remove(existingPath);

                        references[existingIndex] = reference;
                        if (!string.IsNullOrEmpty(filePath))
                            referencePaths.Add(filePath);
                    }

                    continue;
                }

                if (!string.IsNullOrEmpty(filePath))
                    referencePaths.Add(filePath);
                if (!string.IsNullOrEmpty(assemblyName))
                    assemblyIndexes[assemblyName] = references.Count;
                references.Add(reference);
            }
        }

        private static bool CanReplaceReference(string existingPath, string newPath)
        {
            if (string.IsNullOrEmpty(existingPath) || string.IsNullOrEmpty(newPath))
                return false;

            return !IsSharedFrameworkReference(existingPath);
        }

        private static bool IsSharedFrameworkReference(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(runtimeDirectory))
                    return false;

                var frameworkParent = Directory.GetParent(runtimeDirectory);
                var sharedRoot = frameworkParent?.Parent?.FullName;
                return !string.IsNullOrEmpty(sharedRoot) && IsSameOrChildDirectory(directory, sharedRoot);
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<string> ResolveActiveProjectAssemblyNames(string workspaceFolder,
            string currentFilePath, bool hasProjectContext)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!hasProjectContext)
                return assemblyNames;

            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            string assemblyName = ReadProjectAssemblyName(projectFile);
            if (!string.IsNullOrWhiteSpace(assemblyName))
                assemblyNames.Add(assemblyName);

            return assemblyNames;
        }

        private static string ReadProjectAssemblyName(string projectFile)
        {
            try
            {
                var document = XDocument.Load(projectFile);
                string assemblyName = document
                    .Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "AssemblyName",
                        StringComparison.OrdinalIgnoreCase))
                    ?.Value
                    ?.Trim();

                return string.IsNullOrEmpty(assemblyName)
                    ? Path.GetFileNameWithoutExtension(projectFile)
                    : assemblyName;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(projectFile);
            }
        }

        private static string GetReferenceFilePath(MetadataReference reference)
        {
            var portableReference = reference as PortableExecutableReference;
            return portableReference?.FilePath ?? string.Empty;
        }

        private static string GetReferenceAssemblyName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            try
            {
                return AssemblyName.GetAssemblyName(filePath).Name ?? string.Empty;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private static bool ProjectUsesAspNetCore(string workspaceFolder, string currentFilePath,
            bool hasProjectContext)
        {
            if (!hasProjectContext)
                return false;

            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            if (ProjectFileUsesAspNetCore(projectFile))
                return true;

            foreach (string assetsPath in ResolveProjectAssetsFiles(workspaceFolder, currentFilePath,
                hasProjectContext))
            {
                if (ReadFrameworkReferencesFromAssets(assetsPath)
                    .Contains("Microsoft.AspNetCore.App"))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> ResolveFrameworkReferencePaths(string workspaceFolder, string currentFilePath,
            bool hasProjectContext)
        {
            if (!hasProjectContext)
                return new List<string>();

            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            var projectFrameworkReferencePaths = ProjectNuGetManager.GetFrameworkReferencePaths(projectFile);
            if (projectFrameworkReferencePaths.Count > 0)
                return projectFrameworkReferencePaths;

            var frameworkReferences = ReadFrameworkReferencesFromProjectFile(projectFile);
            foreach (string assetsPath in ResolveProjectAssetsFiles(workspaceFolder, currentFilePath,
                hasProjectContext))
            {
                frameworkReferences.UnionWith(ReadFrameworkReferencesFromAssets(assetsPath));
            }

            if (!frameworkReferences.Contains("Microsoft.AspNetCore.App"))
                return new List<string>();

            string targetFramework = ReadProjectTargetFramework(projectFile);
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                foreach (string assetsPath in ResolveProjectAssetsFiles(workspaceFolder, currentFilePath,
                    hasProjectContext))
                {
                    targetFramework = ReadTargetFrameworkFromAssets(assetsPath);
                    if (!string.IsNullOrWhiteSpace(targetFramework))
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(targetFramework))
                targetFramework = GlobalVariables.Framework;

            return ResolveAspNetCoreReferencePaths(targetFramework)
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool ProjectFileUsesAspNetCore(string projectFile)
        {
            return ReadFrameworkReferencesFromProjectFile(projectFile)
                .Contains("Microsoft.AspNetCore.App");
        }

        private static HashSet<string> ReadFrameworkReferencesFromProjectFile(string projectFile)
        {
            var frameworkReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                return frameworkReferences;

            try
            {
                var document = XDocument.Load(projectFile);
                if (SdkListContainsAspNetCoreWeb(document.Root?.Attribute("Sdk")?.Value))
                    frameworkReferences.Add("Microsoft.AspNetCore.App");

                foreach (var sdkElement in document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "Sdk", StringComparison.Ordinal)))
                {
                    string sdkName = sdkElement.Attribute("Name")?.Value ?? sdkElement.Attribute("Sdk")?.Value;
                    if (SdkListContainsAspNetCoreWeb(sdkName))
                        frameworkReferences.Add("Microsoft.AspNetCore.App");
                }

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

        private static bool SdkListContainsAspNetCoreWeb(string sdkList)
        {
            if (string.IsNullOrWhiteSpace(sdkList))
                return false;

            return sdkList
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(sdk => sdk.Trim().StartsWith("Microsoft.NET.Sdk.Web",
                    StringComparison.OrdinalIgnoreCase));
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

        private static string ReadProjectTargetFramework(string projectFile)
        {
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                return string.Empty;

            try
            {
                var document = XDocument.Load(projectFile);
                string targetFramework = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFramework",
                        StringComparison.Ordinal))?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(targetFramework))
                    return targetFramework;

                string targetFrameworks = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFrameworks",
                        StringComparison.Ordinal))?.Value;
                return SplitTargetFrameworks(targetFrameworks).FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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

            return string.Empty;
        }

        private static IEnumerable<string> ResolveAspNetCoreReferencePaths(string targetFramework)
        {
            string refTargetFramework = GetReferenceTargetFramework(targetFramework);
            if (string.IsNullOrEmpty(refTargetFramework))
                return Enumerable.Empty<string>();

            string versionPrefix = GetVersionPrefixFromTargetFramework(refTargetFramework);
            if (string.IsNullOrEmpty(versionPrefix))
                return Enumerable.Empty<string>();

            foreach (string dotnetRoot in GetDotnetRootCandidates())
            {
                string packRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref");
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
                string sharedRoot = Path.Combine(dotnetRoot, "shared", "Microsoft.AspNetCore.App");
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

        private static List<string> ResolveNuGetReferencePaths(string workspaceFolder, string currentFilePath,
            bool hasProjectContext)
        {
            var paths = new List<string>();
            foreach (var assetsPath in ResolveProjectAssetsFiles(workspaceFolder, currentFilePath,
                hasProjectContext))
                paths.AddRange(ReadCompileReferencesFromAssets(assetsPath));

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> ResolveProjectReferenceOutputPaths(string workspaceFolder,
            string currentFilePath, bool hasProjectContext)
        {
            var paths = new List<string>();
            if (!hasProjectContext)
                return paths;

            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                return paths;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NormalizePath(projectFile)
            };
            var projectReferences = new List<string>();
            AddProjectReferenceFiles(projectFile, visited, projectReferences);

            foreach (string referencedProject in projectReferences)
            {
                string outputPath = ResolveProjectOutputAssemblyPath(referencedProject);
                if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                    paths.Add(outputPath);
            }

            return paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AddProjectReferenceFiles(string projectFile, HashSet<string> visited,
            List<string> projectReferences)
        {
            foreach (string referencedProject in ReadProjectReferenceFiles(projectFile))
            {
                string normalized = NormalizePath(referencedProject);
                if (string.IsNullOrEmpty(normalized) || !visited.Add(normalized))
                    continue;

                projectReferences.Add(referencedProject);
                AddProjectReferenceFiles(referencedProject, visited, projectReferences);
            }
        }

        private static IEnumerable<string> ReadProjectReferenceFiles(string projectFile)
        {
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                yield break;

            string projectDirectory = Path.GetDirectoryName(projectFile);
            if (string.IsNullOrEmpty(projectDirectory))
                yield break;

            XDocument document;
            try
            {
                document = XDocument.Load(projectFile);
            }
            catch
            {
                yield break;
            }

            foreach (var projectReference in document.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "ProjectReference",
                    StringComparison.Ordinal)))
            {
                string include = projectReference.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include) || include.Contains("$"))
                    continue;

                string referencedProject;
                try
                {
                    referencedProject = Path.GetFullPath(Path.Combine(projectDirectory, include));
                }
                catch
                {
                    continue;
                }

                if (File.Exists(referencedProject))
                    yield return referencedProject;
            }
        }

        private static string ResolveProjectOutputAssemblyPath(string projectFile)
        {
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                return string.Empty;

            string projectDirectory = Path.GetDirectoryName(projectFile);
            if (string.IsNullOrEmpty(projectDirectory))
                return string.Empty;

            string assemblyName = ReadProjectAssemblyName(projectFile);
            if (string.IsNullOrWhiteSpace(assemblyName))
                return string.Empty;

            string configuration = GetActiveBuildConfiguration();
            string targetFramework = ReadProjectTargetFramework(projectFile);
            string assemblyFile = assemblyName + ".dll";

            var candidates = new List<string>();
            string configuredOutputPath = ReadProjectOutputPath(projectFile, configuration);
            string configuredOutputDirectory = string.Empty;
            if (!string.IsNullOrWhiteSpace(configuredOutputPath))
            {
                try
                {
                    configuredOutputDirectory = Path.GetFullPath(Path.Combine(projectDirectory,
                        configuredOutputPath));
                }
                catch
                {
                    configuredOutputDirectory = string.Empty;
                }

                if (!string.IsNullOrEmpty(configuredOutputDirectory))
                {
                    if (!string.IsNullOrWhiteSpace(targetFramework))
                        candidates.Add(Path.Combine(configuredOutputDirectory, targetFramework, assemblyFile));
                    candidates.Add(Path.Combine(configuredOutputDirectory, assemblyFile));
                }
            }
            if (!string.IsNullOrWhiteSpace(targetFramework))
                candidates.Add(Path.Combine(projectDirectory, "bin", configuration, targetFramework, assemblyFile));
            candidates.Add(Path.Combine(projectDirectory, "bin", configuration, assemblyFile));

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            if (!string.IsNullOrEmpty(configuredOutputDirectory) &&
                TryFindProjectOutputAssembly(configuredOutputDirectory, assemblyFile, targetFramework,
                    out string configuredOutputAssembly))
            {
                return configuredOutputAssembly;
            }

            string configurationDirectory = Path.Combine(projectDirectory, "bin", configuration);
            if (!Directory.Exists(configurationDirectory))
                return string.Empty;

            return TryFindProjectOutputAssembly(configurationDirectory, assemblyFile, targetFramework,
                out string outputAssembly)
                ? outputAssembly
                : string.Empty;
        }

        private static bool TryFindProjectOutputAssembly(string outputDirectory, string assemblyFile,
            string targetFramework, out string outputAssembly)
        {
            outputAssembly = string.Empty;
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
                return false;

            try
            {
                outputAssembly = Directory.EnumerateFiles(outputDirectory, assemblyFile, SearchOption.AllDirectories)
                    .OrderByDescending(path => IsPreferredProjectOutputPath(path, targetFramework))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty;
                return !string.IsNullOrEmpty(outputAssembly);
            }
            catch
            {
                return false;
            }
        }

        private static string ReadProjectOutputPath(string projectFile, string configuration)
        {
            if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
                return string.Empty;

            try
            {
                var document = XDocument.Load(projectFile);
                string fallback = string.Empty;

                foreach (var propertyGroup in document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup",
                        StringComparison.Ordinal)))
                {
                    string outputPath = propertyGroup.Elements()
                        .FirstOrDefault(element => string.Equals(element.Name.LocalName, "OutputPath",
                            StringComparison.Ordinal))
                        ?.Value
                        ?.Trim();
                    if (string.IsNullOrWhiteSpace(outputPath))
                        continue;

                    string condition = propertyGroup.Attribute("Condition")?.Value ?? string.Empty;
                    if (ConditionMatchesConfiguration(condition, configuration))
                        return outputPath;

                    if (string.IsNullOrWhiteSpace(condition) && string.IsNullOrWhiteSpace(fallback))
                        fallback = outputPath;
                }

                return fallback;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ConditionMatchesConfiguration(string condition, string configuration)
        {
            return !string.IsNullOrWhiteSpace(condition) &&
                !string.IsNullOrWhiteSpace(configuration) &&
                condition.IndexOf(configuration, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPreferredProjectOutputPath(string path, string targetFramework)
        {
            return !string.IsNullOrWhiteSpace(targetFramework) &&
                path.IndexOf(Path.DirectorySeparatorChar + targetFramework + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetActiveBuildConfiguration()
        {
            return !string.IsNullOrWhiteSpace(GlobalVariables.configParam) &&
                GlobalVariables.configParam.IndexOf("Release", StringComparison.OrdinalIgnoreCase) >= 0
                ? "Release"
                : "Debug";
        }

        private static IEnumerable<string> ResolveProjectAssetsFiles(string workspaceFolder, string currentFilePath,
            bool hasProjectContext)
        {
            var assetsPaths = new List<string>();
            if (!hasProjectContext)
                return assetsPaths;

            string projectFile = FindNearestProjectFile(currentFilePath, workspaceFolder);
            AddProjectAssetsPath(assetsPaths, projectFile);

            if (assetsPaths.Count == 0 && Directory.Exists(workspaceFolder))
            {
                string rootAssets = Path.Combine(workspaceFolder, "obj", "project.assets.json");
                if (File.Exists(rootAssets))
                    assetsPaths.Add(rootAssets);
                else
                {
                    var projectFiles = EnumerateProjectFiles(workspaceFolder).Take(2).ToList();
                    if (projectFiles.Count == 1)
                        AddProjectAssetsPath(assetsPaths, projectFiles[0]);
                }
            }

            return assetsPaths.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool HasOpenedProjectContext(string workspaceFolder, string currentFilePath)
        {
            if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder) ||
                string.IsNullOrWhiteSpace(currentFilePath) || !File.Exists(currentFilePath))
            {
                return false;
            }

            string currentFolder = Path.GetDirectoryName(currentFilePath);
            if (string.IsNullOrEmpty(currentFolder) || !IsSameOrChildDirectory(currentFolder, workspaceFolder))
                return false;

            return !string.IsNullOrEmpty(FindNearestProjectFile(currentFilePath, workspaceFolder));
        }

        private static void AddProjectAssetsPath(List<string> assetsPaths, string projectFile)
        {
            if (string.IsNullOrEmpty(projectFile) || !File.Exists(projectFile))
                return;

            string projectDirectory = Path.GetDirectoryName(projectFile);
            if (string.IsNullOrEmpty(projectDirectory))
                return;

            string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
            if (File.Exists(assetsPath))
                assetsPaths.Add(assetsPath);
        }

        private static string FindNearestProjectFile(string currentFilePath, string workspaceFolder)
        {
            if (string.IsNullOrWhiteSpace(currentFilePath) || !File.Exists(currentFilePath))
                return string.Empty;

            string folder = Path.GetDirectoryName(currentFilePath);
            string workspace = NormalizePath(workspaceFolder);

            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                if (!string.IsNullOrEmpty(workspace) && !IsSameOrChildDirectory(folder, workspace))
                    break;

                string projectFile = Directory.EnumerateFiles(folder, "*.csproj")
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(projectFile))
                    return projectFile;

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase))
                    break;

                folder = parent;
            }

            return string.Empty;
        }

        private static bool IsSameOrChildDirectory(string path, string folderPath)
        {
            try
            {
                string fullPath = NormalizePath(path);
                string fullFolder = NormalizePath(folderPath);
                return string.Equals(fullPath, fullFolder, StringComparison.OrdinalIgnoreCase) ||
                       fullPath.StartsWith(fullFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> EnumerateProjectFiles(string workspaceFolder)
        {
            var pending = new Stack<string>();
            pending.Push(workspaceFolder);

            while (pending.Count > 0)
            {
                var folder = pending.Pop();

                IEnumerable<string> projectFiles;
                try { projectFiles = Directory.EnumerateFiles(folder, "*.csproj"); }
                catch { projectFiles = Array.Empty<string>(); }

                foreach (var projectFile in projectFiles)
                    yield return projectFile;

                IEnumerable<string> directories;
                try { directories = Directory.EnumerateDirectories(folder); }
                catch { directories = Array.Empty<string>(); }

                foreach (var directory in directories)
                {
                    if (!ShouldSkipWorkspaceDirectory(directory))
                        pending.Push(directory);
                }
            }
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

        private static void ApplyMarkers(TextEditorControl editor, List<Diagnostic> errors, List<Diagnostic> warnings)
        {
            editor.Document.MarkerStrategy.RemoveAll(_ => true);

            foreach (var diagnostic in errors.Concat(warnings))
            {
                var location = diagnostic.Location;
                if (!location.IsInSource) continue;

                var span = location.GetLineSpan();
                int startLine = span.StartLinePosition.Line;
                int startCol = span.StartLinePosition.Character;

                if (startLine < 0 || startLine >= editor.Document.TotalNumberOfLines) continue;

                var lineSegment = editor.Document.GetLineSegment(startLine);
                int offset = lineSegment.Offset + startCol;
                int length = Math.Max(location.SourceSpan.Length, 1);

                // Clamp length to end of line to avoid overrun
                int maxLength = lineSegment.Length - startCol;
                if (maxLength < 1) maxLength = 1;
                length = Math.Min(length, maxLength);

                var color = diagnostic.Severity == DiagnosticSeverity.Error ? ErrorColor : WarningColor;
                var marker = new TextMarker(offset, length, TextMarkerType.WaveLine, color)
                {
                    ToolTip = $"{diagnostic.Id}: {diagnostic.GetMessage()}"
                };
                editor.Document.MarkerStrategy.AddMarker(marker);
            }

            InvalidateEditorTextArea(editor);
        }

        private static void InvalidateEditorTextArea(TextEditorControl editor)
        {
            var textArea = editor?.ActiveTextAreaControl?.TextArea;
            if (textArea == null || textArea.IsDisposed)
            {
                editor?.Invalidate();
                return;
            }

            textArea.Invalidate();
        }

        private static void UpdateErrorLabel(Label label, int errorCount)
        {
            if (label == null) return;
            if (errorCount == 0)
            {
                label.Text = string.Empty;
                return;
            }
            label.Text = $"\u2716 {errorCount} error{(errorCount > 1 ? "s" : "")}";
            label.ForeColor = Color.Red;
        }

        private static void UpdateWarningLabel(Label label, int warningCount)
        {
            if (label == null) return;
            if (warningCount == 0)
            {
                label.Text = string.Empty;
                return;
            }
            label.Text = $"\u26a0 {warningCount} warning{(warningCount > 1 ? "s" : "")}";
            label.ForeColor = Color.Orange;
        }

        private static void ClearMarkersAndStatus(TextEditorControl editor, Label statusLabel, Label warningsLabel = null)
        {
            editor.BeginInvoke((Action)(() =>
            {
                editor.Document.MarkerStrategy.RemoveAll(_ => true);
                InvalidateEditorTextArea(editor);
                if (statusLabel != null)
                    statusLabel.Text = string.Empty;
                if (warningsLabel != null)
                    warningsLabel.Text = string.Empty;
            }));
        }

        private static void ShowCheckerFailure(TextEditorControl editor, Label statusLabel, Label warningsLabel,
            ListView errorsLV, TabPage errorsTabPage, Exception exception)
        {
            if (editor == null || editor.IsDisposed || !editor.IsHandleCreated)
                return;

            try
            {
                string message = exception.GetBaseException()?.Message ?? exception.Message;
                editor.BeginInvoke((Action)(() =>
                {
                    editor.Document.MarkerStrategy.RemoveAll(_ => true);
                    InvalidateEditorTextArea(editor);

                    if (statusLabel != null)
                    {
                        statusLabel.Text = "Type check failed";
                        statusLabel.ForeColor = Color.Red;
                    }

                    if (warningsLabel != null)
                        warningsLabel.Text = string.Empty;

                    if (errorsLV == null)
                        return;

                    errorsLV.BeginUpdate();
                    errorsLV.Items.Clear();

                    var item = new ListViewItem("\u2716") { ForeColor = Color.Red };
                    item.SubItems.Add(string.Empty);
                    item.SubItems.Add("CIARE");
                    item.SubItems.Add($"Type check failed: {message}");
                    errorsLV.Items.Add(item);

                    errorsLV.EndUpdate();

                    SetErrorsTabTitle(errorsTabPage, "Errors (1)");
                }));
            }
            catch
            {
            }
        }

        private static void UpdateErrorsPanel(ListView errorsLV, TabPage errorsTabPage,
            List<Diagnostic> errors, List<Diagnostic> warnings)
        {
            if (errorsLV == null) return;

            errorsLV.BeginUpdate();
            errorsLV.Items.Clear();

            foreach (var d in errors.Concat(warnings))
            {
                var location = d.Location;
                int line = location.IsInSource ? location.GetLineSpan().StartLinePosition.Line + 1 : 0;
                var icon = d.Severity == DiagnosticSeverity.Error ? "\u2716" : "\u26a0";
                var rowColor = d.Severity == DiagnosticSeverity.Error ? Color.Red : Color.Orange;

                var item = new ListViewItem(icon) { ForeColor = rowColor };
                item.SubItems.Add(line > 0 ? line.ToString() : string.Empty);
                item.SubItems.Add(d.Id);
                item.SubItems.Add(d.GetMessage());
                item.Tag = d;
                errorsLV.Items.Add(item);
            }

            errorsLV.EndUpdate();

            if (errorsTabPage != null)
            {
                int total = errors.Count + warnings.Count;
                SetErrorsTabTitle(errorsTabPage, total > 0 ? $"Errors ({total})" : "Errors");
            }
        }

        private static void ClearErrorsPanel(ListView errorsLV, TabPage errorsTabPage)
        {
            if (errorsLV == null) return;
            errorsLV.Items.Clear();
            SetErrorsTabTitle(errorsTabPage, "Errors");
        }

        private static void SetErrorsTabTitle(TabPage errorsTabPage, string title)
        {
            if (errorsTabPage == null)
                return;

            errorsTabPage.Text = title;
            var tabControl = errorsTabPage.Parent as TabControl;
            if (tabControl == null)
                return;

            int titleWidth = TextRenderer.MeasureText(title, tabControl.Font).Width + 24;
            int tabWidth = Math.Max(130, titleWidth);
            if (tabControl.ItemSize.Width != tabWidth)
                tabControl.ItemSize = new Size(tabWidth, tabControl.ItemSize.Height);

            int index = tabControl.TabPages.IndexOf(errorsTabPage);
            if (index >= 0)
                tabControl.Invalidate(tabControl.GetTabRect(index));
            else
                tabControl.Invalidate();
        }

        private static CSharpParseOptions BuildParseOptions(string framework)
        {
            var languageVersion = LanguageVersion.Default;
            switch (framework)
            {
                case "net6.0-windows": languageVersion = LanguageVersion.CSharp10; break;
                case "net7.0-windows": languageVersion = LanguageVersion.CSharp11; break;
                case "net8.0-windows": languageVersion = LanguageVersion.CSharp12; break;
                case "net9.0-windows": languageVersion = LanguageVersion.CSharp13; break;
                case "net10.0-windows": languageVersion = LanguageVersion.CSharp14; break;
            }
            return CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        }
    }
}
