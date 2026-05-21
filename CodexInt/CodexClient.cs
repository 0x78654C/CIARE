// OpenAI Codex API client
//
// OpenAI API access uses an API key rather than a public OAuth device flow.
// The public surface mirrors CopilotClient where useful for the app:
//   - SignInAsync validates the stored API key.
//   - ListModelsAsync shows Codex/coding models available to the key.
//   - SendPromptAsync sends a prompt through the Responses API.

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace CodexInt
{
    public class CodexRequestBody
    {
        public string model { get; set; } = string.Empty;
        public string input { get; set; } = string.Empty;
        public int max_output_tokens { get; set; }
        public CodexReasoning reasoning { get; set; } = new();
        public bool store { get; set; }
    }

    public class CodexReasoning
    {
        public string effort { get; set; } = "medium";
    }

    public class CodexModelInfo
    {
        public string id { get; set; } = string.Empty;
        public string owned_by { get; set; } = string.Empty;
    }

    internal class CodexModelsResponse
    {
        public List<CodexModelInfo>? data { get; set; }
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
        public const string DefaultModel = "gpt-5.3-codex";

        private readonly string _apiKey;

        public CodexClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<(bool ok, string message)> SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "OpenAI API key is empty.");

            using var req = CreateRequest(HttpMethod.Get, $"{OpenAIApiUrl}/models");
            var resp = await s_http.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return (false, $"OpenAI Codex sign-in failed ({(int)resp.StatusCode}): {ExtractErrorMessage(body)}");

            return (true, "Successfully connected to OpenAI Codex.");
        }

        public async Task<string> SendPromptAsync(string prompt, string model = DefaultModel, int maxTokens = 999)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("OpenAI API key is empty. Add the key in Options > AI settings.");

            if (string.IsNullOrWhiteSpace(prompt))
                return string.Empty;

            var body = BuildBody(prompt, string.IsNullOrWhiteSpace(model) ? DefaultModel : model, maxTokens);
            using var req = CreateRequest(HttpMethod.Post, $"{OpenAIApiUrl}/responses");
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var resp = await s_http.SendAsync(req).ConfigureAwait(false);
            return await ParseResponseAsync(resp).ConfigureAwait(false);
        }

        public async Task<string> ListModelsAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "OpenAI API key is empty.\n\nEnter an OpenAI API key, save settings, then click List Models again.";

            using var req = CreateRequest(HttpMethod.Get, $"{OpenAIApiUrl}/models");
            var resp = await s_http.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return $"Failed to list OpenAI models ({(int)resp.StatusCode}): {ExtractErrorMessage(body)}";

            var result = JsonConvert.DeserializeObject<CodexModelsResponse>(body);
            if (result?.data == null || result.data.Count == 0)
                return $"No models returned. Raw response:\n{body}";

            var codexModels = result.data
                .Where(m => m.id.Contains("codex", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.id)
                .ToList();

            var models = codexModels.Count > 0 ? codexModels : result.data.OrderBy(m => m.id).ToList();
            var title = codexModels.Count > 0
                ? "Available OpenAI Codex models (use the ID in the Model field):"
                : "No Codex-specific models were returned. Available OpenAI models:";

            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine();
            foreach (var m in models)
                sb.AppendLine($"  {m.id}   ({m.owned_by})");
            return sb.ToString();
        }

        private static string BuildBody(string prompt, string model, int maxTokens)
        {
            var req = new CodexRequestBody
            {
                model = model,
                input = prompt,
                max_output_tokens = maxTokens > 0 ? maxTokens : 999,
                reasoning = new CodexReasoning { effort = "medium" },
                store = false
            };
            return JsonConvert.SerializeObject(req);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Headers.UserAgent.ParseAdd("CIARE-Codex/1.0");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return req;
        }

        private static async Task<string> ParseResponseAsync(HttpResponseMessage resp)
        {
            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"OpenAI Codex API error {(int)resp.StatusCode}: {ExtractErrorMessage(content)}");

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

        private static string ExtractErrorMessage(string body)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<OpenAiErrorResponse>(body);
                if (!string.IsNullOrWhiteSpace(error?.error?.message))
                    return error.error.message;
            }
            catch
            {
                // Return the raw body below when the API did not return an error object.
            }
            return body;
        }
    }
}
