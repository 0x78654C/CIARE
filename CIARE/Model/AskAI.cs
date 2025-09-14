using CIARE.GUI;
using CIARE.Utils;
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("windows")]

    public partial class AskAI : Form
    {
        private string _displayCode = "";
        public AskAI()
        {
            InitializeComponent();
        }

        private void AskAI_Load(object sender, EventArgs e)
        {
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            bool isSelected = false;
            var aiType = GlobalVariables.aiTypeVar;
            var model = "";
            if (aiType.Contains("Ollama"))
                model = GlobalVariables.modelOllamaVar;
            else
                model = GlobalVariables.model;
            GetSelectedText(out isSelected);
            Text = (isSelected) ? $"Ask AI for selected text ({aiType} - {model}):" : $"Ask AI ({aiType} - {model}):";
        }

        /// <summary>
        /// Store question in global parameter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void askBtn_Click(object sender, EventArgs e)
        {
            var selectedText = GetSelectedText(out _);
            GlobalVariables.aiQuestion = (string.IsNullOrEmpty(selectedText)) ? $"{askAiTxt.Text}. {_displayCode}" : $"{selectedText}\n {askAiTxt}. {_displayCode}";
            if (string.IsNullOrWhiteSpace(GlobalVariables.aiQuestion))
            {
                MessageBox.Show("No question provided!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.Close();
        }

        /// <summary>
        /// Get selected text.
        /// </summary>
        /// <param name="isSelected"></param>
        /// <returns></returns>
        private string GetSelectedText(out bool isSelected)
        {
            var selectedText = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.SelectionManager.SelectedText;
            isSelected = !string.IsNullOrEmpty(selectedText);
            return selectedText;
        }

        /// <summary>
        /// Overwrite Excape button to exit form.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    this.Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Add prompt to display code only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void diplayCodeCkb_CheckedChanged(object sender, EventArgs e)
        {
            _displayCode = (diplayCodeCkb.Checked) ? " Display only code." : "";
        }
    }
}
