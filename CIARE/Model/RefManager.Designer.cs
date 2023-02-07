namespace CIARE.Model
{
    partial class RefManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RefManager));
            this.refLisgGroupBox = new System.Windows.Forms.GroupBox();
            this.refListView = new System.Windows.Forms.ListView();
            this.filePath = new System.Windows.Forms.ColumnHeader();
            this.addRefFileBtn = new System.Windows.Forms.Button();
            this.refLisgGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // refLisgGroupBox
            // 
            this.refLisgGroupBox.Controls.Add(this.refListView);
            this.refLisgGroupBox.Location = new System.Drawing.Point(12, 12);
            this.refLisgGroupBox.Name = "refLisgGroupBox";
            this.refLisgGroupBox.Size = new System.Drawing.Size(732, 310);
            this.refLisgGroupBox.TabIndex = 0;
            this.refLisgGroupBox.TabStop = false;
            this.refLisgGroupBox.Text = "Reference List";
            // 
            // refListView
            // 
            this.refListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.filePath});
            this.refListView.Location = new System.Drawing.Point(6, 22);
            this.refListView.Name = "refListView";
            this.refListView.Size = new System.Drawing.Size(720, 282);
            this.refListView.TabIndex = 0;
            this.refListView.UseCompatibleStateImageBehavior = false;
            this.refListView.View = System.Windows.Forms.View.Details;
            // 
            // filePath
            // 
            this.filePath.Text = "File Path";
            this.filePath.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.filePath.Width = 595;
            // 
            // addRefFileBtn
            // 
            this.addRefFileBtn.Location = new System.Drawing.Point(12, 343);
            this.addRefFileBtn.Name = "addRefFileBtn";
            this.addRefFileBtn.Size = new System.Drawing.Size(106, 28);
            this.addRefFileBtn.TabIndex = 1;
            this.addRefFileBtn.Text = "Add Reference";
            this.addRefFileBtn.UseVisualStyleBackColor = true;
            this.addRefFileBtn.Click += new System.EventHandler(this.addRefFileBtn_Click);
            // 
            // RefManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 397);
            this.Controls.Add(this.addRefFileBtn);
            this.Controls.Add(this.refLisgGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RefManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Reference Manager";
            this.Load += new System.EventHandler(this.RefManager_Load);
            this.refLisgGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox refLisgGroupBox;
        private System.Windows.Forms.ListView refListView;
        private System.Windows.Forms.ColumnHeader @namespace;
        private System.Windows.Forms.ColumnHeader filePath;
        private System.Windows.Forms.Button addRefFileBtn;
    }
}