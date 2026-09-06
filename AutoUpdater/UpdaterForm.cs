using System.Diagnostics;
using CIARE.Updating;

namespace CIARE.AutoUpdater;

internal sealed class UpdaterForm : UpdateWindow
{
    private readonly UpdateOptions _options;
    private readonly Func<System.Net.Http.HttpClient> _createClient;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly EventWaitHandle _ready;
    private readonly EventWaitHandle _cancelled;
    private readonly Process _parent;
    private readonly System.Windows.Forms.Timer _cancelTimer = new() { Interval = 100 };
    private bool _installing;
    private bool _finished;
    private bool _parentClosed;
    private bool _canRestart = true;

    public UpdaterForm(UpdateOptions options, Func<System.Net.Http.HttpClient> createClient = null)
    {
        _options = options;
        _createClient = createClient ?? ReleaseCatalog.CreateClient;
        _ready = EventWaitHandle.OpenExisting(options.Session + ".ready");
        _cancelled = EventWaitHandle.OpenExisting(options.Session + ".cancel");
        _parent = Process.GetProcessById(options.ParentId);
        if (_parent.StartTime.ToUniversalTime().Ticks != options.ParentStart
            || !Path.GetFullPath(_parent.MainModule.FileName).Equals(Path.Combine(options.InstallDirectory, "CIARE.exe"), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The CIARE process does not match this update request.");
        Heading.Text = "Updating CIARE";
        Summary.Text = "We’ll download the release, verify it, and install it. Your settings and saved tabs stay in place.";
        Versions.Text = $"{options.CurrentVersion}   →   {options.Version}     /     {options.Architecture.ToUpperInvariant()}";
        Primary.Visible = false;
        Secondary.Text = "Cancel";
        Secondary.Click += (_, _) => Close();
        _cancelTimer.Tick += (_, _) =>
        {
            if (!_installing && !_finished && _cancelled.WaitOne(0)) _cancellation.Cancel();
        };
        Shown += async (_, _) =>
        {
            _cancelTimer.Start();
            if (_cancelled.WaitOne(0)) _cancellation.Cancel();
            _ready.Set();
            await RunUpdateAsync();
        };
        FormClosing += (_, e) =>
        {
            if (_installing) { e.Cancel = true; return; }
            if (!_finished) { _cancellation.Cancel(); e.Cancel = true; }
        };
    }

    private async Task RunUpdateAsync()
    {
        bool installed = false;
        string zip = Path.Combine(Path.GetTempPath(), "CIARE-package-" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            using var client = _createClient();
            ShowProgress("1 / 4   Checking the release", 0);
            using var checkTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
            checkTimeout.CancelAfter(TimeSpan.FromSeconds(45));
            var releases = await ReleaseCatalog.ReadAsync(client, checkTimeout.Token);
            var release = ReleaseCatalog.SelectUpdate(releases, _options.CurrentVersion, _options.Architecture, _options.Version)
                ?? throw new InvalidOperationException("This release is no longer available. Reopen CIARE and check for updates again.");
            ShowRelease(_options.CurrentVersion, release);
            Summary.Text = $"Downloading {release.Package.Name}. The verified ZIP will replace the files in your current CIARE installation.";
            ShowProgress("2 / 4   Downloading CIARE", 0);
            var download = new Progress<DownloadProgress>(p => ShowProgress("2 / 4   Downloading CIARE", p.Percent, p.Description));
            await AssetDownloader.DownloadAsync(client, release.Package, zip, download, _cancellation.Token);

            ShowProgress("3 / 4   Verifying and unpacking", 0, release.Package.Name);
            using var installer = new PackageInstaller(_options.InstallDirectory);
            string payload = await Task.Run(() => installer.Extract(zip, _cancellation.Token), _cancellation.Token);
            await Task.Run(() => PackageInstaller.ValidatePayload(payload, _options), _cancellation.Token);
            ShowProgress("Waiting for CIARE to close", 0, "Finish saving your work in CIARE to continue.");
            using var waitTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
            waitTimeout.CancelAfter(TimeSpan.FromMinutes(10));
            await _parent.WaitForExitAsync(waitTimeout.Token);
            _parentClosed = true;
            if (_cancelled.WaitOne(0)) _cancellation.Cancel();
            _cancellation.Token.ThrowIfCancellationRequested();
            EnsureInstallationIsClosed();

            _installing = true;
            Secondary.Enabled = false;
            Summary.Text = "Installing the verified release. CIARE will be ready to reopen in a moment.";
            var install = new Progress<(int Percent, string File)>(p => ShowProgress("4 / 4   Installing CIARE", p.Percent, p.File));
            await Task.Run(() => installer.Install(payload, install));
            installed = true;
            Heading.Text = "You’re up to date";
            Summary.Text = $"CIARE {_options.Version} is installed and ready to use.";
            ShowProgress("Update complete", 100, "Your settings and saved tabs are preserved.");
        }
        catch (OperationCanceledException)
        {
            Heading.Text = "Update cancelled";
            Summary.Text = "CIARE’s installed files were not changed. You can try again from the Help menu.";
            ShowProgress("No update installed", 0);
        }
        catch (Exception ex)
        {
            if (ex is UpdateRecoveryException) _canRestart = false;
            Heading.Text = "Couldn’t finish the update";
            Summary.Text = "Review the details below, then try again from CIARE’s Help menu.";
            Notes.Text = ex.Message;
            ShowProgress("Update stopped", 0, "Close other CIARE windows and check that its folder is writable.");
        }
        finally
        {
            _installing = false;
            _finished = true;
            _cancelTimer.Stop();
            try { File.Delete(zip); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            Secondary.Enabled = true;
            Secondary.Text = "Close";
            if (_canRestart && (_parentClosed || _parent.HasExited))
            {
                Primary.Text = "Open CIARE";
                Primary.Visible = true;
                Primary.Click += (_, _) => Restart();
            }
        }
        if (installed)
        {
            await Task.Delay(1500);
            if (!IsDisposed) Restart();
        }
    }

    private void EnsureInstallationIsClosed()
    {
        foreach (var process in Process.GetProcessesByName("CIARE"))
        {
            using (process)
            {
                try
                {
                    if (process.HasExited) continue;
                    if (Path.GetFullPath(process.MainModule.FileName).Equals(Path.Combine(_options.InstallDirectory, "CIARE.exe"), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Another CIARE window is using this installation. Close it and try the update again.");
                }
                catch (InvalidOperationException) { } // The process exited during inspection.
            }
        }
    }

    private void Restart()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(_options.InstallDirectory, "CIARE.exe"))
            { WorkingDirectory = _options.InstallDirectory, UseShellExecute = true });
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Couldn’t start CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancelTimer.Dispose();
            _cancellation.Dispose();
            _ready.Dispose();
            _cancelled.Dispose();
            _parent.Dispose();
        }
        base.Dispose(disposing);
    }
}
