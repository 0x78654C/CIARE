using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectFiles;

namespace CIARE.Utils.Projects;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ProjectBuild
{
    private readonly MainForm _mainForm;

    internal ProjectBuild(MainForm mainForm)
    {
        _mainForm = mainForm;
    }
    internal bool _explorerProjectBuildRunning;

    internal static string GetExplorerBuildProjectPath(string path)
    {
        if (IsProjectFilePath(path)) return path;
        if (!Directory.Exists(path)) return string.Empty;
        try
        {
            string[] projects = Directory.EnumerateFiles(path, "*.csproj").Take(2).ToArray();
            return projects.Length == 1 ? projects[0] : string.Empty;
        }
        catch (IOException) { return string.Empty; }
        catch (UnauthorizedAccessException) { return string.Empty; }
    }

    internal async void fileExplorerBuildProjectMenuItem_Click(object sender, EventArgs e)
    {
        string path = (_mainForm.ExplorerFeature._fileExplorerContextNode ?? _mainForm.ExplorerFeature._fileExplorerTree?.SelectedNode)?.Tag as string;
        await BuildExplorerProjectAsync(GetExplorerBuildProjectPath(path));
    }

    internal async Task BuildExplorerProjectAsync(string projectPath)
    {
        if (_explorerProjectBuildRunning || !IsProjectFilePath(projectPath)) return;
        _explorerProjectBuildRunning = true;
        _mainForm.ExplorerFeature._fileExplorerBuildProjectMenuItem.Enabled = false;
        try
        {
            if (_mainForm.EditorFeature._pendingEditorTextRefresh) _mainForm.EditorFeature.OnEditorTextRefreshTimer(_mainForm, EventArgs.Empty);
            SaveOpenProjectFilesForBuild(projectPath);
            _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
            OutputWindowManage.ShowOutputWindow(_mainForm.splitContainer1, _mainForm.outputRBT);
            _mainForm.outputRBT.ForeColor = GlobalVariables.darkColor ? Color.FromArgb(192, 215, 207) : Color.Black;
            _mainForm.outputRBT.Text = $"Building project: {projectPath}\n";
            ProcessRunResult result = await RoslynRun.BuildSingleProjectAsync(projectPath);
            if (_mainForm.IsDisposed || _mainForm.Disposing || _mainForm.outputRBT.IsDisposed) return;
            _mainForm.outputRBT.Text = $"Project: {projectPath}\n{result.Output.TrimEnd()}\n" +
                (result.Success ? "Build completed." : $"Build failed (exit code {result.ExitCode}).");
            _mainForm.outputRBT.SelectionStart = _mainForm.outputRBT.TextLength;
            _mainForm.outputRBT.ScrollToCaret();
        }
        catch (Exception exception)
        {
            if (!_mainForm.IsDisposed && !_mainForm.Disposing && !_mainForm.outputRBT.IsDisposed)
                _mainForm.outputRBT.Text = $"Build failed: {exception.Message}";
        }
        finally
        {
            _explorerProjectBuildRunning = false;
            if (!_mainForm.IsDisposed && !_mainForm.Disposing) _mainForm.ExplorerFeature._fileExplorerBuildProjectMenuItem.Enabled = true;
        }
    }

    private void SaveOpenProjectFilesForBuild(string projectPath)
    {
        foreach (TabPage page in _mainForm.EditorTabControl.TabPages)
        {
            string path = page.ToolTipText;
            if (!page.Text.StartsWith("*", StringComparison.Ordinal) || !File.Exists(path)) continue;
            bool belongsToProject = string.Equals(path, projectPath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_mainForm.ProjectContextFeature.GetProjectPathFromExplorerPath(path), projectPath, StringComparison.OrdinalIgnoreCase) ||
                (IsCSharpFilePath(path) && ProjectContainsSourceFile(projectPath, path));
            if (!belongsToProject) continue;
            TextEditorControl editor = page.Controls.OfType<TextEditorControl>().FirstOrDefault();
            if (editor == null) continue;
            File.WriteAllText(path, editor.Text);
            page.Text = page.Text.TrimStart('*');
            TabControllerManage.StoreFileMD5(path, GlobalVariables.userProfileDirectory,
                GlobalVariables.tabsFilePath, _mainForm.EditorTabControl.TabPages.IndexOf(page));
            if (page == _mainForm.EditorTabControl.SelectedTab)
            {
                FileManage.SetFileMD5(path);
                _mainForm.Text = _mainForm.Text.TrimStart('*');
            }
        }
    }
}
