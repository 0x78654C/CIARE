﻿using CIARE.Utils;
using CIARE.GUI;
using ICSharpCode.TextEditor;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NRefactory = ICSharpCode.NRefactory;
using Dom = ICSharpCode.SharpDevelop.Dom;
using System.Threading;
using System.Linq;
using CIARE.Roslyn;
using Microsoft.CodeAnalysis;

namespace CIARE
{
    public partial class Form1 : Form
    {
        public string versionName;
        public long openedFileLength = 0;
        public bool visibleSplitContainer = false;
        public bool visibleSplitContainerAutoHide = false;
        private string _editFontSize = "editorFontSizeZoom";
        public static Form1 Instance { get; private set; }
        internal static Dom.ProjectContentRegistry pcRegistry;
        internal static Dom.DefaultProjectContent myProjectContent;
        internal static Dom.ParseInformation parseInformation = new Dom.ParseInformation();
        public static bool IsVisualBasic = false;
        Dom.ICompilationUnit lastCompilationUnit;
        public const string DummyFileName = "edited.cs";
        static readonly Dom.LanguageProperties CurrentLanguageProperties = IsVisualBasic ? Dom.LanguageProperties.VBNet : Dom.LanguageProperties.CSharp;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Instance = this;
            textEditorControl1.TextEditorProperties.StoreZoomSize = true;
            textEditorControl1.TextEditorProperties.RegPath = GlobalVariables.registryPath;
            versionName = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            versionName = versionName.Substring(0, versionName.Length - 2);
            this.Text = $"CIARE {versionName}";
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1, highlightCMB);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, textEditorControl1);
            InitializeEditor.ReadOutputWindowState(GlobalVariables.registryPath, splitContainer1);
            Console.SetOut(new ControlWriter(outputRBT));
            HostCallbackImplementation.Register(this);
            CodeCompletionKeyHandler.Attach(this, textEditorControl1);
            ToolTipProvider.Attach(this, textEditorControl1);

            pcRegistry = new Dom.ProjectContentRegistry(); // Default .NET 2.0 registry

            // Persistence lets SharpDevelop.Dom create a cache file on disk so that
            // future starts are faster.
            // It also caches XML documentation files in an on-disk hash table, thus
            // reducing memory usage.
            pcRegistry.ActivatePersistence(Path.Combine(Path.GetTempPath(),
                                                        "CSharpCodeCompletion"));

            myProjectContent = new Dom.DefaultProjectContent();
            myProjectContent.Language = CurrentLanguageProperties;
            linesCountLbl.Text = string.Empty;
            linesPositionLbl.Text = string.Empty;
            textEditorControl1.ActiveTextAreaControl.Caret.PositionChanged += LinesManage.GetCaretPositon;
            try
            {
                var args = Environment.GetCommandLineArgs();
                LoadParamFile(args[1], textEditorControl1);
                GlobalVariables.openedFilePath = args[1];
                this.Text = $"CIARE {versionName} | {GlobalVariables.openedFilePath}";
                FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                openedFileLength = fileInfo.Length;
            }
            catch { }
        }



        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
            Roslyn.RoslynRun.RunCode(outputRBT, runCodePb, textEditorControl1, splitContainer1, true);
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
                textEditorControl1.Clear(); textEditorControl.Text = File.ReadAllText(data);
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
                this.Text = $"CIARE {versionName} | *{GlobalVariables.openedFilePath}";
            LinesManage.GetTotalLinesCount(textEditorControl1, linesCountLbl);
            textEditorControl1.Document.FoldingManager.FoldingStrategy = new FoldingStrategy();
            textEditorControl1.Document.FoldingManager.UpdateFoldings(null, null);
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
                    FileManage.NewFile(textEditorControl1);
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
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion


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
            if (highlightCMB.Text == "C#-Dark")
            {
                GlobalVariables.darkColor = true;
                DarkMode.SetDarkModeMain(this, outputRBT, groupBox1, label1, label2, label3, highlightLbl,
                    highlightCMB, menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
                return;
            }
            GlobalVariables.darkColor = false;
            LightMode.SetLightModeMain(this, outputRBT, groupBox1, highlightLbl,
                highlightCMB, menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
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
            Roslyn.RoslynRun.CompileBinaryExe(textEditorControl1, splitContainer1, outputRBT, false);
        }

        /// <summary>
        /// Compile code to DLL binary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compileToDLLCtrlSfitBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Roslyn.RoslynRun.CompileBinaryDll(textEditorControl1, splitContainer1, outputRBT, false);
        }

        /// <summary>
        /// Run the method for unsaved data check on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            FileManage.ManageUnsavedData(textEditorControl1);
            if (GlobalVariables.noClear)
                e.Cancel = true;
            else
                e.Cancel = false;
        }



        /// <summary>
        /// Check edited opened files by external application when CIARE is on Top Most event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Activated(object sender, EventArgs e)
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
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            parserThread = new Thread(ParserThread);
            parserThread.IsBackground = true;
            parserThread.Start();
        }
        Thread parserThread;
        void ParserThread()
        {
            myProjectContent.AddReferencedContent(pcRegistry.Mscorlib);

            // do one initial parser step to enable code-completion while other
            // references are loading
            ParseStep();
            //            string assemblyName1 = Path.GetRandomFileName();
            //            string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            //            var references = Directory.GetFiles(assemblyPath).Where(t => t.EndsWith(".dll"))
            //.Where(t => ManageCheck.IsManaged(t));
            var total = pcRegistry.LoadAll();
            foreach (var item in total)
            {
                myProjectContent.AddReferencedContent(item);
                if (myProjectContent is Dom.ReflectionProjectContent)
                {
                    (myProjectContent as Dom.ReflectionProjectContent).InitializeReferences();
                }
            }


            if (IsVisualBasic)
            {
                myProjectContent.DefaultImports = new Dom.DefaultUsing(myProjectContent);
                myProjectContent.DefaultImports.Usings.Add("System");
                myProjectContent.DefaultImports.Usings.Add("System.Text");
                myProjectContent.DefaultImports.Usings.Add("Microsoft.VisualBasic");
            }

            // Parse the current file every 2 seconds
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
                // we only need to parse types and method definitions, no method bodies
                // so speed up the parser and make it more resistent to syntax
                // errors in methods
                p.ParseMethodBodies = false;

                p.Parse();
                newCompilationUnit = ConvertCompilationUnit(p.CompilationUnit);
            }
            // Remove information from lastCompilationUnit and add information from newCompilationUnit.
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
    }
}
