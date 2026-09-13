using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static global::CIARE.Utils.Completion.CompletionParsing;
using static global::CIARE.Utils.Completion.CompletionProject;
using static global::CIARE.Utils.Completion.CompletionSyntax;
using static global::CIARE.Utils.Completion.CompletionWorkspace;

namespace CIARE.Utils.Completion
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class RoslynCompletion
    {
        private readonly MainForm _mainForm;

        internal RoslynCompletion(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText)
        {
            return GetRoslynMemberCompletionData(code, caretOffset, expressionText, _mainForm.EditorFeature.GetActiveEditorFilePath());
        }

        internal ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText,
            string currentFilePath)
        {
            return GetRoslynMemberCompletionData(code, caretOffset, expressionText, currentFilePath,
                CancellationToken.None);
        }

        internal ArrayList GetRoslynMemberCompletionData(string code, int caretOffset, string expressionText,
            string currentFilePath, CancellationToken cancellationToken)
        {
            var result = new ArrayList();
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return result;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var context = BuildRoslynCompletionContext(code, currentFilePath, cancellationToken);
                if (context == null)
                    return result;

                int position = Math.Max(0, Math.Min(caretOffset, code.Length));
                string completionExpression = NormalizeCompletionExpression(expressionText);
                if (string.IsNullOrEmpty(completionExpression))
                    completionExpression = GetMemberCompletionExpressionText(code, position);

                ExpressionSyntax expression = FindRoslynMemberAccessExpression(context.Root, position, completionExpression);
                if (expression == null)
                    return result;

                var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);
                var typeInfo = context.SemanticModel.GetTypeInfo(expression);
                ISymbol expressionSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                if (expressionSymbol is INamespaceSymbol namespaceSymbol)
                {
                    AddRoslynSymbolsToCompletionData(result, namespaceSymbol.GetMembers(), string.Empty,
                        staticOnly: false, instanceOnly: false);
                    return result;
                }

                bool staticOnly = expressionSymbol is INamedTypeSymbol;
                ITypeSymbol typeSymbol = staticOnly
                    ? expressionSymbol as ITypeSymbol
                    : typeInfo.Type ?? typeInfo.ConvertedType;
                if (typeSymbol == null)
                    return result;

                AddRoslynSymbolsToCompletionData(result,
                    context.SemanticModel.LookupSymbols(
                        position,
                        typeSymbol,
                        includeReducedExtensionMethods: !staticOnly),
                    string.Empty,
                    staticOnly,
                    instanceOnly: !staticOnly);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }

            return result;
        }

        private static string GetMemberCompletionExpressionText(string code, int caretOffset)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            int offset = Math.Max(0, Math.Min(caretOffset, code.Length));
            while (offset > 0 && char.IsWhiteSpace(code[offset - 1]))
                offset--;

            if (offset > 0 && code[offset - 1] == '.')
                offset--;

            while (offset > 0 && char.IsWhiteSpace(code[offset - 1]))
                offset--;

            int end = offset;
            while (offset > 0 && IsSimpleMemberExpressionChar(code[offset - 1]))
                offset--;

            return end > offset
                ? NormalizeCompletionExpression(code.Substring(offset, end - offset))
                : string.Empty;
        }

        private static bool IsSimpleMemberExpressionChar(char ch)
        {
            return char.IsLetterOrDigit(ch) ||
                   ch == '_' ||
                   ch == '@' ||
                   ch == '.';
        }

        internal ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix)
        {
            return GetRoslynCtrlSpaceCompletionData(code, caretOffset, prefix, _mainForm.EditorFeature.GetActiveEditorFilePath());
        }

        internal ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix,
            string currentFilePath)
        {
            return GetRoslynCtrlSpaceCompletionData(code, caretOffset, prefix, currentFilePath,
                CancellationToken.None);
        }

        internal ArrayList GetRoslynCtrlSpaceCompletionData(string code, int caretOffset, string prefix,
            string currentFilePath, CancellationToken cancellationToken)
        {
            var result = new ArrayList();
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return result;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var context = BuildRoslynCompletionContext(code, currentFilePath, cancellationToken);
                if (context == null)
                    return result;

                int position = Math.Max(0, Math.Min(caretOffset, code.Length));
                AddRoslynSymbolsToCompletionData(result,
                    context.SemanticModel.LookupSymbols(position),
                    prefix,
                    staticOnly: false,
                    instanceOnly: false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }

            return result;
        }

        private RoslynCompletionContext BuildRoslynCompletionContext(string code)
        {
            return BuildRoslynCompletionContext(code, _mainForm.EditorFeature.GetActiveEditorFilePath(), CancellationToken.None);
        }

        private RoslynCompletionContext BuildRoslynCompletionContext(string code, string currentFilePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string projectPath = _mainForm.CompletionSyntaxFeature.GetCompletionProjectPath(currentFilePath);
            var parseOptions = BuildCompletionParseOptions();
            string activePath = string.IsNullOrWhiteSpace(currentFilePath) ? DummyFileName : currentFilePath;
            SyntaxTree activeTree = CSharpSyntaxTree.ParseText(code ?? string.Empty, parseOptions,
                path: activePath, cancellationToken: cancellationToken);
            RoslynCompletionProjectSnapshot projectSnapshot = _mainForm.CompletionProjectFeature.GetRoslynCompletionProjectSnapshot(
                currentFilePath, projectPath, parseOptions, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = projectSnapshot.WithActiveTree(activeTree);

            var root = activeTree.GetCompilationUnitRoot();
            return new RoslynCompletionContext(
                activeTree,
                root,
                compilation,
                compilation.GetSemanticModel(activeTree, true));
        }

        private static ExpressionSyntax FindRoslynMemberAccessExpression(
            CompilationUnitSyntax root, int caretOffset, string expressionText)
        {
            if (root == null)
                return null;

            int lookupPosition = Math.Max(0, Math.Min(caretOffset - 1, root.FullSpan.End));
            SyntaxToken token = root.FindToken(lookupPosition);
            string normalizedExpression = NormalizeCompletionExpression(expressionText);
            // Requests capture the receiver before the new dot is inserted. In
            // "System.Console" the existing member access is the receiver itself,
            // so returning its left side would suggest members of System.
            var receiver = token.Parent?.AncestorsAndSelf().OfType<ExpressionSyntax>()
                .FirstOrDefault(expression => expression.Span.End <= caretOffset &&
                    string.Equals(NormalizeCompletionExpression(expression.ToString()),
                        normalizedExpression, StringComparison.Ordinal));
            if (receiver != null)
                return receiver;

            var memberAccess = token.Parent?.AncestorsAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(access => access.OperatorToken.SpanStart <= lookupPosition &&
                                 access.SpanStart <= lookupPosition)
                .OrderByDescending(access => access.SpanStart)
                .FirstOrDefault();
            if (memberAccess != null)
                return memberAccess.Expression;

            if (string.IsNullOrEmpty(normalizedExpression))
                return null;

            return root.DescendantNodes()
                .OfType<ExpressionSyntax>()
                .Where(expression => expression.Span.End <= caretOffset &&
                                     string.Equals(NormalizeCompletionExpression(expression.ToString()),
                                         normalizedExpression, StringComparison.Ordinal))
                .OrderByDescending(expression => expression.Span.End)
                .FirstOrDefault();
        }

        private static void AddRoslynSymbolsToCompletionData(ArrayList result, IEnumerable<ISymbol> symbols,
            string prefix, bool staticOnly, bool instanceOnly)
        {
            if (result == null || symbols == null)
                return;

            string normalizedPrefix = prefix ?? string.Empty;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in symbols.OrderBy(symbol => symbol.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsRoslynCompletionSymbol(symbol, staticOnly, instanceOnly))
                    continue;

                string name = GetRoslynCompletionName(symbol);
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!string.IsNullOrEmpty(normalizedPrefix) &&
                    !name.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(name))
                    continue;

                result.Add(new DefaultCompletionData(name,
                    GetRoslynCompletionDescription(symbol),
                    GetRoslynCompletionImageIndex(symbol)));
                if (result.Count >= WorkspaceCompletionMethodLimit)
                    return;
            }
        }

        private static bool IsRoslynCompletionSymbol(ISymbol symbol, bool staticOnly, bool instanceOnly)
        {
            if (symbol == null || symbol.IsImplicitlyDeclared || string.IsNullOrEmpty(symbol.Name))
                return false;

            if (symbol is IMethodSymbol method)
            {
                if (method.MethodKind != MethodKind.Ordinary &&
                    method.MethodKind != MethodKind.ReducedExtension)
                {
                    return false;
                }
            }

            if (staticOnly && symbol.Kind != SymbolKind.NamedType && !symbol.IsStatic)
                return false;

            if (instanceOnly && symbol.IsStatic && symbol.Kind != SymbolKind.NamedType)
                return false;

            return symbol.Kind == SymbolKind.Method ||
                   symbol.Kind == SymbolKind.Property ||
                   symbol.Kind == SymbolKind.Field ||
                   symbol.Kind == SymbolKind.Event ||
                   symbol.Kind == SymbolKind.NamedType ||
                   symbol.Kind == SymbolKind.Namespace ||
                   symbol.Kind == SymbolKind.Local ||
                   symbol.Kind == SymbolKind.Parameter;
        }

        private static string GetRoslynCompletionName(ISymbol symbol)
        {
            if (symbol == null)
                return string.Empty;

            if (symbol is INamedTypeSymbol namedType && !string.IsNullOrEmpty(namedType.Name))
                return namedType.Name;

            return symbol.Name ?? string.Empty;
        }

        private static string GetRoslynCompletionDescription(ISymbol symbol)
        {
            try
            {
                return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            catch
            {
                return symbol?.Name ?? string.Empty;
            }
        }

        private static int GetRoslynCompletionImageIndex(ISymbol symbol)
        {
            switch (symbol?.Kind)
            {
                case SymbolKind.Method:
                    return 1;
                case SymbolKind.Property:
                    return 2;
                case SymbolKind.Field:
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                    return 3;
                case SymbolKind.Event:
                    return 6;
                case SymbolKind.NamedType:
                    return symbol is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Enum ? 4 : 0;
                case SymbolKind.Namespace:
                    return 5;
                default:
                    return 0;
            }
        }

        private sealed class RoslynCompletionContext
        {
            public RoslynCompletionContext(SyntaxTree activeTree, CompilationUnitSyntax root,
                CSharpCompilation compilation, SemanticModel semanticModel)
            {
                ActiveTree = activeTree;
                Root = root;
                Compilation = compilation;
                SemanticModel = semanticModel;
            }

            public SyntaxTree ActiveTree { get; }
            public CompilationUnitSyntax Root { get; }
            public CSharpCompilation Compilation { get; }
            public SemanticModel SemanticModel { get; }
        }
    }
}
