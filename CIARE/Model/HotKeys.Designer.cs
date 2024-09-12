namespace CIARE.GUI
{
    partial class HotKeys
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HotKeys));
            textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            SuspendLayout();
            // 
            // textEditorControl1
            // 
            textEditorControl1.AllowDrop = true;
            textEditorControl1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textEditorControl1.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textEditorControl1.Highlighting = "C#-Dark";
            textEditorControl1.Location = new System.Drawing.Point(12, 12);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.ReadOnly = true;
            textEditorControl1.ShowLineNumbers = false;
            textEditorControl1.ShowVRuler = false;
            textEditorControl1.Size = new System.Drawing.Size(631, 806);
            textEditorControl1.TabIndex = 0;
            textEditorControl1.Text = resources.GetString("textEditorControl1.Text");
            // 
            // HotKeys
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(655, 830);
            Controls.Add(textEditorControl1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "HotKeys";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "HotKeys";
            Load += HotKeys_Load;
            ResumeLayout(false);
        }

        #endregion

        private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
    }
}