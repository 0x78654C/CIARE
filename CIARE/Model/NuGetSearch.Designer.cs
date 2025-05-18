using System.Windows.Forms;

namespace CIARE.Model
{
    partial class NuGetSearch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NuGetSearch));
            SearchBox = new System.Windows.Forms.TextBox();
            SearchBtn = new System.Windows.Forms.Button();
            packageList = new System.Windows.Forms.ListView();
            packageName = new System.Windows.Forms.ColumnHeader();
            versoion = new System.Windows.Forms.ColumnHeader();
            description = new System.Windows.Forms.ColumnHeader();
            ActionNugetMenu = new System.Windows.Forms.ContextMenuStrip(components);
            addToReference = new System.Windows.Forms.ToolStripMenuItem();
            copyPackageName = new System.Windows.Forms.ToolStripMenuItem();
            downloadLbl = new System.Windows.Forms.Label();
            downloadBar = new System.Windows.Forms.ProgressBar();
            ActionNugetMenu.SuspendLayout();
            SuspendLayout();
            // 
            // SearchBox
            // 
            SearchBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            SearchBox.Location = new System.Drawing.Point(267, 15);
            SearchBox.Name = "SearchBox";
            SearchBox.Size = new System.Drawing.Size(430, 23);
            SearchBox.TabIndex = 0;
            // 
            // SearchBtn
            // 
            SearchBtn.Anchor = System.Windows.Forms.AnchorStyles.Top;
            SearchBtn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            SearchBtn.Location = new System.Drawing.Point(714, 15);
            SearchBtn.Name = "SearchBtn";
            SearchBtn.Size = new System.Drawing.Size(92, 23);
            SearchBtn.TabIndex = 1;
            SearchBtn.Text = "Search";
            SearchBtn.UseVisualStyleBackColor = true;
            SearchBtn.Click += SearchBtn_Click;
            // 
            // packageList
            // 
            packageList.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            packageList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { packageName, versoion, description });
            packageList.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            packageList.Location = new System.Drawing.Point(12, 56);
            packageList.MultiSelect = false;
            packageList.Name = "packageList";
            packageList.Size = new System.Drawing.Size(1067, 512);
            packageList.TabIndex = 2;
            packageList.UseCompatibleStateImageBehavior = false;
            packageList.View = System.Windows.Forms.View.Details;
            packageList.DoubleClick += packageList_DoubleClick;
            packageList.MouseClick += packageList_MouseClick;
            // 
            // packageName
            // 
            packageName.Text = "Package Name";
            packageName.Width = 220;
            // 
            // versoion
            // 
            versoion.Text = "Version";
            // 
            // description
            // 
            description.Text = "Description";
            description.Width = 780;
            // 
            // ActionNugetMenu
            // 
            ActionNugetMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { addToReference, copyPackageName });
            ActionNugetMenu.Name = "ActionNugetMenu";
            ActionNugetMenu.Size = new System.Drawing.Size(291, 48);
            // 
            // addToReference
            // 
            addToReference.Name = "addToReference";
            addToReference.Size = new System.Drawing.Size(290, 22);
            addToReference.Text = "Download Package and add to Reference";
            addToReference.Click += addToReference_Click;
            // 
            // copyPackageName
            // 
            copyPackageName.Name = "copyPackageName";
            copyPackageName.Size = new System.Drawing.Size(290, 22);
            copyPackageName.Text = "Copy Package Name";
            copyPackageName.Click += copyPackageName_Click;
            // 
            // downloadLbl
            // 
            downloadLbl.Anchor = System.Windows.Forms.AnchorStyles.None;
            downloadLbl.AutoSize = true;
            downloadLbl.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            downloadLbl.Location = new System.Drawing.Point(459, 257);
            downloadLbl.Name = "downloadLbl";
            downloadLbl.Size = new System.Drawing.Size(140, 15);
            downloadLbl.TabIndex = 3;
            downloadLbl.Text = "Downloading package ....";
            downloadLbl.Visible = false;
            // 
            // downloadBar
            // 
            downloadBar.Anchor = System.Windows.Forms.AnchorStyles.None;
            downloadBar.Location = new System.Drawing.Point(398, 294);
            downloadBar.MarqueeAnimationSpeed = 30;
            downloadBar.Name = "downloadBar";
            downloadBar.Size = new System.Drawing.Size(265, 23);
            downloadBar.TabIndex = 4;
            downloadBar.Visible = false;
            downloadBar.Style = ProgressBarStyle.Marquee;
            // 
            // NuGetSearch
            // 
            AcceptButton = SearchBtn;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1091, 580);
            Controls.Add(downloadLbl);
            Controls.Add(downloadBar);
            Controls.Add(packageList);
            Controls.Add(SearchBtn);
            Controls.Add(SearchBox);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "NuGetSearch";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "NuGet Package Manager";
            Load += NuGetSearch_Load;
            Resize += NuGetSearch_Resize;
            ActionNugetMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Button SearchBtn;
        private System.Windows.Forms.ListView packageList;
        private System.Windows.Forms.ColumnHeader packageName;
        private System.Windows.Forms.ColumnHeader description;
        private System.Windows.Forms.ColumnHeader versoion;
        private System.Windows.Forms.ContextMenuStrip ActionNugetMenu;
        private System.Windows.Forms.ToolStripMenuItem copyPackageName;
        private System.Windows.Forms.ToolStripMenuItem addToReference;
        private System.Windows.Forms.Label downloadLbl;
        private System.Windows.Forms.ProgressBar downloadBar;
    }
}