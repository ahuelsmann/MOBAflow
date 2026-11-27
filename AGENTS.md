# MOBAflow agent instructions

MOBAflow is event-driven model railroad automation on .NET 10. These instructions apply to every coding agent
on Windows and Linux.

## Working agreement

- Complete the requested work through implementation, relevant validation, and a concise handoff.
  Make routine, reversible decisions from repository evidence without asking for confirmation.
- Ask only when missing information materially affects correctness or scope and cannot be inferred, or an action
  needs authorization that the user has not already given. Continue other slices on the Kanban while blocked.
- Make the smallest cohesive change that solves the problem. Preserve unrelated user edits and existing contracts;
  avoid incidental cleanup, speculative abstractions, dependency upgrades, and repository-wide formatting.
  Ask before adding mechanics the request does not name, such as runtime lifecycle commands, chaining/follow-up
  behavior, per-run state or deferred-apply logic.
- Inspect affected code, callers, and tests before editing. Plan briefly for multi-file or risky work; simple fixes
  need no formal plan. Use tools available in the session; no particular planning or diagnostic tool is required.
- Read only the applicable guidance linked in the [instruction index](.github/instructions/instructions-index.md).
  This file owns the repository-wide workflow; specialized instructions supply the linked project policies and technical detail.
  Explicit user instructions take precedence over repository and skill guidance, within system/tool permissions.
  Resolve stale examples against current code and build configuration; report unresolved material conflicts.
  If an instruction blocks progress, identify its exact file and rule rather than inferring an approval requirement.
- Keep updates and the final response brief, in the user's language. State what changed, what was actually checked,
  and any remaining limitation. English UI and commit-message rules do not constrain conversation language.
  Write answers for junior developers: explain unfamiliar terms briefly and make necessary next steps explicit.

## Master coordination: small batches on main

Since 2026-11-16 every agent (Claude Code, ChatGPT Codex, Antigravity) follows the delivery flow in
[master coordination](.github/instructions/master-coordination.instructions.md) (updated on 2026-11-27 to the PR-free Master-Merge workflow):

- **Role != single model.** Roles define domain responsibilities and draw on four capability profiles across Claude
  Code (`P1` Opus 5.5 ceiling, `P4` Sonnet 5.5; `Fable 5.2` prohibited), ChatGPT Codex (`P2` Sol 6.2 / GPT-6 Astra) and Antigravity (`P3` Gemini 4 Carbon / Neon,
  `P4` Hydrogen, plus built-in failover models `P1` `Claude Opus 5.5`, `P4` `Claude Sonnet 5.5`, `P2` `GPT-OSS 120B`). Slices are classified by risk (`H` safety/runtime/protocol, `M` features/UI/audio, `L` docs/small
  non-behavioral fixes); review-fix rounds retain their slice's risk class and dependency updates take the class of
  the affected area.
- **Small slices.** Work is cut into slices of one concern, about 25 files or 400 changed lines at most, each developed in an isolated worktree and task branch that lands on its own via coordinated Master merge onto `main`. An issue may need several slices; no long-running branches.
- **Board, locks, WIP limit.** The COO (Claude; during a verified `Claude Code` `Quota Pause`, `Antigravity` `P3` serves as sole single-writer **`Acting COO`** under `2/3 Acting COO` governance with `Codex` `P2` retaining CPM/TD veto) coordinates open work on the [MOBAflow GitHub Kanban](docs/developer/KANBAN.md)
  and cross-tool `SwarmBus` locks (`bus_locks`). Refactoring and feature work never run in the same locked area at once. At most three code slices (docs-only excluded) are open, at most two local agents build at once.
  One writer per branch.
- **Design first.** Before code, the developer posts a 10-20 line design note (files, approach, risks, tests) in the issue or SwarmBus; code
  starts after cross-model companion/UX review and the COO's (or Acting COO's / Solution Architect's) Go (class `L` slices and
  review-fix rounds on an approved design state their intent in the commit/SwarmBus message instead, while keeping their
  risk class). New features write their acceptance tests first.
- **Sync with main.** Before handing work on for review or Master merge, merge or cleanly rebase against current `main` of the configured GitHub remote, resolve conflicts locally, and rerun the affected local checks. Never force-push a shared branch or rewrite `main`.
- **Multi-model team and cross-family SwarmBus review.** Writer, companion reviewer (`P2`/`P1`/`P3`), and for UI/TrackPlan/editorial slices `P3` Head of UI/UX (`Gemini 4 Carbon`) review pushed branch commits via `SwarmBus` while the slice grows.
- **Independent final review and Master merge.** At Ready every slice (`H`, `M` and `L`) receives a fresh independent
  `P1` Claude review (`Claude Opus 5.5` — exact required tier and ceiling) plus independent cross-family `P2`/`P3` verification on the exact branch `head` and `origin/main` base (neither may be the writer's or companion's session). With zero open Blocker/High/Medium findings and green local Release/analyzer/test checks (plus applicable local Stryker verification), a standing Master session squashes/fast-forwards onto `origin/main` (`Closes #<id>` / `Refs #<id>`), deletes the remote task branch, releases explicit `bus_locks`, and monitors post-merge `main` CI (`.github/workflows/quality.yml`) and SonarCloud (`branch=main`, `0` open issues).
- **Guard rails.** Two failed fix attempts on the same finding (in companion or final review), or more than ten
  agent messages on one topic, stop the loop and go to the user. Multi-slice features stay behind feature toggles until complete.
- **People.** The user decides product goals, priorities, material UX, fundamental architecture, new costs and every
  app, device or hardware action. Since 2026-11-06 the user's personal acceptance is no merge gate. The user does
  not relay messages or inspect code manually. Any role may consult the Head of UI/UX (`Gemini 4 Carbon` via
  Antigravity CLI) under the contract in master coordination.
- An explicit narrower assignment, pause or no-merge instruction stays binding; do not infer new product work.

## Repository policies

- GitHub is the only Git host. Inspect `git remote -v` and branch tracking before synchronizing; use the configured
  GitHub remote rather than assuming `origin` or restoring an Azure remote.
- Never commit directly in the shared root `main` checkout during active development, never `push --force` to `main`, and never delete `main` (`MOBAflow` Ruleset enforces `deletion` and `non_fast_forward`). Implementations, bug fixes and other repository changes develop in isolated worktrees + task branches and land on `main` via coordinated Master fast-forward/squash push after cross-family review; Pull Requests were intentionally disabled on 2026-11-27.
  The shared `main` checkout may be fast-forwarded cleanly to integrated remote `main`.
- Isolate independent write tasks with Git worktrees: use a dedicated task branch/worktree as specified in
  [.github/instructions/git-worktree-isolation.instructions.md](.github/instructions/git-worktree-isolation.instructions.md).
  A user-requested synchronization of the shared checkout can be prepared there and then fast-forwarded into that checkout.
- Never launch MOBAflow without explicit user approval. Build, restore and test requests alone do not authorize
  starting the WinUI executable, debugger, watch process or launch-based UI automation.
- MOBAflow has no released users yet: do not add legacy paths, serialized-format compatibility, migrations or
  adoption flows. Replace a superseded model and remove its code, tests, API endpoints and docs in the same change.
  Keep `Solution.CurrentSchemaVersion` unless the task requires a bump; removed JSON fields are ignored on load.
  Remove legacy paths that already exist only in a dedicated change, not as incidental cleanup.
- SonarQube analysis and review: run full SonarCloud analysis through the GitHub pipeline (`main` automatic analysis + CI) and verify `0` OPEN/CONFIRMED issues on `main` post-merge; do not run local Sonar/Vortex code analysis or install analysis hooks. Keep local Release builds, analyzer baseline checks (`Test-AnalyzerBaseline.ps1`), test suites, and deterministic secrets scans required before every push/merge; cloud sessions follow the cloud rule below. Follow
  [the Sonar policy](.github/instructions/sonarqube-pre-pr.instructions.md); preserve existing analyzer baseline gates.
- Balanced secrets scanning: local sessions scan likely secret-bearing files before reading them (pre-read) and
  changed files before commits/PRs (pre-commit). If a scan finds a secret, stop handling that file and report it
  without exposing its value. Cloud sessions without a local scanner, such as the Executive Assistant, need no local
  scan: pre-read, they do not open likely secret-bearing files; pre-commit, GitHub Push Protection checks each push
  for provider token patterns only, so generic passwords, connection strings and private keys are not detected, and
  each PR records the missing local scan. If a local session's scanner is unavailable,
  continue ordinary development, avoid sensitive files and record the limitation before publication.
- After patches, generators or scripts change text files, run `scripts/Test-LineEndings.ps1 -Path <changed-paths>`.
  Use `-Fix` to normalize those files according to `.gitattributes`, then check again. Preserve LF exceptions
  and encoding. End every file you create or edit with a final newline (`.editorconfig` sets
  `insert_final_newline = true`; `-Fix` does not add one), except vendored `MOBAdisplay/esp32/lib/**` and
  Spec Kit-managed `.specify/**` files, which keep their upstream bytes. Before committing, run `scripts/Test-LineEndings.ps1 -Staged`;
  it checks both staged files on disk and mixed line endings in the index.
- C# whitespace check (folder-based, no restore): `dotnet format whitespace --folder . --verify-no-changes
  --exclude .nuget MOBAdisplay/esp32`, run from the task worktree root (it ignores `.gitignore`). The required
  `Portable tests on Linux` check runs the same command, so fix drift in the files you changed; do not reformat
  files outside the task.
- Standalone Markdown plans belong in `plans/`; remove completed plans, retaining Git history and closed GitHub issues.
  Spec Kit artifacts remain under `specs/`. Use [Spec Kit governance](.github/instructions/spec-kit-governance.instructions.md)
  for product behavior and cross-cutting features; this does not require a standalone plan for every small fix.

## Repository map

Use [Moba.slnx](Moba.slnx) and the affected project files to identify the build graph.
Do not assume every directory is an active project or discover projects inside `.nuget`, `bin`, or `obj`.

| Area | Responsibility |
| --- | --- |
| `Domain/` | Serializable models, enums, workflow payloads and domain rules |
| `Contracts/` | Wire contracts shared by MOBApi and the apps: runtime snapshots, remote commands, runtime hub, discovery protocol |
| `Common/` | Neutral infrastructure: `AppSettings` sections, events, network discovery helpers, paths, recording and logging |
| `Backend/` | Z21 transport/protocol, runtime, journey managers and workflow action handlers |
| `SharedUI/` | Shared MVVM state, UI settings root, commands, runtime projection and UI service interfaces |
| `MOBAflow/` | WinUI 3 desktop UI and Windows service implementations |
| `MOBAsmart/` | Android MAUI UI, local/remote runtime coordination and mobile services |
| `MOBApi/` | ASP.NET Core REST and SignalR host (default REST port 5001) |
| `MOBAdemo/` | MOBAdemo web showcase: static ASP.NET Core host and its web app |
| `MOBAdisplay/` | Display rendering/transport; ESP32-S3 firmware in `esp32/` |
| `Sound/` | Platform-neutral sound-effect playback contracts and null implementation |
| `Speech.Base/` | Platform-neutral speech contracts, shared output and audio cache |
| `Speech.Piper/` | Windows Piper speech synthesis and pronunciation normalization |
| `Speech.SystemSpeech/` | Windows System.Speech synthesis provider |
| `TrackLibrary.Base/`, `TrackLibrary.PikoA/`, `TrackPlan.Editor/`, `TrackPlan.Renderer/` | Track catalogues, track-plan editor and rendering |
| `Test/` | NUnit tests, Moq, test doubles and fixtures; `Analysis/` has separate helper projects |
| `docs/`, `scripts/`, `.vscode/tasks.json` | Architecture/user docs and development workflows |
| `.github/workflows/` | GitHub CI and release definitions |

## Architecture constraints

- Keep `Domain`, `Common`, `Backend`, and shared UI logic free of WinUI/MAUI types. Adapt OS-specific behavior in
  platform services; use existing I/O interfaces and constructor injection. Follow existing service lifetimes.
- Shared ViewModels read runtime state through `IRuntimeSnapshotProvider`, `IConnectionRuntime` and `ITrafficMonitor`
  and send every operator command through the required `IRuntimeCommandGateway`; hosts create and register gateways.
  Preserve local/remote routing; do not reintroduce `IMobaClient` or duplicate runtime ownership in a ViewModel.
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
- Preserve config defaults and safe locomotive startup (speed zero, no restored movement).
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
| Documentation/instructions only | Review diff, links and commands; run `scripts/Test-InstructionConsistency.ps1` for instruction edits; no .NET build/tests |
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
[GitHub quality CI](.github/workflows/quality.yml), including its analyzer baselines when applicable.
Run/watch the desktop app only with explicit launch approval. Check optional tooling such as `winapp` before relying on it.

### Android (with MAUI/Android workloads)

```text
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

For Android Release/AAB changes, use the release workflow's workload/restore/publish steps and validate the bundle
with `scripts/Test-AndroidAppBundle.ps1`. A FastDebug build does not validate a Release bundle.

The app currently has one Android target. `dotnet restore -f` means force, not framework selection.
For an authorized device deployment, add `-p:MobaReliableDeploy=true -t:Run` to the Android build if needed.
ESP32 firmware has its own `MOBAdisplay/esp32/platformio.ini`; a .NET build does not validate firmware.
Use fakes for automated railroad tests. Live track power, locomotive movement and firmware flashing require a
hardware task that authorizes those actions; a code-validation request alone is not such authorization.

### Completion

Review the final diff for unintended edits and architecture regressions. Update existing documentation when the
change affects its accuracy; keep durable project knowledge here and session progress in the conversation.
GitHub issues, milestones and Kanban track open work; consult/update a linked issue when the task calls for it,
without making tracker access a prerequisite for independent local work.
For every integration, assess architecture/developer documentation and the canonical user wiki under `docs/wiki/`.
Record the affected pages and corrections, or a concrete reason no change is needed; reassess after behavior-changing
review fixes and confirm against actual integrated `main`. Record remaining corrections as a follow-up slice on the
Kanban without claiming complete documentation currency while known drift remains.
When committing is requested, use English Conventional Commits. Report the commands/checks run and their outcomes;
identify unavailable platform/hardware checks and the smallest remaining check without claiming they passed.

## Guidance sources

Reviewed against official OpenAI guidance on 2026-09-06. These are the basis for the workflow above, not required
reading for each coding task:

- [GPT-6 Astra prompting guidance](https://developers.openai.com/api/docs/guides/latest-model)
- [Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [AGENTS.md discovery and scope](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
