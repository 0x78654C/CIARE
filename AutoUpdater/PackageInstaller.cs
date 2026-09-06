using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CIARE.Updating;

namespace CIARE.AutoUpdater;

internal sealed class PackageInstaller : IDisposable
{
    private readonly string _installation;
    private readonly string _workspace;
    private readonly string _stage;
    private readonly string _backup;
    private bool _keepBackup;
    public string BackupDirectory => _backup;

    public PackageInstaller(string installation)
    {
        ValidateInstallation(installation);
        _installation = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installation));
        _workspace = Path.Combine(_installation, ".ciare-update-" + Guid.NewGuid().ToString("N"));
        _stage = Path.Combine(_workspace, "stage");
        _backup = Path.Combine(_workspace, "backup");
        Directory.CreateDirectory(_stage);
        Directory.CreateDirectory(_backup);
    }

    public static void ValidateInstallation(string installation)
    {
        string path = Path.GetFullPath(installation);
        if (Path.TrimEndingDirectorySeparator(path).Equals(Path.TrimEndingDirectorySeparator(Path.GetPathRoot(path)), StringComparison.OrdinalIgnoreCase)
            || !File.Exists(Path.Combine(path, "CIARE.exe")))
            throw new InvalidDataException("Choose an existing CIARE installation folder containing CIARE.exe.");
        RejectReparsePoints(path);
        RejectReparsePoints(Path.Combine(path, "CIARE.exe"));
    }

    public static void RejectReparsePoints(string path)
    {
        for (string current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Updates cannot replace files through a symbolic link or junction: " + current);
        }
    }

    internal static string ResolveEntry(string root, string entry)
    {
        var parts = entry.Replace('\\', '/').TrimEnd('/').Split('/');
        if (parts.Length == 0 || parts.Any(p => string.IsNullOrWhiteSpace(p) || p == "." || p == ".."
            || p.EndsWith('.') || p.EndsWith(' ') || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || p.StartsWith(".ciare-update-", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(p.Split('.')[0], @"\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase)))
            throw new InvalidDataException("The update ZIP contains an unsafe path: " + entry);
        string path = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        if (!path.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update ZIP contains a path outside the installation.");
        return path;
    }

    public string Extract(string zipPath, CancellationToken cancellationToken)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        if (zip.Entries.Count == 0 || zip.Entries.Count > 30000)
            throw new InvalidDataException("The update ZIP has an invalid number of entries.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total = checked(total + entry.Length);
            if (total > 4L * 1024 * 1024 * 1024)
                throw new InvalidDataException("The unpacked update is larger than 4 GB.");
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("The update ZIP contains a symbolic link.");
            string path = ResolveEntry(_stage, entry.FullName);
            if (!seen.Add(path)) throw new InvalidDataException("The update ZIP contains duplicate paths.");
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                Directory.CreateDirectory(path);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                entry.ExtractToFile(path);
            }
        }
        if (File.Exists(Path.Combine(_stage, "CIARE.exe"))) return _stage;
        var roots = Directory.GetDirectories(_stage);
        if (roots.Length == 1 && Directory.GetFiles(_stage).Length == 0 && File.Exists(Path.Combine(roots[0], "CIARE.exe")))
            return roots[0];
        throw new InvalidDataException("The update ZIP must contain CIARE.exe at its root or in a single enclosing folder.");
    }

    public static void ValidatePayload(string root, UpdateOptions options)
    {
        string assembly = Path.Combine(root, "CIARE.dll");
        if (!File.Exists(assembly) || !File.Exists(Path.Combine(root, "CIARE.runtimeconfig.json")))
            throw new InvalidDataException("The update is missing CIARE.dll or CIARE.runtimeconfig.json.");
        var version = ReleaseCatalog.Normalize(AssemblyName.GetAssemblyName(assembly).Version);
        if (version.Major != options.Version.Major || version.Minor != options.Version.Minor || version.Build != options.Version.Build
            || version < options.Version || (options.Version.Revision > 0 && version.Revision != options.Version.Revision))
            throw new InvalidDataException("The version inside the update does not match its ZIP filename.");
        // The apphost machine identifies the package's process architecture, even for AnyCPU CIARE.dll.
        using var executable = new BinaryReader(File.OpenRead(Path.Combine(root, "CIARE.exe")));
        if (executable.ReadUInt16() != 0x5A4D) throw new InvalidDataException("CIARE.exe is not a Windows executable.");
        executable.BaseStream.Position = 0x3c;
        int offset = executable.ReadInt32();
        if (offset < 64 || offset > executable.BaseStream.Length - 6) throw new InvalidDataException("CIARE.exe has an invalid executable header.");
        executable.BaseStream.Position = offset;
        if (executable.ReadUInt32() != 0x4550 || executable.ReadUInt16() != (options.Architecture == "x64" ? 0x8664 : 0x014c))
            throw new InvalidDataException("The update architecture does not match this CIARE installation.");
    }

    public void Install(string payload, IProgress<(int Percent, string File)> progress)
    {
        ValidateInstallation(_installation);
        var files = Directory.GetFiles(payload, "*", SearchOption.AllDirectories)
            .Select(source => new { Source = source, Relative = Path.GetRelativePath(payload, source) })
            .OrderBy(file => file.Relative, StringComparer.OrdinalIgnoreCase).ToList();
        // Preflight the entire package before modifying the installed files.
        foreach (var file in files)
        {
            string target = ResolveEntry(_installation, file.Relative);
            RejectReparsePoints(target);
            if (Directory.Exists(target)) throw new IOException("A folder occupies an update file path: " + target);
        }
        File.WriteAllText(Path.Combine(_workspace, "files.json"), JsonSerializer.Serialize(files.Select(f => f.Relative)));
        var applied = new List<(string Target, string Backup, bool Existed)>();
        try
        {
            foreach (var file in files)
            {
                string target = ResolveEntry(_installation, file.Relative);
                string backup = ResolveEntry(_backup, file.Relative);
                RejectReparsePoints(target);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                bool existed = File.Exists(target);
                if (existed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Move(target, backup);
                }
                applied.Add((target, backup, existed));
                File.Copy(file.Source, target);
                progress?.Report((applied.Count * 100 / files.Count, file.Relative));
            }
        }
        catch (Exception installError)
        {
            var failures = new List<Exception>();
            foreach (var file in applied.AsEnumerable().Reverse())
            {
                try
                {
                    RejectReparsePoints(file.Target);
                    if (File.Exists(file.Target)) File.Delete(file.Target);
                    if (file.Existed) File.Move(file.Backup, file.Target);
                }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (failures.Count > 0)
            {
                _keepBackup = true;
                throw new UpdateRecoveryException($"Installation failed and some files could not be restored. Keep this backup for recovery: {_backup}",
                    new AggregateException(new[] { installError }.Concat(failures)));
            }
            throw new IOException("The update could not be installed. The previous CIARE files were restored. " + installError.Message, installError);
        }
    }

    public void Dispose()
    {
        if (_keepBackup) return;
        // Delete only the unique workspace this instance created, never the installation itself.
        try
        {
            if (Path.GetDirectoryName(_workspace).Equals(_installation, StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(_workspace).StartsWith(".ciare-update-", StringComparison.Ordinal)
                && Directory.Exists(_workspace))
            {
                RejectReparsePoints(_workspace);
                Directory.Delete(_workspace, true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

internal sealed class UpdateRecoveryException(string message, Exception inner) : IOException(message, inner);
