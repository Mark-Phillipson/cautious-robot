using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorApp.Client.Shared
{
    public interface IApiKeyService
    {
        Task<string?> GetApiKeyAsync();
        Task SetApiKeyAsync(string apiKey);
        Task ClearApiKeyAsync();
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "wordsApiKey";
        public ApiKeyService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        public async Task<string?> GetApiKeyAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", StorageKey);
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
