using CIARE.Utils.Options;
using CIARE.Utils;
using System;
using System.Windows.Forms;
using System.Runtime.Versioning;

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
            if (GlobalVariables.darkColor)
                GUI.DarkMode.ApiUrlCheckDarkMode(this, liveShareGb, apiUrlLbl, apiUrlTxt, saveApiUrlBtn);
        }

        private void apiUrlTxt_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(apiUrlTxt.Text))
                saveApiUrlBtn.Enabled = false;
            else
                saveApiUrlBtn.Enabled = true;
        }
    }
}
