
namespace CIARE
{
    partial class AboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            logoPictureBox = new System.Windows.Forms.PictureBox();
            labelHeaderBar = new System.Windows.Forms.Label();
            labelProductName = new System.Windows.Forms.Label();
            labelTagLine = new System.Windows.Forms.Label();
            labelVersion = new System.Windows.Forms.Label();
            labelCopyright = new System.Windows.Forms.Label();
            labelCompanyName = new System.Windows.Forms.Label();
            labelStatus = new System.Windows.Forms.Label();
            textBoxDescription = new System.Windows.Forms.TextBox();
            okButton = new System.Windows.Forms.Button();
            tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel
            //
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31F));
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 69F));
            tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
            tableLayoutPanel.Controls.Add(labelHeaderBar, 1, 0);
            tableLayoutPanel.Controls.Add(labelProductName, 1, 1);
            tableLayoutPanel.Controls.Add(labelTagLine, 1, 2);
            tableLayoutPanel.Controls.Add(labelVersion, 1, 3);
            tableLayoutPanel.Controls.Add(labelCopyright, 1, 4);
            tableLayoutPanel.Controls.Add(labelCompanyName, 1, 5);
            tableLayoutPanel.Controls.Add(labelStatus, 1, 6);
            tableLayoutPanel.Controls.Add(textBoxDescription, 1, 7);
            tableLayoutPanel.Controls.Add(okButton, 1, 8);
            tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
            tableLayoutPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 9;
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            tableLayoutPanel.Size = new System.Drawing.Size(736, 396);
            tableLayoutPanel.TabIndex = 0;
            //
            // logoPictureBox
            //
            logoPictureBox.BackColor = System.Drawing.SystemColors.Window;
            logoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            logoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            logoPictureBox.Image = (System.Drawing.Image)resources.GetObject("logoPictureBox.Image");
            logoPictureBox.InitialImage = (System.Drawing.Image)resources.GetObject("logoPictureBox.InitialImage");
            logoPictureBox.Location = new System.Drawing.Point(4, 4);
            logoPictureBox.Margin = new System.Windows.Forms.Padding(4);
            logoPictureBox.Name = "logoPictureBox";
            tableLayoutPanel.SetRowSpan(logoPictureBox, 9);
            logoPictureBox.Size = new System.Drawing.Size(220, 388);
            logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            logoPictureBox.TabIndex = 12;
            logoPictureBox.TabStop = false;
            //
            // labelHeaderBar
            //
            labelHeaderBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            labelHeaderBar.Dock = System.Windows.Forms.DockStyle.Fill;
            labelHeaderBar.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelHeaderBar.Location = new System.Drawing.Point(235, 0);
            labelHeaderBar.Margin = new System.Windows.Forms.Padding(7, 0, 0, 4);
            labelHeaderBar.Name = "labelHeaderBar";
            labelHeaderBar.Size = new System.Drawing.Size(501, 24);
            labelHeaderBar.TabIndex = 25;
            labelHeaderBar.Text = " CIARE://SYSTEM/ABOUT";
            labelHeaderBar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // labelProductName
            // 
            labelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            labelProductName.Font = new System.Drawing.Font("Consolas", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelProductName.Location = new System.Drawing.Point(235, 28);
            labelProductName.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelProductName.Name = "labelProductName";
            labelProductName.Size = new System.Drawing.Size(501, 46);
            labelProductName.TabIndex = 19;
            labelProductName.Text = "Product Name";
            labelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // labelTagLine
            //
            labelTagLine.Dock = System.Windows.Forms.DockStyle.Fill;
            labelTagLine.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            labelTagLine.Location = new System.Drawing.Point(235, 74);
            labelTagLine.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelTagLine.Name = "labelTagLine";
            labelTagLine.Size = new System.Drawing.Size(501, 26);
            labelTagLine.TabIndex = 26;
            labelTagLine.Text = "Runtime C# editor / Roslyn command console";
            labelTagLine.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // labelVersion
            // 
            labelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            labelVersion.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            labelVersion.Location = new System.Drawing.Point(235, 100);
            labelVersion.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new System.Drawing.Size(501, 24);
            labelVersion.TabIndex = 0;
            labelVersion.Text = "Version";
            labelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            labelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
            labelCopyright.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            labelCopyright.Location = new System.Drawing.Point(235, 124);
            labelCopyright.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new System.Drawing.Size(501, 24);
            labelCopyright.TabIndex = 21;
            labelCopyright.Text = "Copyright";
            labelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCompanyName
            // 
            labelCompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            labelCompanyName.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            labelCompanyName.Location = new System.Drawing.Point(235, 148);
            labelCompanyName.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelCompanyName.Name = "labelCompanyName";
            labelCompanyName.Size = new System.Drawing.Size(501, 24);
            labelCompanyName.TabIndex = 22;
            labelCompanyName.Text = "Company Name";
            labelCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // labelStatus
            //
            labelStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            labelStatus.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            labelStatus.Location = new System.Drawing.Point(235, 172);
            labelStatus.Margin = new System.Windows.Forms.Padding(7, 0, 0, 0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new System.Drawing.Size(501, 24);
            labelStatus.TabIndex = 27;
            labelStatus.Text = "STATUS: READY";
            labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // textBoxDescription
            // 
            textBoxDescription.BackColor = System.Drawing.SystemColors.Window;
            textBoxDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            textBoxDescription.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBoxDescription.Location = new System.Drawing.Point(235, 200);
            textBoxDescription.Margin = new System.Windows.Forms.Padding(7, 4, 0, 4);
            textBoxDescription.Multiline = true;
            textBoxDescription.Name = "textBoxDescription";
            textBoxDescription.ReadOnly = true;
            textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            textBoxDescription.Size = new System.Drawing.Size(501, 156);
            textBoxDescription.TabIndex = 23;
            textBoxDescription.TabStop = false;
            textBoxDescription.Text = "Description";
            // 
            // okButton
            // 
            okButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            okButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            okButton.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            okButton.Location = new System.Drawing.Point(642, 367);
            okButton.Margin = new System.Windows.Forms.Padding(4);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(90, 25);
            okButton.TabIndex = 24;
            okButton.Text = "&OK";
            // 
            // AboutBox
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(760, 420);
            Controls.Add(tableLayoutPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox";
            Padding = new System.Windows.Forms.Padding(12);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "About CIARE";
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label labelHeaderBar;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.Label labelTagLine;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Label labelCompanyName;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.TextBox textBoxDescription;
        private System.Windows.Forms.Button okButton;
    }
}
