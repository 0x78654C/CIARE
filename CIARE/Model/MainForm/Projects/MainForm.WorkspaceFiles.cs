using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        /// <summary>
        /// Walks up the directory tree from <paramref name="startDir"/> looking for a project
        /// root marker (.sln, .csproj, .git, .vs). Falls back to the parent directory of
        /// <paramref name="startDir"/> when no marker is found (so sibling folders are still included).
        /// </summary>
        private static string FindWorkspaceRoot(string startDir)
        {
            if (string.IsNullOrEmpty(startDir)) return startDir;
            string dir = startDir;
            for (int i = 0; i < 8; i++)
            {
                if (!Directory.Exists(dir)) break;
                try
                {
                    if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.Exists(Path.Combine(dir, ".git")) ||
                        Directory.Exists(Path.Combine(dir, ".vs")))
                        return dir;
                }
                catch { }
                string parent = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(parent) || parent == dir) break;
                dir = parent;
            }
            // No project marker found — go one level up so sibling directories are included.
            string oneLevelUp = Path.GetDirectoryName(startDir);
            return string.IsNullOrEmpty(oneLevelUp) ? startDir : oneLevelUp;
        }

        private static IEnumerable<string> GetWorkspaceCsFiles(string folder)
        {
            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                IEnumerable<string> dirs;
                try { dirs = Directory.EnumerateDirectories(current); }
                catch { dirs = Array.Empty<string>(); }
                foreach (var d in dirs)
                {
                    string name = Path.GetFileName(d);
                    if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase) ||
                        DirectoryContainsProjectFile(d))
                    {
                        continue;
                    }

                    try
                    {
                        var attrs = File.GetAttributes(d);
                        if ((attrs & FileAttributes.Hidden) == 0 && (attrs & FileAttributes.System) == 0)
                            pending.Push(d);
                    }
                    catch { pending.Push(d); }
                }
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(current, "*.cs"); }
                catch { files = Array.Empty<string>(); }
                foreach (var f in files)
                    yield return f;
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

        private static bool DirectoryContainsSolutionFile(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*.sln", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                return false;
            }
        }
    }
}
