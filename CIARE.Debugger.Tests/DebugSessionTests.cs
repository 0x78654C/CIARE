using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CIARE.Debugger;
using Xunit;

namespace CIARE.Debugger.Tests
{
    public sealed class DebugSessionTests
    {
        [Fact]
        public void StepOverIntoAndOutFollowUserCallDepth()
        {
            const string path = "Stepping.cs";
            const string source = @"class Program
{
    static void Main()
    {
        int value = 1;
        Work();
        value++;
    }

    static void Work()
    {
        int nested = 0;
        nested++;
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            // DebugSession adds its own bridge tree after rewriting all source trees.
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            using var session = new DebugSession();
            using var paused = new AutoResetEvent(false);
            using var ended = new ManualResetEventSlim(false);
            DebugPausedEventArgs lastPause = null;
            DebugSessionEndedEventArgs endArgs = null;
            session.Paused += (sender, args) =>
            {
                lastPause = args;
                paused.Set();
            };
            session.Ended += (sender, args) =>
            {
                endArgs = args;
                ended.Set();
            };

            session.Start(
                compilation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [path] = source
                },
                Array.Empty<DebugSourceBreakpoint>(),
                Array.Empty<string>(),
                DebugCommand.StepInto);

            Assert.True(paused.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.Equal(5, lastPause.SequencePoint.Line);

            session.Resume(DebugCommand.StepOver);
            Assert.True(paused.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.Equal(6, lastPause.SequencePoint.Line);

            session.Resume(DebugCommand.StepInto);
            Assert.True(paused.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.Equal(12, lastPause.SequencePoint.Line);

            session.Resume(DebugCommand.StepOut);
            Assert.True(paused.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.Equal(7, lastPause.SequencePoint.Line);

            session.Stop();
            Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(endArgs.StoppedByUser);
            Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
        }

        [Fact]
        public void ContinueStopsAtConfiguredBreakpointAndThenFinishes()
        {
            const string path = "Breakpoint.cs";
            const string source = @"class Program
{
    static void Main()
    {
        int value = 1;
        value++;
        value++;
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            using var session = new DebugSession();
            using var paused = new ManualResetEventSlim(false);
            using var ended = new ManualResetEventSlim(false);
            DebugPausedEventArgs pauseArgs = null;
            DebugSessionEndedEventArgs endArgs = null;
            session.Paused += (sender, args) =>
            {
                pauseArgs = args;
                paused.Set();
            };
            session.Ended += (sender, args) =>
            {
                endArgs = args;
                ended.Set();
            };

            session.Start(
                compilation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [path] = source
                },
                new[] { new DebugSourceBreakpoint(path, 6) },
                Array.Empty<string>(),
                DebugCommand.Continue);

            Assert.True(paused.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(DebugPauseReason.Breakpoint, pauseArgs.Reason);
            Assert.Equal(6, pauseArgs.SequencePoint.Line);

            session.Resume(DebugCommand.Continue);
            Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
            Assert.False(endArgs.StoppedByUser);
            Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
        }

        [Fact]
        public void PauseIncludesOnlyVariablesAssignedBeforeTheCurrentStatement()
        {
            const string path = "Locals.cs";
            const string source = @"class Program
{
    static void Main(string[] args)
    {
        int assigned = 41;
        string text = ""ready"";
        int assignedLater;
        assigned++;
        assignedLater = 9;
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            using var session = new DebugSession();
            using var paused = new ManualResetEventSlim(false);
            using var ended = new ManualResetEventSlim(false);
            DebugPausedEventArgs pauseArgs = null;
            DebugSessionEndedEventArgs endArgs = null;
            session.Paused += (sender, args) =>
            {
                pauseArgs = args;
                paused.Set();
            };
            session.Ended += (sender, args) =>
            {
                endArgs = args;
                ended.Set();
            };

            session.Start(
                compilation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [path] = source
                },
                new[] { new DebugSourceBreakpoint(path, 8) },
                new[] { "first" },
                DebugCommand.Continue);

            Assert.True(paused.Wait(TimeSpan.FromSeconds(10)));
            Dictionary<string, DebugVariableValue> variables = pauseArgs.Variables
                .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
            Assert.Equal(41, variables["assigned"].Value);
            Assert.Equal("ready", variables["text"].Value);
            Assert.Equal(new[] { "first" }, Assert.IsType<string[]>(variables["args"].Value));
            Assert.DoesNotContain("assignedLater", variables.Keys);

            paused.Reset();
            session.Resume(DebugCommand.StepOver);
            Assert.True(paused.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(9, pauseArgs.SequencePoint.Line);
            variables = pauseArgs.Variables
                .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
            Assert.Equal(42, variables["assigned"].Value);
            Assert.DoesNotContain("assignedLater", variables.Keys);

            session.Stop();
            Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(endArgs.StoppedByUser);
            Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
        }

        [Fact]
        public void PauseProvidesLazyInstanceAndStaticRoots()
        {
            const string path = "Fields.cs";
            const string source = @"class Program
{
    static int GlobalCount = 7;
    int instanceCount = 3;

    static void Main()
    {
        new Program().Run();
    }

    void Run()
    {
        int local = 11;
        local++;
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            using var session = new DebugSession();
            using var paused = new ManualResetEventSlim(false);
            using var ended = new ManualResetEventSlim(false);
            DebugPausedEventArgs pauseArgs = null;
            DebugSessionEndedEventArgs endArgs = null;
            session.Paused += (sender, args) =>
            {
                pauseArgs = args;
                paused.Set();
            };
            session.Ended += (sender, args) =>
            {
                endArgs = args;
                ended.Set();
            };

            session.Start(
                compilation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [path] = source
                },
                new[] { new DebugSourceBreakpoint(path, 14) },
                Array.Empty<string>(),
                DebugCommand.Continue);

            Assert.True(paused.Wait(TimeSpan.FromSeconds(10)));
            Dictionary<string, DebugVariableValue> variables = pauseArgs.Variables
                .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
            Assert.Equal(11, variables["local"].Value);
            object instance = variables["this"].Value;
            Assert.Equal(3, instance.GetType().GetField("instanceCount",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(instance));
            Type globals = Assert.IsAssignableFrom<Type>(variables["Globals (Program)"].Value);
            Assert.Equal(7, globals.GetField("GlobalCount",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic).GetValue(null));

            session.Stop();
            Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(endArgs.StoppedByUser);
            Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
        }

        [Fact]
        public void BreakpointOnNonExecutableLineMovesToNextStatement()
        {
            const string path = "RelocatedBreakpoint.cs";
            const string source = @"class Program
{
    static void Main()
    {
        int value = 1;
        value++;
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            using var session = new DebugSession();
            using var paused = new ManualResetEventSlim(false);
            using var ended = new ManualResetEventSlim(false);
            DebugPausedEventArgs pauseArgs = null;
            DebugSessionEndedEventArgs endArgs = null;
            session.Paused += (sender, args) =>
            {
                pauseArgs = args;
                paused.Set();
            };
            session.Ended += (sender, args) =>
            {
                endArgs = args;
                ended.Set();
            };

            session.Start(
                compilation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [path] = source
                },
                new[] { new DebugSourceBreakpoint(path, 4) },
                Array.Empty<string>(),
                DebugCommand.Continue);

            Assert.True(paused.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(5, pauseArgs.SequencePoint.Line);

            session.Stop();
            Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(endArgs.StoppedByUser);
            Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
        }

        [Fact]
        public void DebugSessionPreservesConsoleOutputAndStructConstructors()
        {
            const string path = "Output.cs";
            const string source = @"using System;
struct Value
{
    int number;
    public Value(int number)
    {
        this.number = number;
    }
    public override string ToString() => number.ToString();
}
class Program
{
    static void Main()
    {
        var value = new Value(5);
        Console.WriteLine(""debug-output:"" + value);
    }
}";
            var compilation = DebugInstrumenterTests.CreateCompilation(source, path);
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees[1]);

            TextWriter originalOutput = Console.Out;
            using var output = new StringWriter();
            using var session = new DebugSession();
            using var ended = new ManualResetEventSlim(false);
            DebugSessionEndedEventArgs endArgs = null;
            try
            {
                Console.SetOut(output);
                session.Ended += (sender, args) =>
                {
                    endArgs = args;
                    ended.Set();
                };
                session.Start(
                    compilation,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [path] = source
                    },
                    Array.Empty<DebugSourceBreakpoint>(),
                    Array.Empty<string>(),
                    DebugCommand.Continue);

                Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
                Assert.False(endArgs.StoppedByUser);
                Assert.True(string.IsNullOrEmpty(endArgs.ErrorMessage), endArgs.ErrorMessage);
                Assert.Contains("debug-output:5", output.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }
}
