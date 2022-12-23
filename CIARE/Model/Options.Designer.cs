
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
            this.winLoginCkb = new System.Windows.Forms.CheckBox();
            this.startBehaveCkb = new System.Windows.Forms.CheckBox();
            this.behaveSetLbl = new System.Windows.Forms.Label();
            this.displaySepLbl = new System.Windows.Forms.Label();
            this.buildGroup = new System.Windows.Forms.GroupBox();
            this.frameworkLbl = new System.Windows.Forms.Label();
            this.frameWorkCMB = new System.Windows.Forms.ComboBox();
            this.warningsCkb = new System.Windows.Forms.CheckBox();
            this.platformBox = new System.Windows.Forms.ComboBox();
            this.configurationBox = new System.Windows.Forms.ComboBox();
            this.platformLbl = new System.Windows.Forms.Label();
            this.liveShareGb = new System.Windows.Forms.GroupBox();
            this.saveApiUrlBtn = new System.Windows.Forms.Button();
            this.apiUrlTxt = new System.Windows.Forms.TextBox();
            this.apiUrlLbl = new System.Windows.Forms.Label();
            this.openAIGroup = new System.Windows.Forms.GroupBox();
            this.openAISaveBtn = new System.Windows.Forms.Button();
            this.maxTokensTxtBox = new System.Windows.Forms.TextBox();
            this.maxTokensLbl = new System.Windows.Forms.Label();
            this.apiKeyAiTxtBox = new System.Windows.Forms.TextBox();
            this.apiKeyAIlbl = new System.Windows.Forms.Label();
            this.displayGroup.SuspendLayout();
            this.buildGroup.SuspendLayout();
            this.liveShareGb.SuspendLayout();
            this.openAIGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // highlightLbl
            // 
            this.highlightLbl.AutoSize = true;
            this.highlightLbl.Location = new System.Drawing.Point(15, 70);
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
            "C#-Light",
            "C#-Dark"});
            this.highlightCMB.Location = new System.Drawing.Point(119, 67);
            this.highlightCMB.Name = "highlightCMB";
            this.highlightCMB.Size = new System.Drawing.Size(93, 23);
            this.highlightCMB.TabIndex = 10;
            this.highlightCMB.Text = "C#-Dark";
            this.highlightCMB.SelectedIndexChanged += new System.EventHandler(this.highlightCMB_SelectedIndexChanged);
            // 
            // codeCompletionCkb
            // 
            this.codeCompletionCkb.AutoSize = true;
            this.codeCompletionCkb.Location = new System.Drawing.Point(18, 110);
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
            this.lineNumberCkb.Location = new System.Drawing.Point(18, 147);
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
            this.codeFoldingCkb.Location = new System.Drawing.Point(18, 183);
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
            this.closeBtn.Location = new System.Drawing.Point(603, 485);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 14;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // displayGroup
            // 
            this.displayGroup.Controls.Add(this.winLoginCkb);
            this.displayGroup.Controls.Add(this.startBehaveCkb);
            this.displayGroup.Controls.Add(this.behaveSetLbl);
            this.displayGroup.Controls.Add(this.displaySepLbl);
            this.displayGroup.Controls.Add(this.highlightLbl);
            this.displayGroup.Controls.Add(this.highlightCMB);
            this.displayGroup.Controls.Add(this.codeFoldingCkb);
            this.displayGroup.Controls.Add(this.codeCompletionCkb);
            this.displayGroup.Controls.Add(this.lineNumberCkb);
            this.displayGroup.Location = new System.Drawing.Point(12, 12);
            this.displayGroup.Name = "displayGroup";
            this.displayGroup.Size = new System.Drawing.Size(330, 341);
            this.displayGroup.TabIndex = 15;
            this.displayGroup.TabStop = false;
            this.displayGroup.Text = "Settings:";
            // 
            // winLoginCkb
            // 
            this.winLoginCkb.AutoSize = true;
            this.winLoginCkb.Location = new System.Drawing.Point(18, 292);
            this.winLoginCkb.Name = "winLoginCkb";
            this.winLoginCkb.Size = new System.Drawing.Size(226, 19);
            this.winLoginCkb.TabIndex = 17;
            this.winLoginCkb.Text = "Open marked files on Windows login";
            this.winLoginCkb.UseVisualStyleBackColor = true;
            this.winLoginCkb.CheckedChanged += new System.EventHandler(this.winLoginCkb_CheckedChanged);
            // 
            // startBehaveCkb
            // 
            this.startBehaveCkb.AutoSize = true;
            this.startBehaveCkb.Location = new System.Drawing.Point(18, 257);
            this.startBehaveCkb.Name = "startBehaveCkb";
            this.startBehaveCkb.Size = new System.Drawing.Size(230, 19);
            this.startBehaveCkb.TabIndex = 16;
            this.startBehaveCkb.Text = "Activate mark files for start on next run";
            this.startBehaveCkb.UseVisualStyleBackColor = true;
            this.startBehaveCkb.CheckedChanged += new System.EventHandler(this.startBehaveCkb_CheckedChanged);
            // 
            // behaveSetLbl
            // 
            this.behaveSetLbl.AutoSize = true;
            this.behaveSetLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.behaveSetLbl.Location = new System.Drawing.Point(15, 220);
            this.behaveSetLbl.Name = "behaveSetLbl";
            this.behaveSetLbl.Size = new System.Drawing.Size(243, 15);
            this.behaveSetLbl.TabIndex = 15;
            this.behaveSetLbl.Text = "Behaviour                                           ";
            // 
            // displaySepLbl
            // 
            this.displaySepLbl.AutoSize = true;
            this.displaySepLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.displaySepLbl.Location = new System.Drawing.Point(15, 35);
            this.displaySepLbl.Name = "displaySepLbl";
            this.displaySepLbl.Size = new System.Drawing.Size(234, 15);
            this.displaySepLbl.TabIndex = 14;
            this.displaySepLbl.Text = "Display                                             ";
            // 
            // buildGroup
            // 
            this.buildGroup.Controls.Add(this.frameworkLbl);
            this.buildGroup.Controls.Add(this.frameWorkCMB);
            this.buildGroup.Controls.Add(this.warningsCkb);
            this.buildGroup.Controls.Add(this.platformBox);
            this.buildGroup.Controls.Add(this.configurationBox);
            this.buildGroup.Controls.Add(this.platformLbl);
            this.buildGroup.Location = new System.Drawing.Point(348, 12);
            this.buildGroup.Name = "buildGroup";
            this.buildGroup.Size = new System.Drawing.Size(330, 168);
            this.buildGroup.TabIndex = 16;
            this.buildGroup.TabStop = false;
            this.buildGroup.Text = "Build";
            // 
            // frameworkLbl
            // 
            this.frameworkLbl.AutoSize = true;
            this.frameworkLbl.Location = new System.Drawing.Point(12, 115);
            this.frameworkLbl.Name = "frameworkLbl";
            this.frameworkLbl.Size = new System.Drawing.Size(159, 15);
            this.frameworkLbl.TabIndex = 18;
            this.frameworkLbl.Text = "Target Compile Framework:";
            // 
            // frameWorkCMB
            // 
            this.frameWorkCMB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.frameWorkCMB.FormattingEnabled = true;
            this.frameWorkCMB.Items.AddRange(new object[] {
            ".NET 6",
            ".NET 7"});
            this.frameWorkCMB.Location = new System.Drawing.Point(172, 112);
            this.frameWorkCMB.Name = "frameWorkCMB";
            this.frameWorkCMB.Size = new System.Drawing.Size(93, 23);
            this.frameWorkCMB.TabIndex = 19;
            this.frameWorkCMB.Text = ".NET 6";
            this.frameWorkCMB.SelectedIndexChanged += new System.EventHandler(this.frameWorkCMB_SelectedIndexChanged);
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
            // platformBox
            // 
            this.platformBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.platformBox.FormattingEnabled = true;
            this.platformBox.Items.AddRange(new object[] {
            "Any CPU",
            "x64"});
            this.platformBox.Location = new System.Drawing.Point(172, 23);
            this.platformBox.Name = "platformBox";
            this.platformBox.Size = new System.Drawing.Size(93, 23);
            this.platformBox.TabIndex = 15;
            this.platformBox.Text = "Any CPU";
            this.platformBox.SelectedIndexChanged += new System.EventHandler(this.platformBox_SelectedIndexChanged);
            // 
            // configurationBox
            // 
            this.configurationBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.configurationBox.FormattingEnabled = true;
            this.configurationBox.Items.AddRange(new object[] {
            "Debug",
            "Release"});
            this.configurationBox.Location = new System.Drawing.Point(73, 23);
            this.configurationBox.Name = "configurationBox";
            this.configurationBox.Size = new System.Drawing.Size(93, 23);
            this.configurationBox.TabIndex = 14;
            this.configurationBox.Text = "Debug";
            this.configurationBox.SelectedIndexChanged += new System.EventHandler(this.configurationBox_SelectedIndexChanged);
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
            // liveShareGb
            // 
            this.liveShareGb.Controls.Add(this.saveApiUrlBtn);
            this.liveShareGb.Controls.Add(this.apiUrlTxt);
            this.liveShareGb.Controls.Add(this.apiUrlLbl);
            this.liveShareGb.Location = new System.Drawing.Point(348, 185);
            this.liveShareGb.Name = "liveShareGb";
            this.liveShareGb.Size = new System.Drawing.Size(330, 168);
            this.liveShareGb.TabIndex = 17;
            this.liveShareGb.TabStop = false;
            this.liveShareGb.Text = "Live Share";
            // 
            // saveApiUrlBtn
            // 
            this.saveApiUrlBtn.Enabled = false;
            this.saveApiUrlBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveApiUrlBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.saveApiUrlBtn.Location = new System.Drawing.Point(237, 124);
            this.saveApiUrlBtn.Name = "saveApiUrlBtn";
            this.saveApiUrlBtn.Size = new System.Drawing.Size(75, 23);
            this.saveApiUrlBtn.TabIndex = 18;
            this.saveApiUrlBtn.Text = "Save";
            this.saveApiUrlBtn.UseVisualStyleBackColor = true;
            this.saveApiUrlBtn.Click += new System.EventHandler(this.saveApiUrlBtn_Click);
            // 
            // apiUrlTxt
            // 
            this.apiUrlTxt.Location = new System.Drawing.Point(16, 78);
            this.apiUrlTxt.Name = "apiUrlTxt";
            this.apiUrlTxt.Size = new System.Drawing.Size(296, 21);
            this.apiUrlTxt.TabIndex = 1;
            this.apiUrlTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.apiUrlTxt.TextChanged += new System.EventHandler(this.apiUrlTxt_TextChanged);
            // 
            // apiUrlLbl
            // 
            this.apiUrlLbl.AutoSize = true;
            this.apiUrlLbl.Location = new System.Drawing.Point(12, 40);
            this.apiUrlLbl.Name = "apiUrlLbl";
            this.apiUrlLbl.Size = new System.Drawing.Size(117, 15);
            this.apiUrlLbl.TabIndex = 0;
            this.apiUrlLbl.Text = "Live Share API URL:";
            // 
            // openAIGroup
            // 
            this.openAIGroup.Controls.Add(this.openAISaveBtn);
            this.openAIGroup.Controls.Add(this.maxTokensTxtBox);
            this.openAIGroup.Controls.Add(this.maxTokensLbl);
            this.openAIGroup.Controls.Add(this.apiKeyAiTxtBox);
            this.openAIGroup.Controls.Add(this.apiKeyAIlbl);
            this.openAIGroup.Location = new System.Drawing.Point(12, 359);
            this.openAIGroup.Name = "openAIGroup";
            this.openAIGroup.Size = new System.Drawing.Size(666, 110);
            this.openAIGroup.TabIndex = 18;
            this.openAIGroup.TabStop = false;
            this.openAIGroup.Text = "OpenAI";
            // 
            // openAISaveBtn
            // 
            this.openAISaveBtn.Enabled = false;
            this.openAISaveBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.openAISaveBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.openAISaveBtn.Location = new System.Drawing.Point(573, 75);
            this.openAISaveBtn.Name = "openAISaveBtn";
            this.openAISaveBtn.Size = new System.Drawing.Size(75, 23);
            this.openAISaveBtn.TabIndex = 19;
            this.openAISaveBtn.Text = "Save";
            this.openAISaveBtn.UseVisualStyleBackColor = true;
            this.openAISaveBtn.Click += new System.EventHandler(this.openAISaveBtn_Click);
            // 
            // maxTokensTxtBox
            // 
            this.maxTokensTxtBox.Location = new System.Drawing.Point(98, 64);
            this.maxTokensTxtBox.Name = "maxTokensTxtBox";
            this.maxTokensTxtBox.Size = new System.Drawing.Size(79, 21);
            this.maxTokensTxtBox.TabIndex = 21;
            this.maxTokensTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.maxTokensTxtBox.TextChanged += new System.EventHandler(this.maxTokensTxtBox_TextChanged);
            // 
            // maxTokensLbl
            // 
            this.maxTokensLbl.AutoSize = true;
            this.maxTokensLbl.CausesValidation = false;
            this.maxTokensLbl.Location = new System.Drawing.Point(15, 66);
            this.maxTokensLbl.Name = "maxTokensLbl";
            this.maxTokensLbl.Size = new System.Drawing.Size(77, 15);
            this.maxTokensLbl.TabIndex = 20;
            this.maxTokensLbl.Text = "Max Tokens:";
            // 
            // apiKeyAiTxtBox
            // 
            this.apiKeyAiTxtBox.Location = new System.Drawing.Point(75, 24);
            this.apiKeyAiTxtBox.Name = "apiKeyAiTxtBox";
            this.apiKeyAiTxtBox.Size = new System.Drawing.Size(573, 21);
            this.apiKeyAiTxtBox.TabIndex = 19;
            this.apiKeyAiTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.apiKeyAiTxtBox.UseSystemPasswordChar = true;
            this.apiKeyAiTxtBox.TextChanged += new System.EventHandler(this.apiKeyAiTxtBox_TextChanged);
            // 
            // apiKeyAIlbl
            // 
            this.apiKeyAIlbl.AutoSize = true;
            this.apiKeyAIlbl.Location = new System.Drawing.Point(15, 26);
            this.apiKeyAIlbl.Name = "apiKeyAIlbl";
            this.apiKeyAIlbl.Size = new System.Drawing.Size(54, 15);
            this.apiKeyAIlbl.TabIndex = 0;
            this.apiKeyAIlbl.Text = "API Key: ";
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.closeBtn;
            this.ClientSize = new System.Drawing.Size(690, 525);
            this.Controls.Add(this.openAIGroup);
            this.Controls.Add(this.liveShareGb);
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
            this.liveShareGb.ResumeLayout(false);
            this.liveShareGb.PerformLayout();
            this.openAIGroup.ResumeLayout(false);
            this.openAIGroup.PerformLayout();
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
        private System.Windows.Forms.CheckBox startBehaveCkb;
        private System.Windows.Forms.Label behaveSetLbl;
        private System.Windows.Forms.Label displaySepLbl;
        private System.Windows.Forms.CheckBox winLoginCkb;
        private System.Windows.Forms.Label frameworkLbl;
        private System.Windows.Forms.ComboBox frameWorkCMB;
        private System.Windows.Forms.GroupBox liveShareGb;
        private System.Windows.Forms.Button saveApiUrlBtn;
        private System.Windows.Forms.TextBox apiUrlTxt;
        private System.Windows.Forms.Label apiUrlLbl;
        private System.Windows.Forms.GroupBox openAIGroup;
        private System.Windows.Forms.TextBox maxTokensTxtBox;
        private System.Windows.Forms.Label maxTokensLbl;
        private System.Windows.Forms.TextBox apiKeyAiTxtBox;
        private System.Windows.Forms.Label apiKeyAIlbl;
        private System.Windows.Forms.Button openAISaveBtn;
    }
}