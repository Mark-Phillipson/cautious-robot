using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorApp.Client.Shared
{
    public interface IOpenAIService
    {
        Task<string> GenerateContentAsync(string prompt, string systemMessage = "You are a helpful English language tutor.");
        Task<OpenAIImageResult> GenerateImageAsync(string prompt, string size = "256x256");
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IOpenAIApiKeyService _apiKeyService;
        private const string OpenAIBaseUrl = "https://api.openai.com/v1/chat/completions";
        private const string OpenAIImagesUrl = "https://api.openai.com/v1/images/generations";

        private static readonly JsonSerializerOptions OpenAISnakeCaseJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string? TryExtractOpenAIErrorMessage(string? errorContent)
        {
            if (string.IsNullOrWhiteSpace(errorContent)) return null;

            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

                if (doc.RootElement.TryGetProperty("error", out var errorObj) &&
                    errorObj.ValueKind == JsonValueKind.Object &&
                    errorObj.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                {
                    var text = msg.GetString();
                    return string.IsNullOrWhiteSpace(text) ? null : text;
                }
            }
            catch
            {
                // Ignore parsing failures; caller will fallback.
            }

            return null;
        }

        public OpenAIService(HttpClient httpClient, IOpenAIApiKeyService apiKeyService)
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

                var json = JsonSerializer.Serialize(request, OpenAISnakeCaseJson);

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
                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, OpenAISnakeCaseJson);

                return openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling OpenAI API: {ex.Message}");
                return "Sorry, there was an error generating AI content. Please try again.";
            }
        }

        public async Task<OpenAIImageResult> GenerateImageAsync(string prompt, string size = "1024x1024")
        {
            try
            {
                var apiKey = await _apiKeyService.GetApiKeyAsync();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return OpenAIImageResult.Fail("Please set your OpenAI API key first to use AI-generated content.");
                }

                // Keep defaults conservative and inexpensive.
                var request = new OpenAIImageRequest
                {
                    Model = "gpt-image-1",
                    Prompt = prompt,
                    Size = string.IsNullOrWhiteSpace(size) ? "1024x1024" : size,
                    N = 1,
                    ResponseFormat = "b64_json"
                };

                async Task<(HttpResponseMessage response, string content)> SendImageRequestAsync(OpenAIImageRequest req)
                {
                    var jsonBody = JsonSerializer.Serialize(req, OpenAISnakeCaseJson);
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAIImagesUrl);
                    httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                    httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var resp = await _httpClient.SendAsync(httpRequest);
                    var content = await resp.Content.ReadAsStringAsync();
                    return (resp, content);
                }

                var (response, responseContent) = await SendImageRequestAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // Some API variants reject `response_format`. If so, retry without it.
                    var extracted = TryExtractOpenAIErrorMessage(responseContent) ?? responseContent;
                    if ((int)response.StatusCode == 400 &&
                        extracted.Contains("Unknown parameter", StringComparison.OrdinalIgnoreCase) &&
                        extracted.Contains("response_format", StringComparison.OrdinalIgnoreCase))
                    {
                        request.ResponseFormat = null;
                        (response, responseContent) = await SendImageRequestAsync(request);
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Some API variants only accept a limited set of sizes.
                    var extracted = TryExtractOpenAIErrorMessage(responseContent) ?? responseContent;
                    if ((int)response.StatusCode == 400 &&
                        extracted.Contains("Invalid value", StringComparison.OrdinalIgnoreCase) &&
                        extracted.Contains("Supported values", StringComparison.OrdinalIgnoreCase) &&
                        extracted.Contains("size", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Size = "1024x1024";
                        (response, responseContent) = await SendImageRequestAsync(request);
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"OpenAI Image API Error: {response.StatusCode} - {responseContent}");
                    var message = TryExtractOpenAIErrorMessage(responseContent);
                    var status = (int)response.StatusCode;
                    var reason = string.IsNullOrWhiteSpace(response.ReasonPhrase) ? response.StatusCode.ToString() : response.ReasonPhrase;
                    var detail = string.IsNullOrWhiteSpace(message) ? "The request was rejected by the API." : message;
                    return OpenAIImageResult.Fail($"Image generation failed ({status} {reason}): {detail}");
                }

                var openAIResponse = JsonSerializer.Deserialize<OpenAIImageResponse>(responseContent, OpenAISnakeCaseJson);
                var item = openAIResponse?.Data?.FirstOrDefault();
                var b64 = item?.B64Json;
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    return OpenAIImageResult.Ok($"data:image/png;base64,{b64}");
                }

                // Fallback: if the API returns a URL, fetch bytes and convert to data URL.
                var url = item?.Url;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        using var imgResp = await _httpClient.GetAsync(url);
                        if (!imgResp.IsSuccessStatusCode)
                        {
                            return OpenAIImageResult.Fail($"Image generation succeeded, but downloading the image failed ({(int)imgResp.StatusCode}).");
                        }

                        var bytes = await imgResp.Content.ReadAsByteArrayAsync();
                        var contentType = imgResp.Content.Headers.ContentType?.MediaType;
                        if (string.IsNullOrWhiteSpace(contentType))
                        {
                            contentType = "image/png";
                        }

                        var b64img = Convert.ToBase64String(bytes);
                        return OpenAIImageResult.Ok($"data:{contentType};base64,{b64img}");
                    }
                    catch (Exception ex)
                    {
                        return OpenAIImageResult.Fail($"Image generation succeeded, but downloading the image failed: {ex.Message}");
                    }
                }

                return OpenAIImageResult.Fail("No image was returned by the AI service.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling OpenAI Image API: {ex.Message}");

                // In Blazor WebAssembly, browser fetch/CORS failures often surface as "TypeError: Failed to fetch".
                // Provide a more actionable message (without over-claiming).
                var msg = ex.Message ?? string.Empty;
                if (msg.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("CORS", StringComparison.OrdinalIgnoreCase))
                {
                    return OpenAIImageResult.Fail("Image request failed in the browser (likely network/CORS). Check DevTools Console for details; you may need a server-side proxy to call OpenAI.");
                }

                return OpenAIImageResult.Fail($"Image request failed: {ex.Message}");
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

    public sealed class OpenAIImageResult
    {
        public string? DataUrl { get; init; }
        public string? Error { get; init; }
        public bool Success => !string.IsNullOrWhiteSpace(DataUrl) && string.IsNullOrWhiteSpace(Error);

        public static OpenAIImageResult Ok(string dataUrl) => new() { DataUrl = dataUrl };
        public static OpenAIImageResult Fail(string error) => new() { Error = error };
    }

    public class OpenAIImageRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-image-1";

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public string Size { get; set; } = "1024x1024";

        [JsonPropertyName("n")]
        public int? N { get; set; }

        [JsonPropertyName("response_format")]
        public string? ResponseFormat { get; set; } = "b64_json";
    }

    public class OpenAIImageResponse
    {
        [JsonPropertyName("data")]
        public OpenAIImageData[]? Data { get; set; }
    }

    public class OpenAIImageData
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
