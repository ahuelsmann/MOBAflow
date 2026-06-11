# AGENTS.md

**For AI coding agents working on MOBAflow** — event-driven model railroad automation on .NET 10.

> **Load `.github/copilot-instructions.md` FIRST** — it contains essential patterns, EventBus threading rules, absolute coding rules, and the 6-Step Workflow that all agents must follow.
>
> This file focuses on **platform-specific setup, build procedures, and architecture notes for agents running on the Cursor Cloud Linux VM.**

---

## 🎯 Critical for All Agents (Read First!)

1. **EventBus Threading Boundary** (MOST CRITICAL)
   - Z21 publishes on background thread → `UiThreadEventBusDecorator` marshals to UI thread
   - ViewModels subscribe directly (no manual dispatcher calls)
   - Key files: `Backend/Z21.cs`, `SharedUI/Service/UiThreadEventBusDecorator.cs`, `SharedUI/ViewModel/MainWindowViewModel.cs`
   - See: `.github/copilot-instructions.md` § EventBus Threading Boundary

2. **Absolute Rules (14 rules)**
   - No `.Result` / `.Wait()` → Always use `await`
   - No hardcoded colors → `ThemeResource` only
   - No `InvokeOnUi` in EventBus handlers → Decorator already marshals
   - Backend/Common platform-independent → Zero WinUI/MAUI references
   - See: `.github/copilot-instructions.md` § Absolute Rules

3. **6-Step Workflow**
   - **1. ANALYSE** → **2. RESEARCH** → **3. PLAN** (use `plan()` tool) → **4. IMPLEMENT** → **5. VALIDATE** → **6. DOCUMENT**
   - Tests for every new/changed feature (run `dotnet test` before commit)
   - See: `.github/copilot-instructions.md` § 6-Step Workflow

---

## 📚 Reference All Instruction Files

Located in `.github/instructions/`:

| File | Purpose |
| ---- | ------- |
| `copilot-instructions.md` | **LOAD THIS FIRST** — EventBus, rules, workflows, MVVM patterns |
| `di-pattern-consistency.instructions.md` | DI registration, singletons, constructor injection |
| `architecture.instructions.md` | Layer boundaries, data flow, threading model |
| `backend.instructions.md` | Platform independence, Z21 protocol, action executors |
| `mvvm-best-practices.instructions.md` | `[ObservableProperty]`, `[RelayCommand]`, observable state |
| `test.instructions.md` | AAA (Arrange-Act-Assert), NUnit, Moq, FakeUdpClientWrapper |
| `winui.instructions.md` | DispatcherQueue, DataTemplates, x:Bind |
| `fluent-design.instructions.md` | `ThemeResource`, 8px grid, icons, visual hierarchy |
| `xaml-page-registration.instructions.md` | XAML compiler, `<Page Remove>` issues |
| `naming-conventions.instructions.md` | PascalCase, `_camelCase`, UPPER_SNAKE_CASE (Z21 constants) |
| `self-explanatory-code-commenting.instructions.md` | Why, not What — document intent |
| `no-special-chars.instructions.md` | ASCII-only identifiers |
| `z21-backend.instructions.md` | Z21 UDP protocol, handler patterns |
| `maui.instructions.md` | MAUI-specific patterns for MOBAsmart |
| `vs-setup.instructions.md` | ReSharper extensions, project setup |

---

## 🤖 Available Agents

- **`enterprise-pr-reviewer`** — Structured PR reviews, architecture validation, security checks
  - Location: `.github/agents/enterprise-pr-reviewer.agent.md`
  - Use when: Reviewing pull requests, checking enterprise quality, producing PR feedback

### Optional Windows WinUI tooling

- **`winapp` CLI** — Available on the Windows development machine (`winapp --version` showed 0.3.1).
  - Use for Windows App SDK / WinUI app execution, packaging, and future UI automation checks.
  - Prefer project-scoped commands; do not scan or build every `.csproj` under the workspace.
- **Microsoft `win-dev-skills`** — Installed as an external Copilot/Claude plugin, not automatically loaded by all agents.
  - Treat it as guidance for WinUI workflows: WinUI 3 lane, Fluent Design, `x:Bind`, accessibility, packaging, and UI automation.
  - Do not let it override MOBAflow-specific rules in `.github/copilot-instructions.md`.
- **Workspace hygiene** — Keep `.nuget`, `bin`, and `obj` excluded from IDE search/watch/project discovery where possible.
  - The local `.nuget/packages` folder can contain package source projects that break central package management during C# language-server discovery.

---

## Cursor Cloud specific instructions

### Platform scope

This is a .NET 10 multi-platform solution. On the Linux Cloud VM **only cross-platform projects** can build and run:

| Buildable on Linux | NOT buildable (platform-specific) |
| ------------------ | -------------------------------- |
| Domain, Common, Backend, Sound, SharedUI, SharedUI.Web, TrackLibrary.Base, TrackLibrary.PikoA, TrackPlan.Renderer, MOBApi, Test | MOBAflow (`net10.0-windows10.0.22621.0`), MOBAsmart (`net10.0-android`), MAUI.Controls (`net10.0;net10.0-android`) |

### Build & test commands

Standard commands are documented in `docs/CLAUDE.md` and `README.md`. Key cross-platform commands:

```bash
# Restore & build individual projects (solution-level restore fails due to Windows/Android TFMs)
dotnet restore <project>.csproj
dotnet build <project>.csproj

# Typical cross-platform host build
dotnet build MOBApi/MOBApi.csproj
dotnet run --project MOBApi/MOBApi.csproj   # REST API host (default port 5001)

# Run tests
dotnet test Test/Test.csproj

# Collect coverage locally
dotnet test Test/Test.csproj --settings Test/coverlet.runsettings \
  --results-directory TestResults
```

### Known issues on Linux Cloud VM

- **Expected `dotnet test Test/Test.csproj` result on Linux:** ~480 pass, ~10 fail. The failures are pre-existing and platform-specific (NOT a setup regression):
  - `SystemSpeechEngineTest.OutputSpeech_MinimalTest` — `System.Speech` is Windows-only (`PlatformNotSupportedException`).
  - `PhotoPathHelperTests.ToFullPath_Strips_photos_prefix_backslash` and the whole `SolutionTest` fixture (`Constructor_InitializesWithDefaults`, `LoadAsync_*`, `LoadExampleSolutionJson_ShouldLoadSuccessfully`, `UpdateFrom_WithNull_ThrowsArgumentNullException`) — these assert on Windows backslash (`\`) path separators / file paths, which differ on Linux.
  - `TrackPlanFeedbackHighlighterTests.Feedback_Respects_IgnoreWindow` — clock/timing-sensitive assertion that does not hold on the Linux VM.
- **SkiaSharp native library**: `MOBAdisplay` references only the `SkiaSharp` meta-package (Windows natives), not `SkiaSharp.NativeAssets.Linux`. The Linux `libSkiaSharp.so` is provided at the system level (`/usr/lib/x86_64-linux-gnu/libSkiaSharp.so`) so the `MOBAdisplay` SkiaSharp tests load and pass. If that file is missing, those tests crash the test host with a `DllNotFoundException` for `libSkiaSharp`.
- **Release / warnings-as-errors build fails on Linux**: building any project in `-c Release` (e.g. `Test`, `MOBApi`, `Domain`) currently fails with pre-existing `CS1591` (missing XML doc comment) errors because Release sets `GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true`. Use `-c Debug` for cross-platform build/test on Linux; the analyzer/warning quality gate is enforced via Visual Studio + SonarQube on the Windows CI agent.
- **Solution-level restore**: `dotnet restore Moba.slnx` fails because the solution contains Windows and Android target frameworks. Restore individual `.csproj` files instead.
- **MOBAflow desktop app**: `MOBAflow/MOBAflow.csproj` requires Windows/WinUI tooling and cannot be built on Linux.
- **MOBAsmart**: `MOBAsmart/MOBAsmart.csproj` targets Android and requires MAUI/Android workloads that are not available on the Linux Cloud VM.

### .NET SDK

The project requires .NET 10 SDK (pinned in `global.json` to `10.0.300-preview` with `latestFeature` rollForward). It is installed under `~/.dotnet` (SDK `10.0.301`) and symlinked to `/usr/local/bin/dotnet`, so `dotnet` is on `PATH` for interactive and non-interactive shells.

---

## 🔀 Key Patterns & Examples for Agents

### MVVM ViewModel Pattern (CommunityToolkit)

```csharp
// ✅ CORRECT
public sealed partial class TrainControlViewModel : ObservableObject
{
    private readonly IMobaRuntime _runtime;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TrainControlViewModel> _logger;

    [ObservableProperty]
    private string statusText = "Ready";

    [RelayCommand]
    private async Task ExecuteWorkflow()
    {
        try
        {
            await _runtime.ExecuteWorkflow(workflowId);
            StatusText = "Workflow completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed");
            StatusText = "Error";
        }
    }

    public TrainControlViewModel(IMobaRuntime runtime, IEventBus eventBus, ILogger<TrainControlViewModel> logger)
    {
        _runtime = runtime;
        _eventBus = eventBus;
        _logger = logger;
        _eventBus.Subscribe<WorkflowCompletedEvent>(OnWorkflowCompleted);
    }

    private void OnWorkflowCompleted(WorkflowCompletedEvent e)
    {
        StatusText = $"Completed: {e.WorkflowId}";  // UI thread safe (decorator guaranteed)
    }
}
```

### UI Interaction Pattern (Commands, Not Code-Behind Logic)

```csharp
// ✅ CORRECT - UserControl forwards input to a ViewModel command
public ICommand? CellTappedCommand { get; set; }

private void OnCellTapped(object sender, TappedRoutedEventArgs e)
{
    if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out var index))
    {
        CellTappedCommand?.Execute(index);
    }
}

// ✅ CORRECT - ViewModel owns the behavior
[RelayCommand]
private void CellClicked(int cellIndex)
{
    MatrixViewModel.SetCellColor(cellIndex, SelectedColorBrush);
}

// ❌ WRONG - Page code-behind performs domain/UI behavior directly
private void OnMatrixCellTapped(object? sender, CellTappedEventArgs e)
{
    ViewModel.MatrixViewModel.SetCellColor(e.CellIndex, ViewModel.SelectedColorBrush);
}
```

### Platform-Neutral State Pattern

- Keep WinUI/MAUI types out of `Common`, `Domain`, and `Backend`.
- If UI state must be unit-tested, extract the behavior into a platform-neutral model in `Common`.
- Example: store matrix colors as ARGB values in `Common.Display.LedMatrix5x5State`; adapt to `SolidColorBrush` only in the WinUI ViewModel.

### Layout Persistence Pattern

- Persist star-sized resizable columns as `*ColumnStarValue`.
- Persist pixel widths only for intentionally fixed side columns/toolboxes.
- Do not reintroduce mixed semantics such as saving a pixel width in `PropertiesColumnWidth` and restoring it as `GridUnitType.Star`.

### EventBus Handler Pattern (No InvokeOnUi!)

```csharp
// ✅ CORRECT - Decorator already marshals to UI thread
private void OnFeedbackReceived(FeedbackReceivedEvent e)
{
    // ... 
    IsConnected = true;  // Safe: runs on UI thread
}

// ❌ WRONG - DO NOT do this
private void OnFeedbackReceived(FeedbackReceivedEvent e)
{
    _dispatcher.InvokeOnUi(() => IsConnected = true);  // Redundant, breaks pattern
}
```

### Backend Service (Platform-Independent)

```csharp
// ✅ CORRECT - Backend layer
public class WorkflowService : IWorkflowService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<WorkflowService> _logger;

    public async Task ExecuteAsync(Workflow workflow, WorkflowExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        _logger.LogInformation("Executing workflow {WorkflowId}", workflow.Id);

        foreach (var action in workflow.Actions)
        {
            await action.ExecuteAsync();  // No `.Result` or `.Wait()`!

            if (options?.StopOnFirstActionFailure ?? false)
            {
                _eventBus.Publish(new WorkflowStoppedEvent(workflow.Id));
                break;
            }
        }
    }
}

// ❌ WRONG - Sync blocking
public void Execute(Workflow workflow)
{
    var task = ExecuteAsync(workflow);
    task.Wait();  // WRONG! Use await instead
}
```

### DI Registration (WinUI/MOBAsmart)

```csharp
// In WinUI/App.xaml.cs or MOBAsmart/MauiProgram.cs
var services = new ServiceCollection();

// Domain & Backend
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<IMobaRuntime, MobaRuntimeService>();
services.AddSingleton<IWorkflowService, WorkflowService>();

// EventBus with UI thread decorator
services.AddSingleton<IEventBus, EventBus>();
services.AddEventBusWithUiDispatch();  // Wraps EventBus, ensures handlers run on UI thread

// ViewModels
services.AddSingleton<MainWindowViewModel>();
services.AddTransient<TrainControlViewModel>();

// Logging
services.AddLogging(builder =>
{
    builder.AddSerilog(/* configure Serilog */);
});
```

### Testing Pattern (NUnit + Moq)

```csharp
[TestFixture]
internal sealed class WorkflowServiceTests
{
    [Test]
    public async Task ExecuteAsync_Should_RunAllActions_When_OptionsNull()
    {
        // Arrange
        var eventBusMock = new Mock<IEventBus>();
        var loggerMock = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(eventBusMock.Object, loggerMock.Object);

        var workflow = new Workflow { Id = 1, Actions = new List<WorkflowAction> { /* ... */ } };

        // Act
        await service.ExecuteAsync(workflow, null);

        // Assert
        eventBusMock.Verify(e => e.Publish(It.IsAny<WorkflowCompletedEvent>()), Times.Once);
    }
}
```

### Architecture notes (for agents)

**Runtime & ViewModels:**

- Shared ViewModels depend on **`IMobaRuntime`** (`MobaRuntimeService`), not `IMobaClient`
- `MobaRuntimeService` is a **sealed partial** type split across:
  - `MobaRuntimeService.cs` (core, constructor, snapshot)
  - `MobaRuntimeService.RuntimeApi.cs` (public IMobaRuntime API)
  - `MobaRuntimeService.Z21Handlers.cs` (Z21 event callbacks, journey projection)
  - `MobaRuntimeService.AutoConnect.cs` (auto-connect timer, endpoint resolution)
  - `MobaRuntimeService.StatusFormatting.cs` (status text helpers, signal polarity)

**Master Data & Configuration:**

- Master JSON (`data.json`) → **`MasterDataStore`** in Backend DI (registered in `AddMobaBackendServices`)
- Project/Solution state → **`IMobaRuntime.CurrentSnapshot`** (immutable at query time)
- Config defaults → `Common.Configuration` (must not break existing defaults; see tests: `Test/Common/AppSettingsDefaultsTests.cs`)

**Workflows & Actions:**

- Workflow execution: **`IWorkflowService.ExecuteAsync`** with optional **`WorkflowExecutionOptions`**
  - Example: `StopOnFirstActionFailure` for sequential runs
- Action executors live in `Backend/Manager/` (pluggable, FakeUdpClientWrapper for testing)

**DI Bindings (see `.github/instructions/di-pattern-consistency.instructions.md`):**

- Registration pattern: `services.AddSingleton<IZ21, Z21>()`, `services.AddSingleton<IMobaRuntime, MobaRuntimeService>()`
- WinUI App.xaml.cs (~line 215), MOBAsmart MauiProgram.cs (~line 98)
- EventBus wrapping: `AddEventBusWithUiDispatch()` decorator ensures UI thread safety

**Test Coverage (Regression Protection):**

- **Path handling:** Use `Common.Path.PhotoPathHelper.ToFullPath(baseDir, relativePath)` → Tests: `Test/Common/PhotoPathHelperTests.cs`
- **Discovery protocol:** Use `Common.Discovery.DiscoveryResponseParser.TryParse()` for "MOBAFLOW_REST_API|ip|port" → Tests: `Test/Common/DiscoveryResponseParserTests.cs`
- **Config defaults:** Changing `Common.Configuration` must preserve defaults → Tests: `Test/Common/AppSettingsDefaultsTests.cs`
- **New features:** Always add unit tests for shared logic (Domain, Backend, Common); platform UI (WinUI/MAUI) at least critical paths covered

**Up-to-date diagrams and DI examples:** See `docs/ARCHITECTURE.md` and `README.md` (runtime boundary)
