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
            Text = $"{GlobalVariables.aiTypeVar} response:";
            TextBoxWrap();
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
