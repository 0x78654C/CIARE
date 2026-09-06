using System.Globalization;
using System.Text.RegularExpressions;
using CIARE.Updating;

namespace CIARE.AutoUpdater;

internal sealed record UpdateOptions(string InstallDirectory, int ParentId, long ParentStart,
    string Session, Version CurrentVersion, Version Version, string Architecture)
{
    public static UpdateOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (args.Length != 14) throw new ArgumentException("Start the updater from CIARE’s Help → Check for updates menu.");
        for (int i = 0; i < args.Length; i += 2)
            if (!values.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Duplicate updater argument.");
        var options = new UpdateOptions(Path.TrimEndingDirectorySeparator(Path.GetFullPath(values["--install-dir"])),
            int.Parse(values["--parent-pid"], CultureInfo.InvariantCulture), long.Parse(values["--parent-start"], CultureInfo.InvariantCulture),
            values["--session"], ReleaseCatalog.Normalize(Version.Parse(values["--current-version"])),
            ReleaseCatalog.Normalize(Version.Parse(values["--version"])), values["--arch"]);
        if (options.ParentId <= 0 || options.ParentStart <= 0 || options.Version <= options.CurrentVersion
            || (options.Architecture != "x64" && options.Architecture != "x86")
            || !Regex.IsMatch(options.Session, @"\ALocal\\CIARE\.Update\.[a-f0-9]{32}\z"))
            throw new ArgumentException("Invalid updater request.");
        PackageInstaller.ValidateInstallation(options.InstallDirectory);
        return options;
    }
}
