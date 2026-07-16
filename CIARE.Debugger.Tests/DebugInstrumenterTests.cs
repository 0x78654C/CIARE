using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CIARE.Debugger;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CIARE.Debugger.Tests
{
    public sealed class DebugInstrumenterTests
    {
        private static readonly CSharpParseOptions ParseOptions =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        [Fact]
        public void InstrumentedControlFlowCompilesAndKeepsOriginalLineMapping()
        {
            const string source = @"using System;
class Program
{
    static void Main()
    {
        int value = 1;
        if (value > 0) Console.WriteLine(value);
        for (int index = 0; index < 2; index++) value += index;
    }
}";

            DebugInstrumentationResult result = DebugInstrumenter.Instrument(
                source, ParseOptions, "Program.cs");

            Assert.Contains(result.SequencePoints, point => point.Line == 6);
            Assert.Contains(result.SequencePoints, point => point.Line == 7);
            Assert.Contains(result.SequencePoints, point => point.Line == 8);
            Assert.Equal(result.SequencePoints.Count,
                result.SequencePoints.Select(point => point.Id).Distinct().Count());
            AssertCompiles(result.Code, "Program.cs");
        }

        [Fact]
        public void InstrumentedTopLevelAndFileScopedProgramsCompile()
        {
            const string topLevelSource = @"using System;
int value = 2;
if (value > 0) Console.WriteLine(value);
static int Double(int input) => input * 2;";
            DebugInstrumentationResult topLevel = DebugInstrumenter.Instrument(
                topLevelSource, ParseOptions, "TopLevel.cs", 40);

            Assert.Equal(40, topLevel.SequencePoints[0].Id);
            AssertCompiles(topLevel.Code, "TopLevel.cs");

            const string fileScopedSource = @"namespace Sample;
public static class Worker
{
    public static void Run()
    {
        int value = 0;
        value++;
    }
}";
            DebugInstrumentationResult fileScoped = DebugInstrumenter.Instrument(
                fileScopedSource, ParseOptions, "Worker.cs");

            AssertCompiles(fileScoped.Code, "Worker.cs", OutputKind.DynamicallyLinkedLibrary);
        }

        [Fact]
        public void InstrumentedLabelsSwitchesAndEmbeddedStatementsCompile()
        {
            const string source = @"using System;
class Program
{
    static void Main()
    {
        int value = 1;
        while (value < 2) value++;
        switch (value)
        {
            case 2:
                goto done;
            default:
                break;
        }
    done:
        Console.WriteLine(value);
    }
}";

            DebugInstrumentationResult result = DebugInstrumenter.Instrument(
                source, ParseOptions, "Labels.cs");

            AssertCompiles(result.Code, "Labels.cs");
        }

        private static void AssertCompiles(string instrumentedCode, string path,
            OutputKind outputKind = OutputKind.ConsoleApplication)
        {
            CSharpCompilation compilation = CreateCompilation(instrumentedCode, path, outputKind);
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        }

        internal static CSharpCompilation CreateCompilation(string source, string path,
            OutputKind outputKind = OutputKind.ConsoleApplication)
        {
            SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(source, ParseOptions, path);
            SyntaxTree bridgeTree = CSharpSyntaxTree.ParseText(
                DebugInstrumenter.BridgeSource, ParseOptions, "__CIARE_Debugger_Bridge.g.cs");
            string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            IEnumerable<MetadataReference> references = trustedAssemblies
                .Split(Path.PathSeparator)
                .Select(reference => MetadataReference.CreateFromFile(reference));
            return CSharpCompilation.Create(
                "DebuggerTest_" + Guid.NewGuid().ToString("N"),
                new[] { sourceTree, bridgeTree },
                references,
                new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Debug));
        }
    }
}
