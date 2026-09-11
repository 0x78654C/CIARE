using CIARE.Roslyn;
using CIARE.GUI;
using CIARE.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CIARE;

internal static class ProjectBuildRegression
{
    public static void Run(MainForm form, Action<bool, string> assert)
    {
        string root = Path.Combine(GlobalVariables.userProfileDirectory, "Build solution");
        string chosen = Path.Combine(root, "Chosen Project");
        string other = Path.Combine(root, "Other");
        Directory.CreateDirectory(chosen);
        Directory.CreateDirectory(other);
        string project = Path.Combine(chosen, "Chosen.csproj");
        string otherProject = Path.Combine(other, "Other.csproj");
        string properties = "<PropertyGroup><TargetFramework>net10.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup>";
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\">" + properties +
            "<ItemGroup><ProjectReference Include=\"../Other/Other.csproj\" ReferenceOutputAssembly=\"false\" /></ItemGroup></Project>");
        File.WriteAllText(otherProject, "<Project Sdk=\"Microsoft.NET.Sdk\">" + properties +
            "<Target Name=\"MustNotBuild\" BeforeTargets=\"Build\"><Error Text=\"The unselected project was built.\" /></Target></Project>");
        string source = Path.Combine(chosen, "Chosen.cs");
        string otherSource = Path.Combine(other, "Other.cs");
        File.WriteAllText(source, "public class Chosen { public const int Number = 1; }");
        File.WriteAllText(otherSource, "public class Other { }");
        form.ExplorerTreeFeature.LoadFileExplorerFolder(root);
        form.StartupProjectFeature._fileExplorerStartupProjectPath = otherProject;

        FileManage.OpenFileFromArgs("cli|" + source, form.EditorTabControl);
        SelectedEditor.GetSelectedEditor().Text = "public class Chosen { public const int Number = 42; }";
        Pump(300);
        FileManage.OpenFileFromArgs("cli|" + otherSource, form.EditorTabControl);
        SelectedEditor.GetSelectedEditor().Text = "// unsaved unrelated edit\npublic class Other { }";
        Pump(300);
        TabPage activeTab = form.EditorTabControl.SelectedTab;

        var item = form.ExplorerFeature._fileExplorerBuildProjectMenuItem;
        void OpenMenu(string path)
        {
            form.ExplorerFeature._fileExplorerContextNode = new TreeNode { Tag = path };
            form.ExplorerActionsFeature.fileExplorerContextMenu_Opening(null, new CancelEventArgs());
        }
        OpenMenu(otherSource);
        assert(!item.Available, "Build Project is hidden on ordinary files");
        OpenMenu(root);
        assert(!item.Available, "Solution folders do not choose an arbitrary project to build");
        OpenMenu(chosen);
        assert(item.Available && item.Enabled && item.Text == "Build Project", "Project folders expose Build Project");
        OpenMenu(project);
        assert(item.Available && item.Enabled, "Project files expose Build Project");

        GlobalVariables.configParam = "-c Debug";
        GlobalVariables.platformParam = "";
        GlobalVariables.binaryPublish = true;
        GlobalVariables.OPublishNative = true;
        int ticks = 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 25 };
        timer.Tick += (_, _) => ticks++;
        timer.Start();
        item.PerformClick();
        assert(!item.Enabled, "Duplicate builds are disabled while the project is building");
        var build = form.ProjectBuildFeature;
        var elapsed = Stopwatch.StartNew();
        while (build._explorerProjectBuildRunning && elapsed.ElapsedMilliseconds < 45000) Pump(25);
        assert(!build._explorerProjectBuildRunning, "Project build completes");
        var output = form.outputRBT;
        Console.Error.WriteLine(output.Text);
        assert(output.Text.Contains("Build completed."), "The selected project builds even when its referenced project cannot build");
        assert(File.Exists(Path.Combine(chosen, "bin", "Debug", "net10.0", "Chosen.dll")), "Selected project output is produced");
        assert(!File.Exists(Path.Combine(other, "bin", "Debug", "net10.0", "Other.dll")), "Referenced project output is not built");
        assert(File.ReadAllText(source).Contains("Number = 42"), "Modified selected-project tabs are saved before building");
        assert(!File.ReadAllText(otherSource).Contains("unsaved"), "Unrelated editor contents remain unsaved");
        assert(form.EditorTabControl.SelectedTab == activeTab, "Building preserves the active editor tab");
        assert(ticks > 0 && item.Enabled, "The UI stays responsive and the build command is restored");

        File.WriteAllText(source, "this is invalid C#;");
        item.PerformClick();
        elapsed.Restart();
        while (build._explorerProjectBuildRunning && elapsed.ElapsedMilliseconds < 30000) Pump(25);
        assert(!build._explorerProjectBuildRunning && item.Enabled && output.Text.Contains("Build failed (exit code"),
            "Build errors appear in Output and allow another build");
    }

    private static void Pump(int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(10); }
    }
}
