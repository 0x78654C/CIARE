using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE.Designer.Wpf;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class WpfCodeBehind
{
    internal static string Create(WpfDocument document)
    {
        string fullName = document.ClassName;
        if (fullName.Length == 0) throw new InvalidDataException("Add x:Class to the root element in XAML to use a C# code-behind file.");
        int split = fullName.LastIndexOf('.');
        string name = fullName[(split + 1)..], ns = split < 0 ? "" : fullName[..split];
        string baseType = document.Root.Name.LocalName == "Window" ? "System.Windows.Window" : "System.Windows.Controls." + document.Root.Name.LocalName;
        return (ns.Length == 0 ? "" : "namespace " + ns + ";\r\n\r\n") +
            $"public partial class {name} : global::{baseType}\r\n{{\r\n    public {name}()\r\n    {{\r\n        InitializeComponent();\r\n    }}\r\n}}\r\n";
    }
    internal static string EnsureHandler(string code, WpfDocument document, EventSpec evt, string handler)
    {
        if (!DesignDocument.IsIdentifier(handler)) throw new InvalidDataException("Use a valid C# event handler name.");
        var syntax = CSharpSyntaxTree.ParseText(code);
        if (syntax.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) throw new InvalidDataException("Fix the code-behind syntax before generating an event handler.");
        var declaration = syntax.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().SingleOrDefault(c =>
            string.Join(".", c.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()).Append(c.Identifier.ValueText)) == document.ClassName);
        if (declaration == null || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)) throw new InvalidDataException("The code-behind must contain the partial class named by x:Class.");
        var methods = declaration.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == handler).ToArray();
        // Compare qualified/short type names and C# aliases, including generic routed event args.
        string Canonical(string type)
        {
            type = System.Text.RegularExpressions.Regex.Replace(type.Replace("global::", ""), @"\b(?:[A-Za-z_]\w*\.)+", "").Replace(" ", "").TrimEnd('?');
            return System.Text.RegularExpressions.Regex.Replace(type, @"\b(object|string|bool|int|double|float|long|short|byte|char|decimal)\b", m => m.Value switch
            {
                "object" => "Object", "string" => "String", "bool" => "Boolean", "int" => "Int32", "double" => "Double", "float" => "Single",
                "long" => "Int64", "short" => "Int16", "byte" => "Byte", "char" => "Char", "decimal" => "Decimal", _ => m.Value
            });
        }
        if (methods.Length > 0)
        {
            if (!methods.Any(m => m.ReturnType.ToString() == "void" && !m.Modifiers.Any(SyntaxKind.StaticKeyword) && m.ParameterList.Parameters.Count == 2 &&
                m.ParameterList.Parameters.All(p => p.Modifiers.Count == 0) && Canonical(m.ParameterList.Parameters[0].Type.ToString()) == "Object" &&
                Canonical(m.ParameterList.Parameters[1].Type.ToString()) == Canonical(evt.ArgumentType)))
                throw new InvalidDataException("The existing handler has a different event signature. Choose another method name.");
            return code;
        }
        if (declaration.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.ValueText == handler) ||
            declaration.Members.OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText == handler)))
            throw new InvalidDataException("The handler name conflicts with an existing member.");
        string stub = $"\r\n    private void {handler}(object sender, global::{evt.ArgumentType} e)\r\n    {{\r\n        // Add your event-handler code here.\r\n    }}\r\n";
        return code.Insert(declaration.CloseBraceToken.SpanStart, stub);
    }
}
