using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using CIARE.Utils.Encryption;
using CIARE.Utils.OpenAISettings;
using ICSharpCode.TextEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static global::CIARE.Utils.Editor.EditorLayout;
using static global::CIARE.Utils.Projects.ProjectContext;

namespace CIARE.Utils.Editor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class Editor
    {
        private readonly MainForm _mainForm;

        internal Editor(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        public bool visibleSplitContainer = false;
        public bool visibleSplitContainerAutoHide = false;
        internal string _editFontSize = "editorFontSizeZoom";
        public TextEditorControl selectedEditor;
        internal BackgroundWorker worker;
        private string[] _filesDrag;
        internal bool _pendingEditorTextRefresh;
        internal System.Windows.Forms.Timer _editorTextRefreshTimer;

        /// <summary>
        /// Initialize settings once for each new editor.
        /// </summary>
        internal void InitializeEditorSettings(TextEditorControl editor, int index)
        {
            editor.TextEditorProperties.StoreZoomSize = true;
            editor.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, editor);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, editor);
            editor.EnableFolding = GlobalVariables.OFoldingCode;
            editor.ShowLineNumbers = GlobalVariables.OLineNumber;
            _mainForm.CompletionParsingFeature.SetCodeCompletion(index);
            _mainForm.linesCountLbl.Text = string.Empty;
            _mainForm.linesPositionLbl.Text = string.Empty;
            editor.ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;
            HookEditorAskAI(editor);
        }

        /// <summary>
        /// Wire the "Ask AI" context-menu action for an editor instance.
        /// </summary>
        private void HookEditorAskAI(TextEditorControl editor)
        {
            if (editor == null) return;
            var menu = editor.ActiveTextAreaControl.ContextMenuStrip as ICSharpCode.TextEditor.ContextMenu;
            if (menu != null)
            {
                menu.AskAIAction = () => AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
                menu.FindUsagesAction = () => _mainForm.FindUsagesFeature.FindUsagesAtCaret();
            }
        }

        internal void ScheduleCurrentTypeCheck(TextEditorControl editor, string code = null)
        {
            if (!_mainForm.isLoaded || editor == null)
                return;

            string filePath = GetActiveEditorFilePath();
            if (!IsCSharpFilePath(filePath))
            {
                RealTimeChecker.Cancel(editor, _mainForm.typeCheckLbl, _mainForm.errorsLV, _mainForm.errorsTabPage, _mainForm.warningsCheckLbl);
                return;
            }

            if (Directory.Exists(_mainForm.ExplorerTreeFeature._fileExplorerRootPath) &&
                !IsPathInsideFolder(filePath, _mainForm.ExplorerTreeFeature._fileExplorerRootPath))
            {
                _mainForm.CompletionWorkspaceFeature.InvalidateCompletionWorkspace();
                _mainForm.CompletionReferencesFeature.ClearProjectPackageCompletionReferences();
            }

            string workspaceFolder = _mainForm.ProjectContextFeature.GetActiveWorkspaceFolder();
            bool useProjectReferences = !string.IsNullOrEmpty(workspaceFolder) &&
                IsPathInsideFolder(filePath, workspaceFolder);

            RealTimeChecker.ScheduleCheck(code ?? editor.Text, editor, _mainForm.typeCheckLbl, _mainForm.errorsLV, _mainForm.errorsTabPage,
                _mainForm.warningsCheckLbl, workspaceFolder, filePath, useProjectReferences);
        }

        internal string GetActiveEditorFilePath()
        {
            try
            {
                string path = _mainForm.EditorTabControl.SelectedTab?.ToolTipText?.Trim();
                return File.Exists(path) ? path : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal string GetActiveEditorFilePathForCompletion()
        {
            return GetActiveEditorFilePath();
        }

        internal bool IsActiveUntitledEditorPage()
        {
            try
            {
                TabPage selectedTab = _mainForm.EditorTabControl?.SelectedTab;
                if (selectedTab == null)
                    return false;

                string path = selectedTab.ToolTipText?.Trim();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return false;

                string title = (selectedTab.Text ?? string.Empty).TrimStart('*').Trim();
                return string.IsNullOrWhiteSpace(path) &&
                    title.StartsWith("New Page", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            RealTimeChecker.InvalidatePendingCheck();
            Interlocked.Increment(ref _mainForm.CompletionParsingFeature._completionTextVersion);
            TextDataChangedAction();
        }

        /// <summary>
        /// Function for check if text is changed in editor.
        /// </summary>
        private void TextDataChangedAction()
        {
            QueueEditorTextRefresh();

            // Send live share data to api.
            _mainForm.LiveShareEventsFeature.SendData();
        }

        internal void QueueEditorTextRefresh()
        {
            if (!_mainForm.isLoaded || _mainForm.EditorTabControl == null || _mainForm.EditorTabControl.IsDisposed || _mainForm.IsDisposed)
                return;

            _pendingEditorTextRefresh = true;
            if (_editorTextRefreshTimer == null)
            {
                _editorTextRefreshTimer = new System.Windows.Forms.Timer(_mainForm.components)
                {
                    Interval = 200
                };
                _editorTextRefreshTimer.Tick += OnEditorTextRefreshTimer;
            }

            _editorTextRefreshTimer.Stop();
            _editorTextRefreshTimer.Start();
        }

        internal void OnEditorTextRefreshTimer(object sender, EventArgs e)
        {
            _editorTextRefreshTimer?.Stop();
            if (!_pendingEditorTextRefresh)
                return;

            _pendingEditorTextRefresh = false;
            TextEditorControl editor = SelectedEditor.GetSelectedEditor();
            if (editor == null || editor.IsDisposed)
                return;

            string code = editor.Text;
            var path = _mainForm.EditorTabControl.SelectedTab.ToolTipText;
            if (File.Exists(path))
            {
                var md5Txt = FileHash.GetFileHash(code);

                //Remove * depende of file size in comparison text size.
                if (GlobalVariables.openedFileMD5 != md5Txt)
                {
                    var title = $"*{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (_mainForm.Text != title)
                    {
                        _mainForm.Text = title;
                        string curentTabTitle = _mainForm.EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        _mainForm.EditorTabControl.SelectedTab.Text = $"*{curentTabTitle}";
                    }
                }
                else
                {
                    var title = $"{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (_mainForm.Text != title)
                    {
                        _mainForm.Text = title;
                        string curentTabTitle = _mainForm.EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        _mainForm.EditorTabControl.SelectedTab.Text = $"{curentTabTitle}";
                    }
                }
            }

            LinesManage.GetTotalLinesCount(_mainForm.linesCountLbl);
            editor.Document.FoldingManager.FoldingStrategy = new FoldingStrategy();
            editor.Document.FoldingManager.UpdateFoldings(null, null);

            // Trigger real-time type checking after a short debounce.
            ScheduleCurrentTypeCheck(editor, code);
        }

        /// <summary>
        /// Hide output richtextbox on textEditorControl1 focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Enter(object sender, EventArgs e)
        {
            if (!visibleSplitContainerAutoHide)
            {
                SplitContainerHideShow.HideSplitContainer(_mainForm.splitContainer1);
                visibleSplitContainer = true;
            }
        }

        /// <summary>
        /// Set design for every new editor controler.
        /// </summary>
        /// <param name="dynamicTextEdtior"></param>
        internal void SetDesignEditor(ref TextEditorControl dynamicTextEdtior)
        {
            var tabCount = _mainForm.EditorTabControl.TabCount;
            var tabIndex = _mainForm.EditorTabControl.SelectedIndex;
            dynamicTextEdtior.Name = $"textEditorControl{tabCount}";
            dynamicTextEdtior.Anchor = AnchorStyles.None;
            dynamicTextEdtior.Dock = DockStyle.Fill;
            dynamicTextEdtior.BackColor = GetEditorSurfaceBackColor();
            dynamicTextEdtior.BorderStyle = BorderStyle.FixedSingle;
            dynamicTextEdtior.Font = new Font("Consolas", 10F);
            dynamicTextEdtior.Highlighting = null;
            dynamicTextEdtior.Location = new Point(0, 0);
            dynamicTextEdtior.Margin = Padding.Empty;
            dynamicTextEdtior.TabIndex = tabIndex;
            dynamicTextEdtior.VRulerRow = 0;
            dynamicTextEdtior.TextChanged += textEditorControl1_TextChanged;
            dynamicTextEdtior.Enter += textEditorControl1_Enter;
            dynamicTextEdtior.Resize += _mainForm.EditorLayoutFeature.textEditorControl1_Resize;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragDrop += DynamicTextEdtior_DragDrop;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragOver += DynamicTextEdtior_DragEnter;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.AllowDrop = true;
            dynamicTextEdtior.ActiveTextAreaControl.HScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.VScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.KeyPress += CurlyBraket.TextArea_KeyPress;
            dynamicTextEdtior.ActiveTextAreaControl.AutoHideScrollbars = false;
            dynamicTextEdtior.ActiveTextAreaControl.TextEditorProperties.AutoInsertCurlyBracket = true;
            dynamicTextEdtior.ActiveTextAreaControl.VerticalScroll.Enabled = true;
            dynamicTextEdtior.ActiveTextAreaControl.HorizontalScroll.Enabled = true;
            dynamicTextEdtior.TextEditorProperties.StoreZoomSize = true;
            dynamicTextEdtior.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            _mainForm.EditorLayoutFeature.ConfigureEditorControlLayout(dynamicTextEdtior);
        }

        /// <summary>
        /// Drag enter event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicTextEdtior_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// OpenFile in drag drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicTextEdtior_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Count() > 1)
            {
                MessageBox.Show("Only one file can be opened with drag & drop!", "CIARE", MessageBoxButtons.OK,
           MessageBoxIcon.Warning);
                return;
            }
            _filesDrag = files;
            worker = new BackgroundWorker();
            worker.DoWork += AddTabOnDop;
            worker.RunWorkerCompleted += DragDropWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void DragDropWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (sender is BackgroundWorker completedWorker)
            {
                completedWorker.DoWork -= AddTabOnDop;
                completedWorker.RunWorkerCompleted -= DragDropWorkerCompleted;
                completedWorker.Dispose();

                if (ReferenceEquals(worker, completedWorker))
                    worker = null;
            }
        }

        /// <summary>
        /// Open data from drag&drop to new tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTabOnDop(object sender, DoWorkEventArgs e)
        {
            _mainForm.Invoke(delegate
            {
                foreach (var file in _filesDrag)
                {
                    var isFileOpenedInTab = TabControllerManage.IsFileOpenedInTab(MainForm.Instance.EditorTabControl, file);
                    if (isFileOpenedInTab) return;
                    TabControllerManage.AddNewTab(_mainForm.EditorTabControl);
                    FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), file);
                }
            });
        }
    }
}
