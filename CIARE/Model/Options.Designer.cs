
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
            highlightLbl = new System.Windows.Forms.Label();
            highlightCMB = new System.Windows.Forms.ComboBox();
            codeCompletionCkb = new System.Windows.Forms.CheckBox();
            lineNumberCkb = new System.Windows.Forms.CheckBox();
            codeFoldingCkb = new System.Windows.Forms.CheckBox();
            closeBtn = new System.Windows.Forms.Button();
            displayGroup = new System.Windows.Forms.GroupBox();
            winLoginCkb = new System.Windows.Forms.CheckBox();
            startBehaveCkb = new System.Windows.Forms.CheckBox();
            behaveSetLbl = new System.Windows.Forms.Label();
            displaySepLbl = new System.Windows.Forms.Label();
            buildGroup = new System.Windows.Forms.GroupBox();
            unsafeCkb = new System.Windows.Forms.CheckBox();
            frameworkLbl = new System.Windows.Forms.Label();
            frameWorkCMB = new System.Windows.Forms.ComboBox();
            warningsCkb = new System.Windows.Forms.CheckBox();
            platformBox = new System.Windows.Forms.ComboBox();
            configurationBox = new System.Windows.Forms.ComboBox();
            platformLbl = new System.Windows.Forms.Label();
            liveShareGb = new System.Windows.Forms.GroupBox();
            saveApiUrlBtn = new System.Windows.Forms.Button();
            apiUrlTxt = new System.Windows.Forms.TextBox();
            apiUrlLbl = new System.Windows.Forms.Label();
            openAIGroup = new System.Windows.Forms.GroupBox();
            modelTxt = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            openAISaveBtn = new System.Windows.Forms.Button();
            maxTokensTxtBox = new System.Windows.Forms.TextBox();
            maxTokensLbl = new System.Windows.Forms.Label();
            apiKeyAiTxtBox = new System.Windows.Forms.TextBox();
            apiKeyAIlbl = new System.Windows.Forms.Label();
            publishCkb = new System.Windows.Forms.CheckBox();
            displayGroup.SuspendLayout();
            buildGroup.SuspendLayout();
            liveShareGb.SuspendLayout();
            openAIGroup.SuspendLayout();
            SuspendLayout();
            // 
            // highlightLbl
            // 
            highlightLbl.AutoSize = true;
            highlightLbl.Location = new System.Drawing.Point(15, 70);
            highlightLbl.Name = "highlightLbl";
            highlightLbl.Size = new System.Drawing.Size(94, 15);
            highlightLbl.TabIndex = 9;
            highlightLbl.Text = "Editor Highlight:";
            // 
            // highlightCMB
            // 
            highlightCMB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            highlightCMB.FormattingEnabled = true;
            highlightCMB.Items.AddRange(new object[] { "Default", "C#-Light", "C#-Dark", "C#-DarkVS" });
            highlightCMB.Location = new System.Drawing.Point(119, 67);
            highlightCMB.Name = "highlightCMB";
            highlightCMB.Size = new System.Drawing.Size(93, 23);
            highlightCMB.TabIndex = 10;
            highlightCMB.Text = "C#-Dark";
            highlightCMB.SelectedIndexChanged += highlightCMB_SelectedIndexChanged;
            // 
            // codeCompletionCkb
            // 
            codeCompletionCkb.AutoSize = true;
            codeCompletionCkb.Location = new System.Drawing.Point(18, 110);
            codeCompletionCkb.Name = "codeCompletionCkb";
            codeCompletionCkb.Size = new System.Drawing.Size(277, 19);
            codeCompletionCkb.TabIndex = 11;
            codeCompletionCkb.Text = "Code Completion (requires application restart)";
            codeCompletionCkb.UseVisualStyleBackColor = true;
            codeCompletionCkb.CheckedChanged += codeCompletionCkb_CheckedChanged;
            // 
            // lineNumberCkb
            // 
            lineNumberCkb.AutoSize = true;
            lineNumberCkb.Location = new System.Drawing.Point(18, 147);
            lineNumberCkb.Name = "lineNumberCkb";
            lineNumberCkb.Size = new System.Drawing.Size(98, 19);
            lineNumberCkb.TabIndex = 12;
            lineNumberCkb.Text = "Line Number";
            lineNumberCkb.UseVisualStyleBackColor = true;
            lineNumberCkb.CheckedChanged += lineNumberCkb_CheckedChanged;
            // 
            // codeFoldingCkb
            // 
            codeFoldingCkb.AutoSize = true;
            codeFoldingCkb.Location = new System.Drawing.Point(18, 183);
            codeFoldingCkb.Name = "codeFoldingCkb";
            codeFoldingCkb.Size = new System.Drawing.Size(99, 19);
            codeFoldingCkb.TabIndex = 13;
            codeFoldingCkb.Text = "Code Folding";
            codeFoldingCkb.UseVisualStyleBackColor = true;
            codeFoldingCkb.CheckedChanged += codeFoldingCkb_CheckedChanged;
            // 
            // closeBtn
            // 
            closeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            closeBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            closeBtn.Location = new System.Drawing.Point(603, 485);
            closeBtn.Name = "closeBtn";
            closeBtn.Size = new System.Drawing.Size(75, 23);
            closeBtn.TabIndex = 14;
            closeBtn.Text = "Close";
            closeBtn.UseVisualStyleBackColor = true;
            closeBtn.Click += closeBtn_Click;
            // 
            // displayGroup
            // 
            displayGroup.Controls.Add(winLoginCkb);
            displayGroup.Controls.Add(startBehaveCkb);
            displayGroup.Controls.Add(behaveSetLbl);
            displayGroup.Controls.Add(displaySepLbl);
            displayGroup.Controls.Add(highlightLbl);
            displayGroup.Controls.Add(highlightCMB);
            displayGroup.Controls.Add(codeFoldingCkb);
            displayGroup.Controls.Add(codeCompletionCkb);
            displayGroup.Controls.Add(lineNumberCkb);
            displayGroup.Location = new System.Drawing.Point(12, 12);
            displayGroup.Name = "displayGroup";
            displayGroup.Size = new System.Drawing.Size(330, 341);
            displayGroup.TabIndex = 15;
            displayGroup.TabStop = false;
            displayGroup.Text = "Settings:";
            // 
            // winLoginCkb
            // 
            winLoginCkb.AutoSize = true;
            winLoginCkb.Location = new System.Drawing.Point(18, 292);
            winLoginCkb.Name = "winLoginCkb";
            winLoginCkb.Size = new System.Drawing.Size(193, 19);
            winLoginCkb.TabIndex = 17;
            winLoginCkb.Text = "Open application on user login";
            winLoginCkb.UseVisualStyleBackColor = true;
            winLoginCkb.CheckedChanged += winLoginCkb_CheckedChanged;
            // 
            // startBehaveCkb
            // 
            startBehaveCkb.AutoSize = true;
            startBehaveCkb.Location = new System.Drawing.Point(18, 257);
            startBehaveCkb.Name = "startBehaveCkb";
            startBehaveCkb.Size = new System.Drawing.Size(228, 19);
            startBehaveCkb.TabIndex = 16;
            startBehaveCkb.Text = "Keep current work sesion on next run";
            startBehaveCkb.UseVisualStyleBackColor = true;
            startBehaveCkb.CheckedChanged += startBehaveCkb_CheckedChanged;
            // 
            // behaveSetLbl
            // 
            behaveSetLbl.AutoSize = true;
            behaveSetLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline);
            behaveSetLbl.Location = new System.Drawing.Point(15, 220);
            behaveSetLbl.Name = "behaveSetLbl";
            behaveSetLbl.Size = new System.Drawing.Size(310, 15);
            behaveSetLbl.TabIndex = 15;
            behaveSetLbl.Text = "Behaviour (requires application restart)             ";
            // 
            // displaySepLbl
            // 
            displaySepLbl.AutoSize = true;
            displaySepLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline);
            displaySepLbl.Location = new System.Drawing.Point(15, 35);
            displaySepLbl.Name = "displaySepLbl";
            displaySepLbl.Size = new System.Drawing.Size(310, 15);
            displaySepLbl.TabIndex = 14;
            displaySepLbl.Text = "Display                                                                ";
            // 
            // buildGroup
            // 
            buildGroup.Controls.Add(publishCkb);
            buildGroup.Controls.Add(unsafeCkb);
            buildGroup.Controls.Add(frameworkLbl);
            buildGroup.Controls.Add(frameWorkCMB);
            buildGroup.Controls.Add(warningsCkb);
            buildGroup.Controls.Add(platformBox);
            buildGroup.Controls.Add(configurationBox);
            buildGroup.Controls.Add(platformLbl);
            buildGroup.Location = new System.Drawing.Point(348, 12);
            buildGroup.Name = "buildGroup";
            buildGroup.Size = new System.Drawing.Size(330, 168);
            buildGroup.TabIndex = 16;
            buildGroup.TabStop = false;
            buildGroup.Text = "Build/Run";
            // 
            // unsafeCkb
            // 
            unsafeCkb.AutoSize = true;
            unsafeCkb.Location = new System.Drawing.Point(15, 85);
            unsafeCkb.Name = "unsafeCkb";
            unsafeCkb.Size = new System.Drawing.Size(118, 19);
            unsafeCkb.TabIndex = 20;
            unsafeCkb.Text = "Use unsafe code";
            unsafeCkb.UseVisualStyleBackColor = true;
            unsafeCkb.CheckedChanged += unsafeCkb_CheckedChanged;
            // 
            // frameworkLbl
            // 
            frameworkLbl.AutoSize = true;
            frameworkLbl.Location = new System.Drawing.Point(12, 137);
            frameworkLbl.Name = "frameworkLbl";
            frameworkLbl.Size = new System.Drawing.Size(185, 15);
            frameworkLbl.TabIndex = 18;
            frameworkLbl.Text = "Target Run/Compile Framework:";
            // 
            // frameWorkCMB
            // 
            frameWorkCMB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            frameWorkCMB.FormattingEnabled = true;
            frameWorkCMB.Items.AddRange(new object[] { ".NET 6", ".NET 6 Windows", ".NET 7", ".NET 7 Windows", ".NET 8", ".NET 8 Windows" });
            frameWorkCMB.Location = new System.Drawing.Point(203, 134);
            frameWorkCMB.Name = "frameWorkCMB";
            frameWorkCMB.Size = new System.Drawing.Size(107, 23);
            frameWorkCMB.TabIndex = 19;
            frameWorkCMB.Text = ".NET 6";
            frameWorkCMB.SelectedIndexChanged += frameWorkCMB_SelectedIndexChanged;
            // 
            // warningsCkb
            // 
            warningsCkb.AutoSize = true;
            warningsCkb.Location = new System.Drawing.Point(15, 60);
            warningsCkb.Name = "warningsCkb";
            warningsCkb.Size = new System.Drawing.Size(165, 19);
            warningsCkb.TabIndex = 14;
            warningsCkb.Text = "Enable compile warnings";
            warningsCkb.UseVisualStyleBackColor = true;
            warningsCkb.CheckedChanged += warningsCkb_CheckedChanged;
            // 
            // platformBox
            // 
            platformBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            platformBox.FormattingEnabled = true;
            platformBox.Items.AddRange(new object[] { "Any CPU", "x64", "x86" });
            platformBox.Location = new System.Drawing.Point(217, 23);
            platformBox.Name = "platformBox";
            platformBox.Size = new System.Drawing.Size(93, 23);
            platformBox.TabIndex = 15;
            platformBox.Text = "Any CPU";
            platformBox.SelectedIndexChanged += platformBox_SelectedIndexChanged;
            // 
            // configurationBox
            // 
            configurationBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            configurationBox.FormattingEnabled = true;
            configurationBox.Items.AddRange(new object[] { "Debug", "Release" });
            configurationBox.Location = new System.Drawing.Point(118, 23);
            configurationBox.Name = "configurationBox";
            configurationBox.Size = new System.Drawing.Size(93, 23);
            configurationBox.TabIndex = 14;
            configurationBox.Text = "Debug";
            configurationBox.SelectedIndexChanged += configurationBox_SelectedIndexChanged;
            // 
            // platformLbl
            // 
            platformLbl.AutoSize = true;
            platformLbl.Location = new System.Drawing.Point(12, 26);
            platformLbl.Name = "platformLbl";
            platformLbl.Size = new System.Drawing.Size(100, 15);
            platformLbl.TabIndex = 0;
            platformLbl.Text = "Params compile:";
            // 
            // liveShareGb
            // 
            liveShareGb.Controls.Add(saveApiUrlBtn);
            liveShareGb.Controls.Add(apiUrlTxt);
            liveShareGb.Controls.Add(apiUrlLbl);
            liveShareGb.Location = new System.Drawing.Point(348, 185);
            liveShareGb.Name = "liveShareGb";
            liveShareGb.Size = new System.Drawing.Size(330, 168);
            liveShareGb.TabIndex = 17;
            liveShareGb.TabStop = false;
            liveShareGb.Text = "Live Share";
            // 
            // saveApiUrlBtn
            // 
            saveApiUrlBtn.Enabled = false;
            saveApiUrlBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            saveApiUrlBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            saveApiUrlBtn.Location = new System.Drawing.Point(237, 124);
            saveApiUrlBtn.Name = "saveApiUrlBtn";
            saveApiUrlBtn.Size = new System.Drawing.Size(75, 23);
            saveApiUrlBtn.TabIndex = 18;
            saveApiUrlBtn.Text = "Save";
            saveApiUrlBtn.UseVisualStyleBackColor = true;
            saveApiUrlBtn.Click += saveApiUrlBtn_Click;
            // 
            // apiUrlTxt
            // 
            apiUrlTxt.Location = new System.Drawing.Point(16, 78);
            apiUrlTxt.Name = "apiUrlTxt";
            apiUrlTxt.Size = new System.Drawing.Size(296, 21);
            apiUrlTxt.TabIndex = 1;
            apiUrlTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            apiUrlTxt.TextChanged += apiUrlTxt_TextChanged;
            // 
            // apiUrlLbl
            // 
            apiUrlLbl.AutoSize = true;
            apiUrlLbl.Location = new System.Drawing.Point(12, 40);
            apiUrlLbl.Name = "apiUrlLbl";
            apiUrlLbl.Size = new System.Drawing.Size(117, 15);
            apiUrlLbl.TabIndex = 0;
            apiUrlLbl.Text = "Live Share API URL:";
            // 
            // openAIGroup
            // 
            openAIGroup.Controls.Add(modelTxt);
            openAIGroup.Controls.Add(label1);
            openAIGroup.Controls.Add(openAISaveBtn);
            openAIGroup.Controls.Add(maxTokensTxtBox);
            openAIGroup.Controls.Add(maxTokensLbl);
            openAIGroup.Controls.Add(apiKeyAiTxtBox);
            openAIGroup.Controls.Add(apiKeyAIlbl);
            openAIGroup.Location = new System.Drawing.Point(12, 359);
            openAIGroup.Name = "openAIGroup";
            openAIGroup.Size = new System.Drawing.Size(666, 110);
            openAIGroup.TabIndex = 18;
            openAIGroup.TabStop = false;
            openAIGroup.Text = "OpenAI";
            // 
            // modelTxt
            // 
            modelTxt.Location = new System.Drawing.Point(311, 64);
            modelTxt.Name = "modelTxt";
            modelTxt.Size = new System.Drawing.Size(166, 21);
            modelTxt.TabIndex = 23;
            modelTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.CausesValidation = false;
            label1.Location = new System.Drawing.Point(260, 66);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(45, 15);
            label1.TabIndex = 22;
            label1.Text = "Model:";
            // 
            // openAISaveBtn
            // 
            openAISaveBtn.Enabled = false;
            openAISaveBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            openAISaveBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            openAISaveBtn.Location = new System.Drawing.Point(573, 75);
            openAISaveBtn.Name = "openAISaveBtn";
            openAISaveBtn.Size = new System.Drawing.Size(75, 23);
            openAISaveBtn.TabIndex = 19;
            openAISaveBtn.Text = "Save";
            openAISaveBtn.UseVisualStyleBackColor = true;
            openAISaveBtn.Click += openAISaveBtn_Click;
            // 
            // maxTokensTxtBox
            // 
            maxTokensTxtBox.Location = new System.Drawing.Point(98, 64);
            maxTokensTxtBox.Name = "maxTokensTxtBox";
            maxTokensTxtBox.Size = new System.Drawing.Size(79, 21);
            maxTokensTxtBox.TabIndex = 21;
            maxTokensTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            maxTokensTxtBox.TextChanged += maxTokensTxtBox_TextChanged;
            // 
            // maxTokensLbl
            // 
            maxTokensLbl.AutoSize = true;
            maxTokensLbl.CausesValidation = false;
            maxTokensLbl.Location = new System.Drawing.Point(15, 66);
            maxTokensLbl.Name = "maxTokensLbl";
            maxTokensLbl.Size = new System.Drawing.Size(77, 15);
            maxTokensLbl.TabIndex = 20;
            maxTokensLbl.Text = "Max Tokens:";
            // 
            // apiKeyAiTxtBox
            // 
            apiKeyAiTxtBox.Location = new System.Drawing.Point(75, 24);
            apiKeyAiTxtBox.Name = "apiKeyAiTxtBox";
            apiKeyAiTxtBox.Size = new System.Drawing.Size(573, 21);
            apiKeyAiTxtBox.TabIndex = 19;
            apiKeyAiTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            apiKeyAiTxtBox.UseSystemPasswordChar = true;
            apiKeyAiTxtBox.TextChanged += apiKeyAiTxtBox_TextChanged;
            // 
            // apiKeyAIlbl
            // 
            apiKeyAIlbl.AutoSize = true;
            apiKeyAIlbl.Location = new System.Drawing.Point(15, 26);
            apiKeyAIlbl.Name = "apiKeyAIlbl";
            apiKeyAIlbl.Size = new System.Drawing.Size(54, 15);
            apiKeyAIlbl.TabIndex = 0;
            apiKeyAIlbl.Text = "API Key: ";
            // 
            // publishCkb
            // 
            publishCkb.AutoSize = true;
            publishCkb.Location = new System.Drawing.Point(15, 110);
            publishCkb.Name = "publishCkb";
            publishCkb.Size = new System.Drawing.Size(128, 19);
            publishCkb.TabIndex = 21;
            publishCkb.Text = "Publish native AOT";
            publishCkb.UseVisualStyleBackColor = true;
            publishCkb.CheckedChanged += publishCkb_CheckedChanged;
            // 
            // Options
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            CancelButton = closeBtn;
            ClientSize = new System.Drawing.Size(690, 525);
            Controls.Add(openAIGroup);
            Controls.Add(liveShareGb);
            Controls.Add(buildGroup);
            Controls.Add(displayGroup);
            Controls.Add(closeBtn);
            Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Options";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Options";
            Load += Options_Load;
            displayGroup.ResumeLayout(false);
            displayGroup.PerformLayout();
            buildGroup.ResumeLayout(false);
            buildGroup.PerformLayout();
            liveShareGb.ResumeLayout(false);
            liveShareGb.PerformLayout();
            openAIGroup.ResumeLayout(false);
            openAIGroup.PerformLayout();
            ResumeLayout(false);
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
        public System.Windows.Forms.ComboBox frameWorkCMB;
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
        private System.Windows.Forms.TextBox modelTxt;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox unsafeCkb;
        private System.Windows.Forms.CheckBox publishCkb;
    }
}