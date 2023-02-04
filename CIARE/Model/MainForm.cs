using CIARE.Utils;
using CIARE.GUI;
using CIARE.Utils.Options;
using ICSharpCode.TextEditor;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NRefactory = ICSharpCode.NRefactory;
using Dom = ICSharpCode.SharpDevelop.Dom;
using System.Threading;
using CIARE.Roslyn;
using System.Drawing;
using CIARE.Utils.FilesOpenOS;
using System.Runtime.Versioning;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using CIARE.LiveShareManage;
using System.Threading.Tasks;
using CIARE.Utils.OpenAISettings;
using Button = System.Windows.Forms.Button;
using CIARE.Reference;
using Microsoft.Win32;
using System.Collections.Generic;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class MainForm : Form
    {
        public HubConnection hubConnection;
        public string versionName;
        public long openedFileLength = 0;
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

        public MainForm()
        {
            InitializeEditor.CreateUserDataDirectory(GlobalVariables.userProfileDirectory, GlobalVariables.markFile);
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, "highlight", "C#-Dark");
            var autoStartFile = new AutoStartFile("", GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.CheckSetAtiveFormState();
            autoStartFile.OpenFilesOnLongOn(ReadArgs(s_args));
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Instance = this;
            textEditorControl1.TextEditorProperties.StoreZoomSize = true;
            textEditorControl1.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            versionName = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            versionName = versionName.Substring(0, versionName.Length - 2);
            this.Text = $"CIARE {versionName}";
            InitializeEditor.ReadEditorWindowSize(this, GlobalVariables.registryPath);
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, textEditorControl1);
            InitializeEditor.ReadOutputWindowState(GlobalVariables.registryPath, splitContainer1);
            InitializeEditor.WinLoginState(GlobalVariables.registryPath, GlobalVariables.OWinLogin, out GlobalVariables.OWinLoginState);
            InitializeEditor.GenerateLiveSessionId();
            Console.SetOut(new ControlWriter(outputRBT));
            FoldingCode.CheckFoldingCodeStatus(GlobalVariables.registryPath);
            CodeCompletion.CheckCodeCompletion(GlobalVariables.registryPath);
            LineNumber.CheckLineNumberStatus(GlobalVariables.registryPath);
            Warnings.CheckWarnings(GlobalVariables.registryPath);
            StartFilesOS.CheckOSStartFile(GlobalVariables.registryPath);
            BuildConfig.CheckConfig(GlobalVariables.registryPath);
            BuildConfig.CheckPlatform(GlobalVariables.registryPath);
            TargetFramework.CheckFramework(GlobalVariables.registryPath);
            LiveShare.CheckApiLiveShare(GlobalVariables.registryPath);
            OpenAISetting.CheckOpenAIData(GlobalVariables.registryPath);
            _apiConnectionEvents = new ApiConnectionEvents();
            //------------------------------

            //Code completion initialize.
            if (GlobalVariables.OCodeCompletion)
            {
                HostCallbackImplementation.Register(this);
                CodeCompletionKeyHandler.Attach(this, textEditorControl1);
                ToolTipProvider.Attach(this, textEditorControl1);

                pcRegistry = new Dom.ProjectContentRegistry();
                if (!Directory.Exists((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"))))
                    Directory.CreateDirectory((Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion")));

                pcRegistry.ActivatePersistence(Path.Combine(Path.GetTempPath(), "CSharpCodeCompletion"));
            }
            //-------------------------------
            myProjectContent = new Dom.DefaultProjectContent();
            myProjectContent.Language = CurrentLanguageProperties;
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            textEditorControl1.ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;

            //File open via parameters(Open with option..)
            try
            {
                string arg = ReadArgs(s_args);
                LoadParamFile(arg, textEditorControl1);
                if (!GlobalVariables.noPath)
                {
                    GlobalVariables.openedFilePath = arg;
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    GlobalVariables.openedFileName = fileInfo.Name;
                    if (arg.Length > 1)
                        this.Text = $"{fileInfo.Name} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {versionName}";
                    openedFileLength = fileInfo.Length;
                    var autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
                    autoStartFile.CheckFilePath();
                }
            }
            catch { }
            //----------------------------------
            ReloadRef();
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
            RoslynRun.RunCode(outputRBT, runCodePb, textEditorControl1, splitContainer1, true);
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
            FileManage.OpenFileDialog(textEditorControl1);
        }

        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.SaveToFileDialog(textEditorControl1);
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
            {
                textEditorControl1.Clear();
                textEditorControl.Text = File.ReadAllText(data);
            }
        }

        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            FileManage.SaveAsDialog(textEditorControl1);
        }

        /// <summary>
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (GlobalVariables.openedFilePath.Length > 0)
                this.Text = $"*{GlobalVariables.openedFileName} : {FileManage.GetFilePath(GlobalVariables.openedFilePath)} - CIARE {versionName}";
            LinesManage.GetTotalLinesCount(textEditorControl1, linesCountLbl);
            textEditorControl1.Document.FoldingManager.FoldingStrategy = new FoldingStrategy();
            textEditorControl1.Document.FoldingManager.UpdateFoldings(null, null);

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
            textEditorControl1.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            this.Text = $"CIARE {versionName}";
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
                case Keys.N | Keys.Control:
                    FileManage.NewFile(textEditorControl1, outputRBT);
                    return true;
                case Keys.H | Keys.Control:
                    GlobalVariables.findTabOpen = false;
                    FindAndReplace findAndReplace = new FindAndReplace();
                    findAndReplace.Show();
                    return true;
                case Keys.S | Keys.Control:
                    FileManage.SaveToFileDialog(textEditorControl1);
                    return true;
                case Keys.S | Keys.Control | Keys.Shift:
                    FileManage.SaveAsDialog(textEditorControl1);
                    return true;
                case Keys.O | Keys.Control:
                    FileManage.OpenFileDialog(textEditorControl1);
                    return true;
                case Keys.F | Keys.Control:
                    GlobalVariables.findTabOpen = true;
                    FindAndReplace find = new FindAndReplace();
                    find.ShowDialog();
                    return true;
                case Keys.F5:
                    Roslyn.RoslynRun.RunCode(outputRBT, runCodePb, textEditorControl1, splitContainer1, true);
                    return true;
                case Keys.T | Keys.Control:
                    FileManage.LoadCSTemplate(textEditorControl1);
                    return true;
                case Keys.B | Keys.Control:
                    Roslyn.RoslynRun.CompileBinaryExe(textEditorControl1, splitContainer1, outputRBT, false);
                    return true;
                case Keys.B | Keys.Control | Keys.Shift:
                    Roslyn.RoslynRun.CompileBinaryDll(textEditorControl1, splitContainer1, outputRBT, false);
                    return true;
                case Keys.W | Keys.Control:
                    SplitEditorWindow.SplitWindow(textEditorControl1, true);
                    return true;
                case Keys.W | Keys.Control | Keys.Shift:
                    SplitEditorWindow.SplitWindow(textEditorControl1, false);
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
                    AiManage.GetDataAI(textEditorControl1, GlobalVariables.aiKey);
                    return true;
                // Test hotkey for load custom reference.
                case Keys.I | Keys.Control:
                    CustomRef.SetCustomRefDirective(textEditorControl1, outputRBT);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion


        public void SetHighLighter(string highlight)
        {
            if (highlight.Length > 0)
            {
                textEditorControl1.SetHighlighting(highlight);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlight);
            }
            if (highlight == "C#-Dark")
            {
                GlobalVariables.darkColor = true;
                ICSharpCode.TextEditor.Gui.CompletionWindow.CodeCompletionListView.darkMode = true;
                DarkModeMain.SetDarkModeMain(this, outputRBT, groupBox1, label2, label3,
                    menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
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
            FileManage.LoadCSTemplate(textEditorControl1);
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
        /// Compile code to binary exe file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToexeCtrlShiftBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RoslynRun.CompileBinaryExe(textEditorControl1, splitContainer1, outputRBT, false);
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RoslynRun.CompileBinaryDll(textEditorControl1, splitContainer1, outputRBT, false);
        }

        /// <summary>
        /// Run the method for unsaved data check on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FileManage.ManageUnsavedData(textEditorControl1);
            if (GlobalVariables.noClear)
                e.Cancel = true;
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
            FileManage.CheckFileExternalEdited(GlobalVariables.openedFilePath, openedFileLength, textEditorControl1);
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
            SendKeys.Send("^w");
        }

        private void splitVEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SplitEditorWindow.SplitWindow(textEditorControl1, false);
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
            AiManage.GetDataAI(textEditorControl1, GlobalVariables.aiKey);
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

                if (myProjectContent is Dom.ReflectionProjectContent)
                {
                    (myProjectContent as Dom.ReflectionProjectContent).InitializeReferences();
                }
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
                code = textEditorControl1.Text;
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
        /// Events on text change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outputRBT_TextChanged(object sender, EventArgs e)
        {
            // Color warning messages.
            if (outputRBT.Text.Contains("warning"))
                RichExtColor.MarkWordRtb(outputRBT, "warning", Color.Orange);

            // Color error messages.
            if (outputRBT.Text.Contains("error"))
                RichExtColor.MarkWordRtb(outputRBT, "error", Color.Red);
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
            SplitEditorWindow.SetSplitWindowSize(textEditorControl1, GlobalVariables.splitWindowPosition);
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
            await _apiConnectionEvents.SendData(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId, textEditorControl1);
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
    }
}
