
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
            this.goToLineGB = new System.Windows.Forms.GroupBox();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.goToLineBtn = new System.Windows.Forms.Button();
            this.goToLineNumberTxt = new System.Windows.Forms.TextBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.goToLineGB.SuspendLayout();
            this.SuspendLayout();
            // 
            // goToLineGB
            // 
            this.goToLineGB.Controls.Add(this.cancelBtn);
            this.goToLineGB.Controls.Add(this.goToLineBtn);
            this.goToLineGB.Controls.Add(this.goToLineNumberTxt);
            this.goToLineGB.Location = new System.Drawing.Point(12, 12);
            this.goToLineGB.Name = "goToLineGB";
            this.goToLineGB.Size = new System.Drawing.Size(232, 98);
            this.goToLineGB.TabIndex = 0;
            this.goToLineGB.TabStop = false;
            // 
            // cancelBtn
            // 
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelBtn.Location = new System.Drawing.Point(151, 61);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 23);
            this.cancelBtn.TabIndex = 2;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // goToLineBtn
            // 
            this.goToLineBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.goToLineBtn.Location = new System.Drawing.Point(6, 61);
            this.goToLineBtn.Name = "goToLineBtn";
            this.goToLineBtn.Size = new System.Drawing.Size(75, 23);
            this.goToLineBtn.TabIndex = 1;
            this.goToLineBtn.Text = "Go";
            this.goToLineBtn.UseVisualStyleBackColor = true;
            this.goToLineBtn.Click += new System.EventHandler(this.goToLineBtn_Click);
            // 
            // goToLineNumberTxt
            // 
            this.goToLineNumberTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.goToLineNumberTxt.Location = new System.Drawing.Point(6, 19);
            this.goToLineNumberTxt.Name = "goToLineNumberTxt";
            this.goToLineNumberTxt.Size = new System.Drawing.Size(220, 22);
            this.goToLineNumberTxt.TabIndex = 0;
            this.goToLineNumberTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.goToLineNumberTxt.TextChanged += new System.EventHandler(this.goToLineNumberTxt_TextChanged);
            // 
            // GoToLine
            // 
            this.AcceptButton = this.goToLineBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size(257, 127);
            this.Controls.Add(this.goToLineGB);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GoToLine";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Go To Line";
            this.Load += new System.EventHandler(this.GoToLine_Load);
            this.goToLineGB.ResumeLayout(false);
            this.goToLineGB.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox goToLineGB;
        private System.Windows.Forms.TextBox goToLineNumberTxt;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button goToLineBtn;
    }
}