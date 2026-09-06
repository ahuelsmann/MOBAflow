# MOBAflow agent instructions

MOBAflow is event-driven model railroad automation on .NET 10. These instructions are tuned for GPT-6 Astra
and apply to coding agents across Windows and Linux.

## Working agreement

- Complete the requested work through implementation, relevant validation, and a concise handoff.
  Make routine, reversible decisions from repository evidence without asking for confirmation.
- Ask only when missing information materially affects correctness or scope and cannot be inferred, or an action
  needs authorization that the user has not already given. Continue independent work while blocked.
- Make the smallest cohesive change that solves the problem. Preserve unrelated user edits and existing contracts;
  avoid incidental cleanup, speculative abstractions, dependency upgrades, and repository-wide formatting.
- Inspect affected code, callers, and tests before editing. Plan briefly for multi-file or risky work; simple fixes
  need no formal plan. Use tools available in the session; no particular planning or diagnostic tool is required.
- Read only the applicable guidance linked in the [instruction index](.github/instructions/instructions-index.md).
  This file owns the repository-wide workflow; specialized instructions add technical detail, not approval gates.
  Explicit user instructions take precedence over repository and skill guidance, within system/tool permissions.
  Resolve stale examples against current code and build configuration; report unresolved material conflicts.
  If an instruction blocks progress, identify its exact file and rule rather than inferring an approval requirement.
- Keep updates and the final response brief, in the user's language. State what changed, what was actually checked,
  and any remaining limitation. English UI and commit-message rules do not constrain conversation language.

## Repository map

Use [Moba.slnx](Moba.slnx) and the affected project files to identify the build graph.
Do not assume every directory is an active project or discover projects inside `.nuget`, `bin`, or `obj`.

| Area | Responsibility |
| --- | --- |
| `Domain/` | Serializable models, enums, workflow payloads and domain rules |
| `Common/` | Configuration, events, runtime snapshots, discovery, paths and platform-neutral state |
| `Backend/` | Z21 transport/protocol, runtime, journey managers and workflow action handlers |
| `SharedUI/` | Shared MVVM state, commands, runtime projection and UI service interfaces |
| `MOBAflow/` | WinUI 3 desktop UI and Windows service implementations |
| `MOBAsmart/` | Android MAUI UI, local/remote runtime coordination and mobile services |
| `MOBApi/` | ASP.NET Core REST and SignalR host (default REST port 5001) |
| `MOBAdisplay/` | Display rendering/transport; ESP32-S3 firmware in `esp32/` |
| `Sound/`, `TrackLibrary.Base/`, `TrackLibrary.PikoA/`, `TrackPlan.Renderer/` | Audio, track catalogues and rendering |
| `Test/` | NUnit tests, Moq, test doubles and fixtures; `Analysis/` has separate helper projects |
| `docs/`, `scripts/`, `.vscode/tasks.json` | Architecture/user docs and development workflows |
| `.github/workflows/`, `.azure-pipelines/` | Checked-in CI and release definitions |

## Architecture constraints

- Keep `Domain`, `Common`, `Backend`, and shared UI logic free of WinUI/MAUI types. Adapt OS-specific behavior in
  platform services; use existing I/O interfaces and constructor injection. Follow existing service lifetimes.
- Shared ViewModels use `IMobaRuntime` and, where present, `IRuntimeCommandGateway`. Preserve local/remote routing;
  do not reintroduce `IMobaClient` as the shared runtime boundary or duplicate runtime ownership in a ViewModel.
- Z21 publishes from background work. UI hosts register `AddEventBusWithUiDispatch()`, whose
  `SharedUI/Service/UiThreadEventBusDecorator.cs` marshals publishing to the UI thread. EventBus handlers using
  that decorated bus update UI state directly. Do not add dispatcher calls there. Raw Z21 callbacks, timers and
  other callbacks outside that bus are separate threading boundaries; inspect their actual execution context.
- Use asynchronous calls with `await`; do not block tasks with `.Result`, `.Wait()` or `GetAwaiter().GetResult()`.
  Preserve cancellation and error handling. Do not apply `ConfigureAwait(false)` to UI continuations that need UI access.
- Keep feature behavior in ViewModel commands/services. Code-behind may coordinate view lifecycle and visuals;
  UserControls forward input through `ICommand`. Use the existing dialog/navigation interfaces.
- Use CommunityToolkit observable properties and relay commands for new state/commands. Preserve model-wrapper
  `SetProperty` and nested `PropertyChanged` propagation required by auto-save and computed properties.
- Keep theme-dependent WinUI brushes in `ThemeResource`; use existing MAUI theme resources/bindings on Android.
  Domain/display color values (for example ARGB LED pixels) are data, adapted to brushes at the UI boundary.
- Keep user-visible UI text, domain defaults and shipped master/sample data in English. TTS/announcement content
  and voice remain user-configurable. Do not translate user-authored content as part of unrelated edits.
- Persist resizable star columns as `*ColumnStarValue`; use pixels only for intentionally fixed columns.
  When changing XAML files, ensure active pages remain included in XAML compilation; do not hide compiler errors
  by adding `<Page Remove="..."/>` for an active page.
- Preserve config defaults, serialized compatibility and safe locomotive startup (speed zero, no restored movement).
  Reuse `PhotoPathHelper`, `DiscoveryResponseParser`, and `MasterDataStore` instead of duplicating their logic.

For runtime ownership, DI entry points and regression tests, see
[architecture.instructions.md](.github/instructions/architecture.instructions.md).

## Build and validation

Read `global.json`, `Directory.Build.props`, `Directory.Packages.props` and the affected `.csproj` when build or
package behavior matters. `global.json` is the SDK source of truth; do not assume an SDK installation path or
installed workloads. NuGet versions are centrally managed. Follow `.editorconfig` and `.gitattributes`.

Run commands from the repository root. Restore/build named projects, not every `.csproj` or the entire solution
unless the task requires it and the environment supports its Windows and Android targets.

### Choose checks by change

| Change | Required evidence |
| --- | --- |
| Documentation/instructions only | Review diff, links, referenced paths/commands and instruction consistency; no .NET build/tests |
| Behavior in shared logic | Add/update meaningful regression tests as needed, run affected tests and build affected consumers |
| Refactor without behavior change | Existing relevant tests and compile checks; new tests only for an uncovered risk |
| WinUI/MAUI UI | Compile the affected app; test changed shared behavior; inspect affected states in Light and Dark themes |
| Runtime, EventBus, DI, persistence, protocol or build graph | Cover affected boundaries and broaden to the relevant full target suite/CI checks |

A filtered run must discover and execute the intended tests. Passing zero tests is not validation.
Once checks pass, repeat or broaden them only for new edits, failures or unresolved risk. Do not add tests that
merely duplicate implementation or assert documentation text. Do not weaken tests/analyzers to make a change pass.

### Portable .NET work (Windows or Linux)

```text
dotnet build MOBApi/MOBApi.csproj
dotnet build MOBAdisplay/MOBAdisplay.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

Select the affected build target; the two build commands are examples, not a mandatory pair.
For focused tests, append `--filter "FullyQualifiedName~ActualFixtureName"` using a fixture found in the repo.
`Test/Test.csproj` also targets Windows: `-p:TargetFrameworks=net10.0` limits its restore graph for portable runs.
Android tests are excluded by default (`IncludeMobaSmartTests=false`). Windows SAPI/audio tests can be unavailable
on Linux/headless machines; report exact failures or skips rather than assuming every failure is environmental.

### Windows desktop

```text
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
```

FastDebug is a compile check that skips API build/copy and some checks. For host integration, packaging or release
changes, build with the normal dependencies and use the Release checks in
[GitHub quality CI](.github/workflows/quality.yml) or [Azure quality CI](.azure-pipelines/quality.yml), as applicable.
Run/watch the desktop app only when needed for the task. Check optional tooling such as `winapp` before relying on it.

### Android (with MAUI/Android workloads)

```text
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

The app currently has one Android target. `dotnet restore -f` means force, not framework selection.
For an authorized device deployment, add `-p:MobaReliableDeploy=true -t:Run` to the Android build if needed.
ESP32 firmware has its own `MOBAdisplay/esp32/platformio.ini`; a .NET build does not validate firmware.
Use fakes for automated railroad tests. Live track power, locomotive movement and firmware flashing require a
hardware task that authorizes those actions; a code-validation request alone is not such authorization.

### Completion

Review the final diff for unintended edits and architecture regressions. Update existing documentation when the
change affects its accuracy; keep durable project knowledge here and session progress in the conversation.
Azure DevOps project MOBAflow tracks open work; consult/update a linked work item when the task calls for it,
without making tracker access a prerequisite for independent local work.
When committing is requested, use English Conventional Commits. Report the commands/checks run and their outcomes;
identify unavailable platform/hardware checks and the smallest remaining check without claiming they passed.

## Guidance sources

Reviewed against official OpenAI guidance on 2026-09-06. These are the basis for the workflow above, not required
reading for each coding task:

- [GPT-6 Astra prompting guidance](https://developers.openai.com/api/docs/guides/latest-model)
- [Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [AGENTS.md discovery and scope](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
