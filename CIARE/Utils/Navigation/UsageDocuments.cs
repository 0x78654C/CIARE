using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CIARE.GUI;
using ICSharpCode.TextEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static global::CIARE.Utils.Completion.CompletionWorkspace;
using static global::CIARE.Utils.Navigation.UsageDocumentCache;
using static global::CIARE.Utils.Projects.ProjectContext;
using static global::CIARE.Utils.Projects.ProjectFiles;
using static global::CIARE.Utils.Projects.WorkspaceFiles;

namespace CIARE.Utils.Navigation
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class UsageDocuments
    {
        private readonly MainForm _mainForm;

        internal UsageDocuments(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal const string CurrentFileUsageDisplayName = "<current file>";

        internal struct OpenTabInfo
        {
            public readonly string FilePath;
            public readonly string Text;
            public readonly bool IsActive;
            public OpenTabInfo(string filePath, string text, bool isActive)
            {
                FilePath = filePath;
                Text = text;
                IsActive = isActive;
            }
        }

        internal sealed class UsageDocument
        {
            private readonly object _linesLock = new object();
            private string[] _lines;

            public UsageDocument(string filePath, string text, SyntaxTree syntaxTree, CompilationUnitSyntax root, bool isActive)
            {
                FilePath = filePath ?? string.Empty;
                Text = text ?? string.Empty;
                SyntaxTree = syntaxTree;
                Root = root;
                IsActive = isActive;
            }

            public string FilePath { get; }
            public string Text { get; }
            public SyntaxTree SyntaxTree { get; }
            public CompilationUnitSyntax Root { get; }
            public bool IsActive { get; }
            public string DisplayPath => string.IsNullOrEmpty(FilePath) ? CurrentFileUsageDisplayName : FilePath;

            public string GetLineText(int lineNumber)
            {
                int index = lineNumber - 1;
                var lines = GetLines();
                return index >= 0 && index < lines.Length ? lines[index] : string.Empty;
            }

            private string[] GetLines()
            {
                if (_lines != null)
                    return _lines;

                lock (_linesLock)
                {
                    if (_lines == null)
                    {
                        _lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    }

                    return _lines;
                }
            }
        }

        internal sealed class UsageLocation
        {
            public UsageLocation(string filePath, int line, int column, string text)
            {
                FilePath = filePath;
                Line = line;
                Column = column;
                Text = text;
            }

            public string FilePath { get; }
            public int Line { get; }
            public int Column { get; }
            public string Text { get; }
        }

        internal List<OpenTabInfo> CollectOpenTabInfo(string identifier)
        {
            var tabs = new List<OpenTabInfo>();
            bool hasActive = false;
            for (int i = 0; i < _mainForm.EditorTabControl.TabPages.Count; i++)
            {
                var tabPage = _mainForm.EditorTabControl.TabPages[i];
                var editor = tabPage.Controls.Count > 0 ? tabPage.Controls[0] as TextEditorControl : null;
                if (editor == null)
                    continue;

                bool isActive = i == _mainForm.EditorTabControl.SelectedIndex;
                string filePath = tabPage.ToolTipText?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(filePath))
                {
                    if (isActive)
                    {
                        tabs.Add(new OpenTabInfo(string.Empty, editor.Text ?? string.Empty, true));
                        hasActive = true;
                    }
                    continue;
                }

                if (!IsCSharpFilePath(filePath))
                    continue;

                if (!isActive && !TextContainsIdentifier(editor.Text, identifier))
                    continue;

                tabs.Add(new OpenTabInfo(filePath, editor.Text ?? string.Empty, isActive));
                if (isActive)
                    hasActive = true;
            }

            if (!hasActive)
            {
                var editor = SelectedEditor.GetSelectedEditor();
                if (editor != null)
                    tabs.Add(new OpenTabInfo(_mainForm.EditorFeature.GetActiveEditorFilePath(), editor.Text ?? string.Empty, true));
            }

            return tabs;
        }

        internal List<OpenTabInfo> CollectActiveUsageTabInfo()
        {
            var editor = SelectedEditor.GetSelectedEditor();
            string filePath = _mainForm.EditorFeature.GetActiveEditorFilePath();
            if (editor == null || !IsCSharpFilePath(filePath))
                return new List<OpenTabInfo>();

            return new List<OpenTabInfo>
            {
                new OpenTabInfo(filePath, editor.Text ?? string.Empty, true)
            };
        }

        internal List<string> GetFileExplorerUsageProjectPaths()
        {
            if (!Directory.Exists(_mainForm.ExplorerTreeFeature._fileExplorerRootPath))
                return new List<string>();

            return EnumerateBuildFiles(_mainForm.ExplorerTreeFeature._fileExplorerRootPath, "*.csproj")
                .Where(IsProjectFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(projectPath => projectPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal IEnumerable<string> GetUsageWorkspaceFolders(string activeFilePath)
        {
            var folders = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddFolder(string folder)
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    return;

                string normalized = NormalizeCompletionPath(folder);
                if (seen.Add(normalized))
                    folders.Add(folder);
            }

            AddFolder(_mainForm.ProjectContextFeature.GetActiveWorkspaceFolder());
            AddUsageFoldersForFile(activeFilePath, AddFolder);

            foreach (TabPage tabPage in _mainForm.EditorTabControl.TabPages)
            {
                string tabPath = tabPage.ToolTipText?.Trim();
                if (IsCSharpFilePath(tabPath))
                    AddUsageFoldersForFile(tabPath, AddFolder);
            }

            lock (_mainForm.CompletionParsingFeature._completionDataLock)
            {
                foreach (var filePath in _mainForm.CompletionWorkspaceFeature._workspaceCompilationUnits.Keys)
                    AddUsageFoldersForFile(filePath, AddFolder);
            }

            return folders;
        }

        internal string GetUsageWorkspaceFolder(string activeFilePath)
        {
            return GetUsageWorkspaceFolders(activeFilePath).FirstOrDefault() ?? string.Empty;
        }

        private static void AddUsageFoldersForFile(string filePath, Action<string> addFolder)
        {
            if (!IsCSharpFilePath(filePath))
                return;

            string activeFolder = Path.GetDirectoryName(filePath);
            addFolder(FindWorkspaceRoot(activeFolder));
            addFolder(FindSolutionOrRepositoryRoot(activeFolder));
        }

        private static string FindSolutionOrRepositoryRoot(string startDir)
        {
            if (string.IsNullOrEmpty(startDir))
                return startDir;

            string dir = startDir;
            string projectRoot = string.Empty;
            for (int i = 0; i < 10; i++)
            {
                if (!Directory.Exists(dir))
                    break;

                try
                {
                    if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0 ||
                        Directory.Exists(Path.Combine(dir, ".git")))
                        return dir;

                    if (string.IsNullOrEmpty(projectRoot) &&
                        Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
                        projectRoot = dir;
                }
                catch { }

                string parent = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(parent) || parent == dir)
                    break;
                dir = parent;
            }

            return projectRoot;
        }
    }
}
