using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CIARE.Debugger;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private readonly HashSet<TextEditorControl> _debugConfiguredEditors =
            new HashSet<TextEditorControl>();
        private readonly BreakpointBookmarkFactory _breakpointFactory =
            new BreakpointBookmarkFactory();

        private DebugSession _debugSession;
        private TextEditorControl _debugEditor;
        private TextEditorControl _debugMarkerEditor;
        private TextMarker _debugLineMarker;
        private ToolStripMenuItem _debugMenu;
        private ToolStripMenuItem _debugStartContinueItem;
        private ToolStripMenuItem _debugStopItem;
        private ToolStripMenuItem _debugStepIntoItem;
        private ToolStripMenuItem _debugStepOverItem;
        private ToolStripMenuItem _debugStepOutItem;
        private ToolStripMenuItem _debugStatusItem;
        private TabPage _debugLocalsTabPage;
        private ListView _debugLocalsList;

        private sealed class DebuggerValueNode
        {
            public DebuggerValueNode(string name, string typeName, object value, int depth)
            {
                Name = name ?? string.Empty;
                TypeName = typeName ?? string.Empty;
                Value = value;
                Depth = depth;
            }

            public string Name { get; }
            public string TypeName { get; }
            public object Value { get; set; }
            public int Depth { get; }
            public bool Expanded { get; set; }
        }

        private sealed class DebuggerArrayRange
        {
            public DebuggerArrayRange(Array array, int start, int count)
            {
                Array = array;
                Start = start;
                Count = count;
            }

            public Array Array { get; }
            public int Start { get; }
            public int Count { get; }
        }

        private sealed class DebuggerStaticMembers
        {
            public DebuggerStaticMembers(Type type)
            {
                Type = type;
            }

            public Type Type { get; }
        }

        private sealed class DebuggerUnavailableValue
        {
            public DebuggerUnavailableValue(string message)
            {
                Message = message ?? "Value unavailable";
            }

            public string Message { get; }
        }

        private sealed class DebuggerDeferredProperty
        {
            public DebuggerDeferredProperty(object target, PropertyInfo property)
            {
                Target = target;
                Property = property;
            }

            public object Target { get; }
            public PropertyInfo Property { get; }
        }

        private void InitializeDebugger()
        {
            _debugMenu = new ToolStripMenuItem("Debug");
            _debugStartContinueItem = CreateDebugMenuItem(
                "Start Debugging", Keys.F5, (sender, args) => StartOrContinueDebugging());
            ToolStripMenuItem runWithoutDebuggingItem = CreateDebugMenuItem(
                "Start Without Debugging", Keys.Control | Keys.F5,
                (sender, args) => RunWithoutDebugging());
            _debugStopItem = CreateDebugMenuItem(
                "Stop Debugging", Keys.Shift | Keys.F5, (sender, args) => StopDebugging());
            _debugStepOverItem = CreateDebugMenuItem(
                "Step Over", Keys.F10, (sender, args) => StepDebugging(DebugCommand.StepOver));
            _debugStepIntoItem = CreateDebugMenuItem(
                "Step Into", Keys.F11, (sender, args) => StepDebugging(DebugCommand.StepInto));
            _debugStepOutItem = CreateDebugMenuItem(
                "Step Out", Keys.Shift | Keys.F11, (sender, args) => StepDebugging(DebugCommand.StepOut));
            ToolStripMenuItem toggleBreakpointItem = CreateDebugMenuItem(
                "Toggle Breakpoint", Keys.F9, (sender, args) => ToggleBreakpointAtCaret());
            ToolStripMenuItem deleteAllBreakpointsItem = CreateDebugMenuItem(
                "Delete All Breakpoints", Keys.Control | Keys.Shift | Keys.F9,
                (sender, args) => DeleteAllBreakpoints());
            _debugStatusItem = new ToolStripMenuItem("Debugger: stopped") { Enabled = false };

            _debugMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                _debugStartContinueItem,
                runWithoutDebuggingItem,
                _debugStopItem,
                new ToolStripSeparator(),
                _debugStepOverItem,
                _debugStepIntoItem,
                _debugStepOutItem,
                new ToolStripSeparator(),
                toggleBreakpointItem,
                deleteAllBreakpointsItem,
                new ToolStripSeparator(),
                _debugStatusItem
            });

            int compileIndex = menuStrip1.Items.IndexOf(compileToolStripMenuItem);
            menuStrip1.Items.Insert(Math.Max(0, compileIndex + 1), _debugMenu);
            menuStrip1.Layout += PositionDebuggerRunButton;
            menuStrip1.PerformLayout();
            PositionDebuggerRunButton(menuStrip1, null);
            InitializeDebuggerLocals();
            fullScreenToolStripMenuItem.Text = "Full Screen            ( Shift + Alt + Enter )";
            toolTip1.SetToolTip(runCodePb, "Start / continue debugging ( F5 )");
            ApplyDebuggerTheme(GlobalVariables.darkColor);
            UpdateDebuggerUi();
        }

        private void InitializeDebuggerLocals()
        {
            _debugLocalsList = new ListView
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10.5F),
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                ShowItemToolTips = true,
                UseCompatibleStateImageBehavior = false,
                View = View.Details
            };
            _debugLocalsList.Columns.Add("Name", 220, HorizontalAlignment.Left);
            _debugLocalsList.Columns.Add("Value", 620, HorizontalAlignment.Left);
            _debugLocalsList.Columns.Add("Type", 280, HorizontalAlignment.Left);
            _debugLocalsList.Resize += (sender, args) => ResizeDebuggerLocalsColumns();
            _debugLocalsList.ItemActivate += DebuggerLocalsItemActivate;

            _debugLocalsTabPage = new TabPage("Locals")
            {
                UseVisualStyleBackColor = false
            };
            _debugLocalsTabPage.Controls.Add(_debugLocalsList);
            outputTabControl.TabPages.Add(_debugLocalsTabPage);
            ResizeDebuggerLocalsColumns();
        }

        private void ResizeDebuggerLocalsColumns()
        {
            if (_debugLocalsList == null || _debugLocalsList.Columns.Count < 3)
                return;

            int available = Math.Max(240, _debugLocalsList.ClientSize.Width - 4);
            int nameWidth = Math.Min(240, Math.Max(120, available / 5));
            int typeWidth = Math.Min(320, Math.Max(160, available / 4));
            _debugLocalsList.Columns[0].Width = nameWidth;
            _debugLocalsList.Columns[2].Width = typeWidth;
            _debugLocalsList.Columns[1].Width = Math.Max(100, available - nameWidth - typeWidth);
        }

        private void PositionDebuggerRunButton(object sender, LayoutEventArgs args)
        {
            Rectangle helpBounds = helpToolStripMenuItem.Bounds;
            if (helpBounds.Width <= 0)
                return;

            const int horizontalGap = 8;
            int separatorTop = menuStrip1.Top +
                Math.Max(0, (menuStrip1.Height - label2.Height) / 2);
            int buttonTop = menuStrip1.Top +
                Math.Max(0, (menuStrip1.Height - runCodePb.Height) / 2);

            label2.Location = new Point(
                menuStrip1.Left + helpBounds.Right + horizontalGap,
                separatorTop);
            runCodePb.Location = new Point(label2.Right + horizontalGap, buttonTop);
            label3.Location = new Point(runCodePb.Right + horizontalGap, separatorTop);
        }

        private static ToolStripMenuItem CreateDebugMenuItem(string text, Keys shortcut,
            EventHandler handler)
        {
            var item = new ToolStripMenuItem(text)
            {
                ShortcutKeys = shortcut,
                ShowShortcutKeys = true
            };
            item.Click += handler;
            return item;
        }

        private void ConfigureDebuggerEditor(TextEditorControl editor)
        {
            if (editor == null || !_debugConfiguredEditors.Add(editor))
                return;

            editor.IsIconBarVisible = true;
            editor.Document.BookmarkManager.Factory = _breakpointFactory;
            editor.Document.BookmarkManager.Added += DebugBookmarkChanged;
            editor.Document.BookmarkManager.Removed += DebugBookmarkChanged;
            editor.ActiveTextAreaControl.TextArea.IconBarMargin.MouseDown +=
                (margin, mousePosition, buttons) =>
                {
                    if ((buttons & MouseButtons.Left) != MouseButtons.Left)
                        return;

                    TextArea textArea = margin.TextArea;
                    int visibleLine = (mousePosition.Y + textArea.VirtualTop.Y) /
                        textArea.TextView.FontHeight;
                    int line = textArea.Document.GetFirstLogicalLine(visibleLine);
                    if (line < 0 || line >= textArea.Document.TotalNumberOfLines)
                        return;

                    textArea.Document.BookmarkManager.ToggleMarkAt(new TextLocation(0, line));
                    textArea.Document.RequestUpdate(
                        new TextAreaUpdate(TextAreaUpdateType.SingleLine, line));
                    textArea.Document.CommitUpdate();
                };
            editor.Refresh();
        }

        private void DebugBookmarkChanged(object sender, BookmarkEventArgs args)
        {
            if (!(args.Bookmark is BreakpointBookmark))
                return;
            _debugSession?.UpdateBreakpoints(GetSourceBreakpoints());
        }

        private void ToggleBreakpointAtCaret()
        {
            TextEditorControl editor = SelectedEditor.GetSelectedEditor();
            if (editor == null)
                return;
            ConfigureDebuggerEditor(editor);
            TextLocation location = editor.ActiveTextAreaControl.TextArea.Caret.Position;
            editor.Document.BookmarkManager.ToggleMarkAt(new TextLocation(0, location.Line));
            editor.Document.RequestUpdate(
                new TextAreaUpdate(TextAreaUpdateType.SingleLine, location.Line));
            editor.Document.CommitUpdate();
        }

        private void DeleteAllBreakpoints()
        {
            foreach (EditorDocument document in GetOpenEditorDocuments())
            {
                document.Editor.Document.BookmarkManager.RemoveMarks(
                    bookmark => bookmark is BreakpointBookmark);
                document.Editor.Refresh();
            }
            _debugSession?.UpdateBreakpoints(Array.Empty<DebugSourceBreakpoint>());
        }

        private void StartOrContinueDebugging()
        {
            if (_debugSession?.State == DebugSessionState.Paused)
            {
                ClearCurrentStatementMarker();
                ClearDebuggerLocals();
                outputTabControl.SelectedTab = outputTabPage;
                _debugSession.Resume(DebugCommand.Continue);
                return;
            }
            if (_debugSession?.IsActive == true)
                return;

            StartDebugging(DebugCommand.Continue);
        }

        private void StepDebugging(DebugCommand command)
        {
            if (_debugSession?.State == DebugSessionState.Paused)
            {
                ClearCurrentStatementMarker();
                ClearDebuggerLocals();
                outputTabControl.SelectedTab = outputTabPage;
                _debugSession.Resume(command);
                return;
            }
            if (_debugSession?.IsActive == true || command == DebugCommand.StepOut)
                return;

            // Starting with F10/F11 behaves like Visual Studio and breaks on the first statement.
            StartDebugging(DebugCommand.StepInto);
        }

        private void StartDebugging(DebugCommand initialCommand)
        {
            TextEditorControl editor = SelectedEditor.GetSelectedEditor();
            if (editor == null || string.IsNullOrWhiteSpace(editor.Text))
            {
                MessageBox.Show("There is no C# code to debug.", "CIARE Debugger",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ConfigureDebuggerEditor(editor);
                RealTimeChecker.Cancel(editor, typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
                outputTabControl.SelectedTab = outputTabPage;
                OutputWindowManage.ShowOutputWindow(splitContainer1, outputRBT);
                outputRBT.Text = "Debugger: preparing source and symbols..." + Environment.NewLine;
                ClearDebuggerLocals();

                CSharpCompilation compilation = CreateDebugCompilation(editor.Text);
                Dictionary<string, string> sourceOverrides = GetOpenSourceOverrides();
                var session = new DebugSession();
                session.Prepared += DebugSessionPrepared;
                session.Paused += DebugSessionPaused;
                session.StateChanged += DebugSessionStateChanged;
                session.Ended += DebugSessionEnded;

                _debugEditor = editor;
                _debugSession = session;
                session.Start(compilation, sourceOverrides, GetSourceBreakpoints(),
                    GetDebuggerCommandLineArguments(), initialCommand);
                UpdateDebuggerUi();
            }
            catch (Exception exception)
            {
                _debugSession?.Dispose();
                _debugSession = null;
                _debugEditor = null;
                AppendDebuggerOutput("Debugger could not start: " + exception.GetBaseException().Message);
                UpdateDebuggerUi();
            }
        }

        private CSharpCompilation CreateDebugCompilation(string activeCode)
        {
            RoslynCompletionContext context = BuildRoslynCompletionContext(activeCode);
            OutputKind outputKind = GlobalVariables.OutputKind;
            if (outputKind != OutputKind.ConsoleApplication && outputKind != OutputKind.WindowsApplication)
                outputKind = OutputKind.ConsoleApplication;

            var options = ((CSharpCompilationOptions)context.Compilation.Options)
                .WithOutputKind(outputKind)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithAllowUnsafe(GlobalVariables.OUnsafeCode)
                .WithPlatform(Platform.AnyCpu);
            return context.Compilation
                .WithAssemblyName("CIARE_Debuggee_" + Guid.NewGuid().ToString("N"))
                .WithOptions(options);
        }

        private Dictionary<string, string> GetOpenSourceOverrides()
        {
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorDocument document in GetOpenEditorDocuments())
                sources[NormalizeDebuggerPath(document.FilePath)] = document.Editor.Text ?? string.Empty;
            return sources;
        }

        private List<DebugSourceBreakpoint> GetSourceBreakpoints()
        {
            var result = new List<DebugSourceBreakpoint>();
            foreach (EditorDocument document in GetOpenEditorDocuments())
            {
                result.AddRange(document.Editor.Document.BookmarkManager.Marks
                    .OfType<BreakpointBookmark>()
                    .Where(bookmark => bookmark.IsEnabled)
                    .Select(bookmark => new DebugSourceBreakpoint(
                        document.FilePath, bookmark.LineNumber + 1)));
            }
            return result;
        }

        private List<EditorDocument> GetOpenEditorDocuments()
        {
            var result = new List<EditorDocument>();
            if (EditorTabControl == null)
                return result;

            foreach (TabPage tab in EditorTabControl.TabPages)
            {
                TextEditorControl editor = tab.Controls.OfType<TextEditorControl>().FirstOrDefault();
                if (editor == null)
                    continue;

                string path = tab.ToolTipText?.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    if (editor != SelectedEditor.GetSelectedEditor())
                        continue;
                    path = DummyFileName;
                }
                result.Add(new EditorDocument(path, editor));
            }
            return result;
        }

        private static string[] GetDebuggerCommandLineArguments()
        {
            if (string.IsNullOrWhiteSpace(GlobalVariables.commandLineArguments))
                return Array.Empty<string>();
            try
            {
                return SplitArguments.CommandLineToArgs(GlobalVariables.commandLineArguments) ??
                    Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private void DebugSessionPrepared(object sender, DebugPreparedEventArgs args)
        {
            RunOnUiThread(() =>
            {
                int breakpointCount = 0;
                foreach (EditorDocument document in GetOpenEditorDocuments())
                {
                    foreach (BreakpointBookmark breakpoint in document.Editor.Document.BookmarkManager.Marks
                        .OfType<BreakpointBookmark>())
                    {
                        DebugSequencePoint resolvedPoint = ResolveDebuggerBreakpoint(
                            args.SequencePoints, document.FilePath, breakpoint.LineNumber + 1);
                        breakpoint.IsHealthy = resolvedPoint != null;
                        if (resolvedPoint != null &&
                            resolvedPoint.Line != breakpoint.LineNumber + 1)
                        {
                            breakpoint.Location = new TextLocation(0,
                                Math.Max(0, resolvedPoint.Line - 1));
                        }
                        breakpointCount++;
                    }
                    document.Editor.Refresh();
                }
                AppendDebuggerOutput($"Debugger: {args.SequencePoints.Count} sequence points, " +
                    $"{breakpointCount} breakpoint(s)." + Environment.NewLine);
            });
        }

        private static DebugSequencePoint ResolveDebuggerBreakpoint(
            IEnumerable<DebugSequencePoint> sequencePoints, string filePath, int requestedLine)
        {
            string normalizedPath = NormalizeDebuggerPath(filePath);
            DebugSequencePoint[] filePoints = sequencePoints
                .Where(point => string.Equals(
                    NormalizeDebuggerPath(point.FilePath), normalizedPath,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return filePoints
                .Where(point => point.Line == requestedLine)
                .OrderBy(point => point.Id)
                .FirstOrDefault()
                ?? filePoints
                    .Where(point => point.Line > requestedLine)
                    .OrderBy(point => point.Line)
                    .ThenBy(point => point.Id)
                    .FirstOrDefault()
                ?? filePoints
                    .Where(point => point.Line < requestedLine)
                    .OrderByDescending(point => point.Line)
                    .ThenBy(point => point.Id)
                    .FirstOrDefault();
        }

        private void DebugSessionPaused(object sender, DebugPausedEventArgs args)
        {
            RunOnUiThread(() =>
            {
                ShowCurrentStatement(args.SequencePoint);
                ShowDebuggerLocals(args.Variables);
                string reason = args.Reason == DebugPauseReason.Breakpoint ? "breakpoint" : "step";
                AppendDebuggerOutput($"Paused ({reason}) at {args.SequencePoint.FilePath}:" +
                    $"{args.SequencePoint.Line} [thread {args.ThreadId}]" + Environment.NewLine);
                UpdateDebuggerUi();
            });
        }

        private void DebugSessionStateChanged(object sender, EventArgs args)
        {
            RunOnUiThread(UpdateDebuggerUi);
        }

        private void DebugSessionEnded(object sender, DebugSessionEndedEventArgs args)
        {
            RunOnUiThread(() =>
            {
                if (!ReferenceEquals(sender, _debugSession))
                    return;

                ClearCurrentStatementMarker();
                ClearDebuggerLocals();
                outputTabControl.SelectedTab = outputTabPage;
                if (!string.IsNullOrWhiteSpace(args.ErrorMessage))
                    AppendDebuggerOutput("Debugger error:" + Environment.NewLine + args.ErrorMessage +
                        Environment.NewLine);
                AppendDebuggerOutput(args.StoppedByUser
                    ? "Debugging stopped." + Environment.NewLine
                    : "Debugging finished." + Environment.NewLine);

                DebugSession completedSession = _debugSession;
                _debugSession = null;
                _debugEditor = null;
                completedSession?.Dispose();
                UpdateDebuggerUi();

                TextEditorControl editor = SelectedEditor.GetSelectedEditor();
                if (editor != null)
                    ScheduleCurrentTypeCheck(editor);
            });
        }

        private void ShowCurrentStatement(DebugSequencePoint point)
        {
            ClearCurrentStatementMarker();
            TextEditorControl editor = FindDebuggerEditor(point.FilePath);
            if (editor == null && File.Exists(point.FilePath))
            {
                OpenFileFromExplorer(point.FilePath);
                editor = SelectedEditor.GetSelectedEditor();
            }
            editor ??= _debugEditor;
            if (editor == null || editor.Document.TotalNumberOfLines == 0)
                return;

            ConfigureDebuggerEditor(editor);
            RealTimeChecker.Cancel(editor, typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
            int line = Math.Max(0, Math.Min(point.Line - 1, editor.Document.TotalNumberOfLines - 1));
            LineSegment segment = editor.Document.GetLineSegment(line);
            _debugLineMarker = new TextMarker(segment.Offset, Math.Max(1, segment.Length),
                TextMarkerType.SolidBlock, Color.FromArgb(255, 224, 108), Color.Black)
            {
                ToolTip = "Current debugger statement"
            };
            _debugMarkerEditor = editor;
            editor.Document.MarkerStrategy.AddMarker(_debugLineMarker);
            editor.Document.BookmarkManager.AddMark(
                new CurrentStatementBookmark(editor.Document, new TextLocation(0, line)));
            editor.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, line));
            editor.Document.CommitUpdate();
            GoToLineNumber.GoToLine(editor, line + 1);
            editor.Focus();
        }

        private TextEditorControl FindDebuggerEditor(string filePath)
        {
            string normalized = NormalizeDebuggerPath(filePath);
            foreach (EditorDocument document in GetOpenEditorDocuments())
            {
                if (string.Equals(NormalizeDebuggerPath(document.FilePath), normalized,
                    StringComparison.OrdinalIgnoreCase))
                {
                    foreach (TabPage tab in EditorTabControl.TabPages)
                    {
                        if (tab.Controls.Contains(document.Editor))
                        {
                            EditorTabControl.SelectedTab = tab;
                            break;
                        }
                    }
                    return document.Editor;
                }
            }
            return null;
        }

        private void ClearCurrentStatementMarker()
        {
            TextEditorControl editor = _debugMarkerEditor;
            if (editor == null || editor.IsDisposed)
            {
                _debugLineMarker = null;
                _debugMarkerEditor = null;
                return;
            }

            if (_debugLineMarker != null)
                editor.Document.MarkerStrategy.RemoveMarker(_debugLineMarker);
            editor.Document.BookmarkManager.RemoveMarks(
                bookmark => bookmark is CurrentStatementBookmark);
            editor.Refresh();
            _debugLineMarker = null;
            _debugMarkerEditor = null;
        }

        private void StopDebugging()
        {
            ClearCurrentStatementMarker();
            ClearDebuggerLocals();
            outputTabControl.SelectedTab = outputTabPage;
            _debugSession?.Stop();
            UpdateDebuggerUi();
        }

        private void ShutdownDebugger()
        {
            _debugSession?.Stop();
            ClearCurrentStatementMarker();
            ClearDebuggerLocals();
        }

        private void ApplyDebuggerTheme(bool dark)
        {
            if (_debugMenu == null)
                return;

            // Clone the neighboring Compile menu palette so Debug participates in the
            // exact same renderer, hover colors, and theme changes as the designer menus.
            Color menuBackColor = compileToolStripMenuItem.BackColor;
            Color menuForeColor = compileToolStripMenuItem.ForeColor;
            Color itemBackColor = compileToexeCtrlShiftBToolStripMenuItem.BackColor;
            Color itemForeColor = compileToexeCtrlShiftBToolStripMenuItem.ForeColor;
            _debugMenu.BackColor = menuBackColor;
            _debugMenu.ForeColor = menuForeColor;
            _debugMenu.DropDown.BackColor = compileToolStripMenuItem.DropDown.BackColor;
            _debugMenu.DropDown.ForeColor = compileToolStripMenuItem.DropDown.ForeColor;
            _debugMenu.DropDown.Renderer = menuStrip1.Renderer;
            foreach (ToolStripItem item in _debugMenu.DropDownItems)
            {
                item.BackColor = itemBackColor;
                item.ForeColor = itemForeColor;
                if (item is ToolStripSeparator separator)
                {
                    separator.Paint -= RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
                    separator.Paint -= RenderToolStripSeparator.RenderToolStripSeparator_PaintLight;
                    separator.Paint += dark
                        ? RenderToolStripSeparator.RenderToolStripSeparator_PaintDark
                        : RenderToolStripSeparator.RenderToolStripSeparator_PaintLight;
                }
            }

            Color localsBackColor = dark ? GlobalVariables.controlBgColor : SystemColors.Window;
            Color localsForeColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            if (_debugLocalsTabPage != null)
                _debugLocalsTabPage.BackColor = localsBackColor;
            if (_debugLocalsList != null)
            {
                _debugLocalsList.BackColor = localsBackColor;
                _debugLocalsList.ForeColor = localsForeColor;
            }
        }

        private void ShowDebuggerLocals(IReadOnlyList<DebugVariableValue> variables)
        {
            if (_debugLocalsList == null || _debugLocalsList.IsDisposed)
                return;

            _debugLocalsList.BeginUpdate();
            try
            {
                _debugLocalsList.Items.Clear();
                foreach (DebugVariableValue variable in variables ?? Array.Empty<DebugVariableValue>())
                {
                    object value = variable.Value;
                    if (value is Type globalType && variable.Name.StartsWith(
                        "Globals (", StringComparison.Ordinal))
                    {
                        value = new DebuggerStaticMembers(globalType);
                    }
                    var node = new DebuggerValueNode(
                        variable.Name, variable.TypeName, value, 0);
                    _debugLocalsList.Items.Add(CreateDebuggerValueItem(node));
                }

                if (_debugLocalsList.Items.Count == 0)
                {
                    var emptyItem = new ListViewItem("No assigned variables are in scope.");
                    emptyItem.SubItems.Add(string.Empty);
                    emptyItem.SubItems.Add(string.Empty);
                    _debugLocalsList.Items.Add(emptyItem);
                }
            }
            finally
            {
                _debugLocalsList.EndUpdate();
            }

            OutputWindowManage.ShowOutputWindow(splitContainer1, outputRBT);
            if (_debugLocalsTabPage != null)
                outputTabControl.SelectedTab = _debugLocalsTabPage;
        }

        private void ClearDebuggerLocals()
        {
            if (_debugLocalsList == null || _debugLocalsList.IsDisposed)
                return;
            _debugLocalsList.Items.Clear();
        }

        private void DebuggerLocalsItemActivate(object sender, EventArgs args)
        {
            if (_debugLocalsList?.SelectedItems.Count != 1 ||
                !(_debugLocalsList.SelectedItems[0].Tag is DebuggerValueNode node))
            {
                return;
            }

            ListViewItem item = _debugLocalsList.SelectedItems[0];
            if (node.Expanded)
                CollapseDebuggerValue(item, node);
            else
                ExpandDebuggerValue(item, node);
        }

        private ListViewItem CreateDebuggerValueItem(DebuggerValueNode node)
        {
            string value = FormatDebuggerValue(node.Value);
            var item = new ListViewItem(GetDebuggerNodeName(node));
            item.SubItems.Add(value);
            item.SubItems.Add(node.TypeName);
            item.ToolTipText = $"{node.Name} = {value}";
            item.Tag = node;
            return item;
        }

        private static string GetDebuggerNodeName(DebuggerValueNode node)
        {
            string indent = new string(' ', node.Depth * 3);
            string marker = CanExpandDebuggerValue(node.Value)
                ? node.Expanded ? "▼ " : "▶ "
                : "  ";
            return indent + marker + node.Name;
        }

        private void ExpandDebuggerValue(ListViewItem item, DebuggerValueNode node)
        {
            if (node.Value is DebuggerDeferredProperty deferred)
            {
                try
                {
                    node.Value = deferred.Property.GetValue(deferred.Target);
                }
                catch (Exception exception)
                {
                    Exception root = exception is TargetInvocationException &&
                        exception.InnerException != null
                        ? exception.InnerException.GetBaseException()
                        : exception.GetBaseException();
                    node.Value = new DebuggerUnavailableValue(
                        root.GetType().Name + ": " + root.Message);
                }
                UpdateDebuggerValueItem(item, node);
            }

            if (!CanExpandDebuggerValue(node.Value))
                return;

            IReadOnlyList<DebuggerValueNode> children = CreateDebuggerChildren(node);
            if (children.Count == 0)
                return;
            node.Expanded = true;
            UpdateDebuggerValueItem(item, node);

            _debugLocalsList.BeginUpdate();
            try
            {
                int insertIndex = item.Index + 1;
                foreach (DebuggerValueNode child in children)
                    _debugLocalsList.Items.Insert(insertIndex++, CreateDebuggerValueItem(child));
            }
            finally
            {
                _debugLocalsList.EndUpdate();
            }
        }

        private void CollapseDebuggerValue(ListViewItem item, DebuggerValueNode node)
        {
            _debugLocalsList.BeginUpdate();
            try
            {
                int index = item.Index + 1;
                while (index < _debugLocalsList.Items.Count &&
                    _debugLocalsList.Items[index].Tag is DebuggerValueNode child &&
                    child.Depth > node.Depth)
                {
                    _debugLocalsList.Items.RemoveAt(index);
                }
                node.Expanded = false;
                UpdateDebuggerValueItem(item, node);
            }
            finally
            {
                _debugLocalsList.EndUpdate();
            }
        }

        private static void UpdateDebuggerValueItem(ListViewItem item, DebuggerValueNode node)
        {
            string value = FormatDebuggerValue(node.Value);
            item.Text = GetDebuggerNodeName(node);
            item.SubItems[1].Text = value;
            item.ToolTipText = $"{node.Name} = {value}";
        }

        private static IReadOnlyList<DebuggerValueNode> CreateDebuggerChildren(
            DebuggerValueNode parent)
        {
            if (parent.Value is DebuggerStaticMembers staticMembers)
                return CreateDebuggerStaticChildren(staticMembers.Type, parent.Depth + 1);

            if (parent.Value is DebuggerArrayRange range)
                return CreateDebuggerArrayItems(range.Array, range.Start, range.Count, parent.Depth + 1);

            if (parent.Value is Array array)
            {
                const int pageSize = 100;
                if (array.Length <= pageSize)
                    return CreateDebuggerArrayItems(array, 0, array.Length, parent.Depth + 1);

                var ranges = new List<DebuggerValueNode>();
                for (int start = 0; start < array.Length; start += pageSize)
                {
                    int count = Math.Min(pageSize, array.Length - start);
                    ranges.Add(new DebuggerValueNode(
                        $"[{start}..{start + count - 1}]",
                        array.GetType().GetElementType()?.Name + "[]",
                        new DebuggerArrayRange(array, start, count),
                        parent.Depth + 1));
                }
                return ranges;
            }

            object target = parent.Value;
            if (target == null)
                return Array.Empty<DebuggerValueNode>();

            Type targetType = target.GetType();
            var children = new List<DebuggerValueNode>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (FieldInfo field in targetType.GetFields(flags)
                .Where(field => !field.IsStatic)
                .OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
            {
                string name = GetDebuggerFieldName(field.Name);
                if (!names.Add(name))
                    continue;
                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(target);
                }
                catch (Exception exception)
                {
                    fieldValue = new DebuggerUnavailableValue(exception.GetBaseException().Message);
                }
                children.Add(new DebuggerValueNode(
                    name, GetDebuggerTypeName(field.FieldType), fieldValue, parent.Depth + 1));
            }

            foreach (PropertyInfo property in targetType.GetProperties(flags)
                .Where(property => property.GetMethod != null &&
                    !property.GetMethod.IsStatic && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!names.Add(property.Name))
                    continue;
                children.Add(new DebuggerValueNode(
                    property.Name,
                    GetDebuggerTypeName(property.PropertyType),
                    new DebuggerDeferredProperty(target, property),
                    parent.Depth + 1));
            }

            return children;
        }

        private static IReadOnlyList<DebuggerValueNode> CreateDebuggerStaticChildren(
            Type targetType, int depth)
        {
            var children = new List<DebuggerValueNode>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

            foreach (FieldInfo field in targetType.GetFields(flags)
                .Where(field => field.IsStatic && !field.IsSpecialName)
                .OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
            {
                string name = GetDebuggerFieldName(field.Name);
                if (!names.Add(name))
                    continue;
                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(null);
                }
                catch (Exception exception)
                {
                    fieldValue = new DebuggerUnavailableValue(exception.GetBaseException().Message);
                }
                children.Add(new DebuggerValueNode(
                    name, GetDebuggerTypeName(field.FieldType), fieldValue, depth));
            }

            foreach (PropertyInfo property in targetType.GetProperties(flags)
                .Where(property => property.GetMethod != null &&
                    property.GetMethod.IsStatic && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!names.Add(property.Name))
                    continue;
                children.Add(new DebuggerValueNode(
                    property.Name,
                    GetDebuggerTypeName(property.PropertyType),
                    new DebuggerDeferredProperty(null, property),
                    depth));
            }

            return children;
        }

        private static IReadOnlyList<DebuggerValueNode> CreateDebuggerArrayItems(
            Array array, int start, int count, int depth)
        {
            var items = new List<DebuggerValueNode>(count);
            Type elementType = array.GetType().GetElementType();
            for (int offset = start; offset < start + count; offset++)
            {
                int[] indices = GetDebuggerArrayIndices(array, offset);
                object value;
                try
                {
                    value = array.GetValue(indices);
                }
                catch (Exception exception)
                {
                    value = new DebuggerUnavailableValue(exception.GetBaseException().Message);
                }
                items.Add(new DebuggerValueNode(
                    "[" + string.Join(",", indices) + "]",
                    GetDebuggerTypeName(elementType), value, depth));
            }
            return items;
        }

        private static int[] GetDebuggerArrayIndices(Array array, int flatIndex)
        {
            var indices = new int[array.Rank];
            for (int dimension = array.Rank - 1; dimension >= 0; dimension--)
            {
                int length = array.GetLength(dimension);
                indices[dimension] = array.GetLowerBound(dimension) + flatIndex % length;
                flatIndex /= length;
            }
            return indices;
        }

        private static string GetDebuggerFieldName(string fieldName)
        {
            const string backingFieldSuffix = ">k__BackingField";
            return fieldName != null && fieldName.StartsWith("<", StringComparison.Ordinal) &&
                fieldName.EndsWith(backingFieldSuffix, StringComparison.Ordinal)
                ? fieldName.Substring(1, fieldName.Length - backingFieldSuffix.Length - 1)
                : fieldName ?? string.Empty;
        }

        private static string GetDebuggerTypeName(Type type)
        {
            return type?.FullName ?? type?.Name ?? string.Empty;
        }

        private static bool CanExpandDebuggerValue(object value)
        {
            if (value == null || value is DebuggerUnavailableValue)
                return false;
            if (value is DebuggerDeferredProperty || value is DebuggerArrayRange)
                return true;
            if (value is DebuggerStaticMembers)
                return true;
            if (value is Array array)
                return array.Length > 0;
            return !IsSimpleDebuggerValue(value);
        }

        private static bool IsSimpleDebuggerValue(object value)
        {
            if (value == null || value is string || value is char || value is bool ||
                value is decimal || value is DateTime || value is DateTimeOffset ||
                value is TimeSpan || value is Guid || value is DateOnly || value is TimeOnly)
            {
                return true;
            }
            Type type = value.GetType();
            return type.IsPrimitive || type.IsEnum;
        }

        private static string FormatDebuggerValue(object value)
        {
            return FormatDebuggerValue(value, 0);
        }

        private static string FormatDebuggerValue(object value, int depth)
        {
            if (value is DebuggerUnavailableValue unavailable)
                return "<" + unavailable.Message + ">";
            if (value is DebuggerDeferredProperty)
                return "<not evaluated>";
            if (value is DebuggerStaticMembers staticMembers)
                return "{" + staticMembers.Type.FullName + " static members}";
            if (value is DebuggerArrayRange range)
                return $"{{{range.Count} items}}";
            if (value == null)
                return "null";
            if (value is string text)
                return "\"" + EscapeDebuggerText(text) + "\"";
            if (value is char character)
                return "'" + EscapeDebuggerText(character.ToString()) + "'";
            if (value is bool boolean)
                return boolean ? "true" : "false";
            if (value is Array array)
            {
                return $"{{{array.GetType().GetElementType()?.Name ?? "item"}" +
                    $"[{string.Join(",", Enumerable.Range(0, array.Rank)
                        .Select(array.GetLength))}]}}";
            }

            Type valueType = value.GetType();
            if (valueType.IsEnum)
                return value.ToString();
            if (value is IFormattable formattable &&
                (valueType.IsPrimitive || value is decimal || value is DateTime ||
                 value is DateTimeOffset || value is TimeSpan || value is Guid ||
                 value is DateOnly || value is TimeOnly))
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            return "{" + (valueType.FullName ?? valueType.Name) + "}";
        }

        private static string EscapeDebuggerText(string value)
        {
            return LimitDebuggerText((value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\"", "\\\""));
        }

        private static string LimitDebuggerText(string value)
        {
            const int maximumLength = 512;
            return value != null && value.Length > maximumLength
                ? value.Substring(0, maximumLength) + "..."
                : value ?? string.Empty;
        }

        private void RunWithoutDebugging()
        {
            if (_debugSession?.IsActive == true)
                return;
            TextEditorControl editor = SelectedEditor.GetSelectedEditor();
            if (editor == null)
                return;
            RealTimeChecker.Cancel(editor, typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
            outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.RunCode(outputRBT, runCodePb, editor, splitContainer1, true);
        }

        private void UpdateDebuggerUi()
        {
            if (_debugMenu == null)
                return;
            DebugSessionState state = _debugSession?.State ?? DebugSessionState.Stopped;
            bool stopped = state == DebugSessionState.Stopped;
            bool paused = state == DebugSessionState.Paused;

            _debugStartContinueItem.Text = paused ? "Continue" : "Start Debugging";
            _debugStartContinueItem.Enabled = stopped || paused;
            _debugStopItem.Enabled = !stopped;
            _debugStepIntoItem.Enabled = stopped || paused;
            _debugStepOverItem.Enabled = stopped || paused;
            _debugStepOutItem.Enabled = paused;
            _debugStatusItem.Text = "Debugger: " + state.ToString().ToLowerInvariant();
            runCodePb.Enabled = stopped || paused;
            toolTip1.SetToolTip(runCodePb, paused
                ? "Continue debugging ( F5 )"
                : "Start debugging ( F5 )");
        }

        private void AppendDebuggerOutput(string text)
        {
            if (outputRBT == null || outputRBT.IsDisposed || string.IsNullOrEmpty(text))
                return;
            outputRBT.AppendText(text);
            outputRBT.SelectionStart = outputRBT.TextLength;
            outputRBT.ScrollToCaret();
        }

        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || Disposing)
                return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }
            action();
        }

        private static string MakeDebuggerLocationKey(string filePath, int line)
        {
            return NormalizeDebuggerPath(filePath) + "|" + line;
        }

        private static string NormalizeDebuggerPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;
            try
            {
                return Path.GetFullPath(filePath).TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return filePath.Trim();
            }
        }

        private sealed class EditorDocument
        {
            public EditorDocument(string filePath, TextEditorControl editor)
            {
                FilePath = filePath;
                Editor = editor;
            }

            public string FilePath { get; }
            public TextEditorControl Editor { get; }
        }
    }
}
