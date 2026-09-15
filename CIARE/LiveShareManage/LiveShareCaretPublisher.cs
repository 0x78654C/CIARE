using CIARE.Utils;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Windows.Forms;

namespace CIARE.LiveShareManage
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class LiveShareCaretPublisher : IDisposable
    {
        private readonly HubConnection _connection;
        private readonly TextEditorControl _editor;
        private readonly UserDisplay _display;
        private readonly string _sessionId;
        private readonly Timer _timer;
        private bool _started;
        private bool _disposed;

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
            _timer.Start();
        }

        private void CaretMoved(object sender, EventArgs e) => Queue();

        private async void Publish(object sender, EventArgs e)
        {
            _timer.Stop();
            if (_disposed || _connection.State != HubConnectionState.Connected || GlobalVariables.codeWriter)
                return;
            var caret = _editor.ActiveTextAreaControl.Caret;
            string position = LiveSharePosition.Encode(caret.Line, caret.Column, GlobalVariables.liveShareNickname);
            try
            {
                // Empty code carries presence only; moving a cursor never replaces a document.
                await _connection.InvokeAsync("GetSendCode", _sessionId, string.Empty, position);
            }
            catch
            {
                // The connection's Closed handler handles loss of connectivity.
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
