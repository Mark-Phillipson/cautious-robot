using Microsoft.JSInterop;

namespace BlazorApp.Client.Shared
{
    public interface IOpenAIApiKeyService
    {
        Task<string?> GetApiKeyAsync();
        Task SetApiKeyAsync(string apiKey);
        Task ClearApiKeyAsync();
    }

    public class OpenAIApiKeyService : IOpenAIApiKeyService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "openaiApiKey";
        
        public OpenAIApiKeyService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string?> GetApiKeyAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        }

        public async Task SetApiKeyAsync(string apiKey)
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, apiKey);
        }

        public async Task ClearApiKeyAsync()
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        }
    }
}
