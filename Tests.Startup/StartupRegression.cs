using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace CIARE;

[SupportedOSPlatform("windows")]
internal static class StartupRegression
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int _checks;

    [STAThread]
    private static int Main(string[] args)
    {
        var output = Console.Out;
        try
        {
            Assert(GlobalVariables.registryPath.StartsWith("SOFTWARE\\CIARE.StartupTests\\"), "Isolated registry");
            Assert(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CIARE_STARTUP_TEST_DATA")), "Isolated data");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Assert(System.Configuration.ConfigurationManager.AppSettings != null, "Runtime configuration loads");
            Run(args[0]);
            output.WriteLine($"PASS {args[0]}: {_checks} checks");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            Console.SetOut(output);
            if (GlobalVariables.registryPath.StartsWith("SOFTWARE\\CIARE.StartupTests\\"))
                Registry.CurrentUser.DeleteSubKeyTree(GlobalVariables.registryPath, false);
        }
    }

    private static void Run(string scenario)
    {
        using var settings = Registry.CurrentUser.CreateSubKey(GlobalVariables.registryPath);
        settings.SetValue("highlight", scenario == "light" ? "C#-Light" : "C#-Dark");
        settings.SetValue("OCodeCompletion", (scenario == "completion").ToString());
        settings.SetValue("OStartUp", "False");
        settings.SetValue("windowSize", scenario == "corrupt" ? "broken|size|value" : "1000|700");
        if (scenario != "corrupt")
            settings.SetValue("windowSizeMax", (scenario == "maximized").ToString());
        settings.SetValue("OutWState", "False");
        settings.SetValue("fileExplorerVisible", (scenario != "light").ToString());
        settings.SetValue("fileExplorerWidth", "260");
        settings.SetValue("fileExplorerNuGetHeight", "180");
        if (scenario == "ollama")
        {
            settings.SetValue(GlobalVariables.aiType, "Ollama(Local)");
            settings.SetValue(GlobalVariables.ollamModel, "test-model");
        }
        var elapsed = Stopwatch.StartNew();
        using var form = new MainForm { ShowInTaskbar = false, Opacity = 0 };
        typeof(MainForm).GetField("s_args", PrivateInstance).SetValue(form, string.Empty);

        var files = new List<string>();
        if (scenario == "restored")
        {
            for (int index = 0; index < 12; index++)
            {
                string path = Path.Combine(GlobalVariables.userProfileDirectory, $"sample{index}.txt");
                File.WriteAllText(path, $"Restored document {index}");
                files.Add(path);
            }
            File.WriteAllLines(GlobalVariables.tabsFilePathAll, files.Select((path, index) => $"{path}|{index + 1}"));
            settings.SetValue("lastTabPosition", files[3]);
            settings.SetValue("fileExplorerPath", GlobalVariables.userProfileDirectory);
            settings.SetValue("OStartUp", "True");
        }

        int visibleEvents = 0;
        form.VisibleChanged += (_, _) =>
        {
            if (!form.Visible) return;
            visibleEvents++;
            Assert(form.isLoaded, "Startup completed before first visibility");
            Assert(form.EditorTabControl.SelectedIndex > 0, "Editor selected before first visibility");
            Assert(GlobalVariables.darkColor == (scenario != "light"), "Theme ready before first visibility");
            Assert(!form.progressBar.Visible, "AI progress indicator is hidden at startup");
            Assert(GetField<TreeView>(form, "_fileExplorerTree").BackColor ==
                (scenario == "light" ? SystemColors.Window : GlobalVariables.controlBgColor),
                "Explorer palette matches the initial theme");
            Assert(GetField<SplitContainer>(form, "_editorExplorerSplitContainer").Panel2Collapsed == (scenario == "light"),
                "Explorer visibility ready before first visibility");
            if (scenario != "light")
            {
                Assert(GetField<SplitContainer>(form, "_editorExplorerSplitContainer").Panel2.Width == 260,
                    "Explorer width ready before first visibility");
                Assert(GetField<SplitContainer>(form, "_fileExplorerContentSplitContainer").Panel2.Height == 180,
                    "NuGet pane height ready before first visibility");
            }
        };
        form.Show();
        elapsed.Stop();
        Assert(visibleEvents == 1, "Main form shown once");
        Assert(form.isLoaded, "Main form loaded");
        if (scenario == "ollama")
            Assert(GlobalVariables.aiTypeVar == "Ollama(Local)" && GlobalVariables.modelOllamaVar == "test-model",
                "Ollama settings restored without requiring a local CLI");
        using (var snapshot = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(snapshot, new Rectangle(Point.Empty, snapshot.Size));
            snapshot.Save(Path.Combine(AppContext.BaseDirectory, $"startup-{scenario}.png"));
        }
        Assert(!GetField<SplitContainer>(form, "splitContainer1").Panel2Collapsed, "Saved output visibility restored");
        var normalSize = new Size(Math.Min(1000, Screen.FromControl(form).WorkingArea.Width),
            Math.Min(700, Screen.FromControl(form).WorkingArea.Height));
        if (scenario == "maximized")
        {
            Assert(form.WindowState == FormWindowState.Maximized, "Maximized state restored");
            form.WindowState = FormWindowState.Normal;
            Assert(form.Size == normalSize, "Normal size retained while maximized");
        }
        else if (scenario == "corrupt")
        {
            Assert(form.Width > 0 && form.Height > 0, "Corrupt size recovers");
            Assert((string)settings.GetValue("windowSizeMax") == "False", "Missing maximized key initialized");
            Assert((string)settings.GetValue("windowSize") == "1225|786", "Invalid size repaired");
        }
        else
            Assert(form.Size == normalSize, "Saved size restored");

        Assert(MainForm.pcRegistry != null == (scenario == "completion"), "Completion setting honored for first editor");
        if (files.Count > 0)
        {
            Assert(form.EditorTabControl.TabCount == files.Count + 1, "All saved tabs restored");
            Assert(form.EditorTabControl.SelectedTab.ToolTipText == files[3], "Saved active tab restored");
            foreach (var path in files)
            {
                var page = form.EditorTabControl.TabPages.Cast<TabPage>().Single(tab => tab.ToolTipText == path);
                Assert(page.Controls.OfType<TextEditorControl>().Single().Text == File.ReadAllText(path), "Restored document preserved");
            }
        }

        var firstPage = form.EditorTabControl.SelectedTab;
        var firstEditor = SelectedEditor.GetSelectedEditor();
        form.Size = new Size(950, 650);
        int paletteChanges = 0;
        form.outputRBT.BackColorChanged += (_, _) => paletteChanges++;
        TabControllerManage.AddNewTab(form.EditorTabControl);
        var addedPage = form.EditorTabControl.SelectedTab;
        Assert(form.Size == new Size(950, 650), "Adding a tab preserves current size");
        Assert(!GetField<SplitContainer>(form, "splitContainer1").Panel2Collapsed, "Adding a tab preserves output visibility");
        Assert(paletteChanges == 0, "Adding a tab does not repaint form palette");
        form.EditorTabControl.SelectedTab = firstPage;
        form.EditorTabControl.TabPages.Remove(addedPage);
        addedPage.Dispose();
        TabControllerManage.AddNewTab(form.EditorTabControl);
        form.EditorTabControl.SelectedTab = firstPage;
        Assert(ReferenceEquals(SelectedEditor.GetSelectedEditor(), firstEditor), "Existing editor survives tab removal and insertion");
        Assert(ReferenceEquals(form.selectedEditor, firstEditor), "Save target follows active editor");
        foreach (TabPage page in form.EditorTabControl.TabPages)
        {
            if (form.EditorTabControl.TabPages.IndexOf(page) == 0) continue;
            Assert(page.Controls.OfType<TextEditorControl>().Count() == 1, "One editor per tab");
            var caret = page.Controls.OfType<TextEditorControl>().Single().ActiveTextAreaControl.Caret;
            var subscribers = (Delegate)caret.GetType().GetField("PositionChanged", PrivateInstance).GetValue(caret);
            Assert(subscribers.GetInvocationList().Count(handler => handler.Method.DeclaringType == typeof(LinesManage)) == 1,
                "One caret status subscription per editor");
        }

        string savedBeforeResize = (string)settings.GetValue("windowSize");
        for (int width = 951; width <= 970; width++)
            form.Width = width;
        Assert((string)settings.GetValue("windowSize") == savedBeforeResize, "Resize persistence is deferred");
        Pump(450);
        Assert((string)settings.GetValue("windowSize") == "970|650", "Final resize persisted");
        Assert(!GetField<bool>(form, "_pendingFileExplorerLayoutApply"), "Explorer layout settles");
        Assert(GetField<System.Windows.Forms.Timer>(form, "_fileExplorerLayoutApplyTimer")?.Enabled != true, "Explorer layout timer stops");
        Invoke(form, "ToggleFullScreen");
        Pump(300);
        Assert((string)settings.GetValue("windowSize") == "970|650", "Fullscreen preserves normal saved size");
        Invoke(form, "ToggleFullScreen");

        for (int index = 0; index < 3; index++)
        {
            form.SetHighLighter(firstEditor, "C#-Light");
            Assert(!GlobalVariables.darkColor && form.BackColor == SystemColors.Window, "Light theme applies");
            form.SetHighLighter(firstEditor, "C#-Dark");
            Assert(GlobalVariables.darkColor && form.BackColor == GlobalVariables.formBgColor, "Dark theme applies");
        }
        object paintKey = typeof(ToolStripItem).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(field => field.Name.Contains("paint", StringComparison.OrdinalIgnoreCase)).GetValue(null);
        var eventListProperty = typeof(Component).GetProperty("Events", PrivateInstance);
        foreach (var separator in ListMenuStripItems.ListToolStripSeparator())
        {
            var events = (EventHandlerList)eventListProperty.GetValue(separator);
            Assert(events[paintKey].GetInvocationList().Length == 1, "Theme changes retain one separator paint handler");
        }
        Program.NewInstanceHandler(null, new StartupNextInstanceEventArgs(Array.AsReadOnly(new[] { "CIARE.exe" }), true));
        Assert(form.EditorTabControl.SelectedTab == firstPage, "Second launch without a file preserves active tab");

        // Dispose without invoking file-saving prompts; only test fixture files have been opened.
        form.Dispose();
        Invoke(form, "ParseStep");
        Assert(form.IsDisposed, "Completion tolerates a disposed form");
        Pump(100);
        Console.Error.WriteLine($"Startup to Show completed: {elapsed.ElapsedMilliseconds} ms ({scenario})");
    }

    private static T GetField<T>(MainForm form, string name) =>
        (T)typeof(MainForm).GetField(name, PrivateInstance).GetValue(form);

    private static void Invoke(MainForm form, string name) =>
        typeof(MainForm).GetMethod(name, PrivateInstance).Invoke(form, null);

    private static void Pump(int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static void Assert(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        _checks++;
    }
}
