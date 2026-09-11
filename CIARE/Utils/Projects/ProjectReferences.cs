using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using Button = System.Windows.Forms.Button;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectFiles;

namespace CIARE.Utils.Projects
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ProjectReferences
    {
        private readonly MainForm _mainForm;

        internal ProjectReferences(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal void fileExplorerAddProjectMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string;
            string solutionPath = _mainForm.ProjectContextFeature.GetSolutionPathFromExplorerPath(contextPath);
            if (string.IsNullOrEmpty(solutionPath))
                return;

            if (!IsAddProjectToSolutionContext(contextPath, solutionPath))
                return;

            using (var dialog = new NewProject(solutionPath))
            {
                if (dialog.ShowDialog(_mainForm) != DialogResult.OK)
                    return;

                NewProjectResult project = dialog.CreatedProject;
                if (project == null)
                    return;

                _mainForm.ProjectContextFeature._fileExplorerSolutionPath = solutionPath;
                _mainForm.StartupProjectFeature._fileExplorerStartupProjectPath =
                    SolutionStartupProjectStore.Ensure(solutionPath, project.ProjectFilePath);

                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                string projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
                if (!string.IsNullOrEmpty(solutionDirectory))
                    _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(solutionDirectory);
                if (!string.IsNullOrEmpty(projectDirectory))
                    _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(projectDirectory);

                _mainForm.ExplorerTreeFeature.SelectExplorerPath(project.ProjectFilePath);
                _mainForm.StartupProjectFeature.UpdateFileExplorerStartupProjectHighlight();
                if (File.Exists(project.StarterFilePath))
                    _mainForm.ExplorerTreeFeature.OpenFileFromExplorer(project.StarterFilePath);

                _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
                RealTimeChecker.InvalidateReferenceCache();
                _mainForm.NuGetFeature.RefreshProjectPackageContext(project.ProjectFilePath, restoreProject: true,
                    showRestoreFailure: false);
                _mainForm.ProjectCommandsFeature.ShowProjectStatus("Added project to solution", project.ProjectFilePath, solutionPath);
            }
        }

        internal void fileExplorerAddProjectReferenceMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string;
            string projectPath = _mainForm.ProjectContextFeature.GetProjectPathFromExplorerPath(contextPath);
            if (!IsProjectFilePath(projectPath))
                return;

            string solutionPath = _mainForm.ProjectContextFeature.GetSolutionPathFromExplorerPath(contextPath);
            var candidates = ProjectReferenceManager.GetReferenceableProjects(projectPath, solutionPath,
                _mainForm.ExplorerTreeFeature._fileExplorerRootPath);
            if (candidates.Count == 0)
            {
                MessageBox.Show("No available project references were found in this solution.",
                    "Add Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string referencedProjectPath = ShowProjectReferencePicker(projectPath, candidates);
            if (string.IsNullOrEmpty(referencedProjectPath))
                return;

            if (!ProjectReferenceManager.AddProjectReference(projectPath, referencedProjectPath, out string message))
            {
                MessageBox.Show(message, "Add Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(projectDirectory))
                _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(projectDirectory);

            _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
            RealTimeChecker.InvalidateReferenceCache();
            _mainForm.NuGetFeature.RefreshProjectPackageContext(projectPath, restoreProject: false, showRestoreFailure: false);
            _mainForm.ProjectCommandsFeature.ShowProjectStatus("Added project reference", projectPath, solutionPath, referencedProjectPath);
        }

        internal void fileExplorerRemoveProjectReferenceMenuItem_Click(object sender, EventArgs e)
        {
            string contextPath = (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string;
            string projectPath = _mainForm.ProjectContextFeature.GetProjectPathFromExplorerPath(contextPath);
            if (!IsProjectFilePath(projectPath))
                return;

            var references = ProjectReferenceManager.GetProjectReferences(projectPath);
            if (references.Count == 0)
            {
                MessageBox.Show("No project references were found in this project.",
                    "Remove Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string referencedProjectPath = ShowProjectReferencePicker(projectPath, references,
                "Remove Project Reference", "Remove");
            if (string.IsNullOrEmpty(referencedProjectPath))
                return;

            DialogResult dialog = MessageBox.Show(
                $"Remove project reference {Path.GetFileNameWithoutExtension(referencedProjectPath)} from {Path.GetFileName(projectPath)}?",
                "Remove Project Reference", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectReferenceManager.RemoveProjectReference(projectPath, referencedProjectPath, out string message))
            {
                MessageBox.Show(message, "Remove Project Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(projectDirectory))
                _mainForm.ExplorerTreeFeature.RefreshAndExpandExplorerFolder(projectDirectory);

            _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
            RealTimeChecker.InvalidateReferenceCache();
            _mainForm.NuGetFeature.RefreshProjectPackageContext(projectPath, restoreProject: false, showRestoreFailure: false);
            _mainForm.ProjectCommandsFeature.ShowProjectStatus("Removed project reference", projectPath, _mainForm.ProjectContextFeature.GetSolutionPathFromExplorerPath(contextPath),
                referencedProjectPath);
        }

        private string ShowProjectReferencePicker(string projectPath, IList<string> candidateProjects,
            string title = "Add Project Reference", string actionText = "Add")
        {
            using (var dialog = new Form())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(560, 340);
                dialog.MinimumSize = new Size(460, 280);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.Font = SystemFonts.MessageBoxFont;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10)
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

                var targetLabel = new Label
                {
                    AutoEllipsis = true,
                    Dock = DockStyle.Fill,
                    Text = "Target: " + Path.GetFileNameWithoutExtension(projectPath),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var projectList = new ListBox
                {
                    Dock = DockStyle.Fill,
                    IntegralHeight = false,
                    HorizontalScrollbar = true
                };
                foreach (string candidate in candidateProjects)
                    projectList.Items.Add(new ProjectReferenceListItem(candidate, _mainForm.ExplorerTreeFeature._fileExplorerRootPath));
                if (projectList.Items.Count > 0)
                    projectList.SelectedIndex = 0;

                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(0, 8, 0, 0)
                };
                var addButton = new Button
                {
                    Text = actionText,
                    Width = 92,
                    Height = 28,
                    DialogResult = DialogResult.OK
                };
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Width = 92,
                    Height = 28,
                    DialogResult = DialogResult.Cancel
                };
                buttons.Controls.Add(addButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(targetLabel, 0, 0);
                layout.Controls.Add(projectList, 0, 1);
                layout.Controls.Add(buttons, 0, 2);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = addButton;
                dialog.CancelButton = cancelButton;
                projectList.DoubleClick += (_, _) =>
                {
                    if (projectList.SelectedItem != null)
                        dialog.DialogResult = DialogResult.OK;
                };

                FrmColorMod.ToogleColorMode(dialog, GlobalVariables.darkColor);

                if (dialog.ShowDialog(_mainForm) != DialogResult.OK)
                    return string.Empty;

                return (projectList.SelectedItem as ProjectReferenceListItem)?.ProjectPath ?? string.Empty;
            }
        }

        private sealed class ProjectReferenceListItem
        {
            private readonly string _workspaceFolder;

            public ProjectReferenceListItem(string projectPath, string workspaceFolder)
            {
                ProjectPath = projectPath;
                _workspaceFolder = workspaceFolder;
            }

            public string ProjectPath { get; }

            public override string ToString()
            {
                string name = Path.GetFileNameWithoutExtension(ProjectPath);
                string displayPath = ProjectPath;
                try
                {
                    if (Directory.Exists(_workspaceFolder))
                        displayPath = Path.GetRelativePath(_workspaceFolder, ProjectPath);
                }
                catch
                {
                }

                return $"{name} ({displayPath})";
            }
        }
    }
}
