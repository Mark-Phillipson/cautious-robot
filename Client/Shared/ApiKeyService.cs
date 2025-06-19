using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorApp.Client.Shared
{
    public interface IWordsApiKeyService
    {
        Task<string?> GetApiKeyAsync();
        Task SetApiKeyAsync(string apiKey);
        Task ClearApiKeyAsync();
    }

    // Keep the old interface for backward compatibility
    public interface IApiKeyService : IWordsApiKeyService { }    public class WordsApiKeyService : IWordsApiKeyService, IApiKeyService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "wordsapi_rapidapi_key";
        
        public WordsApiKeyService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        
        public async Task<string?> GetApiKeyAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
        }
        
        public async Task SetApiKeyAsync(string apiKey)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, apiKey);
        }
        
        public async Task ClearApiKeyAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
    }
}
