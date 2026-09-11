using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CIARE.Utils;

namespace CIARE
{
    public partial class MainForm
    {
        private const string FileExplorerStartupProjectKey = "fileExplorerStartupProject";
        private string _fileExplorerStartupProjectPath = string.Empty;

        private void fileExplorerSetStartupProjectMenuItem_Click(object sender, EventArgs e)
        {
            string projectPath = GetProjectPathFromExplorerPath(
                (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string);
            if (!IsProjectFilePath(projectPath))
                return;

            _fileExplorerStartupProjectPath = projectPath;
            SaveStartupProjectPath();
            UpdateFileExplorerStartupProjectHighlight();
            ShowProjectStatus("Startup project set", projectPath, FindNearestBuildFile(
                Path.GetDirectoryName(projectPath), _fileExplorerRootPath, "*.sln"));
        }

        private void RestoreStartupProjectForLoadedFolder()
        {
            if (IsSolutionFilePath(_fileExplorerSolutionPath))
            {
                _fileExplorerStartupProjectPath =
                    SolutionStartupProjectStore.Ensure(_fileExplorerSolutionPath);
                return;
            }

            string startupProjectPath = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", FileExplorerStartupProjectKey);
            _fileExplorerStartupProjectPath = IsProjectFilePath(startupProjectPath) &&
                IsPathInsideFolder(startupProjectPath, _fileExplorerRootPath)
                    ? startupProjectPath
                    : string.Empty;
        }

        private void SaveStartupProjectPath()
        {
            if (IsSolutionFilePath(_fileExplorerSolutionPath))
            {
                SolutionStartupProjectStore.Save(_fileExplorerSolutionPath,
                    _fileExplorerStartupProjectPath ?? string.Empty);
                return;
            }

            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath,
                FileExplorerStartupProjectKey, _fileExplorerStartupProjectPath ?? string.Empty);
        }

        private bool IsStartupProjectPath(string projectPath)
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

        private void ApplyStartupProjectNodeStyle(TreeNode node)
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

        private void UpdateFileExplorerStartupProjectHighlight()
        {
            if (_fileExplorerTree == null || _fileExplorerTree.IsDisposed)
                return;

            _fileExplorerTree.BeginUpdate();
            try
            {
                foreach (TreeNode node in _fileExplorerTree.Nodes)
                    ApplyStartupProjectNodeStyleRecursive(node);
            }
            finally
            {
                _fileExplorerTree.EndUpdate();
            }
        }

        private void ApplyStartupProjectNodeStyleRecursive(TreeNode node)
        {
            ApplyStartupProjectNodeStyle(node);
            foreach (TreeNode child in node.Nodes)
                ApplyStartupProjectNodeStyleRecursive(child);
        }

        private void UpdateStartupProjectAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
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

        private void UpdateLoadedSolutionAfterExplorerRename(string oldPath, string newPath, bool renamedDirectory)
        {
            string renamedSolutionPath = GetRenamedExplorerPath(_fileExplorerSolutionPath, oldPath, newPath,
                renamedDirectory);
            if (IsSolutionFilePath(renamedSolutionPath))
                _fileExplorerSolutionPath = renamedSolutionPath;
        }

        private void ClearStartupProjectAfterExplorerDelete(string deletedPath, bool deletedDirectory)
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
                if (IsSolutionFilePath(_fileExplorerSolutionPath))
                {
                    _fileExplorerStartupProjectPath =
                        SolutionStartupProjectStore.Ensure(_fileExplorerSolutionPath);
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
