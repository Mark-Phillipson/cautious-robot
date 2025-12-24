Upgrade `Client` to .NET 10 — summary of changes

What I changed:
- Updated `Client.csproj` TargetFramework: `net9.0` → `net10.0` and bumped package references to 10.x where available.
- Removed explicit `System.Text.Json` package reference (use runtime-provided version).
- Added `global.json` to pin SDK to `10.0.100` (rollForward: latestFeature).
- Updated `.github/workflows/azure-static-web-apps.yml` to use `dotnet-version: '10.0.x'`.
- Updated `Client.Tests/Client.Tests.csproj` to target `net10.0` and bumped `Microsoft.Extensions.DependencyInjection` to `10.0.1`.
- Ran local builds and tests. `dotnet build` succeeds and `dotnet test` shows 66 passed, 2 failed.

Next steps (recommended before merge):
1. Investigate and fix the 2 failing tests (see test output in CI/local run).
2. Add/confirm `global.json` in repository root (if desired for all projects).
3. Run CI and verify GitHub Actions succeed on branch.
4. Consider adding a smoke Playwright test for critical UI flows.

Suggested commit message:
"chore: upgrade Client to .NET 10; update packages and CI; add global.json"

If you'd like, I can draft the PR description and suggested reviewers, or I can open the PR for you if you want me to proceed (I can prepare branch/commit instructions next).