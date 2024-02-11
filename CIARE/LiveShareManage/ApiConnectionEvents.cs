using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.Encryption;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.SignalR.Client;

namespace CIARE.LiveShareManage
{
    [SupportedOSPlatform("windows")]
    public class ApiConnectionEvents
    {
        /// <summary>
        /// Build connection events to API.
        /// </summary>
        /// <param name="updateCode"></param>
        /// <param name="writer"></param>
        /// <param name="connected"></param>
        public static void ApiConnection(HubConnection hubConnection, string apiUrl)
        {
            if (string.IsNullOrEmpty(apiUrl))
                return;

            // Closed event.
            hubConnection.Closed += (sender) =>
            {
                GlobalVariables.liveDisconnected = true;
                MainForm.Instance.liveStatusPb.Image = Properties.Resources.orange_dot;
                MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Clean picture status on main form.
        /// </summary>
        /// <param name="liveStatusPb"></param>
        public static void CleanDot(PictureBox liveStatusPb) => liveStatusPb.Image = null;

        /// <summary>
        /// Remove read only from text editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        private static void RemoveReadOnlyTextEditor(TextEditorControl textEditorControl)
        {
            if (textEditorControl.ReadOnly)
                textEditorControl.ReadOnly = false;
        }
        /// <summary>
        /// Connect to remote session.
        /// </summary>
        public static async Task Connect(Form form, HubConnection hubConnection, Button connectBtn, Button liveShareBtn,
           string password, string sessionId, TextEditorControl textEditorControl)
        {
            if (GlobalVariables.apiRemoteConnected)
            {
                GlobalVariables.apiRemoteConnected = false;
                if (hubConnection != null)
                    await hubConnection.StopAsync();
                MainForm.Instance.liveStatusPb.Image = null;
                RemoveReadOnlyTextEditor(textEditorControl);
                GlobalVariables.connected = false;
                GlobalVariables.liveDisconnected = false;
                GlobalVariables.isConnected = false;
                GlobalVariables.livePassword = string.Empty;
                connectBtn.Text = "Remote Connect";
                MessageBox.Show("Connected Stoped", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                liveShareBtn.Enabled = true;
                if (GlobalVariables.darkColor)
                    liveShareBtn.BackColor = Color.FromArgb(30, 30, 30);
                MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
            }
            else
            {

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE  - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    MessageBox.Show("You must enter the remote session ID to connect!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hubConnection.On<string, string, string>("GetSend", (code, position, remoteConnectionId) =>
                {
                    GlobalVariables.remoteConnectionId = remoteConnectionId;
                    SetLiveCode(textEditorControl, code, password, position);
                });

                try
                {
                    connectBtn.Text = "Stop Connection";
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, "remote", "");
                    if (!textEditorControl.ReadOnly)
                        textEditorControl.ReadOnly = true;
                    MainForm.Instance.liveStatusPb.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiRemoteConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    GlobalVariables.reconnectionCount = 6;
                    liveShareBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        liveShareBtn.BackColor = Color.Gray;
                    MessageBox.Show("Connected to remote session!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.Close();
                    MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
                }
                catch (Exception ex)
                {
                    if (!GlobalVariables.liveDisconnected)
                        ErrorDisconection(ex.Message, connectBtn);
                    else
                    {
                        GlobalVariables.reconnectionCount--;
                        if (GlobalVariables.reconnectionCount == 0 && GlobalVariables.liveDisconnected)
                            ErrorDisconection(ex.Message, connectBtn);
                        else
                            ManageHubDisconnection(hubConnection, connectBtn);
                    }
                }
            }
        }

        /// <summary>
        /// Display error message on live connection fail.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="connectionButton"></param>
        private static void ErrorDisconection(string message, Button connectionButton)
        {
            MessageBox.Show(message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            connectionButton.Text = GlobalVariables.typeConnection ? "Start Live Share" : "Remote Connect";
            GlobalVariables.livePassword = string.Empty;
        }

        /// <summary>
        /// Send encrypted data
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="output"></param>
        public async Task SendData(HubConnection hubConnection, string password, string sessionId, TextEditorControl textEditorControl)
        {
            try
            {
                if (!GlobalVariables.codeWriter && hubConnection.ConnectionId != GlobalVariables.remoteConnectionId)
                {
                    int lineNumber = 0;
                    int columnNumber = 0;
                    lineNumber = GoToLineNumber.GetLineNumber(textEditorControl);
                    columnNumber = GoToLineNumber.GetColumnNumber(textEditorControl);
                    var encyrpted = AESEncryption.Encrypt(textEditorControl.Text, password);
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, $"{lineNumber}|{columnNumber}");
                }
            }
            catch{
            }
        }

        /// <summary>
        /// Close api hub connection.
        /// </summary>
        public async Task CloseConnection(HubConnection hubConnection)
        {
            if (hubConnection != null)
                await hubConnection.StopAsync();
        }

        /// <summary>
        /// Start live share event.
        /// </summary>
        /// <param name="connected"></param>
        /// <param name="startShareBtn"></param>
        /// <param name="updateCode"></param>
        /// <param name="writer"></param>
        /// <param name="connectBtn"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static async Task StartShare(HubConnection hubConnection, string password, string sessionId, Button startShareBtn,
            Button connectBtn, TextEditorControl textEditorControl)
        {
            if (GlobalVariables.apiConnected)
            {
                startShareBtn.Text = "Start Live Share";
                GlobalVariables.apiConnected = false;
                if (hubConnection != null)
                    await hubConnection.StopAsync();
                MainForm.Instance.liveStatusPb.Image = null;
                connectBtn.Enabled = true;
                if (GlobalVariables.darkColor)
                    connectBtn.BackColor = Color.FromArgb(30, 30, 30);
                GlobalVariables.livePassword = string.Empty;
                GlobalVariables.connected = false;
                GlobalVariables.liveDisconnected = false;
                GlobalVariables.isConnected = false;
                MessageBox.Show("Live Share stopped!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
            }
            else
            {
                startShareBtn.Text = "Stop Live Share";
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                hubConnection.On<string, string, string>("GetSend", (code, position, remoteConnectionId) =>
                {
                    GlobalVariables.remoteConnectionId = remoteConnectionId;
                    SetLiveCode(textEditorControl, code, password, position);
                    DisplayConnectedUser(ref GlobalVariables.isConnected, remoteConnectionId);
                });

                try
                {
                    string code = textEditorControl.Text;
                    var encyrpted = AESEncryption.Encrypt(code, password);
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, "");
                    MainForm.Instance.liveStatusPb.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    GlobalVariables.reconnectionCount = 6;
                    connectBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        connectBtn.BackColor = Color.Gray;
                    MessageBox.Show("Live Share started!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MainForm.Instance.EditorTabControl.SelectTab(GlobalVariables.liveTabIndex);
                }
                catch (Exception ex)
                {

                    if (!GlobalVariables.liveDisconnected)
                        ErrorDisconection(ex.Message, startShareBtn);
                    else
                    {
                        GlobalVariables.reconnectionCount--;
                        if (GlobalVariables.reconnectionCount == 0)
                            ErrorDisconection(ex.Message, startShareBtn);
                        else
                            ManageHubDisconnection(hubConnection, startShareBtn);
                    }
                }
            }
        }

        /// <summary>
        /// Decrypt and set code on editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="writer"></param>
        public static void SetLiveCode(TextEditorControl textEditorControl, string code, string password, string position)
        {
            try
            {
                RemoveReadOnlyTextEditor(textEditorControl);
                if (!string.IsNullOrEmpty(code))
                {
                    var decrypt = AESEncryption.Decrypt(code, password);
                    if (!string.IsNullOrEmpty(decrypt))
                    {
                        textEditorControl.Text = decrypt;
                        GoToLineNumber.SetPositionCaret(textEditorControl, position);
                        textEditorControl.Refresh();
                    }
                }
                GlobalVariables.codeWriter = true;
            }
            catch { }
        }

        /// <summary>
        /// Display when remote user is connected to host.
        /// </summary>
        /// <param name="connected"></param>
        /// <param name="connectionId"></param>
        private static void DisplayConnectedUser(ref bool connected, string connectionId )
        {
            if (!connected)
            {
                MessageBox.Show($"Remote user is connected!","CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                connected = true;
            }
        }

        /// <summary>
        /// Set timespan to 60 secconds for hub to reconnect(every 10 sec a reconnection).
        /// </summary>
        public static TimeSpan[] DefaultBackoffTimes = new TimeSpan[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
        };


        /// <summary>
        /// Handle live hub disconnection.
        /// </summary>
        /// <param name="textEditorControl"></param>
        public static async void ManageHubDisconnection(HubConnection hubConnection, Button connection)
        {
            GlobalVariables.apiConnected = false;
            GlobalVariables.apiRemoteConnected = false;
            GlobalVariables.connected = false;
            if (hubConnection != null)
                await hubConnection.StopAsync();

            DialogResult dr = DialogResult.No;
            dr = MessageBox.Show("The live share connection or API is down. Do you want to reconnect?", "CIARE - Live Share", MessageBoxButtons.YesNo,
MessageBoxIcon.Warning);

            var fakeButton = new Button();

            if (dr == DialogResult.Yes)
            {
                if (GlobalVariables.typeConnection)
                    await StartShare(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId,
               fakeButton, fakeButton, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
                else
                    await Connect(new Form(),hubConnection, fakeButton, fakeButton,
GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex)); ;
            }
            else
            {
                GlobalVariables.livePassword = string.Empty;
                if (GlobalVariables.typeConnection)
                    connection.Text = "Start Live Share";
                else
                    connection.Text = "Remote Connect";
                GlobalVariables.reconnectionCount = 6;
                GlobalVariables.liveDisconnected = false;
                GlobalVariables.isConnected = false;
                CleanDot(MainForm.Instance.liveStatusPb);
            }
        }
    }
}
