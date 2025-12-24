## Plan: Upgrade to .NET 10 ✅

**Status:** In progress — `Client` updated to `net10.0`, packages bumped, `global.json` added, and CI workflow updated; client builds locally. Next: update test projects to `net10.0`, run CI, and finalize PR.

**Note:** I ran the test suite locally — 66 passed, 2 failed. I created a follow-up todo to investigate and fix the failing tests (`AIWordTutorLogicTests.GameMode` and `AIWordTutorIntegrationTests.Component_ShouldRenderDifficultyOptions`).

**TL;DR —** Upgrade `Client` from **`net9.0` → `net10.0`**, update package references and CI to use .NET 10, run builds/tests, and resolve any serialization/JS-interop breakages. This minimizes risk by doing atomic commits, validating locally with VS Code tasks, and running CI before merging.

---

### Steps 🔧

1. **Create branch** `upgrade/net10` and tag current state (`vpre-net10`).
2. **Edit** `Client.csproj`: change `<TargetFramework>` to `net10.0` and bump relevant `PackageReference` versions.
3. **Update CI** workflow(s) in `.github/workflows/*` to `dotnet-version: '10.0.x'` and add `dotnet workload install wasm-tools` if using AOT.
4. **Build & test** locally using VS Code tasks (`shell: dotnet build`, `shell: test: run all tests`).
5. **Validate** in browser (app boot, console logs, JS interop, service worker, OpenAI features).
6. **Fix** compile/runtime errors (serialization policies, API changes), push branch, open PR and run CI smoke tests.

---

### Further Considerations 💡

- Add `global.json` to lock SDK (`10.0.x`) for CI/dev parity.
- Add unit tests for JSON serialization and mocked `OpenAIService` before merging.
- If enabling WASM AOT, run cross-browser validation and add `wasm-tools` to CI.

---

### Key hotspots to inspect first ⚠️

- `Program.cs` (hosting lifecycle and builder changes)
- JSON serialization usages (System.Text.Json policies, OpenAI payloads)
- JS interop calls (IJSRuntime / InvokeAsync usage)
- `wwwroot/service-worker.*` and publish layout
- Packages pinned to 9.x — update to 10.x equivalents

---

### Commands (recommended)

- Use VS Code tasks (preferred):
  - `shell: dotnet build`
  - `shell: test: run all tests`
  - `shell: dotnet run` or `shell: dotnet watch`
- CLI equivalents:
  - `dotnet restore Client/Client.csproj`
  - `dotnet build -c Release`
  - `dotnet test`
  - `dotnet publish -c Release -o ./publish`
  - `dotnet run`

---

### Rollback & Release

- If major issues: revert branch or reset to pre-upgrade tag. Options:
  - `git revert` or reset branch to previous commit
  - Restore old deployment or re-target workflow to prior successful build
- Once stable: merge to `main` and tag (e.g., `vnet10`).

---

### Estimated Effort & Risk ⏱️

- **Quick compile & smoke test:** 2–4 hours
- **Fixing serialization/interop and adding tests:** 1–2 days
- **Full validation (AOT, browser matrix, CI fine-tuning):** 2–3 days

**Risk level:** Low–Medium — most upgrades are straightforward; higher risk if serialization or JS interop breaks and because the repo currently lacks sufficient tests.

---

### Files & symbols to inspect/change 📋

- `Client.csproj` — `<TargetFramework>` and `<PackageReference>` entries
- `.github/workflows/*` — update `dotnet-version`
- `Program.cs` — hosting and builder usage
- `OpenAIService.cs` / `OpenAIApiKeyService.cs` — JSON payloads and HTTP usage
- `wwwroot/service-worker.*` — caching & routing behavior

---

### Optional follow-ups

- Add a CI smoke test with Playwright for critical flows (OpenAI calls, JS interop, basic UI flows).
- Add `global.json` to pin SDK version and document local setup steps.

---

Would you like me to also draft the exact PR checklist and commit messages (with concrete diffs) to implement this plan? 🔁