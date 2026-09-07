using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE;

public partial class MainForm
{
    private bool _explorerProjectBuildRunning;

    private static string GetExplorerBuildProjectPath(string path)
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

    private async void fileExplorerBuildProjectMenuItem_Click(object sender, EventArgs e)
    {
        string path = (_fileExplorerContextNode ?? _fileExplorerTree?.SelectedNode)?.Tag as string;
        await BuildExplorerProjectAsync(GetExplorerBuildProjectPath(path));
    }

    internal async Task BuildExplorerProjectAsync(string projectPath)
    {
        if (_explorerProjectBuildRunning || !IsProjectFilePath(projectPath)) return;
        _explorerProjectBuildRunning = true;
        _fileExplorerBuildProjectMenuItem.Enabled = false;
        try
        {
            if (_pendingEditorTextRefresh) OnEditorTextRefreshTimer(this, EventArgs.Empty);
            SaveOpenProjectFilesForBuild(projectPath);
            outputTabControl.SelectedTab = outputTabPage;
            OutputWindowManage.ShowOutputWindow(splitContainer1, outputRBT);
            outputRBT.ForeColor = GlobalVariables.darkColor ? Color.FromArgb(192, 215, 207) : Color.Black;
            outputRBT.Text = $"Building project: {projectPath}\n";
            ProcessRunResult result = await RoslynRun.BuildSingleProjectAsync(projectPath);
            if (IsDisposed || Disposing || outputRBT.IsDisposed) return;
            outputRBT.Text = $"Project: {projectPath}\n{result.Output.TrimEnd()}\n" +
                (result.Success ? "Build completed." : $"Build failed (exit code {result.ExitCode}).");
            outputRBT.SelectionStart = outputRBT.TextLength;
            outputRBT.ScrollToCaret();
        }
        catch (Exception exception)
        {
            if (!IsDisposed && !Disposing && !outputRBT.IsDisposed)
                outputRBT.Text = $"Build failed: {exception.Message}";
        }
        finally
        {
            _explorerProjectBuildRunning = false;
            if (!IsDisposed && !Disposing) _fileExplorerBuildProjectMenuItem.Enabled = true;
        }
    }

    private void SaveOpenProjectFilesForBuild(string projectPath)
    {
        foreach (TabPage page in EditorTabControl.TabPages)
        {
            string path = page.ToolTipText;
            if (!page.Text.StartsWith("*", StringComparison.Ordinal) || !File.Exists(path)) continue;
            bool belongsToProject = string.Equals(path, projectPath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetProjectPathFromExplorerPath(path), projectPath, StringComparison.OrdinalIgnoreCase) ||
                (IsCSharpFilePath(path) && ProjectContainsSourceFile(projectPath, path));
            if (!belongsToProject) continue;
            TextEditorControl editor = page.Controls.OfType<TextEditorControl>().FirstOrDefault();
            if (editor == null) continue;
            File.WriteAllText(path, editor.Text);
            page.Text = page.Text.TrimStart('*');
            TabControllerManage.StoreFileMD5(path, GlobalVariables.userProfileDirectory,
                GlobalVariables.tabsFilePath, EditorTabControl.TabPages.IndexOf(page));
            if (page == EditorTabControl.SelectedTab)
            {
                FileManage.SetFileMD5(path);
                Text = Text.TrimStart('*');
            }
        }
    }
}
