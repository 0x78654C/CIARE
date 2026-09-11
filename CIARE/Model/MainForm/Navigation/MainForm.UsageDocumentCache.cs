using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private static readonly object _usageDocumentCacheLock = new object();
        private static readonly Dictionary<string, UsageDocumentCacheEntry> _usageDocumentCache
            = new Dictionary<string, UsageDocumentCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private const int UsageDocumentCacheLimit = 128;
        private const long UsageFastReadMaxFileBytes = 256L * 1024L;
        private const long UsageDocumentCacheMaxFileBytes = 1024L * 1024L;

        private sealed class UsageDocumentCacheEntry
        {
            public UsageDocumentCacheEntry(UsageDocument document, long lastWriteUtcTicks, long length)
            {
                Document = document;
                LastWriteUtcTicks = lastWriteUtcTicks;
                Length = length;
                LastAccessUtc = DateTime.UtcNow;
            }

            public UsageDocument Document { get; }
            public long LastWriteUtcTicks { get; }
            public long Length { get; }
            public DateTime LastAccessUtc { get; private set; }

            public void Touch()
            {
                LastAccessUtc = DateTime.UtcNow;
            }
        }

        private static List<UsageDocument> BuildUsageDocuments(
            string identifier,
            List<OpenTabInfo> openTabs,
            List<string> workspaceFolders,
            List<string> compilationUnitFilePaths)
        {
            var documents = new List<UsageDocument>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parseOptions = CSharpParseOptions.Default;

            // Parse open-tab documents first (already in memory).
            foreach (var tab in openTabs)
                AddUsageDocument(documents, seen, tab.FilePath, tab.Text, tab.IsActive, parseOptions);

            // Gather all candidate file paths from workspace folders and compilation units.
            var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in workspaceFolders)
            {
                if (!Directory.Exists(folder))
                    continue;
                foreach (var f in GetWorkspaceCsFiles(folder))
                    allPaths.Add(f);
            }
            foreach (var f in compilationUnitFilePaths)
                allPaths.Add(f);

            // Snapshot already-seen paths so the parallel scan can filter without locking.
            var seenSnapshot = new HashSet<string>(seen, StringComparer.OrdinalIgnoreCase);
            var pathsToScan = allPaths
                .Where(f => !seenSnapshot.Contains(NormalizeCompletionPath(f)))
                .ToList();

            // Scan and parse files in parallel (disk I/O + Roslyn parse are the bottleneck).
            var newDocs = new ConcurrentDictionary<string, UsageDocument>(StringComparer.OrdinalIgnoreCase);
            Parallel.ForEach(pathsToScan, CreateUsageParallelOptions(), filePath =>
            {
                if (TryGetUsageDocumentFromFile(filePath, identifier, parseOptions, out UsageDocument document))
                    newDocs.TryAdd(NormalizeCompletionPath(filePath), document);
            });

            foreach (var kvp in newDocs)
            {
                if (seen.Add(kvp.Key))
                    documents.Add(kvp.Value);
            }

            return documents;
        }

        private static ParallelOptions CreateUsageParallelOptions()
        {
            return new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };
        }

        private static bool TryGetUsageDocumentFromFile(string filePath, string identifier,
            CSharpParseOptions parseOptions, out UsageDocument document)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrEmpty(identifier))
                return false;

            string normalizedPath = NormalizeCompletionPath(filePath);
            if (string.IsNullOrEmpty(normalizedPath))
                return false;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return false;
            }
            catch
            {
                return false;
            }

            if (TryGetCachedUsageDocument(normalizedPath, fileInfo, identifier, out document))
                return true;

            try
            {
                if (!TryReadUsageFileTextIfContainsIdentifier(filePath, fileInfo, identifier, out string text))
                    return false;

                var syntaxTree = CSharpSyntaxTree.ParseText(text, parseOptions, path: filePath);
                var root = syntaxTree.GetCompilationUnitRoot();
                document = new UsageDocument(filePath, text, syntaxTree, root, false);
                CacheUsageDocument(normalizedPath, document, fileInfo);
                return true;
            }
            catch
            {
                document = null;
                return false;
            }
        }

        private static bool TryReadUsageFileTextIfContainsIdentifier(string filePath, FileInfo fileInfo,
            string identifier, out string text)
        {
            text = null;
            try
            {
                if (fileInfo.Length <= UsageFastReadMaxFileBytes)
                {
                    text = File.ReadAllText(filePath);
                    if (TextContainsIdentifier(text, identifier))
                        return true;

                    text = null;
                    return false;
                }

                if (!FileContainsIdentifier(filePath, identifier))
                    return false;

                text = File.ReadAllText(filePath);
                return true;
            }
            catch
            {
                text = null;
                return false;
            }
        }

        private static bool TryGetCachedUsageDocument(string normalizedPath, FileInfo fileInfo,
            string identifier, out UsageDocument document)
        {
            document = null;
            UsageDocumentCacheEntry entry;
            lock (_usageDocumentCacheLock)
            {
                if (!_usageDocumentCache.TryGetValue(normalizedPath, out entry))
                    return false;

                if (entry.LastWriteUtcTicks != fileInfo.LastWriteTimeUtc.Ticks ||
                    entry.Length != fileInfo.Length)
                {
                    _usageDocumentCache.Remove(normalizedPath);
                    return false;
                }
            }

            if (!TextContainsIdentifier(entry.Document.Text, identifier))
                return false;

            lock (_usageDocumentCacheLock)
            {
                entry.Touch();
            }

            document = entry.Document;
            return true;
        }

        private static void CacheUsageDocument(string normalizedPath, UsageDocument document, FileInfo fileInfo)
        {
            if (document == null || fileInfo.Length > UsageDocumentCacheMaxFileBytes)
                return;

            lock (_usageDocumentCacheLock)
            {
                _usageDocumentCache[normalizedPath] = new UsageDocumentCacheEntry(
                    document,
                    fileInfo.LastWriteTimeUtc.Ticks,
                    fileInfo.Length);
                TrimUsageDocumentCache();
            }
        }

        private static void TrimUsageDocumentCache()
        {
            if (_usageDocumentCache.Count <= UsageDocumentCacheLimit)
                return;

            var keysToRemove = _usageDocumentCache
                .OrderBy(kvp => kvp.Value.LastAccessUtc)
                .Take(_usageDocumentCache.Count - UsageDocumentCacheLimit)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in keysToRemove)
                _usageDocumentCache.Remove(key);
        }

        private static void AddUsageDocument(List<UsageDocument> documents, HashSet<string> seen,
            string filePath, string text, bool isActive, CSharpParseOptions parseOptions)
        {
            string normalizedPath = NormalizeCompletionPath(filePath);
            if (!string.IsNullOrEmpty(normalizedPath) && !seen.Add(normalizedPath))
                return;

            string syntaxPath = string.IsNullOrEmpty(filePath) ? CurrentFileUsageDisplayName : filePath;
            var syntaxTree = CSharpSyntaxTree.ParseText(text ?? string.Empty, parseOptions, path: syntaxPath);
            var root = syntaxTree.GetCompilationUnitRoot();
            documents.Add(new UsageDocument(filePath, text, syntaxTree, root, isActive));
        }

        private static bool FileContainsIdentifier(string filePath, string identifier)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(identifier))
                return false;

            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    if (TextContainsIdentifier(line, identifier))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TextContainsIdentifier(string text, string identifier)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(identifier))
                return false;

            int index = -1;
            while ((index = text.IndexOf(identifier, index + 1, StringComparison.Ordinal)) >= 0)
            {
                int beforeIndex = index - 1;
                int afterIndex = index + identifier.Length;
                bool startsAtIdentifierBoundary = beforeIndex < 0 ||
                    text[beforeIndex] == '@' ||
                    !IsIdentifierPart(text[beforeIndex]);
                bool endsAtIdentifierBoundary = afterIndex >= text.Length ||
                    !IsIdentifierPart(text[afterIndex]);

                if (startsAtIdentifierBoundary && endsAtIdentifierBoundary)
                    return true;
            }

            return false;
        }

        private static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }
}
