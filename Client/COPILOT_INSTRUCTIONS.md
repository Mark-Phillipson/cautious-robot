# GitHub Copilot Development Instructions

## Build System Priority Rules

### ⚠️ CRITICAL: Always Use VS Code Tasks
**When working in this project, ALWAYS use VS Code tasks for builds, runs, and tests:**

- ✅ **Use**: `run_vs_code_task` tool with appropriate task ID
- ❌ **Never**: `run_in_terminal` for build operations (unless explicitly requested)

### Available Tasks
```json
{
  "build": "shell: dotnet build",
  "run": "shell: dotnet run", 
  "watch": "shell: dotnet watch",
  "test-all": "shell: test: run all tests",
  "test-coverage": "shell: test: run with coverage",
  "test-verbose": "shell: test: run verbose"
}
```

### Task Usage Examples
```typescript
// ✅ Correct way to build
await run_vs_code_task({
  id: "shell: dotnet build",
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});

// ✅ Correct way to run with watch
await run_vs_code_task({
  id: "shell: dotnet watch", 
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});

// ❌ Incorrect - avoid unless specifically requested
await run_in_terminal({
  command: "dotnet build",
  explanation: "Building project",
  isBackground: false
});
```

## Project-Specific Guidelines

### Blazor Component Structure
- **Separate Code-Behind**: Always use `.razor.cs` files instead of `@code` blocks
- **Component Isolation CSS**: Use `.razor.css` files for component-specific styling
- **State Management**: Call `StateHasChanged()` after important state updates

### Debugging Best Practices
- Add `Console.WriteLine()` for debugging complex logic
- Include debug panels in UI for development features
- Create test methods for complex algorithms (like word detection)

### Error Handling
- Use try-catch blocks for external API calls
- Implement fallback logic for AI services
- Provide meaningful error messages to users

### Performance Considerations
- Use `async/await` for I/O operations
- Implement proper disposal with `IDisposable`
- Optimize bundle sizes with lazy loading

## Development Workflow

1. **Before Making Changes**: Run build task to ensure clean state
2. **After Code Changes**: Run build task to check for compilation errors  
3. **For Testing**: Use appropriate test tasks
4. **For Development**: Use watch task for live reload

## Example Development Session

```typescript
// 1. Start development session
await run_vs_code_task({
  id: "shell: dotnet build",
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});

// 2. Make code changes using edit tools
await replace_string_in_file({...});

// 3. Verify changes compile
await run_vs_code_task({
  id: "shell: dotnet build", 
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});

// 4. Run tests if applicable
await run_vs_code_task({
  id: "shell: test: run all tests",
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});

// 5. Start watch mode for testing
await run_vs_code_task({
  id: "shell: dotnet watch",
  workspaceFolder: "c:\\Users\\MPhil\\source\\repos\\Words\\cautious-robot\\Client"
});
```

## Why Use VS Code Tasks?

1. **Consistency**: Same commands every time
2. **Problem Matching**: Automatic error detection and reporting
3. **Integration**: Better VS Code integration and output handling
4. **Configuration**: Predefined settings and working directories
5. **Background Processes**: Proper handling of long-running tasks like watch mode

---

**Remember**: The user specifically requested this workflow, so always follow these guidelines unless explicitly told otherwise.
