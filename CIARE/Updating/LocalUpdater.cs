using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CIARE.Updating;

internal static class LocalUpdater
{
    // Run CIARE's built-in update mode from a private copy so installation files can be replaced.
    public static Task<string> PrepareAsync(string installation, CancellationToken cancellationToken) => Task.Run(() =>
    {
        string source = Path.GetFullPath(installation);
        if (!File.Exists(Path.Combine(source, "CIARE.exe")))
            throw new FileNotFoundException("CIARE.exe is missing from the installation folder.");
        string destination = Path.Combine(Path.GetTempPath(), "CIARE-Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destination);
        try
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".exe", ".dll", ".json", ".config" };
            foreach (string file in Directory.EnumerateFiles(source).Where(file => extensions.Contains(Path.GetExtension(file))))
                CopyFile(file, Path.Combine(destination, Path.GetFileName(file)), cancellationToken);
            // Native runtime assets and localized framework/application resources can live in subfolders.
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Select(culture => culture.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string directory in Directory.EnumerateDirectories(source))
            {
                string name = Path.GetFileName(directory);
                if (name.Equals("runtimes", StringComparison.OrdinalIgnoreCase) || cultures.Contains(name))
                    CopyDirectory(directory, Path.Combine(destination, name), cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            return Path.Combine(destination, "CIARE.exe");
        }
        catch
        {
            UpdateCleanup.RemoveCopy(destination);
            throw;
        }
    }, cancellationToken);

    private static void CopyDirectory(string source, string destination, CancellationToken cancellationToken)
    {
        RejectLink(source);
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
            CopyFile(file, Path.Combine(destination, Path.GetFileName(file)), cancellationToken);
        foreach (string directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)), cancellationToken);
    }

    private static void CopyFile(string source, string destination, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RejectLink(source);
        File.Copy(source, destination);
    }

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("An updater runtime file is a symbolic link or junction: " + path);
    }
}
