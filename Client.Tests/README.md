# AIWordTutor Tests

This test project contains comprehensive unit and integration tests for the AIWordTutor Blazor component.

## Test Structure

### Test Files

1. **AIWordTutorTests.cs** - Main component rendering and basic functionality tests
2. **AIWordTutorLogicTests.cs** - Business logic and game mechanics tests
3. **ServiceTests.cs** - Service layer tests (OpenAI API, storage services)
4. **AIWordTutorIntegrationTests.cs** - Integration tests with complex scenarios

### Test Categories

- **Unit Tests**: Test individual methods and properties
- **Integration Tests**: Test component behavior with mocked services
- **Logic Tests**: Test business logic and game mechanics
- **Service Tests**: Test service layer components

## Running Tests

### Run All Tests
```powershell
dotnet test
```

### Run with Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test File
```powershell
dotnet test --filter "ClassName=AIWordTutorTests"
```

### Run Tests by Category
```powershell
# Run only unit tests
dotnet test --filter "TestCategory=Unit"

# Run only integration tests  
dotnet test --filter "TestCategory=Integration"
```

## Test Framework and Tools

- **XUnit**: Testing framework
- **bUnit**: Blazor component testing
- **Moq**: Mocking framework
- **Microsoft.Extensions.DependencyInjection**: Service injection for tests

## Mocked Services

The tests use mocked versions of:

- `IOpenAIApiKeyService` - API key management
- `IOpenAIService` - OpenAI API interactions
- `IJSRuntime` - JavaScript interop
- `HttpClient` - HTTP communications

## Test Coverage Areas

### Component Rendering
- ✅ Renders without crashing
- ✅ Shows API key entry when no key present
- ✅ Shows game interface when API key exists
- ✅ Displays difficulty options
- ✅ Displays game mode options

### Business Logic
- ✅ Difficulty level enumeration
- ✅ Game mode enumeration
- ✅ Score calculation logic
- ✅ Word selection validation
- ✅ Game state management

### Service Layer
- ✅ API key storage and retrieval
- ✅ OpenAI service interaction
- ✅ Error handling for missing API keys
- ✅ Service interface implementations

### Integration Scenarios
- ✅ Component behavior with different API key states
- ✅ Service injection and dependency resolution
- ✅ Multiple re-renders stability
- ✅ Async operation handling

## Adding New Tests

When adding new tests:

1. Choose the appropriate test file based on what you're testing
2. Follow the AAA pattern (Arrange, Act, Assert)
3. Use descriptive test names that explain the scenario
4. Include both positive and negative test cases
5. Mock external dependencies appropriately

### Example Test Structure

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var mockService = new Mock<IService>();
    mockService.Setup(x => x.Method()).Returns(expectedValue);

    // Act
    var result = systemUnderTest.DoSomething();

    // Assert
    Assert.Equal(expectedValue, result);
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines and should:

- Run quickly (under 30 seconds total)
- Be deterministic (no flaky tests)
- Provide clear failure messages
- Have good coverage of critical paths

## Troubleshooting

### Common Issues

1. **Service not registered**: Ensure all required services are registered in test setup
2. **Async operations**: Use proper async/await patterns in tests
3. **Component not rendering**: Check that all dependencies are mocked properly

### Debug Tips

- Use `component.Markup` to inspect rendered HTML
- Add `Console.WriteLine()` statements for debugging
- Use breakpoints in test methods
- Check service setup in test constructors
