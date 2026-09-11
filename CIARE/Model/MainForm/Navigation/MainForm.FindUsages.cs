using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE
{
    public partial class MainForm
    {
        private static readonly object _usageReferencesLock = new object();
        private static List<MetadataReference> _usagePlatformReferences;
        private static List<MetadataReference> _usageCustomReferences = new List<MetadataReference>();
        private static string _usageCustomReferenceKey = string.Empty;

        private async void FindUsagesAtCaret()
        {
            if (IsVisualBasic)
            {
                ShowFindUsagesMessage("Find Usages is available for C# files.");
                return;
            }

            if (!TryGetIdentifierAtCaret(out string identifier, out int identifierOffset))
            {
                ShowFindUsagesMessage("Put the caret on an identifier to find usages.");
                return;
            }

            var previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                // Capture all UI-thread state before going async.
                string activeFilePath = GetActiveEditorFilePath();
                var projectPaths = GetFileExplorerUsageProjectPaths();
                var activeTab = CollectActiveUsageTabInfo();
                bool activeFileIsInProject = projectPaths.Any(
                    projectPath => ProjectContainsSourceFile(projectPath, activeFilePath));
                var projectFolders = activeFileIsInProject
                    ? GetCompletionSourceFolders(projectPaths)
                    : new List<string>();

                List<UsageLocation> usages = await Task.Run(() =>
                {
                    var documents = BuildUsageDocuments(identifier, activeTab, projectFolders, new List<string>());
                    if (documents.Count == 0)
                        return null;

                    var semanticUsages = FindSemanticUsages(identifier, identifierOffset, documents, out bool targetResolved);
                    return targetResolved ? semanticUsages : FindSyntaxUsages(identifier, documents);
                });

                if (usages == null)
                    ShowFindUsagesMessage("No C# files found to search.");
                else
                    ShowFindUsagesResults(identifier, usages);
            }
            catch (Exception ex)
            {
                ShowFindUsagesMessage("Find Usages failed: " + ex.Message);
            }
            finally
            {
                Cursor.Current = previousCursor;
            }
        }

        private bool TryGetIdentifierAtCaret(out string identifier, out int offset)
        {
            identifier = string.Empty;
            offset = -1;

            var editor = SelectedEditor.GetSelectedEditor();
            var textArea = editor?.ActiveTextAreaControl?.TextArea;
            var document = textArea?.Document;
            if (document == null || document.TextLength == 0)
                return false;

            if (textArea.SelectionManager.HasSomethingSelected &&
                textArea.SelectionManager.SelectionCollection.Count == 1)
            {
                string selectedText = textArea.SelectionManager.SelectedText?.Trim();
                if (IsValidIdentifierText(selectedText))
                {
                    identifier = NormalizeIdentifierText(selectedText);
                    offset = textArea.SelectionManager.SelectionCollection[0].Offset;
                    return true;
                }
            }

            int caretOffset = Math.Max(0, Math.Min(textArea.Caret.Offset, document.TextLength));
            int lookupOffset = caretOffset == document.TextLength && caretOffset > 0 ? caretOffset - 1 : caretOffset;
            int wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(document, lookupOffset);
            int wordEnd = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(document, lookupOffset);

            if (wordEnd <= wordStart && caretOffset > 0)
            {
                lookupOffset = caretOffset - 1;
                wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(document, lookupOffset);
                wordEnd = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(document, lookupOffset);
            }

            if (wordEnd <= wordStart)
                return false;

            string word = document.GetText(wordStart, wordEnd - wordStart);
            if (!IsValidIdentifierText(word))
                return false;

            identifier = NormalizeIdentifierText(word);
            offset = wordStart;
            return true;
        }

        private List<UsageLocation> FindSemanticUsages(string identifier, int identifierOffset,
            List<UsageDocument> documents, out bool targetResolved)
        {
            targetResolved = false;
            try
            {
                var activeDocument = documents.FirstOrDefault(document => document.IsActive);
                if (activeDocument == null)
                    return null;

                var compilation = CSharpCompilation.Create(
                    "__CiareFindUsages",
                    syntaxTrees: documents.Select(document => document.SyntaxTree),
                    references: BuildUsageReferences(),
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: GlobalVariables.OUnsafeCode));

                var activeModel = compilation.GetSemanticModel(activeDocument.SyntaxTree, true);
                ISymbol targetSymbol = GetIdentifierSymbolAtOffset(activeModel, activeDocument.Root, identifierOffset, identifier);
                if (targetSymbol == null)
                    return null;

                targetResolved = true;
                targetSymbol = NormalizeUsageSymbol(targetSymbol);

                // Search each document's tokens in parallel (Roslyn compilation/model is thread-safe for reads).
                var rawUsages = new ConcurrentBag<UsageLocation>();
                Parallel.ForEach(documents, CreateUsageParallelOptions(), document =>
                {
                    var semanticModel = compilation.GetSemanticModel(document.SyntaxTree, true);
                    foreach (var token in GetIdentifierTokens(document.Root, identifier))
                    {
                        if (IsDeclarationIdentifier(token))
                            continue;

                        ISymbol symbol = GetReferenceSymbolForIdentifier(semanticModel, token);
                        if (SymbolsMatch(targetSymbol, symbol))
                        {
                            rawUsages.Add(CreateUsageLocation(document, token));
                            continue;
                        }

                        if (symbol == null && IsPotentialUnresolvedUsage(token, targetSymbol))
                            rawUsages.Add(CreateUsageLocation(document, token));
                    }
                });

                // Deduplicate and sort on a single thread.
                var usages = new List<UsageLocation>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var usage in rawUsages)
                    AddUsageLocation(usages, seen, usage);

                return SortUsageLocations(usages);
            }
            catch
            {
                targetResolved = false;
                return null;
            }
        }

        private static List<UsageLocation> FindSyntaxUsages(string identifier, List<UsageDocument> documents)
        {
            var rawUsages = new ConcurrentBag<UsageLocation>();
            Parallel.ForEach(documents, CreateUsageParallelOptions(), document =>
            {
                foreach (var token in GetIdentifierTokens(document.Root, identifier))
                {
                    if (!IsDeclarationIdentifier(token))
                        rawUsages.Add(CreateUsageLocation(document, token));
                }
            });

            return SortUsageLocations(rawUsages.ToList());
        }

        private static IEnumerable<SyntaxToken> GetIdentifierTokens(CompilationUnitSyntax root, string identifier)
        {
            return root.DescendantTokens()
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken) &&
                    string.Equals(token.ValueText, identifier, StringComparison.Ordinal));
        }

        private static ISymbol GetIdentifierSymbolAtOffset(SemanticModel semanticModel,
            CompilationUnitSyntax root, int offset, string identifier)
        {
            if (semanticModel == null || root == null || offset < 0)
                return null;

            int safeOffset = Math.Max(0, Math.Min(offset, Math.Max(0, root.FullSpan.End - 1)));
            SyntaxToken token = root.FindToken(safeOffset);
            if (!token.IsKind(SyntaxKind.IdentifierToken) ||
                !string.Equals(token.ValueText, identifier, StringComparison.Ordinal))
                return null;

            return GetDeclaredSymbolForIdentifier(semanticModel, token) ??
                GetReferenceSymbolForIdentifier(semanticModel, token);
        }

        private static ISymbol GetReferenceSymbolForIdentifier(SemanticModel semanticModel, SyntaxToken token)
        {
            if (semanticModel == null || !(token.Parent is SimpleNameSyntax simpleName))
                return null;

            var symbolInfo = semanticModel.GetSymbolInfo(simpleName);
            return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        }

        private static bool SymbolsMatch(ISymbol targetSymbol, ISymbol candidateSymbol)
        {
            if (targetSymbol == null || candidateSymbol == null)
                return false;

            return SymbolEqualityComparer.Default.Equals(
                NormalizeUsageSymbol(targetSymbol),
                NormalizeUsageSymbol(candidateSymbol));
        }

        private static ISymbol NormalizeUsageSymbol(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method && method.ReducedFrom != null)
                symbol = method.ReducedFrom;

            return symbol?.OriginalDefinition ?? symbol;
        }

        private static bool IsPotentialUnresolvedUsage(SyntaxToken token, ISymbol targetSymbol)
        {
            if (targetSymbol is IMethodSymbol method)
            {
                var invocation = GetInvocationForIdentifier(token);
                if (invocation == null || !ArgumentCountMatches(method, invocation.ArgumentList.Arguments.Count))
                    return false;

                string qualifier = GetInvocationQualifier(token);
                if (string.IsNullOrEmpty(qualifier))
                    return true;

                string containingTypeName = method.ContainingType?.Name;
                string containingTypeFullName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    ?.Replace("global::", string.Empty);
                return string.Equals(qualifier, containingTypeName, StringComparison.Ordinal) ||
                    string.Equals(qualifier, containingTypeFullName, StringComparison.Ordinal) ||
                    qualifier.EndsWith("." + containingTypeName, StringComparison.Ordinal);
            }

            return token.Parent is IdentifierNameSyntax;
        }

        private static InvocationExpressionSyntax GetInvocationForIdentifier(SyntaxToken token)
        {
            if (!(token.Parent is SimpleNameSyntax simpleName))
                return null;

            if (simpleName.Parent is InvocationExpressionSyntax directInvocation &&
                directInvocation.Expression == simpleName)
                return directInvocation;

            if (simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName &&
                memberAccess.Parent is InvocationExpressionSyntax memberInvocation &&
                memberInvocation.Expression == memberAccess)
                return memberInvocation;

            return null;
        }

        private static string GetInvocationQualifier(SyntaxToken token)
        {
            if (token.Parent is SimpleNameSyntax simpleName &&
                simpleName.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name == simpleName)
                return NormalizeCompletionExpression(memberAccess.Expression.ToString());

            return string.Empty;
        }

        private static bool ArgumentCountMatches(IMethodSymbol method, int argumentCount)
        {
            if (method == null)
                return false;

            int requiredCount = method.Parameters.Count(parameter => !parameter.HasExplicitDefaultValue && !parameter.IsParams);
            int totalCount = method.Parameters.Length;
            bool hasParams = method.Parameters.Length > 0 && method.Parameters[method.Parameters.Length - 1].IsParams;

            return argumentCount >= requiredCount && (hasParams || argumentCount <= totalCount);
        }

        private static bool IsDeclarationIdentifier(SyntaxToken token)
        {
            SyntaxNode node = token.Parent;
            while (node != null)
            {
                switch (node)
                {
                    case VariableDeclaratorSyntax variable when variable.Identifier == token:
                    case SingleVariableDesignationSyntax designation when designation.Identifier == token:
                    case ParameterSyntax parameter when parameter.Identifier == token:
                    case TypeParameterSyntax typeParameter when typeParameter.Identifier == token:
                    case LocalFunctionStatementSyntax localFunction when localFunction.Identifier == token:
                    case BaseTypeDeclarationSyntax typeDeclaration when typeDeclaration.Identifier == token:
                    case DelegateDeclarationSyntax delegateDeclaration when delegateDeclaration.Identifier == token:
                    case MethodDeclarationSyntax method when method.Identifier == token:
                    case ConstructorDeclarationSyntax constructor when constructor.Identifier == token:
                    case DestructorDeclarationSyntax destructor when destructor.Identifier == token:
                    case PropertyDeclarationSyntax property when property.Identifier == token:
                    case EventDeclarationSyntax eventDeclaration when eventDeclaration.Identifier == token:
                    case EnumMemberDeclarationSyntax enumMember when enumMember.Identifier == token:
                    case ForEachStatementSyntax forEach when forEach.Identifier == token:
                    case CatchDeclarationSyntax catchDeclaration when catchDeclaration.Identifier == token:
                        return true;
                }

                node = node.Parent;
            }

            return false;
        }

        private static UsageLocation CreateUsageLocation(UsageDocument document, SyntaxToken token)
        {
            var lineSpan = token.GetLocation().GetLineSpan();
            int line = lineSpan.StartLinePosition.Line + 1;
            int column = lineSpan.StartLinePosition.Character + 1;
            return new UsageLocation(document.DisplayPath, line, column, document.GetLineText(line).Trim());
        }

        private static void AddUsageLocation(List<UsageLocation> usages, HashSet<string> seen, UsageLocation usage)
        {
            if (usage == null)
                return;

            string key = $"{usage.FilePath}|{usage.Line}|{usage.Column}";
            if (seen.Add(key))
                usages.Add(usage);
        }

        private static List<UsageLocation> SortUsageLocations(List<UsageLocation> usages)
        {
            return usages
                .OrderBy(usage => usage.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(usage => usage.Line)
                .ThenBy(usage => usage.Column)
                .ToList();
        }

        private static IEnumerable<MetadataReference> BuildUsageReferences()
        {
            lock (_usageReferencesLock)
            {
                if (_usagePlatformReferences == null)
                {
                    _usagePlatformReferences = new List<MetadataReference>();
                    string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
                    if (!string.IsNullOrEmpty(trusted))
                    {
                        foreach (var referencePath in trusted.Split(Path.PathSeparator))
                        {
                            if (!string.IsNullOrEmpty(referencePath) && File.Exists(referencePath))
                                _usagePlatformReferences.Add(SharedMetadataReferences.Get(referencePath));
                        }
                    }
                }

                var customReferences = GlobalVariables.customRefList ?? new List<string>();
                string customReferenceKey = string.Join("|", customReferences);
                if (!string.Equals(_usageCustomReferenceKey, customReferenceKey, StringComparison.Ordinal))
                {
                    _usageCustomReferenceKey = customReferenceKey;
                    _usageCustomReferences = new List<MetadataReference>();
                    foreach (var customReference in customReferences)
                    {
                        try
                        {
                            var parts = customReference.Split('|');
                            string referencePath = parts.Length >= 2 ? parts[1] : customReference;
                            if (!string.IsNullOrEmpty(referencePath) && File.Exists(referencePath))
                                _usageCustomReferences.Add(SharedMetadataReferences.Get(referencePath));
                        }
                        catch { }
                    }
                }

                return _usagePlatformReferences.Concat(_usageCustomReferences).ToList();
            }
        }

        private static string NormalizeIdentifierText(string identifier)
        {
            identifier = identifier?.Trim() ?? string.Empty;
            return identifier.StartsWith("@", StringComparison.Ordinal) ? identifier.Substring(1) : identifier;
        }

        private static bool IsValidIdentifierText(string identifier)
        {
            identifier = NormalizeIdentifierText(identifier);
            return identifier.Length > 0 && SyntaxFacts.IsValidIdentifier(identifier);
        }
    }
}
