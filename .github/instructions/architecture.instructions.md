---
description: 'MOBAflow architecture - layers, projects, key interfaces'
applyTo: '**/*.cs'
---

# MOBAflow Architecture

> Full docs: [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md)

## Layer Overview

Presentation (MOBAflow/MOBAsmart/MOBApi) → SharedUI (ViewModels) → Domain/Backend/TrackPlan → External (Z21 UDP)

## Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| `Domain/` | Domain | POCOs: Solution, Journey, Workflow, Train |
| `Backend/` | Service | Z21, WorkflowService, ActionExecutor |
| `SharedUI/` | Presentation | Base ViewModels |
| `MOBAflow/` | Platform | Windows Desktop |
| `MOBAsmart/` | Platform | Android Mobile |
| `MOBApi/` | Platform | REST API host |
| `TrackPlan*/` | Domain | Track models, geometry, editor |

## Key Interfaces

- `IZ21`: ConnectAsync, SetLocomotiveSpeedAsync, FeedbackReceived event
- `IActionExecutor`: ExecuteActionAsync(WorkflowAction, ExecutionContext)
- `ISpeakerEngine`: SpeakAsync, StopAsync
- `IIoService`: LoadAsync, SaveAsync

## Data Flow

Z21 UDP → IZ21.FeedbackReceived → MainWindowViewModel → Journey.HandleFeedback → WorkflowService → UI Update

## Conventions

- Async suffix on all async methods
- Constructor injection (no service locator)
- `[ObservableProperty]`, `[RelayCommand]` attributes
- `ArgumentNullException.ThrowIfNull()` for null checks
- `.ConfigureAwait(false)` in library code

## Configuration & Paths

| Artifact | Location | Format |
|----------|----------|--------|
| Solution | User-selected | `.mobaflow` (JSON) |
| Settings | `MOBAflow/appsettings.json` | JSON |
| Logs | `bin/Debug/logs/mobaflow-*.log` | Rolling text |
| Plugins | `MOBAflow/bin/Debug/Plugins/` | DLL |
| Track Libraries | `TrackLibrary.*/` projects | Compiled |

---

## Dependency Injection

All services registered in `MOBAflow/App.xaml.cs` / `MOBAsmart/MauiProgram.cs`:

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

| Feature | MOBAflow | MOBAsmart | MOBApi |
| -------- | -------- | ---------- | ------ |
| UI Framework | WinUI 3 XAML | .NET MAUI XAML | ASP.NET Core |
| Navigation / Surface | NavigationView | Shell | Controllers + SignalR Hub |
| File I/O | FileSavePicker | FilePicker | Server-side |
| Speech | Windows SAPI | Android TTS | Not applicable |
| Z21 Integration | Full UDP | Full UDP | API/status surface |

---

## Key Conventions

1. **Async Suffix**: All async methods end with `Async`
2. **Cancellation**: Pass `CancellationToken` through async chains
3. **Logging**: Use structured logging with `{Property}` placeholders
4. **Null Checks**: Use `ArgumentNullException.ThrowIfNull()`
5. **MVVM**: Use `[ObservableProperty]` and `[RelayCommand]` attributes
6. **DI**: Constructor injection, no service locator pattern
7. **ConfigureAwait**: Use `.ConfigureAwait(false)` in library code
