using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CIARE.GUI;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Dom = ICSharpCode.SharpDevelop.Dom;
using static global::CIARE.Utils.Completion.CompletionItems;
using static global::CIARE.Utils.Completion.CompletionParsing;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Navigation.FindUsages;
using static global::CIARE.Utils.Navigation.UsageDocumentCache;
using static global::CIARE.Utils.Navigation.UsageDocuments;

namespace CIARE.Utils.Navigation
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class Definitions
    {
        private readonly MainForm _mainForm;

        internal Definitions(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        /// <summary>
        /// Finds the first declaration of <paramref name="name"/> across all parsed compilation units.
        /// Returns (filePath, lineNumber) or (null, 0) if not found.
        /// </summary>
        internal (string FilePath, int Line) FindDefinition(string name, int offset)
        {
            var hit = FindDefinitionInActiveFile(name, offset, out string qualifier);
            if (hit.FilePath != null)
                return hit;

            hit = FindDefinitionInWorkspaceSemantic(name, offset);
            if (hit.FilePath != null)
                return hit;

            hit = FindDefinitionInWorkspaceMembers(name, qualifier);
            return hit.FilePath != null ? hit : FindDefinition(name);
        }

        internal (string FilePath, int Line) FindDefinition(string name)
        {
            if (string.IsNullOrEmpty(name))
                return (null, 0);

            lock (_mainForm.CompletionParsingFeature._completionDataLock)
            {
                // Top-level local functions and variables in the active file take priority.
                foreach (var item in _mainForm.CompletionWorkspaceFeature._topLevelLocalFunctions)
                {
                    if (string.Equals(item.Name, name, StringComparison.Ordinal)
                        && item.FilePath != null && item.Line > 0)
                        return (item.FilePath, item.Line);
                }

                var hit = FindDefinitionInUnit(parseInformation.MostRecentCompilationUnit, name);
                if (hit.FilePath != null) return hit;
                foreach (var unit in _mainForm.CompletionWorkspaceFeature._workspaceCompilationUnits.Values)
                {
                    hit = FindDefinitionInUnit(unit, name);
                    if (hit.FilePath != null) return hit;
                }
            }
            return (null, 0);
        }

        private (string FilePath, int Line) FindDefinitionInActiveFile(string name, int offset, out string qualifier)
        {
            qualifier = string.Empty;
            if (IsVisualBasic || string.IsNullOrEmpty(name))
                return (null, 0);

            var editor = SelectedEditor.GetSelectedEditor();
            string code = editor?.Text;
            if (string.IsNullOrWhiteSpace(code) || offset < 0 || offset >= code.Length)
                return (null, 0);

            try
            {
                string filePath = _mainForm.EditorFeature.GetActiveEditorFilePath();
                var tree = CSharpSyntaxTree.ParseText(code, path: filePath ?? string.Empty);
                var root = tree.GetCompilationUnitRoot();
                var token = root.FindToken(offset);
                if (!token.IsKind(SyntaxKind.IdentifierToken) || !string.Equals(token.ValueText, name, StringComparison.Ordinal))
                    return (null, 0);

                qualifier = GetMemberAccessQualifier(token);

                var compilation = CSharpCompilation.Create(
                    "__CiareGoToDefinition",
                    syntaxTrees: new[] { tree },
                    references: BuildUsageReferences(),
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: GlobalVariables.OUnsafeCode));
                var semanticModel = compilation.GetSemanticModel(tree, true);

                ISymbol symbol = GetDeclaredSymbolForIdentifier(semanticModel, token);
                var hit = GetDefinitionLocation(symbol, filePath);
                if (hit.FilePath != null)
                    return hit;

                SyntaxNode node = token.Parent;
                if (node is IdentifierNameSyntax || node is GenericNameSyntax)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(node);
                    symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                    hit = GetDefinitionLocation(symbol, filePath);
                    if (hit.FilePath != null)
                        return hit;
                }

                hit = FindLocalDefinitionBySyntax(root, token, name, filePath);
                if (hit.FilePath != null)
                    return hit;
            }
            catch
            {
                // Fall back to the existing workspace-wide DOM lookup below.
            }

            return (null, 0);
        }

        private (string FilePath, int Line) FindDefinitionInWorkspaceSemantic(string name, int offset)
        {
            if (IsVisualBasic || string.IsNullOrEmpty(name) || offset < 0)
                return (null, 0);

            try
            {
                var openTabs = _mainForm.UsageDocumentsFeature.CollectOpenTabInfo(name);
                var workspaceFolders = _mainForm.UsageDocumentsFeature.GetUsageWorkspaceFolders(_mainForm.EditorFeature.GetActiveEditorFilePath()).ToList();
                List<string> compilationUnitKeys;
                lock (_mainForm.CompletionParsingFeature._completionDataLock)
                    compilationUnitKeys = _mainForm.CompletionWorkspaceFeature._workspaceCompilationUnits.Keys.ToList();

                var documents = BuildUsageDocuments(name, openTabs, workspaceFolders, compilationUnitKeys);
                var activeDocument = documents.FirstOrDefault(document => document.IsActive);
                if (activeDocument == null)
                    return (null, 0);

                var compilation = CSharpCompilation.Create(
                    "__CiareGoToDefinitionWorkspace",
                    syntaxTrees: documents.Select(document => document.SyntaxTree),
                    references: BuildUsageReferences(),
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: GlobalVariables.OUnsafeCode));

                var semanticModel = compilation.GetSemanticModel(activeDocument.SyntaxTree, true);
                ISymbol symbol = GetIdentifierSymbolAtOffset(semanticModel, activeDocument.Root, offset, name);
                var hit = GetDefinitionLocation(symbol, activeDocument.FilePath);
                if (hit.FilePath != null)
                    return hit;

                SyntaxToken token = activeDocument.Root.FindToken(
                    Math.Max(0, Math.Min(offset, Math.Max(0, activeDocument.Root.FullSpan.End - 1))));
                return FindLocalDefinitionBySyntax(activeDocument.Root, token, name, activeDocument.FilePath);
            }
            catch
            {
                return (null, 0);
            }
        }

        private static string GetMemberAccessQualifier(SyntaxToken token)
        {
            if (token.Parent is SimpleNameSyntax simpleName &&
                simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName)
                return NormalizeCompletionExpression(memberAccess.Expression.ToString());

            return string.Empty;
        }

        private (string FilePath, int Line) FindDefinitionInWorkspaceMembers(string name, string qualifier)
        {
            if (string.IsNullOrEmpty(name))
                return (null, 0);

            lock (_mainForm.CompletionParsingFeature._completionDataLock)
            {
                return FindDefinitionInWorkspaceMembersCore(name, qualifier);
            }
        }

        private (string FilePath, int Line) FindDefinitionInWorkspaceMembersCore(string name, string qualifier)
        {
            string normalizedQualifier = NormalizeCompletionExpression(qualifier);
            if (!string.IsNullOrEmpty(normalizedQualifier))
            {
                foreach (var completionClass in _mainForm.CompletionWorkspaceFeature._workspaceCompletionClasses)
                {
                    if (!MatchesWorkspaceClass(completionClass, normalizedQualifier))
                        continue;

                    var hit = FindDefinitionInWorkspaceItems(completionClass.StaticMembers, name);
                    if (hit.FilePath != null)
                        return hit;
                }
            }

            foreach (var completionClass in _mainForm.CompletionWorkspaceFeature._workspaceCompletionClasses)
            {
                var hit = FindDefinitionInWorkspaceItems(completionClass.StaticMembers, name);
                if (hit.FilePath != null)
                    return hit;
            }

            return (null, 0);
        }

        private static (string FilePath, int Line) FindDefinitionInWorkspaceItems(IEnumerable<WorkspaceCompletionItem> items, string name)
        {
            foreach (var item in items)
            {
                if (item != null &&
                    string.Equals(item.Name, name, StringComparison.Ordinal) &&
                    item.FilePath != null && item.Line > 0)
                    return (item.FilePath, item.Line);
            }

            return (null, 0);
        }

        private static (string FilePath, int Line) FindLocalDefinitionBySyntax(
            CompilationUnitSyntax root, SyntaxToken targetToken, string name, string filePath)
        {
            if (root == null ||
                !targetToken.IsKind(SyntaxKind.IdentifierToken) ||
                string.IsNullOrEmpty(name))
            {
                return (null, 0);
            }

            if (!TryFindLocalDefinitionToken(root, targetToken, name, out SyntaxToken definitionToken))
                return (null, 0);

            return GetDefinitionLocation(definitionToken.GetLocation(), filePath);
        }

        private static bool TryFindLocalDefinitionToken(
            CompilationUnitSyntax root, SyntaxToken targetToken, string name, out SyntaxToken definitionToken)
        {
            definitionToken = default;
            int bestStart = -1;

            foreach (var variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (IsFieldVariableDeclarator(variable))
                    continue;

                TryUseDefinitionToken(variable, variable.Identifier, targetToken, name,
                    ref definitionToken, ref bestStart);
            }

            foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
                TryUseDefinitionToken(parameter, parameter.Identifier, targetToken, name,
                    ref definitionToken, ref bestStart);

            foreach (var forEach in root.DescendantNodes().OfType<ForEachStatementSyntax>())
                TryUseDefinitionToken(forEach, forEach.Identifier, targetToken, name,
                    ref definitionToken, ref bestStart);

            foreach (var catchDeclaration in root.DescendantNodes().OfType<CatchDeclarationSyntax>())
                TryUseDefinitionToken(catchDeclaration, catchDeclaration.Identifier, targetToken, name,
                    ref definitionToken, ref bestStart);

            foreach (var designation in root.DescendantNodes().OfType<SingleVariableDesignationSyntax>())
                TryUseDefinitionToken(designation, designation.Identifier, targetToken, name,
                    ref definitionToken, ref bestStart);

            return bestStart >= 0;
        }

        private static void TryUseDefinitionToken(SyntaxNode declarationNode, SyntaxToken candidateToken,
            SyntaxToken targetToken, string name, ref SyntaxToken definitionToken, ref int bestStart)
        {
            if (!candidateToken.IsKind(SyntaxKind.IdentifierToken) ||
                !string.Equals(candidateToken.ValueText, name, StringComparison.Ordinal) ||
                candidateToken.SpanStart > targetToken.SpanStart ||
                candidateToken.SpanStart < bestStart ||
                !IsDeclarationVisibleAtToken(declarationNode, targetToken))
            {
                return;
            }

            definitionToken = candidateToken;
            bestStart = candidateToken.SpanStart;
        }

        private static bool IsFieldVariableDeclarator(VariableDeclaratorSyntax variable)
        {
            return variable?.Ancestors().Any(node =>
                node is FieldDeclarationSyntax || node is EventFieldDeclarationSyntax) == true;
        }

        private static bool IsDeclarationVisibleAtToken(SyntaxNode declarationNode, SyntaxToken targetToken)
        {
            if (declarationNode == null || targetToken.Parent == null ||
                declarationNode.SpanStart > targetToken.SpanStart)
            {
                return false;
            }

            SyntaxNode scope = GetDeclarationScopeNode(declarationNode);
            return scope == null || IsAncestorOrSelf(scope, targetToken.Parent);
        }

        private static SyntaxNode GetDeclarationScopeNode(SyntaxNode declarationNode)
        {
            if (declarationNode == null)
                return null;

            if (declarationNode is ParameterSyntax || declarationNode is TypeParameterSyntax)
            {
                return declarationNode.Ancestors().FirstOrDefault(node =>
                    node is BaseMethodDeclarationSyntax ||
                    node is LocalFunctionStatementSyntax ||
                    node is ParenthesizedLambdaExpressionSyntax ||
                    node is SimpleLambdaExpressionSyntax ||
                    node is AnonymousMethodExpressionSyntax ||
                    node is TypeDeclarationSyntax);
            }

            if (declarationNode is ForEachStatementSyntax)
                return declarationNode;

            if (declarationNode is CatchDeclarationSyntax)
                return declarationNode.Parent;

            SyntaxNode localDeclaration = declarationNode.AncestorsAndSelf()
                .FirstOrDefault(node => node is LocalDeclarationStatementSyntax);
            if (localDeclaration != null)
            {
                if (localDeclaration.Parent is GlobalStatementSyntax globalStatement)
                    return globalStatement.Parent;

                return localDeclaration.Parent;
            }

            return declarationNode.AncestorsAndSelf().FirstOrDefault(node =>
                node is ForStatementSyntax ||
                node is UsingStatementSyntax ||
                node is FixedStatementSyntax ||
                node is BlockSyntax ||
                node is SwitchSectionSyntax ||
                node is GlobalStatementSyntax ||
                node is StatementSyntax ||
                node is BaseMethodDeclarationSyntax ||
                node is LocalFunctionStatementSyntax ||
                node is AnonymousFunctionExpressionSyntax);
        }

        private static bool IsAncestorOrSelf(SyntaxNode ancestor, SyntaxNode node)
        {
            for (SyntaxNode current = node; current != null; current = current.Parent)
            {
                if (current == ancestor)
                    return true;
            }

            return false;
        }

        internal static ISymbol GetDeclaredSymbolForIdentifier(SemanticModel semanticModel, SyntaxToken token)
        {
            SyntaxNode node = token.Parent;
            while (node != null)
            {
                switch (node)
                {
                    case VariableDeclaratorSyntax variable when variable.Identifier == token:
                    case SingleVariableDesignationSyntax designation when designation.Identifier == token:
                    case ParameterSyntax parameter when parameter.Identifier == token:
                    case LocalFunctionStatementSyntax localFunction when localFunction.Identifier == token:
                    case BaseTypeDeclarationSyntax typeDeclaration when typeDeclaration.Identifier == token:
                    case MethodDeclarationSyntax method when method.Identifier == token:
                    case PropertyDeclarationSyntax property when property.Identifier == token:
                    case EventDeclarationSyntax eventDeclaration when eventDeclaration.Identifier == token:
                    case EnumMemberDeclarationSyntax enumMember when enumMember.Identifier == token:
                    case ForEachStatementSyntax forEach when forEach.Identifier == token:
                    case CatchDeclarationSyntax catchDeclaration when catchDeclaration.Identifier == token:
                        return semanticModel.GetDeclaredSymbol(node);
                }

                node = node.Parent;
            }

            return null;
        }

        private static (string FilePath, int Line) GetDefinitionLocation(ISymbol symbol, string fallbackFilePath)
        {
            if (symbol == null)
                return (null, 0);

            ISymbol definition = symbol.OriginalDefinition ?? symbol;
            foreach (var location in definition.Locations)
            {
                var hit = GetDefinitionLocation(location, fallbackFilePath);
                if (hit.FilePath != null)
                    return hit;
            }

            foreach (var syntaxRef in definition.DeclaringSyntaxReferences)
            {
                var hit = GetDefinitionLocation(syntaxRef.GetSyntax().GetLocation(), fallbackFilePath);
                if (hit.FilePath != null)
                    return hit;
            }

            return (null, 0);
        }

        private static (string FilePath, int Line) GetDefinitionLocation(Location location, string fallbackFilePath)
        {
            if (location == null || !location.IsInSource)
                return (null, 0);

            var lineSpan = location.GetLineSpan();
            string filePath = string.IsNullOrEmpty(lineSpan.Path) ? fallbackFilePath : lineSpan.Path;
            if (string.IsNullOrEmpty(filePath))
                return (null, 0);

            return (filePath, lineSpan.StartLinePosition.Line + 1);
        }

        private static (string FilePath, int Line) FindDefinitionInUnit(Dom.ICompilationUnit unit, string name)
        {
            if (unit?.Classes == null) return (null, 0);
            foreach (var @class in unit.Classes)
            {
                var hit = FindDefinitionInClass(@class, name);
                if (hit.FilePath != null) return hit;
            }
            return (null, 0);
        }

        private static (string FilePath, int Line) FindDefinitionInClass(Dom.IClass @class, string name)
        {
            if (@class == null) return (null, 0);
            if (string.Equals(@class.Name, name, StringComparison.Ordinal)
                && @class.CompilationUnit?.FileName != null && @class.Region.BeginLine > 0)
                return (@class.CompilationUnit.FileName, @class.Region.BeginLine);

            foreach (var m in @class.Methods)
            {
                if (m != null && !m.IsConstructor
                    && string.Equals(m.Name, name, StringComparison.Ordinal)
                    && m.DeclaringType?.CompilationUnit?.FileName != null && m.Region.BeginLine > 0)
                    return (m.DeclaringType.CompilationUnit.FileName, m.Region.BeginLine);
            }
            foreach (var p in @class.Properties)
            {
                if (p != null && string.Equals(p.Name, name, StringComparison.Ordinal)
                    && p.DeclaringType?.CompilationUnit?.FileName != null && p.Region.BeginLine > 0)
                    return (p.DeclaringType.CompilationUnit.FileName, p.Region.BeginLine);
            }
            foreach (var f in @class.Fields)
            {
                if (f != null && string.Equals(f.Name, name, StringComparison.Ordinal)
                    && f.DeclaringType?.CompilationUnit?.FileName != null && f.Region.BeginLine > 0)
                    return (f.DeclaringType.CompilationUnit.FileName, f.Region.BeginLine);
            }
            foreach (var inner in @class.InnerClasses)
            {
                var hit = FindDefinitionInClass(inner, name);
                if (hit.FilePath != null) return hit;
            }
            return (null, 0);
        }

        /// <summary>
        /// Navigates the editor to the given file and line number. Opens the file in a new
        /// tab when it is not the currently active file.
        /// </summary>
        internal void NavigateToDefinition(string filePath, int lineNumber)
        {
            if (string.IsNullOrEmpty(filePath) || lineNumber <= 0 || !File.Exists(filePath))
                return;
            string normalizedPath = NormalizeCompletionPath(filePath);
            string currentFilePath = NormalizeCompletionPath(_mainForm.EditorFeature.GetActiveEditorFilePath());
            if (!string.Equals(normalizedPath, currentFilePath, StringComparison.OrdinalIgnoreCase))
                _mainForm.ExplorerTreeFeature.OpenFileFromExplorer(filePath);
            var editor = SelectedEditor.GetSelectedEditor();
            if (editor != null)
                editor.ActiveTextAreaControl.JumpTo(lineNumber - 1);
        }

        internal void NavigateToUsageLocation(string filePath, int lineNumber, int columnNumber)
        {
            if (lineNumber <= 0)
                return;

            if (!string.Equals(filePath, CurrentFileUsageDisplayName, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                string normalizedPath = NormalizeCompletionPath(filePath);
                string currentFilePath = NormalizeCompletionPath(_mainForm.EditorFeature.GetActiveEditorFilePath());
                if (!string.Equals(normalizedPath, currentFilePath, StringComparison.OrdinalIgnoreCase))
                    _mainForm.ExplorerTreeFeature.OpenFileFromExplorer(filePath);
            }

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null || editor.Document.TotalNumberOfLines == 0)
                return;

            int lineIndex = Math.Max(0, Math.Min(lineNumber - 1, editor.Document.TotalNumberOfLines - 1));
            var lineSegment = editor.Document.GetLineSegment(lineIndex);
            int columnIndex = Math.Max(0, Math.Min(columnNumber - 1, lineSegment.Length));
            editor.ActiveTextAreaControl.JumpTo(lineIndex, columnIndex);
            editor.Focus();
        }
    }
}
