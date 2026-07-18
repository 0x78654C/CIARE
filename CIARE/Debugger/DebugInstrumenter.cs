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
    internal static global::System.Func<int, bool> ShouldPauseHandler;
    internal static global::System.Action<int, string[], string[], object[]> PauseHandler;

    internal static bool ShouldPause(int sequencePoint)
    {{
        global::System.Func<int, bool> handler = ShouldPauseHandler;
        return handler != null && handler(sequencePoint);
    }}

    internal static void Pause(int sequencePoint, string[] names, string[] types, object[] values)
    {{
        global::System.Action<int, string[], string[], object[]> handler = PauseHandler;
        if (handler != null)
            handler(sequencePoint, names, types, values);
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
            return Instrument(tree, null, firstSequencePointId);
        }

        internal static DebugInstrumentationResult Instrument(
            SyntaxTree tree,
            SemanticModel semanticModel,
            int firstSequencePointId = 1)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var rewriter = new SequencePointRewriter(
                tree.FilePath, firstSequencePointId, semanticModel);
            CompilationUnitSyntax rewritten = (CompilationUnitSyntax)rewriter.Visit(root);
            return new DebugInstrumentationResult(rewritten.ToFullString(), rewriter.SequencePoints);
        }

        private sealed class SequencePointRewriter : CSharpSyntaxRewriter
        {
            private readonly string _filePath;
            private readonly List<DebugSequencePoint> _sequencePoints = new List<DebugSequencePoint>();
            private readonly SemanticModel _semanticModel;
            private int _nextSequencePointId;

            public SequencePointRewriter(string filePath, int firstSequencePointId,
                SemanticModel semanticModel)
            {
                _filePath = filePath ?? string.Empty;
                _nextSequencePointId = firstSequencePointId;
                _semanticModel = semanticModel;
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

                IReadOnlyList<VariableCapture> variables = GetAssignedVariables(source);
                string names = CreateStringArray(variables.Select(variable => variable.Name));
                string types = CreateStringArray(variables.Select(variable => variable.TypeName));
                string values = variables.Count == 0
                    ? "global::System.Array.Empty<object>()"
                    : "new object[] { " + string.Join(", ", variables.Select(variable =>
                        $"(object)({variable.Expression})")) + " }";
                StatementSyntax point = SyntaxFactory.ParseStatement(
                    $"if (global::{BridgeTypeName}.ShouldPause({id})) " +
                    $"global::{BridgeTypeName}.Pause({id}, {names}, {types}, {values});");
                return moveLeadingTrivia
                    ? point.WithLeadingTrivia(source.GetLeadingTrivia())
                    : point;
            }

            private IReadOnlyList<VariableCapture> GetAssignedVariables(StatementSyntax source)
            {
                if (_semanticModel == null || source.SyntaxTree != _semanticModel.SyntaxTree)
                    return Array.Empty<VariableCapture>();

                try
                {
                    DataFlowAnalysis flow = _semanticModel.AnalyzeDataFlow(source);
                    if (flow == null || !flow.Succeeded)
                        return Array.Empty<VariableCapture>();

                    var assigned = new HashSet<ISymbol>(
                        flow.DefinitelyAssignedOnEntry, SymbolEqualityComparer.Default);
                    var variables = _semanticModel.LookupSymbols(source.SpanStart)
                        .Where(symbol => symbol is ILocalSymbol || symbol is IParameterSymbol)
                        .Where(symbol => IsReadableAndAssigned(symbol, assigned))
                        .Select(CreateVariableCapture)
                        .Where(variable => variable != null)
                        .GroupBy(variable => variable.Expression, StringComparer.Ordinal)
                        .Select(group => group.First())
                        .OrderBy(variable => variable.SortOrder)
                        .ThenBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    IMethodSymbol method = _semanticModel.GetEnclosingSymbol(source.SpanStart)
                        as IMethodSymbol;
                    INamedTypeSymbol containingType = method?.ContainingType;
                    if (method != null && !method.IsStatic && containingType != null &&
                        !containingType.IsRefLikeType &&
                        (!containingType.IsValueType ||
                         method.MethodKind != MethodKind.Constructor))
                    {
                        variables.Add(new VariableCapture(
                            "this",
                            containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            "this",
                            2));
                    }

                    if (containingType != null && !containingType.IsImplicitlyDeclared &&
                        containingType.TypeKind != TypeKind.Error)
                    {
                        string sourceTypeName = containingType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat);
                        variables.Add(new VariableCapture(
                            $"Globals ({containingType.Name})",
                            "static " + containingType.ToDisplayString(
                                SymbolDisplayFormat.MinimallyQualifiedFormat),
                            $"typeof({sourceTypeName})",
                            3));
                    }

                    return variables;
                }
                catch (ArgumentException)
                {
                    return Array.Empty<VariableCapture>();
                }
                catch (InvalidOperationException)
                {
                    return Array.Empty<VariableCapture>();
                }
            }

            private static bool IsReadableAndAssigned(ISymbol symbol,
                HashSet<ISymbol> assigned)
            {
                ITypeSymbol type;
                if (symbol is ILocalSymbol local)
                {
                    type = local.Type;
                    if (!assigned.Contains(local))
                        return false;
                }
                else if (symbol is IParameterSymbol parameter)
                {
                    type = parameter.Type;
                    if (parameter.RefKind == RefKind.Out && !assigned.Contains(parameter))
                        return false;
                }
                else
                {
                    return false;
                }

                return type != null &&
                    type.TypeKind != TypeKind.Error &&
                    type.TypeKind != TypeKind.Pointer &&
                    type.TypeKind != TypeKind.FunctionPointer &&
                    !type.IsRefLikeType &&
                    type.SpecialType != SpecialType.System_TypedReference;
            }

            private static VariableCapture CreateVariableCapture(ISymbol symbol)
            {
                ITypeSymbol type = symbol is ILocalSymbol local
                    ? local.Type
                    : (symbol as IParameterSymbol)?.Type;
                if (type == null)
                    return null;

                string name = symbol.Name;
                string expression = EscapeIdentifier(symbol.Name);
                int sortOrder = symbol is IParameterSymbol ? 0 : 1;
                string typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                return new VariableCapture(name, typeName, expression, sortOrder);
            }

            private static string EscapeIdentifier(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;
                return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ||
                    SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
                    ? "@" + name
                    : name;
            }

            private static string CreateStringArray(IEnumerable<string> values)
            {
                string[] items = values
                    .Select(value => SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(value ?? string.Empty)).ToFullString())
                    .ToArray();
                return items.Length == 0
                    ? "global::System.Array.Empty<string>()"
                    : "new string[] { " + string.Join(", ", items) + " }";
            }

            private sealed class VariableCapture
            {
                public VariableCapture(string name, string typeName, string expression,
                    int sortOrder)
                {
                    Name = name;
                    TypeName = typeName;
                    Expression = expression;
                    SortOrder = sortOrder;
                }

                public string Name { get; }
                public string TypeName { get; }
                public string Expression { get; }
                public int SortOrder { get; }
            }
        }
    }
}
