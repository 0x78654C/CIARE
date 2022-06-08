using System;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.Options;

namespace CIARE
{
    public partial class Options : Form
    {
        public Options()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Closes the current form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Options_Load(object sender, EventArgs e)
        {
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, Form1.Instance.textEditorControl1, highlightCMB);
            if (GlobalVariables.darkColor)
                DarkMode.OptionsDarkMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb);
            codeCompletionCkb.Checked = GlobalVariables.OCodeCompletion;
            lineNumberCkb.Checked = GlobalVariables.OLineNumber;
            codeFoldingCkb.Checked = GlobalVariables.OFoldingCode;
        }

        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Form1.Instance.SetHighLighter(highlightCMB.Text);
            if (GlobalVariables.darkColor)
                DarkMode.OptionsDarkMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb);
            else
                LightMode.OptionsLightMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb);
        }

        private void codeCompletionCkb_CheckedChanged(object sender, EventArgs e)
        {
            CodeCompletion.SetCodeCompletionStatus(codeCompletionCkb, GlobalVariables.codeCompletionKey);
        }

        private void lineNumberCkb_CheckedChanged(object sender, EventArgs e)
        {
            LineNumber.SetLineNumberStatus(lineNumberCkb, GlobalVariables.lineNumberKey);
        }

        private void codeFoldingCkb_CheckedChanged(object sender, EventArgs e)
        {
            FoldingCode.SetFoldingCodeStatus(codeFoldingCkb, GlobalVariables.foldingCodeKey);
        }
    }
}
