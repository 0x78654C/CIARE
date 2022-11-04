using System;
using System.Security.Permissions;
using System.Text;
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
        private HubConnection HubConnection;
        private string Password { get; set; }
        private string SessionId { get; set; }
        private string ApiUrl { get; set; }

        private string _codeReceived;

        public ApiConnectionEvents(HubConnection hubConnection, string password, string sessionId, string apiUrl)
        {
            HubConnection = hubConnection;
            Password = password;
            SessionId = sessionId;
            ApiUrl = apiUrl;
        }

        /// <summary>
        /// Connect to remote session.
        /// </summary>
        public async Task Connect(RichTextBox output,Button connectBtn, Button liveShareBtn,Timer updateCode,Timer writer, bool connected)
        {
            if (connected)
            {
                if (HubConnection != null)
                    await HubConnection.StopAsync();
                updateCode.Stop();
                updateCode.Enabled = false;
                writer.Stop();
                writer.Enabled = false;
                connected = false;
                connectBtn.Text = "Remote Connect";
                MessageBox.Show("Connected Stoped", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                liveShareBtn.Enabled = true;
            }
            else
            {

                if (string.IsNullOrEmpty(Password))
                {
                    MessageBox.Show("No password provied!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(SessionId))
                {
                    MessageBox.Show("You must enter the remote session ID to connect!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                HubConnection.On<string>("GetSend", (code) =>
                {
                    _codeReceived = code;
                });


                try
                {
                    await HubConnection.StartAsync();
                    await HubConnection.InvokeAsync("GetSendCode", SessionId, string.Empty);
                    connected = true;
                    updateCode.Enabled = false;
                    updateCode.Start();
                    writer.Enabled = true;
                    writer.Start();
                    connectBtn.Text = "Stop Connection";
                    MessageBox.Show("Connected to remote session!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    liveShareBtn.Enabled = false;
                }
                catch (Exception ex)
                {
                    output.Text = ex.ToString(); // TODO: remove after tests.
                }
            }
        }

        /// <summary>
        /// Send encrypted data
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="output"></param>
        public async Task SendData(TextEditorControl textEditorControl, RichTextBox output)
        {
            try
            {
                var encyrpted = AESEncryption.Encrypt(textEditorControl.Text, Password);
                await HubConnection.InvokeAsync("GetSendCode", SessionId, encyrpted);
            }
            catch (Exception ex)
            {
                output.Text = ex.ToString(); // TODO: remove after tests.
            }
        }

        /// <summary>
        /// Close api hub connection.
        /// </summary>
        public async Task CloseConnection()
        {
            if (HubConnection != null)
                await HubConnection.StopAsync();
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
        public async Task StartShare(bool connected, Button startShareBtn, Timer updateCode, Timer writer, Button connectBtn, RichTextBox output)
        {
            if (GlobalVariables.apiConnected)
            {
                startShareBtn.Text = "Start Live Share";
                if (HubConnection != null)
                    await HubConnection.StopAsync();

                updateCode.Stop();
                updateCode.Enabled = false;
                connectBtn.Enabled = true;
                writer.Stop();
                writer.Enabled = false;
                GlobalVariables.apiConnected = false;
                MessageBox.Show("Live Share stopped!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                startShareBtn.Text = "Stop Live Share";
                if (string.IsNullOrEmpty(Password))
                {
                    MessageBox.Show("No password provied!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                HubConnection.On<string>("GetSend", (code) =>
                {
                    _codeReceived = code;
                });

                try
                {
                    await HubConnection.StartAsync();
                    await HubConnection.InvokeAsync("GetSendCode", SessionId, string.Empty);
                    GlobalVariables.apiConnected = true;
                    updateCode.Enabled = true;
                    updateCode.Start();
                    writer.Enabled = true;
                    writer.Start();
                    connectBtn.Enabled = false;
                    MessageBox.Show("Live Share started!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    output.Text = ex.ToString();
                }
            }
        }

        /// <summary>
        /// Decrypt and set code on editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="writer"></param>
        public void SetLiveCode(TextEditorControl textEditorControl, bool writer)
        {
            try
            {
                if (textEditorControl.Text != _codeReceived &&
                    _codeReceived.Length > 0 && writer)
                {
                    var decrypt = AESEncryption.Decrypt(_codeReceived, Password);
                    textEditorControl.Text = decrypt;
                }
            }
            catch { }
        }
    }
}
