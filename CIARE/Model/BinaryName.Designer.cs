
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
            typeApp = new System.Windows.Forms.ComboBox();
            typeCompileCkb = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
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
            ConfirmButton.Location = new System.Drawing.Point(14, 82);
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
            cancelButton.Location = new System.Drawing.Point(155, 82);
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
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(typeApp);
            groupBox1.Controls.Add(typeCompileCkb);
            groupBox1.Controls.Add(binaryNameTxt);
            groupBox1.Controls.Add(cancelButton);
            groupBox1.Controls.Add(ConfirmButton);
            groupBox1.Location = new System.Drawing.Point(14, 14);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(259, 159);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            // 
            // typeApp
            // 
            typeApp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            typeApp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            typeApp.FormattingEnabled = true;
            typeApp.Items.AddRange(new object[] { ".exe", ".dll" });
            typeApp.Location = new System.Drawing.Point(100, 122);
            typeApp.Name = "typeApp";
            typeApp.Size = new System.Drawing.Size(57, 23);
            typeApp.TabIndex = 4;
            typeApp.SelectedIndexChanged += typeApp_SelectedIndexChanged;
            // 
            // typeCompileCkb
            // 
            typeCompileCkb.AutoSize = true;
            typeCompileCkb.Location = new System.Drawing.Point(59, 55);
            typeCompileCkb.Name = "typeCompileCkb";
            typeCompileCkb.Size = new System.Drawing.Size(139, 19);
            typeCompileCkb.TabIndex = 3;
            typeCompileCkb.Text = "Windows Application";
            typeCompileCkb.UseVisualStyleBackColor = true;
            typeCompileCkb.Visible = false;
            typeCompileCkb.CheckedChanged += typeCompileCkb_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(59, 125);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(34, 15);
            label1.TabIndex = 4;
            label1.Text = "Type:";
            // 
            // BinaryName
            // 
            AcceptButton = ConfirmButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(286, 185);
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
        private System.Windows.Forms.ComboBox typeApp;
        private System.Windows.Forms.Label label1;
    }
}