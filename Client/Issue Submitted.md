Summarized conversation historyI can help you draft a detailed GitHub issue for the duplicate 'TargetFrameworkAttribute' errors in .NET 9.0 Blazor projects. Since I can't directly create GitHub issues, I'll provide you with the complete issue content that you can submit. Here's the draft:

Title: Duplicate TargetFrameworkAttribute errors in .NET 9.0 Blazor projects with VS Code

### Description
When working with .NET 9.0 Blazor WebAssembly projects in Visual Studio Code (both regular and Insiders versions), duplicate 'TargetFrameworkAttribute' errors are appearing during build, despite having a correct project configuration.

### Environment
- .NET Version: 9.0.5
- Project Type: Blazor WebAssembly
- IDE: Visual Studio Code with C# Dev Kit
- Operating System: Windows
- Project SDK: Microsoft.NET.Sdk.BlazorWebAssembly

### Project Configuration
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>BlazorApp.Client</RootNamespace>
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="9.0.5" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="9.0.5" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Steps to Reproduce
1. Create a new Blazor WebAssembly project targeting .NET 9.0
2. Open the project in VS Code
3. Build the project
4. Observe build errors in the Problems panel

### Current Behavior
The build process generates duplicate 'TargetFrameworkAttribute' errors:
```
Duplicate 'System.Runtime.Versioning.TargetFrameworkAttribute' attribute
```

### Expected Behavior
The project should build without duplicate attribute errors, as the project file configuration is correct and follows the standard template structure.

### Additional Information
- The issue persists even after:
  - Cleaning and rebuilding the solution
  - Deleting bin/obj folders
  - Restoring NuGet packages
  - Using different versions of VS Code
- This appears to be specific to .NET 9.0 Blazor projects when using VS Code
- The same project builds correctly in Visual Studio 2022

### Workarounds Attempted
- Manually deleting the generated AssemblyAttributes.cs file (regenerates with same issue)
- Adding `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` (impacts other necessary assembly attributes)
- Various combinations of project file settings related to assembly info generation

Would you like me to add or modify anything in this issue template before you submit it?

Similar code found with 1 license type
