using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.Updating;

namespace CIARE;

public partial class MainForm
{
    private bool _checkingForUpdate;
    private readonly CancellationTokenSource _updateLifetime = new();

    // Called by the production entry point so designer and isolated editor tests never contact GitHub.
    internal void InitializeUpdates()
    {
        InitializeUpdateMenu();
        Shown += async (_, _) =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(6), _updateLifetime.Token);
                await CheckForUpdatesAsync(false);
            }
            catch (OperationCanceledException) { }
        };
        FormClosed += (_, _) => _updateLifetime.Cancel();
    }

    internal void InitializeUpdateMenu()
    {
        if (helpToolStripMenuItem.DropDownItems.ContainsKey("checkForUpdatesToolStripMenuItem")) return;
        var check = new ToolStripMenuItem("Check for updates…") { Name = "checkForUpdatesToolStripMenuItem" };
        check.Click += async (_, _) => await CheckForUpdatesAsync(true);
        helpToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator { Name = "updateToolStripSeparator" });
        helpToolStripMenuItem.DropDownItems.Add(check);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (_checkingForUpdate || IsDisposed || Disposing) return;
        _checkingForUpdate = true;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            using var client = ReleaseCatalog.CreateClient();
            var installed = ReleaseCatalog.Normalize(Assembly.GetExecutingAssembly().GetName().Version);
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                _ => throw new NotSupportedException("Automatic updates support x64 and x86 installations.")
            };
            var releases = await ReleaseCatalog.ReadAsync(client, timeout.Token);
            var update = ReleaseCatalog.SelectUpdate(releases, installed, architecture);
            if (IsDisposed || Disposing) return;
            if (update == null)
            {
                if (manual) MessageBox.Show(this, $"CIARE {installed} ({architecture}) is up to date.", "CIARE Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var window = new UpdateWindow { StartPosition = FormStartPosition.CenterParent };
            window.ShowRelease(installed, update);
            window.Summary.Text = $"Install {update.Package.Name}. You can save your open work before CIARE closes.";
            window.Status.Text = "Ready when you are";
            using var downloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
            var preparationToken = downloadCancellation.Token;
            string updaterPath = null;
            window.Secondary.Click += (_, _) => window.Close();
            window.FormClosing += (_, _) => downloadCancellation.Cancel();
            window.Primary.Click += async (_, _) =>
            {
                window.Primary.Enabled = false;
                window.Secondary.Text = "Cancel";
                string preparedPath = null;
                try
                {
                    window.Heading.Text = "Preparing your update";
                    window.Status.Text = "Preparing to install the release ZIP…";
                    preparedPath = await LocalUpdater.PrepareAsync(AppContext.BaseDirectory, preparationToken);
                    preparationToken.ThrowIfCancellationRequested();
                    updaterPath = preparedPath;
                    preparedPath = null;
                    window.DialogResult = DialogResult.OK;
                    window.Close();
                }
                catch (OperationCanceledException) when (preparationToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    if (window.IsDisposed) return;
                    window.Heading.Text = "Couldn’t prepare the update";
                    window.Summary.Text = "Review the details below, then try again.";
                    window.Notes.Text = ex.Message;
                    window.Status.Text = "CIARE is still open. You can try again later.";
                    window.Primary.Text = "Try again";
                    window.Primary.Enabled = true;
                    window.Secondary.Text = "Close";
                }
                finally
                {
                    if (preparedPath != null) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(preparedPath));
                }
            };
            try
            {
                if (window.ShowDialog(this) == DialogResult.OK && updaterPath != null)
                {
                    string executable = updaterPath;
                    updaterPath = null; // LaunchUpdaterAsync now owns cleanup, including a failed launch.
                    await LaunchUpdaterAsync(executable, installed, update);
                }
            }
            finally
            {
                if (updaterPath != null) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(updaterPath));
            }
        }
        catch (OperationCanceledException) when (_updateLifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Trace.TraceWarning("CIARE update check: {0}", ex);
            if (manual && !IsDisposed && !Disposing)
                MessageBox.Show(this, "Couldn’t check for updates. Please try again later.\n\n" + ex.Message,
                    "CIARE Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally { _checkingForUpdate = false; }
    }

    private async Task LaunchUpdaterAsync(string executable, Version installed, UpdateRelease release)
    {
        Process updater = null;
        try
        {
            string session = "Local\\CIARE.Update." + Guid.NewGuid().ToString("N");
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
            using var cancelled = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".cancel");
            using var parent = Process.GetCurrentProcess();
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable) };
            start.ArgumentList.Add("--apply-update");
            foreach (string argument in new[] { "--install-dir", AppContext.BaseDirectory, "--parent-pid", parent.Id.ToString(),
                "--parent-start", parent.StartTime.ToUniversalTime().Ticks.ToString(), "--session", session,
                "--current-version", installed.ToString(), "--version", release.Version.ToString(), "--arch", release.Architecture })
                start.ArgumentList.Add(argument);
            updater = Process.Start(start) ?? throw new IOException("The updater could not be started.");
            _ = UpdateCleanup.ObserveExitAsync(Path.GetDirectoryName(executable), updater.Id);
            bool handoff = false;
            try
            {
                var started = Stopwatch.StartNew();
                while (!ready.WaitOne(0))
                {
                    if (updater.HasExited || started.Elapsed > TimeSpan.FromSeconds(45))
                        throw new IOException("The updater did not become ready. CIARE has been kept open.");
                    await Task.Delay(100, _updateLifetime.Token);
                }
                if (IsDisposed || Disposing) return;
                Close(); // Existing unsaved-work prompts can cancel this close.
                handoff = IsDisposed || Disposing;
            }
            finally
            {
                if (!handoff) cancelled.Set();
            }
        }
        finally
        {
            // The updater starts its C# cleanup worker when it closes.
            // Also cover failures before its entry point could run (including Process.Start).
            if (updater == null || updater.HasExited) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(executable));
            updater?.Dispose();
        }
    }
}
