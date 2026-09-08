using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CIARE;

public partial class MainForm
{
    private readonly object _completionParseLock = new();
    private readonly object _completionGlobalUsingsLock = new();
    private readonly Dictionary<string, (int Version, string Text)> _completionGlobalUsings = new(StringComparer.OrdinalIgnoreCase);
    private int _completionSourceVersion;
    private int _lastParsedSourceVersion = -1;
    private ulong _lastParsedSourceStamp;
    private bool _hasParsedSourceStamp;

    private void ClearCompletionGlobalUsingsCache()
    {
        lock (_completionGlobalUsingsLock) _completionGlobalUsings.Clear();
    }

    private void InvalidateCompletionSourceFile(string path)
    {
        string extension = Path.GetExtension(path);
        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".targets", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(extension) || ShouldRefreshExplorerNuGetPackages(path))
            Interlocked.Increment(ref _completionSourceVersion);
    }

    // Poll only file metadata for unchanged workspaces. This also catches changes
    // in referenced projects outside the explorer's FileSystemWatcher root.
    private static bool TryGetCompletionSourceStamp(CompletionScopeSnapshot scope, out ulong stamp)
    {
        ulong hash = 14695981039346656037UL;
        void AddFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var file = new FileInfo(path);
            unchecked
            {
                foreach (char ch in path) hash = (hash ^ ch) * 1099511628211UL;
                hash = (hash ^ (ulong)file.LastWriteTimeUtc.Ticks) * 1099511628211UL;
                hash = (hash ^ (ulong)(file.Exists ? file.Length : -1)) * 1099511628211UL;
            }
        }
        try
        {
            foreach (string project in scope.ProjectPaths ?? new List<string>())
            {
                AddFile(project);
                AddFile(FindProjectGlobalUsingsFile(project));
                AddFile(Path.Combine(Path.GetDirectoryName(project), "obj", "project.assets.json"));
            }
            foreach (string folder in scope.SourceFolders ?? new List<string>())
                foreach (string path in GetWorkspaceCsFiles(folder)) AddFile(path);
            stamp = hash;
            return true;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        stamp = 0;
        return false;
    }
}
