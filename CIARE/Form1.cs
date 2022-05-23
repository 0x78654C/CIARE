using CIARE.Utils;
using CIARE.GUI;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Document;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace CIARE
{
    public partial class Form1 : Form
    {
        private string _versionName;
        private int _startPos = 0;
        private long _openedFileLength = 0;
        private bool _visibleSplitContainer = false;
        private bool _visibleSplitContainerAutoHide = false;
        public static Form1 Instance { get; private set; }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _versionName = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _versionName = _versionName.Substring(0, _versionName.Length - 2);
            this.Text = $"CIARE {_versionName}";
            WaterMark.TextBoxWaterMark(searchBox, "Find text...");
            ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1, highlightCMB);
            ReadOutputWindowState(GlobalVariables.registryPath);
            Console.SetOut(new ControlWriter(outputRBT));
            Instance = this;
            try
            {
                var args = Environment.GetCommandLineArgs();
                LoadParamFile(args[1], textEditorControl1);
                GlobalVariables.openedFilePath = args[1];
               // CodeCompletionWindow.ShowCompletionWindow(this, textEditorControl1, args[1], null, ' ');
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
                FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                _openedFileLength = fileInfo.Length;
            }
            catch { }
        }

        /// <summary>
        /// Read and apply highlight setting from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="textEditor"></param>
        /// <param name="comboBox"></param>
        private void ReadEditorHighlight(string regKeyName, ICSharpCode.TextEditor.TextEditorControl textEditor, ComboBox comboBox)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", "highlight");
            if (regHighlight.Length > 0)
            {
                if (regHighlight == "C#-Dark")
                    GlobalVariables.darkColor = true;
                textEditor.SetHighlighting(regHighlight);
                comboBox.Text = regHighlight;
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "highlight", "Default");
        }

        /// <summary>
        /// Read output window state from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        private void ReadOutputWindowState(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", "OutWState");
            if (regHighlight.Length > 0)
            {
                if (regHighlight == "False")
                {
                    SplitContainerHideShow.ShowSplitContainer(splitContainer1);
                    _visibleSplitContainer = false;
                    _visibleSplitContainerAutoHide = true;
                }
                else
                {
                    SplitContainerHideShow.HideSplitContainer(splitContainer1);
                    _visibleSplitContainer = true;
                    _visibleSplitContainerAutoHide = false;
                }
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "OutWState", "False");
        }

        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
            RunCode();
        }

        /// <summary>
        /// Compile and run C# code and controlers handle.
        /// </summary>
        private void RunCode()
        {
            ShowOutputOnCompileRun(true);
            findButton.Enabled = false;
            if (GlobalVariables.darkColor)
                outputRBT.ForeColor = Color.FromArgb(192, 215, 207);
            else
                outputRBT.ForeColor = Color.Black;

            outputRBT.Clear();
            outputRBT.Text = "Compile and Runing..\n";
            runCodePb.Image = Properties.Resources.runButton_gray;
            runCodePb.Enabled = false;
            Roslyn.RoslynRun.CompileAndRun(textEditorControl1.Text, outputRBT);
            runCodePb.Image = Properties.Resources.runButton21;
            runCodePb.Enabled = true;
            findButton.Enabled = true;
            GC.Collect();
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
            OpenFile();
        }

        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveToFile();
        }

        /// <summary>
        /// Load data to text editor and sanitize path of file.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="textEditorControl"></param>
        private void LoadParamFile(string data, TextEditorControl textEditorControl)
        {
            data = FileManage.PathCheck(data);
            if (File.Exists(data))
                textEditorControl1.Clear(); textEditorControl.Text = File.ReadAllText(data);
        }

        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        /// <summary>
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (GlobalVariables.openedFilePath.Length > 0)
                this.Text = $"CIARE {_versionName} | *{GlobalVariables.openedFilePath}";
        }

        /// <summary>
        /// Clear the editor and path for new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textEditorControl1.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            this.Text = $"CIARE {_versionName}";
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
                case Keys.N | Keys.Control:
                    NewFile();
                    return true;
                case Keys.H | Keys.Control:
                    FindAndReplace findAndReplace = new FindAndReplace();
                    findAndReplace.Show();
                    return true;
                case Keys.S | Keys.Control:
                    SaveToFile();
                    return true;
                case Keys.S | Keys.Control | Keys.Shift:
                    SaveAs();
                    return true;
                case Keys.O | Keys.Control:
                    OpenFile();
                    return true;
                case Keys.F | Keys.Control:
                    Find(textEditorControl1, searchBox.Text);
                    return true;
                case Keys.R | Keys.Control:
                    RunCode();
                    return true;
                case Keys.T | Keys.Control:
                    LoadCSTemplate();
                    return true;
                case Keys.B | Keys.Control:
                    CompileBinaryExe();
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    CompileBinaryDll();
                    return true;    
                case Keys.W | Keys.Control:
                    SplitEditorWindow.SplitWindow(textEditorControl1,true);
                    return true;
                case Keys.W | Keys.Control | Keys.Shift:
                    SplitEditorWindow.SplitWindow(textEditorControl1,false);
                    return true;
                case Keys.K | Keys.Control:
                    SetOutputWindowState();
                    return true;
                case Keys.G | Keys.Control:
                    GoToLine goToLine = new GoToLine();
                    goToLine.Show();
                    return true;
                case Keys.L | Keys.Control:
                    CmdLineArgs cmdLineArgs = new CmdLineArgs();
                    cmdLineArgs.Show();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Search next engine for text in text editor.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="text"></param>
        private void Find(TextEditorControl editor, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    MessageBox.Show("You need to provide a text to search!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                    return;
                }
                if (!editor.Text.ToLower().Contains(text.ToLower()))
                {
                    MessageBox.Show($"Cannot find: {text}", "CIARE", MessageBoxButtons.OK,
   MessageBoxIcon.Information);
                    return;
                }
                int pos = _startPos;
                int leng = editor.Text.Length;
                string searchText = editor.Text.Substring(pos).ToLower();
                if (!searchText.Contains(text.ToLower()))
                    _startPos = 0;
                var offset = searchText.IndexOf(text.ToLower()) + _startPos;
                var endOffset = offset + text.Length;
                _startPos = endOffset;
                editor.ActiveTextAreaControl.TextArea.Caret.Position = editor.ActiveTextAreaControl.TextArea.Document.OffsetToPosition(endOffset);
                editor.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
                var document = editor.ActiveTextAreaControl.TextArea.Document;
                var selection = new DefaultSelection(document, document.OffsetToPosition(offset), document.OffsetToPosition(endOffset));
                editor.ActiveTextAreaControl.TextArea.SelectionManager.SetSelection(selection);
            }
            catch
            {
                _startPos = 0;
            }
        }

        /// <summary>
        /// Open file and set title with path.
        /// </summary>
        private void OpenFile()
        {
            ManageUnsavedData(textEditorControl1);
            string openedData = FileManage.OpenFile();
            if (openedData.Length > 0)
            {
                textEditorControl1.Clear();
                textEditorControl1.Text = openedData;
                FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                _openedFileLength = fileInfo.Length;
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Set new empty editor.
        /// </summary>
        private void NewFile()
        {
            ManageUnsavedData(textEditorControl1);
            textEditorControl1.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            this.Text = $"CIARE {_versionName}";
        }

        /// <summary>
        /// Save data from editor to a existing file/other file name if no path is found as opened.
        /// </summary>
        private void SaveToFile()
        {
            try
            {
                if (GlobalVariables.openedFilePath.Length > 0)
                {
                    File.WriteAllText(GlobalVariables.openedFilePath, textEditorControl1.Text);
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    _openedFileLength = fileInfo.Length;
                    this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
                    return;
                }
                FileManage.SaveFile(textEditorControl1.Text);
                if (GlobalVariables.savedFile)
                {
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    _openedFileLength = fileInfo.Length;
                    this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Save data to a file.
        /// </summary>
        private void SaveAs()
        {
            FileManage.SaveFile(textEditorControl1.Text);
            FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            _openedFileLength = fileInfo.Length;
            if (GlobalVariables.savedFile)
            {
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Change Highlight of text via combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (highlightCMB.Text.Length > 0)
            {
                textEditorControl1.SetHighlighting(highlightCMB.Text);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlightCMB.Text);
            }
            if(highlightCMB.Text == "C#-Dark")
            {
                GlobalVariables.darkColor = true;
                DarkMode.SetDarkModeMain(this, outputRBT, groupBox1, label1, label2, label3, highlightLbl, 
                    highlightCMB, menuStrip1,searchBox, ListToolStripMenu(), ListToolStripSeparator(),findButton);
                return;
            }
            GlobalVariables.darkColor = false;
            LightMode.SetLightModeMain(this, outputRBT, groupBox1, highlightLbl,
                highlightCMB, menuStrip1, searchBox, ListToolStripMenu(), ListToolStripSeparator(), findButton);
        }

        /// <summary>
        /// List of toolstripmenu from menu bar.
        /// </summary>
        /// <returns></returns>
        private List<ToolStripMenuItem> ListToolStripMenu()
        {
            List<ToolStripMenuItem> listToosStripM = new List<ToolStripMenuItem>();
            listToosStripM.Add(fIleToolStripMenuItem);
            listToosStripM.Add(openToolStripMenuItem);
            listToosStripM.Add(saveToolStripMenuItem);
            listToosStripM.Add(exitToolStripMenuItem);
            listToosStripM.Add(saveAsStripMenuItem);
            listToosStripM.Add(toolStripMenuItem1);
            listToosStripM.Add(LoadCStripMenuItem);
            listToosStripM.Add(helpToolStripMenuItem);
            listToosStripM.Add(aboutToolStripMenuItem);
            listToosStripM.Add(compileToolStripMenuItem);
            listToosStripM.Add(compileToexeCtrlShiftBToolStripMenuItem);
            listToosStripM.Add(compileToDLLCtrlSfitBToolStripMenuItem);
            listToosStripM.Add(editToolStripMenuItem);
            listToosStripM.Add(undoToolStripMenuItem);
            listToosStripM.Add(copyStripMenuItem);
            listToosStripM.Add(cutStripMenuItem);
            listToosStripM.Add(pasteStripMenuItem);
            listToosStripM.Add(deleteStripMenuItem);
            listToosStripM.Add(replaceStripMenuItem);
            listToosStripM.Add(selectAllStripMenuItem3);
            listToosStripM.Add(viewToolStripMenuItem);
            listToosStripM.Add(splitEditorToolStripMenuItem);
            listToosStripM.Add(showHideHSCToolStripMenuItem);
            listToosStripM.Add(goToLineStripMenuItem);
            listToosStripM.Add(cmdLinesArgsStripMenuItem);
            listToosStripM.Add(splitVEditorToolStripMenuItem);
            return listToosStripM;
        }

        /// <summary>
        /// List of ToolStripSeparators from menu bar.
        /// </summary>
        /// <returns></returns>
        private List<ToolStripSeparator> ListToolStripSeparator()
        {
            List<ToolStripSeparator> listToosStripS = new List<ToolStripSeparator>();
            listToosStripS.Add(toolStripSeparator1);
            listToosStripS.Add(toolStripSeparator2);
            listToosStripS.Add(toolStripSeparator3);
            listToosStripS.Add(toolStripSeparator4);
            listToosStripS.Add(toolStripSeparator5);
    
            return listToosStripS;
        }

        /// <summary>
        /// Load predefined C# code sample for run with Roslyn!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCSTemplate();
        }

        /// <summary>
        /// Load C# code sample method.
        /// </summary>
        private void LoadCSTemplate()
        {
            ManageUnsavedData(textEditorControl1);
            DialogResult dr = MessageBox.Show("Do you really want to load C# code template?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                GlobalVariables.openedFilePath = string.Empty;
                this.Text = $"CIARE {_versionName}";
                textEditorControl1.Text = GlobalVariables.roslynTemplate;
            }
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
        /// Find text in text editor button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void findButton_Click(object sender, EventArgs e)
        {
            Find(textEditorControl1, searchBox.Text);
        }

        /// <summary>
        /// Compile code to binary exe file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompileBinaryExe();
        }

        /// <summary>
        /// Compile code to EXE binary file method.
        /// </summary>
        private void CompileBinaryExe()
        {
            GlobalVariables.exeName = true;
            BinaryName binaryName = new BinaryName();
            if (!GlobalVariables.checkFormOpen)
                binaryName.ShowDialog();
            ShowOutputOnCompileRun(false);
            Roslyn.RoslynRun.BinaryCompile(textEditorControl1.Text, true, GlobalVariables.binaryName, outputRBT);
            GC.Collect();
        }

        /// <summary>
        /// Compile code to DLL binary file method.
        /// </summary>
        private void CompileBinaryDll()
        {
            GlobalVariables.exeName = false;
            BinaryName binaryName = new BinaryName();
            if (!GlobalVariables.checkFormOpen)
                binaryName.ShowDialog();
            ShowOutputOnCompileRun(false);
            Roslyn.RoslynRun.BinaryCompile(textEditorControl1.Text, false, GlobalVariables.binaryName, outputRBT);
            GC.Collect();
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompileBinaryDll();
        }

        /// <summary>
        /// Run the method for unsaved data check on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ManageUnsavedData(textEditorControl1);
        }

        /// <summary>
        /// Handle unsaved data from editor on from closing event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        private void ManageUnsavedData(TextEditorControl textEditorControl)
        {
            DialogResult dr = DialogResult.No;
            if (this.Text.Contains("| *"))
            {
                dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Warning);
            }
            else if (!this.Text.Contains("|"))
            {
                if (!string.IsNullOrEmpty(textEditorControl.Text))
                    dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNo,
    MessageBoxIcon.Warning);
            }

            if (dr == DialogResult.Yes)
                SaveToFile();
        }


        /// <summary>
        /// Check edited opened files by external application when CIARE is on Top Most event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Activated(object sender, EventArgs e)
        {
            CheckFileExternalEdited(GlobalVariables.openedFilePath, _openedFileLength, textEditorControl1);
        }

        /// <summary>
        /// Check if opened files is edited by an external application and ask if want to reaload the changed file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileSize"></param>
        /// <param name="textEditorControl"></param>
        private void CheckFileExternalEdited(string filePath, long fileSize, TextEditorControl textEditorControl)
        {
            if (!File.Exists(filePath))
                return;

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileSize != fileInfo.Length)
            {
                DialogResult dr = MessageBox.Show("The opened file content was changed.\nDo you want to reload it?", "CIARE", MessageBoxButtons.YesNo,
    MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        textEditorControl.Clear();
                        textEditorControl.Text = reader.ReadToEnd();
                        this.Text = $"CIARE {_versionName} | {filePath}";
                        _openedFileLength = fileInfo.Length;
                    }
                    return;
                }
                _openedFileLength = fileInfo.Length;
            }
        }

        #region Hotkeys for Edit Menu
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^z");
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
            SplitEditorWindow.SplitWindow(textEditorControl1, true);
        }

        private void splitVEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^w");
        }

        private void showHideSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^k");
        }

        private void goToLineStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^g");
        }

        private void cmdLinesArgsStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^l");
        }
        #endregion

        /// <summary>
        /// Pop up the output pane on compile or code run.
        /// </summary>
        private void ShowOutputOnCompileRun(bool runner)
        {
            if (runner)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer1);
                _visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outputRBT.Focus();
                return;
            }

            if (GlobalVariables.outPutDisplay)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer1);
                _visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outputRBT.Focus();
            }
        }

        /// <summary>
        /// Set output window state.
        /// </summary>
        private void SetOutputWindowState()
        {
            if (_visibleSplitContainer)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer1);
                _visibleSplitContainer = false;
                _visibleSplitContainerAutoHide = true;
                outputRBT.Focus();
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "False");
            }
            else
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer1);
                _visibleSplitContainer = true;
                _visibleSplitContainerAutoHide = false;
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "True");
            }
        }

        /// <summary>
        /// Hide output richtextbox on textEditorControl1 focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Enter(object sender, EventArgs e)
        {
            if (!_visibleSplitContainerAutoHide)
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer1);
                _visibleSplitContainer = true;
            }
        }
    }
}
