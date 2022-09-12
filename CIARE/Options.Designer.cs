
namespace CIARE
{
    partial class Options
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.highlightLbl = new System.Windows.Forms.Label();
            this.highlightCMB = new System.Windows.Forms.ComboBox();
            this.codeCompletionCkb = new System.Windows.Forms.CheckBox();
            this.lineNumberCkb = new System.Windows.Forms.CheckBox();
            this.codeFoldingCkb = new System.Windows.Forms.CheckBox();
            this.closeBtn = new System.Windows.Forms.Button();
            this.displayGroup = new System.Windows.Forms.GroupBox();
            this.buildGroup = new System.Windows.Forms.GroupBox();
            this.platformBox = new System.Windows.Forms.ComboBox();
            this.configurationBox = new System.Windows.Forms.ComboBox();
            this.platformLbl = new System.Windows.Forms.Label();
            this.warningsCkb = new System.Windows.Forms.CheckBox();
            this.displayGroup.SuspendLayout();
            this.buildGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // highlightLbl
            // 
            this.highlightLbl.AutoSize = true;
            this.highlightLbl.Location = new System.Drawing.Point(12, 26);
            this.highlightLbl.Name = "highlightLbl";
            this.highlightLbl.Size = new System.Drawing.Size(94, 15);
            this.highlightLbl.TabIndex = 9;
            this.highlightLbl.Text = "Editor Highlight:";
            // 
            // highlightCMB
            // 
            this.highlightCMB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
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
            "C#-Light",
            "C#-Dark",
            "Batch",
            "Boo",
            "VBNET",
            "TeX",
            "ASP/XHTML",
            "JavaScript",
            "Java"});
            this.highlightCMB.Location = new System.Drawing.Point(116, 23);
            this.highlightCMB.Name = "highlightCMB";
            this.highlightCMB.Size = new System.Drawing.Size(93, 23);
            this.highlightCMB.TabIndex = 10;
            this.highlightCMB.Text = "Default";
            this.highlightCMB.SelectedIndexChanged += new System.EventHandler(this.highlightCMB_SelectedIndexChanged);
            // 
            // codeCompletionCkb
            // 
            this.codeCompletionCkb.AutoSize = true;
            this.codeCompletionCkb.Location = new System.Drawing.Point(15, 66);
            this.codeCompletionCkb.Name = "codeCompletionCkb";
            this.codeCompletionCkb.Size = new System.Drawing.Size(277, 19);
            this.codeCompletionCkb.TabIndex = 11;
            this.codeCompletionCkb.Text = "Code Completion (requires application restart)";
            this.codeCompletionCkb.UseVisualStyleBackColor = true;
            this.codeCompletionCkb.CheckedChanged += new System.EventHandler(this.codeCompletionCkb_CheckedChanged);
            // 
            // lineNumberCkb
            // 
            this.lineNumberCkb.AutoSize = true;
            this.lineNumberCkb.Location = new System.Drawing.Point(15, 103);
            this.lineNumberCkb.Name = "lineNumberCkb";
            this.lineNumberCkb.Size = new System.Drawing.Size(98, 19);
            this.lineNumberCkb.TabIndex = 12;
            this.lineNumberCkb.Text = "Line Number";
            this.lineNumberCkb.UseVisualStyleBackColor = true;
            this.lineNumberCkb.CheckedChanged += new System.EventHandler(this.lineNumberCkb_CheckedChanged);
            // 
            // codeFoldingCkb
            // 
            this.codeFoldingCkb.AutoSize = true;
            this.codeFoldingCkb.Location = new System.Drawing.Point(15, 139);
            this.codeFoldingCkb.Name = "codeFoldingCkb";
            this.codeFoldingCkb.Size = new System.Drawing.Size(99, 19);
            this.codeFoldingCkb.TabIndex = 13;
            this.codeFoldingCkb.Text = "Code Folding";
            this.codeFoldingCkb.UseVisualStyleBackColor = true;
            this.codeFoldingCkb.CheckedChanged += new System.EventHandler(this.codeFoldingCkb_CheckedChanged);
            // 
            // closeBtn
            // 
            this.closeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.closeBtn.Location = new System.Drawing.Point(603, 212);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 14;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // displayGroup
            // 
            this.displayGroup.Controls.Add(this.highlightLbl);
            this.displayGroup.Controls.Add(this.highlightCMB);
            this.displayGroup.Controls.Add(this.codeFoldingCkb);
            this.displayGroup.Controls.Add(this.codeCompletionCkb);
            this.displayGroup.Controls.Add(this.lineNumberCkb);
            this.displayGroup.Location = new System.Drawing.Point(12, 12);
            this.displayGroup.Name = "displayGroup";
            this.displayGroup.Size = new System.Drawing.Size(330, 187);
            this.displayGroup.TabIndex = 15;
            this.displayGroup.TabStop = false;
            this.displayGroup.Text = "Display";
            // 
            // buildGroup
            // 
            this.buildGroup.Controls.Add(this.warningsCkb);
            this.buildGroup.Controls.Add(this.platformBox);
            this.buildGroup.Controls.Add(this.configurationBox);
            this.buildGroup.Controls.Add(this.platformLbl);
            this.buildGroup.Location = new System.Drawing.Point(348, 12);
            this.buildGroup.Name = "buildGroup";
            this.buildGroup.Size = new System.Drawing.Size(330, 187);
            this.buildGroup.TabIndex = 16;
            this.buildGroup.TabStop = false;
            this.buildGroup.Text = "Build";
            // 
            // platformBox
            // 
            this.platformBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.platformBox.FormattingEnabled = true;
            this.platformBox.Items.AddRange(new object[] {
            "Any CPU",
            "x64",
            "x86"});
            this.platformBox.Location = new System.Drawing.Point(169, 23);
            this.platformBox.Name = "platformBox";
            this.platformBox.Size = new System.Drawing.Size(93, 23);
            this.platformBox.TabIndex = 15;
            this.platformBox.Text = "Any CPU";
            // 
            // configurationBox
            // 
            this.configurationBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.configurationBox.FormattingEnabled = true;
            this.configurationBox.Items.AddRange(new object[] {
            "Debug",
            "Release"});
            this.configurationBox.Location = new System.Drawing.Point(70, 23);
            this.configurationBox.Name = "configurationBox";
            this.configurationBox.Size = new System.Drawing.Size(93, 23);
            this.configurationBox.TabIndex = 14;
            this.configurationBox.Text = "Debug";
            // 
            // platformLbl
            // 
            this.platformLbl.AutoSize = true;
            this.platformLbl.Location = new System.Drawing.Point(12, 26);
            this.platformLbl.Name = "platformLbl";
            this.platformLbl.Size = new System.Drawing.Size(53, 15);
            this.platformLbl.TabIndex = 0;
            this.platformLbl.Text = "Params:";
            // 
            // warningsCkb
            // 
            this.warningsCkb.AutoSize = true;
            this.warningsCkb.Location = new System.Drawing.Point(15, 66);
            this.warningsCkb.Name = "warningsCkb";
            this.warningsCkb.Size = new System.Drawing.Size(165, 19);
            this.warningsCkb.TabIndex = 14;
            this.warningsCkb.Text = "Enable compile warnings";
            this.warningsCkb.UseVisualStyleBackColor = true;
            this.warningsCkb.CheckedChanged += new System.EventHandler(this.warningsCkb_CheckedChanged);
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.closeBtn;
            this.ClientSize = new System.Drawing.Size(690, 250);
            this.Controls.Add(this.buildGroup);
            this.Controls.Add(this.displayGroup);
            this.Controls.Add(this.closeBtn);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Options";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.Options_Load);
            this.displayGroup.ResumeLayout(false);
            this.displayGroup.PerformLayout();
            this.buildGroup.ResumeLayout(false);
            this.buildGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label highlightLbl;
        private System.Windows.Forms.ComboBox highlightCMB;
        private System.Windows.Forms.CheckBox codeCompletionCkb;
        private System.Windows.Forms.CheckBox lineNumberCkb;
        private System.Windows.Forms.CheckBox codeFoldingCkb;
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.GroupBox displayGroup;
        private System.Windows.Forms.GroupBox buildGroup;
        private System.Windows.Forms.ComboBox platformBox;
        private System.Windows.Forms.ComboBox configurationBox;
        private System.Windows.Forms.Label platformLbl;
        private System.Windows.Forms.CheckBox warningsCkb;
    }
}