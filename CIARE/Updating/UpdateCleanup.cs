using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CIARE.Updating;

internal static class UpdateCleanup
{
    public static bool IsPrivateDirectory(string directory)
    {
        string path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        string root = Path.Combine(Path.GetTempPath(), "CIARE-Updates");
        return string.Equals(Path.GetDirectoryName(path), Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(Path.GetFileName(path), "N", out _);
    }

    private static void ValidateDirectory(string directory)
    {
        if (!IsPrivateDirectory(directory)) throw new IOException("Refusing to clean a folder outside CIARE's temporary updater directory.");
        foreach (string path in new[] { Path.GetDirectoryName(directory), directory })
            if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to clean an updater folder through a symbolic link or junction.");
    }

    // Only call once the copy is no longer running (or was never launched).
    public static void RemoveCopy(string directory)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        try { DeleteTree(directory); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void DeleteTree(string directory)
    {
        if (!Directory.Exists(directory)) return;
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to follow a link in the temporary updater copy.");
            if ((attributes & FileAttributes.Directory) != 0) DeleteTree(entry);
            else
            {
                File.SetAttributes(entry, attributes & ~FileAttributes.ReadOnly);
                File.Delete(entry);
            }
        }
        Directory.Delete(directory);
    }

    internal static void ScheduleAfterExit(string directory, Process owner, string installation)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        string executable = Path.GetFullPath(Path.Combine(installation, "CIARE.UpdateCleanup.exe"));
        if (executable.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The cleanup worker must run outside the temporary updater copy.");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = installation
        };
        foreach (string argument in new[] { directory, owner.Id.ToString(CultureInfo.InvariantCulture),
            owner.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture) })
            start.ArgumentList.Add(argument);
        using var cleanup = Process.Start(start) ?? throw new IOException("Could not start temporary updater cleanup.");
    }

    // Called in the installed C# cleanup worker after the updater has finished replacing files.
    public static void RunWorker(string directory, int ownerId, long ownerStart)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        if (ownerId <= 0 || ownerStart <= 0) throw new IOException("Invalid cleanup process identity.");
        using var owner = FindProcess(ownerId);
        if (owner != null)
        {
            try
            {
                if (!owner.HasExited && owner.StartTime.ToUniversalTime().Ticks == ownerStart) owner.WaitForExit();
            }
            catch (InvalidOperationException) when (owner.HasExited) { }
        }
        for (int attempt = 0; attempt < 120; attempt++)
        {
            RemoveCopy(directory);
            if (!Directory.Exists(directory)) return;
            Thread.Sleep(500);
        }
        throw new IOException("Temporary updater files are still locked.");
    }

    private static Process FindProcess(int id)
    {
        try { return Process.GetProcessById(id); }
        catch (ArgumentException) { return null; }
    }

    // The editor can clean an aborted/crashed updater while it remains open.
    public static async Task ObserveExitAsync(string directory, int ownerId)
    {
        try
        {
            using var owner = FindProcess(ownerId);
            if (owner != null) await owner.WaitForExitAsync().ConfigureAwait(false);
            for (int attempt = 0; attempt < 120; attempt++)
            {
                RemoveCopy(directory);
                if (!Directory.Exists(directory)) return;
                await Task.Delay(500).ConfigureAwait(false);
            }
        }
        catch (Exception ex) { Trace.TraceWarning("CIARE updater cleanup: {0}", ex); }
    }
}
