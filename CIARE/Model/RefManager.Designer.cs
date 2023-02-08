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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RefManager));
            this.refLisgGroupBox = new System.Windows.Forms.GroupBox();
            this.refListView = new System.Windows.Forms.ListView();
            this.nameSpace = new System.Windows.Forms.ColumnHeader();
            this.filePath = new System.Windows.Forms.ColumnHeader();
            this.AddRefFileBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.deleteStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyNamespaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refLisgGroupBox.SuspendLayout();
            this.deleteStrip.SuspendLayout();
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
            this.nameSpace,
            this.filePath});
            this.refListView.Location = new System.Drawing.Point(6, 22);
            this.refListView.Name = "refListView";
            this.refListView.Size = new System.Drawing.Size(720, 282);
            this.refListView.TabIndex = 0;
            this.refListView.UseCompatibleStateImageBehavior = false;
            this.refListView.View = System.Windows.Forms.View.Details;
            this.refListView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.refListView_MouseClick);
            // 
            // nameSpace
            // 
            this.nameSpace.Text = "Namespace";
            this.nameSpace.Width = 132;
            // 
            // filePath
            // 
            this.filePath.Text = "File Path";
            this.filePath.Width = 580;
            // 
            // AddRefFileBtn
            // 
            this.AddRefFileBtn.Location = new System.Drawing.Point(12, 343);
            this.AddRefFileBtn.Name = "AddRefFileBtn";
            this.AddRefFileBtn.Size = new System.Drawing.Size(106, 28);
            this.AddRefFileBtn.TabIndex = 1;
            this.AddRefFileBtn.Text = "Add Reference";
            this.AddRefFileBtn.UseVisualStyleBackColor = true;
            this.AddRefFileBtn.Click += new System.EventHandler(this.AddRefFileBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Location = new System.Drawing.Point(638, 343);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(106, 28);
            this.CancelBtn.TabIndex = 2;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // deleteStrip
            // 
            this.deleteStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem,
            this.copyNamespaceToolStripMenuItem});
            this.deleteStrip.Name = "deleteStrip";
            this.deleteStrip.Size = new System.Drawing.Size(181, 70);
            this.deleteStrip.Text = "Delete";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.deleteToolStripMenuItem.Text = "Delete reference";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // copyNamespaceToolStripMenuItem
            // 
            this.copyNamespaceToolStripMenuItem.Name = "copyNamespaceToolStripMenuItem";
            this.copyNamespaceToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.copyNamespaceToolStripMenuItem.Text = "Copy Namespace";
            this.copyNamespaceToolStripMenuItem.Click += new System.EventHandler(this.copyNamespaceToolStripMenuItem_Click);
            // 
            // RefManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(756, 397);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.AddRefFileBtn);
            this.Controls.Add(this.refLisgGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RefManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Reference Manager";
            this.Load += new System.EventHandler(this.RefManager_Load);
            this.refLisgGroupBox.ResumeLayout(false);
            this.deleteStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox refLisgGroupBox;
        private System.Windows.Forms.ListView refListView;
        private System.Windows.Forms.ColumnHeader @namespace;
        private System.Windows.Forms.ColumnHeader filePath;
        private System.Windows.Forms.Button AddRefFileBtn;
        private System.Windows.Forms.ColumnHeader nameSpace;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.ContextMenuStrip deleteStrip;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyNamespaceToolStripMenuItem;
    }
}