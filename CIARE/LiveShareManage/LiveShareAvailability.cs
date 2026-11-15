using Microsoft.AspNetCore.Http.Connections;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CIARE.LiveShareManage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class LiveShareAvailability
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
        MaxResponseContentBufferSize = 64 * 1024
    };

    // Probe the hub's own protocol endpoint without opening a transport or joining a session.
    internal static async Task<bool> CheckAsync(string hubUrl, CancellationToken cancellationToken = default)
    {
        if (!Utils.Options.LiveShare.TryNormalizeApiUrl(hubUrl, out string normalized)) return false;
        var uri = new UriBuilder(normalized);
        uri.Path = uri.Path.TrimEnd('/') + "/negotiate";
        uri.Fragment = string.Empty;
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["negotiateVersion"] = "1";
        uri.Query = query.ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, uri.Uri);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("CIARE-LiveShare/1.0");
        request.Headers.Add("ngrok-skip-browser-warning", "true");
        try
        {
            using var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return false;
            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var negotiation = NegotiateProtocol.ParseResponse(payload);
            if (!string.IsNullOrEmpty(negotiation.Error)) return false;
            // Redirect responses also identify a live SignalR negotiation endpoint.
            if (!string.IsNullOrEmpty(negotiation.Url))
                return Utils.Options.LiveShare.TryNormalizeApiUrl(negotiation.Url, out _);
            return !string.IsNullOrEmpty(negotiation.ConnectionId)
                && (negotiation.Version == 0 || negotiation.Version == 1 && !string.IsNullOrEmpty(negotiation.ConnectionToken))
                && negotiation.AvailableTransports?.Any(transport =>
                    transport.Transport is "WebSockets" or "ServerSentEvents" or "LongPolling"
                    && transport.TransferFormats?.Contains("Text") == true) == true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
        catch (HttpRequestException) { return false; }
        catch (InvalidDataException) { return false; }
    }
}
