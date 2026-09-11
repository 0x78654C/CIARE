using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private void newProjectStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new NewProject())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                OpenCreatedProject(dialog.CreatedProject);
            }
        }

        private void openProjectStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenProjectOrSolutionDialog();
        }

        private void OpenCreatedProject(NewProjectResult project)
        {
            if (project == null)
                return;

            string projectOrSolutionPath = !string.IsNullOrEmpty(project.SolutionFilePath)
                ? project.SolutionFilePath
                : project.ProjectFilePath;

            if (!OpenProjectOrSolutionPath(projectOrSolutionPath, showMessage: false))
                return;

            if (File.Exists(project.StarterFilePath))
                OpenFileFromExplorer(project.StarterFilePath);

            ShowProjectStatus("Created project", project.ProjectFilePath, project.SolutionFilePath);
        }

        private void OpenProjectOrSolutionDialog()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter =
                    "Solution or C# Project (*.sln;*.csproj)|*.sln;*.csproj|Solution Files (*.sln)|*.sln|C# Project Files (*.csproj)|*.csproj|All Files (*.*)|*.*";
                dialog.Title = "Open Project or Solution";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.InitialDirectory = GetInitialProjectDialogDirectory();

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    OpenProjectOrSolutionPath(dialog.FileName, showMessage: true);
            }
        }

        private string GetInitialProjectDialogDirectory()
        {
            if (Directory.Exists(_fileExplorerRootPath))
                return _fileExplorerRootPath;

            string filePath = GetActiveEditorFilePath();
            if (File.Exists(filePath))
                return Path.GetDirectoryName(filePath);

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private bool OpenProjectOrSolutionPath(string filePath, bool showMessage)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            if (!string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Select a .sln or .csproj file.", "Open Project/Solution",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string folderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return false;

            string containingSolutionPath = string.Empty;
            if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                containingSolutionPath = FindContainingSolutionForProject(filePath);
                string solutionDirectory = Path.GetDirectoryName(containingSolutionPath);
                if (!string.IsNullOrEmpty(solutionDirectory) && Directory.Exists(solutionDirectory))
                    folderPath = solutionDirectory;
            }

            string loadedSolutionPath = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
                ? filePath
                : containingSolutionPath;
            LoadFileExplorerFolder(folderPath, loadedSolutionPath);
            ToggleFileExplorer(true);

            if (showMessage)
            {
                string projectPath = string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                    ? filePath
                    : GetActivePackageProjectPath();
                string solutionPath = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
                    ? filePath
                    : containingSolutionPath;
                ShowProjectStatus("Opened project/solution", projectPath, solutionPath);
            }

            return true;
        }

        private static string FindContainingSolutionForProject(string projectPath)
        {
            if (!IsProjectFilePath(projectPath))
                return string.Empty;

            string folder = Path.GetDirectoryName(projectPath);
            while (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                try
                {
                    foreach (string solutionPath in Directory.GetFiles(folder, "*.sln", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    {
                        if (SolutionFileContainsProject(solutionPath, projectPath))
                            return solutionPath;
                    }
                }
                catch
                {
                }

                string parent = Path.GetDirectoryName(folder);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase))
                    break;

                folder = parent;
            }

            return string.Empty;
        }

        private static bool SolutionFileContainsProject(string solutionPath, string projectPath)
        {
            if (!IsSolutionFilePath(solutionPath) || !IsProjectFilePath(projectPath))
                return false;

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(solutionDirectory))
                return false;

            string normalizedProject = NormalizeCompletionPath(projectPath);
            try
            {
                foreach (string line in File.ReadLines(solutionPath))
                {
                    Match match = Regex.Match(line,
                        @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (!match.Success)
                        continue;

                    string referencedProject = match.Groups[1].Value;
                    if (!Path.IsPathRooted(referencedProject))
                        referencedProject = Path.GetFullPath(Path.Combine(solutionDirectory, referencedProject));

                    if (string.Equals(NormalizeCompletionPath(referencedProject), normalizedProject,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private void ShowProjectStatus(string action, string projectPath, string solutionPath,
            string referencePath = null)
        {
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;

            var lines = new List<string> { action + "." };
            if (!string.IsNullOrEmpty(solutionPath))
                lines.Add("Solution: " + solutionPath);
            if (!string.IsNullOrEmpty(projectPath))
                lines.Add("Project: " + projectPath);
            if (!string.IsNullOrEmpty(referencePath))
                lines.Add("Reference: " + referencePath);

            outputRBT.Text = string.Join(Environment.NewLine, lines);
        }
    }
}
