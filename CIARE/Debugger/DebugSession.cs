using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CIARE.Debugger
{
    internal enum DebugSessionState
    {
        Stopped,
        Starting,
        Running,
        Paused,
        Stopping
    }

    internal enum DebugCommand
    {
        Continue,
        StepInto,
        StepOver,
        StepOut
    }

    internal enum DebugPauseReason
    {
        Breakpoint,
        Step
    }

    internal sealed class DebugSourceBreakpoint
    {
        public DebugSourceBreakpoint(string filePath, int line)
        {
            FilePath = filePath ?? string.Empty;
            Line = line;
        }

        public string FilePath { get; }
        public int Line { get; }
    }

    internal sealed class DebugVariableValue
    {
        public DebugVariableValue(string name, string typeName, object value)
        {
            Name = name ?? string.Empty;
            TypeName = typeName ?? string.Empty;
            Value = value;
        }

        public string Name { get; }
        public string TypeName { get; }
        public object Value { get; }
    }

    internal sealed class DebugPreparedEventArgs : EventArgs
    {
        public DebugPreparedEventArgs(IEnumerable<DebugSequencePoint> sequencePoints)
        {
            SequencePoints = new ReadOnlyCollection<DebugSequencePoint>(sequencePoints.ToList());
        }

        public IReadOnlyList<DebugSequencePoint> SequencePoints { get; }
    }

    internal sealed class DebugPausedEventArgs : EventArgs
    {
        public DebugPausedEventArgs(DebugSequencePoint sequencePoint, int stackDepth,
            int threadId, DebugPauseReason reason, IEnumerable<DebugVariableValue> variables)
        {
            SequencePoint = sequencePoint;
            StackDepth = stackDepth;
            ThreadId = threadId;
            Reason = reason;
            Variables = new ReadOnlyCollection<DebugVariableValue>(
                (variables ?? Array.Empty<DebugVariableValue>()).ToList());
        }

        public DebugSequencePoint SequencePoint { get; }
        public int StackDepth { get; }
        public int ThreadId { get; }
        public DebugPauseReason Reason { get; }
        public IReadOnlyList<DebugVariableValue> Variables { get; }
    }

    internal sealed class DebugSessionEndedEventArgs : EventArgs
    {
        public DebugSessionEndedEventArgs(bool stoppedByUser, string errorMessage)
        {
            StoppedByUser = stoppedByUser;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool StoppedByUser { get; }
        public string ErrorMessage { get; }
    }

    /// <summary>
    /// Runs an instrumented Roslyn compilation and controls it with Visual Studio-style
    /// continue, step into, step over, step out, and stop commands.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class DebugSession : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly ManualResetEventSlim _resumeGate = new ManualResetEventSlim(false);
        private readonly List<DebugSourceBreakpoint> _requestedBreakpoints = new List<DebugSourceBreakpoint>();
        private readonly HashSet<int> _breakpointPointIds = new HashSet<int>();
        private readonly Dictionary<int, DebugSequencePoint> _sequencePoints =
            new Dictionary<int, DebugSequencePoint>();

        private Thread _worker;
        private Assembly _debuggeeAssembly;
        private DebugSessionState _state = DebugSessionState.Stopped;
        private DebugCommand _command = DebugCommand.Continue;
        private int _currentPointId = -1;
        private int _currentStackDepth;
        private DebugPauseReason _currentPauseReason = DebugPauseReason.Step;
        private int _resumePointId = -1;
        private int _resumeStackDepth;
        private int _skipBreakpointPointId = -1;
        private bool _stopRequested;
        private bool _disposed;

        public event EventHandler<DebugPreparedEventArgs> Prepared;
        public event EventHandler<DebugPausedEventArgs> Paused;
        public event EventHandler StateChanged;
        public event EventHandler<DebugSessionEndedEventArgs> Ended;

        public DebugSessionState State
        {
            get
            {
                lock (_syncRoot)
                    return _state;
            }
        }

        public bool IsActive => State != DebugSessionState.Stopped;

        public void Start(
            CSharpCompilation compilation,
            IDictionary<string, string> openSourceOverrides,
            IEnumerable<DebugSourceBreakpoint> breakpoints,
            string[] commandLineArguments,
            DebugCommand initialCommand)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                if (_state != DebugSessionState.Stopped)
                    throw new InvalidOperationException("A debug session is already active.");

                _stopRequested = false;
                _command = initialCommand;
                _resumePointId = -1;
                _resumeStackDepth = 0;
                _skipBreakpointPointId = -1;
                _requestedBreakpoints.Clear();
                if (breakpoints != null)
                    _requestedBreakpoints.AddRange(breakpoints);
                _state = DebugSessionState.Starting;
            }
            RaiseStateChanged();

            var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (openSourceOverrides != null)
            {
                foreach (KeyValuePair<string, string> item in openSourceOverrides)
                    overrides[NormalizePath(item.Key)] = item.Value ?? string.Empty;
            }

            _worker = new Thread(() => Execute(compilation, overrides,
                commandLineArguments ?? Array.Empty<string>()))
            {
                IsBackground = true,
                Name = "CIARE source debugger"
            };
            _worker.SetApartmentState(ApartmentState.STA);
            _worker.Start();
        }

        public void UpdateBreakpoints(IEnumerable<DebugSourceBreakpoint> breakpoints)
        {
            lock (_syncRoot)
            {
                _requestedBreakpoints.Clear();
                if (breakpoints != null)
                    _requestedBreakpoints.AddRange(breakpoints);
                BindBreakpointsNoLock();
            }
        }

        public void Resume(DebugCommand command)
        {
            bool changed = false;
            lock (_syncRoot)
            {
                if (_state != DebugSessionState.Paused)
                    return;

                _command = command;
                _resumePointId = _currentPointId;
                _resumeStackDepth = _currentStackDepth;
                _skipBreakpointPointId = _currentPointId;
                _state = DebugSessionState.Running;
                _resumeGate.Set();
                changed = true;
            }
            if (changed)
                RaiseStateChanged();
        }

        public void Stop()
        {
            bool changed = false;
            lock (_syncRoot)
            {
                if (_state == DebugSessionState.Stopped || _state == DebugSessionState.Stopping)
                    return;

                _stopRequested = true;
                _state = DebugSessionState.Stopping;
                _resumeGate.Set();
                changed = true;
            }
            if (changed)
            {
                try
                {
                    _worker?.Interrupt();
                }
                catch (ThreadStateException)
                {
                }
                RaiseStateChanged();
            }
        }

        private void Execute(CSharpCompilation originalCompilation,
            IDictionary<string, string> openSourceOverrides,
            string[] commandLineArguments)
        {
            AssemblyLoadContext runContext = null;
            bool stoppedByUser = false;
            string errorMessage = string.Empty;

            try
            {
                CSharpCompilation compilation = InstrumentCompilation(
                    originalCompilation, openSourceOverrides, out List<DebugSequencePoint> points);

                lock (_syncRoot)
                {
                    _sequencePoints.Clear();
                    foreach (DebugSequencePoint point in points)
                        _sequencePoints[point.Id] = point;
                    BindBreakpointsNoLock();
                }
                Prepared?.Invoke(this, new DebugPreparedEventArgs(points));
                ThrowIfStopRequested();

                using var assemblyStream = new MemoryStream();
                EmitResult emitResult = compilation.Emit(assemblyStream);
                if (!emitResult.Success)
                {
                    errorMessage = FormatDiagnostics(emitResult.Diagnostics);
                    return;
                }

                assemblyStream.Position = 0;
                runContext = new AssemblyLoadContext("CIARE debugger", isCollectible: true);
                ConfigureAssemblyResolution(runContext, compilation.References);
                Assembly assembly = runContext.LoadFromStream(assemblyStream);
                _debuggeeAssembly = assembly;

                Type bridgeType = assembly.GetType(DebugInstrumenter.BridgeTypeName, throwOnError: true);
                FieldInfo shouldPauseHandler = bridgeType.GetField("ShouldPauseHandler",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                FieldInfo pauseHandler = bridgeType.GetField("PauseHandler",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                shouldPauseHandler.SetValue(null, new Func<int, bool>(ShouldPauseAtSequencePoint));
                pauseHandler.SetValue(null,
                    new Action<int, string[], string[], object[]>(PauseAtSequencePoint));

                MethodInfo entryPoint = assembly.EntryPoint;
                if (entryPoint == null)
                {
                    errorMessage = "The selected build target does not have an executable entry point.";
                    return;
                }

                ChangeState(DebugSessionState.Running);
                object[] parameters = entryPoint.GetParameters().Length == 0
                    ? null
                    : new object[] { commandLineArguments };
                object result = entryPoint.Invoke(null, parameters);
                if (result is Task task)
                    task.GetAwaiter().GetResult();

                shouldPauseHandler.SetValue(null, null);
                pauseHandler.SetValue(null, null);
            }
            catch (TargetInvocationException exception) when (
                exception.InnerException?.GetBaseException() is DebuggeeStoppedException)
            {
                stoppedByUser = true;
            }
            catch (DebuggeeStoppedException)
            {
                stoppedByUser = true;
            }
            catch (Exception) when (IsStopRequested())
            {
                stoppedByUser = true;
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException.GetBaseException()
                    : exception.GetBaseException();
                errorMessage = root.ToString();
            }
            finally
            {
                lock (_syncRoot)
                {
                    stoppedByUser |= _stopRequested;
                    _state = DebugSessionState.Stopped;
                    _debuggeeAssembly = null;
                    _currentPointId = -1;
                    _currentStackDepth = 0;
                    _resumeGate.Set();
                }
                runContext?.Unload();
                RaiseStateChanged();
                Ended?.Invoke(this, new DebugSessionEndedEventArgs(stoppedByUser, errorMessage));
            }
        }

        private CSharpCompilation InstrumentCompilation(
            CSharpCompilation compilation,
            IDictionary<string, string> openSourceOverrides,
            out List<DebugSequencePoint> allPoints)
        {
            allPoints = new List<DebugSequencePoint>();
            var sourceTrees = new List<SyntaxTree>();
            var instrumentedTrees = new HashSet<SyntaxTree>();
            CSharpParseOptions bridgeParseOptions = null;

            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                CSharpParseOptions parseOptions = tree.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
                bridgeParseOptions ??= parseOptions;
                string filePath = tree.FilePath ?? string.Empty;
                string pathKey = NormalizePath(filePath);
                string source = openSourceOverrides.TryGetValue(pathKey, out string currentSource)
                    ? currentSource
                    : tree.GetText().ToString();
                SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions, filePath);
                sourceTrees.Add(sourceTree);

                if (ShouldInstrumentTree(filePath, openSourceOverrides.ContainsKey(pathKey)))
                    instrumentedTrees.Add(sourceTree);
            }

            CSharpCompilation analysisCompilation = compilation
                .RemoveAllSyntaxTrees()
                .AddSyntaxTrees(sourceTrees);
            var rewrittenTrees = new List<SyntaxTree>(sourceTrees.Count + 1);
            int nextPointId = 1;

            foreach (SyntaxTree sourceTree in sourceTrees)
            {
                if (!instrumentedTrees.Contains(sourceTree))
                {
                    rewrittenTrees.Add(sourceTree);
                    continue;
                }

                DebugInstrumentationResult result = DebugInstrumenter.Instrument(
                    sourceTree,
                    analysisCompilation.GetSemanticModel(sourceTree, ignoreAccessibility: true),
                    nextPointId);
                allPoints.AddRange(result.SequencePoints);
                if (result.SequencePoints.Count > 0)
                    nextPointId = result.SequencePoints[result.SequencePoints.Count - 1].Id + 1;
                rewrittenTrees.Add(CSharpSyntaxTree.ParseText(
                    result.Code, sourceTree.Options as CSharpParseOptions,
                    sourceTree.FilePath));
            }

            rewrittenTrees.Add(CSharpSyntaxTree.ParseText(
                DebugInstrumenter.BridgeSource,
                bridgeParseOptions ?? CSharpParseOptions.Default,
                "__CIARE_Debugger_Bridge.g.cs"));

            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(rewrittenTrees);
        }

        private static bool ShouldInstrumentTree(string filePath, bool isOpenDocument)
        {
            if (isOpenDocument)
                return true;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;
            if (!string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return false;

            string normalized = filePath.Replace('/', '\\');
            return normalized.IndexOf("\\bin\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                normalized.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private bool ShouldPauseAtSequencePoint(int sequencePointId)
        {
            bool waitForOtherPausedThread = false;
            bool shouldCapture = false;

            lock (_syncRoot)
            {
                if (_stopRequested)
                    throw new DebuggeeStoppedException();

                if (_state == DebugSessionState.Paused)
                {
                    waitForOtherPausedThread = true;
                }
                else if (_sequencePoints.TryGetValue(sequencePointId, out DebugSequencePoint point))
                {
                    int stackDepth = GetUserStackDepth();
                    DebugPauseReason reason;
                    if (ShouldPauseNoLock(sequencePointId, stackDepth, out reason))
                    {
                        _currentPointId = sequencePointId;
                        _currentStackDepth = stackDepth;
                        _currentPauseReason = reason;
                        _state = DebugSessionState.Paused;
                        _resumeGate.Reset();
                        shouldCapture = true;
                    }
                }
            }

            if (waitForOtherPausedThread)
            {
                _resumeGate.Wait();
                ThrowIfStopRequested();
                return false;
            }

            return shouldCapture;
        }

        private void PauseAtSequencePoint(int sequencePointId, string[] names,
            string[] types, object[] values)
        {
            DebugPausedEventArgs pauseArgs;
            lock (_syncRoot)
            {
                if (_stopRequested)
                    throw new DebuggeeStoppedException();
                if (_state != DebugSessionState.Paused || _currentPointId != sequencePointId ||
                    !_sequencePoints.TryGetValue(sequencePointId, out DebugSequencePoint point))
                {
                    return;
                }

                int variableCount = Math.Min(
                    names?.Length ?? 0,
                    Math.Min(types?.Length ?? 0, values?.Length ?? 0));
                var variables = new List<DebugVariableValue>(variableCount);
                for (int index = 0; index < variableCount; index++)
                    variables.Add(new DebugVariableValue(names[index], types[index], values[index]));

                pauseArgs = new DebugPausedEventArgs(point, _currentStackDepth,
                    Environment.CurrentManagedThreadId, _currentPauseReason, variables);
            }

            if (pauseArgs == null)
                return;

            RaiseStateChanged();
            Paused?.Invoke(this, pauseArgs);
            _resumeGate.Wait();
            ThrowIfStopRequested();
        }

        private bool ShouldPauseNoLock(int sequencePointId, int stackDepth,
            out DebugPauseReason reason)
        {
            if (sequencePointId != _skipBreakpointPointId)
                _skipBreakpointPointId = -1;

            if (_breakpointPointIds.Contains(sequencePointId) &&
                sequencePointId != _skipBreakpointPointId)
            {
                reason = DebugPauseReason.Breakpoint;
                return true;
            }

            bool shouldPause = _command switch
            {
                DebugCommand.StepInto => sequencePointId != _resumePointId,
                DebugCommand.StepOver => sequencePointId != _resumePointId &&
                    stackDepth <= _resumeStackDepth,
                DebugCommand.StepOut => stackDepth < _resumeStackDepth,
                _ => false
            };
            reason = DebugPauseReason.Step;
            return shouldPause;
        }

        private int GetUserStackDepth()
        {
            Assembly debuggee = _debuggeeAssembly;
            if (debuggee == null)
                return 0;

            StackFrame[] frames = new StackTrace().GetFrames();
            return frames?.Count(frame => frame.GetMethod()?.DeclaringType?.Assembly == debuggee) ?? 0;
        }

        private void BindBreakpointsNoLock()
        {
            _breakpointPointIds.Clear();
            foreach (DebugSourceBreakpoint breakpoint in _requestedBreakpoints)
            {
                string path = NormalizePath(breakpoint.FilePath);
                DebugSequencePoint point = _sequencePoints.Values
                    .Where(candidate => candidate.Line == breakpoint.Line &&
                        string.Equals(NormalizePath(candidate.FilePath), path,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(candidate => candidate.Id)
                    .FirstOrDefault();
                if (point != null)
                    _breakpointPointIds.Add(point.Id);
            }
        }

        private static void ConfigureAssemblyResolution(AssemblyLoadContext context,
            IEnumerable<MetadataReference> references)
        {
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (PortableExecutableReference reference in references.OfType<PortableExecutableReference>())
            {
                string path = reference.FilePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;
                try
                {
                    string name = AssemblyName.GetAssemblyName(path).Name;
                    if (!string.IsNullOrEmpty(name) && !paths.ContainsKey(name))
                        paths[name] = path;
                }
                catch
                {
                }
            }

            context.Resolving += (loadContext, assemblyName) =>
            {
                Assembly loaded = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(
                    assembly => string.Equals(assembly.GetName().Name, assemblyName.Name,
                        StringComparison.OrdinalIgnoreCase));
                if (loaded != null)
                    return loaded;
                return paths.TryGetValue(assemblyName.Name ?? string.Empty, out string path)
                    ? loadContext.LoadFromAssemblyPath(path)
                    : null;
            };
        }

        private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            var messages = diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error ||
                    diagnostic.IsWarningAsError)
                .Select(diagnostic =>
                {
                    FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
                    string location = span.IsValid
                        ? $"{span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})"
                        : string.Empty;
                    return string.IsNullOrEmpty(location)
                        ? $"{diagnostic.Id}: {diagnostic.GetMessage()}"
                        : $"{location}: {diagnostic.Id}: {diagnostic.GetMessage()}";
                });
            return string.Join(Environment.NewLine, messages);
        }

        private void ChangeState(DebugSessionState state)
        {
            lock (_syncRoot)
            {
                if (_state == DebugSessionState.Stopping)
                    return;
                _state = state;
            }
            RaiseStateChanged();
        }

        private void ThrowIfStopRequested()
        {
            lock (_syncRoot)
            {
                if (_stopRequested)
                    throw new DebuggeeStoppedException();
            }
        }

        private bool IsStopRequested()
        {
            lock (_syncRoot)
                return _stopRequested;
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
                return path.Trim();
            }
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DebugSession));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            Stop();
            _disposed = true;
        }

        private sealed class DebuggeeStoppedException : OperationCanceledException
        {
        }
    }
}
