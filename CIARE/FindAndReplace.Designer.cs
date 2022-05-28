
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ignoreCaseCheckBox = new System.Windows.Forms.CheckBox();
            this.multiReplaceBtn = new System.Windows.Forms.Button();
            this.singleReplaceBtn = new System.Windows.Forms.Button();
            this.repalceWithTxt = new System.Windows.Forms.TextBox();
            this.findTxt = new System.Windows.Forms.TextBox();
            this.findNReplaceTab = new System.Windows.Forms.TabControl();
            this.findTab = new System.Windows.Forms.TabPage();
            this.findGroupBox = new System.Windows.Forms.GroupBox();
            this.ignCaseSensFindCkb = new System.Windows.Forms.CheckBox();
            this.findBtn = new System.Windows.Forms.Button();
            this.findTxtBox = new System.Windows.Forms.TextBox();
            this.repalceTab = new System.Windows.Forms.TabPage();
            this.groupBox1.SuspendLayout();
            this.findNReplaceTab.SuspendLayout();
            this.findTab.SuspendLayout();
            this.findGroupBox.SuspendLayout();
            this.repalceTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ignoreCaseCheckBox);
            this.groupBox1.Controls.Add(this.multiReplaceBtn);
            this.groupBox1.Controls.Add(this.singleReplaceBtn);
            this.groupBox1.Controls.Add(this.repalceWithTxt);
            this.groupBox1.Controls.Add(this.findTxt);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(345, 135);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // ignoreCaseCheckBox
            // 
            this.ignoreCaseCheckBox.AutoSize = true;
            this.ignoreCaseCheckBox.Location = new System.Drawing.Point(22, 98);
            this.ignoreCaseCheckBox.Name = "ignoreCaseCheckBox";
            this.ignoreCaseCheckBox.Size = new System.Drawing.Size(129, 17);
            this.ignoreCaseCheckBox.TabIndex = 4;
            this.ignoreCaseCheckBox.Text = "Ignore Case Sensitive";
            this.ignoreCaseCheckBox.UseVisualStyleBackColor = true;
            this.ignoreCaseCheckBox.CheckedChanged += new System.EventHandler(this.ignoreCaseCheckBox_CheckedChanged);
            // 
            // multiReplaceBtn
            // 
            this.multiReplaceBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.multiReplaceBtn.Location = new System.Drawing.Point(245, 59);
            this.multiReplaceBtn.Name = "multiReplaceBtn";
            this.multiReplaceBtn.Size = new System.Drawing.Size(75, 22);
            this.multiReplaceBtn.TabIndex = 3;
            this.multiReplaceBtn.Text = "Replace All";
            this.multiReplaceBtn.UseVisualStyleBackColor = true;
            this.multiReplaceBtn.Click += new System.EventHandler(this.multiReplaceBtn_Click);
            // 
            // singleReplaceBtn
            // 
            this.singleReplaceBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.singleReplaceBtn.Location = new System.Drawing.Point(245, 27);
            this.singleReplaceBtn.Name = "singleReplaceBtn";
            this.singleReplaceBtn.Size = new System.Drawing.Size(75, 22);
            this.singleReplaceBtn.TabIndex = 2;
            this.singleReplaceBtn.Text = "Replace";
            this.singleReplaceBtn.UseVisualStyleBackColor = true;
            this.singleReplaceBtn.Click += new System.EventHandler(this.singleReplaceBtn_Click);
            // 
            // repalceWithTxt
            // 
            this.repalceWithTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.repalceWithTxt.Location = new System.Drawing.Point(22, 59);
            this.repalceWithTxt.Name = "repalceWithTxt";
            this.repalceWithTxt.Size = new System.Drawing.Size(183, 22);
            this.repalceWithTxt.TabIndex = 1;
            this.repalceWithTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findTxt
            // 
            this.findTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.findTxt.Location = new System.Drawing.Point(22, 27);
            this.findTxt.Name = "findTxt";
            this.findTxt.Size = new System.Drawing.Size(183, 22);
            this.findTxt.TabIndex = 0;
            this.findTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findNReplaceTab
            // 
            this.findNReplaceTab.Controls.Add(this.findTab);
            this.findNReplaceTab.Controls.Add(this.repalceTab);
            this.findNReplaceTab.Location = new System.Drawing.Point(12, 12);
            this.findNReplaceTab.Name = "findNReplaceTab";
            this.findNReplaceTab.SelectedIndex = 0;
            this.findNReplaceTab.Size = new System.Drawing.Size(366, 176);
            this.findNReplaceTab.TabIndex = 1;
            // 
            // findTab
            // 
            this.findTab.BackColor = System.Drawing.Color.White;
            this.findTab.Controls.Add(this.findGroupBox);
            this.findTab.Location = new System.Drawing.Point(4, 22);
            this.findTab.Name = "findTab";
            this.findTab.Padding = new System.Windows.Forms.Padding(3);
            this.findTab.Size = new System.Drawing.Size(358, 150);
            this.findTab.TabIndex = 0;
            this.findTab.Text = "Find";
            // 
            // findGroupBox
            // 
            this.findGroupBox.Controls.Add(this.ignCaseSensFindCkb);
            this.findGroupBox.Controls.Add(this.findBtn);
            this.findGroupBox.Controls.Add(this.findTxtBox);
            this.findGroupBox.Location = new System.Drawing.Point(6, 6);
            this.findGroupBox.Name = "findGroupBox";
            this.findGroupBox.Size = new System.Drawing.Size(346, 138);
            this.findGroupBox.TabIndex = 0;
            this.findGroupBox.TabStop = false;
            // 
            // ignCaseSensFindCkb
            // 
            this.ignCaseSensFindCkb.AutoSize = true;
            this.ignCaseSensFindCkb.Location = new System.Drawing.Point(24, 88);
            this.ignCaseSensFindCkb.Name = "ignCaseSensFindCkb";
            this.ignCaseSensFindCkb.Size = new System.Drawing.Size(129, 17);
            this.ignCaseSensFindCkb.TabIndex = 5;
            this.ignCaseSensFindCkb.Text = "Ignore Case Sensitive";
            this.ignCaseSensFindCkb.UseVisualStyleBackColor = true;
            this.ignCaseSensFindCkb.CheckedChanged += new System.EventHandler(this.ignCaseSensFindCkb_CheckedChanged);
            // 
            // findBtn
            // 
            this.findBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.findBtn.Location = new System.Drawing.Point(249, 37);
            this.findBtn.Name = "findBtn";
            this.findBtn.Size = new System.Drawing.Size(75, 22);
            this.findBtn.TabIndex = 1;
            this.findBtn.Text = "Find";
            this.findBtn.UseVisualStyleBackColor = true;
            this.findBtn.Click += new System.EventHandler(this.findBtn_Click);
            // 
            // findTxtBox
            // 
            this.findTxtBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.findTxtBox.Location = new System.Drawing.Point(24, 37);
            this.findTxtBox.Name = "findTxtBox";
            this.findTxtBox.Size = new System.Drawing.Size(183, 22);
            this.findTxtBox.TabIndex = 0;
            this.findTxtBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // repalceTab
            // 
            this.repalceTab.Controls.Add(this.groupBox1);
            this.repalceTab.Location = new System.Drawing.Point(4, 22);
            this.repalceTab.Name = "repalceTab";
            this.repalceTab.Padding = new System.Windows.Forms.Padding(3);
            this.repalceTab.Size = new System.Drawing.Size(358, 150);
            this.repalceTab.TabIndex = 1;
            this.repalceTab.Text = "Replace";
            this.repalceTab.UseVisualStyleBackColor = true;
            // 
            // FindAndReplace
            // 
            this.AcceptButton = this.findBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(384, 198);
            this.Controls.Add(this.findNReplaceTab);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FindAndReplace";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Find and replace";
            this.Load += new System.EventHandler(this.FindAndReplace_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.findNReplaceTab.ResumeLayout(false);
            this.findTab.ResumeLayout(false);
            this.findGroupBox.ResumeLayout(false);
            this.findGroupBox.PerformLayout();
            this.repalceTab.ResumeLayout(false);
            this.ResumeLayout(false);

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