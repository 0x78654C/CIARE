using ICSharpCode.TextEditor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace CIARE.LiveShareManage
{
    /// <summary>Paints remote typing indicators without taking keyboard focus.</summary>
    [SupportedOSPlatform("windows")]
    internal sealed class UserDisplay : IDisposable
    {
        private const int DisplayMilliseconds = 3000;
        private const string LocalParticipant = "\0local";
        private readonly TextArea _textArea;
        private readonly Timer _timer;
        private readonly Dictionary<string, Participant> _participants = new();
        private bool _disposed;

        private sealed record Participant(string Nickname, int Line, int Column, long UpdatedAt);

        internal UserDisplay(TextEditorControl editor)
        {
            _textArea = editor.ActiveTextAreaControl.TextArea;
            _textArea.Paint += Paint;
            _textArea.Disposed += TextAreaDisposed;
            //_timer = new Timer { Interval = 250 };
            //_timer.Tick += Expire;
        }

        internal void Show(string connectionId, string nickname, int line, int column)
        {
            if (_disposed || string.IsNullOrEmpty(connectionId))
                return;
            if (!LiveSharePosition.TryNormalizeNickname(nickname, out string normalized))
                _participants.Remove(connectionId);
            else
                _participants[connectionId] = new Participant(normalized, line, column, Environment.TickCount64);
            _timer.Enabled = _participants.Count > 0;
            _textArea.Invalidate();
        }

        internal void ShowLocal(string nickname, int line, int column) => Show(LocalParticipant, nickname, line, column);

        private void Expire(object sender, EventArgs e)
        {
            var expired = new List<string>();
            foreach (var entry in _participants)
            {
                if (Environment.TickCount64 - entry.Value.UpdatedAt >= DisplayMilliseconds)
                    expired.Add(entry.Key);
            }
            foreach (string connectionId in expired)
                _participants.Remove(connectionId);
            if (expired.Count > 0)
                _textArea.Invalidate();
            _timer.Enabled = _participants.Count > 0;
        }

        private void Paint(object sender, PaintEventArgs e)
        {
            Rectangle viewport = _textArea.TextView.DrawingPosition;
            if (viewport.Width <= 8 || viewport.Height <= 0)
                return;
            var state = e.Graphics.Save();
            try
            {
                e.Graphics.SetClip(viewport, System.Drawing.Drawing2D.CombineMode.Intersect);
                var badges = new List<Rectangle>();
                foreach (var entry in _participants)
                {
                    Participant participant = entry.Value;
                    int line = Math.Clamp(participant.Line, 0, _textArea.Document.TotalNumberOfLines - 1);
                    int column = Math.Clamp(participant.Column, 0, _textArea.Document.GetLineSegment(line).Length);
                    if (!_textArea.Document.FoldingManager.IsLineVisible(line))
                        continue;
                    int x = viewport.X + _textArea.TextView.GetDrawingXPos(line, column);
                    int y = viewport.Y + _textArea.Document.GetVisibleLine(line) * _textArea.TextView.FontHeight - _textArea.VirtualTop.Y;
                    if (!viewport.Contains(x, y))
                        continue;

                    Font font = _textArea.TextEditorProperties.FontContainer.RegularFont;
                    const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping;
                    bool isLocal = entry.Key == LocalParticipant;
                    string caption = isLocal ? participant.Nickname + " (you)" : participant.Nickname;
                    Size size = TextRenderer.MeasureText(e.Graphics, caption, font);
                    int width = Math.Min(viewport.Width, size.Width + 8);
                    int height = size.Height + 4;
                    // At the top edge put the badge below the caret to keep it readable.
                    int top = y - height - 2;
                    if (top < viewport.Top)
                        top = y + _textArea.TextView.FontHeight;
                    top = Math.Max(viewport.Top, Math.Min(top, viewport.Bottom - height));
                    var bounds = new Rectangle(Math.Clamp(x, viewport.Left, viewport.Right - width), top, width, height);
                    // Stack overlapping labels so people at the same position remain identifiable.
                    while (badges.Exists(previous => previous.IntersectsWith(bounds)) && bounds.Bottom + height + 2 <= viewport.Bottom)
                        bounds.Y += height + 2;
                    badges.Add(bounds);
                    using var background = new SolidBrush(isLocal ? Color.FromArgb(36, 112, 72) : Color.FromArgb(32, 96, 160));
                    using var caretPen = new Pen(background.Color, 2);
                    e.Graphics.DrawLine(caretPen, x, y, x, y + _textArea.TextView.FontHeight);
                    e.Graphics.FillRectangle(background, bounds);
                    bounds.Inflate(-4, 0);
                    TextRenderer.DrawText(e.Graphics, caption, font, bounds, Color.White, flags);
                }
            }
            finally
            {
                e.Graphics.Restore(state);
            }
        }

        private void TextAreaDisposed(object sender, EventArgs e) => Dispose();

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _timer.Stop();
            _timer.Tick -= Expire;
            _timer.Dispose();
            _textArea.Paint -= Paint;
            _textArea.Disposed -= TextAreaDisposed;
            _participants.Clear();
            if (!_textArea.IsDisposed)
                _textArea.Invalidate();
        }
    }
}
