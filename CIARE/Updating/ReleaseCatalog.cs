using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CIARE.Updating;

internal sealed class ReleaseAsset
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; set; } = "";
    [JsonPropertyName("digest")] public string Digest { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class GitHubRelease
{
    [JsonPropertyName("draft")] public bool Draft { get; set; }
    [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("assets")] public List<ReleaseAsset> Assets { get; set; } = new();
}

internal sealed record UpdateRelease(Version Version, string Architecture, ReleaseAsset Package, string Notes);

internal static class ReleaseCatalog
{
    public const string ReleasesUrl = "https://github.com/0x78654C/CIARE/releases";
    private static readonly Regex PackageName = new(
        @"\ACIARE_v(?<version>\d+\.\d+\.\d+(?:\.\d+)?)-(?<arch>x64|x86)\.zip\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static Version Normalize(Version version) => new(version.Major, version.Minor,
        Math.Max(0, version.Build), Math.Max(0, version.Revision));

    public static bool TryParsePackage(string name, out Version version, out string architecture)
    {
        version = null;
        architecture = "";
        var match = PackageName.Match(name ?? "");
        if (!match.Success || !Version.TryParse(match.Groups["version"].Value, out var parsed))
            return false;
        version = Normalize(parsed);
        architecture = match.Groups["arch"].Value.ToLowerInvariant();
        return true;
    }

    public static UpdateRelease SelectUpdate(IEnumerable<GitHubRelease> releases, Version installed, string architecture, Version requested = null)
    {
        if (architecture != "x64" && architecture != "x86")
            throw new NotSupportedException("Automatic updates support x64 and x86 installations.");
        var stable = releases.Where(r => !r.Draft && !r.Prerelease).ToList();
        var candidates = from release in stable
                         from asset in release.Assets ?? new()
                         where IsUploaded(asset) && TryParsePackage(asset.Name, out _, out _)
                         let parsed = ParsePackage(asset.Name)
                         where parsed.Architecture == architecture && parsed.Version > Normalize(installed)
                         where requested == null || parsed.Version == Normalize(requested)
                         orderby parsed.Version descending, asset.UpdatedAt descending
                         select new { release, asset, parsed.Version };
        var latest = candidates.FirstOrDefault();
        if (latest == null)
            return null;

        return new(latest.Version, architecture, latest.asset, latest.release.Body ?? "");
    }

    private static (Version Version, string Architecture) ParsePackage(string name)
    {
        TryParsePackage(name, out var version, out var architecture);
        return (version, architecture);
    }

    private static bool IsUploaded(ReleaseAsset asset) => asset != null && asset.State == "uploaded";

    public static Uri GetDownloadUri(ReleaseAsset asset)
    {
        if (!Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.StartsWith("/0x78654C/CIARE/releases/download/", StringComparison.OrdinalIgnoreCase)
            || !Uri.UnescapeDataString(uri.Segments.Last()).Equals(asset.Name, StringComparison.Ordinal)
            || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidDataException("The download is not a CIARE release asset on GitHub.");
        return uri;
    }

    public static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CIARE-Updater/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    public static async Task<List<GitHubRelease>> ReadAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var releases = new List<GitHubRelease>();
        for (int page = 1; ; page++)
        {
            using var response = await client.GetAsync(
                $"https://api.github.com/repos/0x78654C/CIARE/releases?per_page=100&page={page}", cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode is 403 or 429)
                throw new HttpRequestException("GitHub is limiting update checks. Please try again later.");
            response.EnsureSuccessStatusCode();
            var batch = await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("GitHub returned an empty release response.");
            releases.AddRange(batch);
            if (batch.Count < 100)
                return releases;
        }
    }
}
