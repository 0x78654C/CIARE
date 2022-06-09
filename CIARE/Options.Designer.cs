
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
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
            this.closeBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeBtn.Location = new System.Drawing.Point(267, 212);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 14;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.highlightLbl);
            this.groupBox1.Controls.Add(this.highlightCMB);
            this.groupBox1.Controls.Add(this.codeFoldingCkb);
            this.groupBox1.Controls.Add(this.codeCompletionCkb);
            this.groupBox1.Controls.Add(this.lineNumberCkb);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(330, 187);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.closeBtn;
            this.ClientSize = new System.Drawing.Size(354, 250);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.closeBtn);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Options";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.Options_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label highlightLbl;
        private System.Windows.Forms.ComboBox highlightCMB;
        private System.Windows.Forms.CheckBox codeCompletionCkb;
        private System.Windows.Forms.CheckBox lineNumberCkb;
        private System.Windows.Forms.CheckBox codeFoldingCkb;
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}