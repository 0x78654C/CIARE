/*
    Display AI response in a new form.
*/
using CIARE.GUI;
using CIARE.Utils;
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("windows")]
    public partial class AiResponse : Form
    {
        private string _editFontSize = "editorFontSizeZoom";
        public AiResponse()
        {
            InitializeComponent();
        }

        private void AiResponseError_Load(object sender, EventArgs e)
        {
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl, new ComboBox { });
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, textEditorControl);
            var model = "";
            if(GlobalVariables.aiTypeVar.Contains("Ollama"))
                model  = GlobalVariables.modelOllamaVar;
            else
                model = GlobalVariables.model;
            Text = $"{GlobalVariables.aiTypeVar} - {model} response:";
            TextBoxWrap();
        }
        
        /// <summary>
        /// Hot keys decalaration.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.End | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        var liensCount = SelectedEditor.GetSelectedEditor().Document.TotalNumberOfLines;
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(liensCount);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = liensCount;
                    }
                    return true;
                case Keys.Home | Keys.Control:
                    if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                    {
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.ScrollTo(0);
                        SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Line = 0;
                    }
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        private void textEditorControl_Resize(object sender, EventArgs e) => TextBoxWrap();

        /// <summary>
        /// Wrap text in the TextBox based on the current width.
        /// </summary>
        private void TextBoxWrap()
        {
            var length = this.Width;
            var textWrap = CustomWrap.CustomWordWrap(GlobalVariables.aiResponse, textEditorControl);
            textEditorControl.Text = textWrap;
        }

        /// <summary>
        /// Clear AI response global variable on form close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AiResponseError_FormClosed(object sender, FormClosedEventArgs e) => GlobalVariables.aiResponse = ""; 
    }
}
