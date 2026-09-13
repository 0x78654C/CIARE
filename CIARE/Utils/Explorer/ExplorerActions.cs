using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using Button = System.Windows.Forms.Button;
using VBFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using VBRecycleOption = Microsoft.VisualBasic.FileIO.RecycleOption;
using VBUIOption = Microsoft.VisualBasic.FileIO.UIOption;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Explorer.ExplorerTree;
using static global::CIARE.Utils.Projects.ProjectBuild;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectPaths;

namespace CIARE.Utils.Explorer
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ExplorerActions
    {
        private readonly MainForm _mainForm;

        internal ExplorerActions(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal void fileExplorerContextMenu_Opening(object sender, CancelEventArgs e)
        {
            TreeNode node = _mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || Equals(path, FileExplorerLoadingTag))
            {
                e.Cancel = true;
                return;
            }

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
            {
                e.Cancel = true;
                return;
            }

            string solutionPath = _mainForm.ProjectContextFeature.GetSolutionPathFromExplorerPath(path);
            string projectPath = _mainForm.ProjectContextFeature.GetProjectPathFromExplorerPath(path);
            bool hasSolutionContext = !string.IsNullOrEmpty(solutionPath);
            bool canAddProjectToSolution = hasSolutionContext &&
                IsAddProjectToSolutionContext(path, solutionPath);
            bool hasProjectContext = !string.IsNullOrEmpty(projectPath);
            _mainForm.ExplorerFeature._fileExplorerBuildProjectMenuItem.Visible = !string.IsNullOrEmpty(GetExplorerBuildProjectPath(path));
            _mainForm.ExplorerFeature._fileExplorerBuildProjectMenuItem.Enabled = !_mainForm.ProjectBuildFeature._explorerProjectBuildRunning;
            bool hasProjectReferenceCandidates = hasProjectContext &&
                ProjectReferenceManager.GetReferenceableProjects(projectPath, solutionPath,
                    _mainForm.ExplorerTreeFeature._fileExplorerRootPath).Count > 0;
            bool hasProjectReferences = hasProjectContext &&
                ProjectReferenceManager.GetProjectReferences(projectPath).Count > 0;

            _mainForm.ExplorerFeature._fileExplorerAddProjectMenuItem.Visible = hasSolutionContext;
            _mainForm.ExplorerFeature._fileExplorerAddProjectMenuItem.Enabled = canAddProjectToSolution;
            _mainForm.ExplorerFeature._fileExplorerAddProjectReferenceMenuItem.Visible = hasProjectContext;
            _mainForm.ExplorerFeature._fileExplorerAddProjectReferenceMenuItem.Enabled = hasProjectReferenceCandidates;
            _mainForm.ExplorerFeature._fileExplorerRemoveProjectReferenceMenuItem.Visible = hasProjectContext;
            _mainForm.ExplorerFeature._fileExplorerRemoveProjectReferenceMenuItem.Enabled = hasProjectReferences;
            _mainForm.ExplorerFeature._fileExplorerSetStartupProjectMenuItem.Visible = hasProjectContext;
            _mainForm.ExplorerFeature._fileExplorerSetStartupProjectMenuItem.Enabled = hasProjectContext;
            _mainForm.ExplorerFeature._fileExplorerSetStartupProjectMenuItem.Checked = hasProjectContext &&
                _mainForm.StartupProjectFeature.IsStartupProjectPath(projectPath);
            _mainForm.ExplorerFeature._fileExplorerProjectSeparator.Visible = hasSolutionContext || hasProjectContext;
            _mainForm.ExplorerFeature._fileExplorerNewFileMenuItem.Visible = isDirectory;
            _mainForm.ExplorerFeature._fileExplorerNewFolderMenuItem.Visible = isDirectory;
            _mainForm.ExplorerFeature._fileExplorerContextSeparator.Visible = isDirectory;
            _mainForm.ExplorerFeature._fileExplorerRenameMenuItem.Visible = isDirectory || isFile;
            _mainForm.ExplorerFeature._fileExplorerRenameMenuItem.Enabled = !_mainForm.ExplorerTreeFeature.IsExplorerRootPath(path);
            _mainForm.ExplorerFeature._fileExplorerDeleteMenuItem.Visible = isDirectory || isFile;
            _mainForm.ExplorerFeature._fileExplorerDeleteMenuItem.Enabled = !_mainForm.ExplorerTreeFeature.IsExplorerRootPath(path);
        }

        internal void fileExplorerNewFileMenuItem_Click(object sender, EventArgs e)
        {
            string folderPath = _mainForm.ExplorerTreeFeature.GetExplorerContextFolderPath();
            if (string.IsNullOrEmpty(folderPath))
                return;

            string fileName = PromptForExplorerName("New C# File", "File name:", GetUniqueExplorerName(folderPath, "Class1.cs"));
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            if (!TryNormalizeExplorerFileName(fileName, ".cs", out fileName))
                return;

            string filePath = Path.Combine(folderPath, fileName);
            if (File.Exists(filePath) || Directory.Exists(filePath))
            {
                MessageBox.Show("An item with that name already exists.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                File.WriteAllText(filePath, BuildNewCSharpFileContent(fileName));
                _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(folderPath);
                _mainForm.ExplorerTreeFeature.SelectExplorerPath(filePath);
                _mainForm.ExplorerTreeFeature.OpenFileFromExplorer(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "New C# File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal void fileExplorerNewFolderMenuItem_Click(object sender, EventArgs e)
        {
            string folderPath = _mainForm.ExplorerTreeFeature.GetExplorerContextFolderPath();
            if (string.IsNullOrEmpty(folderPath))
                return;

            string folderName = PromptForExplorerName("New Folder", "Folder name:", GetUniqueExplorerName(folderPath, "New Folder"));
            if (string.IsNullOrWhiteSpace(folderName))
                return;

            if (!TryValidateExplorerItemName(folderName, out folderName))
                return;

            string newFolderPath = Path.Combine(folderPath, folderName);
            if (Directory.Exists(newFolderPath) || File.Exists(newFolderPath))
            {
                MessageBox.Show("An item with that name already exists.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(newFolderPath);
                _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(folderPath);
                _mainForm.ExplorerTreeFeature.SelectExplorerPath(newFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "New Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal void fileExplorerRenameMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = _mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || _mainForm.ExplorerTreeFeature.IsExplorerRootPath(path))
                return;

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
                return;

            if (!_mainForm.ExplorerTreeFeature.IsPathInsideExplorerRoot(path))
            {
                MessageBox.Show("This item is outside the opened explorer folder.", "Rename",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sourcePath = isDirectory
                ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : path;
            string currentName = Path.GetFileName(sourcePath);
            string newName = PromptForExplorerName("Rename", "Name:", currentName);
            if (string.IsNullOrWhiteSpace(newName))
                return;

            if (!TryValidateExplorerItemName(newName, out newName))
                return;

            if (string.Equals(currentName, newName, StringComparison.Ordinal))
                return;

            string parentPath = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(parentPath))
                return;

            string newPath = Path.Combine(parentPath, newName);
            bool samePathIgnoreCase = string.Equals(NormalizeCompletionPath(path),
                NormalizeCompletionPath(newPath), StringComparison.OrdinalIgnoreCase);
            if (!samePathIgnoreCase && (File.Exists(newPath) || Directory.Exists(newPath)))
            {
                MessageBox.Show("An item with that name already exists.", "Rename",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                MoveExplorerItem(path, newPath, isDirectory);
                _mainForm.StartupProjectFeature.UpdateLoadedSolutionAfterExplorerRename(path, newPath, isDirectory);
                _mainForm.StartupProjectFeature.UpdateStartupProjectAfterExplorerRename(path, newPath, isDirectory);
                _mainForm.ProjectPathsFeature.UpdateProjectReferencesAfterExplorerRename(path, newPath, isDirectory);
                _mainForm.TabsFeature.UpdateOpenTabsAfterExplorerRename(path, newPath, isDirectory);

                _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(parentPath);
                _mainForm.ExplorerTreeFeature.SelectExplorerPath(newPath);
                _mainForm.StartupProjectFeature.UpdateFileExplorerStartupProjectHighlight();

                _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
                RealTimeChecker.InvalidateReferenceCache();
                _mainForm.NuGetFeature.RefreshProjectPackageContext(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
                _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Rename", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal void fileExplorerDeleteMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = _mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode;
            string path = node?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || _mainForm.ExplorerTreeFeature.IsExplorerRootPath(path))
                return;

            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
                return;

            if (!_mainForm.ExplorerTreeFeature.IsPathInsideExplorerRoot(path))
            {
                MessageBox.Show("This item is outside the opened explorer folder.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string itemType = isDirectory ? "folder" : "file";
            DialogResult dialog = MessageBox.Show(
                $"Delete {itemType} '{Path.GetFileName(path)}'?",
                "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            string parentPath = Path.GetDirectoryName(path);
            List<string> deletedProjectPaths = GetExplorerDeletedProjectPaths(path, isDirectory);
            try
            {
                if (isDirectory)
                {
                    VBFileSystem.DeleteDirectory(path, VBUIOption.OnlyErrorDialogs,
                        VBRecycleOption.SendToRecycleBin);
                }
                else
                {
                    VBFileSystem.DeleteFile(path, VBUIOption.OnlyErrorDialogs,
                        VBRecycleOption.SendToRecycleBin);
                }

                RemoveProjectsFromWorkspaceSolutions(deletedProjectPaths, _mainForm.ExplorerTreeFeature._fileExplorerRootPath);
                _mainForm.StartupProjectFeature.ClearStartupProjectAfterExplorerDelete(path, isDirectory);
                if (!string.IsNullOrEmpty(parentPath))
                    _mainForm.ExplorerWatcherFeature.RefreshExplorerNodeForPath(parentPath);
                _mainForm.StartupProjectFeature.UpdateFileExplorerStartupProjectHighlight();
                _mainForm.NuGetFeature.RefreshProjectPackageContext(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(), restoreProject: false,
                    showRestoreFailure: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void MoveExplorerItem(string path, string newPath, bool isDirectory)
        {
            string normalizedPath = NormalizeCompletionPath(path);
            string normalizedNewPath = NormalizeCompletionPath(newPath);
            bool isCaseOnlyRename = string.Equals(normalizedPath, normalizedNewPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedPath, normalizedNewPath, StringComparison.Ordinal);

            if (!isCaseOnlyRename)
            {
                if (isDirectory)
                    Directory.Move(path, newPath);
                else
                    File.Move(path, newPath);
                return;
            }

            string parentPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrEmpty(parentPath))
                return;

            string tempPath;
            do
            {
                tempPath = Path.Combine(parentPath, ".ciare-rename-" + Guid.NewGuid().ToString("N"));
            }
            while (File.Exists(tempPath) || Directory.Exists(tempPath));

            if (isDirectory)
            {
                Directory.Move(path, tempPath);
                Directory.Move(tempPath, newPath);
            }
            else
            {
                File.Move(path, tempPath);
                File.Move(tempPath, newPath);
            }
        }

        private static bool TryNormalizeExplorerFileName(string value, string extension, out string fileName)
        {
            if (!TryValidateExplorerItemName(value, out fileName))
                return false;

            if (!string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase))
                fileName = Path.ChangeExtension(fileName, extension);

            return true;
        }

        private static bool TryValidateExplorerItemName(string value, out string itemName)
        {
            itemName = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(itemName))
                return false;

            if (itemName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("The name contains characters that cannot be used in a file name.", "Explorer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static string GetUniqueExplorerName(string folderPath, string preferredName)
        {
            string candidate = preferredName;
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(preferredName);
            string extension = Path.GetExtension(preferredName);
            int count = 1;

            while (File.Exists(Path.Combine(folderPath, candidate)) ||
                   Directory.Exists(Path.Combine(folderPath, candidate)))
            {
                count++;
                candidate = string.IsNullOrEmpty(extension)
                    ? $"{preferredName} {count}"
                    : $"{nameWithoutExtension}{count}{extension}";
            }

            return candidate;
        }

        private static string BuildNewCSharpFileContent(string fileName)
        {
            string className = Path.GetFileNameWithoutExtension(fileName);
            className = ToSafeCSharpIdentifier(className);

            return "public class " + className + Environment.NewLine +
                   "{" + Environment.NewLine +
                   "}" + Environment.NewLine;
        }

        private static string ToSafeCSharpIdentifier(string value)
        {
            var builder = new StringBuilder();
            foreach (char ch in value ?? string.Empty)
                builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private string PromptForExplorerName(string title, string labelText, string defaultValue)
        {
            using (var form = new Form())
            using (var label = new Label())
            using (var textBox = new TextBox())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(360, 118);
                form.Font = _mainForm.Font;

                label.AutoSize = true;
                label.Text = labelText;
                label.Location = new Point(12, 14);

                textBox.Text = defaultValue;
                textBox.Location = new Point(12, 38);
                textBox.Width = 336;
                textBox.SelectAll();

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Location = new Point(156, 78);
                okButton.Width = 92;

                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.Location = new Point(256, 78);
                cancelButton.Width = 92;

                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                FrmColorMod.ToogleColorMode(form, GlobalVariables.darkColor);

                return form.ShowDialog(_mainForm) == DialogResult.OK
                    ? textBox.Text.Trim()
                    : string.Empty;
            }
        }
    }
}
