using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class LiveShareHost : Form
    {
        public LiveShareHost()
        {
            InitializeComponent();
        }

        private void LiveShareHost_Load(object sender, EventArgs e)
        {
            // Check if api url exist.
            CheckApiUrl(ref GlobalVariables.apiUrl);

            // Set dark mode if enabled.
            if (GlobalVariables.darkColor)
                DarkMode.LiveShareDarkMode(this, liveShareStartGrp, sessionIdLbl, sessionTxt, passwordLbl, passwordTxt, startLiveBtn,
                    remoteGrp, remoteSessionLbl, remoteSessioniDtxt, remotePassLbl, remotePasswordTxt, connectHostBtn);

            // Generate secure encryption password.
            GeneratePassword(passwordTxt, GlobalVariables.livePassword, GlobalVariables.apiRemoteConnected);

            // Display current session id.
            // Generated only on first app run.
            SetSessionId(sessionTxt, GlobalVariables.sessionIdMain);

            // Set live share button text depending on live connection.
            SetLiveButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, startLiveBtn, connectHostBtn);

            // Set remote button text depending on live connection.
            SetRemoteButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, connectHostBtn, startLiveBtn);

            // Set remote session id in textbox.
            SetRemoteSessionId(remoteSessioniDtxt, GlobalVariables.remoteSessionId, GlobalVariables.apiRemoteConnected);

            // Set remote password in textbox.
            SetRemotePassword(remotePasswordTxt, GlobalVariables.livePassword, GlobalVariables.apiRemoteConnected);

            // Set live share button dark mode for disable status.
            SetColorButtonsOnDisable(startLiveBtn, GlobalVariables.darkColor);

            // Set remote connect button dark mode for disable status.
            SetColorButtonsOnDisable(connectHostBtn, GlobalVariables.darkColor);
        }

        /// <summary>
        /// Start/Stop live share envent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void startLiveBtn_Click(object sender, EventArgs e)
        {
            GlobalVariables.livePassword = passwordTxt.Text;
            GlobalVariables.sessionId = sessionTxt.Text;
            GlobalVariables.typeConnection = true;
            try
            {
                HubConnectionBuild();
                await ApiConnectionEvents.StartShare(Form1.Instance.hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId,
                    startLiveBtn, connectHostBtn, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Set live share button text.
        /// </summary>
        /// <param name="liveConnected"></param>
        /// <param name="startLiveBtn"></param>
        private void SetLiveButtonText(bool remoteConnected, bool liveConnected, Button startLiveBtn, Button remoteConnect)
        {
            if (liveConnected && !remoteConnected)
            {
                startLiveBtn.Text = "Stop Live Share";
                remoteConnect.Enabled = false;
            }
            else
            {
                startLiveBtn.Text = "Start Live Share";
                remoteConnect.Enabled = true;
            }
        }

        /// <summary>
        /// Set remote connect button text.
        /// </summary>
        /// <param name="liveConnected"></param>
        /// <param name="startLiveBtn"></param>
        private void SetRemoteButtonText(bool remoteConnected, bool liveConnected, Button remoteConnect, Button startLiveBtn)
        {
            if (remoteConnected && !liveConnected)
            {
                remoteConnect.Text = "Stop Connection";
                startLiveBtn.Enabled = false;
            }
            else
            {
                remoteConnect.Text = "Remote Connect";
                startLiveBtn.Enabled = true;
            }
        }

        /// <summary>
        /// Display generated password on textbox.
        /// </summary>
        /// <param name="password"></param>
        private void GeneratePassword(TextBox password, string livePassword, bool apiRemoteConnected)
        {
            password.Text = (string.IsNullOrEmpty(livePassword) || apiRemoteConnected) ? Utils.Encryption.KeyGenerator.GeneratePassword(15, true, true, false) : livePassword;

        }

        /// <summary>
        /// Display session id on textbox.
        /// </summary>
        /// <param name="sessionId"></param>
        private void SetSessionId(TextBox sessionIdTxt, string sessionId)
        {
            sessionIdTxt.Text = sessionId;
        }

        /// <summary>
        /// Display generated password on textbox.
        /// </summary>
        /// <param name="password"></param>
        private void SetRemotePassword(TextBox RemotePasswordTxt, string RemotePassword, bool remoteConnected)
        {
            if (remoteConnected)
                RemotePasswordTxt.Text = RemotePassword;
        }

        /// <summary>
        /// Display session id on textbox.
        /// </summary>
        /// <param name="sessionId"></param>
        private void SetRemoteSessionId(TextBox RemoteSessionIdTxt, string RemoteSessionId, bool remoteConnected)
        {
            if (remoteConnected)
                RemoteSessionIdTxt.Text = RemoteSessionId;
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectHostBtn_Click(object sender, EventArgs e)
        {
            GlobalVariables.livePassword = remotePasswordTxt.Text;
            GlobalVariables.sessionId = remoteSessioniDtxt.Text;
            GlobalVariables.remoteSessionId = remoteSessioniDtxt.Text;
            GlobalVariables.typeConnection = false;
            try
            {
                HubConnectionBuild();
                await ApiConnectionEvents.Connect(Form1.Instance.hubConnection, connectHostBtn, startLiveBtn,
                    GlobalVariables.livePassword, GlobalVariables.sessionId, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Set color to gray on disabled button to be displayed properly in dark mode.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="darkMode"></param>
        private void SetColorButtonsOnDisable(Button button, bool darkMode)
        {
            if (!darkMode)
                return;

            if (button.Enabled)
                button.BackColor = Color.FromArgb(30, 30, 30);
            else
                button.BackColor = Color.Gray;
        }


        /// <summary>
        /// Hub connection builder.
        /// </summary>
        private void HubConnectionBuild()
        {
            if (!GlobalVariables.connected)
            {
                Form1.Instance.hubConnection = new HubConnectionBuilder()
      .WithUrl(GlobalVariables.apiUrl)
      .Build();

                ApiConnectionEvents.ApiConnection(Form1.Instance.hubConnection, Form1.Instance.liveStatusPb, Form1.Instance.textEditorControl1,
                    GlobalVariables.connected, GlobalVariables.apiUrl);
            }
        }

        /// <summary>
        /// Check if API url is loaded from registry and store it.
        /// </summary>
        /// <param name="apiUrl"></param>
        private void CheckApiUrl(ref string apiUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                DialogResult dr = DialogResult.No;
                dr = MessageBox.Show("There is no API url stored. Do you want to add one?", "CIARE - Live Share", MessageBoxButtons.YesNo,
    MessageBoxIcon.Warning);

                if (dr == DialogResult.Yes)
                {
                    ApiUrlCheck apiUrlCheck = new ApiUrlCheck();
                    apiUrlCheck.ShowDialog();
                }
               
                if(string.IsNullOrWhiteSpace(apiUrl))
                    this.Close();
            }
        }
    }
}
