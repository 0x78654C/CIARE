// OpenAI Codex client.
//
// Supports both Codex CLI-style ChatGPT authentication from ~/.codex/auth.json
// and direct OpenAI API keys. ChatGPT auth uses the same Codex backend that the
// CLI selects for ChatGPT accounts: https://chatgpt.com/backend-api/codex.

using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodexInt
{
    public class CodexRequestBody
    {
        public string model { get; set; } = string.Empty;
        public string instructions { get; set; } = string.Empty;
        public object input { get; set; } = string.Empty;
        public int? max_output_tokens { get; set; }
        public CodexReasoning? reasoning { get; set; }
        public bool? store { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? stream { get; set; }
    }

    public class CodexInputMessage
    {
        public string type { get; set; } = "message";
        public string role { get; set; } = "user";
        public List<CodexInputContent> content { get; set; } = new();
    }

    public class CodexInputContent
    {
        public string type { get; set; } = "input_text";
        public string text { get; set; } = string.Empty;
    }

    public class CodexReasoning
    {
        public string effort { get; set; } = "medium";
    }

    public class CodexModelInfo
    {
        public string id { get; set; } = string.Empty;
        public string slug { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
        public string owned_by { get; set; } = string.Empty;
        public bool supported_in_api { get; set; }
        public string visibility { get; set; } = string.Empty;
        public string default_reasoning_level { get; set; } = string.Empty;
        public List<CodexReasoningLevel> supported_reasoning_levels { get; set; } = new();

        public string ModelId => !string.IsNullOrWhiteSpace(id) ? id : slug;
        public string DisplayName => !string.IsNullOrWhiteSpace(display_name) ? display_name : ModelId;
    }

    public class CodexReasoningLevel
    {
        public string effort { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
    }

    internal class CodexModelsResponse
    {
        public List<CodexModelInfo>? data { get; set; }
    }

    internal class CodexModelsCatalogResponse
    {
        public List<CodexModelInfo>? models { get; set; }
    }

    internal class CodexContent
    {
        public string type { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
    }

    internal class CodexOutputItem
    {
        public string type { get; set; } = string.Empty;
        public List<CodexContent>? content { get; set; }
    }

    internal class CodexResponse
    {
        public string output_text { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public CodexIncompleteDetails? incomplete_details { get; set; }
        public List<CodexOutputItem>? output { get; set; }
        public OpenAiError? error { get; set; }
    }

    internal class CodexIncompleteDetails
    {
        public string reason { get; set; } = string.Empty;
    }

    internal class OpenAiErrorResponse
    {
        public OpenAiError? error { get; set; }
        public string detail { get; set; } = string.Empty;
    }

    internal class OpenAiError
    {
        public string message { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
    }

    public class CodexClient
    {
        private static readonly HttpClient s_http = new();
        private const string OpenAIApiUrl = "https://api.openai.com/v1";
        private const string DefaultInstructions = "You are OpenAI Codex in CIARE. Answer the user's coding request directly, accurately, and concisely.";
        public const string DefaultModel = "gpt-5.3-codex";

        private CodexAuthCredentials _credentials;

        public CodexClient(string apiKey)
            : this(CodexAuthCredentials.FromApiKey(apiKey))
        {
        }

        public CodexClient(CodexAuthCredentials credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public static async Task<CodexClient> CreateFromCodexCliAsync()
        {
            var credentials = await CodexCliAuth.LoadAsync().ConfigureAwait(false);
            return new CodexClient(credentials);
        }

        public async Task<(bool ok, string message)> SignInAsync()
        {
            var credentials = await GetActiveCredentialsAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(credentials.Token))
                return (false, MissingCredentialMessage(credentials));

            using var resp = await SendWithAuthRetryAsync(auth =>
                CreateRequest(HttpMethod.Get, BuildModelsUrl(auth), auth)).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return (false, $"OpenAI Codex sign-in failed ({(int)resp.StatusCode}): {ExtractErrorMessage(body)}");

            return credentials.IsChatGpt
                ? (true, "Successfully connected to OpenAI Codex using Codex CLI ChatGPT sign-in.")
                : (true, "Successfully connected to OpenAI Codex using an OpenAI API key.");
        }

        public async Task<string> SendPromptAsync(string prompt, string model = DefaultModel, int maxTokens = 999, string reasoningEffort = "medium")
        {
            var credentials = await GetActiveCredentialsAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(credentials.Token))
                throw new InvalidOperationException(MissingCredentialMessage(credentials));

            if (string.IsNullOrWhiteSpace(prompt))
                return string.Empty;

            using var resp = await SendWithAuthRetryAsync(auth =>
            {
                var body = BuildBody(prompt, string.IsNullOrWhiteSpace(model) ? DefaultModel : model, maxTokens, auth.IsChatGpt, reasoningEffort);
                var req = CreateRequest(HttpMethod.Post, $"{GetBaseUrl(auth)}/responses", auth, acceptsEventStream: auth.IsChatGpt);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                return req;
            }).ConfigureAwait(false);

            return await ParseResponseAsync(resp).ConfigureAwait(false);
        }

        public async Task<string> ListModelsAsync()
        {
            List<CodexModelInfo> models;
            try
            {
                models = await GetModelsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return $"Failed to list OpenAI Codex models: {ex.Message}";
            }

            if (models.Count == 0)
                return "No models returned.";

            var sb = new StringBuilder();
            sb.AppendLine("Available OpenAI Codex models (use the ID in the Model field):");
            sb.AppendLine();
            foreach (var m in models)
            {
                var suffix = !string.IsNullOrWhiteSpace(m.owned_by)
                    ? $"   ({m.owned_by})"
                    : !string.IsNullOrWhiteSpace(m.DisplayName) && m.DisplayName != m.ModelId
                        ? $"   ({m.DisplayName})"
                        : string.Empty;
                sb.AppendLine($"  {m.ModelId}{suffix}");
            }
            return sb.ToString();
        }

        public async Task<List<CodexModelInfo>> GetModelsAsync()
        {
            var credentials = await GetActiveCredentialsAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(credentials.Token))
                throw new InvalidOperationException(MissingCredentialMessage(credentials));

            var models = new List<CodexModelInfo>();
            Exception? apiError = null;
            try
            {
                using var resp = await SendWithAuthRetryAsync(auth =>
                    CreateRequest(HttpMethod.Get, BuildModelsUrl(auth), auth)).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"HTTP {(int)resp.StatusCode}: {ExtractErrorMessage(body)}");

                models.AddRange(ParseModels(body, _credentials.IsChatGpt));
            }
            catch (Exception ex)
            {
                apiError = ex;
            }

            models.AddRange(await TryGetCodexCliCatalogModelsAsync().ConfigureAwait(false));
            var merged = MergeModels(models);
            if (merged.Count == 0 && apiError != null)
                throw apiError;

            return merged;
        }

        private async Task<CodexAuthCredentials> GetActiveCredentialsAsync()
        {
            if (_credentials.IsChatGpt)
                _credentials = await CodexCliAuth.RefreshIfNeededAsync(_credentials).ConfigureAwait(false);

            return _credentials;
        }

        private async Task<HttpResponseMessage> SendWithAuthRetryAsync(Func<CodexAuthCredentials, HttpRequestMessage> createRequest)
        {
            var credentials = await GetActiveCredentialsAsync().ConfigureAwait(false);
            var resp = await s_http.SendAsync(createRequest(credentials)).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.Unauthorized && credentials.CanRefresh)
            {
                resp.Dispose();
                _credentials = await CodexCliAuth.RefreshIfNeededAsync(credentials, force: true).ConfigureAwait(false);
                resp = await s_http.SendAsync(createRequest(_credentials)).ConfigureAwait(false);
            }

            return resp;
        }

        private static string BuildBody(string prompt, string model, int maxTokens, bool chatGptAuth, string reasoningEffort)
        {
            var req = new CodexRequestBody
            {
                model = model,
                instructions = DefaultInstructions,
                input = chatGptAuth ? BuildChatGptInput(prompt) : prompt,
                max_output_tokens = chatGptAuth ? null : maxTokens > 0 ? maxTokens : 999,
                reasoning = new CodexReasoning { effort = NormalizeReasoningEffort(reasoningEffort) },
                store = false,
                stream = chatGptAuth ? true : null
            };
            return JsonConvert.SerializeObject(req, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        private static List<CodexInputMessage> BuildChatGptInput(string prompt)
        {
            return new List<CodexInputMessage>
            {
                new()
                {
                    role = "user",
                    content = new List<CodexInputContent>
                    {
                        new()
                        {
                            type = "input_text",
                            text = prompt
                        }
                    }
                }
            };
        }

        private static string NormalizeReasoningEffort(string reasoningEffort)
        {
            return reasoningEffort?.Trim().ToLowerInvariant() switch
            {
                "low" => "low",
                "high" => "high",
                "xhigh" => "xhigh",
                _ => "medium"
            };
        }

        private static string BuildModelsUrl(CodexAuthCredentials credentials)
        {
            var baseUrl = GetBaseUrl(credentials);
            return credentials.IsChatGpt
                ? $"{baseUrl}/models?client_version={Uri.EscapeDataString(CodexCliAuth.CodexClientVersion)}"
                : $"{baseUrl}/models";
        }

        private static string GetBaseUrl(CodexAuthCredentials credentials)
        {
            return credentials.IsChatGpt ? CodexCliAuth.ChatGptCodexApiUrl : OpenAIApiUrl;
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            CodexAuthCredentials credentials,
            bool acceptsEventStream = false)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptsEventStream ? "text/event-stream" : "application/json"));

            if (credentials.IsChatGpt)
            {
                CodexCliAuth.AddCodexCliHeaders(req);
                req.Headers.TryAddWithoutValidation("version", CodexCliAuth.CodexClientVersion);
                if (!string.IsNullOrWhiteSpace(credentials.AccountId))
                    req.Headers.TryAddWithoutValidation("ChatGPT-Account-ID", credentials.AccountId);
                if (credentials.IsFedRampAccount)
                    req.Headers.TryAddWithoutValidation("X-OpenAI-Fedramp", "true");
            }
            else
            {
                req.Headers.UserAgent.ParseAdd("CIARE-Codex/1.0");
            }

            return req;
        }

        private static async Task<string> ParseResponseAsync(HttpResponseMessage resp)
        {
            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"OpenAI Codex API error {(int)resp.StatusCode}: {ExtractErrorMessage(content)}");

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("event-stream", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("event:", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var streamText = ParseEventStreamResponse(content);
                return string.IsNullOrWhiteSpace(streamText) ? "No response" : streamText;
            }

            var result = JsonConvert.DeserializeObject<CodexResponse>(content);
            if (!string.IsNullOrWhiteSpace(result?.error?.message))
                throw new Exception($"OpenAI Codex API error: {result.error.message}");

            var text = ExtractOutputText(result);
            if (string.Equals(result?.status, "incomplete", StringComparison.OrdinalIgnoreCase))
            {
                var reason = result?.incomplete_details?.reason;
                return string.IsNullOrWhiteSpace(text)
                    ? $"Response incomplete: {reason}"
                    : $"{text}\n\nResponse incomplete: {reason}";
            }

            return string.IsNullOrWhiteSpace(text) ? "No response" : text;
        }

        private static string ParseEventStreamResponse(string content)
        {
            var text = new StringBuilder();
            var data = new StringBuilder();
            var eventName = string.Empty;
            var finalText = string.Empty;

            using var reader = new StringReader(content);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    ProcessSseEvent(eventName, data.ToString(), text, ref finalText);
                    eventName = string.Empty;
                    data.Clear();
                    continue;
                }

                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                    eventName = line.Substring("event:".Length).Trim();
                else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    data.AppendLine(line.Substring("data:".Length).Trim());
            }

            ProcessSseEvent(eventName, data.ToString(), text, ref finalText);

            var streamed = text.ToString().Trim();
            return !string.IsNullOrWhiteSpace(streamed) ? streamed : finalText.Trim();
        }

        private static void ProcessSseEvent(string eventName, string data, StringBuilder text, ref string finalText)
        {
            data = data.Trim();
            if (string.IsNullOrWhiteSpace(data) || data == "[DONE]")
                return;

            JObject json;
            try
            {
                json = JObject.Parse(data);
            }
            catch
            {
                return;
            }

            var type = json.Value<string>("type") ?? eventName;
            if (type.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                var message = json.SelectToken("error.message")?.Value<string>()
                              ?? json.Value<string>("message")
                              ?? json.Value<string>("detail")
                              ?? data;
                throw new Exception($"OpenAI Codex API error: {message}");
            }

            if (type.EndsWith("output_text.delta", StringComparison.OrdinalIgnoreCase))
            {
                text.Append(json.Value<string>("delta"));
                return;
            }

            if (type.EndsWith("output_text.done", StringComparison.OrdinalIgnoreCase) &&
                text.Length == 0)
            {
                text.Append(json.Value<string>("text"));
                return;
            }

            if (type.Equals("response.completed", StringComparison.OrdinalIgnoreCase))
                finalText = ExtractOutputText(json["response"]);
        }

        private static List<CodexModelInfo> ParseModels(string body, bool chatGptAuth)
        {
            if (chatGptAuth)
            {
                var catalog = JsonConvert.DeserializeObject<CodexModelsCatalogResponse>(body);
                return catalog?.models ?? new List<CodexModelInfo>();
            }

            var result = JsonConvert.DeserializeObject<CodexModelsResponse>(body);
            return result?.data ?? new List<CodexModelInfo>();
        }

        private static async Task<List<CodexModelInfo>> TryGetCodexCliCatalogModelsAsync()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = BuildCodexDebugModelsCommand(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                if (!process.Start())
                    return new List<CodexModelInfo>();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                if (await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(8))).ConfigureAwait(false) != exitTask)
                {
                    TryKillProcess(process);
                    return new List<CodexModelInfo>();
                }

                var output = await outputTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);
                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                    return new List<CodexModelInfo>();

                var catalog = JsonConvert.DeserializeObject<CodexModelsCatalogResponse>(output);
                return catalog?.models ?? new List<CodexModelInfo>();
            }
            catch
            {
                return new List<CodexModelInfo>();
            }
        }

        private static string BuildCodexDebugModelsCommand()
        {
            return CodexCliAuth.BuildCodexCmdArguments("debug models");
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // The model picker can fall back to API results if the CLI catalog times out.
            }
        }

        private static List<CodexModelInfo> MergeModels(IEnumerable<CodexModelInfo> models)
        {
            var result = new List<CodexModelInfo>();
            var byId = new Dictionary<string, CodexModelInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var model in models)
            {
                var id = model.ModelId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id) ||
                    string.Equals(model.visibility, "hide", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (byId.TryGetValue(id, out var existing))
                {
                    MergeModelInfo(existing, model);
                    continue;
                }

                result.Add(model);
                byId[id] = model;
            }

            return result;
        }

        private static void MergeModelInfo(CodexModelInfo target, CodexModelInfo source)
        {
            if (string.IsNullOrWhiteSpace(target.id))
                target.id = source.id;
            if (string.IsNullOrWhiteSpace(target.slug))
                target.slug = source.slug;
            if (string.IsNullOrWhiteSpace(target.display_name))
                target.display_name = source.display_name;
            if (string.IsNullOrWhiteSpace(target.owned_by))
                target.owned_by = source.owned_by;
            if (string.IsNullOrWhiteSpace(target.visibility))
                target.visibility = source.visibility;
            if (string.IsNullOrWhiteSpace(target.default_reasoning_level))
                target.default_reasoning_level = source.default_reasoning_level;
            target.supported_reasoning_levels ??= new List<CodexReasoningLevel>();
            source.supported_reasoning_levels ??= new List<CodexReasoningLevel>();
            if (target.supported_reasoning_levels.Count == 0 && source.supported_reasoning_levels.Count > 0)
                target.supported_reasoning_levels = source.supported_reasoning_levels;
            target.supported_in_api = target.supported_in_api || source.supported_in_api;
        }

        private static string ExtractOutputText(CodexResponse? response)
        {
            if (!string.IsNullOrWhiteSpace(response?.output_text))
                return response.output_text;

            var sb = new StringBuilder();
            foreach (var item in response?.output ?? Enumerable.Empty<CodexOutputItem>())
            {
                foreach (var content in item.content ?? Enumerable.Empty<CodexContent>())
                {
                    if (content.type == "output_text" && !string.IsNullOrWhiteSpace(content.text))
                        sb.AppendLine(content.text);
                }
            }
            return sb.ToString().Trim();
        }

        private static string ExtractOutputText(JToken? response)
        {
            var direct = response?["output_text"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            var sb = new StringBuilder();
            foreach (var item in response?["output"]?.Children() ?? Enumerable.Empty<JToken>())
            {
                foreach (var content in item["content"]?.Children() ?? Enumerable.Empty<JToken>())
                {
                    if (string.Equals(content["type"]?.Value<string>(), "output_text", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = content["text"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(text))
                            sb.AppendLine(text);
                    }
                }
            }

            return sb.ToString().Trim();
        }

        private static string ExtractErrorMessage(string body)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<OpenAiErrorResponse>(body);
                if (!string.IsNullOrWhiteSpace(error?.error?.message))
                    return error.error.message;
                if (!string.IsNullOrWhiteSpace(error?.detail))
                    return error.detail;
            }
            catch
            {
                // Return the raw body below when the API did not return an error object.
            }
            return body;
        }

        private static string MissingCredentialMessage(CodexAuthCredentials credentials)
        {
            return credentials.IsChatGpt
                ? "Codex CLI ChatGPT access token is empty. Run `codex login` and try again."
                : "OpenAI API key is empty. Add an API key or use Codex CLI ChatGPT sign-in.";
        }
    }
}
