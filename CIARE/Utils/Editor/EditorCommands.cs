using System;
using System.ComponentModel;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Model;
using CIARE.Roslyn;
using CIARE.Utils;
using CIARE.Utils.OpenAISettings;

namespace CIARE.Utils.Editor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class EditorCommands
    {
        private readonly MainForm _mainForm;

        internal EditorCommands(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void runCodePb_Click(object sender, EventArgs e)
        {
            RealTimeChecker.Cancel(SelectedEditor.GetSelectedEditor(), _mainForm.typeCheckLbl, _mainForm.errorsLV, _mainForm.errorsTabPage, _mainForm.warningsCheckLbl);
            if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
            RoslynRun.RunCode(_mainForm.outputRBT, _mainForm.runCodePb, SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, true);
        }

        /// <summary>
        /// Exit Main application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Open file on text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int indexTab = _mainForm.EditorTabControl.SelectedIndex;
            FileManage.OpenFileTab(_mainForm.EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
        }

        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void saveToolStripMenuItem_Click(object sender, EventArgs e) =>
            FileManage.SaveFileTab(_mainForm.EditorTabControl, _mainForm.EditorFeature.selectedEditor);

        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.SaveAsDialog(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Clear the editor and path for new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FileManage.NewFile(_mainForm.EditorFeature.selectedEditor, _mainForm.outputRBT);
        }

        /// <summary>
        /// Override the key combination listener for file management events.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        internal bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.U | Keys.Control:
                    SwitchSplit.SwitchSplitWindow();
                    return true;
                case Keys.End | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        var liensCount = SelectedEditor.GetSelectedEditor().Document.TotalNumberOfLines;
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(liensCount);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = liensCount;
                    }
                    return true;
                case Keys.Home | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(0);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = 0;
                    }
                    return true;
                case Keys.PageDown | Keys.Control:
                    TabControllerManage.SwitchTabs(_mainForm.EditorTabControl, true);
                    return true;
                case Keys.PageUp | Keys.Control:
                    TabControllerManage.SwitchTabs(_mainForm.EditorTabControl, false);
                    return true;
                case Keys.Q | Keys.Control:
                    LiveShareHost liveShareHost = new LiveShareHost();
                    liveShareHost.ShowDialog();
                    return true;
                case Keys.Left | Keys.Control:
                    TabControllerManage.SwitchTabs(_mainForm.EditorTabControl, true);
                    return true;
                case Keys.Right | Keys.Control:
                    TabControllerManage.SwitchTabs(_mainForm.EditorTabControl, false);
                    return true;
                case Keys.Tab | Keys.Control:
                    _mainForm.EditorFeature.worker = new BackgroundWorker();
                    _mainForm.EditorFeature.worker.DoWork += _mainForm.TabsFeature.NewHotKeyTab;
                    _mainForm.EditorFeature.worker.RunWorkerAsync();
                    return true;
                case Keys.N | Keys.Control:
                    FileManage.NewFile(SelectedEditor.GetSelectedEditor(), _mainForm.outputRBT);
                    return true;
                case Keys.N | Keys.Control | Keys.Shift:
                    _mainForm.ProjectCommandsFeature.newProjectStripMenuItem_Click(_mainForm, EventArgs.Empty);
                    return true;
                case Keys.H | Keys.Control:
                    GlobalVariables.findTabOpen = false;
                    FindAndReplace findAndReplace = new FindAndReplace();
                    findAndReplace.Show();
                    return true;
                case Keys.S | Keys.Control:
                    FileManage.SaveToFileDialog();
                    return true;
                case Keys.S | Keys.Control | Keys.Shift:
                    FileManage.SaveAsDialog(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.O | Keys.Control:
                    int indexTab = _mainForm.EditorTabControl.SelectedIndex;
                    FileManage.OpenFileTab(_mainForm.EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
                    return true;
                case Keys.O | Keys.Control | Keys.Shift:
                    _mainForm.ProjectCommandsFeature.OpenProjectOrSolutionDialog();
                    return true;
                case Keys.F | Keys.Control:
                    GlobalVariables.findTabOpen = true;
                    FindAndReplace find = new FindAndReplace();
                    find.ShowDialog();
                    return true;
                case Keys.F12 | Keys.Shift:
                    _mainForm.FindUsagesFeature.FindUsagesAtCaret();
                    return true;
                case Keys.F5:
                    if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                        _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
                    RoslynRun.RunCode(_mainForm.outputRBT, _mainForm.runCodePb, SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, true);
                    return true;
                case Keys.T | Keys.Control:
                    FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.B | Keys.Control:
                    GlobalVariables.binaryPublish = false;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                        _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, _mainForm.outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    GlobalVariables.binaryPublish = true;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                        _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, _mainForm.outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.W | Keys.Control:
                    _mainForm.EditorFeature.worker = new BackgroundWorker();
                    _mainForm.EditorFeature.worker.DoWork += _mainForm.EditorLayoutFeature.SplitWindowHorizontally;
                    _mainForm.EditorFeature.worker.RunWorkerAsync();
                    return true;
                case Keys.W | Keys.Control | Keys.Shift:
                    _mainForm.EditorFeature.worker = new BackgroundWorker();
                    _mainForm.EditorFeature.worker.DoWork += _mainForm.EditorLayoutFeature.SplitWindowVertically;
                    _mainForm.EditorFeature.worker.RunWorkerAsync();
                    return true;
                case Keys.K | Keys.Control:
                    OutputWindowManage.SetOutputWindowState(_mainForm.outputRBT, _mainForm.splitContainer1);
                    return true;
                case Keys.E | Keys.Control:
                    _mainForm.ExplorerLayoutFeature.ToggleFileExplorer();
                    return true;
                case Keys.G | Keys.Control:
                    GoToLine goToLine = new GoToLine();
                    goToLine.ShowDialog();
                    return true;
                case Keys.L | Keys.Control:
                    CmdLineArgs cmdLineArgs = new CmdLineArgs();
                    cmdLineArgs.ShowDialog();
                    return true;
                case Keys.P | Keys.Control | Keys.Shift:
                    AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
                    return true;
                case Keys.R | Keys.Control:
                    RefManager refManager = new RefManager();
                    if (!refManager.Visible)
                        refManager.ShowDialog();
                    var editorRef = SelectedEditor.GetSelectedEditor();
                    if (editorRef != null)
                        _mainForm.EditorFeature.ScheduleCurrentTypeCheck(editorRef);
                    return true;
                case Keys.F11:
                    _mainForm.WindowStateFeature.ToggleFullScreen();
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Load predefined C# code sample for run with Roslyn!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void LoadCStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Open about window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        /// <summary>
        /// Show hotkeys info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal void HotKeyToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            HotKeys hotKeys = new HotKeys();
            hotKeys.ShowDialog();
        }

        /// <summary>
        /// Compile code to binary exe file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = false;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, _mainForm.outputRBT, false, GlobalVariables.OutputKind);
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = true;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (_mainForm.outputTabControl.SelectedTab == _mainForm.errorsTabPage)
                _mainForm.outputTabControl.SelectedTab = _mainForm.outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), _mainForm.splitContainer1, _mainForm.outputRBT, false, GlobalVariables.OutputKind);
        }

        internal void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^z");
        }

        internal void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^y");
        }

        internal void cutStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^x");
        }

        internal void copyStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^c");
        }

        internal void pasteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^v");
        }

        internal void deleteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("{DELETE}");
        }

        internal void replaceStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^h");
        }

        internal void selectAllStripMenuItem3_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^a");
        }

        internal void splitEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^w");
        }

        internal void splitVEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainForm.EditorFeature.worker = new BackgroundWorker();
            _mainForm.EditorFeature.worker.DoWork += _mainForm.EditorLayoutFeature.SplitWindowVertically;
            _mainForm.EditorFeature.worker.RunWorkerAsync();
        }

        internal void showHideSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^k");
        }

        internal void showHideExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainForm.ExplorerLayoutFeature.ToggleFileExplorer();
        }

        internal void goToLineStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^g");
        }

        internal void cmdLinesArgsStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^l");
        }

        internal void finStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^f");
        }

        internal void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CIARE.Options options = new CIARE.Options();
            options.ShowDialog();
        }

        internal void chatGPTCTRLShiftPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
        }

        internal void referenceAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^r");
        }
    }
}
