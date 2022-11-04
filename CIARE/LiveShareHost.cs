using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
            GeneratePassword(passwordTxt);

            // Display current session id.
            // Generated only on first app run.
            SetSessionId(sessionTxt);

            // Set live share button text depending on live connection.
            SetLiveButtonText(GlobalVariables.apiConnected, startLiveBtn);

            // Set remote button text depending on live connection.
            SetRemoteButtonText(GlobalVariables.apiConnected, connectHostBtn);
        }

        /// <summary>
        /// Start/Stop live share envent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void startLiveBtn_Click(object sender, EventArgs e)
        {
            GlobalVariables.livePassword = passwordTxt.Text;
            var apiConnectionEvents = new ApiConnectionEvents(Form1.Instance.hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId, GlobalVariables.apiUrl);
            await apiConnectionEvents.StartShare(GlobalVariables.apiConnected, startLiveBtn,Form1.Instance.updateLiveCode,Form1.Instance.writer, connectHostBtn, Form1.Instance.outputRBT);
        }

        /// <summary>
        /// Set live share button text.
        /// </summary>
        /// <param name="liveConnected"></param>
        /// <param name="startLiveBtn"></param>
        private void SetLiveButtonText(bool liveConnected, Button startLiveBtn)
        {
            if(liveConnected)
                startLiveBtn.Text= "Stop Live Share";
            else
                startLiveBtn.Text= "Start Live Share";
        }

        /// <summary>
        /// Set remote connect button text.
        /// </summary>
        /// <param name="liveConnected"></param>
        /// <param name="startLiveBtn"></param>
        private void SetRemoteButtonText(bool liveConnected, Button remoteConnect)
        {
            if (liveConnected)
                remoteConnect.Text = "Stop Connection";
            else
                remoteConnect.Text = "Remote Connect";
        }

        /// <summary>
        /// Display generated password on textbox.
        /// </summary>
        /// <param name="password"></param>
        private void GeneratePassword(TextBox password)
        {
            password.Text = Utils.Encryption.KeyGenerator.GeneratePassword(15, true, true, false);
        }

        /// <summary>
        /// Display session id on textbox.
        /// </summary>
        /// <param name="sessionId"></param>
        private void SetSessionId(TextBox sessionId)
        {
            sessionId.Text = GlobalVariables.sessionId;
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectHostBtn_Click(object sender, EventArgs e)
        {
            var apiConnectionEvents = new ApiConnectionEvents(Form1.Instance.hubConnection, remotePasswordTxt.Text, remoteSessioniDtxt.Text, GlobalVariables.apiUrl);
            await apiConnectionEvents.Connect(Form1.Instance.outputRBT, connectHostBtn, startLiveBtn, Form1.Instance.updateLiveCode, Form1.Instance.writer,GlobalVariables.apiConnected);
        }
    }
}
