using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;

namespace CIARE.Roslyn
{
    // Roslyn shares metadata and symbol bindings when compilations use the same
    // reference instance. Weak entries let closed projects release their images.
    internal static class SharedMetadataReferences
    {
        private sealed class Entry
        {
            public long LastWriteTicks;
            public long Length;
            public WeakReference<PortableExecutableReference> Reference;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries = new(StringComparer.OrdinalIgnoreCase);

        public static PortableExecutableReference Get(string path)
        {
            var file = new FileInfo(path);
            long writeTicks = file.LastWriteTimeUtc.Ticks;
            long length = file.Length;
            lock (Sync)
            {
                if (Entries.TryGetValue(file.FullName, out var entry) &&
                    entry.LastWriteTicks == writeTicks && entry.Length == length &&
                    entry.Reference.TryGetTarget(out var reference))
                    return reference;

                reference = MetadataReference.CreateFromFile(file.FullName);
                if (Entries.Count >= 1024)
                {
                    var expired = new List<string>();
                    foreach (var pair in Entries)
                        if (!pair.Value.Reference.TryGetTarget(out _)) expired.Add(pair.Key);
                    foreach (string key in expired) Entries.Remove(key);
                }
                Entries[file.FullName] = new Entry
                {
                    LastWriteTicks = writeTicks,
                    Length = length,
                    Reference = new WeakReference<PortableExecutableReference>(reference)
                };
                return reference;
            }
        }
    }
}
