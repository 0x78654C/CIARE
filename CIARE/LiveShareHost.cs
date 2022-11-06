using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
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
            await ApiConnectionEvents.StartShare(Form1.Instance.hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId,
                startLiveBtn, connectHostBtn, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
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
            await ApiConnectionEvents.Connect(Form1.Instance.hubConnection, connectHostBtn, startLiveBtn,
                GlobalVariables.livePassword, GlobalVariables.sessionId, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
        }
    }
}
