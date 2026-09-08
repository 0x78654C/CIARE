using System.Globalization;
using CIARE.Updating;

try
{
    if (args.Length != 3) return 1;
    UpdateCleanup.RunWorker(args[0], int.Parse(args[1], CultureInfo.InvariantCulture),
        long.Parse(args[2], CultureInfo.InvariantCulture));
    return 0;
}
catch { return 1; }
