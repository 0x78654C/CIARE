using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CIARE;

internal static class ResourceRegression
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunRetention(MainForm form, Action<bool, string> assert, bool validateChanges)
    {
        WaitUntil(() => ((HashSet<string>)form.CompletionParsingFeature.GetType().GetField("_alreadyLoaded", Private).GetValue(form.CompletionParsingFeature)).Count > 0, 20000);
        Pump(2200);
        ReportMemory("references ready");
        RunCompletionWorkload(form, assert);
        Pump(1500);
        ReportMemory("completion retained");
        ReportCompletionMetadata();
        if (validateChanges)
        {
            var content = new ICSharpCode.SharpDevelop.Dom.DefaultProjectContent();
            var unit = new ICSharpCode.SharpDevelop.Dom.DefaultCompilationUnit(content);
            var type = new ICSharpCode.SharpDevelop.Dom.ReflectionLayer.ReflectionClass(unit,
                typeof(System.Text.StringBuilder), typeof(System.Text.StringBuilder).FullName, null);
            type.Freeze();
            var reads = Enumerable.Range(0, 8).Select(_ => System.Threading.Tasks.Task.Run(() => type.Methods)).ToArray();
            System.Threading.Tasks.Task.WaitAll(reads);
            assert(reads.All(task => ReferenceEquals(task.Result, reads[0].Result)), "Concurrent requests share one member table");
            assert(type.Methods.IsReadOnly && type.Methods.All(method => method.IsFrozen), "Lazily created metadata remains immutable");
            assert(type.Properties.Single(property => property.Name == "Length").ReturnType.FullyQualifiedName == "System.Int32",
                "Reflection return types use type names instead of assembly names");
            var extensionType = new ICSharpCode.SharpDevelop.Dom.ReflectionLayer.ReflectionClass(unit,
                typeof(Enumerable), typeof(Enumerable).FullName, null);
            extensionType.Freeze();
            assert(extensionType.HasExtensionMethods && extensionType.Methods.Any(method => method.Name == "Select" && method.IsExtensionMethod),
                "Extension methods remain discoverable before members are initialized");
        }
        RealTimeChecker.Cancel();
    }

    private static void RunCompletionWorkload(MainForm form, Action<bool, string> assert)
    {
        using var process = Process.GetCurrentProcess();
        foreach (var (source, expected) in new[] {
            ("using System; class C { void M() { Console", "WriteLine"),
            ("using System.Text; class C { void M() { var s = new StringBuilder(); s", "Append"),
            ("using System.Collections.Generic; class C { void M() { var l = new List<string>(); l", "Add"),
            ("using System.Linq; class C { void M() { var l = new int[0]; l", "Select"),
            ("class C { void M() { System.Windows.Forms.Form", "ActiveForm") })
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                var request = new CodeCompletionProvider.CompletionRequest {
                    RawCode = source, CurrentFilePath = string.Empty, CaretOffset = source.Length,
                    CaretLine = 0, CaretColumn = source.Length, CharacterTyped = '.'
                };
                var cpu = process.TotalProcessorTime;
                var elapsed = Stopwatch.StartNew();
                var result = new CodeCompletionProvider(form).GenerateCompletionData(request, CancellationToken.None);
                Console.Error.WriteLine($"RETENTION {expected} {iteration}: wall={elapsed.ElapsedMilliseconds} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms");
                assert(result.Any(item => item.Text == expected), "Completion still resolves " + expected);
            }
        }
        string largePrefix = "using System; class Large {\n" + string.Join("\n", Enumerable.Range(0, 2500)
            .Select(i => $"int M{i}()\n{{\nreturn {i};\n}}")) + "\nvoid Editing() { Conso";
        foreach (string source in new[] { "using System; class C { void M() { Conso", largePrefix })
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                int line = source.Count(ch => ch == '\n');
                var request = new CodeCompletionProvider.CompletionRequest {
                    RawCode = source, CurrentFilePath = string.Empty, CaretOffset = source.Length,
                    CaretLine = line, CaretColumn = source.Length - source.LastIndexOf('\n') - 1, CharacterTyped = 'o'
                };
                var cpu = process.TotalProcessorTime;
                var elapsed = Stopwatch.StartNew();
                var result = new CodeCompletionProvider(form, "Conso").GenerateCompletionData(request, CancellationToken.None);
                Console.Error.WriteLine($"RETENTION ordinary {line + 1} lines {iteration}: wall={elapsed.ElapsedMilliseconds} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms");
                assert(result.Any(item => item.Text == "Console"), "Ordinary suggestions remain available for " + (line + 1) + " lines");
            }
        }
    }

    public static void Run(MainForm form, Action<bool, string> assert, bool validateReuse = true)
    {
        string folder = Path.Combine(GlobalVariables.userProfileDirectory, "ResourceProject");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "ResourceProject.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup></Project>");
        for (int file = 0; file < 12; file++)
            File.WriteAllText(Path.Combine(folder, $"Other{file}.cs"), $"class Other{file} {{\n" +
                string.Join("\n", Enumerable.Range(0, 60).Select(i => $"public static int M{i}() {{ int x = 0; for (int n = 0; n < 10; n++) x += n; return x; }}")) + "\n}");
        string active = Path.Combine(folder, "Active.cs");
        File.WriteAllText(active, "class Active { public int M() => Other0.M0(); }\n");
        form.ExplorerTreeFeature.LoadFileExplorerFolder(folder);
        FileManage.OpenFileFromArgs("cli|" + active, form.EditorTabControl);
        var editor = SelectedEditor.GetSelectedEditor();
        Pump(6500);
        WaitUntil(() => ((HashSet<string>)form.CompletionParsingFeature.GetType().GetField("_alreadyLoaded", Private).GetValue(form.CompletionParsingFeature)).Count > 0, 15000);
        Pump(2200); // Exclude initial reference loading from the idle/typing measurement.
        assert(editor.Text.Contains("Other0.M0"), "Resource workload opens the project file");
        ReportMemory("project loaded");
        using var process = Process.GetCurrentProcess();
        var parsing = form.CompletionParsingFeature;
        var unitField = parsing.GetType().GetField("lastCompilationUnit", Private);
        object unit = unitField.GetValue(parsing);
        var workspaceUnits = form.CompletionWorkspaceFeature._workspaceCompilationUnits;
        string other = Path.Combine(folder, "Other0.cs");
        object otherUnit = workspaceUnits[other];
        var cpu = process.TotalProcessorTime;
        var elapsed = Stopwatch.StartNew();
        Pump(6000);
        Console.Error.WriteLine($"RESOURCE idle: wall={elapsed.ElapsedMilliseconds} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms; reparsed={!ReferenceEquals(unit, unitField.GetValue(parsing))}");
        if (validateReuse) assert(unit != null && ReferenceEquals(unit, unitField.GetValue(parsing)), "Idle projects retain their existing completion parse");
        cpu = process.TotalProcessorTime;
        elapsed.Restart();
        for (int i = 0; i < 8; i++)
        {
            editor.Document.Insert(editor.Document.TextLength, "// typing\n");
            Pump(650);
        }
        Console.Error.WriteLine($"RESOURCE typing: wall={elapsed.ElapsedMilliseconds} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms");
        Pump(2500);
        ReportMemory("after typing");
        assert(!ReferenceEquals(unit, unitField.GetValue(parsing)), "Typing refreshes the active completion parse");
        if (validateReuse) assert(otherUnit != null && ReferenceEquals(otherUnit, workspaceUnits[other]), "Typing in the active editor reuses unchanged project units");

        var area = editor.ActiveTextAreaControl.TextArea;
        for (int iteration = 0; iteration < 4; iteration++)
        {
            foreach (var openedPopup in form.OwnedForms.OfType<CodeCompletionWindow>().ToArray()) openedPopup.Close();
            editor.Text = "System.Console";
            area.Caret.Position = editor.Document.OffsetToPosition(editor.Document.TextLength);
            form.Activate();
            area.Focus();
            cpu = process.TotalProcessorTime;
            elapsed.Restart();
            area.SimulateKeyPress('.');
            foreach (char ch in "WriteL") { area.SimulateKeyPress(ch); Pump(20); }
            WaitUntil(() => form.OwnedForms.OfType<CodeCompletionWindow>().Any(), 10000);
            var popup = form.OwnedForms.OfType<CodeCompletionWindow>().FirstOrDefault();
            assert(popup != null, "Suggestions remain available while typing a member prefix");
            Console.Error.WriteLine($"RESOURCE completion {iteration}: wall={elapsed.ElapsedMilliseconds} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms");
            popup?.Close();
        }

        File.AppendAllText(other, "\nclass ExternalChange {}\n");
        WaitUntil(() => !ReferenceEquals(otherUnit, workspaceUnits[other]), 6000);
        assert(!ReferenceEquals(otherUnit, workspaceUnits[other]), "External source changes refresh workspace completion");
        File.WriteAllText(Path.Combine(folder, "Global.cs"), "global using Alias = System.Text.StringBuilder;\n");
        Pump(350);
        string prepared = form.PrepareCodeForNRefactoryCompletion("class C {}", active, out _, out _, out _);
        assert(prepared.Contains("using Alias = System.Text.StringBuilder;"), "New global using aliases invalidate the small directive cache");
        File.Delete(Path.Combine(folder, "Global.cs"));
        Pump(350);
        prepared = form.PrepareCodeForNRefactoryCompletion("class C {}", active, out _, out _, out _);
        assert(!prepared.Contains("using Alias ="), "Deleted global aliases are removed from completion");

        foreach (var (receiver, member) in new[] { ("System.Console", "WriteLine"), ("System.Windows.Forms.Form", "ActiveForm"),
            ("Microsoft.CodeAnalysis.CSharp.SyntaxFactory", "ParseCompilationUnit") })
        {
            string code = "class C { void M() { " + receiver;
            var request = new CodeCompletionProvider.CompletionRequest
            {
                RawCode = code, CurrentFilePath = active, CaretOffset = code.Length,
                CaretLine = 0, CaretColumn = code.Length, CharacterTyped = '.'
            };
            var result = new CodeCompletionProvider(form).GenerateCompletionData(request, CancellationToken.None);
            assert(result.Any(item => item.Text == member), "Completion still resolves " + receiver + "." + member);
        }
        RealTimeChecker.Cancel();
        string ordinaryCode = "using System;\nclass C { void M() {\n\n} }\n";
        MeasureSuggestions(form, ordinaryCode, "ordinary", assert);
        string largeCode = "using System;\nclass Big {\n" + string.Join("\n",
            Enumerable.Range(0, 2500).Select(i => $"int M{i}()\n{{\nreturn {i};\n}}")) +
            "\nvoid Editing() {\n\n} }\n";
        MeasureSuggestions(form, largeCode, "10000-line", assert);
        RealTimeChecker.Cancel();
        CheckContexts(assert, validateReuse);
        ReportMemory("after suggestions");
        ReportCompletionMetadata();
    }

    private static void ReportCompletionMetadata()
    {
        var classes = MainForm.myProjectContent.ReferencedContents.SelectMany(pc => pc.Classes).ToArray();
        int methods = 0, parameters = 0, incorrectTypeNames = 0, initialized = 0;
        foreach (var type in classes)
        {
            object lazy = type.GetType().GetField("members", Private)?.GetValue(type);
            if (lazy != null && !(bool)lazy.GetType().GetProperty("IsValueCreated").GetValue(lazy)) continue;
            initialized++;
            foreach (var method in type.Methods)
            {
                methods++;
                parameters += method.Parameters.Count;
                if (method.ReturnType?.FullyQualifiedName.Contains(", Version=") == true) incorrectTypeNames++;
            }
        }
        Console.Error.WriteLine($"RESOURCE framework metadata: types={classes.Length}; initialized={initialized}; methods={methods}; parameters={parameters}; incorrect return names={incorrectTypeNames}");
    }

    private static void MeasureSuggestions(MainForm form, string code, string label, Action<bool, string> assert)
    {
        var editor = SelectedEditor.GetSelectedEditor();
        var area = editor.ActiveTextAreaControl.TextArea;
        using var process = Process.GetCurrentProcess();
        for (int iteration = 0; iteration < 3; iteration++)
        {
            foreach (var popup in form.OwnedForms.OfType<CodeCompletionWindow>().ToArray()) popup.Close();
            var load = Stopwatch.StartNew();
            editor.Text = code;
            long loadMs = load.ElapsedMilliseconds;
            area.Caret.Position = editor.Document.OffsetToPosition(code.IndexOf("\n\n", StringComparison.Ordinal) + 1);
            form.Activate();
            area.Focus();
            var cpu = process.TotalProcessorTime;
            var elapsed = Stopwatch.StartNew();
            long inputTicks = 0;
            foreach (char ch in "Conso")
            {
                var input = Stopwatch.StartNew();
                area.SimulateKeyPress(ch);
                inputTicks += input.ElapsedTicks;
                Pump(20);
            }
            WaitUntil(() => form.OwnedForms.OfType<CodeCompletionWindow>().Any(), 10000);
            assert(form.OwnedForms.OfType<CodeCompletionWindow>().Any(), label + " automatic suggestions appear during ordinary typing");
            Console.Error.WriteLine($"RESOURCE {label} suggestion {iteration}: load={loadMs} ms; wall={elapsed.ElapsedMilliseconds} ms; input={inputTicks * 1000d / Stopwatch.Frequency:F1} ms; CPU={(process.TotalProcessorTime - cpu).TotalMilliseconds:F0} ms");
        }
        foreach (var popup in form.OwnedForms.OfType<CodeCompletionWindow>().ToArray()) popup.Close();
    }

    private static void CheckContexts(Action<bool, string> assert, bool validateCancellation)
    {
        var method = typeof(CodeCompletionKeyHandler).GetMethod("IsCompletionSuppressedContext", BindingFlags.Static | BindingFlags.NonPublic);
        bool HasSuppression(string code, int position, CancellationToken token = default) => (bool)method.Invoke(null,
            method.GetParameters().Length == 2 ? new object[] { code, position } : new object[] { code, position, token });
        foreach (var (sample, expected) in new[] {
            ("class C { void M() { System.Console|; } }", false),
            ("// comment|", true), ("/* comment| */", true), ("#if DEBUG|", true),
            ("class C { string s = \"text|\"; }", true), ("class C { char c = 'x|'; }", true),
            ("class C { string s = @\"line1\nline2|\"; }", true),
            ("class C { string s = \"\"\"raw|\"\"\"; }", true),
            ("class C { string s = $\"value {System.Console|}\"; }", false),
            ("class C { string s = $\"text| {1}\"; }", true),
            ("// $ordinary comment\nclass C { void M() { System.Console|; } }", false) })
        {
            int position = sample.IndexOf('|');
            assert(HasSuppression(sample.Remove(position, 1), position) == expected, "Completion suppression preserves strings, comments, directives and interpolation");
        }
        if (validateCancellation)
        {
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            foreach (string prefix in new[] { "", "$\"value\";" })
            {
                bool stopped = false;
                try { HasSuppression(prefix + new string(' ', 100000), 100000, cancelled.Token); }
                catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException) { stopped = true; }
                assert(stopped, "Cancelled context scans stop before doing full-file work");
            }
        }
    }

    private static void WaitUntil(Func<bool> condition, int timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition() && elapsed.ElapsedMilliseconds < timeout) Pump(25);
    }

    private static void ReportMemory(string phase)
    {
        using var process = Process.GetCurrentProcess();
        Console.Error.WriteLine($"RESOURCE {phase} before collection: managed={GC.GetTotalMemory(false) / 1048576d:F1} MiB; private={process.PrivateMemorySize64 / 1048576d:F1} MiB; working set={process.WorkingSet64 / 1048576d:F1} MiB");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        process.Refresh();
        Console.Error.WriteLine($"RESOURCE {phase}: managed={GC.GetTotalMemory(false) / 1048576d:F1} MiB; private={process.PrivateMemorySize64 / 1048576d:F1} MiB; working set={process.WorkingSet64 / 1048576d:F1} MiB");
    }

    private static void Pump(int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }
}
