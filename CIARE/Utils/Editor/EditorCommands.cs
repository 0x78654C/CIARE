using System;
using System.ComponentModel;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Model;
using CIARE.Roslyn;
using CIARE.Utils;
using CIARE.Utils.OpenAISettings;

namespace CIARE
{
    public partial class MainForm
    {
        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
            RealTimeChecker.Cancel(SelectedEditor.GetSelectedEditor(), typeCheckLbl, errorsLV, errorsTabPage, warningsCheckLbl);
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.RunCode(outputRBT, runCodePb, SelectedEditor.GetSelectedEditor(), splitContainer1, true);
        }

        /// <summary>
        /// Exit Main application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Open file on text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int indexTab = EditorTabControl.SelectedIndex;
            FileManage.OpenFileTab(EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
        }

        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) =>
            FileManage.SaveFileTab(EditorTabControl, selectedEditor);

        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.SaveAsDialog(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Clear the editor and path for new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FileManage.NewFile(selectedEditor, outputRBT);
        }

        /// <summary>
        /// Override the key combination listener for file management events.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
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
                    TabControllerManage.SwitchTabs(EditorTabControl, true);
                    return true;
                case Keys.PageUp | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, false);
                    return true;
                case Keys.Q | Keys.Control:
                    LiveShareHost liveShareHost = new LiveShareHost();
                    liveShareHost.ShowDialog();
                    return true;
                case Keys.Left | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, true);
                    return true;
                case Keys.Right | Keys.Control:
                    TabControllerManage.SwitchTabs(EditorTabControl, false);
                    return true;
                case Keys.Tab | Keys.Control:
                    worker = new BackgroundWorker();
                    worker.DoWork += NewHotKeyTab;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.N | Keys.Control:
                    FileManage.NewFile(SelectedEditor.GetSelectedEditor(), outputRBT);
                    return true;
                case Keys.N | Keys.Control | Keys.Shift:
                    newProjectStripMenuItem_Click(this, EventArgs.Empty);
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
                    int indexTab = EditorTabControl.SelectedIndex;
                    FileManage.OpenFileTab(EditorTabControl, SelectedEditor.GetSelectedEditor(indexTab));
                    return true;
                case Keys.O | Keys.Control | Keys.Shift:
                    OpenProjectOrSolutionDialog();
                    return true;
                case Keys.F | Keys.Control:
                    GlobalVariables.findTabOpen = true;
                    FindAndReplace find = new FindAndReplace();
                    find.ShowDialog();
                    return true;
                case Keys.F12 | Keys.Shift:
                    FindUsagesAtCaret();
                    return true;
                case Keys.F5:
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.RunCode(outputRBT, runCodePb, SelectedEditor.GetSelectedEditor(), splitContainer1, true);
                    return true;
                case Keys.T | Keys.Control:
                    FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.B | Keys.Control:
                    GlobalVariables.binaryPublish = false;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    GlobalVariables.binaryPublish = true;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    if (outputTabControl.SelectedTab == errorsTabPage)
                        outputTabControl.SelectedTab = outputTabPage;
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.W | Keys.Control:
                    worker = new BackgroundWorker();
                    worker.DoWork += SplitWindowHorizontally;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.W | Keys.Control | Keys.Shift:
                    worker = new BackgroundWorker();
                    worker.DoWork += SplitWindowVertically;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.K | Keys.Control:
                    OutputWindowManage.SetOutputWindowState(outputRBT, splitContainer1);
                    return true;
                case Keys.E | Keys.Control:
                    ToggleFileExplorer();
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
                        ScheduleCurrentTypeCheck(editorRef);
                    return true;
                case Keys.F11:
                    ToggleFullScreen();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Load predefined C# code sample for run with Roslyn!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Open about window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
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
        private void HotKeyToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            HotKeys hotKeys = new HotKeys();
            hotKeys.ShowDialog();
        }

        /// <summary>
        /// Compile code to binary exe file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = false;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalVariables.binaryPublish = true;
            FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
            if (outputTabControl.SelectedTab == errorsTabPage)
                outputTabControl.SelectedTab = outputTabPage;
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^z");
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^y");
        }

        private void cutStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^x");
        }

        private void copyStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^c");
        }

        private void pasteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^v");
        }

        private void deleteStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("{DELETE}");
        }

        private void replaceStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^h");
        }

        private void selectAllStripMenuItem3_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^a");
        }

        private void splitEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^w");
        }

        private void splitVEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker = new BackgroundWorker();
            worker.DoWork += SplitWindowVertically;
            worker.RunWorkerAsync();
        }

        private void showHideSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^k");
        }

        private void showHideExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleFileExplorer();
        }

        private void goToLineStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^g");
        }

        private void cmdLinesArgsStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^l");
        }

        private void finStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^f");
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options options = new Options();
            options.ShowDialog();
        }

        private void chatGPTCTRLShiftPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
        }

        private void referenceAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^r");
        }
    }
}
