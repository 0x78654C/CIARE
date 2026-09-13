using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CIARE.Utils;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectFiles;
using static global::CIARE.Utils.Projects.ProjectPaths;

namespace CIARE.Utils.Projects
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class StartupProject
    {
        private readonly MainForm _mainForm;

        internal StartupProject(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal const string FileExplorerStartupProjectKey = "fileExplorerStartupProject";
        internal string _fileExplorerStartupProjectPath = string.Empty;

        internal void fileExplorerSetStartupProjectMenuItem_Click(object sender, EventArgs e)
        {
            string projectPath = _mainForm.ProjectContextFeature.GetProjectPathFromExplorerPath(
                (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string);
            if (!IsProjectFilePath(projectPath))
                return;

            _fileExplorerStartupProjectPath = projectPath;
            SaveStartupProjectPath();
            UpdateFileExplorerStartupProjectHighlight();
            _mainForm.ProjectCommandsFeature.ShowProjectStatus("Startup project set", projectPath, FindNearestBuildFile(
                Path.GetDirectoryName(projectPath), _mainForm.ExplorerTreeFeature._fileExplorerRootPath, "*.sln"));
        }

        internal void RestoreStartupProjectForLoadedFolder()
        {
            if (IsSolutionFilePath(_mainForm.ProjectContextFeature._fileExplorerSolutionPath))
            {
                _fileExplorerStartupProjectPath =
                    SolutionStartupProjectStore.Ensure(_mainForm.ProjectContextFeature._fileExplorerSolutionPath);
                return;
            }

            string startupProjectPath = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerStartupProjectKey);
            _fileExplorerStartupProjectPath = IsProjectFilePath(startupProjectPath) &&
                IsPathInsideFolder(startupProjectPath, _mainForm.ExplorerTreeFeature._fileExplorerRootPath)
                    ? startupProjectPath
                    : string.Empty;
        }

        private void SaveStartupProjectPath()
        {
            if (IsSolutionFilePath(_mainForm.ProjectContextFeature._fileExplorerSolutionPath))
            {
                SolutionStartupProjectStore.Save(_mainForm.ProjectContextFeature._fileExplorerSolutionPath,
                    _fileExplorerStartupProjectPath ?? string.Empty);
                return;
            }

            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath,
                FileExplorerStartupProjectKey, _fileExplorerStartupProjectPath ?? string.Empty);
        }

        internal bool IsStartupProjectPath(string projectPath)
        {
            return IsProjectFilePath(projectPath) &&
                IsProjectFilePath(_fileExplorerStartupProjectPath) &&
                string.Equals(NormalizeCompletionPath(projectPath),
                    NormalizeCompletionPath(_fileExplorerStartupProjectPath),
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool IsStartupProjectNode(TreeNode node)
        {
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) ||
                !IsProjectFilePath(_fileExplorerStartupProjectPath))
                return false;

            string startupDirectory = Path.GetDirectoryName(_fileExplorerStartupProjectPath);
            return !string.IsNullOrEmpty(startupDirectory) &&
                string.Equals(NormalizeCompletionPath(path), NormalizeCompletionPath(startupDirectory),
                    StringComparison.OrdinalIgnoreCase);
        }

        internal void ApplyStartupProjectNodeStyle(TreeNode node)
        {
            if (node == null)
                return;

            if (IsStartupProjectNode(node))
            {
                node.BackColor = GlobalVariables.darkColor
                    ? Color.FromArgb(70, 72, 38)
                    : Color.FromArgb(255, 245, 189);
                node.ForeColor = GlobalVariables.darkColor
                    ? Color.FromArgb(255, 235, 150)
                    : Color.FromArgb(80, 63, 0);
                if (!node.ToolTipText.EndsWith("Startup project", StringComparison.Ordinal))
                    node.ToolTipText = node.ToolTipText + Environment.NewLine + "Startup project";
            }
            else
            {
                node.BackColor = Color.Empty;
                node.ForeColor = Color.Empty;
                string marker = Environment.NewLine + "Startup project";
                if (!string.IsNullOrEmpty(node.ToolTipText) &&
                    node.ToolTipText.EndsWith(marker, StringComparison.Ordinal))
                {
                    node.ToolTipText = node.ToolTipText.Substring(0, node.ToolTipText.Length - marker.Length);
                }
            }
        }

        internal void UpdateFileExplorerStartupProjectHighlight()
        {
            if (_mainForm.ExplorerFeature._fileExplorerTree == null || _mainForm.ExplorerFeature._fileExplorerTree.IsDisposed)
                return;

            _mainForm.ExplorerFeature._fileExplorerTree.BeginUpdate();
            try
            {
                foreach (TreeNode node in _mainForm.ExplorerFeature._fileExplorerTree.Nodes)
                    ApplyStartupProjectNodeStyleRecursive(node);
            }
            finally
            {
                _mainForm.ExplorerFeature._fileExplorerTree.EndUpdate();
            }
        }

        private void ApplyStartupProjectNodeStyleRecursive(TreeNode node)
        {
            ApplyStartupProjectNodeStyle(node);
            foreach (TreeNode child in node.Nodes)
                ApplyStartupProjectNodeStyleRecursive(child);
        }

        internal void UpdateStartupProjectAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            if (string.IsNullOrWhiteSpace(_fileExplorerStartupProjectPath) ||
                !string.Equals(Path.GetExtension(_fileExplorerStartupProjectPath), ".csproj",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string renamedStartupProject = GetRenamedExplorerPath(_fileExplorerStartupProjectPath, oldPath, newPath,
                renamedDirectory);
            if (!string.IsNullOrEmpty(renamedStartupProject))
            {
                _fileExplorerStartupProjectPath = renamedStartupProject;
                SaveStartupProjectPath();
            }
        }

        internal void UpdateLoadedSolutionAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            string renamedSolutionPath = GetRenamedExplorerPath(_mainForm.ProjectContextFeature._fileExplorerSolutionPath, oldPath, newPath,
                renamedDirectory);
            if (IsSolutionFilePath(renamedSolutionPath))
                _mainForm.ProjectContextFeature._fileExplorerSolutionPath = renamedSolutionPath;
        }

        internal void ClearStartupProjectAfterExplorerDelete(string deletedPath, bool deletedDirectory)
        {
            if (string.IsNullOrWhiteSpace(_fileExplorerStartupProjectPath) ||
                !string.Equals(Path.GetExtension(_fileExplorerStartupProjectPath), ".csproj",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool deletedStartupProject = deletedDirectory
                ? IsSameOrChildDirectory(_fileExplorerStartupProjectPath, deletedPath)
                : string.Equals(NormalizeCompletionPath(_fileExplorerStartupProjectPath),
                    NormalizeCompletionPath(deletedPath), StringComparison.OrdinalIgnoreCase);

            if (deletedStartupProject)
            {
                if (IsSolutionFilePath(_mainForm.ProjectContextFeature._fileExplorerSolutionPath))
                {
                    _fileExplorerStartupProjectPath =
                        SolutionStartupProjectStore.Ensure(_mainForm.ProjectContextFeature._fileExplorerSolutionPath);
                }
                else
                {
                    _fileExplorerStartupProjectPath = string.Empty;
                    SaveStartupProjectPath();
                }
            }
        }
    }
}
