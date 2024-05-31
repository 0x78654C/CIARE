
namespace CIARE
{
    partial class FindAndReplace
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindAndReplace));
            groupBox1 = new System.Windows.Forms.GroupBox();
            ignoreCaseCheckBox = new System.Windows.Forms.CheckBox();
            multiReplaceBtn = new System.Windows.Forms.Button();
            singleReplaceBtn = new System.Windows.Forms.Button();
            repalceWithTxt = new System.Windows.Forms.TextBox();
            findTxt = new System.Windows.Forms.TextBox();
            findNReplaceTab = new System.Windows.Forms.TabControl();
            findTab = new System.Windows.Forms.TabPage();
            findGroupBox = new System.Windows.Forms.GroupBox();
            ignCaseSensFindCkb = new System.Windows.Forms.CheckBox();
            findBtn = new System.Windows.Forms.Button();
            findTxtBox = new System.Windows.Forms.TextBox();
            repalceTab = new System.Windows.Forms.TabPage();
            groupBox1.SuspendLayout();
            findNReplaceTab.SuspendLayout();
            findTab.SuspendLayout();
            findGroupBox.SuspendLayout();
            repalceTab.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(ignoreCaseCheckBox);
            groupBox1.Controls.Add(multiReplaceBtn);
            groupBox1.Controls.Add(singleReplaceBtn);
            groupBox1.Controls.Add(repalceWithTxt);
            groupBox1.Controls.Add(findTxt);
            groupBox1.Location = new System.Drawing.Point(7, 7);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(402, 156);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            // 
            // ignoreCaseCheckBox
            // 
            ignoreCaseCheckBox.AutoSize = true;
            ignoreCaseCheckBox.Location = new System.Drawing.Point(26, 113);
            ignoreCaseCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            ignoreCaseCheckBox.Name = "ignoreCaseCheckBox";
            ignoreCaseCheckBox.Size = new System.Drawing.Size(137, 19);
            ignoreCaseCheckBox.TabIndex = 4;
            ignoreCaseCheckBox.Text = "Ignore Case Sensitive";
            ignoreCaseCheckBox.UseVisualStyleBackColor = true;
            ignoreCaseCheckBox.CheckedChanged += ignoreCaseCheckBox_CheckedChanged;
            // 
            // multiReplaceBtn
            // 
            multiReplaceBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            multiReplaceBtn.Location = new System.Drawing.Point(286, 68);
            multiReplaceBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            multiReplaceBtn.Name = "multiReplaceBtn";
            multiReplaceBtn.Size = new System.Drawing.Size(88, 25);
            multiReplaceBtn.TabIndex = 3;
            multiReplaceBtn.Text = "Replace All";
            multiReplaceBtn.UseVisualStyleBackColor = true;
            multiReplaceBtn.Click += multiReplaceBtn_Click;
            // 
            // singleReplaceBtn
            // 
            singleReplaceBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            singleReplaceBtn.Location = new System.Drawing.Point(286, 31);
            singleReplaceBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            singleReplaceBtn.Name = "singleReplaceBtn";
            singleReplaceBtn.Size = new System.Drawing.Size(88, 25);
            singleReplaceBtn.TabIndex = 2;
            singleReplaceBtn.Text = "Replace";
            singleReplaceBtn.UseVisualStyleBackColor = true;
            singleReplaceBtn.Click += singleReplaceBtn_Click;
            // 
            // repalceWithTxt
            // 
            repalceWithTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            repalceWithTxt.Location = new System.Drawing.Point(26, 68);
            repalceWithTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            repalceWithTxt.Name = "repalceWithTxt";
            repalceWithTxt.Size = new System.Drawing.Size(213, 22);
            repalceWithTxt.TabIndex = 1;
            repalceWithTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findTxt
            // 
            findTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            findTxt.Location = new System.Drawing.Point(26, 31);
            findTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findTxt.Name = "findTxt";
            findTxt.Size = new System.Drawing.Size(213, 22);
            findTxt.TabIndex = 0;
            findTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findNReplaceTab
            // 
            findNReplaceTab.Controls.Add(findTab);
            findNReplaceTab.Controls.Add(repalceTab);
            findNReplaceTab.Location = new System.Drawing.Point(14, 14);
            findNReplaceTab.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findNReplaceTab.Name = "findNReplaceTab";
            findNReplaceTab.SelectedIndex = 0;
            findNReplaceTab.Size = new System.Drawing.Size(427, 203);
            findNReplaceTab.TabIndex = 1;
            // 
            // findTab
            // 
            findTab.BackColor = System.Drawing.Color.White;
            findTab.Controls.Add(findGroupBox);
            findTab.Location = new System.Drawing.Point(4, 24);
            findTab.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findTab.Name = "findTab";
            findTab.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findTab.Size = new System.Drawing.Size(419, 175);
            findTab.TabIndex = 0;
            findTab.Text = "Find";
            // 
            // findGroupBox
            // 
            findGroupBox.Controls.Add(ignCaseSensFindCkb);
            findGroupBox.Controls.Add(findBtn);
            findGroupBox.Controls.Add(findTxtBox);
            findGroupBox.Location = new System.Drawing.Point(7, 7);
            findGroupBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findGroupBox.Name = "findGroupBox";
            findGroupBox.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findGroupBox.Size = new System.Drawing.Size(402, 156);
            findGroupBox.TabIndex = 0;
            findGroupBox.TabStop = false;
            // 
            // ignCaseSensFindCkb
            // 
            ignCaseSensFindCkb.AutoSize = true;
            ignCaseSensFindCkb.Location = new System.Drawing.Point(28, 102);
            ignCaseSensFindCkb.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            ignCaseSensFindCkb.Name = "ignCaseSensFindCkb";
            ignCaseSensFindCkb.Size = new System.Drawing.Size(137, 19);
            ignCaseSensFindCkb.TabIndex = 5;
            ignCaseSensFindCkb.Text = "Ignore Case Sensitive";
            ignCaseSensFindCkb.UseVisualStyleBackColor = true;
            ignCaseSensFindCkb.CheckedChanged += ignCaseSensFindCkb_CheckedChanged;
            // 
            // findBtn
            // 
            findBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            findBtn.Location = new System.Drawing.Point(286, 42);
            findBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findBtn.Name = "findBtn";
            findBtn.Size = new System.Drawing.Size(88, 25);
            findBtn.TabIndex = 1;
            findBtn.Text = "Find";
            findBtn.UseVisualStyleBackColor = true;
            findBtn.Click += findBtn_Click;
            // 
            // findTxtBox
            // 
            findTxtBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            findTxtBox.Location = new System.Drawing.Point(28, 43);
            findTxtBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            findTxtBox.Name = "findTxtBox";
            findTxtBox.Size = new System.Drawing.Size(213, 22);
            findTxtBox.TabIndex = 0;
            findTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // repalceTab
            // 
            repalceTab.Controls.Add(groupBox1);
            repalceTab.Location = new System.Drawing.Point(4, 24);
            repalceTab.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            repalceTab.Name = "repalceTab";
            repalceTab.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            repalceTab.Size = new System.Drawing.Size(419, 175);
            repalceTab.TabIndex = 1;
            repalceTab.Text = "Replace";
            repalceTab.UseVisualStyleBackColor = true;
            // 
            // FindAndReplace
            // 
            AcceptButton = findBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(448, 228);
            Controls.Add(findNReplaceTab);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FindAndReplace";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Find and replace";
            Load += FindAndReplace_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            findNReplaceTab.ResumeLayout(false);
            findTab.ResumeLayout(false);
            findGroupBox.ResumeLayout(false);
            findGroupBox.PerformLayout();
            repalceTab.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button multiReplaceBtn;
        private System.Windows.Forms.Button singleReplaceBtn;
        private System.Windows.Forms.TextBox repalceWithTxt;
        private System.Windows.Forms.TextBox findTxt;
        private System.Windows.Forms.CheckBox ignoreCaseCheckBox;
        private System.Windows.Forms.TabControl findNReplaceTab;
        private System.Windows.Forms.TabPage findTab;
        private System.Windows.Forms.TabPage repalceTab;
        private System.Windows.Forms.GroupBox findGroupBox;
        private System.Windows.Forms.Button findBtn;
        private System.Windows.Forms.TextBox findTxtBox;
        private System.Windows.Forms.CheckBox ignCaseSensFindCkb;
    }
}