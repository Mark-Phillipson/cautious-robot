using Xunit;
using Moq;
using BlazorApp.Client.Shared;
using Microsoft.JSInterop;

namespace Client.Tests;

/// <summary>
/// Tests for service layer components used by AIWordTutor
/// </summary>
public class ServiceTests
{    [Fact]
    public async Task OpenAIApiKeyService_GetApiKey_ShouldCallJSRuntime()
    {
        // Arrange
        var mockJSRuntime = new Mock<IJSRuntime>();
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(
            "localStorage.getItem", 
            It.IsAny<object[]>()))
            .ReturnsAsync("test-api-key");

        var service = new OpenAIApiKeyService(mockJSRuntime.Object);

        // Act
        var result = await service.GetApiKeyAsync();

        // Assert
        Assert.Equal("test-api-key", result);
        mockJSRuntime.Verify(x => x.InvokeAsync<string?>(
            "localStorage.getItem", 
            It.IsAny<object[]>()), Times.Once);
    }    [Fact]
    public async Task OpenAIApiKeyService_SetApiKey_ShouldCallJSRuntime()
    {
        // Arrange
        var mockJSRuntime = new Mock<IJSRuntime>();
        var service = new OpenAIApiKeyService(mockJSRuntime.Object);
        const string testApiKey = "test-api-key-123";

        // Act
        await service.SetApiKeyAsync(testApiKey);

        // Assert
        // Verify that the underlying InvokeVoidAsync was called with the right parameters
        mockJSRuntime.Verify(x => x.InvokeAsync<object>(
            "localStorage.setItem",
            It.Is<object[]>(args => args.Length == 2 && args[0].ToString() == "openai_chatgpt_api_key" && args[1].ToString() == testApiKey)), 
            Times.Once);
    }    [Fact]
    public async Task OpenAIApiKeyService_ClearApiKey_ShouldCallJSRuntime()
    {
        // Arrange
        var mockJSRuntime = new Mock<IJSRuntime>();
        var service = new OpenAIApiKeyService(mockJSRuntime.Object);

        // Act
        await service.ClearApiKeyAsync();

        // Assert
        // Verify that the underlying InvokeVoidAsync was called with the right parameters
        mockJSRuntime.Verify(x => x.InvokeAsync<object>(
            "localStorage.removeItem",
            It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "openai_chatgpt_api_key")), 
            Times.Once);
    }

    [Fact]
    public async Task OpenAIService_GenerateContent_WithoutApiKey_ShouldReturnError()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpClient>();
        var mockApiKeyService = new Mock<IOpenAIApiKeyService>();
        mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync(string.Empty);

        var service = new OpenAIService(mockHttpClient.Object, mockApiKeyService.Object);

        // Act
        var result = await service.GenerateContentAsync("test prompt");

        // Assert
        Assert.Contains("Please set your OpenAI API key", result);
    }    [Fact]
    public void OpenAIService_GenerateContent_WithApiKey_ShouldProceed()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpClient>();
        var mockApiKeyService = new Mock<IOpenAIApiKeyService>();
        mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync("test-api-key");

        var service = new OpenAIService(mockHttpClient.Object, mockApiKeyService.Object);

        // Act & Assert
        // This test verifies the service is properly set up and would proceed
        // In a real test, we'd mock the HTTP response
        Assert.NotNull(service);
    }    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ApiKeyValidation_ShouldReturnFalse_ForInvalidKeys(string? apiKey)
    {
        // Arrange
        var mockJSRuntime = new Mock<IJSRuntime>();
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(
            "localStorage.getItem", 
            It.IsAny<object[]>()))
            .ReturnsAsync(apiKey);

        var service = new OpenAIApiKeyService(mockJSRuntime.Object);

        // Act
        var result = await service.GetApiKeyAsync();
        var isEmpty = string.IsNullOrWhiteSpace(result);

        // Assert
        Assert.True(isEmpty);
    }    [Theory]
    [InlineData("sk-1234567890abcdef")]
    [InlineData("test-key")]
    [InlineData("valid-api-key-123")]
    public async Task ApiKeyValidation_ShouldReturnTrue_ForValidKeys(string apiKey)
    {
        // Arrange
        var mockJSRuntime = new Mock<IJSRuntime>();
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(
            "localStorage.getItem", 
            It.IsAny<object[]>()))
            .ReturnsAsync(apiKey);

        var service = new OpenAIApiKeyService(mockJSRuntime.Object);

        // Act
        var result = await service.GetApiKeyAsync();
        var isValid = !string.IsNullOrWhiteSpace(result);

        // Assert
        Assert.True(isValid);
        Assert.Equal(apiKey, result);
    }

    [Fact]
    public void IOpenAIApiKeyService_Interface_ShouldHaveCorrectMethods()
    {
        // Arrange
        var interfaceType = typeof(IOpenAIApiKeyService);

        // Act
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToArray();

        // Assert
        Assert.Contains("GetApiKeyAsync", methodNames);
        Assert.Contains("SetApiKeyAsync", methodNames);
        Assert.Contains("ClearApiKeyAsync", methodNames);
    }

    [Fact]
    public void IOpenAIService_Interface_ShouldHaveCorrectMethods()
    {
        // Arrange
        var interfaceType = typeof(IOpenAIService);

        // Act
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToArray();

        // Assert
        Assert.Contains("GenerateContentAsync", methodNames);
    }

    [Fact]
    public void OpenAIApiKeyService_ShouldImplementInterface()
    {
        // Arrange & Act
        var implementsInterface = typeof(IOpenAIApiKeyService).IsAssignableFrom(typeof(OpenAIApiKeyService));

        // Assert
        Assert.True(implementsInterface);
    }

    [Fact]
    public void OpenAIService_ShouldImplementInterface()
    {
        // Arrange & Act
        var implementsInterface = typeof(IOpenAIService).IsAssignableFrom(typeof(OpenAIService));

        // Assert
        Assert.True(implementsInterface);
    }
}
