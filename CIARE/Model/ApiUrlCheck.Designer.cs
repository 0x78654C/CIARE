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
            this.liveShareGb = new System.Windows.Forms.GroupBox();
            this.saveApiUrlBtn = new System.Windows.Forms.Button();
            this.apiUrlTxt = new System.Windows.Forms.TextBox();
            this.apiUrlLbl = new System.Windows.Forms.Label();
            this.liveShareGb.SuspendLayout();
            this.SuspendLayout();
            // 
            // liveShareGb
            // 
            this.liveShareGb.Controls.Add(this.saveApiUrlBtn);
            this.liveShareGb.Controls.Add(this.apiUrlTxt);
            this.liveShareGb.Controls.Add(this.apiUrlLbl);
            this.liveShareGb.Location = new System.Drawing.Point(12, 12);
            this.liveShareGb.Name = "liveShareGb";
            this.liveShareGb.Size = new System.Drawing.Size(330, 142);
            this.liveShareGb.TabIndex = 18;
            this.liveShareGb.TabStop = false;
            // 
            // saveApiUrlBtn
            // 
            this.saveApiUrlBtn.Enabled = false;
            this.saveApiUrlBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveApiUrlBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.saveApiUrlBtn.Location = new System.Drawing.Point(231, 103);
            this.saveApiUrlBtn.Name = "saveApiUrlBtn";
            this.saveApiUrlBtn.Size = new System.Drawing.Size(75, 23);
            this.saveApiUrlBtn.TabIndex = 18;
            this.saveApiUrlBtn.Text = "Save";
            this.saveApiUrlBtn.UseVisualStyleBackColor = true;
            this.saveApiUrlBtn.Click += new System.EventHandler(this.saveApiUrlBtn_Click);
            // 
            // apiUrlTxt
            // 
            this.apiUrlTxt.Location = new System.Drawing.Point(10, 57);
            this.apiUrlTxt.Name = "apiUrlTxt";
            this.apiUrlTxt.Size = new System.Drawing.Size(296, 23);
            this.apiUrlTxt.TabIndex = 1;
            this.apiUrlTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.apiUrlTxt.TextChanged += new System.EventHandler(this.apiUrlTxt_TextChanged);
            // 
            // apiUrlLbl
            // 
            this.apiUrlLbl.AutoSize = true;
            this.apiUrlLbl.Location = new System.Drawing.Point(6, 19);
            this.apiUrlLbl.Name = "apiUrlLbl";
            this.apiUrlLbl.Size = new System.Drawing.Size(108, 15);
            this.apiUrlLbl.TabIndex = 0;
            this.apiUrlLbl.Text = "Live Share API URL:";
            // 
            // ApiUrlCheck
            // 
            this.AcceptButton = this.saveApiUrlBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(354, 172);
            this.Controls.Add(this.liveShareGb);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiUrlCheck";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CIARE - Save API URL";
            this.Load += new System.EventHandler(this.ApiUrlCheck_Load);
            this.liveShareGb.ResumeLayout(false);
            this.liveShareGb.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox liveShareGb;
        private System.Windows.Forms.Button saveApiUrlBtn;
        private System.Windows.Forms.TextBox apiUrlTxt;
        private System.Windows.Forms.Label apiUrlLbl;
    }
}