namespace CIARE.Model
{
    partial class AskAI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AskAI));
            askAiTxt = new System.Windows.Forms.TextBox();
            askBtn = new System.Windows.Forms.Button();
            diplayCodeCkb = new System.Windows.Forms.CheckBox();
            SuspendLayout();
            // 
            // askAiTxt
            // 
            askAiTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            askAiTxt.Location = new System.Drawing.Point(12, 12);
            askAiTxt.Multiline = true;
            askAiTxt.Name = "askAiTxt";
            askAiTxt.Size = new System.Drawing.Size(686, 43);
            askAiTxt.TabIndex = 0;
            // 
            // askBtn
            // 
            askBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            askBtn.Location = new System.Drawing.Point(712, 12);
            askBtn.Name = "askBtn";
            askBtn.Size = new System.Drawing.Size(75, 43);
            askBtn.TabIndex = 1;
            askBtn.Text = "Ask";
            askBtn.UseVisualStyleBackColor = true;
            askBtn.Click += askBtn_Click;
            // 
            // diplayCodeCkb
            // 
            diplayCodeCkb.AutoSize = true;
            diplayCodeCkb.Location = new System.Drawing.Point(584, 58);
            diplayCodeCkb.Name = "diplayCodeCkb";
            diplayCodeCkb.Size = new System.Drawing.Size(119, 19);
            diplayCodeCkb.TabIndex = 2;
            diplayCodeCkb.Text = "Display only code";
            diplayCodeCkb.UseVisualStyleBackColor = true;
            diplayCodeCkb.CheckedChanged += diplayCodeCkb_CheckedChanged;
            // 
            // AskAI
            // 
            AcceptButton = askBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 79);
            Controls.Add(diplayCodeCkb);
            Controls.Add(askBtn);
            Controls.Add(askAiTxt);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AskAI";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Ask AI:";
            Load += AskAI_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox askAiTxt;
        private System.Windows.Forms.Button askBtn;
        private System.Windows.Forms.CheckBox diplayCodeCkb;
    }
}