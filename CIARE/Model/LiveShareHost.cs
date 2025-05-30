﻿using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.ComponentModel;
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
            // Check if CIARE is reconnecting to Live API.
            CheckReconnectionStatus();

            // Check if api url exist.
            CheckApiUrl(ref GlobalVariables.apiUrl);

            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);

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

            // Start timer for check if live share API is up.
            checkLiveAPITimer.Enabled = true;
            checkLiveAPITimer.Start();
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


        /// <summary>
        /// Start/Stop live share envent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void startLiveBtn_Click(object sender, EventArgs e)
        {
            // Check if live share API is up.
            Network network = new Network(GlobalVariables.apiUrl);
            if (!network.IsLiveApiConnected())
            {
                MessageBox.Show("The API link seems down for the moment!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (passwordTxt.Text.Length < 5)
            {
                MessageBox.Show("Minimum password length is 5 characters!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GlobalVariables.livePassword = passwordTxt.Text;
            GlobalVariables.sessionId = sessionTxt.Text;
            GlobalVariables.typeConnection = true;
            try
            {
                GlobalVariables.liveTabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                HubConnectionBuild();
                await ApiConnectionEvents.StartShare(MainForm.Instance.hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId,
                    startLiveBtn, connectHostBtn, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
                SetColorButtonsOnDisable(startLiveBtn, GlobalVariables.darkColor);
                SetColorButtonsOnDisable(connectHostBtn, GlobalVariables.darkColor);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
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
        /// Check if live share API is up.
        /// </summary>
        private void CheckIfAPIisALIVE()
        {
            Network network = new Network(GlobalVariables.apiUrl);
                liveApiPb.Image = (network.IsLiveApiConnected()) ? Properties.Resources.green_dot : Properties.Resources.red_dot;
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectHostBtn_Click(object sender, EventArgs e)
        {
            Network network = new Network(GlobalVariables.apiUrl);
            if (!network.IsLiveApiConnected())
            {
                MessageBox.Show("The API link seems down for the moment!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (remotePasswordTxt.Text.Length < 5)
            {
                MessageBox.Show("Minimum password length is 5 characters!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GlobalVariables.livePassword = remotePasswordTxt.Text;
            GlobalVariables.sessionId = remoteSessioniDtxt.Text;
            GlobalVariables.remoteSessionId = remoteSessioniDtxt.Text;
            GlobalVariables.typeConnection = false;
            try
            {
                GlobalVariables.liveTabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                HubConnectionBuild();
                await ApiConnectionEvents.Connect(this, MainForm.Instance.hubConnection, connectHostBtn, startLiveBtn,
                    GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
                SetColorButtonsOnDisable(startLiveBtn, GlobalVariables.darkColor);
                SetColorButtonsOnDisable(connectHostBtn, GlobalVariables.darkColor);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
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
            {
                if (GlobalVariables.isVStheme)
                    button.BackColor = Color.FromArgb(30, 30, 30);
                else
                    button.BackColor = Color.FromArgb(2, 0, 10);
            }
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
                MainForm.Instance.hubConnection = new HubConnectionBuilder()
      .WithUrl(GlobalVariables.apiUrl, opts =>
        {
            opts.TransportMaxBufferSize = 100000000; //100mb
            opts.ApplicationMaxBufferSize = 100000000; //100mb
        }
      )
      .Build();
                ApiConnectionEvents.ApiConnection(MainForm.Instance.hubConnection, GlobalVariables.apiUrl);
            }
        }

        /// <summary>
        /// Check if CIARE is reconnecting to Live API.
        /// </summary>
        private void CheckReconnectionStatus()
        {
            if (GlobalVariables.liveDisconnected || GlobalVariables.reconnectionCount < 6)
            {
                MessageBox.Show("You cannot access this setting when trying to reconnect to Live Share API!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
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

                if (string.IsNullOrWhiteSpace(apiUrl))
                    this.Close();
            }
        }

        /// <summary>
        /// Timer function for call API live check if is up in background.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkLiveAPITimer_Tick(object sender, EventArgs e)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += APICheck_DoWork;
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// BW function for check if live API is UP;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void APICheck_DoWork(object sender, DoWorkEventArgs e) =>
            CheckIfAPIisALIVE();

        /// <summary>
        /// Close timer on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LiveShareHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            checkLiveAPITimer.Stop();
            checkLiveAPITimer.Enabled = false;
        }
    }
}
