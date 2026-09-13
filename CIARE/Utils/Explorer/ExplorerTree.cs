using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Explorer.ExplorerLayout;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.StartupProject;
using static global::CIARE.Utils.Projects.WorkspaceFiles;

namespace CIARE.Utils.Explorer
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ExplorerTree
    {
        private readonly MainForm _mainForm;

        internal ExplorerTree(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal const string FileExplorerLoadingTag = "__loading__";
        private const string FileExplorerPathKey = "fileExplorerPath";
        private static readonly string FileExplorerExpandedPathsFilePath =
            Path.Combine(GlobalVariables.userProfileDirectory, "fileExplorerExpandedPaths.cDat");
        internal string _fileExplorerRootPath = string.Empty;
        private bool _suppressFileExplorerExpandedStateSave;

        internal void fileExplorerOpenFolderButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Open folder in file explorer";
                dialog.ShowNewFolderButton = false;

                if (Directory.Exists(_fileExplorerRootPath))
                    dialog.SelectedPath = _fileExplorerRootPath;
                else
                {
                    var filePath = _mainForm.EditorFeature.GetActiveEditorFilePath();
                    if (File.Exists(filePath))
                        dialog.SelectedPath = Path.GetDirectoryName(filePath);
                }

                if (dialog.ShowDialog(_mainForm) == DialogResult.OK)
                    LoadFileExplorerFolder(dialog.SelectedPath);
            }
        }

        internal void RestoreFileExplorerState()
        {
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, FileExplorerPathKey, string.Empty);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, FileExplorerVisibleKey, "True");
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, FileExplorerStartupProjectKey, string.Empty);
            _mainForm.ExplorerLayoutFeature.LoadFileExplorerLayoutValues();

            string savedPath = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerPathKey);
            string savedVisible = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerVisibleKey);

            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                LoadFileExplorerFolder(savedPath);

            if (savedVisible == "False")
                _mainForm.ExplorerLayoutFeature.ToggleFileExplorer(false, saveWidth: false);
            else
                _mainForm.ExplorerLayoutFeature.QueueFileExplorerLayoutApply();
        }

        internal void LoadFileExplorerFolder(string folderPath, string solutionPath = null)
        {
            if (!Directory.Exists(folderPath) || _mainForm.ExplorerFeature._fileExplorerTree == null)
                return;

            _fileExplorerRootPath = folderPath;
            _mainForm.ProjectContextFeature._fileExplorerSolutionPath = ResolveSolutionPathForLoadedFolder(folderPath, solutionPath);
            _mainForm.StartupProjectFeature.RestoreStartupProjectForLoadedFolder();

            _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
            Interlocked.Increment(ref _mainForm.NuGetFeature._projectPackageRefreshVersion);
            _mainForm.CompletionReferencesFeature.ClearProjectPackageCompletionReferences();
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerPathKey, folderPath);
            var expandedPaths = ReadFileExplorerExpandedPaths(folderPath);
            _mainForm.ExplorerFeature._fileExplorerTree.BeginUpdate();
            _suppressFileExplorerExpandedStateSave = true;
            try
            {
                _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Clear();
                var root = CreateDirectoryNode(new DirectoryInfo(folderPath));
                _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Add(root);
                PopulateDirectoryNode(root);
                root.Expand();
                RestoreExpandedPaths(root, expandedPaths);
            }
            finally
            {
                _suppressFileExplorerExpandedStateSave = false;
                _mainForm.ExplorerFeature._fileExplorerTree.EndUpdate();
            }
            _mainForm.ExplorerFeature._fileExplorerTitleLabel.Text = "Explorer";
            _mainForm.toolTip1.SetToolTip(_mainForm.ExplorerFeature._fileExplorerTitleLabel, folderPath);

            _mainForm.ExplorerWatcherFeature.StartFileExplorerWatcher(folderPath);
            SaveFileExplorerExpandedState();
            _mainForm.NuGetFeature.RefreshProjectPackageContext(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(), restoreProject: false,
                showRestoreFailure: false);
            _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
        }

        internal static TreeNode FindTreeNodeByPath(TreeNode root, string path)
        {
            if (string.Equals(root.Tag as string, path, StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (TreeNode child in root.Nodes)
            {
                string tag = child.Tag as string;
                if (tag == null) continue;
                if (string.Equals(tag, path, StringComparison.OrdinalIgnoreCase))
                    return child;
                if (path.StartsWith(tag + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    var found = FindTreeNodeByPath(child, path);
                    if (found != null) return found;
                }
            }
            return null;
        }

        internal static void CollectExpandedPaths(TreeNode node, HashSet<string> paths)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.IsExpanded)
                {
                    string path = NormalizeCompletionPath(child.Tag as string);
                    if (!string.IsNullOrEmpty(path))
                        paths.Add(path);
                    CollectExpandedPaths(child, paths);
                }
            }
        }

        internal void RestoreExpandedPaths(TreeNode node, HashSet<string> expandedPaths)
        {
            foreach (TreeNode child in node.Nodes)
            {
                string tag = NormalizeCompletionPath(child.Tag as string);
                if (tag != null && expandedPaths.Contains(tag))
                {
                    PopulateDirectoryNode(child);
                    child.Expand();
                    RestoreExpandedPaths(child, expandedPaths);
                }
            }
        }

        private TreeNode CreateDirectoryNode(DirectoryInfo directory)
        {
            string text = string.IsNullOrEmpty(directory.Name) ? directory.FullName : directory.Name;
            string imageKey = GetDirectoryImageKey(directory.FullName, open: false);
            string selectedImageKey = GetDirectoryImageKey(directory.FullName, open: true);
            var node = new TreeNode(text)
            {
                Tag = directory.FullName,
                ImageKey = imageKey,
                SelectedImageKey = selectedImageKey,
                ToolTipText = directory.FullName
            };
            _mainForm.StartupProjectFeature.ApplyStartupProjectNodeStyle(node);
            node.Nodes.Add(new TreeNode("Loading...") { Tag = FileExplorerLoadingTag });
            return node;
        }

        private TreeNode CreateFileNode(FileInfo file)
        {
            string imageKey = GetFileExplorerImageKey(file.FullName);
            var node = new TreeNode(file.Name)
            {
                Tag = file.FullName,
                ImageKey = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText = file.FullName
            };
            _mainForm.StartupProjectFeature.ApplyStartupProjectNodeStyle(node);
            return node;
        }

        private static string GetDirectoryImageKey(string folderPath, bool open)
        {
            if (DirectoryContainsSolutionFile(folderPath))
                return open ? "folder-solution-open" : "folder-solution";

            if (DirectoryContainsProjectFile(folderPath))
                return open ? "folder-project-open" : "folder-project";

            return open ? "folder-open" : "folder";
        }

        private static string GetFileExplorerImageKey(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return "cs";
                case ".csproj":
                    return "project";
                case ".sln":
                    return "solution";
                case ".txt":
                case ".md":
                case ".json":
                case ".xml":
                case ".config":
                case ".xshd":
                    return "text";
                default:
                    return "file";
            }
        }

        internal void fileExplorerTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && Equals(e.Node.Nodes[0].Tag, FileExplorerLoadingTag))
                PopulateDirectoryNode(e.Node);
        }

        internal void fileExplorerTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            SaveFileExplorerExpandedState();
        }

        internal void fileExplorerTree_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            SaveFileExplorerExpandedState();
        }

        internal void fileExplorerTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _mainForm.NuGetFeature.RefreshExplorerNuGetPackages();
        }

        internal void SaveFileExplorerExpandedState()
        {
            if (_suppressFileExplorerExpandedStateSave ||
                _mainForm.ExplorerFeature._fileExplorerTree == null ||
                _mainForm.ExplorerFeature._fileExplorerTree.IsDisposed ||
                _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Count == 0)
            {
                return;
            }

            try
            {
                var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectExpandedPaths(_mainForm.ExplorerFeature._fileExplorerTree.Nodes[0], expandedPaths);

                Directory.CreateDirectory(GlobalVariables.userProfileDirectory);
                File.WriteAllLines(FileExplorerExpandedPathsFilePath,
                    expandedPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                // Explorer state persistence is best-effort.
            }
        }

        private HashSet<string> ReadFileExplorerExpandedPaths(string rootPath)
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rootPath) || !File.Exists(FileExplorerExpandedPathsFilePath))
                return expandedPaths;

            try
            {
                foreach (string line in File.ReadAllLines(FileExplorerExpandedPathsFilePath))
                {
                    string path = NormalizeCompletionPath(line);
                    if (string.IsNullOrEmpty(path) ||
                        !Directory.Exists(path) ||
                        !IsSameOrChildDirectory(path, rootPath))
                    {
                        continue;
                    }

                    expandedPaths.Add(path);
                }
            }
            catch
            {
            }

            return expandedPaths;
        }

        internal void PopulateDirectoryNode(TreeNode node)
        {
            string folderPath = node.Tag as string;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            node.Nodes.Clear();

            try
            {
                foreach (var directory in Directory.GetDirectories(folderPath)
                    .Select(path => new DirectoryInfo(path))
                    .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldHideExplorerDirectory(directory))
                        continue;
                    node.Nodes.Add(CreateDirectoryNode(directory));
                }

                foreach (var file in Directory.GetFiles(folderPath)
                    .Select(path => new FileInfo(path))
                    .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldHideExplorerItem(file.Attributes))
                        continue;
                    node.Nodes.Add(CreateFileNode(file));
                }
            }
            catch
            {
                node.Nodes.Add(new TreeNode("Unable to read folder") { ImageKey = "file", SelectedImageKey = "file" });
            }
        }

        private static bool ShouldHideExplorerItem(FileAttributes attributes)
        {
            return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                   (attributes & FileAttributes.System) == FileAttributes.System;
        }

        private static bool ShouldHideExplorerDirectory(DirectoryInfo directory)
        {
            return directory == null ||
                string.Equals(directory.Name, ".ciare", StringComparison.OrdinalIgnoreCase) ||
                ShouldHideExplorerItem(directory.Attributes);
        }

        internal void fileExplorerTree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            TreeNode node = _mainForm.ExplorerFeature._fileExplorerTree.GetNodeAt(e.Location);
            if (node == null)
            {
                _mainForm.ExplorerFeature._fileExplorerContextNode = null;
                return;
            }

            _mainForm.ExplorerFeature._fileExplorerTree.SelectedNode = node;
            _mainForm.ExplorerFeature._fileExplorerContextNode = node;
        }

        internal string GetExplorerContextFolderPath()
        {
            string path = (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string;
            return Directory.Exists(path) && IsPathInsideExplorerRoot(path) ? path : string.Empty;
        }

        internal bool IsExplorerRootPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                Directory.Exists(_fileExplorerRootPath) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(_fileExplorerRootPath),
                    StringComparison.OrdinalIgnoreCase);
        }

        internal bool IsPathInsideExplorerRoot(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                Directory.Exists(_fileExplorerRootPath) &&
                IsSameOrChildDirectory(path, _fileExplorerRootPath);
        }

        internal void SelectExplorerPath(string path)
        {
            if (_mainForm.ExplorerFeature._fileExplorerTree == null || _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Count == 0 || string.IsNullOrWhiteSpace(path))
                return;

            TreeNode node = FindTreeNodeByPath(_mainForm.ExplorerFeature._fileExplorerTree.Nodes[0], path);
            if (node != null)
            {
                _mainForm.ExplorerFeature._fileExplorerTree.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        internal void RefreshAndExpandExplorerFolder(string folderPath)
        {
            if (_mainForm.ExplorerFeature._fileExplorerTree == null || _mainForm.ExplorerFeature._fileExplorerTree.Nodes.Count == 0)
                return;

            TreeNode node = FindTreeNodeByPath(_mainForm.ExplorerFeature._fileExplorerTree.Nodes[0], folderPath);
            if (node == null)
                return;

            PopulateDirectoryNode(node);
            if (!node.IsExpanded)
                node.Expand();
        }

        internal void fileExplorerTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OpenFileExplorerNode(e.Node);
        }

        internal void fileExplorerTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || _mainForm.ExplorerFeature._fileExplorerTree.SelectedNode == null)
                return;

            OpenFileExplorerNode(_mainForm.ExplorerFeature._fileExplorerTree.SelectedNode);
            e.Handled = true;
        }

        private void OpenFileExplorerNode(TreeNode node)
        {
            string path = node?.Tag as string;
            if (string.IsNullOrEmpty(path))
                return;

            if (Directory.Exists(path))
            {
                if (node.IsExpanded)
                    node.Collapse();
                else
                    node.Expand();
                return;
            }

            if (!File.Exists(path))
                return;

            OpenFileFromExplorer(path);
        }

        internal void OpenFileFromExplorer(string filePath)
        {
            try
            {
                if (TabControllerManage.IsFileOpenedInTab(_mainForm.EditorTabControl, filePath))
                {
                    _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                    return;
                }

                var editor = SelectedEditor.GetSelectedEditor();
                bool useCurrentBlankTab = editor != null &&
                    _mainForm.EditorTabControl.SelectedIndex > 0 &&
                    string.IsNullOrWhiteSpace(editor.Text) &&
                    string.IsNullOrWhiteSpace(_mainForm.EditorTabControl.SelectedTab.ToolTipText);

                if (!useCurrentBlankTab)
                    TabControllerManage.AddNewTab(_mainForm.EditorTabControl);

                FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), filePath);
                _mainForm.NuGetFeature.RefreshProjectPackageContext(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
