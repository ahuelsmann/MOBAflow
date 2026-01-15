---
description: 'MOBAflow architecture overview - layers, data flow, key interfaces, and project structure for Copilot context.'
applyTo: '**'
---

# MOBAflow Architecture

> **Reference:** Full documentation in [`ARCHITECTURE.md`](../../ARCHITECTURE.md)

---

## Layer Overview (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                   Presentation Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │  WinUI   │  │  MAUI    │  │  Blazor  │  │  Plugins │    │
│  │(Windows) │  │(Android) │  │  (Web)   │  │(Dynamic) │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
└───────┼──────────────┼────────────┼────────────┼────────────┘
        └──────────────┼────────────┼────────────┘
                       ↓            ↓
          ┌────────────────────────────────────┐
          │     SharedUI (Base ViewModels)     │
          │   MVVM, Commands, Observable Props │
          └────────────────┬───────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│              Domain & Business Logic Layer                   │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │  Domain    │  │   Backend    │  │  TrackPlan   │         │
│  │ (Models)   │  │  (Services)  │  │  (Geometry)  │         │
│  └────────────┘  └──────────────┘  └──────────────┘         │
└──────────────────────────┬───────────────────────────────────┘
                           ↓
            ┌──────────────┴──────────────┐
            │                             │
        ┌───────────┐              ┌────────────┐
        │ External  │              │ Logging &  │
        │ Services  │              │ Config     │
        │ (Z21 UDP) │              │ (Serilog)  │
        └───────────┘              └────────────┘
```

---

## Project Structure

| Project | Layer | Purpose |
|---------|-------|---------|
| `Domain/` | Domain | Pure POCOs (Solution, Journey, Workflow, Train, FeedbackPoint) |
| `Backend/` | Service | Z21 communication, WorkflowService, ActionExecutor |
| `Common/` | Shared | Configuration, Logging, Plugin interfaces |
| `SharedUI/` | Presentation | Base ViewModels (MainWindowViewModel, etc.) |
| `WinUI/` | Platform | Windows Desktop (XAML Pages, WinUI Services) |
| `MAUI/` | Platform | Android Mobile (XAML Pages, MAUI Services) |
| `WebApp/` | Platform | Blazor Server (Razor Components) |
| `TrackPlan/` | Domain | Track plan models and validation |
| `TrackPlan.Renderer/` | Service | Geometry calculations, SVG rendering |
| `TrackPlan.Editor/` | Presentation | Editor ViewModels, drag & drop logic |
| `TrackLibrary.PikoA/` | Plugin | Piko A-Gleis track templates |

---

## Key Interfaces

### IZ21 (Z21 Communication)

```csharp
public interface IZ21
{
    bool IsConnected { get; }
    Task ConnectAsync(string ipAddress);
    Task DisconnectAsync();
    Task SetTrackPowerAsync(bool on);
    Task SetLocomotiveSpeedAsync(int address, int speed, bool forward);
    Task SetLocomotiveFunctionAsync(int address, int function, bool on);
    event EventHandler<FeedbackReceivedEventArgs> FeedbackReceived;
    event EventHandler<SystemStateEventArgs> SystemStateChanged;
}
```

### IActionExecutor (Workflow Actions)

```csharp
public interface IActionExecutor
{
    Task ExecuteActionAsync(WorkflowAction action, ExecutionContext context);
}
```

### ISpeakerEngine (Text-to-Speech)

```csharp
public interface ISpeakerEngine
{
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);
    Task StopAsync();
}
```

### IIoService (File Operations)

```csharp
public interface IIoService
{
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? path);
    Task<string?> BrowseForJsonFileAsync();
}
```

---

## Data Flow: Z21 Feedback

```
Z21 Station (UDP broadcast Port 21105)
  ↓ UDP packet
IZ21.ReceiveFeedback()
  ↓
[FeedbackReceivedEventArgs]
  ├─ InPort (track sensor address)
  └─ Value (1=occupied, 0=free)
  ↓
MainWindowViewModel.OnFeedbackReceived()
  ↓
Journey.HandleFeedback()
  ├─ Match station by FeedbackPoint
  └─ Trigger associated Workflow
  ↓
WorkflowService.ExecuteWorkflow()
  ├─ Execute action sequence
  ├─ TTS announcements
  └─ Update UI state
  ↓
UI Updates (ObservableProperty)
  └─ WinUI/MAUI/Blazor re-render
```

---

## Data Flow: Command Execution

```
User clicks button
  ↓
XAML Command binding
  ↓
[RelayCommand] in ViewModel
  ↓
await _z21.SetLocomotiveSpeedAsync(...)
  ↓
IZ21 builds UDP packet
  ↓
UDP send to Z21 (Port 21105)
  ↓
Z21 executes command
  ↓
Train moves on track
```

---

## ViewModel Pattern (CommunityToolkit.Mvvm)

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly ILogger<MainWindowViewModel> _logger;

    // Observable property (source generator)
    [ObservableProperty]
    private bool _isConnected;

    // Command (source generator)
    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            await _z21.ConnectAsync(IpAddress);
            IsConnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            ErrorMessage = ex.Message;
        }
    }

    // Property change notification
    partial void OnIsConnectedChanged(bool value)
    {
        _logger.LogInformation("Connection state: {IsConnected}", value);
    }
}
```

---

## Plugin System

### Plugin Lifecycle

```
[1] DISCOVERY  → Scan Plugins/*.dll, reflect IPlugin
[2] VALIDATION → Check names, tags, reserved words
[3] CONFIGURE  → Plugin.ConfigureServices(services)
[4] INITIALIZE → Plugin.OnInitializedAsync()
[5] RUNTIME    → Pages available in NavigationView
[6] UNLOAD     → Plugin.OnUnloadingAsync()
```

### Plugin Implementation

```csharp
public class MyPlugin : PluginBase
{
    public override string Name => "My Plugin";
    public override string Description => "Example plugin";

    public override IEnumerable<PageDescriptor> GetPages()
    {
        yield return new PageDescriptor(
            Tag: "MyPage",
            Title: "My Page",
            Icon: Symbol.Page,
            PageType: typeof(MyPluginPage));
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MyPluginViewModel>();
        services.AddTransient<MyPluginPage>();
    }
}
```

---

## Domain Models (Key Entities)

| Entity | Description | Key Properties |
|--------|-------------|----------------|
| `Solution` | Root aggregate, saved as .mobaflow file | Name, Projects, Settings |
| `Project` | Container for journeys, workflows, trains | Name, Journeys, Workflows, Trains |
| `Journey` | Train route with stations | Name, Stations, Text (announcement template) |
| `Station` | Stop on a journey | Name, FeedbackPointId, IsExitOnLeft |
| `Workflow` | Action sequence | Name, Trigger, Actions, ExecutionMode |
| `WorkflowAction` | Single action in workflow | ActionType, Parameters, DelayAfterMs |
| `Train` | Locomotive definition | Name, Address, MaxSpeed, Functions |
| `FeedbackPoint` | Track sensor | Id, Address, Name |

---

## File Locations

| File Type | Location | Format |
|-----------|----------|--------|
| Solution | User-selected | `.mobaflow` (JSON) |
| Settings | `WinUI/appsettings.json` | JSON |
| Logs | `bin/Debug/logs/mobaflow-*.log` | Rolling text |
| Plugins | `WinUI/bin/Debug/Plugins/` | DLL |
| Track Libraries | `TrackLibrary.*/` projects | Compiled |

---

## Dependency Injection

All services registered in `App.xaml.cs` (WinUI) / `MauiProgram.cs` (MAUI):

```csharp
// Core services
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<IWorkflowService, WorkflowService>();
services.AddSingleton<IAnnouncementService, AnnouncementService>();

// Speech engines (factory pattern)
services.AddSingleton<ISpeakerEngine>(sp => 
    SpeakerEngineFactory.Create(settings.Speech));

// ViewModels
services.AddSingleton<MainWindowViewModel>();
services.AddTransient<JourneyViewModel>();
services.AddTransient<WorkflowViewModel>();

// Pages
services.AddTransient<OverviewPage>();
services.AddTransient<JourneysPage>();
// ... etc.
```

---

## Cross-Platform Strategy

| Feature | WinUI | MAUI | Blazor |
|---------|-------|------|--------|
| UI Framework | WinUI 3 XAML | .NET MAUI XAML | Razor Components |
| Navigation | NavigationView | Shell | NavMenu |
| File I/O | FileSavePicker | FilePicker | Server-side |
| Speech | Windows SAPI | Android TTS | Not supported |
| Z21 Connection | Full UDP | Full UDP | Via REST API |

---

## Key Conventions

1. **Async Suffix**: All async methods end with `Async`
2. **Cancellation**: Pass `CancellationToken` through async chains
3. **Logging**: Use structured logging with `{Property}` placeholders
4. **Null Checks**: Use `ArgumentNullException.ThrowIfNull()`
5. **MVVM**: Use `[ObservableProperty]` and `[RelayCommand]` attributes
6. **DI**: Constructor injection, no service locator pattern
7. **ConfigureAwait**: Use `.ConfigureAwait(false)` in library code
