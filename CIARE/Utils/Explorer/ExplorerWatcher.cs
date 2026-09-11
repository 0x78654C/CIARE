using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CIARE
{
    public partial class MainForm
    {
        private FileSystemWatcher _fileExplorerWatcher;
        private System.Windows.Forms.Timer _fileExplorerRefreshTimer;
        private readonly HashSet<string> _pendingRefreshPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void StartFileExplorerWatcher(string folderPath)
        {
            StopFileExplorerWatcher();

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            _fileExplorerWatcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _fileExplorerWatcher.Created += OnFileExplorerWatcherEvent;
            _fileExplorerWatcher.Deleted += OnFileExplorerWatcherEvent;
            _fileExplorerWatcher.Changed += OnFileExplorerWatcherChanged;
            _fileExplorerWatcher.Renamed += OnFileExplorerWatcherRenamed;

            _fileExplorerRefreshTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _fileExplorerRefreshTimer.Tick += OnFileExplorerRefreshTimer;
        }

        private void StopFileExplorerWatcher()
        {
            if (_fileExplorerWatcher != null)
            {
                _fileExplorerWatcher.EnableRaisingEvents = false;
                _fileExplorerWatcher.Dispose();
                _fileExplorerWatcher = null;
            }
            if (_fileExplorerRefreshTimer != null)
            {
                _fileExplorerRefreshTimer.Stop();
                _fileExplorerRefreshTimer.Tick -= OnFileExplorerRefreshTimer;
                _fileExplorerRefreshTimer.Dispose();
                _fileExplorerRefreshTimer = null;
            }
            if (_fileExplorerNuGetRefreshTimer != null)
            {
                _fileExplorerNuGetRefreshTimer.Stop();
                _fileExplorerNuGetRefreshTimer.Tick -= OnFileExplorerNuGetRefreshTimer;
                _fileExplorerNuGetRefreshTimer.Dispose();
                _fileExplorerNuGetRefreshTimer = null;
            }
            _pendingProjectPackageRefreshPath = string.Empty;
            _pendingProjectPackageRestore = false;
            _pendingProjectPackageShowRestoreFailure = false;
            _pendingRefreshPaths.Clear();
        }

        private void OnFileExplorerWatcherEvent(object sender, FileSystemEventArgs e)
        {
            InvalidateCompletionSourceFile(e.FullPath);
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherChanged(object sender, FileSystemEventArgs e)
        {
            InvalidateCompletionSourceFile(e.FullPath);
            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherRenamed(object sender, RenamedEventArgs e)
        {
            InvalidateCompletionSourceFile(e.OldFullPath);
            InvalidateCompletionSourceFile(e.FullPath);
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath) ||
                ShouldRefreshExplorerNuGetPackages(e.OldFullPath))
            {
                ScheduleExplorerNuGetRefresh();
            }
        }

        private void ScheduleExplorerRefresh(string dirPath)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            BeginInvoke((Action)(() =>
            {
                _pendingRefreshPaths.Add(dirPath);
                _fileExplorerRefreshTimer?.Stop();
                _fileExplorerRefreshTimer?.Start();
            }));
        }

        private void OnFileExplorerRefreshTimer(object sender, EventArgs e)
        {
            _fileExplorerRefreshTimer?.Stop();
            var toRefresh = new HashSet<string>(_pendingRefreshPaths, StringComparer.OrdinalIgnoreCase);
            _pendingRefreshPaths.Clear();
            RefreshExplorerNodes(toRefresh);
        }

        private void RefreshExplorerNodes(HashSet<string> paths)
        {
            if (_fileExplorerTree == null || _fileExplorerTree.IsDisposed || _fileExplorerTree.Nodes.Count == 0)
                return;

            _fileExplorerTree.BeginUpdate();
            foreach (string path in paths)
                RefreshExplorerNodeForPath(path);
            _fileExplorerTree.EndUpdate();
        }

        private void RefreshExplorerNodeForPath(string dirPath)
        {
            var node = FindTreeNodeByPath(_fileExplorerTree.Nodes[0], dirPath);
            if (node == null)
                return;

            if (!node.IsExpanded)
            {
                node.Nodes.Clear();
                node.Nodes.Add(new TreeNode("Loading...") { Tag = FileExplorerLoadingTag });
                return;
            }

            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectExpandedPaths(node, expandedPaths);
            PopulateDirectoryNode(node);
            RestoreExpandedPaths(node, expandedPaths);
        }
    }
}
