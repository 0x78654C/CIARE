
namespace CIARE
{
    partial class BinaryName
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BinaryName));
            binaryNameTxt = new System.Windows.Forms.TextBox();
            ConfirmButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            typeCompileCkb = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // binaryNameTxt
            // 
            binaryNameTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            binaryNameTxt.Location = new System.Drawing.Point(14, 22);
            binaryNameTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            binaryNameTxt.Name = "binaryNameTxt";
            binaryNameTxt.Size = new System.Drawing.Size(228, 22);
            binaryNameTxt.TabIndex = 0;
            binaryNameTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ConfirmButton
            // 
            ConfirmButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            ConfirmButton.Location = new System.Drawing.Point(14, 73);
            ConfirmButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            ConfirmButton.Name = "ConfirmButton";
            ConfirmButton.Size = new System.Drawing.Size(88, 27);
            ConfirmButton.TabIndex = 1;
            ConfirmButton.Text = "OK";
            ConfirmButton.UseVisualStyleBackColor = true;
            ConfirmButton.Click += ConfirmButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            cancelButton.Location = new System.Drawing.Point(155, 73);
            cancelButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(88, 27);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(typeCompileCkb);
            groupBox1.Controls.Add(binaryNameTxt);
            groupBox1.Controls.Add(cancelButton);
            groupBox1.Controls.Add(ConfirmButton);
            groupBox1.Location = new System.Drawing.Point(14, 14);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(259, 111);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            // 
            // typeCompileCkb
            // 
            typeCompileCkb.AutoSize = true;
            typeCompileCkb.Location = new System.Drawing.Point(59, 50);
            typeCompileCkb.Name = "typeCompileCkb";
            typeCompileCkb.Size = new System.Drawing.Size(139, 19);
            typeCompileCkb.TabIndex = 3;
            typeCompileCkb.Text = "Windows Application";
            typeCompileCkb.UseVisualStyleBackColor = true;
            typeCompileCkb.Visible = false;
            typeCompileCkb.CheckedChanged += typeCompileCkb_CheckedChanged;
            // 
            // BinaryName
            // 
            AcceptButton = ConfirmButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(286, 145);
            Controls.Add(groupBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BinaryName";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Set Binary Name";
            FormClosed += BinaryName_FormClosed;
            Load += BinaryName_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TextBox binaryNameTxt;
        private System.Windows.Forms.Button ConfirmButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox typeCompileCkb;
    }
}