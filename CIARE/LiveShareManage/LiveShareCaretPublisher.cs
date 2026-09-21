using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace CIARE.LiveShareManage
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class LiveShareCaretPublisher : IDisposable
    {
        private const int HeartbeatMilliseconds = 2000;
        private readonly HubConnection _connection;
        private readonly TextEditorControl _editor;
        private readonly UserDisplay _display;
        private readonly string _sessionId;
        private readonly Timer _timer;
        private bool _started;
        private bool _disposed;
        private bool _pending;
        private long _nextHeartbeat;
        private Task _publishTask = Task.CompletedTask;

        internal LiveShareCaretPublisher(HubConnection connection, TextEditorControl editor,
            UserDisplay display, string sessionId)
        {
            _connection = connection;
            _editor = editor;
            _display = display;
            _sessionId = sessionId;
            _timer = new Timer { Interval = 100 };
            _timer.Tick += Publish;
            editor.ActiveTextAreaControl.Caret.PositionChanged += CaretMoved;
            editor.Disposed += EditorDisposed;
        }

        internal void Start()
        {
            _started = true;
            Queue();
        }

        internal void Queue()
        {
            if (_disposed || !_started || GlobalVariables.codeWriter)
                return;
            var caret = _editor.ActiveTextAreaControl.Caret;
            _display.ShowLocal(GlobalVariables.liveShareNickname, caret.Line, caret.Column);
            _pending = true;
            _timer.Start();
        }

        private void CaretMoved(object sender, EventArgs e) => Queue();

        private async void Publish(object sender, EventArgs e)
        {
            if (_disposed || !_started || !_publishTask.IsCompleted ||
                _connection.State != HubConnectionState.Connected || GlobalVariables.codeWriter ||
                (!_pending && Environment.TickCount64 < _nextHeartbeat))
                return;
            _pending = false;
            _nextHeartbeat = Environment.TickCount64 + HeartbeatMilliseconds;
            var caret = _editor.ActiveTextAreaControl.Caret;
            string position = LiveSharePosition.Encode(caret.Line, caret.Column, GlobalVariables.liveShareNickname);
            try
            {
                // Empty code carries presence only; moving a cursor never replaces a document.
                _publishTask = _connection.InvokeAsync("GetSendCode", _sessionId, string.Empty, position);
                await _publishTask;
            }
            catch
            {
                // The connection's Closed handler handles loss of connectivity.
            }
        }

        internal async Task LeaveAsync()
        {
            Dispose();
            if (_connection.State != HubConnectionState.Connected)
                return;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                // Finish an in-flight heartbeat before announcing departure so it cannot
                // recreate the marker. A nameless caret works with the existing relay API.
                await _publishTask.WaitAsync(timeout.Token).ConfigureAwait(false);
                await _connection.InvokeAsync("GetSendCode", _sessionId, string.Empty, "0|0", timeout.Token)
                    .ConfigureAwait(false);
            }
            catch
            {
                // If the network is already down, peers expire the missing heartbeat.
            }
        }

        private void EditorDisposed(object sender, EventArgs e) => Dispose();

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _timer.Stop();
            _timer.Tick -= Publish;
            _timer.Dispose();
            _editor.ActiveTextAreaControl.Caret.PositionChanged -= CaretMoved;
            _editor.Disposed -= EditorDisposed;
        }
    }
}
