# MOBAflow Architecture

## 📐 System Overview

MOBAflow is built on **Clean Architecture** principles with a clear
separation of concerns:

```text
┌──────────────────────────────────────────────┐
│                   Presentation Layer         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │MOBAflow  │  │MOBAsmart │  │  MOBApi  │    │
│  │(Windows) │  │(Android) │  │  (REST)  │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
└─────────┼────────────┼────────────┼──────────┘
          │            │            │ 
          └────────────┼────────────┼
                       │            │
          ┌────────────┴────────────┴────────────┐
          │        Presentation Layer            │
          │     (SharedUI ViewModels)            │
          │  MVVM, Commands, Observable Props    │
          └────────────┬────────────┬────────────┘
                       │            │
┌──────────────────────┴────────────┴─────────────────────┐
│              Domain & Business Logic Layer              │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Domain    │  │   Backend    │  │  TrackPlan   │     │
│  │ (Models)   │  │  (Services)  │  │  (Geometry)  │     │
│  └────────────┘  └──────────────┘  └──────────────┘     │
└──────────────────────┬──────────────────────────────────┘
                       │
            ┌──────────┴──────────┐
            │                     │
        ┌───────────┐        ┌────────────┐
        │ External  │        │ Logging &  │
        │ Services  │        │ Config     │
        │ (Z21 UDP) │        │ (Serilog)  │
        └───────────┘        └────────────┘
```

## 🏗️ Architecture Layers

### 1. **Domain Layer** (`Domain/`)

**Purpose:** Pure business logic, independent from UI or infrastructure.

**Content:**

- POCO Models (Solution, Journey, Workflow, FeedbackPoint, etc.)
- Domain Events & Aggregates
- Business Rules & Validation Logic

**Key Classes:**

```csharp
// Immutable domain models
public record Solution(string Name, List<Project> Projects, ...);
public record Journey(int Id, string Name, List<Station> Stations, ...);
public record Workflow(int Id, string Name, List<WorkflowAction> Actions, ...);
```

**Characteristics:**

- ✅ **Framework-agnostic** - No dependencies on UI frameworks
- ✅ **Testable** - Unit tests without mocking UI
- ✅ **Reusable** - Shared across all platforms (MOBAflow, MOBAsmart, MOBApi)
- ✅ **Serializable** - JSON for configuration files

---

### 2. **Backend/Service Layer** (`Backend/`, `Common/`)

**Purpose:** Application services, coordination, external integrations.

**Content:**

- IZ21 (Z21 Control Station Communication)
- IMobaRuntime / MobaRuntimeService (authoritative runtime owner)
- WorkflowService (Action Execution)
- ActionExecutor (Action Implementation)
- ProjectRuntimeFactory / ActiveProjectContext (runtime project activation)
- Runtime Snapshots (MobaRuntimeSnapshot, JourneyRuntimeSnapshot)
- Configuration & Settings Management
- Logging Infrastructure (Serilog)

**Key Interfaces:**

```csharp
public interface IZ21
{
    Task ConnectAsync(string ipAddress);
    Task SetTrackPowerAsync(bool on);
    Task SetLocomotiveSpeedAsync(int address, int speed);
    event EventHandler<FeedbackReceivedEventArgs> FeedbackReceived;
}

public interface IMobaRuntime
{
    MobaRuntimeSnapshot Current { get; }
    Task ActivateProjectAsync(Project editableProject);
    Task ConnectAsync();
    Task SetTrackPowerAsync(bool isOn);
}

public interface IActionExecutor
{
    Task ExecuteActionAsync(WorkflowAction action, ExecutionContext context);
}
```

**Characteristics:**

- ✅ **UDP Communication** - Direct Z21 protocol (no HTTP/REST dependencies)
- ✅ **Async-First** - All I/O operations use async/await
- ✅ **DI-Friendly** - Registered in ServiceCollection
- ✅ **Logging** - Structured logging with Serilog

---

### 3. **Presentation Layer** (`SharedUI/`)

**Purpose:** Shared ViewModels, MVVM infrastructure, cross-platform utilities.

**Content:**

- MainWindowViewModel (App State Management)
- Page-specific ViewModels (JourneyViewModel, WorkflowViewModel, etc.)
- IMobaClient / InProcessMobaClient (UI-facing runtime access)
- MVVM Commands & Converters
- Observable Property Definitions

**Key Pattern:**

```csharp
// Shared ViewModel consuming runtime snapshots
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IMobaClient _mobaClient;
    private readonly Solution _solution;
    
    [ObservableProperty]
    private bool isConnected;
    
    [RelayCommand]
    private async Task ConnectAsync()
    {
        await _mobaClient.ConnectAsync();
    }

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        IsConnected = snapshot.IsConnected;
    }
}
```

**Characteristics:**

- ✅ **Platform-Agnostic** - Used by MOBAflow, MOBAsmart, and MOBApi
- ✅ **MVVM Toolkit** - CommunityToolkit.Mvvm for source generators
- ✅ **Observable Properties** - Reactive UI updates
- ✅ **Commands** - RelayCommand for user interactions

---

### Runtime Boundary (Current Refactoring State)

The runtime split now covers the shared shell and the remaining
Z21-driven shared ViewModels:

```text
MainWindowViewModel
    ↓
IMobaClient
    ↓
IMobaRuntime
    ↓
IZ21 / JourneyManager / WorkflowService
```

**Current responsibilities of `IMobaRuntime`:**

- Owns Z21 connection state, auto-connect, disconnect and track power
- Owns active project execution via `ActiveProjectContext`
- Owns `JourneyManager`, fail-safe latching and traffic monitor access
- Publishes immutable runtime projections via `MobaRuntimeSnapshot`
  including current system and locomotive state

**Current responsibilities of `IMobaClient`:**

- Provides a UI-safe access point for commands, runtime snapshots,
  locomotive control and feedback events
- Hides whether the runtime is in-process or remote
- Uses `InProcessMobaClient` today; a remote client can replace it
  later without changing the ViewModels again

**Current migration status:**

- MainWindowViewModel now consumes IMobaClient instead of
  orchestrating Z21 and JourneyManager directly
- JourneyViewModel receives runtime state from snapshots instead of
  directly from the runtime manager
- TrainControlViewModel now routes locomotive drive/function/info
  handling through IMobaClient and consumes runtime snapshots for
  connection/system state
- MauiViewModel now routes connect/disconnect/track-power through
  IMobaClient and consumes runtime snapshots plus feedback events

**Important transition note:**

- `ProjectRuntimeFactory` currently still reuses the live `Project`
  reference from the loaded `Solution`
- This is still an intentionally incremental refactoring step to keep
  the migration safe
- The next architectural step is a true runtime copy so editor state
  and execution state are fully separated

---

#### Threading und UI-Thread-Grenze

**Warum gibt es überhaupt einen Dispatcher?**

- Backend und Dienste (Z21, Datei-I/O, Timer, Post-Startup) laufen auf **Hintergrund-Threads**.
- Das **EventBus** ruft Handler **auf dem Thread des Aufrufers** auf:
  `Publish` führt alle Subscriber synchron aus. Ruft also z. B. Z21
  aus einem Thread-Pool-Thread `Publish` auf, laufen die
  ViewModel-Handler auf diesem Hintergrund-Thread und ändern
  Observable-Properties → Verstöße gegen „UI-Updates nur auf dem
  UI-Thread“ und potenzielle COMException in WinUI.

**Saubere Architektur-Lösung:**

- **Eine zentrale Marshalling-Stelle:** Statt in jedem ViewModel
  `IUiDispatcher.InvokeOnUi` um jeden Event-Handler zu wickeln,
  marshalieren wir an der **EventBus-Grenze**. Ein
  `UiThreadEventBusDecorator` implementiert `IEventBus` und leitet
  `Publish` so weiter, dass alle Handler auf dem UI-Thread ausgeführt
  werden (über `IUiDispatcher.InvokeOnUi`). Dann müssen ViewModels für
  **EventBus-Subscriptions** den Dispatcher nicht mehr kennen.
- **Verbleibende Dispatcher-Nutzung:** Direkte Event-Quellen, die
  **nicht** über das EventBus laufen (z. B. `IZ21.Received`,
  `IZ21.OnConnectionLost`, async Datei-Lade-Completion,
  Post-Startup-Status), müssen weiterhin an einer Stelle auf den
  UI-Thread marshalieren – entweder in einem dünnen Adapter/Bridge,
  der nur dispatcht und dann das ViewModel aufruft, oder (derzeit) im
  ViewModel mit `IUiDispatcher`. Ziel ist, diese Fälle langfristig
  entweder über das EventBus zu führen (dann deckt der Decorator sie
  ab) oder in einem einzigen „UI-Bridge“-Service zu bündeln.

**MVVM-Konsequenz:**

- ViewModels sollen keine Thread-Logik enthalten; die Grenze
  „Hintergrund → UI“ gehört in eine **einzige** Schicht
  (EventBus-Decorator bzw. UI-Bridge). Dann bleibt der
  Dispatcher-Service eine technische Plattform-Detail-Implementierung,
  die an genau dieser Grenze verwendet wird, nicht in jedem ViewModel.

**Umsetzung (Stand):**

- **WinUI:** `AddEventBusWithUiDispatch()` bleibt fuer klassische
  UI-Events aktiv. `MainWindowViewModel` nutzt weiterhin EventBus-
  und View-basierte Statuspfade; `TrainControlViewModel` bezieht
  Z21-nahe Laufzeitdaten jetzt ueber `IMobaClient`-Snapshots und
  Runtime-Events statt direkt ueber EventBus-Subscriptions.
- **Verbleibende Dispatcher-Nutzung:** Dort, wo Snapshot-/Runtime-
  Events oder View-Callbacks außerhalb des UI-Threads ankommen:
  z. B. Datei-Lade-Callbacks (Solution laden),
  Health-Status-Updates aus der View (`MainWindow.xaml.cs`) und
  Runtime-Projection in `TrainControlViewModel`/`MauiViewModel`.
  Diese Fälle können später weiter in dedizierte UI-Bridges
  verschoben werden.

---

### 4. **Platform-Specific Layers** (`MOBAflow/`, `MOBAsmart/`, `MOBApi/`)

**Purpose:** UI rendering, platform-specific features, page and API definitions.

**MOBAflow (Windows Desktop, WinUI 3):**

```text
MOBAflow/
├── View/               # XAML Pages (MainWindow, JourneyPage, etc.)
├── ViewModel/          # WinUI-specific ViewModels
├── Service/            # WinUI Services (NavigationService, etc.)
├── Converter/          # XAML Value Converters
└── Resources/          # Styles, Brushes, Templates
```

**MOBAsmart (Android, .NET MAUI):**

```text
MOBAsmart/
├── View/               # XAML Pages (MainPage, etc.)
├── Resources/          # Styles, Colors, Fonts
├── Platforms/          # Platform-specific code (Permissions, etc.)
└── Service/            # MAUI Services
```

**MOBApi (REST API, ASP.NET Core):**

```text
MOBApi/
├── Controllers/        # REST API Controllers
├── Hubs/               # SignalR Hubs
└── Service/            # API Services
```

---

## 🔄 Data Flow

### Z21 Feedback Flow

```text
Z21 Station (UDP broadcast on Port 21105)
  ↓ (UDP packet)
IZ21.ReceiveFeedback()
  ↓
IMobaRuntime
  ├─ JourneyManager processes feedback
  ├─ Runtime state is updated
  └─ MobaRuntimeSnapshot is published
  ↓
IMobaClient.SnapshotChanged
  ↓
MainWindowViewModel.ApplyRuntimeSnapshot()
  ↓
JourneyViewModel.UpdateFromRuntimeSnapshot()
  └─ MOBAflow/MOBAsmart re-render
```

### Command Execution Flow

```text
User clicks Button (MOBAflow/MOBAsmart)
  ↓
[RelayCommand] -> ViewModel Method
  ↓
IMobaClient.ExecuteCommand()
  ↓
IMobaRuntime
  ↓
ActionExecutor.ExecuteActionAsync()
  ├─ Set Track Power
  ├─ Control Locomotive
  ├─ Play Sound
  └─ Speak Text
  ↓
IZ21 (UDP) or External Service
  ↓
Result -> UI State Update
  └─ ObservableProperty change
```

---

## 📦 Dependency Injection Container

The DI container (`Microsoft.Extensions.DependencyInjection`) is the
heart of the architecture:

### Registration Structure

```csharp
// App.xaml.cs / Program.cs
var services = new ServiceCollection();

// Domain/Backend Services
services.AddSingleton<Solution>();           // Domain
services.AddSingleton<IZ21>(/* z21 impl */); // Backend Service
services.AddSingleton<IMobaRuntime, MobaRuntimeService>();
services.AddSingleton<WorkflowService>();    // Business Logic

// Presentation Layer
services.AddSingleton<IMobaClient, InProcessMobaClient>();
services.AddSingleton<MainWindowViewModel>(); // Shared ViewModel
services.AddTransient<JourneyPage>();        // Platform-specific Pages

// Platform Services
services.AddSingleton<NavigationService>();  // WinUI Navigation
services.AddSingleton<IIoService, IoService>(); // File Operations

// Plugins
var pluginLoader = new PluginLoader(...);
pluginLoader.LoadPluginsAsync(services);      // Discover & Register

var provider = services.BuildServiceProvider();
```

### Resolution Example

```csharp
// Request: MainWindowViewModel
provider.GetRequiredService<MainWindowViewModel>()

// DI Container resolves:
MainWindowViewModel(
    IMobaClient mobaClient,      // → Singleton instance
    IEventBus eventBus,          // → Singleton instance
    Solution solution,           // → Singleton instance
    ...
)
```

---

## 🛡️ Error Handling & Robustness

### Graceful Degradation

```text
If Z21 Connection Fails:
  • App still starts
  • UI shows "Disconnected" status
  • Commands are disabled
  • No actions can execute
    
If DLL is corrupted:
  • Error logged
  • Next DLL is attempted
  • App always runs
```

### Logging Strategy

```text
Critical: App won't start
  └─ DI setup failure
  └─ Configuration corruption

Error: Feature won't work
  └─ Z21 connection lost
  └─ Plugin DLL corrupt

Warning: Unexpected but recoverable
  └─ Duplicate page tag in plugin
  └─ Missing configuration value

Info: Normal operations
  └─ Z21 connected
  └─ Plugin loaded
  └─ Command executed

Debug: Diagnostic info
  └─ Property changed
  └─ Event fired
  └─ Service method called
```

---

## 🔗 Layer Communication

### How Layers Interact

```text
User Input (UI Layer)
  ↓
ViewModel Command (Presentation Layer)
  ↓
Service Method (Backend Layer)
  ↓
Domain Model Logic (Domain Layer)
  ↓
External Integration (Z21 UDP)
  ↓
Result back up the stack
  ↓
Observable Property Update
  ↓
UI Re-renders
```

---

## 📊 Technology Decisions

- **UI:** WinUI 3 (MOBAflow), .NET MAUI (MOBAsmart)
  - **Why:** Native look & feel, platform-specific features
- **API:** ASP.NET Core REST + SignalR (MOBApi)
  - **Why:** Lightweight REST + real-time hub
- **MVVM:** CommunityToolkit.Mvvm
  - **Why:** Source generators, zero-reflection overhead
- **DI:** Microsoft.Extensions.DependencyInjection
  - **Why:** Standard .NET DI, no external dependencies
- **Logging:** Serilog
  - **Why:** Structured, extensible, file + in-memory sinks
- **Z21 Protocol:** UDP (direct)
  - **Why:** Low latency, no external dependencies
- **Testing:** NUnit
  - **Why:** Simple, focused unit tests

---

## 🚀 Future Extensibility

The architecture supports:

- ✅ **New UI Platforms** - Just implement UI layer above SharedUI
- ✅ **New Services** - Add to Backend, register in DI
- ✅ **New Plugins** - Drop DLL in Plugins folder
- ✅ **Protocol Upgrades** - Encapsulated in IZ21
- ✅ **Configuration Expansion** - AppSettings extensible
- ✅ **Domain Evolution** - Models can change independently

---

**Last Updated:** March 2026
