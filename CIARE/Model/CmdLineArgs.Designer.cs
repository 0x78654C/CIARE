
namespace CIARE
{
    partial class CmdLineArgs
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CmdLineArgs));
            groupBox1 = new System.Windows.Forms.GroupBox();
            cancelBtn = new System.Windows.Forms.Button();
            confirmBtn = new System.Windows.Forms.Button();
            cmdLineArgTxtBox = new System.Windows.Forms.TextBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cancelBtn);
            groupBox1.Controls.Add(confirmBtn);
            groupBox1.Controls.Add(cmdLineArgTxtBox);
            groupBox1.Location = new System.Drawing.Point(14, 14);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(371, 174);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Set Command Line Arguments:";
            // 
            // cancelBtn
            // 
            cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            cancelBtn.Location = new System.Drawing.Point(275, 134);
            cancelBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new System.Drawing.Size(88, 27);
            cancelBtn.TabIndex = 2;
            cancelBtn.Text = "Cancel";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // confirmBtn
            // 
            confirmBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            confirmBtn.Location = new System.Drawing.Point(7, 134);
            confirmBtn.Margin = new System.Windows.Forms.Padding(0);
            confirmBtn.Name = "confirmBtn";
            confirmBtn.Size = new System.Drawing.Size(88, 27);
            confirmBtn.TabIndex = 1;
            confirmBtn.Text = "OK";
            confirmBtn.UseVisualStyleBackColor = true;
            confirmBtn.Click += confirmBtn_Click;
            // 
            // cmdLineArgTxtBox
            // 
            cmdLineArgTxtBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            cmdLineArgTxtBox.Location = new System.Drawing.Point(7, 22);
            cmdLineArgTxtBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cmdLineArgTxtBox.Multiline = true;
            cmdLineArgTxtBox.Name = "cmdLineArgTxtBox";
            cmdLineArgTxtBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            cmdLineArgTxtBox.Size = new System.Drawing.Size(355, 89);
            cmdLineArgTxtBox.TabIndex = 0;
            // 
            // CmdLineArgs
            // 
            AcceptButton = confirmBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            CancelButton = cancelBtn;
            ClientSize = new System.Drawing.Size(401, 207);
            Controls.Add(groupBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CmdLineArgs";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Command Line Arguments";
            Load += CmdLineArgs_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button confirmBtn;
        private System.Windows.Forms.TextBox cmdLineArgTxtBox;
    }
}