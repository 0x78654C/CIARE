using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CIARE.LiveShareManage;

// Keep the session attached to the connection that owns it. SignalR callbacks run off the UI thread.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class LiveShareConnection
{
    private static readonly ConditionalWeakTable<HubConnection, LiveShareConnection> Sessions = new();
    private readonly HubConnection _connection;
    private readonly MainForm _main;
    private TextEditorControl _editor;
    private string _password, _sessionId;
    private bool _hosting, _stopping;

    internal static HubConnection Create(string url)
    {
        if (!Utils.Options.LiveShare.TryNormalizeApiUrl(url, out string normalized))
            throw new ArgumentException("Use an HTTPS Live Share hub URL. HTTP is allowed for localhost.");
        var connection = new HubConnectionBuilder().WithUrl(normalized, options =>
        {
            options.TransportMaxBufferSize = 8L * 1024 * 1024;
            options.ApplicationMaxBufferSize = 8L * 1024 * 1024;
        }).WithAutomaticReconnect(ApiConnectionEvents.DefaultBackoffTimes).Build();
        Attach(connection);
        return connection;
    }

    internal static LiveShareConnection Attach(HubConnection connection) => Sessions.GetValue(connection, c => new LiveShareConnection(c));
    private LiveShareConnection(HubConnection connection)
    {
        _connection = connection; _main = MainForm.Instance;
        connection.Reconnecting += Reconnecting;
        connection.Reconnected += Reconnected;
        connection.Closed += Closed;
    }
    internal void Configure(TextEditorControl editor, string password, string sessionId, bool hosting)
    {
        _editor = editor; _password = password; _sessionId = sessionId; _hosting = hosting; _stopping = false;
    }
    internal void Stopping() => _stopping = true;
    private bool IsCurrent => _main != null && !_main.IsDisposed && ReferenceEquals(_main.hubConnection, _connection);

    private Task Reconnecting(Exception error) => OnUI(() =>
    {
        if (!IsCurrent || _stopping) return Task.CompletedTask;
        GlobalVariables.connected = false;
        GlobalVariables.liveDisconnected = true;
        ApiConnectionEvents.ClearUserDisplay(_connection);
        _main.liveStatusPb.Image = Properties.Resources.orange_dot;
        return Task.CompletedTask;
    });

    private Task Reconnected(string connectionId) => OnUI(async () =>
    {
        if (!IsCurrent || _stopping || _editor == null || _editor.IsDisposed) return;
        try
        {
            // A new transport connection has a new ID and must register with the relay again.
            ApiConnectionEvents.RegisterReceiver(_connection, _editor, _password, _hosting, _sessionId);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await ApiConnectionEvents.SendInitialDocument(_connection, _editor, _password, _sessionId, _hosting, timeout.Token);
            if (!IsCurrent || _stopping || _connection.State != HubConnectionState.Connected) return;
            ApiConnectionEvents.MarkConnected(_connection, _hosting);
        }
        catch (Exception error)
        {
            if (!IsCurrent || _stopping) return;
            _main.outputRBT.AppendText("Live Share could not rejoin the session: " + error.Message + Environment.NewLine);
            await ApiConnectionEvents.StopConnection(_connection);
        }
    });

    private Task Closed(Exception error) => OnUI(() =>
    {
        if (!IsCurrent || _connection.State != HubConnectionState.Disconnected) return Task.CompletedTask;
        Reset();
        if (error != null && !_stopping)
            _main.outputRBT.AppendText("Live Share disconnected: " + error.Message + Environment.NewLine);
        return Task.CompletedTask;
    });

    internal void Reset()
    {
        ApiConnectionEvents.ClearUserDisplay(_connection);
        if (!IsCurrent) return;
        if (_editor != null && !_editor.IsDisposed) _editor.ReadOnly = false;
        GlobalVariables.connected = GlobalVariables.apiConnected = GlobalVariables.apiRemoteConnected = false;
        GlobalVariables.liveDisconnected = GlobalVariables.isConnected = false;
        GlobalVariables.reconnectionCount = 6;
        GlobalVariables.livePassword = string.Empty;
        GlobalVariables.remoteConnectionId = string.Empty;
        _main.liveStatusPb.Image = null;
    }

    private Task OnUI(Func<Task> action)
    {
        if (_main == null || _main.IsDisposed || !_main.IsHandleCreated) return Task.CompletedTask;
        if (!_main.InvokeRequired) return action();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _main.BeginInvoke((Action)(async () =>
            {
                try { if (!_main.IsDisposed) await action(); completion.TrySetResult(); }
                catch (Exception error) { completion.TrySetException(error); }
            }));
        }
        catch (InvalidOperationException) { completion.TrySetResult(); }
        return completion.Task;
    }
}
