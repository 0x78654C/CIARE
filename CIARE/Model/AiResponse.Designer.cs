namespace CIARE.Model
{
    partial class AiResponse
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AiResponse));
            textEditorControl = new ICSharpCode.TextEditor.TextEditorControl();
            SuspendLayout();
            // 
            // textEditorControl
            // 
            textEditorControl.AllowDrop = true;
            textEditorControl.Dock = System.Windows.Forms.DockStyle.Fill;
            textEditorControl.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textEditorControl.Highlighting = "C#-Dark";
            textEditorControl.Location = new System.Drawing.Point(0, 0);
            textEditorControl.Name = "textEditorControl";
            textEditorControl.ReadOnly = true;
            textEditorControl.ShowLineNumbers = false;
            textEditorControl.ShowVRuler = false;
            textEditorControl.Size = new System.Drawing.Size(649, 661);
            textEditorControl.TabIndex = 1;
            textEditorControl.Resize += textEditorControl_Resize;
            // 
            // AiResponseError
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(649, 661);
            Controls.Add(textEditorControl);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "AiResponseError";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "AI respond on error message";
            FormClosed += AiResponseError_FormClosed;
            Load += AiResponseError_Load;
            ResumeLayout(false);
        }

        #endregion

        private ICSharpCode.TextEditor.TextEditorControl textEditorControl;
    }
}