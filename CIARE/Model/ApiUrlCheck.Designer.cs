namespace CIARE
{
    partial class ApiUrlCheck
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUrlCheck));
            liveShareGb = new System.Windows.Forms.GroupBox();
            saveApiUrlBtn = new System.Windows.Forms.Button();
            apiUrlTxt = new System.Windows.Forms.TextBox();
            apiUrlLbl = new System.Windows.Forms.Label();
            liveShareGb.SuspendLayout();
            SuspendLayout();
            // 
            // liveShareGb
            // 
            liveShareGb.Controls.Add(saveApiUrlBtn);
            liveShareGb.Controls.Add(apiUrlTxt);
            liveShareGb.Controls.Add(apiUrlLbl);
            liveShareGb.Location = new System.Drawing.Point(12, 12);
            liveShareGb.Name = "liveShareGb";
            liveShareGb.Size = new System.Drawing.Size(330, 142);
            liveShareGb.TabIndex = 18;
            liveShareGb.TabStop = false;
            // 
            // saveApiUrlBtn
            // 
            saveApiUrlBtn.Enabled = false;
            saveApiUrlBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            saveApiUrlBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            saveApiUrlBtn.Location = new System.Drawing.Point(249, 113);
            saveApiUrlBtn.Name = "saveApiUrlBtn";
            saveApiUrlBtn.Size = new System.Drawing.Size(75, 23);
            saveApiUrlBtn.TabIndex = 18;
            saveApiUrlBtn.Text = "Save";
            saveApiUrlBtn.UseVisualStyleBackColor = true;
            saveApiUrlBtn.Click += saveApiUrlBtn_Click;
            // 
            // apiUrlTxt
            // 
            apiUrlTxt.Location = new System.Drawing.Point(6, 57);
            apiUrlTxt.Name = "apiUrlTxt";
            apiUrlTxt.Size = new System.Drawing.Size(318, 23);
            apiUrlTxt.TabIndex = 1;
            apiUrlTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            apiUrlTxt.TextChanged += apiUrlTxt_TextChanged;
            // 
            // apiUrlLbl
            // 
            apiUrlLbl.AutoSize = true;
            apiUrlLbl.Location = new System.Drawing.Point(6, 19);
            apiUrlLbl.Name = "apiUrlLbl";
            apiUrlLbl.Size = new System.Drawing.Size(108, 15);
            apiUrlLbl.TabIndex = 0;
            apiUrlLbl.Text = "Live Share API URL:";
            // 
            // ApiUrlCheck
            // 
            AcceptButton = saveApiUrlBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(354, 172);
            Controls.Add(liveShareGb);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ApiUrlCheck";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "CIARE - Save API URL";
            Load += ApiUrlCheck_Load;
            liveShareGb.ResumeLayout(false);
            liveShareGb.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox liveShareGb;
        private System.Windows.Forms.Button saveApiUrlBtn;
        private System.Windows.Forms.TextBox apiUrlTxt;
        private System.Windows.Forms.Label apiUrlLbl;
    }
}