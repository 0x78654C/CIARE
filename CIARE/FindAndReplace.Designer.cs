
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
            this.multiReplaceBtn = new System.Windows.Forms.Button();
            this.singleReplaceBtn = new System.Windows.Forms.Button();
            this.repalceWithTxt = new System.Windows.Forms.TextBox();
            this.findTxt = new System.Windows.Forms.TextBox();
            this.ignoreCaseCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ignoreCaseCheckBox);
            this.groupBox1.Controls.Add(this.multiReplaceBtn);
            this.groupBox1.Controls.Add(this.singleReplaceBtn);
            this.groupBox1.Controls.Add(this.repalceWithTxt);
            this.groupBox1.Controls.Add(this.findTxt);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(345, 135);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // multiReplaceBtn
            // 
            this.multiReplaceBtn.Location = new System.Drawing.Point(245, 56);
            this.multiReplaceBtn.Name = "multiReplaceBtn";
            this.multiReplaceBtn.Size = new System.Drawing.Size(75, 23);
            this.multiReplaceBtn.TabIndex = 3;
            this.multiReplaceBtn.Text = "Replace All";
            this.multiReplaceBtn.UseVisualStyleBackColor = true;
            this.multiReplaceBtn.Click += new System.EventHandler(this.multiReplaceBtn_Click);
            // 
            // singleReplaceBtn
            // 
            this.singleReplaceBtn.Location = new System.Drawing.Point(245, 26);
            this.singleReplaceBtn.Name = "singleReplaceBtn";
            this.singleReplaceBtn.Size = new System.Drawing.Size(75, 23);
            this.singleReplaceBtn.TabIndex = 2;
            this.singleReplaceBtn.Text = "Replace";
            this.singleReplaceBtn.UseVisualStyleBackColor = true;
            this.singleReplaceBtn.Click += new System.EventHandler(this.singleReplaceBtn_Click);
            // 
            // repalceWithTxt
            // 
            this.repalceWithTxt.Location = new System.Drawing.Point(22, 58);
            this.repalceWithTxt.Name = "repalceWithTxt";
            this.repalceWithTxt.Size = new System.Drawing.Size(183, 20);
            this.repalceWithTxt.TabIndex = 1;
            this.repalceWithTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // findTxt
            // 
            this.findTxt.Location = new System.Drawing.Point(22, 28);
            this.findTxt.Name = "findTxt";
            this.findTxt.Size = new System.Drawing.Size(183, 20);
            this.findTxt.TabIndex = 0;
            this.findTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            // FindAndReplace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(369, 159);
            this.Controls.Add(this.groupBox1);
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
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button multiReplaceBtn;
        private System.Windows.Forms.Button singleReplaceBtn;
        private System.Windows.Forms.TextBox repalceWithTxt;
        private System.Windows.Forms.TextBox findTxt;
        private System.Windows.Forms.CheckBox ignoreCaseCheckBox;
    }
}