// GitHub Copilot API client
//
// Authentication flow (OAuth device flow — requires active Copilot subscription):
//   1. Call StartDeviceAuthAsync() to get a user_code + verification_uri.
//   2. Show the user_code to the user and open verification_uri in a browser.
//   3. Poll PollForTokenAsync() until the user authorizes — returns a long-lived GitHub OAuth token.
//   4. Store that token and pass it as copilotOAuthToken to CopilotClient.
//   5. The client exchanges it for a short-lived session token via
//      GET https://api.github.com/copilot_internal/v2/token
//   6. Uses the session token with POST https://api.githubcopilot.com/chat/completions
//      (supports all Copilot models: GPT-4o, Claude Sonnet, Gemini, etc.)
//
// The device flow uses client_id Iv1.b507a08c87ecfe98 (VS Code Copilot extension OAuth app),
// which is the standard approach used by open-source Copilot clients (copilot.vim, copilot.lua...).
//
// Fallback (no Copilot OAuth token stored — uses GitHub Models free tier):
//   POST https://models.inference.ai.azure.com/chat/completions  (PAT with models:read scope)
//   Only supports OpenAI, Phi, Mistral, Llama models. No Claude.

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace CopilotInt
{
    public class CopilotMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }

    public class CopilotRequestBody
    {
        public string model { get; set; } = string.Empty;
        public List<CopilotMessage> messages { get; set; } = new();
        public int max_tokens { get; set; }
    }

    public class CopilotChoice
    {
        public CopilotMessage? message { get; set; }
    }

    public class CopilotResponse
    {
        public List<CopilotChoice>? choices { get; set; }
    }

    public class CopilotModelInfo
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string vendor { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
    }

    internal class CopilotModelsResponse
    {
        public List<CopilotModelInfo>? data { get; set; }
    }

    internal class CopilotSessionToken
    {
        public string token { get; set; } = string.Empty;
        public long expires_at { get; set; }
        public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expires_at - 60;
    }

    internal class DeviceCodeResponse
    {
        public string device_code { get; set; } = string.Empty;
        public string user_code { get; set; } = string.Empty;
        public string verification_uri { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public int interval { get; set; }
    }

    internal class AccessTokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public string error_description { get; set; } = string.Empty;
    }

    public class CopilotClient
    {
        private static readonly HttpClient s_http = new();
        private const string GitHubApiUrl  = "https://api.github.com";
        private const string CopilotApiUrl = "https://api.githubcopilot.com";
        private const string ModelsApiUrl  = "https://models.inference.ai.azure.com";

        // VS Code Copilot extension OAuth app client ID -- used by open-source Copilot clients.
        private const string CopilotClientId = "Iv1.b507a08c87ecfe98";

        private const string EditorVersion       = "vscode/1.95.0";
        private const string EditorPluginVersion = "copilot-chat/0.24.0";

        // Short-lived session token cache (~30 min lifetime).
        private static CopilotSessionToken? s_sessionToken;
        private static readonly SemaphoreSlim s_lock = new(1, 1);

        private readonly string _copilotOAuthToken;
        private readonly string _pat;

        /// <param name="copilotOAuthToken">Long-lived OAuth token from the device flow. Used to obtain session tokens.</param>
        /// <param name="modelsApiPat">GitHub PAT used only as fallback for the GitHub Models API.</param>
        public CopilotClient(string copilotOAuthToken = "", string modelsApiPat = "")
        {
            _copilotOAuthToken = copilotOAuthToken;
            _pat = modelsApiPat;
        }

        // -- Device flow -----------------------------------------------------------------

        /// <summary>
        /// Starts the GitHub device authorization flow. Open <c>verificationUri</c> in a browser,
        /// show the user <c>userCode</c>, then poll <see cref="PollForTokenAsync"/>.
        /// </summary>
        public static async Task<(string userCode, string verificationUri, string deviceCode, int intervalSec, string error)> StartDeviceAuthAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/device/code");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", CopilotClientId),
                new KeyValuePair<string, string>("scope", "read:user"),
            });

            var resp = await s_http.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return (string.Empty, string.Empty, string.Empty, 5, $"Device auth failed ({(int)resp.StatusCode}): {body}");

            var dc = JsonConvert.DeserializeObject<DeviceCodeResponse>(body);
            if (dc == null || string.IsNullOrEmpty(dc.device_code))
                return (string.Empty, string.Empty, string.Empty, 5, $"Invalid device code response: {body}");

            return (dc.user_code, dc.verification_uri, dc.device_code, dc.interval > 0 ? dc.interval : 5, string.Empty);
        }

        /// <summary>
        /// Polls GitHub until the user authorizes the device code. Returns the long-lived OAuth token.
        /// </summary>
        public static async Task<(string token, string error)> PollForTokenAsync(string deviceCode, int intervalSec, CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), ct).ConfigureAwait(false);

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", CopilotClientId),
                    new KeyValuePair<string, string>("device_code", deviceCode),
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                });

                var resp = await s_http.SendAsync(req).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<AccessTokenResponse>(body);

                if (!string.IsNullOrEmpty(result?.access_token))
                    return (result.access_token, string.Empty);

                switch (result?.error)
                {
                    case "authorization_pending":
                        continue;
                    case "slow_down":
                        intervalSec = Math.Min(intervalSec + 2, 15);
                        continue;
                    default:
                        if (!resp.IsSuccessStatusCode)
                            return (string.Empty, $"Poll error ({(int)resp.StatusCode}): {body}");
                        return (string.Empty, result?.error_description ?? result?.error ?? "Unknown error");
                }
            }
            return (string.Empty, "Authorization cancelled.");
        }

        // -- Session token exchange ------------------------------------------------------

        private async Task<(string? token, string? error)> TryGetSessionTokenAsync()
        {
            await s_lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (s_sessionToken != null && !s_sessionToken.IsExpired)
                    return (s_sessionToken.token, null);

                if (string.IsNullOrEmpty(_copilotOAuthToken))
                    return (null, "Not signed in to GitHub Copilot. Use 'Sign in to Copilot' in Options > AI settings.");

                using var req = new HttpRequestMessage(HttpMethod.Get, $"{GitHubApiUrl}/copilot_internal/v2/token");
                req.Headers.Authorization = new AuthenticationHeaderValue("Token", _copilotOAuthToken);
                req.Headers.UserAgent.ParseAdd("GitHubCopilotChat/0.24.0");
                req.Headers.Add("Editor-Version", EditorVersion);
                req.Headers.Add("Editor-Plugin-Version", EditorPluginVersion);

                var resp = await s_http.SendAsync(req).ConfigureAwait(false);
                var json  = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    return (null, $"HTTP {(int)resp.StatusCode} from copilot_internal/v2/token: {json}");

                var tok = JsonConvert.DeserializeObject<CopilotSessionToken>(json);
                if (tok == null || string.IsNullOrEmpty(tok.token))
                    return (null, $"Unexpected response (no token field): {json}");

                s_sessionToken = tok;
                return (tok.token, null);
            }
            finally
            {
                s_lock.Release();
            }
        }

        // -- Public API ------------------------------------------------------------------

        public async Task<string> SendPromptAsync(string prompt, string model = "claude-3.7-sonnet", int maxTokens = 999)
        {
            if (!string.IsNullOrEmpty(_copilotOAuthToken))
            {
                var (sessionToken, _) = await TryGetSessionTokenAsync();
                if (sessionToken != null)
                    return await SendViaCopilotApiAsync(sessionToken, prompt, model, maxTokens);
            }
            // Fallback: GitHub Models API with PAT
            return await SendViaModelsApiAsync(prompt, model, maxTokens);
        }

        public async Task<string> ListModelsAsync()
        {
            if (string.IsNullOrEmpty(_copilotOAuthToken))
            {
                return "Not signed in to GitHub Copilot.\n\n" +
                       "In Options > AI settings, select 'GitHub Copilot' and click 'Sign in to Copilot'.";
            }

            var (sessionToken, tokenError) = await TryGetSessionTokenAsync();
            if (sessionToken == null)
                return $"Copilot session token exchange failed.\n\nReason: {tokenError}";

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{CopilotApiUrl}/models");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
            req.Headers.UserAgent.ParseAdd("GitHubCopilotChat/0.24.0");
            req.Headers.Add("Editor-Version", EditorVersion);
            req.Headers.Add("Copilot-Integration-Id", "vscode-chat");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await s_http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return $"Failed to list models ({(int)resp.StatusCode}): {body}";

            var result = JsonConvert.DeserializeObject<CopilotModelsResponse>(body);
            if (result?.data == null || result.data.Count == 0)
                return $"No models returned. Raw response:\n{body}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Available Copilot models (use the ID in the Model field):");
            sb.AppendLine();
            foreach (var m in result.data.OrderBy(x => x.id))
                sb.AppendLine($"  {m.id}   ({m.name})");
            return sb.ToString();
        }

        // -- Private helpers -------------------------------------------------------------

        private async Task<string> SendViaCopilotApiAsync(string sessionToken, string prompt, string model, int maxTokens)
        {
            var body = BuildBody(prompt, model, maxTokens);
            var resp = await PostAsync($"{CopilotApiUrl}/chat/completions", body, sessionToken, copilotHeaders: true);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Session token expired mid-use -- invalidate and retry once.
                s_sessionToken = null;
                var (newToken, _) = await TryGetSessionTokenAsync();
                if (newToken != null)
                    resp = await PostAsync($"{CopilotApiUrl}/chat/completions", body, newToken, copilotHeaders: true);
            }

            return await ParseResponseAsync(resp, "Copilot API");
        }

        private async Task<string> SendViaModelsApiAsync(string prompt, string model, int maxTokens)
        {
            var body = BuildBody(prompt, model, maxTokens);
            var resp = await PostAsync($"{ModelsApiUrl}/chat/completions", body, _pat, copilotHeaders: false);
            return await ParseResponseAsync(resp, "GitHub Models API");
        }

        private static string BuildBody(string prompt, string model, int maxTokens)
        {
            var req = new CopilotRequestBody
            {
                model      = model,
                messages   = new List<CopilotMessage> { new() { role = "user", content = prompt } },
                max_tokens = maxTokens
            };
            return JsonConvert.SerializeObject(req);
        }

        private static async Task<HttpResponseMessage> PostAsync(string url, string jsonBody, string token, bool copilotHeaders)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.UserAgent.ParseAdd("GitHubCopilotChat/0.24.0");

            if (copilotHeaders)
            {
                req.Headers.Add("Editor-Version", EditorVersion);
                req.Headers.Add("Editor-Plugin-Version", EditorPluginVersion);
                req.Headers.Add("Copilot-Integration-Id", "vscode-chat");
            }

            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await s_http.SendAsync(req);
        }

        private static async Task<string> ParseResponseAsync(HttpResponseMessage resp, string apiName)
        {
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{apiName} error {(int)resp.StatusCode}: {content}");

            var result = JsonConvert.DeserializeObject<CopilotResponse>(content);
            return result?.choices?[0]?.message?.content ?? "No response";
        }
    }
}