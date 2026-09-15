using System;
using System.Drawing;
using System.Linq;
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
        private static UserDisplay _userDisplay;
        private static TextEditorControl _sharedEditor;
        private static HubConnection _displayConnection;
        private static LiveShareCaretPublisher _caretPublisher;

        internal static void RegisterReceiver(HubConnection hubConnection, TextEditorControl editor, string password, bool hosting)
        {
            ClearUserDisplay();
            _sharedEditor = editor;
            _displayConnection = hubConnection;
            var display = _userDisplay = new UserDisplay(editor);
            _caretPublisher = new LiveShareCaretPublisher(hubConnection, editor, display, GlobalVariables.sessionId);
            // Reconnecting must not apply each incoming edit more than once.
            hubConnection.Remove("GetSend");
            hubConnection.On<string, string, string>("GetSend", (code, position, connectionId) =>
            {
                RunOnEditor(editor, () =>
                {
                    if (!ReferenceEquals(_userDisplay, display))
                        return;
                    GlobalVariables.remoteConnectionId = connectionId;
                    // The server uses the recipient's ID for the initial document snapshot.
                    string participantId = connectionId == hubConnection.ConnectionId ? null : connectionId;
                    SetLiveCode(editor, code, password, position, participantId);
                    if (code == "remote" && participantId != null)
                    {
                        // The hub caches only code, not the host's identity. Reply with the
                        // current document so new arrivals see the host's name and caret too.
                        if (hosting)
                            _ = new ApiConnectionEvents().SendData(hubConnection, password, GlobalVariables.sessionId, editor);
                        else
                            _caretPublisher?.Queue();
                    }
                    if (hosting && participantId != null)
                        DisplayConnectedUser(ref GlobalVariables.isConnected, connectionId);
                });
            });
        }

        private static void RunOnEditor(TextEditorControl editor, Action action)
        {
            if (editor == null || editor.IsDisposed || !editor.IsHandleCreated)
                return;
            if (editor.InvokeRequired)
            {
                try
                {
                    editor.BeginInvoke((Action)(() =>
                    {
                        if (!editor.IsDisposed)
                            action();
                    }));
                }
                catch (InvalidOperationException) { } // Editor closed while dispatching.
            }
            else
                action();
        }

        internal static void ClearUserDisplay(HubConnection connection = null)
        {
            if (connection != null && !ReferenceEquals(connection, _displayConnection))
                return;
            var display = _userDisplay;
            var editor = _sharedEditor;
            var publisher = _caretPublisher;
            _caretPublisher = null;
            _userDisplay = null;
            _sharedEditor = null;
            _displayConnection = null;
            if (display != null)
                RunOnEditor(editor, () =>
                {
                    publisher?.Dispose();
                    display.Dispose();
                });
        }

        internal static void StartPresence(HubConnection connection)
        {
            if (ReferenceEquals(connection, _displayConnection))
                _caretPublisher?.Start();
        }

        private static string GetPosition(TextEditorControl editor) => LiveSharePosition.Encode(
            GoToLineNumber.GetLineNumber(editor), GoToLineNumber.GetColumnNumber(editor), GlobalVariables.liveShareNickname);

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
                ClearUserDisplay(hubConnection);
                if (!ReferenceEquals(MainForm.Instance.hubConnection, hubConnection))
                    return Task.CompletedTask;
                GlobalVariables.liveDisconnected = true;
                var form = MainForm.Instance;
                if (!form.IsDisposed && form.IsHandleCreated)
                {
                    try
                    {
                        form.BeginInvoke((Action)(() =>
                        {
                            if (!form.IsDisposed && ReferenceEquals(form.hubConnection, hubConnection))
                                CleanDot(form.liveStatusPb);
                        }));
                    }
                    catch (InvalidOperationException) { } // Main window closed while disconnecting.
                }
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
                ClearUserDisplay();
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

                RegisterReceiver(hubConnection, textEditorControl, password, hosting: false);

                try
                {
                    connectBtn.Text = "Stop Connection";
                    await hubConnection.StartAsync();
                    if (!textEditorControl.ReadOnly)
                        textEditorControl.ReadOnly = true;
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, "remote", GetPosition(textEditorControl));
                    MainForm.Instance.liveStatusPb.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiRemoteConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    GlobalVariables.reconnectionCount = 6;
                    StartPresence(hubConnection);
                    liveShareBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        liveShareBtn.BackColor = Color.Gray;
                    MessageBox.Show("Connected to remote session!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.Close();
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
            ClearUserDisplay();
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
                if (!GlobalVariables.codeWriter && hubConnection?.State == HubConnectionState.Connected &&
                    textEditorControl != null && !textEditorControl.IsDisposed)
                {
                    var encyrpted = AESEncryption.Encrypt(textEditorControl.Text, password);
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, GetPosition(textEditorControl));
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Close api hub connection.
        /// </summary>
        public async Task CloseConnection(HubConnection hubConnection)
        {
            ClearUserDisplay();
            MainForm.Instance.liveStatusPb.Image = Properties.Resources.orange_dot;
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
                ClearUserDisplay();
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
            }
            else
            {
                startShareBtn.Text = "Stop Live Share";
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("No password provied!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RegisterReceiver(hubConnection, textEditorControl, password, hosting: true);

                try
                {
                    string code = textEditorControl.Text;
                    var encyrpted = AESEncryption.Encrypt(code, password);
                    await hubConnection.StartAsync();
                    await hubConnection.InvokeAsync("GetSendCode", sessionId, encyrpted, GetPosition(textEditorControl));
                    MainForm.Instance.liveStatusPb.Image = Properties.Resources.red_dot;
                    GlobalVariables.apiConnected = true;
                    GlobalVariables.connected = true;
                    GlobalVariables.liveDisconnected = false;
                    GlobalVariables.reconnectionCount = 6;
                    StartPresence(hubConnection);
                    connectBtn.Enabled = false;
                    if (GlobalVariables.darkColor)
                        connectBtn.BackColor = Color.Gray;
                    MessageBox.Show("Live Share started!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        public static void SetLiveCode(TextEditorControl textEditorControl, string code, string password, string position,
            string connectionId = null)
        {
            if (textEditorControl == null || textEditorControl.IsDisposed)
                return;
            RunOnEditor(textEditorControl, () =>
            {
                bool presenceOnly = code == "remote" || string.IsNullOrEmpty(code);
                var decrypt = presenceOnly ? null : AESEncryption.Decrypt(code, password);
                if (!presenceOnly && string.IsNullOrEmpty(decrypt))
                    return;
                GlobalVariables.codeWriter = true;
                try
                {
                    var area = textEditorControl.ActiveTextAreaControl.TextArea;
                    if (!presenceOnly)
                    {
                        RemoveReadOnlyTextEditor(textEditorControl);
                        if (textEditorControl.Text != decrypt)
                        {
                            // Replacing the shared text resets the editor's caret and selection.
                            // Restore the local editing position, independently of the sender.
                            TextLocation localCaret = area.Caret.Position;
                            Point viewport = area.VirtualTop;
                            var selections = area.SelectionManager.SelectionCollection
                                .Select(selection => (selection.StartPosition, selection.EndPosition)).ToArray();
                            textEditorControl.Text = decrypt;
                            area.Caret.Position = area.Caret.ValidatePosition(localCaret);
                            area.SelectionManager.ClearSelection();
                            foreach (var selection in selections)
                                area.SelectionManager.SetSelection(area.Caret.ValidatePosition(selection.StartPosition),
                                    area.Caret.ValidatePosition(selection.EndPosition));
                            area.VirtualTop = viewport;
                        }
                    }
                    if (LiveSharePosition.TryDecode(position, out int line, out int column, out string nickname))
                    {
                        line = Math.Clamp(line, 0, area.Document.TotalNumberOfLines - 1);
                        column = Math.Clamp(column, 0, area.Document.GetLineSegment(line).Length);
                        if (ReferenceEquals(textEditorControl, _sharedEditor))
                            _userDisplay?.Show(connectionId, nickname, line, column);
                    }
                    textEditorControl.Refresh();
                }
                finally
                {
                    GlobalVariables.codeWriter = false;
                }
            });
        }

        /// <summary>
        /// Display when remote user is connected to host.
        /// </summary>
        /// <param name="connected"></param>
        /// <param name="connectionId"></param>
        private static void DisplayConnectedUser(ref bool connected, string connectionId)
        {
            if (!connected)
            {
                MessageBox.Show($"Remote user is connected!", "CIARE - Live Share", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            ClearUserDisplay();
            GlobalVariables.apiConnected = false;
            GlobalVariables.apiRemoteConnected = false;
            GlobalVariables.connected = false;
            if (hubConnection != null)
                await hubConnection.StopAsync();
            MainForm.Instance.liveStatusPb.Image = Properties.Resources.orange_dot;
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
                    await Connect(new Form(), hubConnection, fakeButton, fakeButton,
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
