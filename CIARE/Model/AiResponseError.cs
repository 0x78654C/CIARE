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
    public partial class AiResponseError : Form
    {
        private string _editFontSize = "editorFontSizeZoom";
        public AiResponseError()
        {
            InitializeComponent();
        }

        private void AiResponseError_Load(object sender, EventArgs e)
        {
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl, new ComboBox { });
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            textEditorControl.Text = GlobalVariables.errorAiResponse;
            GlobalVariables.errorAiResponse = "";
            InitializeEditor.ReadEditorFontSize(GlobalVariables.registryPath, _editFontSize, textEditorControl);
        }
    }
}
