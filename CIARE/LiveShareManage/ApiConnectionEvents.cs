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
        public static async Task Connect(HubConnection hubConnection,RichTextBox output,Button connectBtn, Button liveShareBtn,
           string password,string sessionId, TextEditorControl textEditorControl)
        {
            if (GlobalVariables.apiConnected)
            {
                if (hubConnection != null)
                    await hubConnection.StopAsync();
                GlobalVariables.apiConnected = false;
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

                hubConnection.On<string>("GetSend", (code) =>
                {
                    SetLiveCode(textEditorControl,code, password, output);
                });

                try
                {
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, string.Empty);
                    GlobalVariables.apiConnected = true;
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
        public async Task SendData(HubConnection hubConnection,string password,string sessionId,TextEditorControl textEditorControl, RichTextBox output)
        {
            try
            {
                if (!GlobalVariables.codeWriter)
                {
                    var encyrpted = AESEncryption.Encrypt(textEditorControl.Text, password);
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted);
                }
            }
            catch (Exception ex)
            {
                output.Text = ex.ToString(); // TODO: remove after tests.
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
        public static async Task StartShare(HubConnection hubConnection,string password, string sessionId, Button startShareBtn, 
            Button connectBtn, RichTextBox output, TextEditorControl textEditorControl)
        {
            if (GlobalVariables.apiConnected)
            {
                startShareBtn.Text = "Start Live Share";
                if (hubConnection != null)
                    await hubConnection.StopAsync();

                connectBtn.Enabled = true;

                GlobalVariables.apiConnected = false;
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

                hubConnection.On<string>("GetSend", (code) =>
                {
                    SetLiveCode(textEditorControl,code, password, output);
                });

                try
                {
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, string.Empty);
                    GlobalVariables.apiConnected = true;
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
        public static void SetLiveCode(TextEditorControl textEditorControl, string code, string password, RichTextBox output)
        {
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    var decrypt = AESEncryption.Decrypt(code, password);
                    textEditorControl.Text = decrypt;
                }
                GlobalVariables.codeWriter = true;
            }
            catch ( Exception ex)
            {
                output.Text = ex.ToString();
            }
        }
    }
}
