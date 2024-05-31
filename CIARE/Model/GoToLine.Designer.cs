
namespace CIARE
{
    partial class GoToLine
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GoToLine));
            goToLineGB = new System.Windows.Forms.GroupBox();
            cancelBtn = new System.Windows.Forms.Button();
            goToLineBtn = new System.Windows.Forms.Button();
            goToLineNumberTxt = new System.Windows.Forms.TextBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            goToLineGB.SuspendLayout();
            SuspendLayout();
            // 
            // goToLineGB
            // 
            goToLineGB.Controls.Add(cancelBtn);
            goToLineGB.Controls.Add(goToLineBtn);
            goToLineGB.Controls.Add(goToLineNumberTxt);
            goToLineGB.Location = new System.Drawing.Point(14, 14);
            goToLineGB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            goToLineGB.Name = "goToLineGB";
            goToLineGB.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            goToLineGB.Size = new System.Drawing.Size(271, 113);
            goToLineGB.TabIndex = 0;
            goToLineGB.TabStop = false;
            // 
            // cancelBtn
            // 
            cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            cancelBtn.Location = new System.Drawing.Point(176, 70);
            cancelBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new System.Drawing.Size(88, 27);
            cancelBtn.TabIndex = 2;
            cancelBtn.Text = "Cancel";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // goToLineBtn
            // 
            goToLineBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            goToLineBtn.Location = new System.Drawing.Point(7, 70);
            goToLineBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            goToLineBtn.Name = "goToLineBtn";
            goToLineBtn.Size = new System.Drawing.Size(88, 27);
            goToLineBtn.TabIndex = 1;
            goToLineBtn.Text = "Go";
            goToLineBtn.UseVisualStyleBackColor = true;
            goToLineBtn.Click += goToLineBtn_Click;
            // 
            // goToLineNumberTxt
            // 
            goToLineNumberTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            goToLineNumberTxt.Location = new System.Drawing.Point(7, 22);
            goToLineNumberTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            goToLineNumberTxt.Name = "goToLineNumberTxt";
            goToLineNumberTxt.Size = new System.Drawing.Size(256, 22);
            goToLineNumberTxt.TabIndex = 0;
            goToLineNumberTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            goToLineNumberTxt.TextChanged += goToLineNumberTxt_TextChanged;
            // 
            // GoToLine
            // 
            AcceptButton = goToLineBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            CancelButton = cancelBtn;
            ClientSize = new System.Drawing.Size(300, 147);
            Controls.Add(goToLineGB);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GoToLine";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Go To Line";
            Load += GoToLine_Load;
            goToLineGB.ResumeLayout(false);
            goToLineGB.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox goToLineGB;
        private System.Windows.Forms.TextBox goToLineNumberTxt;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button goToLineBtn;
    }
}