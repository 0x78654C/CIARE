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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE;

internal static class LiveShareRegression
{
    internal static void Run(MainForm form, Action<bool, string> assert, string scenario)
    {
        if (scenario == "live-share-connection")
        {
            // Async button handlers need the same persistent UI message loop as the application.
            // Standalone DoEvents calls uninstall the WinForms synchronization context on exit.
            using var context = new ApplicationContext();
            Exception failure = null;
            form.BeginInvoke((Action)(() =>
            {
                try { RunConnectionChecks(form, assert); }
                catch (Exception error) { failure = error; }
                finally { context.ExitThread(); }
            }));
            Application.Run(context);
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            return;
        }
        RunSession(form, assert, scenario, disconnectNotifications: true);
        RunSession(form, assert, scenario, disconnectNotifications: false);
    }

    private static void RunSession(MainForm form, Action<bool, string> assert, string scenario, bool disconnectNotifications)
    {
        LiveShareTestHub.Messages = 0;
        LiveShareTestHub.DisconnectNotifications = disconnectNotifications;
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
        var receiver = LiveShareConnection.Create(url);
        var otherParticipant = new HubConnectionBuilder().WithUrl(url).Build();
        var replies = new ConcurrentQueue<(string Code, string Position)>();
        sender.On<string, string, string>("GetSend", (code, position, _) => replies.Enqueue((code, position)));
        var otherReplies = new ConcurrentQueue<(string Code, string Position, string Id)>();
        otherParticipant.On<string, string, string>("GetSend", (code, position, id) => otherReplies.Enqueue((code, position, id)));
        var editor = SelectedEditor.GetSelectedEditor();
        var area = editor.ActiveTextAreaControl.TextArea;
        using var source = new TextEditorControl();
        using var senderDisplay = new UserDisplay(source);
        using var senderPresence = new LiveShareCaretPublisher(sender, source, senderDisplay, "local-test");
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
            source.ActiveTextAreaControl.Caret.Position = new TextLocation(3, 0);
            senderPresence.Start();
            int beforeHeartbeat = changed;
            Pump(UserDisplay.PresenceTimeoutMilliseconds + 1500);
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "Heartbeats keep an idle participant visible beyond the disconnect timeout");
            assert(changed == beforeHeartbeat, "Presence heartbeats do not rewrite the document");

            ApiConnectionEvents.StartPresence(receiver);
            Complete(sender.InvokeAsync("GetSendCode", "local-test", AESEncryption.Encrypt(source.Text, password), "0|3|Ana"));
            PumpUntil(() => editor.Text == source.Text);
            Complete(otherParticipant.StartAsync());
            Complete(otherParticipant.InvokeAsync("GetSendCode", "local-test", string.Empty, "50|3|Ana"));
            Pump(100);
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "Remote name and caret are visible before disconnect");
            string disconnectedId = sender.ConnectionId;
            if (!disconnectNotifications)
                Complete(senderPresence.LeaveAsync());
            Complete(sender.StopAsync());
            PumpUntil(() =>
            {
                using var snapshot = Capture(area);
                return BadgeBounds(snapshot).IsEmpty;
            });
            using (var snapshot = Capture(area))
            {
                assert(BadgeBounds(snapshot).IsEmpty, "Remote disconnect removes the blue name and caret");
                assert(!BadgeBounds(snapshot, local: true).IsEmpty, "Remote disconnect preserves the local name and caret");
                snapshot.Save(Path.Combine(AppContext.BaseDirectory, scenario +
                    (disconnectNotifications ? "-remote-disconnect.png" : "-legacy-disconnect.png")));
            }
            assert(receiver.State == HubConnectionState.Connected && editor.Text == source.Text,
                "Remote disconnect preserves the local connection and shared document");
            area.VirtualTop = new Point(0, area.TextView.FontHeight * 45);
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot).IsEmpty, "Another connected participant with the same nickname keeps their caret");
            area.VirtualTop = Point.Empty;

            Complete(sender.StartAsync());
            assert(sender.ConnectionId != disconnectedId, "Rejoining participant gets a new connection identity");
            Complete(sender.InvokeAsync("GetSendCode", "local-test", string.Empty, "0|3|Ana"));
            PumpUntil(() =>
            {
                using var snapshot = Capture(area);
                return !BadgeBounds(snapshot).IsEmpty;
            });
            Complete(sender.SendAsync("AbortConnection"));
            PumpUntil(() =>
            {
                using var snapshot = Capture(area);
                return BadgeBounds(snapshot).IsEmpty;
            });
            assert(receiver.State == HubConnectionState.Connected, "Unexpected remote disconnect also removes its name and caret");
            using (var snapshot = Capture(area))
                assert(!BadgeBounds(snapshot, local: true).IsEmpty, "Missing remote heartbeats never expire the local marker");

            Complete(otherParticipant.InvokeAsync("GetSendCode", "local-test", string.Empty, "0|3|Ana"));
            PumpUntil(() =>
            {
                using var snapshot = Capture(area);
                return !BadgeBounds(snapshot).IsEmpty;
            });
            string receiverId = receiver.ConnectionId;
            Complete(ApiConnectionEvents.StopConnection(receiver));
            PumpUntil(() => otherReplies.Any(reply => reply.Id == receiverId && reply.Code == string.Empty && reply.Position == "0|0"));
            assert(receiver.State == HubConnectionState.Disconnected,
                "Stopping a session announces departure through the existing relay before closing the connection");
            Pump(100);
            using (var snapshot = Capture(area))
                assert(BadgeBounds(snapshot).IsEmpty && BadgeBounds(snapshot, local: true).IsEmpty,
                    "Local disconnect removes all names and carets");
            GlobalVariables.connected = false;
            GlobalVariables.liveDisconnected = false;
        }
        finally
        {
            GlobalVariables.connected = false;
            ApiConnectionEvents.ClearUserDisplay();
            Complete(sender.DisposeAsync().AsTask());
            Complete(receiver.DisposeAsync().AsTask());
            Complete(otherParticipant.DisposeAsync().AsTask());
            Complete(server.DisposeAsync().AsTask());
            form.hubConnection = null;
            GlobalVariables.liveDisconnected = false;
        }
    }

    private static void RunConnectionChecks(MainForm form, Action<bool, string> assert)
    {
        Control.CheckForIllegalCrossThreadCalls = true;
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0"); builder.Services.AddSignalR();
        var server = builder.Build();
        int pingRequests = 0;
        server.Use(async (context, next) => { if (context.Request.Path.Value.EndsWith("/ping")) Interlocked.Increment(ref pingRequests); await next(); });
        server.MapHub<LiveShareTestHub>("/collaboration");
        Complete(server.StartAsync());
        string url = server.Urls.Single() + "/collaboration";
        const string password = "latest-signalr-password";
        var observer = new HubConnectionBuilder().WithUrl(url).Build();
        var received = new ConcurrentQueue<(string Code, string Id)>();
        observer.On<string, string, string>("GetSend", (code, _, id) => received.Enqueue((code, id)));
        var editor = SelectedEditor.GetSelectedEditor();
        editor.Text = "class SharedSource { }";
        GlobalVariables.apiUrl = url;
        using var dialogs = DismissMessages();
        try
        {
            Complete(observer.StartAsync());
            assert(typeof(HubConnection).Assembly.GetName().Version.Major >= 11, "Connection tests run with the upgraded SignalR client");
            using (var manage = new LiveShareHost { ShowInTaskbar = false, Opacity = 0 })
            {
                manage.Show(form); Application.DoEvents();
                Control Field(string name) => manage.Controls.Find(name, true).Single();
                Field("nicknameTxt").Text = "SignalR tester"; Field("passwordTxt").Text = password; Field("sessionTxt").Text = "connection-test";
                var start = (Button)Field("startLiveBtn");
                assert(manage.Visible && start.Enabled && form.LiveShareEventsFeature.CanShareEditor(editor), "The host dialog is ready on an ordinary code tab");
                start.PerformClick();
                assert(!start.Enabled, "Starting disables duplicate connection attempts");
                PumpUntil(() => GlobalVariables.apiConnected && start.Enabled);
                var connection = form.hubConnection;
                PumpUntil(() => received.Any(r => r.Id == connection.ConnectionId && !string.IsNullOrEmpty(r.Code)));
                assert(connection.State == HubConnectionState.Connected && pingRequests == 0,
                    "The real Start button connects to a custom hub route without requiring a ping endpoint");
                assert(AESEncryption.Decrypt(received.First(r => !string.IsNullOrEmpty(r.Code)).Code, password) == editor.Text,
                    "The host publishes encrypted source through SignalR 11");
                int reconnecting = 0;
                connection.Reconnecting += _ => { Interlocked.Increment(ref reconnecting); return Task.CompletedTask; };
                string oldId = connection.ConnectionId;
                editor.Text = "class SharedSource { public int Updated; }";
                GlobalVariables.isConnected = true; // Suppress the informational participant-joined dialog.
                Complete(connection.SendAsync("BreakTransport"));
                PumpUntil(() => reconnecting > 0 && GlobalVariables.connected && connection.State == HubConnectionState.Connected && connection.ConnectionId != oldId);
                PumpUntil(() => received.Any(r => r.Id == connection.ConnectionId && !string.IsNullOrEmpty(r.Code)));
                assert(received.Any(r => r.Id == connection.ConnectionId && !string.IsNullOrEmpty(r.Code) && AESEncryption.Decrypt(r.Code, password) == editor.Text),
                    "Automatic reconnect registers the new connection ID and republishes the host's current document");
                assert(GlobalVariables.apiConnected && !GlobalVariables.liveDisconnected, "Reconnection restores the host's UI state");

                GlobalVariables.apiUrl = "http://127.0.0.1:1/offline"; Field("passwordTxt").Text = "";
                start.PerformClick();
                PumpUntil(() => !GlobalVariables.connected && !GlobalVariables.apiConnected && start.Enabled);
                assert(connection.State == HubConnectionState.Disconnected && !GlobalVariables.liveDisconnected && !editor.ReadOnly,
                    "Stop remains available without a reachable health endpoint or password validation");
                manage.Close();
            }

            GlobalVariables.apiUrl = url; received.Clear();
            using (var join = new LiveShareHost { ShowInTaskbar = false, Opacity = 0 })
            {
                join.Show(form); Application.DoEvents();
                join.Controls.Find("remotePasswordTxt", true).Single().Text = password;
                join.Controls.Find("remoteSessioniDtxt", true).Single().Text = "connection-test";
                ((Button)join.Controls.Find("connectHostBtn", true).Single()).PerformClick();
                PumpUntil(() => GlobalVariables.apiRemoteConnected && !join.Visible);
            }
            var joined = form.hubConnection;
            PumpUntil(() => received.Any(r => r.Id == joined.ConnectionId && r.Code == "remote"));
            assert(joined.State == HubConnectionState.Connected && GlobalVariables.apiRemoteConnected,
                "The real Join button creates a fresh connection after a stopped host session");
            Complete(observer.InvokeAsync("GetSendCode", "connection-test", AESEncryption.Encrypt("// remote snapshot", password), "0|0|Host"));
            PumpUntil(() => editor.Text == "// remote snapshot");
            assert(!editor.ReadOnly, "A joined snapshot unlocks the shared editor");
            string joinedId = joined.ConnectionId;
            Complete(joined.SendAsync("BreakTransport"));
            PumpUntil(() => GlobalVariables.connected && joined.State == HubConnectionState.Connected && joined.ConnectionId != joinedId);
            PumpUntil(() => received.Any(r => r.Id == joined.ConnectionId && r.Code == "remote"));
            assert(GlobalVariables.apiRemoteConnected && !GlobalVariables.apiConnected, "A reconnecting guest requests the session snapshot and keeps its guest role");
            Complete(ApiConnectionEvents.StopConnection(joined)); Complete(joined.DisposeAsync().AsTask());

            foreach (bool rejectInvocation in new[] { false, true })
            {
                var failed = LiveShareConnection.Create(rejectInvocation ? url : url + "/missing");
                form.hubConnection = failed;
                bool rejected = false;
                try { Complete(ApiConnectionEvents.StartSession(failed, editor, password, rejectInvocation ? "reject" : "connection-test", hosting: false)); }
                catch (Exception) { rejected = true; }
                assert(rejected && failed.State == HubConnectionState.Disconnected && !GlobalVariables.connected && !GlobalVariables.apiRemoteConnected && !editor.ReadOnly,
                    rejectInvocation ? "A failed join invocation cleans up the transport and restores editor access" : "Negotiation errors are surfaced without leaving a stuck session");
                Complete(failed.DisposeAsync().AsTask());
            }
            var cancelled = LiveShareConnection.Create(url); form.hubConnection = cancelled;
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel(); bool wasCancelled = false;
                try { Complete(ApiConnectionEvents.StartSession(cancelled, editor, password, "connection-test", true, cancellation.Token)); }
                catch (OperationCanceledException) { wasCancelled = true; }
                assert(wasCancelled && !GlobalVariables.connected && cancelled.State == HubConnectionState.Disconnected, "Cancelling an initial connection leaves it ready for another attempt");
            }
            Complete(ApiConnectionEvents.StartSession(cancelled, editor, password, "connection-test", hosting: true));
            assert(GlobalVariables.connected && GlobalVariables.apiConnected, "Starting again after cancellation succeeds");
            Complete(ApiConnectionEvents.StopConnection(cancelled));
            Pump(150);
            assert(cancelled.State == HubConnectionState.Disconnected && !GlobalVariables.connected, "An explicit stop does not trigger automatic reconnect");
            Complete(cancelled.DisposeAsync().AsTask());
        }
        finally
        {
            if (form.hubConnection != null) Complete(form.hubConnection.DisposeAsync().AsTask());
            ApiConnectionEvents.ClearUserDisplay(); form.hubConnection = null;
            GlobalVariables.connected = GlobalVariables.apiConnected = GlobalVariables.apiRemoteConnected = GlobalVariables.liveDisconnected = false;
            editor.ReadOnly = false;
            Complete(observer.DisposeAsync().AsTask()); Complete(server.DisposeAsync().AsTask());
        }
    }

    private static System.Windows.Forms.Timer DismissMessages()
    {
        uint thread = GetCurrentThreadId();
        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) => EnumThreadWindows(thread, (handle, _) =>
        {
            var name = new System.Text.StringBuilder(64); GetClassName(handle, name, name.Capacity);
            if (name.ToString() == "#32770")
            {
                EnumChildWindows(handle, (child, _) =>
                {
                    var text = new System.Text.StringBuilder(2048); GetWindowText(child, text, text.Capacity);
                    if (text.Length > 4) Console.Error.WriteLine("Live Share dialog: " + text);
                    return true;
                }, IntPtr.Zero);
                SendMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE dismisses OK-only dialogs.
            }
            return true;
        }, IntPtr.Zero);
        timer.Start(); return timer;
    }
    private delegate bool WindowCallback(IntPtr handle, IntPtr argument);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, IntPtr argument);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, WindowCallback callback, IntPtr argument);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, System.Text.StringBuilder text, int length);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, System.Text.StringBuilder name, int length);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

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
            if (elapsed.ElapsedMilliseconds > 15000)
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
    internal static bool DisconnectNotifications;
    public async Task GetSendCode(string sessionId, string code, string position)
    {
        if (sessionId == "reject") throw new HubException("Session rejected for connection regression.");
        if (position?.Length > 64)
            throw new HubException("Position exceeds deployed server limit.");
        Interlocked.Increment(ref Messages);
        LastPosition = position;
        await Clients.Others.SendAsync("GetSend", code, position, Context.ConnectionId);
    }

    public void AbortConnection() => Context.Abort();
    public void BreakTransport() => Context.GetHttpContext().Abort();

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (DisconnectNotifications)
            await Clients.Others.SendAsync("UserDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
