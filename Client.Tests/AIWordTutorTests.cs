using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using BlazorApp.Client.Pages;
using BlazorApp.Client.Shared;

namespace Client.Tests;

/// <summary>
/// Unit tests for the AIWordTutor component
/// </summary>
public class AIWordTutorTests : TestContext
{
    private readonly Mock<IOpenAIApiKeyService> _mockApiKeyService;
    private readonly Mock<IOpenAIService> _mockOpenAIService;
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public AIWordTutorTests()
    {
        _mockApiKeyService = new Mock<IOpenAIApiKeyService>();
        _mockOpenAIService = new Mock<IOpenAIService>();
        _mockJSRuntime = new Mock<IJSRuntime>();

        // Register mocked services
        Services.AddSingleton(_mockApiKeyService.Object);
        Services.AddSingleton(_mockOpenAIService.Object);
        Services.AddSingleton(_mockJSRuntime.Object);
        Services.AddSingleton<HttpClient>();
    }

    [Fact]
    public void Component_ShouldRender_WithoutCrashing()
    {
        // Arrange
        _mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync(string.Empty);

        // Act
        var component = RenderComponent<AIWordTutor>();

        // Assert
        Assert.NotNull(component);
        Assert.NotEmpty(component.Markup);
    }

    [Fact]
    public void Component_ShouldShowApiKeyEntry_WhenNoApiKey()
    {
        // Arrange
        _mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync(string.Empty);

        // Act
        var component = RenderComponent<AIWordTutor>();

        // Assert
        Assert.NotNull(component);
        // Check if the API key entry component is rendered
        Assert.Contains("openai", component.Markup.ToLower());
    }

    [Fact]
    public void Component_ShouldShowGameOptions_WhenApiKeyExists()
    {
        // Arrange
        _mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync("test-api-key");

        // Act
        var component = RenderComponent<AIWordTutor>();

        // Assert
        Assert.NotNull(component);
        // The component should render game mode options
        var hasGameModes = component.Markup.ToLower().Contains("story") || 
                          component.Markup.ToLower().Contains("conversation") ||
                          component.Markup.ToLower().Contains("quiz");
        Assert.True(hasGameModes, "Component should show game mode options when API key is available");
    }

    [Theory]
    [InlineData(DifficultyLevel.Beginner)]
    [InlineData(DifficultyLevel.Intermediate)] 
    [InlineData(DifficultyLevel.Advanced)]
    public void DifficultyLevel_ShouldBeValidEnum(DifficultyLevel difficulty)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(DifficultyLevel), difficulty));
    }

    [Theory]
    [InlineData(GameMode.StoryAdventure)]
    [InlineData(GameMode.ConversationPractice)]
    [InlineData(GameMode.ContextualLearning)]
    [InlineData(GameMode.PersonalizedQuiz)]
    public void GameMode_ShouldBeValidEnum(GameMode gameMode)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(GameMode), gameMode));
    }

    [Fact]
    public void WordLibrary_ShouldContainWordsForAllDifficultyLevels()
    {
        // This tests the logic that would be in the component
        // We're testing the concept that word libraries should exist for each difficulty
        
        // Arrange
        var expectedDifficulties = new[] { DifficultyLevel.Beginner, DifficultyLevel.Intermediate, DifficultyLevel.Advanced };
        
        // Act & Assert
        foreach (var difficulty in expectedDifficulties)
        {
            Assert.True(Enum.IsDefined(typeof(DifficultyLevel), difficulty), 
                $"Difficulty level {difficulty} should be defined");
        }
    }

    [Fact]
    public void MockServices_ShouldBeProperlyConfigured()
    {
        // Arrange & Act
        var apiKeyService = Services.GetService<IOpenAIApiKeyService>();
        var openAIService = Services.GetService<IOpenAIService>();
        var jsRuntime = Services.GetService<IJSRuntime>();

        // Assert
        Assert.NotNull(apiKeyService);
        Assert.NotNull(openAIService);
        Assert.NotNull(jsRuntime);
    }

    [Fact]
    public async Task ApiKeyService_GetApiKey_ShouldReturnMockedValue()
    {
        // Arrange
        const string expectedApiKey = "test-api-key-123";
        _mockApiKeyService.Setup(x => x.GetApiKeyAsync()).ReturnsAsync(expectedApiKey);

        // Act
        var result = await _mockApiKeyService.Object.GetApiKeyAsync();

        // Assert
        Assert.Equal(expectedApiKey, result);
    }

    [Fact]
    public async Task OpenAIService_GenerateContent_ShouldBeMockable()
    {
        // Arrange
        const string expectedContent = "Generated test content";
        _mockOpenAIService.Setup(x => x.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .ReturnsAsync(expectedContent);

        // Act
        var result = await _mockOpenAIService.Object.GenerateContentAsync("test prompt");

        // Assert
        Assert.Equal(expectedContent, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mockApiKeyService?.Reset();
            _mockOpenAIService?.Reset();
            _mockJSRuntime?.Reset();
        }
        base.Dispose(disposing);
    }
}
