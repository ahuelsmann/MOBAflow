# MOBAflow Architecture

## 📐 System Overview

MOBAflow is built on **Clean Architecture** principles with a clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                   Presentation Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │  WinUI   │  │  MAUI    │  │  Blazor  │  │  Plugins │    │
│  │(Windows) │  │(Android) │  │  (Web)   │  │(Dynamic) │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
└─────────┼────────────┼────────────┼────────────┼────────────┘
          │            │            │            │
          └────────────┼────────────┼────────────┘
                       │            │
          ┌────────────┴────────────┴────────────┐
          │        Presentation Layer            │
          │     (SharedUI ViewModels)            │
          │  MVVM, Commands, Observable Props   │
          └────────────┬────────────┬────────────┘
                       │            │
┌──────────────────────┴────────────┴──────────────────────┐
│              Domain & Business Logic Layer               │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Domain    │  │   Backend    │  │  TrackPlan   │     │
│  │ (Models)   │  │  (Services)  │  │  (Geometry)  │     │
│  └────────────┘  └──────────────┘  └──────────────┘     │
└──────────────────────┬────────────────────────────────────┘
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
- ✅ **Reusable** - Shared across all platforms (WinUI, MAUI, Blazor)
- ✅ **Serializable** - JSON for configuration files

---

### 2. **Backend/Service Layer** (`Backend/`, `Common/`)

**Purpose:** Application services, coordination, external integrations.

**Content:**
- IZ21 (Z21 Control Station Communication)
- WorkflowService (Action Execution)
- ActionExecutor (Action Implementation)
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
- MVVM Commands & Converters
- Observable Property Definitions

**Key Pattern:**
```csharp
// Shared ViewModel (works on all platforms)
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly Solution _solution;
    
    [ObservableProperty]
    private bool isConnected;
    
    [RelayCommand]
    private async Task ConnectAsync()
    {
        IsConnected = await _z21.ConnectAsync(...);
    }
}
```

**Characteristics:**
- ✅ **Platform-Agnostic** - Used by WinUI, MAUI, and Blazor
- ✅ **MVVM Toolkit** - CommunityToolkit.Mvvm for source generators
- ✅ **Observable Properties** - Reactive UI updates
- ✅ **Commands** - RelayCommand for user interactions

---

#### Threading und UI-Thread-Grenze

**Warum gibt es überhaupt einen Dispatcher?**

- Backend und Dienste (Z21, Datei-I/O, Timer, Post-Startup) laufen auf **Hintergrund-Threads**.
- Das **EventBus** ruft Handler **auf dem Thread des Aufrufers** auf: `Publish` führt alle Subscriber synchron aus. Ruft also z. B. Z21 aus einem Thread-Pool-Thread `Publish` auf, laufen die ViewModel-Handler auf diesem Hintergrund-Thread und ändern Observable-Properties → Verstöße gegen „UI-Updates nur auf dem UI-Thread“ und potenzielle COMException in WinUI.

**Saubere Architektur-Lösung:**

- **Eine zentrale Marshalling-Stelle:** Statt in jedem ViewModel `IUiDispatcher.InvokeOnUi` um jeden Event-Handler zu wickeln, marshalieren wir an der **EventBus-Grenze**. Ein `UiThreadEventBusDecorator` implementiert `IEventBus` und leitet `Publish` so weiter, dass alle Handler auf dem UI-Thread ausgeführt werden (über `IUiDispatcher.InvokeOnUi`). Dann müssen ViewModels für **EventBus-Subscriptions** den Dispatcher nicht mehr kennen.
- **Verbleibende Dispatcher-Nutzung:** Direkte Event-Quellen, die **nicht** über das EventBus laufen (z. B. `IZ21.Received`, `IZ21.OnConnectionLost`, async Datei-Lade-Completion, Post-Startup-Status), müssen weiterhin an einer Stelle auf den UI-Thread marshalieren – entweder in einem dünnen Adapter/Bridge, der nur dispatcht und dann das ViewModel aufruft, oder (derzeit) im ViewModel mit `IUiDispatcher`. Ziel ist, diese Fälle langfristig entweder über das EventBus zu führen (dann deckt der Decorator sie ab) oder in einem einzigen „UI-Bridge“-Service zu bündeln.

**MVVM-Konsequenz:**

- ViewModels sollen keine Thread-Logik enthalten; die Grenze „Hintergrund → UI“ gehört in eine **einzige** Schicht (EventBus-Decorator bzw. UI-Bridge). Dann bleibt der Dispatcher-Service eine technische Plattform-Detail-Implementierung, die an genau dieser Grenze verwendet wird, nicht in jedem ViewModel.

**Umsetzung (Stand):**

- **WinUI:** `AddEventBusWithUiDispatch()` – alle EventBus-Handler laufen auf dem UI-Thread. Z21 publiziert u. a. `Z21ConnectionEstablishedEvent`, `Z21ConnectionLostEvent`, `FeedbackReceivedEvent`; PostStartupInitializationService publiziert `PostStartupStatusEvent`. MainWindowViewModel und TrainControlViewModel nutzen EventBus-Subscriptions für ihre UI-relevanten Statusänderungen.
- **Verbleibende Dispatcher-Nutzung:** Nur noch dort, wo kein EventBus genutzt wird: z. B. asynchrone Datei-Lade-Callbacks (Solution laden), Health-Status-Updates aus der View (MainWindow.xaml.cs), Settings-Callbacks, JourneyManager-StationChanged. Diese können bei Bedarf ebenfalls auf Events umgestellt werden.

---

### 4. **Platform-Specific Layers** (`WinUI/`, `MAUI/`, `WebApp/`)

**Purpose:** UI rendering, platform-specific features, page definitions.

**WinUI (Windows Desktop):**
```
WinUI/
├── View/               # XAML Pages (MainWindow, JourneyPage, etc.)
├── ViewModel/          # WinUI-specific ViewModels
├── Service/            # WinUI Services (NavigationService, etc.)
├── Converter/          # XAML Value Converters
└── Resources/          # Styles, Brushes, Templates
```

**MAUI (Android):**
```
MAUI/
├── Pages/              # XAML Pages (MainPage, etc.)
├── Resources/          # Styles, Colors, Fonts
├── Platforms/          # Platform-specific code (Permissions, etc.)
└── Services/           # MAUI Services (Camera, Location, etc.)
```

**WebApp (Blazor):**
```
WebApp/
├── Pages/              # Blazor Components (.razor)
├── Services/           # Backend Services
├── Shared/             # Shared Components
└── wwwroot/            # Static Assets
```

---

## 🔄 Data Flow

### Z21 Feedback Flow

```
Z21 Station (UDP broadcast on Port 21105)
  ↓ (UDP packet)
IZ21.ReceiveFeedback()
  ↓
[FeedbackReceivedEventArgs]
  ├─ InPort (track feedback address)
  └─ Value (occupied = 1, free = 0)
  ↓
MainWindowViewModel.OnFeedbackReceived()
  ↓
Journey.HandleFeedback()
  ├─ Check if station matches
  └─ Execute associated actions
  ↓
WorkflowService.ExecuteWorkflow()
  ├─ Execute sequence of actions
  ├─ Text-to-Speech Announcements
  └─ Update UI state
  ↓
UI Updates (via Observable Properties)
  └─ WinUI/MAUI/Blazor re-render
```

### Command Execution Flow

```
User clicks Button (WinUI/MAUI/Blazor)
  ↓
[RelayCommand] -> ViewModel Method
  ↓
MainWindowViewModel.ExecuteCommand()
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

The DI container (`Microsoft.Extensions.DependencyInjection`) is the heart of the architecture:

### Registration Structure

```csharp
// App.xaml.cs / Program.cs
var services = new ServiceCollection();

// Domain/Backend Services
services.AddSingleton<Solution>();           // Domain
services.AddSingleton<IZ21>(/* z21 impl */); // Backend Service
services.AddSingleton<WorkflowService>();    // Business Logic

// Presentation Layer
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
    IZ21 z21,                    // → Singleton instance
    WorkflowService workflow,    // → Singleton instance
    Solution solution,           // → Singleton instance
    MainWindowViewModel parent   // → Circular? No! Resolved once
)
```

---

## 🛡️ Error Handling & Robustness

### Graceful Degradation

```
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

```
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

## 📐 Pattern Usage

### MVVM Pattern

All ViewModels follow MVVM with CommunityToolkit.Mvvm:

```csharp
public sealed partial class MyViewModel : ObservableObject
{
    [ObservableProperty]           // Auto INotifyPropertyChanged
    private string title = "...";
    
    [RelayCommand]                  // Auto ICommand implementation
    private async Task DoAction()
    {
        Title = "Updated";          // Notification sent automatically
    }
}
```

### Async-Everywhere Pattern

All I/O operations are async:

```csharp
// ✅ CORRECT: Async all the way
public async Task LoadDataAsync()
{
    var data = await _service.FetchAsync();
    UpdateUI(data);
}

// ❌ WRONG: Synchronous I/O
public void LoadData()
{
    var data = _service.Fetch();  // Blocks UI thread!
}
```

### Dependency Injection Pattern

All dependencies injected via constructor:

```csharp
// ✅ CORRECT: Explicit dependencies
public MyViewModel(IZ21 z21, WorkflowService workflow)
{
    _z21 = z21;
    _workflow = workflow;
}

// ❌ WRONG: Service Locator
public MyViewModel()
{
    _z21 = ServiceLocator.Get<IZ21>();  // Hidden dependencies!
}
```

---

## 🔗 Layer Communication

### How Layers Interact

```
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

| Layer | Technology | Why |
|-------|-----------|-----|
| **UI** | WinUI 3, MAUI, Blazor | Native look & feel, platform-specific features |
| **MVVM** | CommunityToolkit.Mvvm | Source generators, zero-reflection overhead |
| **DI** | Microsoft.Extensions.DependencyInjection | Standard .NET DI, no external dependencies |
| **Logging** | Serilog | Structured, extensible, file + in-memory sinks |
| **Z21 Protocol** | UDP (direct) | Low latency, no external dependencies |
| **Testing** | NUnit | Simple, focused unit tests |

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

**Last Updated:** February 2026