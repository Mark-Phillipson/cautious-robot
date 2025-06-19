# Words Game Client

A Blazor WebAssembly application featuring educational word games and AI-powered learning tools.

## Development Guidelines

For comprehensive development guidelines, coding standards, and build instructions, see:
**[.github/copilot-instructions.md](.github/copilot-instructions.md)**

## Quick Start

### Building the Project
Always use VS Code tasks for builds:
```
Ctrl+Shift+P → Tasks: Run Task → "dotnet build"
```

### Running the Project
```
Ctrl+Shift+P → Tasks: Run Task → "dotnet watch"
```

### Running Tests
```
Ctrl+Shift+P → Tasks: Run Task → "test: run all tests"
```

## Key Features

- **Word Games**: Interactive vocabulary games (Hangman, Codele, Scrabble, Definitions)
- **AI Word Tutor**: Conversation practice with OpenAI integration
- **Responsive Design**: Works on desktop and mobile devices
- **Audio Support**: Sound effects for game interactions
- **Progressive Web App**: Installable as a mobile app

## Technology Stack

- **Framework**: Blazor WebAssembly (.NET 9.0)
- **Hosting**: Microsoft Azure Static Web Apps
- **Styling**: CSS with Bootstrap + component isolation
- **AI Integration**: OpenAI API for conversation practice

## Project Structure

```
├── Pages/           # Routable Blazor pages (.razor + .razor.cs)
├── Shared/          # Reusable components and services
├── Models/          # Data models and game logic
├── wwwroot/         # Static assets (CSS, JS, images, audio, data)
└── .github/         # Development guidelines and workflows
```

---

For detailed development instructions, patterns, and best practices, refer to the comprehensive guidelines in `.github/copilot-instructions.md`.
