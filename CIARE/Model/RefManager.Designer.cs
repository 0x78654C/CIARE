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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RefManager));
            refLisgGroupBox = new System.Windows.Forms.GroupBox();
            refListView = new System.Windows.Forms.ListView();
            nameSpace = new System.Windows.Forms.ColumnHeader();
            filePath = new System.Windows.Forms.ColumnHeader();
            AddRefFileBtn = new System.Windows.Forms.Button();
            CancelBtn = new System.Windows.Forms.Button();
            deleteStrip = new System.Windows.Forms.ContextMenuStrip(components);
            copyNamespaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            NugetManagerBtn = new System.Windows.Forms.Button();
            refLisgGroupBox.SuspendLayout();
            deleteStrip.SuspendLayout();
            SuspendLayout();
            // 
            // refLisgGroupBox
            // 
            refLisgGroupBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            refLisgGroupBox.Controls.Add(refListView);
            refLisgGroupBox.Location = new System.Drawing.Point(12, 12);
            refLisgGroupBox.Name = "refLisgGroupBox";
            refLisgGroupBox.Size = new System.Drawing.Size(878, 387);
            refLisgGroupBox.TabIndex = 0;
            refLisgGroupBox.TabStop = false;
            refLisgGroupBox.Text = "Reference List";
            // 
            // refListView
            // 
            refListView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            refListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { nameSpace, filePath });
            refListView.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            refListView.Location = new System.Drawing.Point(6, 22);
            refListView.MultiSelect = false;
            refListView.Name = "refListView";
            refListView.Size = new System.Drawing.Size(866, 359);
            refListView.TabIndex = 0;
            refListView.UseCompatibleStateImageBehavior = false;
            refListView.View = System.Windows.Forms.View.Details;
            refListView.MouseClick += refListView_MouseClick;
            // 
            // nameSpace
            // 
            nameSpace.Text = "Namespace";
            nameSpace.Width = 220;
            // 
            // filePath
            // 
            filePath.Text = "File Path";
            filePath.Width = 639;
            // 
            // AddRefFileBtn
            // 
            AddRefFileBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            AddRefFileBtn.Location = new System.Drawing.Point(12, 421);
            AddRefFileBtn.Name = "AddRefFileBtn";
            AddRefFileBtn.Size = new System.Drawing.Size(106, 28);
            AddRefFileBtn.TabIndex = 1;
            AddRefFileBtn.Text = "(A)dd Reference";
            AddRefFileBtn.UseVisualStyleBackColor = true;
            AddRefFileBtn.Click += AddRefFileBtn_Click;
            // 
            // CancelBtn
            // 
            CancelBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            CancelBtn.Location = new System.Drawing.Point(784, 421);
            CancelBtn.Name = "CancelBtn";
            CancelBtn.Size = new System.Drawing.Size(106, 28);
            CancelBtn.TabIndex = 2;
            CancelBtn.Text = "Cancel";
            CancelBtn.UseVisualStyleBackColor = true;
            CancelBtn.Click += CancelBtn_Click;
            // 
            // deleteStrip
            // 
            deleteStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { copyNamespaceToolStripMenuItem, deleteToolStripMenuItem });
            deleteStrip.Name = "deleteStrip";
            deleteStrip.Size = new System.Drawing.Size(170, 48);
            deleteStrip.Text = "Delete";
            // 
            // copyNamespaceToolStripMenuItem
            // 
            copyNamespaceToolStripMenuItem.Name = "copyNamespaceToolStripMenuItem";
            copyNamespaceToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            copyNamespaceToolStripMenuItem.Text = "Copy Namespace";
            copyNamespaceToolStripMenuItem.Click += copyNamespaceToolStripMenuItem_Click;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            deleteToolStripMenuItem.Text = "Remove reference";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // NugetManagerBtn
            // 
            NugetManagerBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            NugetManagerBtn.Location = new System.Drawing.Point(124, 421);
            NugetManagerBtn.Name = "NugetManagerBtn";
            NugetManagerBtn.Size = new System.Drawing.Size(110, 28);
            NugetManagerBtn.TabIndex = 3;
            NugetManagerBtn.Text = "(N)uGet Manager ";
            NugetManagerBtn.UseVisualStyleBackColor = true;
            NugetManagerBtn.Click += NugetManagerBtn_Click;
            // 
            // RefManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = CancelBtn;
            ClientSize = new System.Drawing.Size(902, 474);
            Controls.Add(NugetManagerBtn);
            Controls.Add(CancelBtn);
            Controls.Add(AddRefFileBtn);
            Controls.Add(refLisgGroupBox);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "RefManager";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Reference Manager";
            Load += RefManager_Load;
            Resize += RefManager_Resize;
            refLisgGroupBox.ResumeLayout(false);
            deleteStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox refLisgGroupBox;
        public System.Windows.Forms.ListView refListView;
        private System.Windows.Forms.ColumnHeader filePath;
        private System.Windows.Forms.Button AddRefFileBtn;
        private System.Windows.Forms.ColumnHeader nameSpace;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.ContextMenuStrip deleteStrip;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyNamespaceToolStripMenuItem;
        private System.Windows.Forms.Button NugetManagerBtn;
    }
}