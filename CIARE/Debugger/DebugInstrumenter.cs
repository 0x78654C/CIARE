using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE.Debugger
{
    internal sealed class DebugSequencePoint
    {
        public DebugSequencePoint(int id, string filePath, int line)
        {
            Id = id;
            FilePath = filePath ?? string.Empty;
            Line = line;
        }

        public int Id { get; }
        public string FilePath { get; }
        public int Line { get; }
    }

    internal sealed class DebugInstrumentationResult
    {
        public DebugInstrumentationResult(string code, IEnumerable<DebugSequencePoint> sequencePoints)
        {
            Code = code ?? string.Empty;
            SequencePoints = new ReadOnlyCollection<DebugSequencePoint>(sequencePoints.ToList());
        }

        public string Code { get; }
        public IReadOnlyList<DebugSequencePoint> SequencePoints { get; }
    }

    /// <summary>
    /// Adds lightweight callbacks before executable C# statements. The callbacks are used by
    /// <see cref="DebugSession"/> to implement breakpoints and source-level stepping without an
    /// external debugger process.
    /// </summary>
    internal static class DebugInstrumenter
    {
        internal const string BridgeTypeName = "__CIARE_Internal_Debugger_Bridge_8F43A7D2";

        internal static string BridgeSource => $@"
internal static class {BridgeTypeName}
{{
    internal static global::System.Action<int> HitHandler;

    internal static void Hit(int sequencePoint)
    {{
        global::System.Action<int> handler = HitHandler;
        if (handler != null)
            handler(sequencePoint);
    }}
}}";

        public static DebugInstrumentationResult Instrument(
            string code,
            CSharpParseOptions parseOptions,
            string filePath,
            int firstSequencePointId = 1)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                code ?? string.Empty,
                parseOptions ?? CSharpParseOptions.Default,
                filePath ?? string.Empty);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var rewriter = new SequencePointRewriter(filePath, firstSequencePointId);
            CompilationUnitSyntax rewritten = (CompilationUnitSyntax)rewriter.Visit(root);
            return new DebugInstrumentationResult(rewritten.ToFullString(), rewriter.SequencePoints);
        }

        private sealed class SequencePointRewriter : CSharpSyntaxRewriter
        {
            private readonly string _filePath;
            private readonly List<DebugSequencePoint> _sequencePoints = new List<DebugSequencePoint>();
            private int _nextSequencePointId;

            public SequencePointRewriter(string filePath, int firstSequencePointId)
            {
                _filePath = filePath ?? string.Empty;
                _nextSequencePointId = firstSequencePointId;
            }

            public IReadOnlyList<DebugSequencePoint> SequencePoints => _sequencePoints;

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                var visited = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
                var members = new List<MemberDeclarationSyntax>(visited.Members.Count * 2);

                for (int index = 0; index < visited.Members.Count; index++)
                {
                    MemberDeclarationSyntax originalMember = node.Members[index];
                    MemberDeclarationSyntax visitedMember = visited.Members[index];
                    if (originalMember is GlobalStatementSyntax originalGlobal &&
                        visitedMember is GlobalStatementSyntax visitedGlobal &&
                        ShouldInstrument(originalGlobal.Statement))
                    {
                        StatementSyntax point = CreateSequencePoint(originalGlobal.Statement, moveLeadingTrivia: true);
                        members.Add(SyntaxFactory.GlobalStatement(point));
                        members.Add(visitedGlobal.WithStatement(
                            visitedGlobal.Statement.WithoutLeadingTrivia()));
                    }
                    else
                    {
                        members.Add(visitedMember);
                    }
                }

                return visited.WithMembers(SyntaxFactory.List(members));
            }

            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                return node.WithStatements(RewriteStatementList(node.Statements));
            }

            public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
            {
                var visitedLabels = SyntaxFactory.List(node.Labels.Select(label => (SwitchLabelSyntax)Visit(label)));
                return node.WithLabels(visitedLabels).WithStatements(RewriteStatementList(node.Statements));
            }

            public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
            {
                var visited = (IfStatementSyntax)base.VisitIfStatement(node);
                visited = visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
                if (node.Else != null && visited.Else != null)
                {
                    visited = visited.WithElse(visited.Else.WithStatement(
                        WrapEmbeddedStatement(node.Else.Statement, visited.Else.Statement)));
                }
                return visited;
            }

            public override SyntaxNode VisitForStatement(ForStatementSyntax node)
            {
                var visited = (ForStatementSyntax)base.VisitForStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
            {
                var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
            {
                var visited = (ForEachVariableStatementSyntax)base.VisitForEachVariableStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
            {
                var visited = (WhileStatementSyntax)base.VisitWhileStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
            {
                var visited = (DoStatementSyntax)base.VisitDoStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
            {
                var visited = (UsingStatementSyntax)base.VisitUsingStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitLockStatement(LockStatementSyntax node)
            {
                var visited = (LockStatementSyntax)base.VisitLockStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitFixedStatement(FixedStatementSyntax node)
            {
                var visited = (FixedStatementSyntax)base.VisitFixedStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node)
            {
                var visited = (LabeledStatementSyntax)base.VisitLabeledStatement(node);
                return visited.WithStatement(WrapEmbeddedStatement(node.Statement, visited.Statement));
            }

            private SyntaxList<StatementSyntax> RewriteStatementList(SyntaxList<StatementSyntax> statements)
            {
                var rewritten = new List<StatementSyntax>(statements.Count * 2);
                foreach (StatementSyntax original in statements)
                {
                    StatementSyntax visited = (StatementSyntax)Visit(original);
                    if (ShouldInstrument(original) && !(original is LabeledStatementSyntax))
                    {
                        rewritten.Add(CreateSequencePoint(original, moveLeadingTrivia: true));
                        visited = visited.WithoutLeadingTrivia();
                    }
                    rewritten.Add(visited);
                }
                return SyntaxFactory.List(rewritten);
            }

            private StatementSyntax WrapEmbeddedStatement(StatementSyntax original, StatementSyntax visited)
            {
                if (original is BlockSyntax || !ShouldInstrument(original))
                    return visited;

                StatementSyntax point = CreateSequencePoint(original, moveLeadingTrivia: false);
                return SyntaxFactory.Block(point, visited.WithoutLeadingTrivia())
                    .WithTriviaFrom(visited);
            }

            private static bool ShouldInstrument(StatementSyntax statement)
            {
                return statement != null &&
                    !statement.IsMissing &&
                    !(statement is LocalFunctionStatementSyntax);
            }

            private StatementSyntax CreateSequencePoint(StatementSyntax source, bool moveLeadingTrivia)
            {
                int id = _nextSequencePointId++;
                int line = source.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                _sequencePoints.Add(new DebugSequencePoint(id, _filePath, line));

                StatementSyntax point = SyntaxFactory.ParseStatement(
                    $"global::{BridgeTypeName}.Hit({id});");
                return moveLeadingTrivia
                    ? point.WithLeadingTrivia(source.GetLeadingTrivia())
                    : point;
            }
        }
    }
}
