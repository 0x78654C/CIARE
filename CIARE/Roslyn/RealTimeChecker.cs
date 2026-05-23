using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using CIARE.Utils;

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("windows")]
    public static class RealTimeChecker
    {
        private const int DebounceMs = 800;
        private static System.Threading.Timer _debounceTimer;
        private static CancellationTokenSource _cts;
        private static readonly object _lock = new object();

        private static readonly Color ErrorColor = Color.Red;
        private static readonly Color WarningColor = Color.Orange;

        // Cached platform references (never change at runtime).
        private static ImmutableArray<MetadataReference> _platformRefs;
        private static readonly object _refLock = new object();

        // Snapshot of custom refs used for the last build, so we only rebuild when they change.
        private static List<string> _lastCustomRefSnapshot;
        private static List<MetadataReference> _customRefs = new List<MetadataReference>();

        /// <summary>
        /// Schedule a real-time type check after a short debounce delay.
        /// Resets the timer on every call so it only fires once typing pauses.
        /// </summary>
        public static void ScheduleCheck(string code, TextEditorControl editor, Label statusLabel,
            RichTextBox errorsRTB = null, TabPage errorsTabPage = null, Label warningsLabel = null,
            string workspaceFolder = null, string currentFilePath = null)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new System.Threading.Timer(
                    _ => RunCheck(code, editor, statusLabel, errorsRTB, errorsTabPage, warningsLabel,
                        workspaceFolder, currentFilePath),
                    null, DebounceMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Cancel any pending or running check and clear all markers.
        /// </summary>
        public static void Cancel(TextEditorControl editor = null, Label statusLabel = null,
            RichTextBox errorsRTB = null, TabPage errorsTabPage = null, Label warningsLabel = null)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }

            if (editor != null)
            {
                editor.BeginInvoke((Action)(() =>
                {
                    editor.Document.MarkerStrategy.RemoveAll(_ => true);
                    editor.Refresh();
                    if (statusLabel != null)
                        statusLabel.Text = string.Empty;
                    if (warningsLabel != null)
                        warningsLabel.Text = string.Empty;
                    ClearErrorsPanel(errorsRTB, errorsTabPage);
                }));
            }
        }

        private static void RunCheck(string code, TextEditorControl editor, Label statusLabel,
            RichTextBox errorsRTB, TabPage errorsTabPage, Label warningsLabel,
            string workspaceFolder, string currentFilePath)
        {
            CancellationTokenSource cts;
            lock (_lock)
            {
                var old = _cts;
                old?.Cancel();
                old?.Dispose();
                cts = new CancellationTokenSource();
                _cts = cts;
            }

            Task.Run(() =>
            {
                try
                {
                    if (cts.IsCancellationRequested) return;

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        ClearMarkersAndStatus(editor, statusLabel, warningsLabel);
                        ClearErrorsPanel(errorsRTB, errorsTabPage);
                        return;
                    }

                    var diagnostics = GetDiagnostics(code, cts.Token, workspaceFolder, currentFilePath);
                    if (cts.IsCancellationRequested) return;

                    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                    var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

                    editor.BeginInvoke((Action)(() =>
                    {
                        if (cts.IsCancellationRequested) return;
                        ApplyMarkers(editor, errors, warnings);
                        UpdateErrorLabel(statusLabel, errors.Count);
                        UpdateWarningLabel(warningsLabel, warnings.Count);
                        UpdateErrorsPanel(errorsRTB, errorsTabPage, errors, warnings);
                    }));
                }
                catch (OperationCanceledException) { }
                catch { }
            }, cts.Token);
        }

        private static List<Diagnostic> GetDiagnostics(string code, CancellationToken ct,
            string workspaceFolder = null, string currentFilePath = null)
        {
            var parseOptions = BuildParseOptions(GlobalVariables.Framework);
            string activeFilePath = string.IsNullOrWhiteSpace(currentFilePath) ? "edited.cs" : currentFilePath;
            var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions, path: activeFilePath, cancellationToken: ct);
            var syntaxTrees = new List<SyntaxTree> { syntaxTree };
            syntaxTrees.AddRange(BuildWorkspaceSyntaxTrees(workspaceFolder, currentFilePath, parseOptions, ct));

            var outputKind = syntaxTrees.Any(tree => HasTopLevelStatements(tree, ct))
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary;

            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: syntaxTrees,
                references: BuildReferences(),
                options: new CSharpCompilationOptions(
                    outputKind,
                    reportSuppressedDiagnostics: true,
                    allowUnsafe: GlobalVariables.OUnsafeCode,
                    optimizationLevel: OptimizationLevel.Debug,
                    platform: Platform.AnyCpu));

            return compilation.GetDiagnostics(ct)
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Where(d => ShouldReportDiagnostic(d, syntaxTree))
                .ToList();
        }

        private static List<SyntaxTree> BuildWorkspaceSyntaxTrees(string workspaceFolder, string currentFilePath,
            CSharpParseOptions parseOptions, CancellationToken ct)
        {
            var trees = new List<SyntaxTree>();
            if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
                return trees;

            string normalizedCurrentFile = NormalizePath(currentFilePath);
            foreach (var filePath in EnumerateWorkspaceCsFiles(workspaceFolder, ct))
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(normalizedCurrentFile) &&
                    string.Equals(NormalizePath(filePath), normalizedCurrentFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var code = File.ReadAllText(filePath);
                    trees.Add(CSharpSyntaxTree.ParseText(code, parseOptions, path: filePath, cancellationToken: ct));
                }
                catch
                {
                    // Ignore unreadable workspace files; the active editor should still be checked.
                }
            }

            return trees;
        }

        private static IEnumerable<string> EnumerateWorkspaceCsFiles(string workspaceFolder, CancellationToken ct)
        {
            var pending = new Stack<string>();
            pending.Push(workspaceFolder);

            while (pending.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var folder = pending.Pop();

                List<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(folder).ToList();
                }
                catch
                {
                    directories = new List<string>();
                }

                foreach (var directory in directories)
                {
                    if (!ShouldSkipWorkspaceDirectory(directory))
                        pending.Push(directory);
                }

                List<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, "*.cs").ToList();
                }
                catch
                {
                    files = new List<string>();
                }

                foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    yield return file;
            }
        }

        private static bool ShouldSkipWorkspaceDirectory(string directory)
        {
            string name = Path.GetFileName(directory);
            if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "packages", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var attributes = File.GetAttributes(directory);
                return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                       (attributes & FileAttributes.System) == FileAttributes.System;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }

        private static bool ShouldReportDiagnostic(Diagnostic diagnostic, SyntaxTree activeSyntaxTree)
        {
            return !diagnostic.Location.IsInSource || diagnostic.Location.SourceTree == activeSyntaxTree;
        }

        private static bool HasTopLevelStatements(SyntaxTree syntaxTree, CancellationToken ct)
        {
            return syntaxTree.GetRoot(ct)
                .DescendantNodes()
                .OfType<GlobalStatementSyntax>()
                .Any();
        }

        private static IEnumerable<MetadataReference> BuildReferences()
        {
            lock (_refLock)
            {
                // Load platform references once and keep them forever.
                if (_platformRefs.IsDefault)
                {
                    var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
                    _platformRefs = trusted
                        .Split(Path.PathSeparator)
                        .Select(r => (MetadataReference)MetadataReference.CreateFromFile(r))
                        .ToImmutableArray();
                }

                // Rebuild custom references only when the list actually changes.
                var currentCustom = GlobalVariables.customRefList;
                if (_lastCustomRefSnapshot == null || !_lastCustomRefSnapshot.SequenceEqual(currentCustom))
                {
                    _lastCustomRefSnapshot = currentCustom.ToList();
                    _customRefs = new List<MetadataReference>();
                    foreach (var libPath in currentCustom)
                    {
                        var parts = libPath.Split('|');
                        if (parts.Length < 2) continue;
                        var lib = parts[1];
                        if (!string.IsNullOrEmpty(lib) && File.Exists(lib))
                            _customRefs.Add(MetadataReference.CreateFromFile(lib));
                    }
                }

                return _platformRefs.AddRange(_customRefs);
            }
        }

        private static void ApplyMarkers(TextEditorControl editor, List<Diagnostic> errors, List<Diagnostic> warnings)
        {
            editor.Document.MarkerStrategy.RemoveAll(_ => true);

            foreach (var diagnostic in errors.Concat(warnings))
            {
                var location = diagnostic.Location;
                if (!location.IsInSource) continue;

                var span = location.GetLineSpan();
                int startLine = span.StartLinePosition.Line;
                int startCol = span.StartLinePosition.Character;

                if (startLine < 0 || startLine >= editor.Document.TotalNumberOfLines) continue;

                var lineSegment = editor.Document.GetLineSegment(startLine);
                int offset = lineSegment.Offset + startCol;
                int length = Math.Max(location.SourceSpan.Length, 1);

                // Clamp length to end of line to avoid overrun
                int maxLength = lineSegment.Length - startCol;
                if (maxLength < 1) maxLength = 1;
                length = Math.Min(length, maxLength);

                var color = diagnostic.Severity == DiagnosticSeverity.Error ? ErrorColor : WarningColor;
                var marker = new TextMarker(offset, length, TextMarkerType.WaveLine, color)
                {
                    ToolTip = $"{diagnostic.Id}: {diagnostic.GetMessage()}"
                };
                editor.Document.MarkerStrategy.AddMarker(marker);
            }

            editor.Refresh();
        }

        private static void UpdateErrorLabel(Label label, int errorCount)
        {
            if (label == null) return;
            if (errorCount == 0)
            {
                label.Text = string.Empty;
                return;
            }
            label.Text = $"\u2716 {errorCount} error{(errorCount > 1 ? "s" : "")}";
            label.ForeColor = Color.Red;
        }

        private static void UpdateWarningLabel(Label label, int warningCount)
        {
            if (label == null) return;
            if (warningCount == 0)
            {
                label.Text = string.Empty;
                return;
            }
            label.Text = $"\u26a0 {warningCount} warning{(warningCount > 1 ? "s" : "")}";
            label.ForeColor = Color.Orange;
        }

        private static void ClearMarkersAndStatus(TextEditorControl editor, Label statusLabel, Label warningsLabel = null)
        {
            editor.BeginInvoke((Action)(() =>
            {
                editor.Document.MarkerStrategy.RemoveAll(_ => true);
                editor.Refresh();
                if (statusLabel != null)
                    statusLabel.Text = string.Empty;
                if (warningsLabel != null)
                    warningsLabel.Text = string.Empty;
            }));
        }

        private static void UpdateErrorsPanel(RichTextBox errorsRTB, TabPage errorsTabPage,
            List<Diagnostic> errors, List<Diagnostic> warnings)
        {
            if (errorsRTB == null) return;

            errorsRTB.Clear();

            foreach (var d in errors.Concat(warnings))
            {
                var location = d.Location;
                int line = location.IsInSource ? location.GetLineSpan().StartLinePosition.Line + 1 : 0;
                var icon = d.Severity == DiagnosticSeverity.Error ? "\u2716" : "\u26a0";
                var entry = line > 0
                    ? $"{icon}  Line {line,-5}  {d.Id,-8}  {d.GetMessage()}\n"
                    : $"{icon}  {d.Id,-8}  {d.GetMessage()}\n";

                int start = errorsRTB.TextLength;
                errorsRTB.AppendText(entry);

                // Colour the icon red/orange
                errorsRTB.Select(start, 1);
                errorsRTB.SelectionColor = d.Severity == DiagnosticSeverity.Error ? Color.Red : Color.Orange;
                errorsRTB.Select(errorsRTB.TextLength, 0);
                errorsRTB.SelectionColor = errorsRTB.ForeColor;
            }

            if (errorsTabPage != null)
            {
                int total = errors.Count + warnings.Count;
                errorsTabPage.Text = total > 0 ? $"Errors ({total})" : "Errors";
            }
        }

        private static void ClearErrorsPanel(RichTextBox errorsRTB, TabPage errorsTabPage)
        {
            if (errorsRTB == null) return;
            errorsRTB.Clear();
            if (errorsTabPage != null)
                errorsTabPage.Text = "Errors";
        }

        private static CSharpParseOptions BuildParseOptions(string framework)
        {
            var languageVersion = LanguageVersion.Default;
            switch (framework)
            {
                case "net6.0-windows": languageVersion = LanguageVersion.CSharp10; break;
                case "net7.0-windows": languageVersion = LanguageVersion.CSharp11; break;
                case "net8.0-windows": languageVersion = LanguageVersion.CSharp12; break;
            }
            return CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        }
    }
}
