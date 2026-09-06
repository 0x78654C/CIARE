using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
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
using System.Runtime.InteropServices;
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
        {
            form.Width = width;
            Assert(firstEditor.Bounds == firstPage.DisplayRectangle, "Editor fills tab during continuous resize");
            var areaControl = firstEditor.ActiveTextAreaControl;
            Assert(areaControl.TextArea.Right == areaControl.ClientSize.Width - areaControl.VScrollBar.Width,
                "Text surface and scrollbar stay aligned during resize");
        }
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

        if (scenario == "dark")
            CheckEditorScrolling(firstEditor);
        if (scenario == "completion")
            CheckCompletionWhileTyping(form, firstEditor);

        // Dispose without invoking file-saving prompts; only test fixture files have been opened.
        form.Dispose();
        Invoke(form, "ParseStep");
        Assert(form.IsDisposed, "Completion tolerates a disposed form");
        Pump(100);
        Console.Error.WriteLine($"Startup to Show completed: {elapsed.ElapsedMilliseconds} ms ({scenario})");
    }

    private static T GetField<T>(MainForm form, string name) =>
        (T)typeof(MainForm).GetField(name, PrivateInstance).GetValue(form);

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);

    private static void CheckEditorScrolling(TextEditorControl editor)
    {
        editor.Text = string.Join(Environment.NewLine,
            Enumerable.Range(0, 1000).Select(index => $"\tConsole.WriteLine(\"Line {index}\");"));
        var area = editor.ActiveTextAreaControl;
        area.Caret.Position = new TextLocation(10, 0);
        area.TextArea.Refresh();
        var view = area.TextArea.TextView;
        int expectedX = view.GetDrawingXPos(0, 10);
        using var process = Process.GetCurrentProcess();
        uint resourcesBefore = GetGuiResources(process.Handle, 0);
        var elapsed = Stopwatch.StartNew();
        for (int index = 0; index < 1000; index++)
            Assert(view.GetDrawingXPos(0, 10) == expectedX, "Repeated position measurement stays stable");
        uint resourcesAfter = GetGuiResources(process.Handle, 0);
        Assert(resourcesAfter <= resourcesBefore + 8, "Position measurement releases GDI resources immediately");
        for (int index = 1; index <= 100; index++)
        {
            area.VScrollBar.Value = index * view.FontHeight;
            area.TextArea.Update();
            Assert(view.FirstVisibleLine == index, "Scroll viewport follows the scrollbar");
        }
        area.HScrollBar.Value = 3;
        Assert(view.GetDrawingXPos(0, 10) == expectedX - 3 * view.WideSpaceWidth,
            "Horizontal scrolling keeps text coordinates aligned");
        editor.EnableFolding = true;
        editor.Document.FoldingManager.UpdateFoldings(new List<FoldMarker>
        {
            new FoldMarker(editor.Document, 10, 0, 20, 0, FoldType.Unspecified, "...", true)
        });
        area.VScrollBar.Value = 11 * view.FontHeight + 2;
        area.TextArea.Refresh();
        Assert(view.FirstVisibleLine == 21 && view.VisibleLineDrawingRemainder == 2,
            "Scrolling past a collapsed block preserves logical lines and pixel offset");
        Console.Error.WriteLine($"Scroll/measurement regression: {elapsed.ElapsedMilliseconds} ms; GDI {resourcesBefore} -> {resourcesAfter}");
    }

    private static void CheckCompletionWhileTyping(MainForm form, TextEditorControl editor)
    {
        var gate = (SemaphoreSlim)typeof(CodeCompletionKeyHandler)
            .GetField("CompletionGenerationLock", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var area = editor.ActiveTextAreaControl.TextArea;

        void Prepare(string code)
        {
            foreach (var popup in form.OwnedForms.OfType<CodeCompletionWindow>().ToArray())
                popup.Close();
            editor.Text = code;
            area.Caret.Position = editor.Document.OffsetToPosition(editor.Document.TextLength);
            form.Activate();
            area.Focus();
            Assert(area.Focused, "Completion editor has focus");
        }

        void CheckPopup(string expected, string expectedCode)
        {
            var elapsed = Stopwatch.StartNew();
            CodeCompletionWindow popup;
            while ((popup = form.OwnedForms.OfType<CodeCompletionWindow>().FirstOrDefault()) == null &&
                elapsed.ElapsedMilliseconds < 15000)
                Pump(10);
            Assert(popup != null, "Suggestions appear while the prefix continues to grow");
            var list = popup.Controls.OfType<CodeCompletionListView>().Single();
            if (list.SelectedCompletionData?.Text != expected)
            {
                var items = (ICompletionData[])typeof(CodeCompletionListView).GetField("completionData", PrivateInstance).GetValue(list);
                Console.Error.WriteLine("Completion items: " + string.Join(", ", items.Take(40).Select(item => item.Text)));
            }
            Assert(list.SelectedCompletionData?.Text == expected,
                $"Suggestions select the latest typed prefix: expected {expected}, got {list.SelectedCompletionData?.Text ?? "<none>"}; code={editor.Text}; caret={area.Caret.Offset}");
            popup.ProcessKeyEvent('\t');
            Assert(editor.Text == expectedCode, "Completion replaces the entire current prefix once");
            Console.Error.WriteLine($"Completion {expected}: {elapsed.ElapsedMilliseconds} ms after releasing worker");
        }

        Prepare("System.Console");
        Assert(gate.Wait(1000), "Completion worker can be delayed for member typing test");
        try
        {
            area.SimulateKeyPress('.');
            foreach (char ch in "WriteL") area.SimulateKeyPress(ch);
            Pump(30);
        }
        finally { gate.Release(); }
        CheckPopup("WriteLine", "System.Console.WriteLine");

        Prepare("using System;\n\n");
        Assert(gate.Wait(1000), "Completion worker can be delayed for automatic typing test");
        try
        {
            area.SimulateKeyPress('C');
            Pump(30);
            foreach (char ch in "onso") area.SimulateKeyPress(ch);
            Pump(30);
        }
        finally { gate.Release(); }
        CheckPopup("Console", "using System;\n\nConsole");

        Prepare("System.Console");
        Assert(gate.Wait(1000), "Completion worker can be delayed for cancellation test");
        try
        {
            area.SimulateKeyPress('.');
            editor.Document.Insert(0, "// changed context ");
            Pump(30);
        }
        finally { gate.Release(); }
        Pump(200);
        Assert(!form.OwnedForms.OfType<CodeCompletionWindow>().Any(), "Stale suggestions are discarded after surrounding edits");

        Prepare("System.Console");
        Assert(gate.Wait(1000), "Completion worker can be delayed for Escape test");
        try
        {
            area.SimulateKeyPress('.');
            area.ExecuteDialogKey(Keys.Escape);
        }
        finally { gate.Release(); }
        Pump(200);
        Assert(!form.OwnedForms.OfType<CodeCompletionWindow>().Any(), "Escape dismisses pending suggestions");

        Prepare("System.Console.");
        var disposablePopup = CodeCompletionWindow.ShowCompletionWindow(form, editor,
            new CodeCompletionProvider(form),
            new ICompletionData[] { new DefaultCompletionData("WriteLine", "Writes a line.", 1) },
            area.Caret.Offset, area.Caret.Offset, true, true);
        disposablePopup.Dispose();
        area.Caret.Column--;
        editor.Width++;
        Assert(!form.OwnedForms.OfType<CodeCompletionWindow>().Any(), "Disposed popup detaches from caret and resize events");

        // Leave a popup and its declaration visible for the owner's disposal test.
        CodeCompletionWindow.ShowCompletionWindow(form, editor, new CodeCompletionProvider(form),
            new ICompletionData[] { new DefaultCompletionData("WriteLine", "Writes a line.", 1) },
            area.Caret.Offset, area.Caret.Offset, true, true);
    }

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
