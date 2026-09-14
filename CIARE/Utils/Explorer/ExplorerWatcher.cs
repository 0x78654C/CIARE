using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static global::CIARE.Utils.Explorer.ExplorerTree;
using static global::CIARE.Utils.NuGetManage.NuGet;

namespace CIARE.Utils.Explorer
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ExplorerWatcher
    {
        private readonly MainForm _mainForm;

        internal ExplorerWatcher(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private FileSystemWatcher _fileExplorerWatcher;
        private System.Windows.Forms.Timer _fileExplorerRefreshTimer;
        private readonly HashSet<string> _pendingRefreshPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal void StartFileExplorerWatcher(string folderPath)
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

        internal void StopFileExplorerWatcher()
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
            if (_mainForm.NuGetFeature._fileExplorerNuGetRefreshTimer != null)
            {
                _mainForm.NuGetFeature._fileExplorerNuGetRefreshTimer.Stop();
                _mainForm.NuGetFeature._fileExplorerNuGetRefreshTimer.Tick -= _mainForm.NuGetFeature.OnFileExplorerNuGetRefreshTimer;
                _mainForm.NuGetFeature._fileExplorerNuGetRefreshTimer.Dispose();
                _mainForm.NuGetFeature._fileExplorerNuGetRefreshTimer = null;
            }
            _mainForm.NuGetFeature._pendingProjectPackageRefreshPath = string.Empty;
            _mainForm.NuGetFeature._pendingProjectPackageRestore = false;
            _mainForm.NuGetFeature._pendingProjectPackageShowRestoreFailure = false;
            _pendingRefreshPaths.Clear();
        }

        private void OnFileExplorerWatcherEvent(object sender, FileSystemEventArgs e)
        {
            _mainForm.CompletionResourcesFeature.InvalidateCompletionSourceFile(e.FullPath);
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                _mainForm.NuGetFeature.ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherChanged(object sender, FileSystemEventArgs e)
        {
            _mainForm.CompletionResourcesFeature.InvalidateCompletionSourceFile(e.FullPath);
            if (ShouldRefreshExplorerNuGetPackages(e.FullPath))
                _mainForm.NuGetFeature.ScheduleExplorerNuGetRefresh();
        }

        private void OnFileExplorerWatcherRenamed(object sender, RenamedEventArgs e)
        {
            _mainForm.CompletionResourcesFeature.InvalidateCompletionSourceFile(e.OldFullPath);
            _mainForm.CompletionResourcesFeature.InvalidateCompletionSourceFile(e.FullPath);
            string parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
                ScheduleExplorerRefresh(parentDir);

            if (ShouldRefreshExplorerNuGetPackages(e.FullPath) ||
                ShouldRefreshExplorerNuGetPackages(e.OldFullPath))
            {
                _mainForm.NuGetFeature.ScheduleExplorerNuGetRefresh();
            }
        }

        private void ScheduleExplorerRefresh(string dirPath)
        {
            if (_mainForm.IsDisposed || !_mainForm.IsHandleCreated)
                return;
            _mainForm.BeginInvoke((Action)(() =>
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
            if (_mainForm.ExplorerFeature._fileExplorerTree == null || _mainForm.ExplorerFeature._fileExplorerTree.IsDisposed || _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Count == 0)
                return;

            _mainForm.ExplorerFeature._fileExplorerTree.BeginUpdate();
            foreach (string path in paths)
                RefreshExplorerNodeForPath(path);
            _mainForm.ExplorerFeature._fileExplorerTree.EndUpdate();
        }

        internal void RefreshExplorerNodeForPath(string dirPath)
        {
            var node = FindTreeNodeByPath(_mainForm.ExplorerFeature._fileExplorerTree.Nodes[0], dirPath);
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
            _mainForm.ExplorerTreeFeature.PopulateDirectoryNode(node);
            _mainForm.ExplorerTreeFeature.RestoreExpandedPaths(node, expandedPaths);
        }
    }
}
