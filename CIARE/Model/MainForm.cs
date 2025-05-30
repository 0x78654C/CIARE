﻿/*
       Description: Simple text editor for Windows with C# runtime compiler and code execution using Roslyn. 
       Useful to run code on the fly and get instant result.

       This app is distributed under the MIT License.
       Copyright © 2022 - 2024 x_coding. All rights reserved.

       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
*/
using CIARE.Utils;
using CIARE.GUI;
using CIARE.Utils.Options;
using ICSharpCode.TextEditor;
using System;
using System.IO;
using System.Windows.Forms;
using NRefactory = ICSharpCode.NRefactory;
using Dom = ICSharpCode.SharpDevelop.Dom;
using System.Threading;
using CIARE.Roslyn;
using CIARE.Utils.FilesOpenOS;
using System.Runtime.Versioning;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using CIARE.LiveShareManage;
using System.Threading.Tasks;
using CIARE.Utils.OpenAISettings;
using Button = System.Windows.Forms.Button;
using CIARE.Model;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.ComponentModel;
using CIARE.Utils.Encryption;


namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
        public HubConnection hubConnection;
        public bool visibleSplitContainer = false;
        public bool visibleSplitContainerAutoHide = false;
        public bool isLoaded = false;
        private string _editFontSize = "editorFontSizeZoom";
        public static MainForm Instance { get; private set; }
        public static Dom.ProjectContentRegistry pcRegistry;
        internal static Dom.DefaultProjectContent myProjectContent;
        internal static Dom.ParseInformation parseInformation = new Dom.ParseInformation();
        public static bool IsVisualBasic = false;
        Dom.ICompilationUnit lastCompilationUnit;
        public const string DummyFileName = "edited.cs";
        static readonly Dom.LanguageProperties CurrentLanguageProperties = IsVisualBasic ? Dom.LanguageProperties.VBNet : Dom.LanguageProperties.CSharp;
        Thread parserThread;
        private string s_args = SplitArguments.GetCommandLineArgs();
        private ApiConnectionEvents _apiConnectionEvents;
        public TextEditorControl selectedEditor;
        TextEditorControl dynamicTextEdtior;
        private int _countTabs = 0;
        BackgroundWorker worker;
        private string[] _filesDrag;


        // Used for tab's auto-resize
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;
        //----------------------------

        public MainForm()
        {
            InitializeEditor.CreateUserDataDirectory(GlobalVariables.userProfileDirectory, GlobalVariables.markFile);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, "highlight", "C#-Dark");
            var autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.CheckSetAtiveFormState();
            autoStartFile.OpenFilesOnLongOn(ReadArgs(s_args));
            InitializeComponent();
        }

        /// <summary>
        /// Initiliaze settings for dynamic text edtior.
        /// </summary>
        /// <param name="index"></param>
        private void Initiliaze(int index = 1)
        {
            EditorTabControl.SelectedIndex = index;
            int selectedTab = EditorTabControl.SelectedIndex;
            int countTabs = EditorTabControl.TabCount - 1;
            Control ctrl = EditorTabControl.Controls[countTabs].Controls[0];
            selectedEditor = ctrl as TextEditorControl;
            selectedEditor.TextEditorProperties.StoreZoomSize = true;
            selectedEditor.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            InitializeEditor.ReadEditorWindowSize(this, GlobalVariables.registryPath);
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, selectedEditor);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, selectedEditor);
            InitializeEditor.ReadOutputWindowState(GlobalVariables.registryPath, splitContainer1);
            InitializeEditor.WinLoginState(GlobalVariables.registryPath, GlobalVariables.OWinLogin, out GlobalVariables.OWinLoginState);
            FoldingCode.CheckFoldingCodeStatus(GlobalVariables.registryPath);
            LineNumber.CheckLineNumberStatus(GlobalVariables.registryPath);
            SetCodeCompletion(index);
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Hide();
            Instance = this;
            this.Text = $"CIARE {GlobalVariables.versionName}";
            TabControllerManage.CleanFileSizeStoreFile(GlobalVariables.tabsFilePath);
            Initiliaze();
            Console.SetOut(new ControlWriter(outputRBT));
            InitializeEditor.GenerateLiveSessionId();
            InitializeEditor.CleanNugetFolder(GlobalVariables.downloadNugetPath);
            CodeCompletion.CheckCodeCompletion(GlobalVariables.registryPath);
            StartFilesOS.CheckOSStartFile(GlobalVariables.registryPath);
            StartFilesOS.CheckWinLoginState(GlobalVariables.registryPath);
            BuildConfig.CheckConfig(GlobalVariables.registryPath);
            BuildConfig.CheckPlatform(GlobalVariables.registryPath);
            TargetFramework.CheckFramework(GlobalVariables.registryPath);
            LiveShare.CheckApiLiveShare(GlobalVariables.registryPath);
            OpenAISetting.CheckOpenAIData(GlobalVariables.registryPath);
            UnsafeCode.CheckUnsafeStatus(GlobalVariables.registryPath);
            Publish.CheckPublishStatus(GlobalVariables.registryPath);
            if (GlobalVariables.OStartUp)
                TabControllerManage.ReadTabs(EditorTabControl, SelectedEditor.GetSelectedEditor(), GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll);
            else
                TabControllerManage.CleanStoredTabs(GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll);
            this.Show();
            myProjectContent = new Dom.DefaultProjectContent();
            myProjectContent.Language = CurrentLanguageProperties;
            _apiConnectionEvents = new ApiConnectionEvents();
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;

            //File open via parameters(Open with option..)
            string arg = ReadArgs(s_args);
            FileManage.OpenFileFromArgs(arg, EditorTabControl);
            //----------------------------------

            if (!GlobalVariables.isCLIOpen)
            {
                InitializeEditor.GetTabIndexPosLine(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, EditorTabControl);
            }
            else
            {
                TabControllerManage.IsFileOpenedInTab(MainForm.Instance.EditorTabControl, GlobalVariables.openedFilePath);
            }
            isLoaded = true;
            ReloadRef();

            // Get last opened tab MD5.
            if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                GlobalVariables.openedFileMD5 = MD5Hash.GetMD5Hash(SelectedEditor.GetSelectedEditor().Text);
            
        }

        private void SetCodeCompletion(int index)
        {
            if (GlobalVariables.OCodeCompletion)
            {
                HostCallbackImplementation.Register(this);
                CodeCompletionKeyHandler.Attach(this, SelectedEditor.GetSelectedEditor(index));
                ToolTipProvider.Attach(this, SelectedEditor.GetSelectedEditor(index));

                pcRegistry = new Dom.ProjectContentRegistry();
                if (!Directory.Exists((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"))))
                    Directory.CreateDirectory((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion")));

                pcRegistry.ActivatePersistence(Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"));
                SelectedEditor.GetSelectedEditor(index).ActiveTextAreaControl.Refresh();
            }
        }

        /// <summary>
        /// Read arguments on CIARE start.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string ReadArgs(string args)
        {
            if (args.StartsWith("\"") && args.Length > 1)
            {
                int ix = args.IndexOf("\"", 1);

                if (ix != -1)
                {
                    args = args.Substring(ix + 1).TrimStart();
                }
            }
            else
            {
                int ix = args.IndexOf(" ");

                if (ix != -1)
                {
                    args = args.Substring(ix + 1).TrimStart();
                }
            }
            args = args.Trim('"');
            return args;
        }

        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
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
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)  => TextDataChangedAction();

        /// <summary>
        /// Function for check if text is changed in editor.
        /// </summary>
        private void TextDataChangedAction()
        {
            var path = EditorTabControl.SelectedTab.ToolTipText;
            if (File.Exists(path))
            {
                var md5Txt = MD5Hash.GetMD5Hash(SelectedEditor.GetSelectedEditor().Text);

                //Remove * depende of file size in comparison text size.
                if (GlobalVariables.openedFileMD5 != md5Txt)
                {
                    var title = $"*{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (Text != title)
                    {
                        this.Text = title;
                        string curentTabTitle = EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        EditorTabControl.SelectedTab.Text = $"*{curentTabTitle}";
                    }
                }
                else
                {
                    var title = $"{GlobalVariables.openedFileName.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";
                    if (Text != title)
                    {
                        this.Text = title;
                        string curentTabTitle = EditorTabControl.SelectedTab.Text.Replace("*", string.Empty);
                        EditorTabControl.SelectedTab.Text = $"{curentTabTitle}";
                    }
                }
            }

            LinesManage.GetTotalLinesCount(linesCountLbl);
            SelectedEditor.GetSelectedEditor().Document.FoldingManager.FoldingStrategy = new FoldingStrategy();
            SelectedEditor.GetSelectedEditor().Document.FoldingManager.UpdateFoldings(null, null);

            // Send live share data to api.
            SendData();
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
        /// Add new tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewHotKeyTab(object sender, DoWorkEventArgs e)
        {
            if (outputRBT.ForeColor == Color.Red)
                GlobalVariables.isRed = true;
            TabControllerManage.AddNewTab(EditorTabControl);
        }

        /// <summary>
        /// Split window horizontaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitWindowHorizontally(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), true);
            });
        }

        /// <summary>
        /// Split window verticaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitWindowVertically(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), false);
            });
        }

        #region HotKeys Actions
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
                    TabControllerManage.SwitchTabs(ref EditorTabControl, true);
                    return true;
                case Keys.PageUp | Keys.Control:
                    TabControllerManage.SwitchTabs(ref EditorTabControl, false);
                    return true;
                case Keys.Q | Keys.Control:
                    LiveShareHost liveShareHost = new LiveShareHost();
                    liveShareHost.ShowDialog();
                    return true;
                case Keys.Left | Keys.Control:
                    TabControllerManage.SwitchTabs(ref EditorTabControl, true);
                    return true;
                case Keys.Right | Keys.Control:
                    TabControllerManage.SwitchTabs(ref EditorTabControl, false);
                    return true;
                case Keys.Tab | Keys.Control:
                    worker = new BackgroundWorker();
                    worker.DoWork += NewHotKeyTab;
                    worker.RunWorkerAsync();
                    return true;
                case Keys.N | Keys.Control:
                    FileManage.NewFile(SelectedEditor.GetSelectedEditor(), outputRBT);
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
                case Keys.F | Keys.Control:
                    GlobalVariables.findTabOpen = true;
                    FindAndReplace find = new FindAndReplace();
                    find.ShowDialog();
                    return true;
                case Keys.F5:
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    RoslynRun.RunCode(outputRBT, runCodePb, SelectedEditor.GetSelectedEditor(), splitContainer1, true);
                    return true;
                case Keys.T | Keys.Control:
                    FileManage.LoadCSTemplate(SelectedEditor.GetSelectedEditor());
                    return true;
                case Keys.B | Keys.Control:
                    GlobalVariables.binaryPublish = false;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
                    RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    GlobalVariables.binaryPublish = true;
                    FileManage.CompileRunSaveData(SelectedEditor.GetSelectedEditor());
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
                case Keys.G | Keys.Control:
                    GoToLine goToLine = new GoToLine();
                    goToLine.ShowDialog();
                    return true;
                case Keys.L | Keys.Control:
                    CmdLineArgs cmdLineArgs = new CmdLineArgs();
                    cmdLineArgs.ShowDialog();
                    return true;
                case Keys.P | Keys.Control | Keys.Shift:
                    AiManage.LoadProgressBar();
                    AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
                    return true;
                case Keys.R | Keys.Control:
                    RefManager refManager = new RefManager();
                    if (!refManager.Visible)
                        refManager.ShowDialog();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        public void SetHighLighter(TextEditorControl textEditorControl, string highlight)
        {
            if (highlight.Length > 0)
            {
                textEditorControl.SetHighlighting(highlight);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlight);
            }
            if (highlight.StartsWith("C#-Dark"))
            {
                GlobalVariables.darkColor = true;
                ICSharpCode.TextEditor.Gui.CompletionWindow.CodeCompletionListView.darkMode = true;
                GlobalVariables.isVStheme = (highlight.EndsWith("VS")) ? true : false;
                DarkModeMain.SetDarkModeMain(this, outputRBT, groupBox1, label2, label3,
                    menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator(), GlobalVariables.isVStheme);
                return;
            }
            GlobalVariables.darkColor = false;
            ICSharpCode.TextEditor.Gui.CompletionWindow.CodeCompletionListView.darkMode = false;
            LightModeMain.SetLightModeMain(this, outputRBT, groupBox1,
                menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
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
            RoslynRun.CompileBinary(SelectedEditor.GetSelectedEditor(), splitContainer1, outputRBT, false, GlobalVariables.OutputKind);
        }


        /// <summary>
        /// Run the method for unsaved data check on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Store tab text of current opened tab.
            var toolTipText = EditorTabControl.SelectedTab.ToolTipText.Trim();
            if (toolTipText.StartsWith("Add Tab"))
                TabControllerManage.StoreTabPosition(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, string.Empty);
            else
                TabControllerManage.StoreTabPosition(GlobalVariables.registryPath, GlobalVariables.OlastTabPosition, EditorTabControl.SelectedTab.ToolTipText.Trim());

            FileManage.ManageUnsavedData(SelectedEditor.GetSelectedEditor(), 0, true);
            if (GlobalVariables.noClear)
            {
                e.Cancel = true;
                GlobalVariables.noClear = false;
                return;
            }
            else
                e.Cancel = false;

            // Delete temp mark file.
            if (GetCiareProcesses() == 1)
            {
                AutoStartFile autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, "");
                autoStartFile.DelTempFile();
            }

            // Set if form is not active anymore if there is not process left
            if (ProcessRun.CheckActiveProcessCount("CIARE") <= 1)
            {
                CrashCheck crashCheck = new CrashCheck(GlobalVariables.registryPath, GlobalVariables.activeForm);
                crashCheck.SetClosedFormState();
            }

            // Stop Live share if connected.
            Task.Run(() => _apiConnectionEvents.CloseConnection(hubConnection));

        }

        /// <summary>
        /// Get count of all CIARE procesess opened.
        /// </summary>
        /// <returns></returns>
        private int GetCiareProcesses() => Process.GetProcessesByName("CIARE").Count();

        /// <summary>
        /// Check edited opened files by external application when CIARE is on Top Most event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Activated(object sender, EventArgs e)
        {
            try
            {
                FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath, GlobalVariables.savedFileNoMD5Check);
            }
            catch
            {
            }
        }


        #region Hotkeys for Edit Menu
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
            AiManage.LoadProgressBar();
            AiManage.GetDataAI(SelectedEditor.GetSelectedEditor(), GlobalVariables.aiKey.ConvertSecureStringToString());
        }

        private void referenceAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^r");
        }

        #endregion

        /// <summary>
        /// Hide output richtextbox on textEditorControl1 focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Enter(object sender, EventArgs e)
        {
            if (!visibleSplitContainerAutoHide)
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer1);
                visibleSplitContainer = true;
            }
        }

        #region Code Completion settup.

        /// <summary>
        /// Relaod ref in texteditor control.
        /// </summary>
        public void ReloadRef()
        {
            if (GlobalVariables.OCodeCompletion)
            {
                var parserThread = new Thread(ParserThread);
                parserThread.IsBackground = true;
                parserThread.Start();
            }
        }
        private HashSet<string> _alreadyLoaded = new HashSet<string>();
        void ParserThread()
        {
            myProjectContent.AddReferencedContent(pcRegistry.Mscorlib);
            ParseStep();

            Dom.IProjectContent[] total = pcRegistry.LoadAll();

            foreach (var item in total)
            {
                if (_alreadyLoaded.Contains(item.ToString()))
                    continue;

                _alreadyLoaded.Add(item.ToString());

                myProjectContent.AddReferencedContent(item);

                if (myProjectContent is Dom.ReflectionProjectContent myObj) myObj.InitializeReferences();
            }

            while (!IsDisposed)
            {
                ParseStep();
                Thread.Sleep(2000);
            }
        }

        void ParseStep()
        {
            string code = null;
            Invoke(new MethodInvoker(delegate
            {
                code = SelectedEditor.GetSelectedEditor().Text;
            }));
            TextReader textReader = new StringReader(code);
            Dom.ICompilationUnit newCompilationUnit;
            NRefactory.SupportedLanguage supportedLanguage;
            if (IsVisualBasic)
                supportedLanguage = NRefactory.SupportedLanguage.VBNet;
            else
                supportedLanguage = NRefactory.SupportedLanguage.CSharp;
            using (NRefactory.IParser p = NRefactory.ParserFactory.CreateParser(supportedLanguage, textReader))
            {
                p.ParseMethodBodies = false;
                p.Parse();
                newCompilationUnit = ConvertCompilationUnit(p.CompilationUnit);
            }
            myProjectContent.UpdateCompilationUnit(lastCompilationUnit, newCompilationUnit, DummyFileName);
            lastCompilationUnit = newCompilationUnit;
            parseInformation.SetCompilationUnit(newCompilationUnit);
        }

        Dom.ICompilationUnit ConvertCompilationUnit(NRefactory.Ast.CompilationUnit cu)
        {
            Dom.NRefactoryResolver.NRefactoryASTConvertVisitor converter;
            converter = new Dom.NRefactoryResolver.NRefactoryASTConvertVisitor(myProjectContent);
            cu.AcceptVisitor(converter, null);
            return converter.Cu;
        }
        #endregion


        /// <summary>
        /// Form resize event used to store window size in registry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            if (this.WindowState != FormWindowState.Maximized)
            {
                InitializeEditor.SetEditorWindowSize(GlobalVariables.registryPath, this.Width, this.Height);
                InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath, false);
            }
            else
            {
                InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath, true);
            }
        }


        /// <summary>
        /// Mark file for auto start event on Windows reboot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void markStartFileChk_CheckedChanged(object sender, EventArgs e)
        {
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.SetFilePath(markStartFileChk);
            if (GlobalVariables.OWinLoginState)
                autoStartFile.SetRegistryRunApp();
        }

        /// <summary>
        /// Set realtime spliter position controler event to middle on texteditor resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Resize(object sender, EventArgs e)
        {
            SplitEditorWindow.SetSplitWindowSize(SelectedEditor.GetSelectedEditor(), GlobalVariables.splitWindowPosition);
        }

        /// <summary>
        /// Start live share host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void liveShareHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LiveShareHost liveShareHost = new LiveShareHost();
            liveShareHost.ShowDialog();
        }

        /// <summary>
        /// Send data to remote client.
        /// </summary>
        private async void SendData()
        {
            GlobalVariables.codeWriter = false;

            if (!GlobalVariables.connected)
                return;

            await Task.Delay(10);
            await _apiConnectionEvents.SendData(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
        }

        /// <summary>
        /// Manage live hub disconection event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void liveStatusPb_Paint(object sender, PaintEventArgs e)
        {
            if (GlobalVariables.connected && GlobalVariables.liveDisconnected)
            {
                if (GlobalVariables.apiRemoteConnected || GlobalVariables.apiConnected)
                    ApiConnectionEvents.ManageHubDisconnection(hubConnection, new Button());
            }
        }

        /// <summary>
        /// RichtextBox mouse wheel event for store zoomfactor value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outputRBT_MouseWheel(object sender, MouseEventArgs e) => GlobalVariables.zoomFactor = outputRBT.ZoomFactor;

        /// <summary>
        /// Create/close new tab with new editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            TabControllerManage.CloseTab(EditorTabControl, e);
        }

        /// <summary>
        /// Handler for resize tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_HandleCreated(object sender, EventArgs e) =>
            SendMessage(this.EditorTabControl.Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);


        /// <summary>
        /// Autoresize tab names.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            string titleTab = EditorTabControl.SelectedTab.Text.Trim();
            string filePath = EditorTabControl.SelectedTab.ToolTipText.Trim();

            if (isLoaded)
            {
                try
                {
                    GlobalVariables.textAreaFirst = SelectedEditor.GetSelectedEditor().primaryTextArea;
                    GlobalVariables.textAreaSecond = SelectedEditor.GetSelectedEditor().secondaryTextArea;
                    InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, SelectedEditor.GetSelectedEditor());
                }
                catch
                {
                    // Ignore index 0 error. The area will be set after load on first split anyway.
                }
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                GlobalVariables.openedFilePath = filePath;
                var fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                GlobalVariables.openedFileName = fileInfo.Name;
                if (File.Exists(filePath))
                {
                    try
                    {
                        FileManage.SetFileMD5(filePath);
                    }
                    catch { }
                }
            }
            if (!titleTab.Contains("New Pag") && !titleTab.Contains("+"))
            {
                this.Text = $"{titleTab.Trim()} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {GlobalVariables.versionName}";

            }
            else
            {
                this.Text = $"CIARE {GlobalVariables.versionName}";
            }
            var tabCount = this.EditorTabControl.TabCount;
            if (tabCount != _countTabs)
            {
                _countTabs = tabCount;
                dynamicTextEdtior = new TextEditorControl();
                TabPage tabPage = EditorTabControl.TabPages[EditorTabControl.SelectedIndex];
                SetDesignEditor(ref dynamicTextEdtior);
                tabPage.Controls.Add(dynamicTextEdtior);
                Initiliaze(EditorTabControl.SelectedIndex);
            }

            //TODO: Will see in future if is needed
            // FileManage.CheckFileExternalEdited(GlobalVariables.tabsFilePath);

            // Clear line/col position on new tab switch
            ClearInfoLinescs.ClearLinesInfo();
            LinesManage.GetTotalLinesCount(linesCountLbl);
        }

        /// <summary>
        /// Set design for every new editor controler.
        /// </summary>
        /// <param name="dynamicTextEdtior"></param>
        private void SetDesignEditor(ref TextEditorControl dynamicTextEdtior)
        {
            var tabCount = this.EditorTabControl.TabCount;
            var tabIndex = this.EditorTabControl.SelectedIndex;
            dynamicTextEdtior.Name = $"textEditorControl{tabCount}";
            dynamicTextEdtior.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dynamicTextEdtior.Dock = DockStyle.Fill;
            dynamicTextEdtior.BackColor = SystemColors.Window;
            dynamicTextEdtior.BorderStyle = BorderStyle.FixedSingle;
            dynamicTextEdtior.Font = new Font("Consolas", 10F);
            dynamicTextEdtior.Highlighting = null;
            dynamicTextEdtior.Location = new Point(0, 0);
            dynamicTextEdtior.Margin = new Padding(4, 3, 4, 3);
            dynamicTextEdtior.TabIndex = tabIndex;
            dynamicTextEdtior.VRulerRow = 0;
            dynamicTextEdtior.TextChanged += textEditorControl1_TextChanged;
            dynamicTextEdtior.Enter += textEditorControl1_Enter;
            dynamicTextEdtior.Resize += textEditorControl1_Resize;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragDrop += DynamicTextEdtior_DragDrop;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.DragOver += DynamicTextEdtior_DragEnter;
            dynamicTextEdtior.ActiveTextAreaControl.TextArea.AllowDrop = true;
            dynamicTextEdtior.ActiveTextAreaControl.HScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.VScrollBar.Visible = true;
            dynamicTextEdtior.ActiveTextAreaControl.AutoHideScrollbars = true;
            dynamicTextEdtior.ActiveTextAreaControl.TextEditorProperties.AutoInsertCurlyBracket = true;
            dynamicTextEdtior.ActiveTextAreaControl.VerticalScroll.Enabled = true;
            dynamicTextEdtior.ActiveTextAreaControl.HorizontalScroll.Enabled = true;
            dynamicTextEdtior.TextEditorProperties.StoreZoomSize = true;
            dynamicTextEdtior.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            dynamicTextEdtior.Focus();
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
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Open data from drag&drop to new tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTabOnDop(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                foreach (var file in _filesDrag)
                {
                    var isFileOpenedInTab = TabControllerManage.IsFileOpenedInTab(MainForm.Instance.EditorTabControl, file);
                    if (isFileOpenedInTab) return;
                    TabControllerManage.AddNewTab(EditorTabControl);
                    FileManage.OpenFileDragDrop(SelectedEditor.GetSelectedEditor(), file);
                }
            });
        }


        /// <summary>
        /// Draw new tab with X for close after.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw tab on initialize.
            TabControllerManage.DrawTabControl(EditorTabControl, e);

            // Set transparent header bar.
            if (GlobalVariables.isVStheme)
                TabControllerManage.SetTransparentTabBar(EditorTabControl, e, 51, 51, 51);
            else
                TabControllerManage.SetTransparentTabBar(EditorTabControl, e, 0, 1, 10);

            // Color tab to red if live shared started on that index.
            if (GlobalVariables.apiConnected || GlobalVariables.apiRemoteConnected)
                TabControllerManage.ColorTab(EditorTabControl, GlobalVariables.liveTabIndex, e, Color.Red);
            var taBindex = EditorTabControl.SelectedIndex;

            // Color light green if editor data is unsaved.
            TabControllerManage.ColorTab(EditorTabControl, taBindex, e, Color.LightGray);
        }

        /// <summary>
        /// Show the menu strip on right click tab event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = EditorTabControl.PointToClient(Cursor.Position);
                for (int i = 0; i < EditorTabControl.TabCount; i++)
                {
                    Rectangle r = EditorTabControl.GetTabRect(i);
                    if (r.Contains(p))
                    {
                        if (i >= 1)
                        {
                            EditorTabControl.SelectedIndex = i;
                            tabMenu.Show(EditorTabControl, e.Location);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Close event on right click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeTab_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseTabEvent(EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close event on right click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeAllTabs_Click(object sender, EventArgs e)
        {
            TabControllerManage.CloseAllTabs(EditorTabControl, SelectedEditor.GetSelectedEditor());
        }

        /// <summary>
        /// Close all tabs but not selected one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeAllTabsOne_Click(object sender, EventArgs e)
        {
            int index = EditorTabControl.SelectedIndex;
            TabControllerManage.CloseAllTabsOne(EditorTabControl, SelectedEditor.GetSelectedEditor(), index);
        }

        /// <summary>
        /// Cancel left/right and home/end key scroll.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditorTabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.Handled = true;
            if (e.KeyCode == Keys.End || e.KeyCode == Keys.Home)
                e.Handled = true;
        }

        /// <summary>
        /// Refresh top most.
        /// </summary>
        public void RefreshTopMost()
        {
            TopMost = true;
            TopMost = false;
        }
    }
}
