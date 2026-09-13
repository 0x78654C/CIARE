using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using Dom = ICSharpCode.SharpDevelop.Dom;
using NRefactory = ICSharpCode.NRefactory;
using static global::CIARE.Utils.Completion.CompletionItems;
using static global::CIARE.Utils.Completion.CompletionResources;
using static global::CIARE.Utils.Completion.CompletionWorkspace;

namespace CIARE.Utils.Completion
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class CompletionParsing
    {
        private readonly MainForm _mainForm;

        internal CompletionParsing(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        public static Dom.ProjectContentRegistry pcRegistry;
        internal static Dom.DefaultProjectContent myProjectContent;
        internal static Dom.ParseInformation parseInformation = new Dom.ParseInformation();
        public static bool IsVisualBasic = false;
        Dom.ICompilationUnit lastCompilationUnit;
        public const string DummyFileName = "edited.cs";
        internal         static readonly Dom.LanguageProperties CurrentLanguageProperties = IsVisualBasic ? Dom.LanguageProperties.VBNet : Dom.LanguageProperties.CSharp;
        Thread parserThread;
        internal readonly object _completionDataLock = new object();
        private bool _completionRegistryInitialized;
        internal int _completionTextVersion;
        private int _lastParsedTextVersion = -1;
        private int _lastParsedScopeVersion = -1;
        private HashSet<string> _alreadyLoaded = new HashSet<string>();

        internal void SetCodeCompletion(int index)
        {
            if (GlobalVariables.OCodeCompletion)
            {
                HostCallbackImplementation.Register(_mainForm);
                CodeCompletionKeyHandler.Attach(_mainForm, SelectedEditor.GetSelectedEditor(index));
                ToolTipProvider.Attach(_mainForm, SelectedEditor.GetSelectedEditor(index));

                EnsureCompletionRegistry();
            }
        }

        private void EnsureCompletionRegistry()
        {
            if (_completionRegistryInitialized && pcRegistry != null)
                return;

            pcRegistry = new Dom.ProjectContentRegistry();
            // Persisted DOM tables eagerly recreate every member and defeat lazy metadata.
            _completionRegistryInitialized = true;
        }

        /// <summary>
        /// Relaod ref in texteditor control.
        /// </summary>
        public void ReloadRef()
        {
            if (!_mainForm.isLoaded || _mainForm.Disposing || _mainForm.IsDisposed)
                return;

            if (GlobalVariables.OCodeCompletion)
            {
                if (parserThread != null && parserThread.IsAlive)
                    return;

                EnsureCompletionRegistry();
                parserThread = new Thread(ParserThread);
                parserThread.IsBackground = true;
                parserThread.Start();
            }
        }

        void ParserThread()
        {
            lock (_completionDataLock)
                myProjectContent.AddReferencedContent(pcRegistry.Mscorlib);
            ParseStep();

            Dom.IProjectContent[] total = pcRegistry.LoadAll();

            foreach (var item in total)
            {
                if (_alreadyLoaded.Contains(item.ToString()))
                    continue;

                _alreadyLoaded.Add(item.ToString());

                lock (_completionDataLock)
                {
                    myProjectContent.AddReferencedContent(item);

                    if (myProjectContent is Dom.ReflectionProjectContent myObj) myObj.InitializeReferences();
                }
            }

            while (!_mainForm.IsDisposed)
            {
                ParseStep();
                Thread.Sleep(2000);
            }
        }

        void ParseStep()
        {
            if (!Monitor.TryEnter(_mainForm.CompletionResourcesFeature._completionParseLock)) return;
            try { ParseCompletionStep(); }
            finally { Monitor.Exit(_mainForm.CompletionResourcesFeature._completionParseLock); }
        }

        private void ParseCompletionStep()
        {
            if (_mainForm.IsDisposed || _mainForm.Disposing || !_mainForm.IsHandleCreated)
                return;

            string code = null;
            TextEditorControl editor = null;
            CompletionScopeSnapshot completionScope = default;
            int textVersion = 0;
            try
            {
                _mainForm.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                {
                    if (_mainForm.IsDisposed || _mainForm.Disposing || _mainForm.EditorTabControl.SelectedIndex <= 0)
                        return;

                    completionScope = _mainForm.CompletionWorkspaceFeature.RefreshCompletionScope(_mainForm.EditorFeature.GetActiveEditorFilePath());
                    textVersion = Volatile.Read(ref _completionTextVersion);
                    editor = SelectedEditor.GetSelectedEditor();
                }));
            }
            catch (InvalidOperationException) when (_mainForm.IsDisposed || _mainForm.Disposing || !_mainForm.IsHandleCreated)
            {
                // The form can close between checking its handle and invoking the UI thread.
                return;
            }
            if (editor == null)
                return;

            bool hasStamp = TryGetCompletionSourceStamp(completionScope, out ulong sourceStamp);
            int sourceVersion = Volatile.Read(ref _mainForm.CompletionResourcesFeature._completionSourceVersion);
            bool sameWorkspace = hasStamp && _mainForm.CompletionResourcesFeature._hasParsedSourceStamp && sourceStamp == _mainForm.CompletionResourcesFeature._lastParsedSourceStamp
                && sourceVersion == _mainForm.CompletionResourcesFeature._lastParsedSourceVersion && completionScope.Version == _lastParsedScopeVersion;
            if (sameWorkspace && textVersion == _lastParsedTextVersion)
                return;
            // An unchanged document needs no full-text allocation on each idle poll.
            try
            {
                _mainForm.Invoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    if (editor.IsDisposed || editor != SelectedEditor.GetSelectedEditor() ||
                        !_mainForm.CompletionWorkspaceFeature.IsCompletionScopeCurrent(completionScope))
                        return;
                    textVersion = Volatile.Read(ref _completionTextVersion);
                    code = editor.Text;
                }));
            }
            catch (InvalidOperationException) when (_mainForm.IsDisposed || _mainForm.Disposing || !_mainForm.IsHandleCreated) { return; }
            if (code == null) return;
            if (!sameWorkspace)
                _mainForm.CompletionResourcesFeature.ClearCompletionGlobalUsingsCache();

            var workspaceCompletionClasses = new List<WorkspaceCompletionClass>();
            AddRoslynCompletionClasses(workspaceCompletionClasses, code, completionScope.CurrentFilePath);
            var topLevelFunctions = new List<WorkspaceCompletionItem>();
            CollectTopLevelLocalFunctions(topLevelFunctions, code, completionScope.CurrentFilePath);
            Dom.ICompilationUnit activeCompilationUnit = ParseCompletionCompilationUnit(code,
                completionScope.CurrentFilePath);
            if (!_mainForm.CompletionWorkspaceFeature.IsCompletionScopeCurrent(completionScope))
                return;

            SetActiveCompletionCompilationUnit(activeCompilationUnit, completionScope.CurrentFilePath);
            if (!sameWorkspace)
                _mainForm.CompletionWorkspaceFeature.ParseWorkspaceFilesForCompletion(completionScope, workspaceCompletionClasses);
            if (!_mainForm.CompletionWorkspaceFeature.IsCompletionScopeCurrent(completionScope))
                return;

            lock (_completionDataLock)
            {
                if (!_mainForm.CompletionWorkspaceFeature.IsCompletionScopeCurrent(completionScope))
                    return;

                if (sameWorkspace)
                    _mainForm.CompletionWorkspaceFeature._workspaceCompletionClasses.RemoveAll(item => string.Equals(item.FilePath,
                        completionScope.CurrentFilePath, StringComparison.OrdinalIgnoreCase));
                else
                    _mainForm.CompletionWorkspaceFeature._workspaceCompletionClasses.Clear();
                _mainForm.CompletionWorkspaceFeature._workspaceCompletionClasses.AddRange(workspaceCompletionClasses);
                _mainForm.CompletionWorkspaceFeature._topLevelLocalFunctions.Clear();
                _mainForm.CompletionWorkspaceFeature._topLevelLocalFunctions.AddRange(topLevelFunctions);
                _lastParsedTextVersion = textVersion;
                _lastParsedScopeVersion = completionScope.Version;
                _mainForm.CompletionResourcesFeature._lastParsedSourceStamp = sourceStamp;
                _mainForm.CompletionResourcesFeature._lastParsedSourceVersion = sourceVersion;
                _mainForm.CompletionResourcesFeature._hasParsedSourceStamp = hasStamp;
            }
        }

        internal void RefreshActiveCompletionUnit(string code)
        {
            RefreshActiveCompletionUnit(code, _mainForm.EditorFeature.GetActiveEditorFilePath());
        }

        internal void RefreshActiveCompletionUnit(string code, string currentFilePath)
        {
            if (!GlobalVariables.OCodeCompletion || myProjectContent == null)
                return;
            string preparedCode = _mainForm.CompletionSyntaxFeature.PrepareCodeForNRefactoryCompletion(code ?? string.Empty, currentFilePath,
                out _, out _, out _);
            RefreshPreparedActiveCompletionUnit(preparedCode, currentFilePath, CancellationToken.None);
        }

        internal void RefreshPreparedActiveCompletionUnit(string preparedCode, string currentFilePath,
            CancellationToken cancellationToken)
        {
            if (!GlobalVariables.OCodeCompletion || myProjectContent == null)
                return;

            CompletionScopeSnapshot completionScope = _mainForm.CompletionWorkspaceFeature.RefreshCompletionScope(currentFilePath);
            SetActiveCompletionCompilationUnit(ParsePreparedCompletionCompilationUnit(preparedCode, cancellationToken),
                completionScope.CurrentFilePath);
        }

        private Dom.ICompilationUnit ParseCompletionCompilationUnit(string code, string currentFilePath)
        {
            string parsedCode = _mainForm.CompletionSyntaxFeature.PrepareCodeForNRefactoryCompletion(code ?? string.Empty, currentFilePath,
                out _, out _, out _);
            return ParsePreparedCompletionCompilationUnit(parsedCode, CancellationToken.None);
        }

        private Dom.ICompilationUnit ParsePreparedCompletionCompilationUnit(string parsedCode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            NRefactory.SupportedLanguage supportedLanguage = IsVisualBasic
                ? NRefactory.SupportedLanguage.VBNet
                : NRefactory.SupportedLanguage.CSharp;

            using (TextReader textReader = new StringReader(parsedCode))
            using (NRefactory.IParser parser = NRefactory.ParserFactory.CreateParser(supportedLanguage, textReader))
            {
                parser.ParseMethodBodies = false;
                parser.Parse();
                cancellationToken.ThrowIfCancellationRequested();
                return ConvertCompilationUnit(parser.CompilationUnit);
            }
        }

        private void SetActiveCompletionCompilationUnit(Dom.ICompilationUnit compilationUnit,
            string currentFilePath)
        {
            if (compilationUnit == null)
                return;

            if (compilationUnit is Dom.DefaultCompilationUnit dcuCurrent &&
                !string.IsNullOrEmpty(currentFilePath))
            {
                dcuCurrent.FileName = currentFilePath;
            }

            lock (_completionDataLock)
            {
                myProjectContent.UpdateCompilationUnit(lastCompilationUnit, compilationUnit, DummyFileName);
                lastCompilationUnit = compilationUnit;
                parseInformation.SetCompilationUnit(compilationUnit);
            }
        }

        internal Dom.ICompilationUnit ConvertCompilationUnit(NRefactory.Ast.CompilationUnit cu)
        {
            Dom.NRefactoryResolver.NRefactoryASTConvertVisitor converter;
            converter = new Dom.NRefactoryResolver.NRefactoryASTConvertVisitor(myProjectContent);
            cu.AcceptVisitor(converter, null);
            return converter.Cu;
        }
    }
}
