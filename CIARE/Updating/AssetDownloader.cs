using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CIARE.Updating;

internal sealed record DownloadProgress(long Received, long Total)
{
    public int Percent => Total > 0 ? (int)Math.Clamp(Received * 100 / Total, 0, 100) : 0;
    public string Description => $"{Received / 1048576d:0.0} MB of {Total / 1048576d:0.0} MB";
}

internal static class AssetDownloader
{
    public static async Task DownloadAsync(HttpClient client, ReleaseAsset asset, string destination,
        IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        var uri = ReleaseCatalog.GetDownloadUri(asset);
        if (!Regex.IsMatch(asset.Digest ?? "", @"\Asha256:[a-fA-F0-9]{64}\z") || asset.Size <= 0)
            throw new InvalidDataException($"GitHub has not provided a SHA-256 checksum and size for {asset.Name}. Try again after the asset finishes uploading.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(20));
        string partial = destination + ".partial";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.ParseAdd("application/octet-stream");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != asset.Size)
                throw new InvalidDataException("The download size does not match the GitHub release.");
            await using (var source = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false))
            await using (var target = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                byte[] buffer = new byte[81920];
                long received = 0;
                long lastReport = Environment.TickCount64;
                int count;
                while ((count = await source.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    received += count;
                    if (received > asset.Size)
                        throw new InvalidDataException("The download exceeds its expected size.");
                    hash.AppendData(buffer, 0, count);
                    await target.WriteAsync(buffer.AsMemory(0, count), timeout.Token).ConfigureAwait(false);
                    if (Environment.TickCount64 - lastReport >= 100 || received == asset.Size)
                    {
                        progress?.Report(new(received, asset.Size));
                        lastReport = Environment.TickCount64;
                    }
                }
                if (received != asset.Size || !Convert.ToHexString(hash.GetHashAndReset())
                    .Equals(asset.Digest.Substring(7), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The download failed its SHA-256 integrity check. Please try again.");
            }
            File.Move(partial, destination);
        }
        finally
        {
            try { File.Delete(partial); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
