using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorApp.Client.Shared
{
    public interface IOpenAIService
    {
        Task<string> GenerateContentAsync(string prompt, string systemMessage = "You are a helpful English language tutor.");
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IApiKeyService _apiKeyService;
        private const string OpenAIBaseUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(HttpClient httpClient, IApiKeyService apiKeyService)
        {
            _httpClient = httpClient;
            _apiKeyService = apiKeyService;
        }

        public async Task<string> GenerateContentAsync(string prompt, string systemMessage = "You are a helpful English language tutor.")
        {
            try
            {
                var apiKey = await _apiKeyService.GetApiKeyAsync();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return "Please set your OpenAI API key first to use AI-generated content.";
                }

                var request = new OpenAIRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[]
                    {
                        new OpenAIMessage { Role = "system", Content = systemMessage },
                        new OpenAIMessage { Role = "user", Content = prompt }
                    },
                    MaxTokens = 500,
                    Temperature = 0.7
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAIBaseUrl);
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"OpenAI API Error: {response.StatusCode} - {errorContent}");
                    return "Sorry, there was an error generating AI content. Please check your API key and try again.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling OpenAI API: {ex.Message}");
                return "Sorry, there was an error generating AI content. Please try again.";
            }
        }
    }

    // OpenAI API Models
    public class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";

        [JsonPropertyName("messages")]
        public OpenAIMessage[] Messages { get; set; } = Array.Empty<OpenAIMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 500;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public OpenAIChoice[]? Choices { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
    }
}
