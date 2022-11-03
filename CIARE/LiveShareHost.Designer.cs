namespace CIARE
{
    partial class LiveShareHost
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LiveShareHost));
            this.liveShareStartGrp = new System.Windows.Forms.GroupBox();
            this.passwordTxt = new System.Windows.Forms.TextBox();
            this.passwordLbl = new System.Windows.Forms.Label();
            this.startLiveBtn = new System.Windows.Forms.Button();
            this.sessionTxt = new System.Windows.Forms.TextBox();
            this.sessionIdLbl = new System.Windows.Forms.Label();
            this.remoteGrp = new System.Windows.Forms.GroupBox();
            this.remotePasswordTxt = new System.Windows.Forms.TextBox();
            this.remotePassLbl = new System.Windows.Forms.Label();
            this.connectHostBtn = new System.Windows.Forms.Button();
            this.remoteSessioniDtxt = new System.Windows.Forms.TextBox();
            this.remoteSessionLbl = new System.Windows.Forms.Label();
            this.liveShareStartGrp.SuspendLayout();
            this.remoteGrp.SuspendLayout();
            this.SuspendLayout();
            // 
            // liveShareStartGrp
            // 
            this.liveShareStartGrp.Controls.Add(this.passwordTxt);
            this.liveShareStartGrp.Controls.Add(this.passwordLbl);
            this.liveShareStartGrp.Controls.Add(this.startLiveBtn);
            this.liveShareStartGrp.Controls.Add(this.sessionTxt);
            this.liveShareStartGrp.Controls.Add(this.sessionIdLbl);
            this.liveShareStartGrp.Location = new System.Drawing.Point(12, 12);
            this.liveShareStartGrp.Name = "liveShareStartGrp";
            this.liveShareStartGrp.Size = new System.Drawing.Size(255, 213);
            this.liveShareStartGrp.TabIndex = 0;
            this.liveShareStartGrp.TabStop = false;
            this.liveShareStartGrp.Text = "Live Share Host";
            // 
            // passwordTxt
            // 
            this.passwordTxt.BackColor = System.Drawing.SystemColors.Window;
            this.passwordTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.passwordTxt.Location = new System.Drawing.Point(9, 113);
            this.passwordTxt.Name = "passwordTxt";
            this.passwordTxt.Size = new System.Drawing.Size(236, 25);
            this.passwordTxt.TabIndex = 4;
            this.passwordTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // passwordLbl
            // 
            this.passwordLbl.AutoSize = true;
            this.passwordLbl.Location = new System.Drawing.Point(6, 95);
            this.passwordLbl.Name = "passwordLbl";
            this.passwordLbl.Size = new System.Drawing.Size(60, 15);
            this.passwordLbl.TabIndex = 3;
            this.passwordLbl.Text = "Password:";
            // 
            // startLiveBtn
            // 
            this.startLiveBtn.BackColor = System.Drawing.SystemColors.Window;
            this.startLiveBtn.Location = new System.Drawing.Point(71, 160);
            this.startLiveBtn.Name = "startLiveBtn";
            this.startLiveBtn.Size = new System.Drawing.Size(101, 28);
            this.startLiveBtn.TabIndex = 1;
            this.startLiveBtn.Text = "Start Live Share";
            this.startLiveBtn.UseVisualStyleBackColor = false;
            this.startLiveBtn.Click += new System.EventHandler(this.startLiveBtn_Click);
            // 
            // sessionTxt
            // 
            this.sessionTxt.BackColor = System.Drawing.SystemColors.Window;
            this.sessionTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.sessionTxt.Location = new System.Drawing.Point(9, 59);
            this.sessionTxt.Name = "sessionTxt";
            this.sessionTxt.ReadOnly = true;
            this.sessionTxt.Size = new System.Drawing.Size(236, 25);
            this.sessionTxt.TabIndex = 2;
            this.sessionTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // sessionIdLbl
            // 
            this.sessionIdLbl.AutoSize = true;
            this.sessionIdLbl.Location = new System.Drawing.Point(6, 41);
            this.sessionIdLbl.Name = "sessionIdLbl";
            this.sessionIdLbl.Size = new System.Drawing.Size(62, 15);
            this.sessionIdLbl.TabIndex = 0;
            this.sessionIdLbl.Text = "Session Id:";
            // 
            // remoteGrp
            // 
            this.remoteGrp.Controls.Add(this.remotePasswordTxt);
            this.remoteGrp.Controls.Add(this.remotePassLbl);
            this.remoteGrp.Controls.Add(this.connectHostBtn);
            this.remoteGrp.Controls.Add(this.remoteSessioniDtxt);
            this.remoteGrp.Controls.Add(this.remoteSessionLbl);
            this.remoteGrp.Location = new System.Drawing.Point(284, 12);
            this.remoteGrp.Name = "remoteGrp";
            this.remoteGrp.Size = new System.Drawing.Size(255, 213);
            this.remoteGrp.TabIndex = 5;
            this.remoteGrp.TabStop = false;
            this.remoteGrp.Text = "Remote Host Connect";
            // 
            // remotePasswordTxt
            // 
            this.remotePasswordTxt.BackColor = System.Drawing.SystemColors.Window;
            this.remotePasswordTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.remotePasswordTxt.Location = new System.Drawing.Point(9, 113);
            this.remotePasswordTxt.Name = "remotePasswordTxt";
            this.remotePasswordTxt.Size = new System.Drawing.Size(236, 25);
            this.remotePasswordTxt.TabIndex = 4;
            this.remotePasswordTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // remotePassLbl
            // 
            this.remotePassLbl.AutoSize = true;
            this.remotePassLbl.Location = new System.Drawing.Point(6, 95);
            this.remotePassLbl.Name = "remotePassLbl";
            this.remotePassLbl.Size = new System.Drawing.Size(104, 15);
            this.remotePassLbl.TabIndex = 3;
            this.remotePassLbl.Text = "Remote Password:";
            // 
            // connectHostBtn
            // 
            this.connectHostBtn.BackColor = System.Drawing.SystemColors.Window;
            this.connectHostBtn.Location = new System.Drawing.Point(72, 160);
            this.connectHostBtn.Name = "connectHostBtn";
            this.connectHostBtn.Size = new System.Drawing.Size(101, 28);
            this.connectHostBtn.TabIndex = 1;
            this.connectHostBtn.Text = "Connect";
            this.connectHostBtn.UseVisualStyleBackColor = false;
            // 
            // remoteSessioniDtxt
            // 
            this.remoteSessioniDtxt.BackColor = System.Drawing.SystemColors.Window;
            this.remoteSessioniDtxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.remoteSessioniDtxt.Location = new System.Drawing.Point(9, 59);
            this.remoteSessioniDtxt.Name = "remoteSessioniDtxt";
            this.remoteSessioniDtxt.ReadOnly = true;
            this.remoteSessioniDtxt.Size = new System.Drawing.Size(236, 25);
            this.remoteSessioniDtxt.TabIndex = 2;
            this.remoteSessioniDtxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // remoteSessionLbl
            // 
            this.remoteSessionLbl.AutoSize = true;
            this.remoteSessionLbl.Location = new System.Drawing.Point(6, 41);
            this.remoteSessionLbl.Name = "remoteSessionLbl";
            this.remoteSessionLbl.Size = new System.Drawing.Size(106, 15);
            this.remoteSessionLbl.TabIndex = 0;
            this.remoteSessionLbl.Text = "Remote Session Id:";
            // 
            // LiveShareHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(551, 241);
            this.Controls.Add(this.remoteGrp);
            this.Controls.Add(this.liveShareStartGrp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "LiveShareHost";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Live Share Management";
            this.Load += new System.EventHandler(this.LiveShareHost_Load);
            this.liveShareStartGrp.ResumeLayout(false);
            this.liveShareStartGrp.PerformLayout();
            this.remoteGrp.ResumeLayout(false);
            this.remoteGrp.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox liveShareStartGrp;
        private System.Windows.Forms.Button startLiveBtn;
        private System.Windows.Forms.TextBox sessionTxt;
        private System.Windows.Forms.Label sessionIdLbl;
        private System.Windows.Forms.TextBox passwordTxt;
        private System.Windows.Forms.Label passwordLbl;
        private System.Windows.Forms.GroupBox remoteGrp;
        private System.Windows.Forms.TextBox remotePasswordTxt;
        private System.Windows.Forms.Label remotePassLbl;
        private System.Windows.Forms.Button connectHostBtn;
        private System.Windows.Forms.TextBox remoteSessioniDtxt;
        private System.Windows.Forms.Label remoteSessionLbl;
    }
}