using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE
{
    public partial class MainForm
    {
        private static void AddRoslynCompletionClasses(List<WorkspaceCompletionClass> result, string code, string filePath)
        {
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                foreach (var member in root.Members)
                    AddRoslynCompletionMember(result, member, string.Empty, string.Empty, null, filePath);
            }
            catch
            {
                // The NRefactory parser still handles the primary completion path.
            }
        }

        private static void CollectTopLevelLocalFunctions(List<WorkspaceCompletionItem> result, string code, string filePath)
        {
            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                foreach (var globalStmt in root.Members.OfType<GlobalStatementSyntax>())
                {
                    if (globalStmt.Statement is LocalFunctionStatementSyntax localFunc)
                    {
                        string name = localFunc.Identifier.ValueText;
                        if (string.IsNullOrEmpty(name))
                            continue;
                        string parameters = string.Join(", ", localFunc.ParameterList.Parameters.Select(p => p.ToString()));
                        string returnType = localFunc.ReturnType.ToString();
                        string description = returnType + " " + name + "(" + parameters + ")";
                        int line = localFunc.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        result.Add(new WorkspaceCompletionItem(name, description, 1, filePath, line));
                    }
                    else if (globalStmt.Statement is LocalDeclarationStatementSyntax localDecl)
                    {
                        int line = localDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        string typeStr = localDecl.Declaration.Type.ToString();
                        foreach (var declarator in localDecl.Declaration.Variables)
                        {
                            string name = declarator.Identifier.ValueText;
                            if (string.IsNullOrEmpty(name))
                                continue;
                            result.Add(new WorkspaceCompletionItem(name, typeStr + " " + name, 0, filePath, line));
                        }
                    }
                }
            }
            catch { }
        }

        private static void AddRoslynCompletionMember(
            List<WorkspaceCompletionClass> result,
            MemberDeclarationSyntax member,
            string namespaceName,
            string containingTypeName,
            WorkspaceCompletionClass parentClass,
            string filePath)
        {
            if (member is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                string childNamespace = CombineName(namespaceName, namespaceDeclaration.Name.ToString());
                foreach (var childMember in namespaceDeclaration.Members)
                    AddRoslynCompletionMember(result, childMember, childNamespace, containingTypeName, parentClass, filePath);
                return;
            }

            if (member is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                string childNamespace = CombineName(namespaceName, fileScopedNamespace.Name.ToString());
                foreach (var childMember in fileScopedNamespace.Members)
                    AddRoslynCompletionMember(result, childMember, childNamespace, containingTypeName, parentClass, filePath);
                return;
            }

            if (member is BaseTypeDeclarationSyntax typeDeclaration)
            {
                bool isNested = parentClass != null;
                if (!IsWorkspaceVisibleType(typeDeclaration.Modifiers, isNested))
                    return;

                string typeName = typeDeclaration.Identifier.ValueText;
                if (string.IsNullOrEmpty(typeName))
                    return;

                string typePrefix = string.IsNullOrEmpty(containingTypeName) ? namespaceName : containingTypeName;
                var completionClass = new WorkspaceCompletionClass(
                    typeName,
                    CombineName(typePrefix, typeName),
                    namespaceName,
                    GetTypeKeyword(typeDeclaration),
                    GetTypeImageIndex(typeDeclaration),
                    isNested,
                    filePath);

                result.Add(completionClass);
                if (parentClass != null)
                    parentClass.NestedTypes.Add(completionClass.ToCompletionItem());

                if (typeDeclaration is TypeDeclarationSyntax typeWithMembers)
                {
                    foreach (var childMember in typeWithMembers.Members)
                    {
                        if (childMember is BaseTypeDeclarationSyntax || childMember is NamespaceDeclarationSyntax || childMember is FileScopedNamespaceDeclarationSyntax)
                            AddRoslynCompletionMember(result, childMember, namespaceName, completionClass.FullName, completionClass, filePath);
                        else
                            AddRoslynStaticMember(completionClass, childMember);
                    }
                }
                else if (typeDeclaration is EnumDeclarationSyntax enumDeclaration)
                {
                    foreach (var enumMember in enumDeclaration.Members)
                    {
                        string enumMemberName = enumMember.Identifier.ValueText;
                        if (enumMemberName.Length > 0)
                            completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                                enumMemberName,
                                completionClass.FullName + "." + enumMemberName,
                                3,
                                completionClass.FilePath,
                                GetSyntaxLine(enumMember)));
                    }
                }
            }
        }

        private static void AddRoslynStaticMember(WorkspaceCompletionClass completionClass, MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax fieldDeclaration)
            {
                if (!IsWorkspaceVisibleMember(fieldDeclaration.Modifiers) || !IsStaticOrConst(fieldDeclaration.Modifiers))
                    return;

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    string name = variable.Identifier.ValueText;
                    if (name.Length == 0)
                        continue;

                    string description = JoinDeclarationParts(GetModifierText(fieldDeclaration.Modifiers), fieldDeclaration.Declaration.Type.ToString(), name);
                    completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                        name,
                        description,
                        3,
                        completionClass.FilePath,
                        GetSyntaxLine(variable)));
                }
                return;
            }

            if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                if (!IsWorkspaceVisibleMember(propertyDeclaration.Modifiers) || !HasModifier(propertyDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = propertyDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string description = JoinDeclarationParts(GetModifierText(propertyDeclaration.Modifiers), propertyDeclaration.Type.ToString(), name);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    2,
                    completionClass.FilePath,
                    GetSyntaxLine(propertyDeclaration)));
                return;
            }

            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                if (!IsWorkspaceVisibleMember(methodDeclaration.Modifiers) || !HasModifier(methodDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = methodDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string parameters = string.Join(", ", methodDeclaration.ParameterList.Parameters.Select(parameter => parameter.ToString()));
                string signature = name + "(" + parameters + ")";
                string description = JoinDeclarationParts(GetModifierText(methodDeclaration.Modifiers), methodDeclaration.ReturnType.ToString(), signature);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    1,
                    completionClass.FilePath,
                    GetSyntaxLine(methodDeclaration)));
                return;
            }

            if (member is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                if (!IsWorkspaceVisibleMember(eventFieldDeclaration.Modifiers) || !HasModifier(eventFieldDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                foreach (var variable in eventFieldDeclaration.Declaration.Variables)
                {
                    string name = variable.Identifier.ValueText;
                    if (name.Length == 0)
                        continue;

                    string description = JoinDeclarationParts(GetModifierText(eventFieldDeclaration.Modifiers), "event", eventFieldDeclaration.Declaration.Type.ToString(), name);
                    completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                        name,
                        description,
                        6,
                        completionClass.FilePath,
                        GetSyntaxLine(variable)));
                }
                return;
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                if (!IsWorkspaceVisibleMember(eventDeclaration.Modifiers) || !HasModifier(eventDeclaration.Modifiers, SyntaxKind.StaticKeyword))
                    return;

                string name = eventDeclaration.Identifier.ValueText;
                if (name.Length == 0)
                    return;

                string description = JoinDeclarationParts(GetModifierText(eventDeclaration.Modifiers), "event", eventDeclaration.Type.ToString(), name);
                completionClass.StaticMembers.Add(new WorkspaceCompletionItem(
                    name,
                    description,
                    6,
                    completionClass.FilePath,
                    GetSyntaxLine(eventDeclaration)));
            }
        }

        private static int GetSyntaxLine(SyntaxNode node)
        {
            return node?.GetLocation().GetLineSpan().StartLinePosition.Line + 1 ?? 0;
        }

        private static bool IsWorkspaceVisibleType(SyntaxTokenList modifiers, bool isNested)
        {
            if (HasModifier(modifiers, SyntaxKind.PrivateKeyword) || HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
                return false;

            return !isNested || HasModifier(modifiers, SyntaxKind.PublicKeyword) || HasModifier(modifiers, SyntaxKind.InternalKeyword);
        }

        private static bool IsWorkspaceVisibleMember(SyntaxTokenList modifiers)
        {
            return HasModifier(modifiers, SyntaxKind.PublicKeyword) || HasModifier(modifiers, SyntaxKind.InternalKeyword);
        }

        private static bool IsStaticOrConst(SyntaxTokenList modifiers)
        {
            return HasModifier(modifiers, SyntaxKind.StaticKeyword) || HasModifier(modifiers, SyntaxKind.ConstKeyword);
        }

        private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier.IsKind(kind))
                    return true;
            }
            return false;
        }

        private static string CombineName(string prefix, string name)
        {
            if (string.IsNullOrEmpty(prefix))
                return name ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                return prefix;
            return prefix + "." + name;
        }

        private static string GetModifierText(SyntaxTokenList modifiers)
        {
            return string.Join(" ", modifiers.Select(modifier => modifier.ValueText));
        }

        private static string JoinDeclarationParts(params string[] parts)
        {
            return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string GetTypeKeyword(BaseTypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration is InterfaceDeclarationSyntax)
                return "interface";
            if (typeDeclaration is StructDeclarationSyntax)
                return "struct";
            if (typeDeclaration is EnumDeclarationSyntax)
                return "enum";
            if (typeDeclaration is RecordDeclarationSyntax)
                return "record";
            return "class";
        }

        private static int GetTypeImageIndex(BaseTypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration is EnumDeclarationSyntax ? 4 : 0;
        }

        private sealed class WorkspaceCompletionClass
        {
            public WorkspaceCompletionClass(string name, string fullName, string namespaceName, string kindKeyword, int imageIndex, bool isNested, string filePath)
            {
                Name = name;
                FullName = fullName;
                NamespaceName = namespaceName ?? string.Empty;
                KindKeyword = kindKeyword;
                ImageIndex = imageIndex;
                IsNested = isNested;
                FilePath = filePath;
            }

            public string Name { get; }
            public string FullName { get; }
            public string NamespaceName { get; }
            public string KindKeyword { get; }
            public int ImageIndex { get; }
            public bool IsNested { get; }
            public string FilePath { get; }
            public List<WorkspaceCompletionItem> StaticMembers { get; } = new List<WorkspaceCompletionItem>();
            public List<WorkspaceCompletionItem> NestedTypes { get; } = new List<WorkspaceCompletionItem>();

            public WorkspaceCompletionItem ToCompletionItem()
            {
                return new WorkspaceCompletionItem(Name, KindKeyword + " " + FullName, ImageIndex);
            }
        }

        private sealed class WorkspaceCompletionItem
        {
            public WorkspaceCompletionItem(string name, string description, int imageIndex, string filePath = null, int line = 0)
            {
                Name = name;
                Description = description;
                ImageIndex = imageIndex;
                FilePath = filePath;
                Line = line;
            }

            public string Name { get; }
            public string Description { get; }
            public int ImageIndex { get; }
            public string FilePath { get; }
            public int Line { get; }
        }
    }
}
