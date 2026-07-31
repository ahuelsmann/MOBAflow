# MOBAflow Copilot Instructions

**For AI coding agents working on MOBAflow — event-driven model railroad automation on .NET 10.**

> Essential patterns, boundaries, and critical knowledge. Read `.github/instructions/` for deep dives. **This file is ALWAYS loaded.**

AI-generated commit messages must be written in english and must follow the “Conventional Commits” format: https://www.conventionalcommits.org/en/v1.0.0/

---

## 🎯 EventBus Threading Boundary (MOST CRITICAL)

**Single thread-marshalling boundary.** Z21 publishes on background thread → decorator marshals to UI → ViewModels update safely.

```
Z21 (background thread) → Publish(FeedbackReceivedEvent)
UiThreadEventBusDecorator → dispatcher.InvokeOnUi(handler)
MainWindowViewModel.OnFeedbackReceived() → IsConnected = true (UI thread safe)
```

**Key files:**
- `Backend/Z21.cs` — Publishes events
- `SharedUI/Service/UiThreadEventBusDecorator.cs` — Marshals to UI thread
- `SharedUI/ViewModel/MainWindowViewModel.cs` — Subscribes (no dispatcher needed!)
- `MOBAflow/App.xaml.cs` — `AddEventBusWithUiDispatch()` registration

---

## 🚫 Absolute Rules

1. **No code without analysis** — `code_search`, `file_search`, `find_symbol` first
2. **No hardcoded colors** — `ThemeResource` only (`CardBackgroundFillColorDefaultBrush`, etc.)
3. **No `.Result` / `.Wait()`** — Use `await`
4. **No `InvokeOnUi` in EventBus handlers** — Decorator already marshals to UI thread
5. **No separate README.md in subdirs** — Root only
6. **No `<Page Remove="..."/>` in .csproj** — Breaks XAML compiler
7. **No TODO comments in code** — create a GitHub issue instead
8. **Backend/Common platform-independent** — Zero WinUI/MAUI references
9. **Never guess file names, APIs** — Use tools first
10. **No session details here** — use GitHub issues for durable project work
11. **All new or changed features must have tests** — Every suggested/implemented feature needs unit or integration tests; run `dotnet test` before commit.
12. **No commands in code-behind** — Move to ViewModel with `IDialogService` for UI interaction
13. **UserControls are input adapters only** — They may translate XAML events to `ICommand`, but must not own feature behavior
14. **Persist star layout as star values** — Use `*ColumnStarValue` for star-sized columns; do not mix pixel persistence with star restore
15. **Validate Light and Dark theme for every UI change** — Use `ThemeResource` / platform theme tokens for foregrounds, backgrounds, borders, icons, selected states, hover states, disabled states, drag/drop visuals, and custom converters. Never encode colors that only work in one theme.
16. **English UI language** — All user-visible text in UI apps (MOBAflow WinUI, MOBAsmart MAUI) must be English: labels, buttons, status messages, tooltips, empty states, defaults, and shipped master data (`data.json`, `solution.json`). No German (or other non-English) UI strings in XAML, ViewModels, or domain defaults. TTS/announcement language remains user-configurable in settings and is independent of UI language.
17. **Standalone Markdown plans belong in `plans/`** — Keep project, quality, refactoring, and roadmap planning files out of `docs/`. Delete completed plans; Git history and closed GitHub work items retain the record. `docs/` contains current reference and user documentation. Spec Kit artifacts such as `specs/*/plan.md` remain in their feature directory. GitHub issues, milestones, and Kanban remain authoritative for active work and progress.
18. **SonarQube before PR review** — Attempt local Sonar analysis against the actual PR base, then create every PR as a draft. Do not mark it ready for review until the remote SonarCloud check is green and the PR has zero `OPEN`/`CONFIRMED` Sonar issues. Do not lower quality gates, suppress valid findings, or exclude changed files to make the analysis pass. See `.github/instructions/sonarqube-pre-pr.instructions.md`.
19. **Balanced secrets scanning** — Run `sonar analyze secrets` before reading files likely to contain credentials, tokens, private keys, certificates, connection strings, or deployment secrets. Ordinary source, tests, Markdown, schemas, and checked-in templates do not require an individual pre-read scan unless context suggests secret material. Run a secrets scan over all changed files before every commit and PR. If the scanner is unavailable, do not read sensitive files; ordinary development may continue and the limitation must be recorded before publication.
20. **Never launch MOBAflow without explicit user approval** — Building, restoring, and testing are allowed, but every command or tool action that starts the MOBAflow WinUI application requires the user's explicit prior approval. This includes `dotnet run`, `dotnet watch run`, `winapp` launch commands, starting the executable, debugger launches, and UI automation that launches the application. Do not infer launch approval from a request to build, test, diagnose, or prepare UI automation.
21. **Write answers for junior developers** — Formulate explanations and answers as if they are addressed to a junior developer. Use clear, plain language, provide the context needed to understand the reasoning, briefly explain unfamiliar terms, and make next steps explicit without being patronizing.

---

## ✅ 6-Step Workflow

### 1. ANALYSE
- Understand requirements
- Identify affected files
- Find existing patterns
- Identify existing tests

### 2. RESEARCH
- Existing implementations
- Documentation
- .NET 10 / WinUI 3 specs

### 3. PLAN
- **ALWAYS use `plan()` tool**
- Affected files, risks, test strategy

### 4. IMPLEMENTATION
- Backend → ViewModel → View
- Use `get_errors()` after each file
- XAML: `ThemeResource` only
- UI theme validation: verify Light and Dark theme contrast for new or changed views, templates, icons, visual states, and converters
- MVVM: `[ObservableProperty]`, `[RelayCommand]`
- UserControl input: expose `ICommand` dependency properties instead of page code-behind actions
- Async: `await` always

### 5. VALIDATION
- `run_build()` at end
- `run_tests()` for relevant projects
- `.editorconfig` compliance
- Attempt local SonarQube analysis against the actual base branch
- Run a secrets scan over all changed files
- Create the PR as a draft, then verify the remote SonarCloud result and PR issue count before review

### 6. DOCUMENTATION
- `README.md` updated (if user-facing)
- GitHub issues updated (if applicable)
- Inline comments: Why, not What
- Public APIs: XML docs

---

## 🏗️ Layer Responsibilities

| Layer | Location | Role |
|-------|----------|------|
| Domain | `Domain/*.cs` | POCOs, business logic |
| Backend | `Backend/Service/*.cs` | Z21, WorkflowService, events |
| EventBus | `Common/Events/`, `SharedUI/Service/` | Pub/sub, UI marshalling |
| ViewModels | `SharedUI/ViewModel/*.cs` | Observable state |
| Platform | `MOBAflow/`, `MOBAsmart/`, `MOBApi/` | Pages, navigation, API |
| Testable state | `Common/`, `Domain/` | Platform-neutral behavior and state models |

---

## 🔀 MVVM & EventBus Pattern

```csharp
// ✅ CORRECT
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IEventBus _eventBus;
    
    public MainWindowViewModel(..., IEventBus eventBus, ...)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived);
    }
    
    private void OnFeedbackReceived(FeedbackReceivedEvent e)
    {
        IsConnected = true;  // Safe: UI thread (decorator guaranteed)
    }
}

// ❌ WRONG
_dispatcher.InvokeOnUi(() => IsConnected = true);
```

### UI Interaction Pattern

```csharp
// ✅ CORRECT - UserControl forwards input to ViewModel command
public ICommand? CellTappedCommand { get; set; }

private void OnCellTapped(object sender, TappedRoutedEventArgs e)
{
    if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out var index))
    {
        CellTappedCommand?.Execute(index);
    }
}

// ✅ CORRECT - ViewModel owns behavior
[RelayCommand]
private void CellClicked(int cellIndex)
{
    MatrixViewModel.SetCellColor(cellIndex, SelectedColorBrush);
}

// ❌ WRONG - Page code-behind owns behavior
private void OnMatrixCellTapped(object? sender, CellTappedEventArgs e)
{
    ViewModel.MatrixViewModel.SetCellColor(e.CellIndex, ViewModel.SelectedColorBrush);
}
```

---

## 💉 DI Registration (MOBAflow/App.xaml.cs)

```csharp
var services = new ServiceCollection();
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<IEventBus, EventBus>();
services.AddEventBusWithUiDispatch();  // Decorator wrapping
services.AddSingleton<MainWindowViewModel>();
services.AddTransient<View.JourneyPage>();
```

---

## 📁 Key File Locations

**Domain:** `Domain/Project.cs`, `Domain/Journey.cs`, `Domain/Locomotive.cs`

**Backend:** `Backend/Z21.cs`, `Backend/Service/WorkflowService.cs`, `Backend/Service/ProjectValidator.cs`

**ViewModels:** `SharedUI/ViewModel/MainWindowViewModel.cs`, `SharedUI/ViewModel/TrainControlViewModel.cs`, `SharedUI/ViewModel/SignalBoxPlanViewModel.cs`

**WinUI Pages:** `MOBAflow/View/MainWindow.xaml`, `TrackPlanPage.xaml`, `SignalBoxPage.xaml`

---

## 🛠️ Build & Test

```bash
dotnet build MOBApi/MOBApi.csproj
dotnet test Test/Test.csproj
dotnet build MOBAflow/MOBAflow.csproj --no-restore --no-dependencies  # Windows/WinUI compile check
dotnet run --project MOBAflow    # Windows app; requires explicit prior user approval
dotnet run --project MOBApi      # REST API (Port 5001)
```

**Windows WinUI tooling:**
- `winapp` is available on the Windows development machine (`winapp --version` returned 0.3.1). Use it when packaged WinUI execution, packaging/signing, or UI automation is needed.
- Microsoft `win-dev-skills` can guide WinUI 3 workflows, Fluent Design, accessibility, `x:Bind`, packaging, and UI automation, but MOBAflow instructions remain authoritative.
- Keep `.nuget`, `bin`, and `obj` excluded from IDE search/watch/project discovery. Do not scan or build every `.csproj` under the workspace because `.nuget/packages` can contain package source projects that conflict with central package management.

**Test coverage (regression protection):**
- **Path handling:** Use `Common.Path.PhotoPathHelper.ToFullPath(baseDir, relativePath)` for photo paths; do not duplicate path logic. Tests: `Test/Common/PhotoPathHelperTests.cs`.
- **Discovery protocol:** Use `Common.Discovery.DiscoveryResponseParser.TryParse()` for "MOBAFLOW_REST_API|ip|port". Tests: `Test/Common/DiscoveryResponseParserTests.cs`.
- **Config defaults:** Changing `Common.Configuration` (e.g. `Application.PhotoStoragePath`, `RestApi.Port`) must not break defaults. Tests: `Test/Common/AppSettingsDefaultsTests.cs`.
- **New features:** Add unit tests for shared logic (path, config, parsing, state models). Platform-specific UI (WinUI/MAUI) should at least have critical paths covered by integration or documented manual checks.
- **UI testability:** If WinUI/MAUI types make tests impractical, extract behavior into platform-neutral `Common`, `Domain`, or `Backend` types and adapt it in the platform ViewModel.

---

## 🧭 Design Principles

- **SOLID:** Single Responsibility | Open/Closed | Liskov | Interface Segregation | Dependency Inversion
- **DRY:** Don't Repeat Yourself (max 2x duplication)
- **KISS:** Keep It Simple (<20 lines/method)
- **Meaningful Names:** Not "x", "temp", "data"
- **Separation of Concerns:** Domain ↔ ViewModel ↔ View strictly separated
- **Layout Persistence:** Store star-sized columns as `*ColumnStarValue`; store pixel widths only for intentionally fixed columns

---

## 📚 Instruction Files

**Architecture:**
- `di-pattern-consistency.instructions.md` — DI rules, singletons
- `architecture.instructions.md` — Layers, data flow, threading
- `backend.instructions.md` — Platform independence
- `mvvm-best-practices.instructions.md` — MVVM patterns
- `test.instructions.md` — AAA, Fakes, NUnit

**WinUI:**
- `winui.instructions.md` — DispatcherQueue, DataTemplates, x:Bind
- `fluent-design.instructions.md` — ThemeResource, 8px grid, icons
- `xaml-page-registration.instructions.md` — XAML compiler issues

**Code Quality:**
- `naming-conventions.instructions.md` — PascalCase
- `self-explanatory-code-commenting.instructions.md` — Why not What
- `no-special-chars.instructions.md` — ASCII only

**Workflow:**
- **GitHub issues, milestones, and Kanban** — authoritative for open work

---

## ✅ Pre-Commit Checklist

- [ ] `.editorconfig` compliance
- [ ] No TODO comments
- [ ] No `.Result` / `.Wait()`
- [ ] Constructor injection only
- [ ] `[ObservableProperty]` for MVVM
- [ ] XML-docs for public APIs
- [ ] **Tests for new/changed behavior** — Unit or integration tests added; `dotnet test` passes
- [ ] **User-visible UI strings are English** (MOBAflow, MOBAsmart, SharedUI ViewModels bound to UI)
- [ ] Local Sonar analysis completed, or its documented capability limitation was recorded on the draft PR
- [ ] Changed files passed `sonar analyze secrets`
- [ ] PR was created as a draft and stayed draft until SonarCloud was green with zero `OPEN`/`CONFIRMED` issues
- [ ] Build: `dotnet build`
- [ ] README updated (if user-facing)

---

## 🌍 When in Doubt

1. **GitHub issues, milestones, and Kanban** — authoritative for open work
2. `.github/instructions/` — Technical deep dives
3. Microsoft Learn — .NET 10 / WinUI 3 specs
4. Existing code — Follow surrounding style
