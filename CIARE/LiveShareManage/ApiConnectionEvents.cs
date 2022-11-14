using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.Utils;
using CIARE.Utils.Encryption;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.SignalR.Client;

namespace CIARE.LiveShareManage
{
    public class ApiConnectionEvents
    {
        /// <summary>
        /// Build connection events to API.
        /// </summary>
        /// <param name="updateCode"></param>
        /// <param name="writer"></param>
        /// <param name="connected"></param>
        public static void ApiConnection(HubConnection hubConnection, PictureBox liveStatusPb, TextEditorControl textEditorControl, bool connected, string apiUrl)
        {
            if (string.IsNullOrEmpty(apiUrl))
                return;

            // Closed event.
            hubConnection.Closed += (sender) =>
            {
                GlobalVariables.liveDisconnected = true;
                liveStatusPb.Image = null;
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        public static async Task Connect(HubConnection hubConnection, Button connectBtn, Button liveShareBtn,
           string password, string sessionId, TextEditorControl textEditorControl, PictureBox pictureBox)
        {
            if (GlobalVariables.apiRemoteConnected)
            {
                GlobalVariables.apiRemoteConnected = false;

                if (hubConnection != null)
                    await hubConnection.StopAsync();

                GlobalVariables.connected = false;
                GlobalVariables.livePassword = string.Empty;
                connectBtn.Text = "Remote Connect";
                MessageBox.Show("Connected Stoped", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                liveShareBtn.Enabled = true;
                if (GlobalVariables.darkColor)
                    liveShareBtn.BackColor = Color.FromArgb(30, 30, 30);
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

                hubConnection.On<string, int, string>("GetSend", (code, lineNumber, remoteConnectionId) =>
                {
                    GlobalVariables.remoteConnectionId = remoteConnectionId;
                    SetLiveCode(textEditorControl, code, password, lineNumber);
                });

                try
                {
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, "remote", 0);
                    if (!textEditorControl.ReadOnly)
                        textEditorControl.ReadOnly = true;
                    pictureBox.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiRemoteConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    connectBtn.Text = "Stop Connection";
                    MessageBox.Show("Connected to remote session!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    liveShareBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        liveShareBtn.BackColor = Color.Gray;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    connectBtn.Text = "Remote Connect";
                }
            }
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
                    lineNumber = GoToLineNumber.GetLineNumber(textEditorControl);
                    var encyrpted = AESEncryption.Encrypt(textEditorControl.Text, password);
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, lineNumber + 20);
                }
            }
            catch { }
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
            Button connectBtn, TextEditorControl textEditorControl, PictureBox pictureBox)
        {
            if (GlobalVariables.apiConnected)
            {
                startShareBtn.Text = "Start Live Share";
                GlobalVariables.apiConnected = false;
                if (hubConnection != null)
                    await hubConnection.StopAsync();

                connectBtn.Enabled = true;
                if (GlobalVariables.darkColor)
                    connectBtn.BackColor = Color.FromArgb(30, 30, 30);
                GlobalVariables.livePassword = string.Empty;
                GlobalVariables.connected = false;
                MessageBox.Show("Live Share stopped!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                startShareBtn.Text = "Stop Live Share";
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hubConnection.On<string, int, string>("GetSend", (code, lineNumber, remoteConnectionId) =>
                {
                    GlobalVariables.remoteConnectionId = remoteConnectionId;
                    SetLiveCode(textEditorControl, code, password, lineNumber);
                });

                try
                {
                    string code = textEditorControl.Text;
                    var encyrpted = AESEncryption.Encrypt(code, password);
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, 0);
                    pictureBox.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    connectBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        connectBtn.BackColor = Color.Gray;

                    MessageBox.Show("Live Share started!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "CIARE - Live share", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startShareBtn.Text = "Start Live Share";
                }
            }
        }

        /// <summary>
        /// Decrypt and set code on editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="writer"></param>
        public static void SetLiveCode(TextEditorControl textEditorControl, string code, string password, int lineNumber)
        {
            try
            {
                if (textEditorControl.ReadOnly)
                    textEditorControl.ReadOnly = false;
                if (!string.IsNullOrEmpty(code))
                {
                    var decrypt = AESEncryption.Decrypt(code, password);
                    if (!string.IsNullOrEmpty(decrypt))
                    {
                        textEditorControl.Text = decrypt;
                        GoToLineNumber.GoToLine(textEditorControl, lineNumber);
                        textEditorControl.Refresh();
                    }
                }
                GlobalVariables.codeWriter = true;
            }
            catch { }
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
        public static async void ManageHubDisconnection(HubConnection hubConnection)
        {
            GlobalVariables.apiConnected = false;
            GlobalVariables.apiRemoteConnected = false;
            GlobalVariables.connected = false;
            if (hubConnection != null)
                await hubConnection.StopAsync();

            DialogResult dr = DialogResult.No;
            dr = MessageBox.Show("The live share connection is down. Do you want to reconnect?", "CIARE - Live Share", MessageBoxButtons.YesNo,
MessageBoxIcon.Warning);

            var fakeButton = new Button();

            if (dr == DialogResult.Yes)
            {
                if (GlobalVariables.typeConnection)
                    await StartShare(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId,
               fakeButton, fakeButton, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
                else
                    await Connect(hubConnection, fakeButton, fakeButton,
GlobalVariables.livePassword, GlobalVariables.sessionId, Form1.Instance.textEditorControl1, Form1.Instance.liveStatusPb);
            }
            else
                GlobalVariables.livePassword = string.Empty;
        }
    }
}
