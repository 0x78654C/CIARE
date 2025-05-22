using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.FilesOpenOS;
using CIARE.Utils.OpenAISettings;
using CIARE.Utils.Options;
using ICSharpCode.TextEditor;
using OllamaInt;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class Options : Form
    {
        int _tokenTxtLen = 0;
        public static Options Instance { get; private set; }
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
            Instance = this;
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, SelectedEditor.GetSelectedEditor(), highlightCMB);
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            codeCompletionCkb.Checked = GlobalVariables.OCodeCompletion;
            lineNumberCkb.Checked = GlobalVariables.OLineNumber;
            codeFoldingCkb.Checked = GlobalVariables.OFoldingCode;
            startBehaveCkb.Checked = GlobalVariables.OStartUp;
            winLoginCkb.Checked = GlobalVariables.OWinLoginState;
            unsafeCkb.Checked = GlobalVariables.OUnsafeCode;
            publishCkb.Checked = GlobalVariables.OPublishNative;
            apiUrlTxt.Text = GlobalVariables.apiUrl;
            CheckOllama();
            var isNullAPIKey = false;
            if (GlobalVariables.aiTypeVar.StartsWith("Ollama"))
            {
                AiTypeCombo.Text = GlobalVariables.aiTypeVar;
                isNullAPIKey = string.IsNullOrEmpty(GlobalVariables.aiKey.ConvertSecureStringToString());
                if (isNullAPIKey)
                    WaterMark.TextBoxWaterMark(apiKeyAiTxtBox, "Enter OpenAI/OpenRouter API key.........");
                else
                    apiKeyAiTxtBox.Text = "******************************************";
                maxTokensTxtBox.Text = GlobalVariables.aiMaxTokens;
                modelLocalCombo.Text = GlobalVariables.modelOllamaVar;
                openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisableCombo(openAISaveBtn, modelLocalCombo, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            }
            else
                FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            AiTypeCombo.Text = GlobalVariables.aiTypeVar;
            isNullAPIKey = string.IsNullOrEmpty(GlobalVariables.aiKey.ConvertSecureStringToString());
            if(isNullAPIKey)
                WaterMark.TextBoxWaterMark(apiKeyAiTxtBox, "Enter OpenAI/OpenRouter API key.........");
            else
                apiKeyAiTxtBox.Text = "******************************************";
            maxTokensTxtBox.Text = GlobalVariables.aiMaxTokens;
            modelTxt.Text = GlobalVariables.model;
            TargetFramework.GetFramework(frameWorkCMB, GlobalVariables.registryPath);
            BuildConfig.SetConfigControl(configurationBox);
            BuildConfig.SetPlatformControl(platformBox);
            _tokenTxtLen = maxTokensTxtBox.Text.Length;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Hightlight set for text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabs = MainForm.Instance.EditorTabControl;
            int count = 0;
            foreach (TabPage tab in tabs.TabPages)
            {
                if (count > 0)
                {
                    Control ctrl = MainForm.Instance.EditorTabControl.Controls[count].Controls[0];
                    var textEditor = ctrl as TextEditorControl;
                    MainForm.Instance.SetHighLighter(textEditor, highlightCMB.Text);
                }
                count++;
            }
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
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

        private void configurationBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (configurationBox.Text == "Release")
                GlobalVariables.configParam = "/p:configuration=Release";
            else
                GlobalVariables.configParam = "/p:configuration=Debug";
            BuildConfig.StoreConfig(GlobalVariables.OConfigParam, GlobalVariables.configParam);
        }

        private void platformBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string platform = platformBox.Text;
            GlobalVariables.platformParam = $"/p:Platform=\"{platform}\"";
            BuildConfig.StorePlatform(GlobalVariables.OPlatformParam, GlobalVariables.platformParam);
        }

        private void startBehaveCkb_CheckedChanged(object sender, EventArgs e)
        {
            StartFilesOS.SetOSStartStatus(startBehaveCkb, GlobalVariables.startUp);
        }

        /// <summary>
        /// Check and set state of Windows login checkbox.
        /// </summary>
        /// <param name="markFileCkb"></param>
        /// <param name="winLoginCkb"></param>
        private void CheckMarkFileActivation(CheckBox markFileCkb, CheckBox winLoginCkb)
        {
            if (markFileCkb.Checked)
                winLoginCkb.Enabled = true;
            else
            {
                winLoginCkb.Enabled = false;
                winLoginCkb.Checked = false;
            }
        }

        private void winLoginCkb_CheckedChanged(object sender, EventArgs e)
        {
            var autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
            autoStartFile.SetRegistryRunApp(winLoginCkb);
            StartFilesOS.SetWinLoginState(winLoginCkb, GlobalVariables.OWinLogin);
        }

        private void frameWorkCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            TargetFramework.SetFramework(frameWorkCMB, GlobalVariables.OFramework);
        }

        /// <summary>
        /// Store API url for live share.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveApiUrlBtn_Click(object sender, EventArgs e)
        {
            LiveShare.SetApiLiveShare(apiUrlTxt, GlobalVariables.liveShare);
            MessageBox.Show("API url was saved!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Enable save button if apiUrlTxt is not empty.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apiUrlTxt_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(apiUrlTxt.Text))
                saveApiUrlBtn.Enabled = true;
            else
                saveApiUrlBtn.Enabled = false;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Store OpenAI API key and max tokens.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openAISaveBtn_Click(object sender, EventArgs e)
        {
            OpenAISetting.SetOpenAIData(apiKeyAiTxtBox, maxTokensTxtBox, modelTxt, AiTypeCombo, modelLocalCombo, GlobalVariables.openAIKey, GlobalVariables.openAIMaxTokens, GlobalVariables.openModel, GlobalVariables.ollamModel, GlobalVariables.aiType);
            MessageBox.Show("AI settings are saved!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Enable save button if apiKeyAiTxtBox is not empty.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apiKeyAiTxtBox_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(apiKeyAiTxtBox.Text))
                openAISaveBtn.Enabled = true;
            else
                openAISaveBtn.Enabled = false;
            FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Check if field contains numbers only on text changed!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void maxTokensTxtBox_TextChanged(object sender, EventArgs e)
        {
            if (!DataManage.IsNumberAllowed(maxTokensTxtBox.Text))
            {
                MessageBox.Show("Field must contain numbers only!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                maxTokensTxtBox.Text = maxTokensTxtBox.Text.Substring(0, _tokenTxtLen);
                maxTokensTxtBox.SelectionStart = maxTokensTxtBox.Text.Length;
                maxTokensTxtBox.ScrollToCaret();
                return;
            }
        }

        /// <summary>
        /// Enable unsafe code use.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unsafeCkb_CheckedChanged(object sender, EventArgs e)
        {
            UnsafeCode.SetUnsafeStatus(unsafeCkb, GlobalVariables.unsafeCode);
        }

        /// <summary>
        /// Enalble publish
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void publishCkb_CheckedChanged(object sender, EventArgs e)
        {
            Publish.SetPublishStatus(publishCkb, GlobalVariables.publish);
        }

        /// <summary>
        /// AI selection logic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AiTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var isOllama = AiTypeCombo.Text.StartsWith("Ollama");
            if (isOllama)
            {
                apiKeyAiTxtBox.Enabled = !isOllama;
                maxTokensTxtBox.Enabled = !isOllama;
                modelTxt.Enabled = !isOllama;
                if (isOllama)
                    AiManage.LoadOllamaModels(ref modelLocalCombo);
                modelLocalCombo.Visible = true;
                modelLocalLbl.Visible = true;
                openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisableCombo(openAISaveBtn, modelLocalCombo, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            }
            else
            {
                apiKeyAiTxtBox.Enabled = true;
                maxTokensTxtBox.Enabled = true;
                modelTxt.Enabled = true;
                modelLocalCombo.Visible = false;
                modelLocalLbl.Visible = false;
                if (string.IsNullOrEmpty(apiKeyAiTxtBox.Text))
                    openAISaveBtn.Enabled = false;
                else
                    openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            }
        }

        /// <summary>
        /// Load ollama in AI combo if installed
        /// </summary>
        private void CheckOllama()
        {
            var client = new OllamaLLM();
            var isOllama = client.IsOllamaInstalled();
            if (!isOllama)
                return;
            AiTypeCombo.Items.Add("Ollama(Local)");
        }
    }
}
