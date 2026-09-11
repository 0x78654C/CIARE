using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CIARE.Roslyn;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static global::CIARE.Utils.Completion.CompletionReferences;
using static global::CIARE.Utils.Completion.CompletionSyntax;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Projects.WorkspaceFiles;

namespace CIARE.Utils.Completion
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class CompletionProject
    {
        private readonly MainForm _mainForm;

        internal CompletionProject(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private const int RoslynCompletionSourceFileLimit = 300;
        private static readonly TimeSpan RoslynCompletionCacheLifetime = TimeSpan.FromSeconds(5);
        private readonly object _roslynCompletionCacheLock = new object();
        private RoslynCompletionProjectSnapshot _roslynCompletionProjectSnapshot;

        internal RoslynCompletionProjectSnapshot GetRoslynCompletionProjectSnapshot(
            string currentFilePath, string projectPath, CSharpParseOptions parseOptions,
            CancellationToken cancellationToken)
        {
            string cacheKey = BuildRoslynCompletionCacheKey(currentFilePath, projectPath, parseOptions);
            DateTime now = DateTime.UtcNow;
            lock (_roslynCompletionCacheLock)
            {
                if (_roslynCompletionProjectSnapshot != null &&
                    string.Equals(_roslynCompletionProjectSnapshot.CacheKey, cacheKey,
                        StringComparison.Ordinal) &&
                    now - _roslynCompletionProjectSnapshot.CreatedUtc < RoslynCompletionCacheLifetime)
                {
                    return _roslynCompletionProjectSnapshot;
                }

                cancellationToken.ThrowIfCancellationRequested();
                List<SyntaxTree> syntaxTrees = BuildRoslynCompletionProjectSyntaxTrees(
                    currentFilePath, projectPath, parseOptions, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                List<MetadataReference> references = BuildCompletionReferences(projectPath).ToList();
                cancellationToken.ThrowIfCancellationRequested();
                _roslynCompletionProjectSnapshot = new RoslynCompletionProjectSnapshot(
                    cacheKey, now, syntaxTrees, references);
                return _roslynCompletionProjectSnapshot;
            }
        }

        private static string BuildRoslynCompletionCacheKey(string currentFilePath, string projectPath,
            CSharpParseOptions parseOptions)
        {
            long projectWriteTicks = 0;
            try
            {
                if (!string.IsNullOrEmpty(projectPath) && File.Exists(projectPath))
                    projectWriteTicks = File.GetLastWriteTimeUtc(projectPath).Ticks;
            }
            catch
            {
            }

            return string.Join("|",
                NormalizeCompletionPath(currentFilePath),
                NormalizeCompletionPath(projectPath),
                projectWriteTicks.ToString(),
                parseOptions.LanguageVersion.ToString(),
                GlobalVariables.OUnsafeCode.ToString(),
                GlobalVariables.Framework ?? string.Empty);
        }

        private List<SyntaxTree> BuildRoslynCompletionProjectSyntaxTrees(string currentFilePath,
            string projectPath, CSharpParseOptions parseOptions, CancellationToken cancellationToken)
        {
            var syntaxTrees = new List<SyntaxTree>();
            var projectPaths = GetCompletionProjectPaths(projectPath);
            AddRoslynCompletionGlobalUsings(syntaxTrees, projectPaths, parseOptions, cancellationToken);
            foreach (string completionProjectPath in projectPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                syntaxTrees.AddRange(RealTimeChecker.BuildProjectGeneratedXamlSyntaxTrees(
                    completionProjectPath, parseOptions, cancellationToken,
                    useTypedSyntheticFields: true));
            }

            var sourceFolders = GetCompletionSourceFolders(projectPaths);
            if (sourceFolders.Count == 0)
                return syntaxTrees;

            string normalizedCurrent = NormalizeCompletionPath(currentFilePath);
            var seenSourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(normalizedCurrent))
                seenSourceFiles.Add(normalizedCurrent);

            int sourceFileCount = 0;
            foreach (string sourceFolder in sourceFolders)
            {
                foreach (string filePath in GetWorkspaceCsFiles(sourceFolder))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string normalizedFile = NormalizeCompletionPath(filePath);
                    if (string.IsNullOrEmpty(normalizedFile) || !seenSourceFiles.Add(normalizedFile))
                        continue;

                    if (++sourceFileCount > RoslynCompletionSourceFileLimit)
                        return syntaxTrees;

                    AddRoslynCompletionSyntaxTree(syntaxTrees, filePath, parseOptions, cancellationToken);
                }
            }

            return syntaxTrees;
        }

        private static void AddRoslynCompletionGlobalUsings(List<SyntaxTree> syntaxTrees,
            IList<string> projectPaths, CSharpParseOptions parseOptions,
            CancellationToken cancellationToken)
        {
            var addedGeneratedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string projectPath in projectPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string globalUsingsPath = FindProjectGlobalUsingsFile(projectPath);
                if (!string.IsNullOrEmpty(globalUsingsPath) &&
                    addedGeneratedFiles.Add(NormalizeCompletionPath(globalUsingsPath)))
                {
                    AddRoslynCompletionSyntaxTree(syntaxTrees, globalUsingsPath, parseOptions,
                        cancellationToken);
                    continue;
                }

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(BuildCompletionImplicitUsingsCode(projectPath),
                    parseOptions, path: GetCompletionImplicitUsingsTreePath(projectPath)));
            }
        }

        private static string GetCompletionImplicitUsingsTreePath(string projectPath)
        {
            string normalizedProject = NormalizeCompletionPath(projectPath);
            string fileName = string.IsNullOrEmpty(normalizedProject)
                ? "Project"
                : Path.GetFileNameWithoutExtension(normalizedProject);
            return "__CIARE_ImplicitUsings_" + fileName + ".g.cs";
        }

        private static void AddRoslynCompletionSyntaxTree(List<SyntaxTree> syntaxTrees,
            string filePath, CSharpParseOptions parseOptions, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(filePath))
                    return;

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(filePath),
                    parseOptions, path: filePath, cancellationToken: cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        internal sealed class RoslynCompletionProjectSnapshot
        {
            private readonly object compilationLock = new object();
            private CSharpCompilation compilation;
            private SyntaxTree activeTree;

            public RoslynCompletionProjectSnapshot(string cacheKey, DateTime createdUtc,
                List<SyntaxTree> syntaxTrees, List<MetadataReference> references)
            {
                CacheKey = cacheKey;
                CreatedUtc = createdUtc;
                compilation = CSharpCompilation.Create("__CiareCompletion", syntaxTrees, references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: GlobalVariables.OUnsafeCode));
            }

            public CSharpCompilation WithActiveTree(SyntaxTree tree)
            {
                lock (compilationLock)
                {
                    // Preserve metadata bindings and unchanged project syntax trees
                    // across keystrokes; Roslyn compilations are immutable snapshots.
                    compilation = activeTree == null
                        ? compilation.AddSyntaxTrees(tree)
                        : compilation.ReplaceSyntaxTree(activeTree, tree);
                    activeTree = tree;
                    return compilation;
                }
            }

            public string CacheKey { get; }
            public DateTime CreatedUtc { get; }
        }
    }
}
