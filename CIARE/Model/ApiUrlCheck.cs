﻿using CIARE.Utils.Options;
using CIARE.Utils;
using System;
using System.Windows.Forms;
using System.Runtime.Versioning;
using CIARE.GUI;

namespace CIARE
{
    /* Form for store api url on live share management access. */

    [SupportedOSPlatform("windows")]
    public partial class ApiUrlCheck : Form
    {
        public ApiUrlCheck()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Overwrite the key press.
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

        private void saveApiUrlBtn_Click(object sender, EventArgs e)
        {
            StoreLiveShareApiUrl();
        }

        /// <summary>
        /// Store live share api url in registry
        /// </summary>
        private void StoreLiveShareApiUrl()
        {
            LiveShare.SetApiLiveShare(apiUrlTxt, GlobalVariables.liveShare);
            MessageBox.Show("API url was saved!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void ApiUrlCheck_Load(object sender, EventArgs e)
        {
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            if (string.IsNullOrEmpty(apiUrlTxt.Text))
                saveApiUrlBtn.Enabled = false;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        private void apiUrlTxt_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(apiUrlTxt.Text))
                saveApiUrlBtn.Enabled = false;
            else
                saveApiUrlBtn.Enabled = true;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }
    }
}
