using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CIARE.Utils;
using CIARE.Utils.NuGetManage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CIARE
{
    public partial class MainForm
    {
        private static readonly string[] CompletionImplicitUsingNamespaces =
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks"
        };
        private static readonly Regex FileScopedNamespaceRegex = new Regex(
            @"^[ \t]*namespace[ \t]+([\w.]+)[ \t]*;",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static CSharpParseOptions BuildCompletionParseOptions()
        {
            string framework = GlobalVariables.Framework ?? string.Empty;
            var languageVersion = LanguageVersion.Default;
            if (framework.StartsWith("net6.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp10;
            else if (framework.StartsWith("net7.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp11;
            else if (framework.StartsWith("net8.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp12;
            else if (framework.StartsWith("net9.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp13;
            else if (framework.StartsWith("net10.0", StringComparison.OrdinalIgnoreCase))
                languageVersion = LanguageVersion.CSharp14;

            return CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        }

        private static string BuildCompletionImplicitUsingsCode(string projectPath)
        {
            var namespaces = ProjectNuGetManager.GetImplicitUsingNamespaces(projectPath);
            if (namespaces.Count == 0)
                namespaces.AddRange(CompletionImplicitUsingNamespaces);

            return string.Join(Environment.NewLine,
                namespaces.Distinct(StringComparer.Ordinal).Select(ns => $"global using {ns};"));
        }

        /// <summary>
        /// Converts file-scoped namespace declarations (namespace Foo.Bar;) to
        /// block-scoped (namespace Foo.Bar { ... }) so NRefactory can parse them.
        /// </summary>
        private static string ConvertFileScopedNamespace(string code)
        {
            var match = FileScopedNamespaceRegex.Match(code);
            if (!match.Success)
                return code;
            string nsName = match.Groups[1].Value;
            string before = code.Substring(0, match.Index);
            string after = code.Substring(match.Index + match.Length);
            return before + "namespace " + nsName + "\n{" + after + "\n}";
        }

        internal string PrepareCodeForNRefactoryCompletion(string code, out int prefixLineOffset,
            out int wrapLineOffset, out int bodyStartLine)
        {
            return PrepareCodeForNRefactoryCompletion(code, GetActiveEditorFilePath(),
                out prefixLineOffset, out wrapLineOffset, out bodyStartLine);
        }

        internal string PrepareCodeForNRefactoryCompletion(string code, string filePath,
            out int prefixLineOffset, out int wrapLineOffset, out int bodyStartLine,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            prefixLineOffset = 0;
            wrapLineOffset = 0;
            bodyStartLine = 1;

            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return code;

            string globalUsings = GetProjectGlobalUsingsForCompletion(filePath);
            string codeWithGlobalUsings = code;
            if (!string.IsNullOrEmpty(globalUsings))
            {
                prefixLineOffset = CountLineBreaks(globalUsings);
                codeWithGlobalUsings = globalUsings + code;
            }

            return WrapTopLevelStatementsForNRefactory(ConvertFileScopedNamespace(codeWithGlobalUsings),
                out wrapLineOffset, out bodyStartLine, cancellationToken);
        }

        private string GetProjectGlobalUsingsForCompletion(string sourceFilePath)
        {
            string projectPath = GetCompletionProjectPath(sourceFilePath);
            if (string.IsNullOrEmpty(projectPath)) return string.Empty;
            lock (_completionGlobalUsingsLock)
            {
                int version = Volatile.Read(ref _completionSourceVersion);
                if (_completionGlobalUsings.TryGetValue(projectPath, out var entry) && entry.Version == version)
                    return entry.Text;
                string text = ReadGlobalUsingsAsRegularDirectives(projectPath);
                if (_completionGlobalUsings.Count >= 16) _completionGlobalUsings.Clear();
                _completionGlobalUsings[projectPath] = (version, text);
                return text;
            }
        }

        private string GetCompletionProjectPath(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                return string.Empty;

            if (Directory.Exists(_fileExplorerRootPath))
            {
                return IsPathInsideFolder(sourceFilePath, _fileExplorerRootPath)
                    ? FindProjectFileForPath(sourceFilePath, _fileExplorerRootPath)
                    : string.Empty;
            }

            return string.Empty;
        }

        private static string ReadGlobalUsingsAsRegularDirectives(string projectPath)
        {
            var directives = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string globalUsingsPath = FindProjectGlobalUsingsFile(projectPath);
            if (!string.IsNullOrEmpty(globalUsingsPath))
                AddGlobalUsingDirectivesFromFile(globalUsingsPath, directives, seen);

            try
            {
                string projectDirectory = string.IsNullOrWhiteSpace(projectPath)
                    ? string.Empty
                    : Path.GetDirectoryName(projectPath);
                if (!string.IsNullOrEmpty(projectDirectory) && Directory.Exists(projectDirectory))
                {
                    foreach (string filePath in GetWorkspaceCsFiles(projectDirectory))
                        AddGlobalUsingDirectivesFromFile(filePath, directives, seen);
                }

                foreach (string namespaceName in ProjectNuGetManager.GetImplicitUsingNamespaces(projectPath))
                    AddRegularUsingDirective("using " + namespaceName + ";", directives, seen);

                return directives.Count == 0
                    ? string.Empty
                    : string.Join(Environment.NewLine, directives) + Environment.NewLine;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AddGlobalUsingDirectivesFromFile(string filePath, List<string> directives,
            HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                foreach (string line in File.ReadAllLines(filePath))
                    AddRegularUsingDirective(ConvertGlobalUsingLine(line), directives, seen);
            }
            catch
            {
            }
        }

        private static void AddRegularUsingDirective(string directive, List<string> directives,
            HashSet<string> seen)
        {
            if (!string.IsNullOrWhiteSpace(directive) && seen.Add(directive))
                directives.Add(directive);
        }

        private static string FindProjectGlobalUsingsFile(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return string.Empty;

            string projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
                return string.Empty;

            string objDirectory = Path.Combine(projectDirectory, "obj");
            if (!Directory.Exists(objDirectory))
                return string.Empty;

            try
            {
                string targetFramework = ProjectNuGetManager.GetProjectTargetFramework(projectPath);
                var files = Directory.EnumerateFiles(objDirectory, "*GlobalUsings.g.cs",
                        SearchOption.AllDirectories)
                    .Where(File.Exists)
                    .OrderByDescending(path => IsPreferredGlobalUsingsPath(path, targetFramework))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return files.FirstOrDefault(path => IsPreferredGlobalUsingsPath(path, targetFramework)) ??
                    files.FirstOrDefault() ??
                    string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsPreferredGlobalUsingsPath(string filePath, string targetFramework)
        {
            string framework = string.IsNullOrWhiteSpace(targetFramework)
                ? GlobalVariables.Framework
                : targetFramework;
            if (string.IsNullOrWhiteSpace(framework))
                return false;

            return filePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith(framework, StringComparison.OrdinalIgnoreCase));
        }

        private static string ConvertGlobalUsingLine(string line)
        {
            string trimmed = (line ?? string.Empty).Trim();
            const string prefix = "global using ";
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                return string.Empty;

            string body = trimmed.Substring(prefix.Length);
            if (body.StartsWith("static ", StringComparison.Ordinal))
                return string.Empty;

            string directive = "using " + body.Replace("global::", string.Empty);
            return directive.EndsWith(";", StringComparison.Ordinal) ? directive : directive + ";";
        }

        private static int CountLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int count = 0;
            foreach (char ch in text)
                if (ch == '\n')
                    count++;
            return count;
        }

        /// <summary>
        /// When code uses top-level statements (C# 9+), wraps the body in a synthetic
        /// class and method so the NRefactory resolver can find a method scope and
        /// provide proper code-completion. Using-directives remain at the top so the
        /// resolver still sees them in their original positions.
        /// </summary>
        /// <param name="code">Source code to inspect and possibly wrap.</param>
        /// <param name="lineOffset">
        /// Number of wrapper lines inserted between the using-directives and the body.
        /// 0 when no wrapping was needed.
        /// </param>
        /// <param name="bodyStartLine">
        /// First 1-based line in the <paramref name="code"/> that belongs to the body
        /// (i.e. the first line that will be shifted by <paramref name="lineOffset"/>).
        /// </param>
        /// <returns>Wrapped code, or the original code unchanged when no wrapping is needed.</returns>
        internal static string WrapTopLevelStatementsForNRefactory(string code, out int lineOffset, out int bodyStartLine,
            CancellationToken cancellationToken = default)
        {
            lineOffset = 0;
            bodyStartLine = 1;

            if (IsVisualBasic || string.IsNullOrWhiteSpace(code))
                return code;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
                var root = tree.GetCompilationUnitRoot(cancellationToken);
                if (!root.Members.OfType<GlobalStatementSyntax>().Any())
                    return code;

                // Keep using-directives outside the wrapper so they stay at the same lines.
                int splitPos = 0;
                if (root.Usings.Count > 0)
                    splitPos = root.Usings.Last().FullSpan.End;

                // Compute the 1-based line number of the first body line in the original code.
                int linesBeforeSplit = 0;
                for (int i = 0; i < splitPos && i < code.Length; i++)
                    if (code[i] == '\n') linesBeforeSplit++;
                bodyStartLine = linesBeforeSplit + 1;

                string head = code.Substring(0, splitPos);
                string body = code.Substring(splitPos);

                lineOffset = 2; // "class __TopLevel__ {\n" + "void __Main__() {\n"
                return head + "class __TopLevel__ {\nvoid __Main__() {" + body + "\n}\n}";
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return code;
            }
        }
    }
}
