using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodexInt
{
    public enum CodexAuthMode
    {
        ApiKey,
        ChatGpt
    }

    public sealed class CodexAuthCredentials
    {
        public CodexAuthMode Mode { get; }
        public string Token { get; internal set; }
        public string RefreshToken { get; internal set; }
        public string AccountId { get; internal set; }
        public bool IsFedRampAccount { get; internal set; }
        public string AuthFilePath { get; }

        public bool IsChatGpt => Mode == CodexAuthMode.ChatGpt;
        public bool CanRefresh => IsChatGpt && !string.IsNullOrWhiteSpace(RefreshToken) && !string.IsNullOrWhiteSpace(AuthFilePath);

        private CodexAuthCredentials(
            CodexAuthMode mode,
            string token,
            string refreshToken = "",
            string accountId = "",
            bool isFedRampAccount = false,
            string authFilePath = "")
        {
            Mode = mode;
            Token = token;
            RefreshToken = refreshToken;
            AccountId = accountId;
            IsFedRampAccount = isFedRampAccount;
            AuthFilePath = authFilePath;
        }

        public static CodexAuthCredentials FromApiKey(string apiKey)
        {
            return new CodexAuthCredentials(CodexAuthMode.ApiKey, apiKey?.Trim() ?? string.Empty);
        }

        internal static CodexAuthCredentials FromChatGpt(
            string accessToken,
            string refreshToken,
            string accountId,
            bool isFedRampAccount,
            string authFilePath)
        {
            return new CodexAuthCredentials(
                CodexAuthMode.ChatGpt,
                accessToken?.Trim() ?? string.Empty,
                refreshToken?.Trim() ?? string.Empty,
                accountId?.Trim() ?? string.Empty,
                isFedRampAccount,
                authFilePath);
        }
    }

    public static class CodexCliAuth
    {
        internal const string ClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
        internal const string ChatGptCodexApiUrl = "https://chatgpt.com/backend-api/codex";

        private const string FallbackCodexClientVersion = "0.130.0";
        private const string RefreshTokenUrl = "https://auth.openai.com/oauth/token";

        internal static string CodexClientVersion => GetCodexClientVersion();

        public static string GetCodexHome()
        {
            var configured = Environment.GetEnvironmentVariable("CODEX_HOME");
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(profile, ".codex");
        }

        public static string GetAuthFilePath()
        {
            return Path.Combine(GetCodexHome(), "auth.json");
        }

        public static bool AuthFileExists()
        {
            return File.Exists(GetAuthFilePath());
        }

        internal static string GetCodexCommandPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                var cmd = Path.Combine(appData, "npm", "codex.cmd");
                if (File.Exists(cmd))
                    return cmd;

                var ps1 = Path.Combine(appData, "npm", "codex.ps1");
                if (File.Exists(ps1))
                    return ps1;
            }

            return "codex";
        }

        internal static string BuildCodexCmdArguments(string commandArguments)
        {
            var commandPath = GetCodexCommandPath();
            return string.Equals(commandPath, "codex", StringComparison.OrdinalIgnoreCase)
                ? $"/c codex {commandArguments}"
                : $"/c \"\"{commandPath}\" {commandArguments}\"";
        }

        public static async Task<CodexAuthCredentials?> TryLoadAsync(bool refreshIfNeeded = true)
        {
            var authFile = GetAuthFilePath();
            if (!File.Exists(authFile))
                return null;

            return await LoadAsync(authFile, refreshIfNeeded).ConfigureAwait(false);
        }

        public static async Task<CodexAuthCredentials> LoadAsync(bool refreshIfNeeded = true)
        {
            return await LoadAsync(GetAuthFilePath(), refreshIfNeeded).ConfigureAwait(false);
        }

        internal static async Task<CodexAuthCredentials> RefreshIfNeededAsync(
            CodexAuthCredentials credentials,
            bool force = false)
        {
            if (!credentials.CanRefresh)
                return credentials;

            if (!force && !ShouldRefresh(credentials.Token))
                return credentials;

            return await RefreshAsync(credentials).ConfigureAwait(false);
        }

        private static async Task<CodexAuthCredentials> LoadAsync(string authFile, bool refreshIfNeeded)
        {
            var raw = await File.ReadAllTextAsync(authFile).ConfigureAwait(false);
            var root = JObject.Parse(raw);
            var authMode = root.Value<string>("auth_mode") ?? string.Empty;
            var apiKey = root.Value<string>("OPENAI_API_KEY") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(apiKey) &&
                (authMode.Contains("api", StringComparison.OrdinalIgnoreCase) || root["tokens"] == null))
            {
                return CodexAuthCredentials.FromApiKey(apiKey);
            }

            var tokens = root["tokens"] as JObject;
            var accessToken = tokens?.Value<string>("access_token") ?? string.Empty;
            var refreshToken = tokens?.Value<string>("refresh_token") ?? string.Empty;
            var idToken = tokens?.Value<string>("id_token") ?? string.Empty;
            var accountId = tokens?.Value<string>("account_id") ?? ExtractAccountId(idToken);
            var isFedRamp = ExtractFedRampFlag(idToken);

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException($"Codex CLI auth file does not contain ChatGPT tokens: {authFile}");

            var credentials = CodexAuthCredentials.FromChatGpt(
                accessToken,
                refreshToken,
                accountId,
                isFedRamp,
                authFile);

            return refreshIfNeeded
                ? await RefreshIfNeededAsync(credentials).ConfigureAwait(false)
                : credentials;
        }

        private static async Task<CodexAuthCredentials> RefreshAsync(CodexAuthCredentials credentials)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, RefreshTokenUrl);
            AddCodexCliHeaders(req);
            req.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    client_id = ClientId,
                    grant_type = "refresh_token",
                    refresh_token = credentials.RefreshToken
                }),
                Encoding.UTF8,
                "application/json");

            using var client = new HttpClient();
            var resp = await client.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var hint = resp.StatusCode == HttpStatusCode.Unauthorized
                    ? "Your Codex CLI ChatGPT refresh token is no longer valid. Run `codex login` again."
                    : "Codex CLI ChatGPT token refresh failed.";
                throw new InvalidOperationException($"{hint} ({(int)resp.StatusCode}): {ExtractRefreshError(body)}");
            }

            var refresh = JObject.Parse(body);
            var accessToken = refresh.Value<string>("access_token") ?? credentials.Token;
            var refreshToken = refresh.Value<string>("refresh_token") ?? credentials.RefreshToken;
            var idToken = refresh.Value<string>("id_token") ?? string.Empty;
            var accountId = ExtractAccountId(idToken);
            var isFedRamp = ExtractFedRampFlag(idToken);

            await UpdateAuthFileAsync(
                credentials.AuthFilePath,
                accessToken,
                refreshToken,
                idToken,
                accountId,
                isFedRamp).ConfigureAwait(false);

            return CodexAuthCredentials.FromChatGpt(
                accessToken,
                refreshToken,
                accountId,
                isFedRamp,
                credentials.AuthFilePath);
        }

        internal static void AddCodexCliHeaders(HttpRequestMessage req)
        {
            req.Headers.TryAddWithoutValidation("originator", "codex_cli_rs");
            req.Headers.TryAddWithoutValidation(
                "User-Agent",
                $"codex_cli_rs/{CodexClientVersion} (Windows; CIARE)");
        }

        private static string GetCodexClientVersion()
        {
            return ReadCodexPackageVersion()
                   ?? ReadCodexCommandVersion()
                   ?? FallbackCodexClientVersion;
        }

        private static string? ReadCodexPackageVersion()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
                return null;

            var packageJson = Path.Combine(appData, "npm", "node_modules", "@openai", "codex", "package.json");
            if (!File.Exists(packageJson))
                return null;

            try
            {
                var package = JObject.Parse(File.ReadAllText(packageJson));
                return NormalizeCodexVersion(package.Value<string>("version"));
            }
            catch
            {
                return null;
            }
        }

        private static string? ReadCodexCommandVersion()
        {
            try
            {
                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = BuildCodexCmdArguments("--version"),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                if (!process.Start())
                    return null;

                if (!process.WaitForExit(3000))
                {
                    TryKillProcess(process);
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();

                return process.ExitCode == 0
                    ? NormalizeCodexVersion(output)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? NormalizeCodexVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return null;

            var normalized = version.Trim();
            if (normalized.StartsWith("codex-cli ", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("codex-cli ".Length).Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1).Trim();

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static void TryKillProcess(System.Diagnostics.Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Version detection falls back to the bundled compatibility version.
            }
        }

        private static async Task UpdateAuthFileAsync(
            string authFile,
            string accessToken,
            string refreshToken,
            string idToken,
            string accountId,
            bool isFedRamp)
        {
            var root = JObject.Parse(await File.ReadAllTextAsync(authFile).ConfigureAwait(false));
            var tokens = root["tokens"] as JObject ?? new JObject();
            tokens["access_token"] = accessToken;
            tokens["refresh_token"] = refreshToken;
            if (!string.IsNullOrWhiteSpace(idToken))
                tokens["id_token"] = idToken;
            if (!string.IsNullOrWhiteSpace(accountId))
                tokens["account_id"] = accountId;
            root["tokens"] = tokens;
            root["last_refresh"] = DateTimeOffset.UtcNow.ToString("O");

            await File.WriteAllTextAsync(authFile, root.ToString(Formatting.Indented)).ConfigureAwait(false);
        }

        private static bool ShouldRefresh(string accessToken)
        {
            var expiresAt = ExtractExpiration(accessToken);
            return expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow.AddMinutes(2);
        }

        private static DateTimeOffset? ExtractExpiration(string jwt)
        {
            var payload = TryDecodeJwtPayload(jwt);
            var exp = payload?.Value<long?>("exp");
            return exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(exp.Value) : null;
        }

        private static string ExtractAccountId(string jwt)
        {
            var auth = TryDecodeJwtPayload(jwt)?["https://api.openai.com/auth"] as JObject;
            return auth?.Value<string>("chatgpt_account_id") ?? string.Empty;
        }

        private static bool ExtractFedRampFlag(string jwt)
        {
            var auth = TryDecodeJwtPayload(jwt)?["https://api.openai.com/auth"] as JObject;
            return auth?.Value<bool?>("chatgpt_account_is_fedramp") ?? false;
        }

        private static JObject? TryDecodeJwtPayload(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            var parts = jwt.Split('.');
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                return JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }
            return Convert.FromBase64String(padded);
        }

        private static string ExtractRefreshError(string body)
        {
            try
            {
                var json = JObject.Parse(body);
                var message = json.SelectToken("error.message")?.Value<string>()
                              ?? json.Value<string>("error_description")
                              ?? json.Value<string>("error")
                              ?? json.Value<string>("detail");
                return string.IsNullOrWhiteSpace(message) ? body : message;
            }
            catch
            {
                return body;
            }
        }
    }
}
