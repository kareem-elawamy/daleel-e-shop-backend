using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace daleel_e_shop.Services.AIChat
{
    public interface IGeminiApiClient
    {
        Task<string?> GenerateChatResponseAsync(List<(string role, string text)> conversationHistory, string userMessage, CancellationToken cancellationToken = default);
    }

    public class GeminiApiClient : IGeminiApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiApiClient> _logger;
        private readonly string _apiKey;
        private const string GeminiModel = "gemini-3-flash-preview";
        private const string GeminiRestApi = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        private const string SystemPrompt = "You are the Neurix AI Assistant. You provide helpful, accurate, and concise responses to user queries. You maintain context from the conversation history to provide coherent and relevant answers.";

        public GeminiApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key is not configured.");
        }

        public async Task<string?> GenerateChatResponseAsync(List<(string role, string text)> conversationHistory, string userMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting Gemini API call with {HistoryCount} messages", conversationHistory.Count);

                var client = _httpClientFactory.CreateClient();
                var url = $"{string.Format(GeminiRestApi, GeminiModel)}?key={_apiKey}";

                // Build the request payload
                var messages = new List<object>();

                // Add conversation history
                foreach (var (role, text) in conversationHistory)
                {
                    messages.Add(new
                    {
                        role = role.ToLower() == "user" ? "user" : "model",
                        parts = new[] { new { text } }
                    });
                }

                // Add current user message
                messages.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                var payload = new
                {
                    contents = messages,
                    systemInstruction = new
                    {
                        parts = new[] { new { text = SystemPrompt } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        top_p = 0.9,
                        top_k = 40,
                        max_output_tokens = 2048
                    }
                };

                var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending request to Gemini API: {Url}", url);

                var response = await client.PostAsync(url, httpContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Extract text from candidates
                if (responseData.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        if (parts[0].TryGetProperty("text", out var text))
                        {
                            var generatedText = text.GetString();
                            _logger.LogInformation("Successfully generated response from Gemini API");
                            return generatedText;
                        }
                    }
                }

                _logger.LogWarning("Unexpected response structure from Gemini API");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return null;
            }
        }
    }
}
