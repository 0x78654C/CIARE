
using System.Drawing.Text;
using System.Windows.Forms;

namespace CIARE
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            outputRBT = new RichTextBox();
            toolTip1 = new ToolTip(components);
            runCodePb = new PictureBox();
            menuStrip1 = new MenuStrip();
            fIleToolStripMenuItem = new ToolStripMenuItem();
            newFileStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            LoadCStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            redoToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            cutStripMenuItem = new ToolStripMenuItem();
            copyStripMenuItem = new ToolStripMenuItem();
            pasteStripMenuItem = new ToolStripMenuItem();
            deleteStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            finStripMenuItem = new ToolStripMenuItem();
            replaceStripMenuItem = new ToolStripMenuItem();
            goToLineStripMenuItem = new ToolStripMenuItem();
            selectAllStripMenuItem3 = new ToolStripMenuItem();
            toolStripSeparator6 = new ToolStripSeparator();
            chatGPTCTRLShiftPToolStripMenuItem = new ToolStripMenuItem();
            compileToolStripMenuItem = new ToolStripMenuItem();
            compileToexeCtrlShiftBToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator8 = new ToolStripSeparator();
            compileToDLLCtrlSfitBToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            cmdLinesArgsStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator7 = new ToolStripSeparator();
            referenceAddToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            splitEditorToolStripMenuItem = new ToolStripMenuItem();
            splitVEditorToolStripMenuItem = new ToolStripMenuItem();
            compileStripSeparator1 = new ToolStripSeparator();
            showHideHSCToolStripMenuItem = new ToolStripMenuItem();
            viewSeparator1 = new ToolStripSeparator();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            liveToolStripMenuItem = new ToolStripMenuItem();
            liveShareToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            hotKeyToolStripMenuItem = new ToolStripMenuItem();
            groupBox1 = new GroupBox();
            splitContainer1 = new SplitContainer();
            EditorTabControl = new CIARE.GUI.DarkTabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            label2 = new Label();
            label3 = new Label();
            linesCountLbl = new Label();
            linesPositionLbl = new Label();
            typeCheckLbl = new Label();
            warningsCheckLbl = new Label();
            imageList1 = new ImageList(components);
            markStartFileChk = new CheckBox();
            liveStatusPb = new PictureBox();
            tabMenu = new ContextMenuStrip(components);
            closeTab = new ToolStripMenuItem();
            closeAllTabs = new ToolStripMenuItem();
            closeAllTabsOne = new ToolStripMenuItem();
            errorsContextMenu = new ContextMenuStrip(components);
            copyErrorMenuItem = new ToolStripMenuItem();
            askAiErrorMenuItem = new ToolStripMenuItem();
            outputTabControl = new CIARE.GUI.DarkTabControl();
            outputTabPage = new TabPage();
            errorsTabPage = new TabPage();
            errorsRTB = new RichTextBox();
            progressBar = new ProgressBar();
            ((System.ComponentModel.ISupportInitialize)runCodePb).BeginInit();
            menuStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            outputTabControl.SuspendLayout();
            outputTabPage.SuspendLayout();
            errorsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            EditorTabControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)liveStatusPb).BeginInit();
            tabMenu.SuspendLayout();
            errorsContextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // outputTabPage
            // 
            outputTabPage.Controls.Add(outputRBT);
            outputTabPage.Name = "outputTabPage";
            outputTabPage.Text = "Output";
            outputTabPage.Size = new System.Drawing.Size(88, 50);
            outputTabPage.UseVisualStyleBackColor = true;
            // 
            // outputRBT
            // 
            outputRBT.BackColor = System.Drawing.SystemColors.Window;
            outputRBT.BorderStyle = BorderStyle.None;
            outputRBT.Dock = DockStyle.Fill;
            outputRBT.Font = new System.Drawing.Font("Consolas", 11.25F);
            outputRBT.ForeColor = System.Drawing.SystemColors.MenuText;
            outputRBT.Margin = new Padding(4, 3, 4, 3);
            outputRBT.Name = "outputRBT";
            outputRBT.ReadOnly = true;
            outputRBT.ScrollBars = RichTextBoxScrollBars.Vertical;
            outputRBT.TabIndex = 3;
            outputRBT.Text = "";
            outputRBT.MouseWheel += outputRBT_MouseWheel;
            // 
            // errorsTabPage
            // 
            errorsTabPage.Controls.Add(errorsRTB);
            errorsTabPage.Name = "errorsTabPage";
            errorsTabPage.Text = "Errors";
            errorsRTB.Size = new System.Drawing.Size(88, 50);
            errorsTabPage.UseVisualStyleBackColor = true;
            // 
            // errorsRTB
            // 
            errorsRTB.BackColor = System.Drawing.SystemColors.Window;
            errorsRTB.BorderStyle = BorderStyle.None;
            errorsRTB.Dock = DockStyle.Fill;
            errorsRTB.Font = new System.Drawing.Font("Consolas", 10.5F);
            errorsRTB.ForeColor = System.Drawing.SystemColors.MenuText;
            errorsRTB.Name = "errorsRTB";
            errorsRTB.ReadOnly = true;
            errorsRTB.ScrollBars = RichTextBoxScrollBars.Vertical;
            errorsRTB.TabIndex = 0;
            errorsRTB.Text = "";
            errorsRTB.ContextMenuStrip = errorsContextMenu;
            errorsRTB.MouseDown += errorsRTB_MouseDown;
            errorsRTB.MouseDoubleClick += errorsRTB_MouseDoubleClick;
            // 
            // outputTabControl
            // 
            outputTabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            outputTabControl.Controls.Add(outputTabPage);
            outputTabControl.Controls.Add(errorsTabPage);
            outputTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            outputTabControl.ItemSize = new System.Drawing.Size(130, 22);
            outputTabControl.Location = new System.Drawing.Point(4, 14);
            outputTabControl.Name = "outputTabControl";
            outputTabControl.Size = new System.Drawing.Size(1394, 163);
            outputTabControl.TabIndex = 3;
            outputTabControl.DrawItem += OutputTabControl_DrawItem;
            // 
            // toolTip1
            // 
            toolTip1.Tag = "Run Code";
            // 
            // runCodePb
            // 
            runCodePb.Image = Properties.Resources.runButton21;
            runCodePb.Location = new System.Drawing.Point(365, 4);
            runCodePb.Margin = new Padding(4, 3, 4, 3);
            runCodePb.Name = "runCodePb";
            runCodePb.Size = new System.Drawing.Size(33, 24);
            runCodePb.TabIndex = 2;
            runCodePb.TabStop = false;
            toolTip1.SetToolTip(runCodePb, "Run code ( F5 )");
            runCodePb.Click += runCodePb_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = System.Drawing.SystemColors.Window;
            menuStrip1.Items.AddRange(new ToolStripItem[] { fIleToolStripMenuItem, editToolStripMenuItem, compileToolStripMenuItem, viewToolStripMenuItem, liveToolStripMenuItem, settingsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(7, 2, 0, 2);
            menuStrip1.Size = new System.Drawing.Size(1410, 24);
            menuStrip1.TabIndex = 4;
            menuStrip1.Text = "menuStrip1";
            // 
            // fIleToolStripMenuItem
            // 
            fIleToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newFileStripMenuItem, openToolStripMenuItem, saveToolStripMenuItem, saveAsStripMenuItem, toolStripSeparator1, LoadCStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            fIleToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fIleToolStripMenuItem.Text = "File";
            // 
            // newFileStripMenuItem
            // 
            newFileStripMenuItem.Name = "newFileStripMenuItem";
            newFileStripMenuItem.Size = new System.Drawing.Size(273, 22);
            newFileStripMenuItem.Text = "New         ( CTRL + N )";
            newFileStripMenuItem.Click += toolStripMenuItem1_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            openToolStripMenuItem.Text = "Open       ( CTRL + O )";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            saveToolStripMenuItem.Text = "Save         ( CTRL + S ) ";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsStripMenuItem
            // 
            saveAsStripMenuItem.Name = "saveAsStripMenuItem";
            saveAsStripMenuItem.Size = new System.Drawing.Size(273, 22);
            saveAsStripMenuItem.Text = "Save As    ( CTRL+Shift+S )";
            saveAsStripMenuItem.Click += saveAsStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(270, 6);
            // 
            // LoadCStripMenuItem
            // 
            LoadCStripMenuItem.Name = "LoadCStripMenuItem";
            LoadCStripMenuItem.Size = new System.Drawing.Size(273, 22);
            LoadCStripMenuItem.Text = "Load C# Code Template    ( CTRL + T )";
            LoadCStripMenuItem.Click += LoadCStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(270, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.ForeColor = System.Drawing.Color.Red;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoToolStripMenuItem, redoToolStripMenuItem, toolStripSeparator3, cutStripMenuItem, copyStripMenuItem, pasteStripMenuItem, deleteStripMenuItem, toolStripSeparator4, finStripMenuItem, replaceStripMenuItem, goToLineStripMenuItem, selectAllStripMenuItem3, toolStripSeparator6, chatGPTCTRLShiftPToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.ShowShortcutKeys = false;
            editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            undoToolStripMenuItem.Text = "Undo        ( CTRL + Z )";
            undoToolStripMenuItem.Click += undoToolStripMenuItem_Click;
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            redoToolStripMenuItem.Text = "Redo         ( CTRL + Y )";
            redoToolStripMenuItem.Click += redoToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            toolStripSeparator3.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(226, 6);
            // 
            // cutStripMenuItem
            // 
            cutStripMenuItem.Name = "cutStripMenuItem";
            cutStripMenuItem.Size = new System.Drawing.Size(229, 22);
            cutStripMenuItem.Text = "Cut           ( CTRL + X )";
            cutStripMenuItem.Click += cutStripMenuItem_Click;
            // 
            // copyStripMenuItem
            // 
            copyStripMenuItem.Name = "copyStripMenuItem";
            copyStripMenuItem.Size = new System.Drawing.Size(229, 22);
            copyStripMenuItem.Text = "Copy        ( CTRL + C )";
            copyStripMenuItem.Click += copyStripMenuItem_Click;
            // 
            // pasteStripMenuItem
            // 
            pasteStripMenuItem.Name = "pasteStripMenuItem";
            pasteStripMenuItem.Size = new System.Drawing.Size(229, 22);
            pasteStripMenuItem.Text = "Paste        ( CTRL + V )";
            pasteStripMenuItem.Click += pasteStripMenuItem_Click;
            // 
            // deleteStripMenuItem
            // 
            deleteStripMenuItem.Name = "deleteStripMenuItem";
            deleteStripMenuItem.Size = new System.Drawing.Size(229, 22);
            deleteStripMenuItem.Text = "Delete                     Del";
            deleteStripMenuItem.Click += deleteStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size(226, 6);
            // 
            // finStripMenuItem
            // 
            finStripMenuItem.Name = "finStripMenuItem";
            finStripMenuItem.Size = new System.Drawing.Size(229, 22);
            finStripMenuItem.Text = "Find          ( CTRL + F  )";
            finStripMenuItem.Click += finStripMenuItem_Click;
            // 
            // replaceStripMenuItem
            // 
            replaceStripMenuItem.Name = "replaceStripMenuItem";
            replaceStripMenuItem.Size = new System.Drawing.Size(229, 22);
            replaceStripMenuItem.Text = "Replace    ( CTRL + H )";
            replaceStripMenuItem.Click += replaceStripMenuItem_Click;
            // 
            // goToLineStripMenuItem
            // 
            goToLineStripMenuItem.Name = "goToLineStripMenuItem";
            goToLineStripMenuItem.Size = new System.Drawing.Size(229, 22);
            goToLineStripMenuItem.Text = "Go To ..     ( CTRL + G )";
            goToLineStripMenuItem.Click += goToLineStripMenuItem_Click;
            // 
            // selectAllStripMenuItem3
            // 
            selectAllStripMenuItem3.Name = "selectAllStripMenuItem3";
            selectAllStripMenuItem3.Size = new System.Drawing.Size(229, 22);
            selectAllStripMenuItem3.Text = "Select All  ( CTRL + A )";
            selectAllStripMenuItem3.Click += selectAllStripMenuItem3_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new System.Drawing.Size(226, 6);
            // 
            // chatGPTCTRLShiftPToolStripMenuItem
            // 
            chatGPTCTRLShiftPToolStripMenuItem.Name = "chatGPTCTRLShiftPToolStripMenuItem";
            chatGPTCTRLShiftPToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            chatGPTCTRLShiftPToolStripMenuItem.Text = "Ask AI       ( CTRL + Shift + P )";
            chatGPTCTRLShiftPToolStripMenuItem.Click += chatGPTCTRLShiftPToolStripMenuItem_Click;
            // 
            // compileToolStripMenuItem
            // 
            compileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { compileToexeCtrlShiftBToolStripMenuItem, toolStripSeparator8, compileToDLLCtrlSfitBToolStripMenuItem, toolStripSeparator5, cmdLinesArgsStripMenuItem, toolStripSeparator7, referenceAddToolStripMenuItem });
            compileToolStripMenuItem.Name = "compileToolStripMenuItem";
            compileToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            compileToolStripMenuItem.Text = "Compile";
            // 
            // compileToexeCtrlShiftBToolStripMenuItem
            // 
            compileToexeCtrlShiftBToolStripMenuItem.Name = "compileToexeCtrlShiftBToolStripMenuItem";
            compileToexeCtrlShiftBToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            compileToexeCtrlShiftBToolStripMenuItem.Text = "Compile                          ( Ctrl + B )";
            compileToexeCtrlShiftBToolStripMenuItem.Click += compileToexeCtrlShiftBToolStripMenuItem_Click;
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new System.Drawing.Size(250, 6);
            // 
            // compileToDLLCtrlSfitBToolStripMenuItem
            // 
            compileToDLLCtrlSfitBToolStripMenuItem.Name = "compileToDLLCtrlSfitBToolStripMenuItem";
            compileToDLLCtrlSfitBToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            compileToDLLCtrlSfitBToolStripMenuItem.Text = "Publish               ( Ctrl + Shift + B )";
            compileToDLLCtrlSfitBToolStripMenuItem.Click += compileToDLLCtrlSfitBToolStripMenuItem_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new System.Drawing.Size(250, 6);
            // 
            // cmdLinesArgsStripMenuItem
            // 
            cmdLinesArgsStripMenuItem.Name = "cmdLinesArgsStripMenuItem";
            cmdLinesArgsStripMenuItem.Size = new System.Drawing.Size(253, 22);
            cmdLinesArgsStripMenuItem.Text = "Command Line Args...  ( Ctrl + L )";
            cmdLinesArgsStripMenuItem.Click += cmdLinesArgsStripMenuItem_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new System.Drawing.Size(250, 6);
            // 
            // referenceAddToolStripMenuItem
            // 
            referenceAddToolStripMenuItem.Name = "referenceAddToolStripMenuItem";
            referenceAddToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            referenceAddToolStripMenuItem.Text = "Add Reference                ( Ctrl + R )";
            referenceAddToolStripMenuItem.Click += referenceAddToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { splitEditorToolStripMenuItem, splitVEditorToolStripMenuItem, compileStripSeparator1, showHideHSCToolStripMenuItem, viewSeparator1, fullScreenToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // splitEditorToolStripMenuItem
            // 
            splitEditorToolStripMenuItem.Name = "splitEditorToolStripMenuItem";
            splitEditorToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            splitEditorToolStripMenuItem.Text = "Split Editor  V                  ( CTRL + W )";
            splitEditorToolStripMenuItem.Click += splitEditorToolStripMenuItem_Click;
            // 
            // splitVEditorToolStripMenuItem
            // 
            splitVEditorToolStripMenuItem.Name = "splitVEditorToolStripMenuItem";
            splitVEditorToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            splitVEditorToolStripMenuItem.Text = "Split Editor  H     ( CTRL + Shfit + W )";
            splitVEditorToolStripMenuItem.Click += splitVEditorToolStripMenuItem_Click;
            // 
            // compileStripSeparator1
            // 
            compileStripSeparator1.Name = "compileStripSeparator1";
            compileStripSeparator1.Size = new System.Drawing.Size(263, 6);
            // 
            // showHideHSCToolStripMenuItem
            // 
            showHideHSCToolStripMenuItem.Name = "showHideHSCToolStripMenuItem";
            showHideHSCToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            showHideHSCToolStripMenuItem.Text = "Show/Hide Output          ( CTRL + K )";
            showHideHSCToolStripMenuItem.Click += showHideSCToolStripMenuItem_Click;
            // 
            // viewSeparator1
            // 
            viewSeparator1.Name = "viewSeparator1";
            viewSeparator1.Size = new System.Drawing.Size(263, 6);
            // 
            // fullScreenToolStripMenuItem
            // 
            fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            fullScreenToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            fullScreenToolStripMenuItem.Text = "Full Screen                    ( F11 )";
            fullScreenToolStripMenuItem.Click += fullScreenToolStripMenuItem_Click;
            // 
            // liveToolStripMenuItem
            // 
            liveToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { liveShareToolStripMenuItem });
            liveToolStripMenuItem.Name = "liveToolStripMenuItem";
            liveToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            liveToolStripMenuItem.Text = "Live";
            // 
            // liveShareToolStripMenuItem
            // 
            liveShareToolStripMenuItem.Name = "liveShareToolStripMenuItem";
            liveShareToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            liveShareToolStripMenuItem.Text = "Live Share Manage ( CTRL + Q )";
            liveShareToolStripMenuItem.Click += liveShareHostToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { optionsToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            optionsToolStripMenuItem.Text = "Options";
            optionsToolStripMenuItem.Click += optionsToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem, hotKeyToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // hotKeyToolStripMenuItem
            // 
            hotKeyToolStripMenuItem.Name = "hotKeyToolStripMenuItem";
            hotKeyToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            hotKeyToolStripMenuItem.Text = "HotKeys";
            hotKeyToolStripMenuItem.Click += HotKeyToolStripMenuItem_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(outputTabControl);
            groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            groupBox1.Location = new System.Drawing.Point(4, 3);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(1402, 181);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 24);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(EditorTabControl);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(groupBox1);
            splitContainer1.Size = new System.Drawing.Size(1410, 838);
            splitContainer1.SplitterDistance = 646;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 6;
            // 
            // EditorTabControl
            // 
            EditorTabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            EditorTabControl.Controls.Add(tabPage1);
            EditorTabControl.Controls.Add(tabPage2);
            EditorTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            EditorTabControl.ItemSize = new System.Drawing.Size(130, 20);
            EditorTabControl.Location = new System.Drawing.Point(3, 3);
            EditorTabControl.Name = "EditorTabControl";
            EditorTabControl.SelectedIndex = 0;
            EditorTabControl.ShowToolTips = true;
            EditorTabControl.Size = new System.Drawing.Size(1403, 640);
            EditorTabControl.TabIndex = 1;
            EditorTabControl.DrawItem += EditorTabControl_DrawItem;
            EditorTabControl.Selecting += EditorTabControl_Selecting;
            EditorTabControl.SelectedIndexChanged += EditorTabControl_SelectedIndexChanged;
            EditorTabControl.HandleCreated += EditorTabControl_HandleCreated;
            EditorTabControl.KeyDown += EditorTabControl_KeyDown;
            EditorTabControl.MouseClick += EditorTabControl_MouseClick;
            EditorTabControl.MouseDown += EditorTabControl_MouseDown;
            // 
            // tabPage1
            // 
            tabPage1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            tabPage1.Location = new System.Drawing.Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new System.Drawing.Size(1395, 612);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "   +        ";
            tabPage1.ToolTipText = "Add Tab (CTRL + Tab)";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Location = new System.Drawing.Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Size = new System.Drawing.Size(1395, 612);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "New Page             ";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F);
            label2.Location = new System.Drawing.Point(343, 2);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(13, 18);
            label2.TabIndex = 10;
            label2.Text = "|";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F);
            label3.Location = new System.Drawing.Point(395, 2);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(13, 18);
            label3.TabIndex = 11;
            label3.Text = "|";
            // 
            // linesCountLbl
            // 
            linesCountLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            linesCountLbl.AutoSize = true;
            linesCountLbl.Location = new System.Drawing.Point(1219, 6);
            linesCountLbl.Margin = new Padding(4, 0, 4, 0);
            linesCountLbl.Name = "linesCountLbl";
            linesCountLbl.Size = new System.Drawing.Size(64, 15);
            linesCountLbl.TabIndex = 14;
            linesCountLbl.Text = "linesCount";
            // 
            // linesPositionLbl
            // 
            linesPositionLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            linesPositionLbl.AutoSize = true;
            linesPositionLbl.Location = new System.Drawing.Point(1036, 6);
            linesPositionLbl.Margin = new Padding(4, 0, 4, 0);
            linesPositionLbl.Name = "linesPositionLbl";
            linesPositionLbl.Size = new System.Drawing.Size(74, 15);
            linesPositionLbl.TabIndex = 15;
            linesPositionLbl.Text = "linesPosition";
            // 
            // typeCheckLbl
            // 
            typeCheckLbl.AutoSize = true;
            typeCheckLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            typeCheckLbl.Location = new System.Drawing.Point(600, 5);
            typeCheckLbl.Margin = new Padding(4, 0, 4, 0);
            typeCheckLbl.Name = "typeCheckLbl";
            typeCheckLbl.Size = new System.Drawing.Size(0, 15);
            typeCheckLbl.TabIndex = 18;
            typeCheckLbl.Text = "";
            // 
            // warningsCheckLbl
            // 
            warningsCheckLbl.AutoSize = true;
            warningsCheckLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            warningsCheckLbl.ForeColor = System.Drawing.Color.Orange;
            warningsCheckLbl.Location = new System.Drawing.Point(800, 5);
            warningsCheckLbl.Margin = new Padding(4, 0, 4, 0);
            warningsCheckLbl.Name = "warningsCheckLbl";
            warningsCheckLbl.Size = new System.Drawing.Size(0, 15);
            warningsCheckLbl.TabIndex = 19;
            warningsCheckLbl.Text = "";
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth8Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = System.Drawing.Color.Transparent;
            imageList1.Images.SetKeyName(0, "Icons.16x16.Class.png");
            imageList1.Images.SetKeyName(1, "Icons.16x16.Delegate.png");
            imageList1.Images.SetKeyName(2, "Icons.16x16.Enum.png");
            imageList1.Images.SetKeyName(3, "Icons.16x16.Event.png");
            imageList1.Images.SetKeyName(4, "Icons.16x16.ExtensionMethod.png");
            imageList1.Images.SetKeyName(5, "Icons.16x16.Field.png");
            imageList1.Images.SetKeyName(6, "Icons.16x16.Indexer.png");
            imageList1.Images.SetKeyName(7, "Icons.16x16.Interface.png");
            imageList1.Images.SetKeyName(8, "Icons.16x16.InternalClass.png");
            imageList1.Images.SetKeyName(9, "Icons.16x16.InternalDelegate.png");
            imageList1.Images.SetKeyName(10, "Icons.16x16.InternalEnum.png");
            imageList1.Images.SetKeyName(11, "Icons.16x16.InternalEvent.png");
            imageList1.Images.SetKeyName(12, "Icons.16x16.InternalExtensionMethod.png");
            imageList1.Images.SetKeyName(13, "Icons.16x16.InternalField.png");
            imageList1.Images.SetKeyName(14, "Icons.16x16.InternalIndexer.png");
            imageList1.Images.SetKeyName(15, "Icons.16x16.InternalInterface.png");
            imageList1.Images.SetKeyName(16, "Icons.16x16.InternalMethod.png");
            imageList1.Images.SetKeyName(17, "Icons.16x16.InternalProperty.png");
            imageList1.Images.SetKeyName(18, "Icons.16x16.InternalStruct.png");
            imageList1.Images.SetKeyName(19, "Icons.16x16.Keyword.png");
            imageList1.Images.SetKeyName(20, "Icons.16x16.Literal.png");
            imageList1.Images.SetKeyName(21, "Icons.16x16.Local.png");
            imageList1.Images.SetKeyName(22, "Icons.16x16.Method.png");
            imageList1.Images.SetKeyName(23, "Icons.16x16.NameSpace.png");
            imageList1.Images.SetKeyName(24, "Icons.16x16.Operator.png");
            imageList1.Images.SetKeyName(25, "Icons.16x16.Parameter.png");
            imageList1.Images.SetKeyName(26, "Icons.16x16.PrivateClass.png");
            imageList1.Images.SetKeyName(27, "Icons.16x16.PrivateDelegate.png");
            imageList1.Images.SetKeyName(28, "Icons.16x16.PrivateEnum.png");
            imageList1.Images.SetKeyName(29, "Icons.16x16.PrivateEvent.png");
            imageList1.Images.SetKeyName(30, "Icons.16x16.PrivateExtensionMethod.png");
            imageList1.Images.SetKeyName(31, "Icons.16x16.PrivateField.png");
            imageList1.Images.SetKeyName(32, "Icons.16x16.PrivateIndexer.png");
            imageList1.Images.SetKeyName(33, "Icons.16x16.PrivateInterface.png");
            imageList1.Images.SetKeyName(34, "Icons.16x16.PrivateMethod.png");
            imageList1.Images.SetKeyName(35, "Icons.16x16.PrivateProperty.png");
            imageList1.Images.SetKeyName(36, "Icons.16x16.PrivateStruct.png");
            imageList1.Images.SetKeyName(37, "Icons.16x16.Property.png");
            imageList1.Images.SetKeyName(38, "Icons.16x16.ProtectedClass.png");
            imageList1.Images.SetKeyName(39, "Icons.16x16.ProtectedDelegate.png");
            imageList1.Images.SetKeyName(40, "Icons.16x16.ProtectedEnum.png");
            imageList1.Images.SetKeyName(41, "Icons.16x16.ProtectedEvent.png");
            imageList1.Images.SetKeyName(42, "Icons.16x16.ProtectedExtensionMethod.png");
            imageList1.Images.SetKeyName(43, "Icons.16x16.ProtectedField.png");
            imageList1.Images.SetKeyName(44, "Icons.16x16.ProtectedIndexer.png");
            imageList1.Images.SetKeyName(45, "Icons.16x16.ProtectedInterface.png");
            imageList1.Images.SetKeyName(46, "Icons.16x16.ProtectedMethod.png");
            imageList1.Images.SetKeyName(47, "Icons.16x16.ProtectedProperty.png");
            imageList1.Images.SetKeyName(48, "Icons.16x16.ProtectedStruct.png");
            imageList1.Images.SetKeyName(49, "Icons.16x16.Reference.png");
            imageList1.Images.SetKeyName(50, "Icons.16x16.Struct.png");
            // 
            // markStartFileChk
            // 
            markStartFileChk.AutoSize = true;
            markStartFileChk.Location = new System.Drawing.Point(420, 4);
            markStartFileChk.Name = "markStartFileChk";
            markStartFileChk.Size = new System.Drawing.Size(147, 19);
            markStartFileChk.TabIndex = 16;
            markStartFileChk.Text = "Mark file for auto open";
            markStartFileChk.UseVisualStyleBackColor = true;
            markStartFileChk.Visible = false;
            markStartFileChk.CheckedChanged += markStartFileChk_CheckedChanged;
            // 
            // liveStatusPb
            // 
            liveStatusPb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            liveStatusPb.Location = new System.Drawing.Point(1387, 5);
            liveStatusPb.Name = "liveStatusPb";
            liveStatusPb.Size = new System.Drawing.Size(18, 19);
            liveStatusPb.TabIndex = 17;
            liveStatusPb.TabStop = false;
            liveStatusPb.Paint += liveStatusPb_Paint;
            // 
            // tabMenu
            // 
            tabMenu.Items.AddRange(new ToolStripItem[] { closeTab, closeAllTabs, closeAllTabsOne });
            tabMenu.Name = "tabMenu";
            tabMenu.Size = new System.Drawing.Size(169, 70);
            // 
            // closeTab
            // 
            closeTab.Name = "closeTab";
            closeTab.Size = new System.Drawing.Size(168, 22);
            closeTab.Text = "Close Tab";
            closeTab.Click += closeTab_Click;
            // 
            // closeAllTabs
            // 
            closeAllTabs.Name = "closeAllTabs";
            closeAllTabs.Size = new System.Drawing.Size(168, 22);
            closeAllTabs.Text = "Close All Tabs";
            closeAllTabs.Click += closeAllTabs_Click;
            // 
            // closeAllTabsOne
            // 
            closeAllTabsOne.Name = "closeAllTabsOne";
            closeAllTabsOne.Size = new System.Drawing.Size(168, 22);
            closeAllTabsOne.Text = "Close All BUT This";
            closeAllTabsOne.Click += closeAllTabsOne_Click;
            // 
            // errorsContextMenu
            // 
            errorsContextMenu.Items.AddRange(new ToolStripItem[] { copyErrorMenuItem, askAiErrorMenuItem });
            errorsContextMenu.Name = "errorsContextMenu";
            errorsContextMenu.Size = new System.Drawing.Size(128, 48);
            // 
            // copyErrorMenuItem
            // 
            copyErrorMenuItem.Name = "copyErrorMenuItem";
            copyErrorMenuItem.Size = new System.Drawing.Size(127, 22);
            copyErrorMenuItem.Text = "Copy";
            copyErrorMenuItem.Click += copyErrorMenuItem_Click;
            // 
            // askAiErrorMenuItem
            // 
            askAiErrorMenuItem.Name = "askAiErrorMenuItem";
            askAiErrorMenuItem.Size = new System.Drawing.Size(127, 22);
            askAiErrorMenuItem.Text = "Ask AI";
            askAiErrorMenuItem.Click += askAiErrorMenuItem_Click;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.None;
            progressBar.Location = new System.Drawing.Point(398, 294);
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(265, 10);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 1;
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(1410, 862);
            Controls.Add(liveStatusPb);
            Controls.Add(markStartFileChk);
            Controls.Add(warningsCheckLbl);
            Controls.Add(typeCheckLbl);
            Controls.Add(linesPositionLbl);
            Controls.Add(linesCountLbl);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(splitContainer1);
            Controls.Add(runCodePb);
            Controls.Add(menuStrip1);
            Controls.Add(progressBar);
            ForeColor = System.Drawing.SystemColors.ControlText;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 3, 4, 3);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CIARE";
            Activated += MainForm_Activated;
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            ((System.ComponentModel.ISupportInitialize)runCodePb).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            outputTabControl.ResumeLayout(false);
            outputTabPage.ResumeLayout(false);
            errorsTabPage.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            EditorTabControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)liveStatusPb).EndInit();
            tabMenu.ResumeLayout(false);
            errorsContextMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion
        private System.Windows.Forms.PictureBox runCodePb;
        public System.Windows.Forms.RichTextBox outputRBT;
        public CIARE.GUI.DarkTabControl outputTabControl;
        public System.Windows.Forms.TabPage outputTabPage;
        public System.Windows.Forms.TabPage errorsTabPage;
        public System.Windows.Forms.RichTextBox errorsRTB;
        private System.Windows.Forms.ToolTip toolTip1;
        public System.Windows.Forms.ToolStripMenuItem fIleToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        public System.Windows.Forms.ToolStripMenuItem saveAsStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem newFileStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        public System.Windows.Forms.ToolStripMenuItem LoadCStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem hotKeyToolStripMenuItem;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.ToolStripMenuItem compileToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem compileToexeCtrlShiftBToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem compileToDLLCtrlSfitBToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        public System.Windows.Forms.ToolStripMenuItem copyStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem cutStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem pasteStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem deleteStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        public System.Windows.Forms.ToolStripMenuItem replaceStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem selectAllStripMenuItem3;
        public System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem splitEditorToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem showHideHSCToolStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator viewSeparator1;
        public System.Windows.Forms.ToolStripMenuItem fullScreenToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem goToLineStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        public System.Windows.Forms.ToolStripMenuItem cmdLinesArgsStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem splitVEditorToolStripMenuItem;
        public System.Windows.Forms.Label linesCountLbl;
        public System.Windows.Forms.Label linesPositionLbl;
        public System.Windows.Forms.Label typeCheckLbl;
        public System.Windows.Forms.Label warningsCheckLbl;
        public System.Windows.Forms.ToolStripSeparator compileStripSeparator1;
        public System.Windows.Forms.ToolStripMenuItem finStripMenuItem;
        public System.Windows.Forms.MenuStrip menuStrip1;
        internal System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        public System.Windows.Forms.CheckBox markStartFileChk;
        private System.Windows.Forms.ToolStripMenuItem liveToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem liveShareToolStripMenuItem;
        public System.Windows.Forms.PictureBox liveStatusPb;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        public System.Windows.Forms.ToolStripMenuItem chatGPTCTRLShiftPToolStripMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        public System.Windows.Forms.ToolStripMenuItem referenceAddToolStripMenuItem;
        public CIARE.GUI.DarkTabControl EditorTabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ContextMenuStrip tabMenu;
        private System.Windows.Forms.ToolStripMenuItem closeTab;
        private System.Windows.Forms.ToolStripMenuItem closeAllTabs;
        private System.Windows.Forms.ToolStripMenuItem closeAllTabsOne;
        private System.Windows.Forms.ContextMenuStrip errorsContextMenu;
        private System.Windows.Forms.ToolStripMenuItem copyErrorMenuItem;
        private System.Windows.Forms.ToolStripMenuItem askAiErrorMenuItem;
        public System.Windows.Forms.ProgressBar progressBar;
    }
}

