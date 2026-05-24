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
                    editor.Refresh();
                    if (statusLabel != null)
                        statusLabel.Text = string.Empty;
                    if (warningsLabel != null)
                        warningsLabel.Text = string.Empty;
                    ClearErrorsPanel(errorsLV, errorsTabPage);
                }));
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
            if (hasProjectContext)
            {
                syntaxTrees.Add(BuildImplicitUsingsSyntaxTree(parseOptions, ct));
                syntaxTrees.AddRange(BuildWorkspaceSyntaxTrees(workspaceFolder, currentFilePath, parseOptions, ct));
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

        private static SyntaxTree BuildImplicitUsingsSyntaxTree(CSharpParseOptions parseOptions, CancellationToken ct)
        {
            string code = string.Join(Environment.NewLine,
                SdkImplicitUsingNamespaces.Select(ns => $"global using {ns};"));

            return CSharpSyntaxTree.ParseText(code, parseOptions, path: "__CIARE_ImplicitUsings.g.cs",
                cancellationToken: ct);
        }

        private static List<SyntaxTree> BuildWorkspaceSyntaxTrees(string workspaceFolder, string currentFilePath,
            CSharpParseOptions parseOptions, CancellationToken ct)
        {
            var trees = new List<SyntaxTree>();
            if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
                return trees;

            string normalizedCurrentFile = NormalizePath(currentFilePath);
            foreach (var filePath in EnumerateWorkspaceCsFiles(workspaceFolder, ct))
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

        private static IEnumerable<string> EnumerateWorkspaceCsFiles(string workspaceFolder, CancellationToken ct)
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
                    if (!ShouldSkipWorkspaceDirectory(directory))
                        pending.Push(directory);
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
                    _lastNuGetRefSnapshot = new List<string>();
                    _nuGetRefs = new List<MetadataReference>();
                }

                var references = new List<MetadataReference>();
                var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var assemblyIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var sourceAssemblyNames = ResolveWorkspaceProjectAssemblyNames(workspaceFolder, currentFilePath,
                    hasProjectContext);

                AddReferences(references, referencePaths, assemblyIndexes, sourceAssemblyNames, _platformRefs, false);
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

        private static HashSet<string> ResolveWorkspaceProjectAssemblyNames(string workspaceFolder,
            string currentFilePath, bool hasProjectContext)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!hasProjectContext)
                return assemblyNames;

            foreach (var projectFile in EnumerateProjectFiles(workspaceFolder))
            {
                string assemblyName = ReadProjectAssemblyName(projectFile);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                    assemblyNames.Add(assemblyName);
            }

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

            editor.Refresh();
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
                editor.Refresh();
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
                    editor.Refresh();

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

                    if (errorsTabPage != null)
                        errorsTabPage.Text = "Errors (1)";
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
                errorsTabPage.Text = total > 0 ? $"Errors ({total})" : "Errors";
            }
        }

        private static void ClearErrorsPanel(ListView errorsLV, TabPage errorsTabPage)
        {
            if (errorsLV == null) return;
            errorsLV.Items.Clear();
            if (errorsTabPage != null)
                errorsTabPage.Text = "Errors";
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
