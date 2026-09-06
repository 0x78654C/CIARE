using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CIARE.AutoUpdater;
using CIARE.Updating;

internal static class Program
{
    private static int _checks;
    private static string _root;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--wait-parent")
        {
            using var exit = EventWaitHandle.OpenExisting(args[1]);
            return exit.WaitOne(TimeSpan.FromSeconds(40)) ? 0 : 2;
        }
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, ".updater-fixture")))
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, ".restarted"), "yes");
            return 0;
        }
        _root = Path.GetFullPath(Path.Combine("artifacts", "updater-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(_root);
        try
        {
            Catalog();
            Network().GetAwaiter().GetResult();
            LocalCopy().GetAwaiter().GetResult();
            Packages();
            RenderWindow();
            Handoff(cancel: true);
            Handoff(cancel: false);
            Handoff(cancel: false, packageVersion: "3.2.3.1");
            if (args.Length == 2 && args[0] == "--application") BundledStartup(args[1]);
            Console.WriteLine($"PASS: {_checks} updater checks. Artifacts: {_root}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
    }

    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException) { rejected = true; }
        Assert(rejected, message);
    }

    private static ReleaseAsset Asset(string name, byte[] content = null)
    {
        content ??= Encoding.UTF8.GetBytes("release bytes");
        return new() { Name = name, DownloadUrl = "https://github.com/0x78654C/CIARE/releases/download/test/" + name,
            Digest = "sha256:" + Convert.ToHexString(SHA256.HashData(content)), Size = content.Length, State = "uploaded" };
    }

    private static GitHubRelease Release(params string[] names) => new() { Assets = names.Select(n => Asset(n)).ToList() };

    private static void Catalog()
    {
        foreach (string name in new[] { "CIARE_v3.2.4-x64.zip", "CIARE_v3.2.4-x86.zip", "CIARE_v3.2.4.1-x86.zip", "CIARE_V3.2.4-X64.ZIP" })
            Assert(ReleaseCatalog.TryParsePackage(name, out _, out _), "Valid package: " + name);
        foreach (string name in new[] { "CIARE_v3.2-x64.zip", "CIARE_v3.2.4-arm64.zip", "CIARE_v3.2.4-beta-x64.zip", "xCIARE_v3.2.4-x64.zip", "CIARE_v3.2.4-x64.zip.exe", "CIARE_v3.2.4_x86.zip", "CIARE_v999999999999.2.4-x64.zip" })
            Assert(!ReleaseCatalog.TryParsePackage(name, out _, out _), "Invalid package: " + name);
        var releases = new[] { Release("CIARE_v3.9.0-x64.zip"), Release("CIARE_v3.10.0-x64.zip", "CIARE_v3.10.0-x86.zip", "unrelated.exe"),
            new GitHubRelease { Prerelease = true, Assets = new() { Asset("CIARE_v9.0.0-x64.zip") } },
            new GitHubRelease { Draft = true, Assets = new() { Asset("CIARE_v10.0.0-x64.zip") } } };
        var chosen = ReleaseCatalog.SelectUpdate(releases, new Version(3, 2, 3, 1), "x64");
        Assert(chosen.Version == new Version(3, 10, 0, 0), "Numeric order across stable releases");
        Assert(chosen.Package.Name == "CIARE_v3.10.0-x64.zip", "Only the matching release ZIP is selected");
        var x86 = ReleaseCatalog.SelectUpdate(releases, new Version(3, 2, 3), "x86");
        Assert(x86.Package.Name == "CIARE_v3.10.0-x86.zip", "x86 update needs only the release ZIP");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(3, 10, 0), "x64") == null, "Equal version is not an update");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(4, 0, 0), "x64") == null, "Never downgrade");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("CIARE_v3.2.3-x64.zip") }, new Version(3, 2, 3, 1), "x64") == null, "Revision does not trigger an older ZIP");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("CIARE_v3.2.3.2-x64.zip") }, new Version(3, 2, 3, 1), "x64") != null, "Revision update supported");
        var mixedVersions = new[] { Release("CIARE_v3.2.3-x64.zip", "CIARE_v3.2.3.1-x64.zip") };
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 2, 1), "x64").Package.Name == "CIARE_v3.2.3.1-x64.zip",
            "Four-part release supersedes its three-part base version");
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 3), "x64").Version == new Version(3, 2, 3, 1),
            "A nonzero revision updates an installed three-part version");
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 3, 1), "x64") == null,
            "Neither ZIP replaces an equal or newer installed version");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(3, 0, 0), "x64", new Version(3, 9, 0)).Version == new Version(3, 9, 0, 0), "Updater stays on approved release");
        Assert(ReleaseCatalog.SelectUpdate(new[] { releases[0] }, new Version(3, 0, 0), "x64") != null, "A release containing only the application ZIP can update");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("unrelated.exe") }, new Version(3, 0, 0), "x64") == null, "Executable release assets are ignored");
        var unfinished = Release("CIARE_v99.0.0-x64.zip");
        unfinished.Assets[0].State = "starter";
        Assert(ReleaseCatalog.SelectUpdate(new[] { unfinished }, new Version(3, 0, 0), "x64") == null, "Unfinished uploads skipped");
        foreach (string url in new[] { "http://github.com/0x78654C/CIARE/releases/download/test/tool.exe", "https://evil.test/0x78654C/CIARE/releases/download/test/tool.exe", "https://github.com/other/CIARE/releases/download/test/tool.exe", "https://github.com/0x78654C/CIARE/releases/download/test/tool.exe?x=1" })
            Reject(() => ReleaseCatalog.GetDownloadUri(new() { Name = "tool.exe", DownloadUrl = url }), "Reject untrusted asset URL");
    }

    private static async Task Network()
    {
        int requests = 0;
        using (var client = new HttpClient(new Handler(request =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(
                requests == 1 ? Enumerable.Range(0, 100).Select(_ => Release()).ToArray() : new[] { Release("CIARE_v3.4.0-x64.zip") })) };
        })))
        {
            var pages = await ReleaseCatalog.ReadAsync(client, CancellationToken.None);
            Assert(requests == 2 && pages.Count == 101, "Release pagination");
            Assert(ReleaseCatalog.SelectUpdate(pages, new Version(3, 0, 0), "x64") != null, "Packages on later pages are considered");
        }
        using (var limited = new HttpClient(new Handler(_ => new(HttpStatusCode.Forbidden))))
        {
            bool caught = false;
            try { await ReleaseCatalog.ReadAsync(limited, CancellationToken.None); } catch (HttpRequestException ex) { caught = ex.Message.Contains("limiting"); }
            Assert(caught, "Useful rate limit error");
        }
        byte[] bytes = Encoding.UTF8.GetBytes("verified download");
        using var download = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        string path = Path.Combine(_root, "download.exe");
        await AssetDownloader.DownloadAsync(download, Asset("tool.exe", bytes), path, null, CancellationToken.None);
        Assert(File.ReadAllBytes(path).SequenceEqual(bytes), "Verified download saved");
        var corrupt = Asset("bad.exe", bytes);
        corrupt.Digest = "sha256:" + new string('0', 64);
        string bad = Path.Combine(_root, "bad.exe");
        bool rejected = false;
        try { await AssetDownloader.DownloadAsync(download, corrupt, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected && !File.Exists(bad) && !File.Exists(bad + ".partial"), "Corrupt download never becomes executable");
        corrupt.Digest = null;
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, corrupt, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Missing checksum rejected");
        var wrongSize = Asset("tool.exe", bytes);
        wrongSize.Size++;
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, wrongSize, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Size mismatch rejected");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, Asset("tool.exe", bytes), bad, null, cancellation.Token); } catch (OperationCanceledException) { rejected = true; }
        Assert(rejected && !File.Exists(bad), "Cancelled download leaves installed files untouched");
    }

    private static async Task LocalCopy()
    {
        string source = Installation("local-copy");
        File.WriteAllText(Path.Combine(source, "CIARE.dll"), "app assembly");
        File.WriteAllText(Path.Combine(source, "CIARE.Updater.dll"), "bundled installer");
        File.WriteAllText(Path.Combine(source, "CIARE.runtimeconfig.json"), "runtime configuration");
        File.WriteAllText(Path.Combine(source, "personal.cs"), "user work");
        Directory.CreateDirectory(Path.Combine(source, "runtimes", "win-x64", "native"));
        File.WriteAllText(Path.Combine(source, "runtimes", "win-x64", "native", "native.dll"), "native dependency");
        Directory.CreateDirectory(Path.Combine(source, "fr"));
        File.WriteAllText(Path.Combine(source, "fr", "app.resources.dll"), "localized dependency");
        string executable = await LocalUpdater.PrepareAsync(source, CancellationToken.None);
        string copy = Path.GetDirectoryName(executable);
        Assert(!copy.StartsWith(source, StringComparison.OrdinalIgnoreCase), "Updater copy runs outside the installation");
        Assert(File.Exists(executable) && File.ReadAllText(Path.Combine(copy, "CIARE.Updater.dll")) == "bundled installer", "Updater code comes from the installed app");
        Assert(File.Exists(Path.Combine(copy, "CIARE.runtimeconfig.json")), "Runtime configuration accompanies the private copy");
        Assert(File.Exists(Path.Combine(copy, "runtimes", "win-x64", "native", "native.dll")) && File.Exists(Path.Combine(copy, "fr", "app.resources.dll")), "Native and localized runtime dependencies are copied");
        Assert(!File.Exists(Path.Combine(copy, "personal.cs")) && File.ReadAllText(Path.Combine(source, "personal.cs")) == "user work", "Local updater preparation preserves user documents");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool cancelled = false;
        try { await LocalUpdater.PrepareAsync(source, cancellation.Token); } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled, "Local updater preparation can be cancelled");
    }

    private static string Installation(string name)
    {
        string path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "CIARE.exe"), "old app");
        return path;
    }

    private static string Zip(params (string Name, string Content)[] entries)
    {
        string path = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            using var writer = new StreamWriter(zip.CreateEntry(entry.Name).Open());
            writer.Write(entry.Content);
        }
        return path;
    }

    private static void Packages()
    {
        string install = Installation("success");
        File.WriteAllText(Path.Combine(install, "my-code.cs"), "personal work");
        using (var installer = new PackageInstaller(install))
        {
            string payload = installer.Extract(Zip(("CIARE.exe", "new app"), ("lib/dependency.dll", "new library")), CancellationToken.None);
            installer.Install(payload, null);
            Assert(File.ReadAllText(Path.Combine(install, "CIARE.exe")) == "new app", "Application replaced");
            Assert(File.ReadAllText(Path.Combine(install, "lib/dependency.dll")) == "new library", "Dependencies installed");
            Assert(File.ReadAllText(Path.Combine(install, "my-code.cs")) == "personal work", "Personal files preserved");
        }
        Assert(Directory.GetDirectories(install, ".ciare-update-*").Length == 0, "Successful staging and backup cleaned");
        using (var installer = new PackageInstaller(Installation("wrapped")))
            Assert(Path.GetFileName(installer.Extract(Zip(("CIARE_v3.4.0/CIARE.exe", "app")), CancellationToken.None)) == "CIARE_v3.4.0", "Single wrapper folder supported");
        foreach (string entry in new[] { "../escape.exe", "nested/../../escape.exe", "C:/escape.exe", "/escape.exe", "lib/file:stream", "CON.txt", "lib./file", "nested\\..\\escape.exe" })
        {
            using var installer = new PackageInstaller(Installation("unsafe-" + Guid.NewGuid()));
            Reject(() => installer.Extract(Zip(("CIARE.exe", "app"), (entry, "bad")), CancellationToken.None), "Unsafe ZIP path rejected: " + entry);
        }
        using (var installer = new PackageInstaller(Installation("duplicate")))
            Reject(() => installer.Extract(Zip(("CIARE.exe", "app"), ("ciare.EXE", "other")), CancellationToken.None), "Case-insensitive duplicate rejected");
        using (var installer = new PackageInstaller(Installation("missing")))
            Reject(() => installer.Extract(Zip(("readme.txt", "no executable")), CancellationToken.None), "Missing application rejected");
        string payloadCheck = Installation("payload-check");
        File.Copy(typeof(Program).Assembly.Location, Path.Combine(payloadCheck, "CIARE.dll"));
        File.WriteAllText(Path.Combine(payloadCheck, "CIARE.runtimeconfig.json"), "{}");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"), Path.Combine(payloadCheck, "CIARE.exe"), true);
        string architecture = Environment.Is64BitProcess ? "x64" : "x86";
        var options = new UpdateOptions(payloadCheck, 1, 1, "unused", new Version(3, 2, 2, 1), new Version(3, 2, 3, 1), architecture);
        PackageInstaller.ValidatePayload(payloadCheck, options);
        Assert(true, "Application payload version and architecture accepted");
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options with { Architecture = architecture == "x64" ? "x86" : "x64" }), "Wrong executable architecture rejected");
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options with { Version = new Version(3, 2, 4, 0) }), "Mislabeled application version rejected");
        File.Delete(Path.Combine(payloadCheck, "CIARE.runtimeconfig.json"));
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options), "Incomplete runtime payload rejected");
        string rollback = Installation("rollback");
        File.WriteAllText(Path.Combine(rollback, "z-locked.dll"), "original library");
        using (var installer = new PackageInstaller(rollback))
        {
            string payload = installer.Extract(Zip(("CIARE.exe", "replacement"), ("a-new.dll", "new"), ("z-locked.dll", "replacement")), CancellationToken.None);
            using (var locked = new FileStream(Path.Combine(rollback, "z-locked.dll"), FileMode.Open, FileAccess.Read, FileShare.None))
                Reject(() => installer.Install(payload, null), "Locked file fails installation");
            Assert(File.ReadAllText(Path.Combine(rollback, "CIARE.exe")) == "old app", "Original app restored after later-file failure");
            Assert(File.ReadAllText(Path.Combine(rollback, "z-locked.dll")) == "original library", "Locked original preserved");
            Assert(!File.Exists(Path.Combine(rollback, "a-new.dll")), "Newly added file removed during rollback");
        }
        string blocked = Installation("blocked");
        Directory.CreateDirectory(Path.Combine(blocked, "dependency.dll"));
        using (var installer = new PackageInstaller(blocked))
        {
            string payload = installer.Extract(Zip(("CIARE.exe", "new"), ("dependency.dll", "new")), CancellationToken.None);
            Reject(() => installer.Install(payload, null), "Folder/file conflict rejected before installation");
            Assert(File.ReadAllText(Path.Combine(blocked, "CIARE.exe")) == "old app", "Preflight leaves application untouched");
        }
    }

    private static void RenderWindow()
    {
        ApplicationConfiguration.Initialize();
        using var window = new UpdateWindow();
        window.ShowRelease(new Version(3, 2, 3, 1), new(new Version(3, 2, 4, 0), "x64", null,
            "What’s new in CIARE\r\n\r\n• Faster editor startup\r\n• Improvements to code completion\r\n• Stability fixes and a smoother editing experience"));
        window.Status.Text = "Ready when you are";
        window.StartPosition = FormStartPosition.Manual;
        window.Location = new Point(-10000, -10000);
        window.ShowInTaskbar = false;
        window.Show();
        Application.DoEvents();
        window.PerformLayout();
        using var bitmap = new Bitmap(window.Width, window.Height);
        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
        bitmap.Save(Path.Combine(_root, "update-prompt.png"));
        Assert(window.Primary.Bounds.Width > 100 && window.Notes.Bounds.Height > 60, "Update window has usable button and release note bounds");
        window.Heading.Text = "Updating CIARE";
        window.Primary.Visible = false;
        window.Secondary.Text = "Cancel";
        window.ShowProgress("2 / 4   Downloading CIARE", 64, "12.4 MB of 19.4 MB");
        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
        bitmap.Save(Path.Combine(_root, "update-progress.png"));
        window.ClientSize = new Size(614, 531);
        window.PerformLayout();
        Assert(window.Notes.Height > 60 && window.Secondary.Bottom <= window.Secondary.Parent.ClientSize.Height,
            "Notes and actions fit the minimum window size");
    }

    private static void Handoff(bool cancel, string packageVersion = "3.2.3")
    {
        string install = Installation((cancel ? "handoff-cancel-" : "handoff-install-") + packageVersion);
        foreach (string extension in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater" + extension), Path.Combine(install, "Tests.Updater" + extension));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"), Path.Combine(install, "CIARE.exe"), true);
        File.WriteAllText(Path.Combine(install, ".updater-fixture"), "Disposable test installation");
        string zipPath = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(Path.Combine(install, "CIARE.exe"), "CIARE.exe");
            zip.CreateEntryFromFile(typeof(Program).Assembly.Location, "CIARE.dll");
            zip.CreateEntryFromFile(Path.Combine(install, "Tests.Updater.runtimeconfig.json"), "CIARE.runtimeconfig.json");
            using var writer = new StreamWriter(zip.CreateEntry("updated.txt").Open());
            writer.Write("installed");
        }
        byte[] bytes = File.ReadAllBytes(zipPath);
        string architecture = Environment.Is64BitProcess ? "x64" : "x86";
        var release = new GitHubRelease { Assets = new() { Asset($"CIARE_v{packageVersion}-{architecture}.zip", bytes) } };
        string session = "Local\\CIARE.Update." + Guid.NewGuid().ToString("N");
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
        using var cancelled = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".cancel");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".exit");
        var start = new ProcessStartInfo(Path.Combine(install, "CIARE.exe"))
        { WorkingDirectory = install, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true };
        start.ArgumentList.Add("--wait-parent");
        start.ArgumentList.Add(session + ".exit");
        using var parent = Process.Start(start);
        var options = new UpdateOptions(install, parent.Id, parent.StartTime.ToUniversalTime().Ticks,
            session, new Version(3, 2, 2, 1), ReleaseCatalog.Normalize(Version.Parse(packageVersion)), architecture);
        using var window = new UpdaterForm(options, () => new HttpClient(new Handler(request =>
            new(HttpStatusCode.OK) { Content = request.RequestUri.Host == "api.github.com"
                ? new StringContent(JsonSerializer.Serialize(new[] { release })) : new ByteArrayContent(bytes) })))
        { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false };
        var clock = Stopwatch.StartNew();
        bool released = false;
        Exception failure = null;
        using var monitor = new System.Windows.Forms.Timer { Interval = 50 };
        monitor.Tick += (_, _) =>
        {
            try
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(25)) throw new Exception("Updater handoff timed out: " + window.Notes.Text);
                if (!released && window.Status.Text == "Waiting for CIARE to close")
                {
                    Assert(ready.WaitOne(0), "Updater reports readiness to the parent");
                    Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "updated.txt")), "No installed files change while the parent is running");
                    released = true;
                    if (cancel) cancelled.Set(); else exit.Set();
                }
                if (window.Heading.Text == "Couldn’t finish the update") throw new Exception(window.Notes.Text);
                if (cancel && window.Heading.Text == "Update cancelled") window.Close();
            }
            catch (Exception ex) { failure = ex; cancelled.Set(); exit.Set(); window.Close(); }
        };
        try
        {
            monitor.Start();
            Application.Run(window);
            if (failure != null) throw failure;
            Assert(released, "The updater reached the parent handoff");
            if (cancel)
                Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "updated.txt")), "Cancelling the close leaves the parent and installation intact");
            else
            {
                Assert(parent.HasExited && File.ReadAllText(Path.Combine(install, "updated.txt")) == "installed", "Update installed only after the parent exited");
                Assert(SpinWait.SpinUntil(() => File.Exists(Path.Combine(install, ".restarted")), TimeSpan.FromSeconds(5)), "CIARE restarts after a successful update");
            }
        }
        finally
        {
            monitor.Stop();
            exit.Set();
            Assert(parent.WaitForExit(5000), "Disposable parent exits cleanly");
        }
    }

    private static void BundledStartup(string applicationDirectory)
    {
        string executable = LocalUpdater.PrepareAsync(applicationDirectory, CancellationToken.None).GetAwaiter().GetResult();
        string install = Installation("bundled-parent");
        foreach (string extension in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater" + extension), Path.Combine(install, "Tests.Updater" + extension));
        File.Copy(Path.Combine(install, "Tests.Updater.exe"), Path.Combine(install, "CIARE.exe"), true);
        string session = "Local\\CIARE.Update." + Guid.NewGuid().ToString("N");
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
        using var cancelled = new EventWaitHandle(true, EventResetMode.ManualReset, session + ".cancel");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".exit");
        var parentStart = new ProcessStartInfo(Path.Combine(install, "CIARE.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        parentStart.ArgumentList.Add("--wait-parent");
        parentStart.ArgumentList.Add(session + ".exit");
        using var parent = Process.Start(parentStart);
        var start = new ProcessStartInfo(executable)
        { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable), WindowStyle = ProcessWindowStyle.Hidden };
        foreach (string argument in new[] { "--apply-update", "--install-dir", install, "--parent-pid", parent.Id.ToString(),
            "--parent-start", parent.StartTime.ToUniversalTime().Ticks.ToString(), "--session", session,
            "--current-version", "3.2.2.1", "--version", "3.2.3", "--arch", Environment.Is64BitProcess ? "x64" : "x86" })
            start.ArgumentList.Add(argument);
        using var updater = Process.Start(start);
        try
        {
            Assert(ready.WaitOne(TimeSpan.FromSeconds(20)), "The copied CIARE executable starts its bundled installer before editor startup");
            var timer = Stopwatch.StartNew();
            while (!updater.HasExited && timer.Elapsed < TimeSpan.FromSeconds(5))
            {
                // The smoke test starts hidden, so Process.MainWindowHandle can be zero.
                EnumWindows((handle, _) =>
                {
                    GetWindowThreadProcessId(handle, out uint processId);
                    if (processId == updater.Id) PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                Thread.Sleep(100);
            }
            Assert(updater.HasExited && updater.ExitCode == 0,
                "The real bundled installer closes cleanly after cancellation: " + (updater.HasExited ? updater.ExitCode.ToString() : "still running"));
            Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "CIARE.dll")), "Bundled installer cancellation preserves the running installation");
        }
        finally
        {
            exit.Set();
            parent.WaitForExit(5000);
            if (!updater.HasExited) { updater.Kill(); updater.WaitForExit(5000); }
        }
    }

    private delegate bool WindowCallback(IntPtr handle, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(handle(request));
        }
    }
}
