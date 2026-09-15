using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using CIARE.Utils.Encryption;
using ICSharpCode.TextEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE;

internal static class LiveShareRegression
{
    internal static void Run(MainForm form, Action<bool, string> assert, string scenario)
    {
        Control.CheckForIllegalCrossThreadCalls = true;
        const string password = "local-test-password";
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSignalR();
        var server = builder.Build();
        server.MapHub<LiveShareTestHub>("/live");
        Complete(server.StartAsync());
        string url = server.Urls.Single() + "/live";
        var sender = new HubConnectionBuilder().WithUrl(url).Build();
        var receiver = new HubConnectionBuilder().WithUrl(url).Build();
        var replies = new ConcurrentQueue<(string Code, string Position)>();
        sender.On<string, string, string>("GetSend", (code, position, _) => replies.Enqueue((code, position)));
        var editor = SelectedEditor.GetSelectedEditor();
        var area = editor.ActiveTextAreaControl.TextArea;
        using var source = new TextEditorControl();
        try
        {
            GlobalVariables.apiUrl = url;
            using (var manage = new LiveShareHost { ShowInTaskbar = false, Opacity = 0 })
            {
                manage.Show();
                var nickname = (TextBox)manage.Controls["nicknameTxt"];
                assert(nickname.MaxLength == 32 && nickname.Visible, "Nickname field is available for hosting and joining");
                assert(!nickname.Bounds.IntersectsWith(manage.Controls["liveShareStartGrp"].Bounds), "Nickname does not overlap host controls");
                nickname.Text = "  Ana | Ștefan  ";
                using var snapshot = new Bitmap(manage.Width, manage.Height);
                manage.DrawToBitmap(snapshot, new Rectangle(Point.Empty, manage.Size));
                snapshot.Save(Path.Combine(AppContext.BaseDirectory, scenario + "-management.png"));
                manage.Close();
            }
            using (var reopened = new LiveShareHost())
                assert(reopened.Controls["nicknameTxt"].Text == "Ana | Ștefan", "Nickname is trimmed and remembered");

            string maximum = LiveSharePosition.Encode(int.MaxValue, int.MaxValue, new string('名', 32));
            assert(maximum.Length <= 64, "Maximum nickname and coordinates fit the deployed hub limit");
            assert(!LiveSharePosition.TryNormalizeNickname(" \t ", out _) &&
                !LiveSharePosition.TryNormalizeNickname("two\nlines", out _), "Blank and control-character nicknames are rejected");

            Complete(sender.StartAsync());
            Complete(receiver.StartAsync());
            form.hubConnection = receiver;
            GlobalVariables.livePassword = password;
            GlobalVariables.sessionId = "local-test";
            GlobalVariables.liveTabIndex = form.EditorTabControl.SelectedIndex;
            source.Text = string.Join("\n", Enumerable.Range(0, 80).Select(i => "\t// Shared line " + i));
            source.ActiveTextAreaControl.Caret.Position = new TextLocation(8, 5);
            editor.Text = source.Text.Replace("Shared line 5", "Original line 5");
            var localCaret = new TextLocation(1, 9);
            area.Caret.Position = localCaret;
            area.SelectionManager.SetSelection(localCaret, new TextLocation(5, 9));
            area.VirtualTop = new Point(0, area.TextView.FontHeight * 2);
            Point localViewport = area.VirtualTop;
            string localSelection = area.SelectionManager.SelectedText;
            GlobalVariables.connected = true;
            GlobalVariables.isConnected = true;
            ApiConnectionEvents.ApiConnection(receiver, url);
            ApiConnectionEvents.RegisterReceiver(receiver, editor, password, hosting: true);
            int changed = 0;
            editor.TextChanged += (_, _) => changed++;
            Complete(new ApiConnectionEvents().SendData(sender, password, "local-test", source));
            PumpUntil(() => editor.Text == source.Text);
            Pump(100);
            assert(changed == 1 && LiveShareTestHub.Messages == 1, "Remote edit is applied once without an echo");
            assert(LiveShareTestHub.LastPosition == "5|8|Ana | Ștefan", "Edit transports the configured Unicode nickname and caret");
            assert(area.Caret.Position == localCaret, "Remote typing preserves the local caret");
            assert(area.SelectionManager.SelectedText == localSelection, "Remote typing preserves the local selection");
            assert(area.VirtualTop == localViewport, "Remote typing preserves the local viewport");
            assert(!GlobalVariables.codeWriter, "Remote edit suppression is released for local typing");
            ApiConnectionEvents.StartPresence(receiver);
            PumpUntil(() => replies.Any(reply => reply.Code == string.Empty));
            assert(replies.Last().Position == "9|1|Ana | Ștefan", "Local presence advertises its own caret independently");
            area.VirtualTop = Point.Empty;
            using (var snapshot = Capture(area))
            {
                Rectangle badge = BadgeBounds(snapshot);
                assert(!badge.IsEmpty && badge.Top < area.TextView.FontHeight * 5, "Nickname is drawn above the separate remote caret");
                assert(!BadgeBounds(snapshot, local: true).IsEmpty, "Local nickname is displayed in a distinct color");
                snapshot.Save(Path.Combine(AppContext.BaseDirectory, scenario + "-caret.png"));
            }

            // Local caret movement must not relocate someone else's typing indicator.
            area.Caret.Position = new TextLocation(2, 10);
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).Bottom < area.Caret.ScreenPosition.Y, "Remote indicator is independent of the local caret");
            PumpUntil(() => replies.Any(reply => reply.Code == string.Empty && reply.Position.StartsWith("10|2|")));
            localCaret = area.Caret.Position;
            int beforeMovement = changed;
            Complete(sender.InvokeAsync("GetSendCode", "local-test", string.Empty, "6|7|Ana"));
            Pump(100);
            assert(changed == beforeMovement && area.Caret.Position == localCaret, "Remote cursor-only movement never rewrites text or moves the local caret");
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).Bottom >= area.TextView.FontHeight * 6, "Remote marker follows cursor movement without typing");

            replies.Clear();
            Complete(sender.InvokeAsync("GetSendCode", "local-test", "remote", "3|4|Joining user"));
            PumpUntil(() => replies.Any(reply => !string.IsNullOrEmpty(reply.Code)));
            var hostReply = replies.First(reply => !string.IsNullOrEmpty(reply.Code));
            assert(AESEncryption.Decrypt(hostReply.Code, password) == editor.Text && hostReply.Position == "10|2|Ana | Ștefan",
                "Joining participants receive the current document plus the host nickname and separate caret");
            assert(changed == beforeMovement && area.Caret.Position == localCaret, "Join announcement preserves the local document and caret");
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "The joining user's nickname appears before they type");

            area.VirtualTop = new Point(0, area.TextView.FontHeight * 30);
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).IsEmpty, "Offscreen participant labels are hidden");

            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt(source.Text, password), "0|0|Ana"));
            Pump(100);
            assert(area.Caret.Position == localCaret && area.VirtualTop.Y == area.TextView.FontHeight * 30,
                "An offscreen remote caret does not scroll the local editor");
            area.VirtualTop = Point.Empty;
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "First-line nickname remains within the editor");

            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt("legacy client", password), "0|4"));
            PumpUntil(() => editor.Text == "legacy client");
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).IsEmpty && area.Caret.Column == localCaret.Column, "Older clients still sync without moving the local caret or leaving a stale nickname");
            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt("malformed position", password), "bad|position|Name"));
            PumpUntil(() => editor.Text == "malformed position");
            assert(!GlobalVariables.codeWriter, "Malformed caret data does not block document updates");

            // Re-registering after reconnection must detach the previous receiver and badge.
            ApiConnectionEvents.RegisterReceiver(receiver, editor, password, hosting: false);
            changed = 0;
            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt("reconnected", password), "0|3|New name"));
            PumpUntil(() => editor.Text == "reconnected");
            assert(changed == 1, "Reconnection retains only one receive handler");
            ApiConnectionEvents.ClearUserDisplay(sender);
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "A stale connection cannot clear the current participant display");
            Pump(3300);
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).IsEmpty, "Typing badge expires after inactivity");

            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt("disconnect", password), "0|3|Ana"));
            PumpUntil(() => editor.Text == "disconnect");
            Complete(receiver.StopAsync());
            Pump(100);
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).IsEmpty, "Disconnect removes the typing badge");
            GlobalVariables.connected = false;
            GlobalVariables.liveDisconnected = false;
        }
        finally
        {
            GlobalVariables.connected = false;
            ApiConnectionEvents.ClearUserDisplay();
            Complete(sender.DisposeAsync().AsTask());
            Complete(receiver.DisposeAsync().AsTask());
            Complete(server.DisposeAsync().AsTask());
            form.hubConnection = null;
            GlobalVariables.liveDisconnected = false;
        }
    }

    private static Bitmap Capture(Control control)
    {
        control.Refresh();
        var bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, control.ClientRectangle);
        return bitmap;
    }

    private static Rectangle BadgeBounds(Bitmap bitmap, bool local = false)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        int color = (local ? Color.FromArgb(36, 112, 72) : Color.FromArgb(32, 96, 160)).ToArgb();
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).ToArgb() == color)
                {
                    left = Math.Min(left, x); top = Math.Min(top, y);
                    right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                }
        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static void Complete(Task task)
    {
        PumpUntil(() => task.IsCompleted);
        task.GetAwaiter().GetResult();
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.ElapsedMilliseconds > 10000)
                throw new TimeoutException("Live Share test did not complete.");
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static void Pump(int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        PumpUntil(() => elapsed.ElapsedMilliseconds >= milliseconds);
    }
}

// Mirrors the deployed hub's three-argument relay and caret payload limit.
public sealed class LiveShareTestHub : Hub
{
    internal static int Messages;
    internal static string LastPosition;
    public async Task GetSendCode(string sessionId, string code, string position)
    {
        if (position?.Length > 64)
            throw new HubException("Position exceeds deployed server limit.");
        Interlocked.Increment(ref Messages);
        LastPosition = position;
        await Clients.Others.SendAsync("GetSend", code, position, Context.ConnectionId);
    }
}
