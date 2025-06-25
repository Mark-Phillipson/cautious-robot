# GitHub Copilot Instructions

## ⚠️ CRITICAL: Build System Priority Rules

### Always Use VS Code Tasks for Builds
**When working in this project, ALWAYS use VS Code tasks for builds, runs, and tests:**

- ✅ **Use**: `run_vs_code_task` tool with appropriate task ID
- ❌ **Never**: `run_in_terminal` for build operations (unless explicitly requested by user)

### Available Tasks
- **Build**: `shell: dotnet build`
- **Run**: `shell: dotnet run` 
- **Watch**: `shell: dotnet watch`
- **Test All**: `shell: test: run all tests`
- **Test Coverage**: `shell: test: run with coverage`
- **Test Verbose**: `shell: test: run verbose`

### Task Usage Example
```typescript
// ✅ Correct way to build
await run_vs_code_task({
  id: "shell: dotnet build",
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});
```

## Project Overview
This is a Blazor WebAssembly (WASM) static web application hosted on Microsoft Azure Static Web Apps. The application includes word games and educational content, built with .NET 9.0.

## Technology Stack
- **Framework**: Blazor WebAssembly (.NET 9.0)
- **Hosting**: Microsoft Azure Static Web Apps
- **Frontend**: Blazor components with Razor syntax
- **Styling**: CSS with Bootstrap
- **Audio**: HTML5 audio elements for sound effects
- **Data**: JSON files for word libraries and game data

## Project Structure Guidelines

### Component Organization
- **Pages/**: Contains routable Blazor pages (.razor files with @page directive)
- **Shared/**: Contains reusable components and layouts
- **Models/**: Data models and game logic classes
- **wwwroot/**: Static assets (CSS, JS, images, audio, JSON data)

### File Naming Conventions
- Blazor components: PascalCase (e.g., `WordsGame.razor`)
- Code-behind files: Component name + `.razor.cs` (e.g., `WordsGame.razor.cs`)
- CSS files: Component name + `.razor.css` (e.g., `WordsGame.razor.css`)
- Models: PascalCase (e.g., `GameOptions.cs`, `WordResult.cs`)
- Do not put styles inside the Blazor component always use a separate `.razor.css` file for styles
## Coding Standards

### Blazor Components
- Use component isolation CSS files (`.razor.css`) for component-specific styling
- Implement IDisposable when components need cleanup
- Do Not Use `@code` blocks for component logic, instead separate code-behind files for components
- Apply `@page` directive for routable components
- Use proper parameter binding with `[Parameter]` attributes

### Debugging Best Practices
- Add `Console.WriteLine()` for debugging complex logic
- Include debug panels in UI for development features
- Create test methods for complex algorithms (like word detection)
- Force `StateHasChanged()` after important state updates
- Use debug buttons for manual testing during development

### C# Code Style
- Follow Microsoft C# coding conventions
- Use nullable reference types where appropriate
- Implement async/await patterns for I/O operations
- Use dependency injection for services
- Apply proper error handling and validation

### CSS and Styling
- Use CSS Grid and Flexbox for layouts
- Follow BEM methodology for CSS class naming when not using component isolation
- Ensure responsive design with mobile-first approach
- Maintain consistent spacing and typography scales

## Azure Static Web Apps Considerations

### Configuration
- Use `staticwebapp.config.json` for routing and security rules
- Configure fallback routes for SPA behavior
- Set up proper MIME types for static assets

### Performance Optimization
- Minimize bundle sizes by using lazy loading
- Optimize images and audio files
- Use compression for static assets
- Implement proper caching strategies

### Deployment
- Build artifacts go to `bin/publish/` directory
- Ensure `wwwroot` contains all necessary static files
- Use `web.config` for IIS-specific configurations if needed

## Game Development Patterns

### State Management
- Use component parameters for parent-child communication
- Implement proper game state management
- Use events for component communication when needed
- Store game data in appropriate models

### Audio Integration
- Use HTML5 audio elements for sound effects
- Implement proper audio loading and error handling
- Provide mute/unmute functionality
- Consider accessibility for audio features

### AI-Powered Learning Features
- Implement robust word detection algorithms for conversation practice
- Use fallback logic when AI services are unavailable
- Create encouraging evaluation prompts that support learning
- Include debug features for complex scoring systems
- Handle API key availability gracefully with meaningful fallbacks

### Data Handling
- Load game data from JSON files in `wwwroot/sample-data/`
- Implement proper error handling for data loading
- Use appropriate data models for type safety
- Consider caching strategies for large datasets

## Security Best Practices
- Validate all user inputs
- Sanitize data before rendering
- Use HTTPS for all communications
- Follow OWASP guidelines for web application security

## Accessibility Guidelines
- Provide proper ARIA labels and roles
- Ensure keyboard navigation support for both voice and keyboard only usesrs
- Maintain sufficient color contrast ratios
- Include alternative text for images
- Support screen readers with semantic HTML

## Testing Recommendations
- Write unit tests for game logic and models
- Test component rendering and user interactions
- Validate responsive design across devices
- Test audio functionality across browsers
- Verify accessibility compliance

## Performance Considerations
- Use `@rendermode` appropriately for different scenarios
- Implement virtual scrolling for large lists
- Optimize image and audio asset sizes
- Use lazy loading for non-critical components
- Monitor bundle size and loading times

## Common Patterns to Follow

### Component Structure
```csharp
@page "/example"
@using Models
@inject IJSRuntime JSRuntime

<div class="example-container">
    <!-- Component markup -->
</div>

@code {
    [Parameter] public string? Title { get; set; }
    
    private GameOptions gameOptions = new();
    
    protected override async Task OnInitializedAsync()
    {
        // Initialization logic
    }
}
```

### Service Registration
```csharp
builder.Services.AddScoped<IGameService, GameService>();
```

### Error Handling
```csharp
try
{
    // Game logic
}
catch (Exception ex)
{
    // Log and handle errors appropriately
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Debugging and Development
- Use browser developer tools for client-side debugging
- Leverage Visual Studio debugging for C# code
- Monitor network requests for data loading issues

## Documentation Standards
- Document public APIs and complex game logic
- Include XML documentation comments for methods
- Maintain README files for setup instructions
- Document deployment and configuration processes

---

When working on this project, prioritize user experience, performance, and maintainability. Follow these guidelines to ensure consistency and quality across the codebase.
