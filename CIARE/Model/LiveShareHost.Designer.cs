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
            liveShareStartGrp = new System.Windows.Forms.GroupBox();
            passwordTxt = new System.Windows.Forms.TextBox();
            passwordLbl = new System.Windows.Forms.Label();
            startLiveBtn = new System.Windows.Forms.Button();
            sessionTxt = new System.Windows.Forms.TextBox();
            sessionIdLbl = new System.Windows.Forms.Label();
            remoteGrp = new System.Windows.Forms.GroupBox();
            remotePasswordTxt = new System.Windows.Forms.TextBox();
            remotePassLbl = new System.Windows.Forms.Label();
            connectHostBtn = new System.Windows.Forms.Button();
            remoteSessioniDtxt = new System.Windows.Forms.TextBox();
            remoteSessionLbl = new System.Windows.Forms.Label();
            liveShareStartGrp.SuspendLayout();
            remoteGrp.SuspendLayout();
            SuspendLayout();
            // 
            // liveShareStartGrp
            // 
            liveShareStartGrp.Controls.Add(passwordTxt);
            liveShareStartGrp.Controls.Add(passwordLbl);
            liveShareStartGrp.Controls.Add(startLiveBtn);
            liveShareStartGrp.Controls.Add(sessionTxt);
            liveShareStartGrp.Controls.Add(sessionIdLbl);
            liveShareStartGrp.Location = new System.Drawing.Point(12, 12);
            liveShareStartGrp.Name = "liveShareStartGrp";
            liveShareStartGrp.Size = new System.Drawing.Size(255, 213);
            liveShareStartGrp.TabIndex = 0;
            liveShareStartGrp.TabStop = false;
            liveShareStartGrp.Text = "Live Share Host";
            // 
            // passwordTxt
            // 
            passwordTxt.BackColor = System.Drawing.SystemColors.Window;
            passwordTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            passwordTxt.Location = new System.Drawing.Point(9, 113);
            passwordTxt.Name = "passwordTxt";
            passwordTxt.Size = new System.Drawing.Size(236, 25);
            passwordTxt.TabIndex = 4;
            passwordTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // passwordLbl
            // 
            passwordLbl.AutoSize = true;
            passwordLbl.Location = new System.Drawing.Point(6, 95);
            passwordLbl.Name = "passwordLbl";
            passwordLbl.Size = new System.Drawing.Size(60, 15);
            passwordLbl.TabIndex = 3;
            passwordLbl.Text = "Password:";
            // 
            // startLiveBtn
            // 
            startLiveBtn.BackColor = System.Drawing.SystemColors.Window;
            startLiveBtn.Location = new System.Drawing.Point(71, 160);
            startLiveBtn.Name = "startLiveBtn";
            startLiveBtn.Size = new System.Drawing.Size(101, 28);
            startLiveBtn.TabIndex = 1;
            startLiveBtn.Text = "Start Live Share";
            startLiveBtn.UseVisualStyleBackColor = false;
            startLiveBtn.Click += startLiveBtn_Click;
            // 
            // sessionTxt
            // 
            sessionTxt.BackColor = System.Drawing.SystemColors.Window;
            sessionTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            sessionTxt.Location = new System.Drawing.Point(9, 59);
            sessionTxt.Name = "sessionTxt";
            sessionTxt.ReadOnly = true;
            sessionTxt.Size = new System.Drawing.Size(236, 25);
            sessionTxt.TabIndex = 2;
            sessionTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // sessionIdLbl
            // 
            sessionIdLbl.AutoSize = true;
            sessionIdLbl.Location = new System.Drawing.Point(6, 41);
            sessionIdLbl.Name = "sessionIdLbl";
            sessionIdLbl.Size = new System.Drawing.Size(62, 15);
            sessionIdLbl.TabIndex = 0;
            sessionIdLbl.Text = "Session Id:";
            // 
            // remoteGrp
            // 
            remoteGrp.Controls.Add(remotePasswordTxt);
            remoteGrp.Controls.Add(remotePassLbl);
            remoteGrp.Controls.Add(connectHostBtn);
            remoteGrp.Controls.Add(remoteSessioniDtxt);
            remoteGrp.Controls.Add(remoteSessionLbl);
            remoteGrp.Location = new System.Drawing.Point(284, 12);
            remoteGrp.Name = "remoteGrp";
            remoteGrp.Size = new System.Drawing.Size(255, 213);
            remoteGrp.TabIndex = 5;
            remoteGrp.TabStop = false;
            remoteGrp.Text = "Remote Host Connect";
            // 
            // remotePasswordTxt
            // 
            remotePasswordTxt.BackColor = System.Drawing.SystemColors.Window;
            remotePasswordTxt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            remotePasswordTxt.Location = new System.Drawing.Point(9, 113);
            remotePasswordTxt.Name = "remotePasswordTxt";
            remotePasswordTxt.Size = new System.Drawing.Size(236, 25);
            remotePasswordTxt.TabIndex = 4;
            remotePasswordTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // remotePassLbl
            // 
            remotePassLbl.AutoSize = true;
            remotePassLbl.Location = new System.Drawing.Point(6, 95);
            remotePassLbl.Name = "remotePassLbl";
            remotePassLbl.Size = new System.Drawing.Size(104, 15);
            remotePassLbl.TabIndex = 3;
            remotePassLbl.Text = "Remote Password:";
            // 
            // connectHostBtn
            // 
            connectHostBtn.BackColor = System.Drawing.SystemColors.Window;
            connectHostBtn.Location = new System.Drawing.Point(70, 160);
            connectHostBtn.Name = "connectHostBtn";
            connectHostBtn.Size = new System.Drawing.Size(108, 28);
            connectHostBtn.TabIndex = 1;
            connectHostBtn.Text = "Remote Connect";
            connectHostBtn.UseVisualStyleBackColor = false;
            connectHostBtn.Click += connectHostBtn_Click;
            // 
            // remoteSessioniDtxt
            // 
            remoteSessioniDtxt.BackColor = System.Drawing.SystemColors.Window;
            remoteSessioniDtxt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            remoteSessioniDtxt.Location = new System.Drawing.Point(9, 59);
            remoteSessioniDtxt.Name = "remoteSessioniDtxt";
            remoteSessioniDtxt.Size = new System.Drawing.Size(236, 25);
            remoteSessioniDtxt.TabIndex = 2;
            remoteSessioniDtxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // remoteSessionLbl
            // 
            remoteSessionLbl.AutoSize = true;
            remoteSessionLbl.Location = new System.Drawing.Point(6, 41);
            remoteSessionLbl.Name = "remoteSessionLbl";
            remoteSessionLbl.Size = new System.Drawing.Size(106, 15);
            remoteSessionLbl.TabIndex = 0;
            remoteSessionLbl.Text = "Remote Session Id:";
            // 
            // LiveShareHost
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(551, 241);
            Controls.Add(remoteGrp);
            Controls.Add(liveShareStartGrp);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "LiveShareHost";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Live Share Management";
            FormClosed += LiveShareHost_FormClosed;
            Load += LiveShareHost_Load;
            liveShareStartGrp.ResumeLayout(false);
            liveShareStartGrp.PerformLayout();
            remoteGrp.ResumeLayout(false);
            remoteGrp.PerformLayout();
            ResumeLayout(false);
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