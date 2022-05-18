
namespace CIARE
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            this.outputRBT = new System.Windows.Forms.RichTextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fIleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.LoadCStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.cutStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.replaceStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectAllStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.compileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compileToexeCtrlShiftBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compileToDLLCtrlSfitBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showHideSCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.highlightCMB = new System.Windows.Forms.ComboBox();
            this.highlightLbl = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.findButton = new System.Windows.Forms.Button();
            this.runCodePb = new System.Windows.Forms.PictureBox();
            this.goToLineStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.runCodePb)).BeginInit();
            this.SuspendLayout();
            // 
            // textEditorControl1
            // 
            this.textEditorControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textEditorControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textEditorControl1.Highlighting = null;
            this.textEditorControl1.Location = new System.Drawing.Point(6, 3);
            this.textEditorControl1.Name = "textEditorControl1";
            this.textEditorControl1.Size = new System.Drawing.Size(1200, 589);
            this.textEditorControl1.TabIndex = 0;
            this.textEditorControl1.TextChanged += new System.EventHandler(this.textEditorControl1_TextChanged);
            // 
            // outputRBT
            // 
            this.outputRBT.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputRBT.BackColor = System.Drawing.SystemColors.Window;
            this.outputRBT.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.outputRBT.Font = new System.Drawing.Font("Bahnschrift", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputRBT.ForeColor = System.Drawing.SystemColors.MenuText;
            this.outputRBT.Location = new System.Drawing.Point(6, 16);
            this.outputRBT.Name = "outputRBT";
            this.outputRBT.ReadOnly = true;
            this.outputRBT.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.outputRBT.Size = new System.Drawing.Size(1185, 96);
            this.outputRBT.TabIndex = 3;
            this.outputRBT.Text = "";
            // 
            // toolTip1
            // 
            this.toolTip1.Tag = "Run Code";
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fIleToolStripMenuItem,
            this.editToolStripMenuItem,
            this.compileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1209, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fIleToolStripMenuItem
            // 
            this.fIleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsStripMenuItem,
            this.toolStripSeparator1,
            this.LoadCStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            this.fIleToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fIleToolStripMenuItem.Text = "File";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(273, 22);
            this.toolStripMenuItem1.Text = "New         ( CTRL + N )";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            this.openToolStripMenuItem.Text = "Open       ( CTRL + O )";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            this.saveToolStripMenuItem.Text = "Save         ( CTRL + S ) ";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsStripMenuItem
            // 
            this.saveAsStripMenuItem.Name = "saveAsStripMenuItem";
            this.saveAsStripMenuItem.Size = new System.Drawing.Size(273, 22);
            this.saveAsStripMenuItem.Text = "Save As    ( CTRL+Shift+S )";
            this.saveAsStripMenuItem.Click += new System.EventHandler(this.saveAsStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(270, 6);
            // 
            // LoadCStripMenuItem
            // 
            this.LoadCStripMenuItem.Name = "LoadCStripMenuItem";
            this.LoadCStripMenuItem.Size = new System.Drawing.Size(273, 22);
            this.LoadCStripMenuItem.Text = "Load C# Code Template    ( CTRL + T )";
            this.LoadCStripMenuItem.Click += new System.EventHandler(this.LoadCStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(270, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.ForeColor = System.Drawing.Color.Red;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.toolStripSeparator3,
            this.cutStripMenuItem,
            this.copyStripMenuItem,
            this.pasteStripMenuItem,
            this.deleteStripMenuItem,
            this.toolStripSeparator4,
            this.replaceStripMenuItem,
            this.goToLineStripMenuItem,
            this.selectAllStripMenuItem3});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.ShowShortcutKeys = false;
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.undoToolStripMenuItem.Text = "Undo        ( CTRL + Z )";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(188, 6);
            // 
            // cutStripMenuItem
            // 
            this.cutStripMenuItem.Name = "cutStripMenuItem";
            this.cutStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.cutStripMenuItem.Text = "Cut           ( CTRL + X )";
            this.cutStripMenuItem.Click += new System.EventHandler(this.cutStripMenuItem_Click);
            // 
            // copyStripMenuItem
            // 
            this.copyStripMenuItem.Name = "copyStripMenuItem";
            this.copyStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.copyStripMenuItem.Text = "Copy        ( CTRL + C )";
            this.copyStripMenuItem.Click += new System.EventHandler(this.copyStripMenuItem_Click);
            // 
            // pasteStripMenuItem
            // 
            this.pasteStripMenuItem.Name = "pasteStripMenuItem";
            this.pasteStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.pasteStripMenuItem.Text = "Paste        ( CTRL + V )";
            this.pasteStripMenuItem.Click += new System.EventHandler(this.pasteStripMenuItem_Click);
            // 
            // deleteStripMenuItem
            // 
            this.deleteStripMenuItem.Name = "deleteStripMenuItem";
            this.deleteStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.deleteStripMenuItem.Text = "Delete                     Del";
            this.deleteStripMenuItem.Click += new System.EventHandler(this.deleteStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(188, 6);
            // 
            // replaceStripMenuItem
            // 
            this.replaceStripMenuItem.Name = "replaceStripMenuItem";
            this.replaceStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.replaceStripMenuItem.Text = "Replace    ( CTRL + H )";
            this.replaceStripMenuItem.Click += new System.EventHandler(this.replaceStripMenuItem_Click);
            // 
            // selectAllStripMenuItem3
            // 
            this.selectAllStripMenuItem3.Name = "selectAllStripMenuItem3";
            this.selectAllStripMenuItem3.Size = new System.Drawing.Size(191, 22);
            this.selectAllStripMenuItem3.Text = "Select All  ( CTRL + A )";
            this.selectAllStripMenuItem3.Click += new System.EventHandler(this.selectAllStripMenuItem3_Click);
            // 
            // compileToolStripMenuItem
            // 
            this.compileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.compileToexeCtrlShiftBToolStripMenuItem,
            this.compileToDLLCtrlSfitBToolStripMenuItem});
            this.compileToolStripMenuItem.Name = "compileToolStripMenuItem";
            this.compileToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.compileToolStripMenuItem.Text = "Compile";
            // 
            // compileToexeCtrlShiftBToolStripMenuItem
            // 
            this.compileToexeCtrlShiftBToolStripMenuItem.Name = "compileToexeCtrlShiftBToolStripMenuItem";
            this.compileToexeCtrlShiftBToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.compileToexeCtrlShiftBToolStripMenuItem.Text = "Compile to EXE ( Ctrl + B )";
            this.compileToexeCtrlShiftBToolStripMenuItem.Click += new System.EventHandler(this.compileToexeCtrlShiftBToolStripMenuItem_Click);
            // 
            // compileToDLLCtrlSfitBToolStripMenuItem
            // 
            this.compileToDLLCtrlSfitBToolStripMenuItem.Name = "compileToDLLCtrlSfitBToolStripMenuItem";
            this.compileToDLLCtrlSfitBToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.compileToDLLCtrlSfitBToolStripMenuItem.Text = "Compile to DLL ( Ctrl + Shift + B )";
            this.compileToDLLCtrlSfitBToolStripMenuItem.Click += new System.EventHandler(this.compileToDLLCtrlSfitBToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.splitEditorToolStripMenuItem,
            this.showHideSCToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // splitEditorToolStripMenuItem
            // 
            this.splitEditorToolStripMenuItem.Name = "splitEditorToolStripMenuItem";
            this.splitEditorToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.splitEditorToolStripMenuItem.Text = "Split Editor                     ( CTRL + W )";
            this.splitEditorToolStripMenuItem.Click += new System.EventHandler(this.splitEditorToolStripMenuItem_Click);
            // 
            // showHideSCToolStripMenuItem
            // 
            this.showHideSCToolStripMenuItem.Name = "showHideSCToolStripMenuItem";
            this.showHideSCToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.showHideSCToolStripMenuItem.Text = "Show/Hide Output       ( CTRL + K )";
            this.showHideSCToolStripMenuItem.Click += new System.EventHandler(this.showHideSCToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.outputRBT);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(6, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1197, 118);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.textEditorControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(1209, 723);
            this.splitContainer1.SplitterDistance = 595;
            this.splitContainer1.TabIndex = 6;
            // 
            // highlightCMB
            // 
            this.highlightCMB.FormattingEnabled = true;
            this.highlightCMB.Items.AddRange(new object[] {
            "Default",
            "XML",
            "HTML",
            "C++.NET",
            "BAT",
            "Coco",
            "Python",
            "PHP",
            "SQL",
            "C#",
            "Batch",
            "Boo",
            "VBNET",
            "TeX",
            "ASP/XHTML",
            "JavaScript",
            "Java"});
            this.highlightCMB.Location = new System.Drawing.Point(333, 3);
            this.highlightCMB.Name = "highlightCMB";
            this.highlightCMB.Size = new System.Drawing.Size(80, 21);
            this.highlightCMB.TabIndex = 7;
            this.highlightCMB.Text = "Default";
            this.highlightCMB.SelectedIndexChanged += new System.EventHandler(this.highlightCMB_SelectedIndexChanged);
            // 
            // highlightLbl
            // 
            this.highlightLbl.AutoSize = true;
            this.highlightLbl.Location = new System.Drawing.Point(262, 6);
            this.highlightLbl.Name = "highlightLbl";
            this.highlightLbl.Size = new System.Drawing.Size(65, 13);
            this.highlightLbl.TabIndex = 8;
            this.highlightLbl.Text = "Highlighting:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(246, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(13, 18);
            this.label1.TabIndex = 9;
            this.label1.Text = "|";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(423, 2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(13, 18);
            this.label2.TabIndex = 10;
            this.label2.Text = "|";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(468, 2);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(13, 18);
            this.label3.TabIndex = 11;
            this.label3.Text = "|";
            // 
            // searchBox
            // 
            this.searchBox.Location = new System.Drawing.Point(486, 3);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(114, 20);
            this.searchBox.TabIndex = 12;
            this.searchBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findButton
            // 
            this.findButton.Location = new System.Drawing.Point(612, 1);
            this.findButton.Name = "findButton";
            this.findButton.Size = new System.Drawing.Size(97, 23);
            this.findButton.TabIndex = 13;
            this.findButton.Text = "Find ( CTRL + F )";
            this.findButton.UseVisualStyleBackColor = true;
            this.findButton.Click += new System.EventHandler(this.findButton_Click);
            // 
            // runCodePb
            // 
            this.runCodePb.Image = global::CIARE.Properties.Resources.runButton2;
            this.runCodePb.Location = new System.Drawing.Point(442, 4);
            this.runCodePb.Name = "runCodePb";
            this.runCodePb.Size = new System.Drawing.Size(28, 21);
            this.runCodePb.TabIndex = 2;
            this.runCodePb.TabStop = false;
            this.toolTip1.SetToolTip(this.runCodePb, "Run code (CTRL + R)");
            this.runCodePb.Click += new System.EventHandler(this.runCodePb_Click);
            // 
            // goToLineStripMenuItem
            // 
            this.goToLineStripMenuItem.Name = "goToLineStripMenuItem";
            this.goToLineStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.goToLineStripMenuItem.Text = "Go To ..     ( CTRL + G )";
            this.goToLineStripMenuItem.Click += new System.EventHandler(this.goToLineStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(1209, 747);
            this.Controls.Add(this.findButton);
            this.Controls.Add(this.searchBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.highlightLbl);
            this.Controls.Add(this.highlightCMB);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.runCodePb);
            this.Controls.Add(this.menuStrip1);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CIARE";
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.runCodePb)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
        private System.Windows.Forms.PictureBox runCodePb;
        private System.Windows.Forms.RichTextBox outputRBT;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fIleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripMenuItem saveAsStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ComboBox highlightCMB;
        private System.Windows.Forms.Label highlightLbl;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem LoadCStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.Button findButton;
        private System.Windows.Forms.ToolStripMenuItem compileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compileToexeCtrlShiftBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compileToDLLCtrlSfitBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem copyStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem replaceStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAllStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem splitEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHideSCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem goToLineStripMenuItem;
    }
}

