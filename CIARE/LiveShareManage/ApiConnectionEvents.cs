using System;
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
        /// Connect to remote session.
        /// </summary>
        public static async Task Connect(HubConnection hubConnection, Button connectBtn, Button liveShareBtn,
           string password, string sessionId, TextEditorControl textEditorControl, PictureBox pictureBox)
        {
            if (GlobalVariables.apiRemoteConnected)
            {
                if (hubConnection != null)
                    await hubConnection.StopAsync();
                GlobalVariables.connected = false;
                GlobalVariables.apiRemoteConnected = false;
                connectBtn.Text = "Remote Connect";
                MessageBox.Show("Connected Stoped", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                liveShareBtn.Enabled = true;
            }
            else
            {

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    MessageBox.Show("You must enter the remote session ID to connect!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hubConnection.On<string, int>("GetSend", (code, lineNumber) =>
                {
                    SetLiveCode(textEditorControl, code, password, lineNumber);
                });

                try
                {
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, string.Empty, 0);
                    pictureBox.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiRemoteConnected = true;
                    GlobalVariables.connected = true;
                    connectBtn.Text = "Stop Connection";
                    MessageBox.Show("Connected to remote session!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    liveShareBtn.Enabled = false;
                }
                catch { }
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
                if (!GlobalVariables.codeWriter)
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
                if (hubConnection != null)
                    await hubConnection.StopAsync();

                connectBtn.Enabled = true;
                GlobalVariables.livePassword = string.Empty;
                GlobalVariables.apiConnected = false;
                GlobalVariables.connected = false;
                MessageBox.Show("Live Share stopped!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                startShareBtn.Text = "Stop Live Share";
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hubConnection.On<string, int>("GetSend", (code, lineNumber) =>
                {
                    SetLiveCode(textEditorControl, code, password, lineNumber);
                });

                try
                {
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, string.Empty, 0);
                    pictureBox.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiConnected = true;
                    GlobalVariables.connected = true;
                    connectBtn.Enabled = false;
                    MessageBox.Show("Live Share started!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
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
                if (!string.IsNullOrEmpty(code))
                {
                    var decrypt = AESEncryption.Decrypt(code, password);
                    if (!string.IsNullOrEmpty(decrypt))
                    {
                        textEditorControl.Text = decrypt;
                        GoToLineNumber.GoToLine(textEditorControl, lineNumber);
                    }
                }
                GlobalVariables.codeWriter = true;
            }
            catch { }
        }
    }
}
