using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class LiveShareHost : Form
    {
        private const string NicknameRegistryKey = "LiveShareNickname";
        private bool _connectionBusy;
        private CancellationTokenSource _connectionCancellation;

        public LiveShareHost()
        {
            InitializeComponent();
            string savedNickname = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", NicknameRegistryKey);
            nicknameTxt.Text = LiveSharePosition.TryNormalizeNickname(savedNickname, out string nickname)
                ? nickname : GlobalVariables.liveShareNickname;
        }

        private void LiveShareHost_Load(object sender, EventArgs e)
        {
            if (!MainForm.Instance.LiveShareEventsFeature.CanShareEditor(SelectedEditor.GetSelectedEditor()))
            {
                Close(); return;
            }

            // Check if api url exist.
            CheckApiUrl(ref GlobalVariables.apiUrl);
            if (IsDisposed || string.IsNullOrWhiteSpace(GlobalVariables.apiUrl)) return;

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

            // Display the current SignalR connection state on the UI thread.
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
            if (_connectionBusy) return;
            if (!GlobalVariables.apiConnected && !SaveNickname())
                return;
            if (!GlobalVariables.apiConnected && passwordTxt.Text.Length < 5)
            {
                MessageBox.Show("Minimum password length is 5 characters!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GlobalVariables.typeConnection = true;
            BeginConnectionChange();
            try
            {
                if (!GlobalVariables.apiConnected)
                {
                    GlobalVariables.liveTabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                    await HubConnectionBuild();
                }
                await ApiConnectionEvents.StartShare(MainForm.Instance.hubConnection, passwordTxt.Text, sessionTxt.Text,
                    startLiveBtn, connectHostBtn, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex), _connectionCancellation.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { EndConnectionChange(); }
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
        /// Display the current SignalR connection state.
        /// </summary>
        private void RefreshConnectionStatus()
        {
            liveApiPb.Image = MainForm.Instance.hubConnection?.State switch
            {
                HubConnectionState.Connected => Properties.Resources.green_dot,
                HubConnectionState.Connecting or HubConnectionState.Reconnecting => Properties.Resources.orange_dot,
                _ => Properties.Resources.red_dot
            };
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectHostBtn_Click(object sender, EventArgs e)
        {
            if (_connectionBusy) return;
            if (!GlobalVariables.apiRemoteConnected && !SaveNickname())
                return;
            if (!GlobalVariables.apiRemoteConnected && remotePasswordTxt.Text.Length < 5)
            {
                MessageBox.Show("Minimum password length is 5 characters!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GlobalVariables.remoteSessionId = remoteSessioniDtxt.Text;
            GlobalVariables.typeConnection = false;
            BeginConnectionChange();
            try
            {
                if (!GlobalVariables.apiRemoteConnected)
                {
                    GlobalVariables.liveTabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                    await HubConnectionBuild();
                }
                await ApiConnectionEvents.Connect(this, MainForm.Instance.hubConnection, connectHostBtn, startLiveBtn,
                    remotePasswordTxt.Text, remoteSessioniDtxt.Text, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex), _connectionCancellation.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { EndConnectionChange(); }
        }

        private void BeginConnectionChange()
        {
            _connectionBusy = true;
            _connectionCancellation = new CancellationTokenSource();
            startLiveBtn.Enabled = connectHostBtn.Enabled = false;
        }

        private void EndConnectionChange()
        {
            _connectionBusy = false;
            _connectionCancellation?.Dispose(); _connectionCancellation = null;
            if (!IsDisposed) RefreshConnectionButtons();
        }

        private void RefreshConnectionButtons()
        {
            SetLiveButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, startLiveBtn, connectHostBtn);
            SetRemoteButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, connectHostBtn, startLiveBtn);
            SetColorButtonsOnDisable(startLiveBtn, GlobalVariables.darkColor);
            SetColorButtonsOnDisable(connectHostBtn, GlobalVariables.darkColor);
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
        private async Task HubConnectionBuild()
        {
            var previous = MainForm.Instance.hubConnection;
            if (previous != null)
            {
                if (previous.State != HubConnectionState.Disconnected) return;
                MainForm.Instance.hubConnection = null;
                await previous.DisposeAsync();
            }
            MainForm.Instance.hubConnection = LiveShareConnection.Create(GlobalVariables.apiUrl);
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
        /// Refresh status and buttons without making a separate health request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkLiveAPITimer_Tick(object sender, EventArgs e)
        {
            RefreshConnectionStatus();
            if (!_connectionBusy) RefreshConnectionButtons();
        }

        /// <summary>
        /// Close timer on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LiveShareHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            _connectionCancellation?.Cancel();
            SaveNickname(showValidation: false);
            checkLiveAPITimer.Stop();
            checkLiveAPITimer.Enabled = false;
        }

        private bool SaveNickname(bool showValidation = true)
        {
            if (!LiveSharePosition.TryNormalizeNickname(nicknameTxt.Text, out string nickname))
            {
                if (showValidation)
                {
                    MessageBox.Show("Enter a nickname of 1 to 32 characters without control characters.",
                        "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nicknameTxt.Focus();
                }
                return false;
            }
            GlobalVariables.liveShareNickname = nickname;
            RegistryManagement.RegKey_CreateKey(GlobalVariables.registryPath, NicknameRegistryKey, nickname);
            return true;
        }
    }
}
