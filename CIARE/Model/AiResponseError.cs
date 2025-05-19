/*
    Display AI response in a new form.
*/
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor.Document;
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace CIARE.Model
{
    [SupportedOSPlatform("windows")]
    public partial class AiResponseError : Form
    {
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
        }
    }
}
