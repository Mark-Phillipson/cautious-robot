using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient();

// Register API key services
builder.Services.AddScoped<BlazorApp.Client.Shared.IWordsApiKeyService, BlazorApp.Client.Shared.WordsApiKeyService>();
builder.Services.AddScoped<BlazorApp.Client.Shared.IApiKeyService, BlazorApp.Client.Shared.WordsApiKeyService>();
builder.Services.AddScoped<BlazorApp.Client.Shared.IOpenAIApiKeyService, BlazorApp.Client.Shared.OpenAIApiKeyService>();
builder.Services.AddScoped<BlazorApp.Client.Shared.IOpenAIService, BlazorApp.Client.Shared.OpenAIService>();

await builder.Build().RunAsync();
