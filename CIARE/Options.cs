using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.FilesOpenOS;
using CIARE.Utils.Options;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class Options : Form
    {
        /// <summary>
        /// TODO: add compile param /p:Platform=
        /// </summary>
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
                DarkMode.OptionsDarkMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb, displayGroup, buildGroup, displaySepLbl, behaveSetLbl, startBehaveCkb,
                    apiUrlLbl,apiUrlTxt,saveApiUrlBtn, liveShareGb);
            codeCompletionCkb.Checked = GlobalVariables.OCodeCompletion;
            lineNumberCkb.Checked = GlobalVariables.OLineNumber;
            codeFoldingCkb.Checked = GlobalVariables.OFoldingCode;
            warningsCkb.Checked = GlobalVariables.OWarnings;
            startBehaveCkb.Checked = GlobalVariables.OStartUp;
            winLoginCkb.Checked = GlobalVariables.OWinLoginState;
            apiUrlTxt.Text = GlobalVariables.apiUrl;
            CheckMarkFileActivation(startBehaveCkb, winLoginCkb);
            TargetFramework.GetFramework(frameWorkCMB, GlobalVariables.registryPath);
            BuildConfig.SetConfigControl(configurationBox);
            BuildConfig.SetPlatformControl(platformBox);
        }

        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Form1.Instance.SetHighLighter(highlightCMB.Text);
            if (GlobalVariables.darkColor)
                DarkMode.OptionsDarkMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb, displayGroup, buildGroup, displaySepLbl, behaveSetLbl, startBehaveCkb,
                    apiUrlLbl, apiUrlTxt, saveApiUrlBtn, liveShareGb);
            else
                LightMode.OptionsLightMode(this, closeBtn, highlightLbl, highlightCMB, codeCompletionCkb, lineNumberCkb, codeFoldingCkb, displayGroup, buildGroup, displaySepLbl, behaveSetLbl, startBehaveCkb,
                    apiUrlLbl, apiUrlTxt, saveApiUrlBtn, liveShareGb);
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


        /// <summary>
        /// Enable build warnings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void warningsCkb_CheckedChanged(object sender, EventArgs e)
        {
            Warnings.SetWarnings(warningsCkb, GlobalVariables.warnings);
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
            if (platformBox.Text == "Any CPU")
                GlobalVariables.platformParam = "/p:Platform=\"Any CPU\"";
            else
                GlobalVariables.platformParam = "/p:Platform=\"x64\"";
            BuildConfig.StorePlatform(GlobalVariables.OPlatformParam, GlobalVariables.platformParam);
        }

        private void startBehaveCkb_CheckedChanged(object sender, EventArgs e)
        {
            StartFilesOS.SetOSStartStatus(startBehaveCkb, GlobalVariables.startUp);
            CheckMarkFileActivation(startBehaveCkb, winLoginCkb);
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
            if (string.IsNullOrEmpty(GlobalVariables.openedFilePath))
            {
                winLoginCkb.Checked = false;
                return;
            }

            var autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
            if (!autoStartFile.CheckFileContent(GlobalVariables.markFile))
            {
                winLoginCkb.Checked = false;
                return;
            }
            StartFilesOS.SetWinLoginState(winLoginCkb, GlobalVariables.OWinLogin);
            autoStartFile.SetRegistryRunApp(winLoginCkb);
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
            MessageBox.Show("API url was saved!");
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
        }
    }
}
