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
        private CancellationTokenSource _availabilityCancellation = new();
        private bool _apiCheckBusy;
        private bool? _apiReachable;
        private string _apiCheckedUrl;
        private DateTime _nextApiCheck;

        public LiveShareHost()
        {
            InitializeComponent();
            startLiveBtn.EnabledChanged += (_, _) => ApplyButtonTheme(startLiveBtn);
            connectHostBtn.EnabledChanged += (_, _) => ApplyButtonTheme(connectHostBtn);
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

            ApplyButtonTheme(startLiveBtn);
            ApplyButtonTheme(connectHostBtn);

            // Display the current SignalR connection state on the UI thread.
            RefreshConnectionStatus();
            checkLiveAPITimer.Enabled = true;
            checkLiveAPITimer.Start();
            _ = RefreshApiAvailabilityAsync();
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
            var state = MainForm.Instance.hubConnection?.State ?? HubConnectionState.Disconnected;
            bool? reachable = _apiCheckedUrl == GlobalVariables.apiUrl ? _apiReachable : null;
            connectionStatusLbl.Text = state switch
            {
                HubConnectionState.Connected => "API online — connected to Live Share",
                HubConnectionState.Connecting => "Connecting to Live Share…",
                HubConnectionState.Reconnecting => "Reconnecting to Live Share…",
                _ => reachable switch
                {
                    true => "API online — not connected",
                    false => "API offline or unavailable",
                    _ => "Checking API…"
                }
            };
            liveApiPb.Image = state switch
            {
                HubConnectionState.Connected => Properties.Resources.green_dot,
                HubConnectionState.Connecting or HubConnectionState.Reconnecting => Properties.Resources.orange_dot,
                _ => reachable switch
                {
                    true => Properties.Resources.green_dot,
                    false => Properties.Resources.red_dot,
                    _ => Properties.Resources.orange_dot
                }
            };
        }

        internal async Task RefreshApiAvailabilityAsync()
        {
            if (_apiCheckBusy || IsDisposed || Disposing || _availabilityCancellation == null) return;
            // A working session is stronger evidence than a separate probe, and consumes no extra requests.
            if (MainForm.Instance.hubConnection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
                return;
            string url = GlobalVariables.apiUrl;
            var cancellationToken = _availabilityCancellation.Token;
            _apiCheckBusy = true;
            if (_apiCheckedUrl != url)
            {
                _apiCheckedUrl = url;
                _apiReachable = null;
                RefreshConnectionStatus();
            }
            try
            {
                bool reachable = await LiveShareAvailability.CheckAsync(url, cancellationToken);
                if (cancellationToken.IsCancellationRequested || IsDisposed || Disposing) return;
                _apiCheckedUrl = url;
                _apiReachable = reachable;
                RefreshConnectionStatus();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            finally
            {
                _apiCheckBusy = false;
                _nextApiCheck = DateTime.UtcNow.AddSeconds(15);
            }
        }

        private void StopApiAvailabilityChecks()
        {
            _availabilityCancellation?.Cancel();
            _availabilityCancellation?.Dispose();
            _availabilityCancellation = null;
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
            if (!IsDisposed)
            {
                RefreshConnectionButtons();
                RefreshConnectionStatus();
            }
        }

        private void RefreshConnectionButtons()
        {
            SetLiveButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, startLiveBtn, connectHostBtn);
            SetRemoteButtonText(GlobalVariables.apiRemoteConnected, GlobalVariables.apiConnected, connectHostBtn, startLiveBtn);
            ApplyButtonTheme(startLiveBtn);
            ApplyButtonTheme(connectHostBtn);
        }

        /// <summary>
        /// Use the active editor palette for every button state, without native bright borders.
        /// </summary>
        /// <param name="button"></param>
        private void ApplyButtonTheme(Button button)
        {
            string themeName = MainForm.Instance?.WindowThemeFeature._appliedTheme
                ?? (GlobalVariables.darkColor ? "C#-Dark" : "C#-Light");
            var colors = ThemeManager.GetCompletionThemeColors(themeName);
            Color background = GlobalVariables.darkColor ? GlobalVariables.controlBgColor : SystemColors.Control;
            Color panel = GlobalVariables.darkColor ? GlobalVariables.formBgColor : SystemColors.Control;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = button.Enabled ? background : panel;
            button.ForeColor = button.Enabled ? colors.ForeColor : Blend(colors.ForeColor, panel, 50);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = button.Enabled ? colors.BorderColor : Blend(colors.BorderColor, panel, 50);
            button.FlatAppearance.MouseOverBackColor = Blend(background, colors.AccentColor, 20);
            button.FlatAppearance.MouseDownBackColor = Blend(background, colors.AccentColor, 35);
        }

        private static Color Blend(Color first, Color second, int percent) => Color.FromArgb(
            (first.R * (100 - percent) + second.R * percent) / 100,
            (first.G * (100 - percent) + second.G * percent) / 100,
            (first.B * (100 - percent) + second.B * percent) / 100);


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
        /// Refresh connection status and periodically probe idle hubs without overlapping requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void checkLiveAPITimer_Tick(object sender, EventArgs e)
        {
            RefreshConnectionStatus();
            if (!_connectionBusy) RefreshConnectionButtons();
            if (_apiCheckedUrl != GlobalVariables.apiUrl || DateTime.UtcNow >= _nextApiCheck)
                await RefreshApiAvailabilityAsync();
        }

        /// <summary>
        /// Close timer on form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LiveShareHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            _connectionCancellation?.Cancel();
            StopApiAvailabilityChecks();
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
