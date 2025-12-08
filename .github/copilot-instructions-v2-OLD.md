# MOBAflow - Consolidated Copilot Instructions

> **Multi-platform railway automation control system (.NET 10)**
> - MOBAflow (WinUI) - Desktop control center
> - MOBAsmart (MAUI) - Android mobile app
> - MOBAdash (Blazor) - Web dashboard

---

## 🎯 Primary Analysis Guidelines

### When Analyzing Code or Proposing Changes:

1. **❗ Check for Aggregat-Design Problems**
   - ❌ **ANTI-PATTERN**: Nested objects in Domain (e.g., `Journey.Stations = List<Station>`)
   - ❌ **ANTI-PATTERN**: Circular references (e.g., `Journey.NextJourney → Journey`)
   - ❌ **ANTI-PATTERN**: Redundant storage (entity in multiple lists)
   - ✅ **CORRECT**: GUID references only (e.g., `Journey.StationIds = List<Guid>`)
   - ✅ **CORRECT**: Single Source of Truth in Project aggregate root
   - ✅ **CORRECT**: ViewModels resolve references at runtime

2. **🏗️ Verify Clean Architecture Layers**
   - Domain = Pure POCOs (no framework dependencies)
   - Backend = Platform-independent (no UI thread code)
   - ViewModels = Resolve references, handle UI threading
   - Platform = UI-specific code only

3. **📚 Consult Specific Instructions**
   - Backend rules: `.github/instructions/backend.instructions.md`
   - WinUI patterns: `.github/instructions/winui.instructions.md`
   - MAUI patterns: `.github/instructions/maui.instructions.md`
   - Blazor patterns: `.github/instructions/blazor.instructions.md`
   - Test patterns: `.github/instructions/test.instructions.md`
   - State management: `.github/instructions/hasunsavedchanges-patterns.instructions.md`

---

## 🏗️ Core Architecture

### Clean Architecture Layers
```
Domain (Pure POCOs)
  ↑
Backend (Platform-independent logic)
  ↑
SharedUI (Base ViewModels)
  ↑
WinUI / MAUI / Blazor (Platform-specific)
```

**Critical Rules:**
- ✅ **Domain:** Pure POCOs - NO attributes, NO INotifyPropertyChanged, NO logic
- ✅ **Backend:** Platform-independent - NO DispatcherQueue, NO MainThread
- ✅ **SharedUI:** ViewModels with CommunityToolkit.Mvvm
- ✅ **Platform:** UI-specific code only

### Reference-Based Domain Architecture (Dec 2025)

**Principle**: Domain objects use **GUID references** instead of nested object trees.

#### Aggregate Root Pattern
```csharp
public class Project  // ✅ Aggregate Root
{
    // Master lists (Single Source of Truth)
    public List<Locomotive> Locomotives { get; set; }
    public List<PassengerWagon> PassengerWagons { get; set; }
    public List<GoodsWagon> GoodsWagons { get; set; }
    public List<Station> Stations { get; set; }        // ✅ NEW (Dec 2025)
    public List<Workflow> Workflows { get; set; }
    public List<Journey> Journeys { get; set; }
    public List<Train> Trains { get; set; }
}
```

#### Reference Properties (Not Nested Objects!)
```csharp
// ❌ OLD: Nested objects (circular refs, JSON hell)
public class Journey
{
    public List<Station> Stations { get; set; }      // ❌ Nested objects
    public Journey? NextJourney { get; set; }         // ❌ Circular reference!
}

// ✅ NEW: GUID references (flat, clean JSON)
public class Journey
{
    public List<Guid> StationIds { get; set; }       // ✅ Only IDs
    public Guid? NextJourneyId { get; set; }          // ✅ Only ID
}
```

#### Resolution in ViewModels
```csharp
public class JourneyViewModel
{
    private readonly Journey _journey;
    private readonly Project _project;  // ✅ Project for lookups
    
    // Resolved at runtime
    public ObservableCollection<StationViewModel> Stations =>
        _journey.StationIds
            .Select(id => _project.Stations.FirstOrDefault(s => s.Id == id))
            .Where(s => s != null)
            .Select(s => new StationViewModel(s, _project))
            .ToObservableCollection();
}
```

#### Key Rules
- ✅ **All entities have `Guid Id`** (Station, Locomotive, Wagon, Train)
- ✅ **Domain uses only IDs** (StationIds, LocomotiveIds, WorkflowId, NextJourneyId)
- ✅ **ViewModels resolve references** via `_project.XXX.FirstOrDefault(x => x.Id == id)`
- ✅ **No custom JSON converters** (GUIDs serialize natively)
- ❌ **NEVER store nested objects** in Domain (breaks serialization, creates circular refs)

---

## 🎯 MVVM Best Practices

### Rule 1: Minimize Code-Behind
```csharp
// ❌ WRONG: Logic in code-behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    ViewModel.Property = value;
    ViewModel.DoSomething();
}

// ✅ CORRECT: Command binding in XAML
<Button Command="{x:Bind ViewModel.DoSomethingCommand}" />
```

### Rule 2: Use Property Changed Notifications
```csharp
// ✅ CORRECT: CommunityToolkit.Mvvm
[ObservableProperty]
private string name;

partial void OnNameChanged(string value)
{
    // Side effects here (NOT in code-behind!)
    UpdateRelatedProperty();
}
```

### Rule 3: Acceptable Code-Behind
```csharp
// ✅ ACCEPTABLE:
- Constructor with DI injection
- Window lifecycle events (delegating to ViewModel)
- Platform-specific UI code (Window.SetTitleBar, etc.)
- Simple event handlers for drag-and-drop (XAML limitation)

// ❌ NEVER:
- Business logic
- Command execution
- Data manipulation
- State management
```

---

## 💉 Dependency Injection

### Service Registration Pattern
```csharp
// WinUI/App.xaml.cs
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
services.AddSingleton<IUiDispatcher, WinUIDispatcher>();
services.AddSingleton<Solution>();
services.AddSingleton<MainWindowViewModel>();

// Pages receive MainWindowViewModel via constructor
services.AddTransient<EditorPage1>();
```

**Lifetime Rules:**
- **Singleton:** Application state, hardware abstraction (IZ21, Solution)
- **Transient:** Pages, disposable services
- **Scoped:** Blazor only (per-request state)

---

## 📁 File Organization

### Namespace Rules
```
Backend/Manager/JourneyManager.cs    → namespace Moba.Backend.Manager;
WinUI/View/EditorPage1.xaml.cs      → namespace Moba.WinUI.View;
SharedUI/ViewModel/JourneyViewModel  → namespace Moba.SharedUI.ViewModel;
```

**One Class Per File:** `JourneyManager.cs` contains ONLY `class JourneyManager`

---

## 🔄 JSON Serialization

### StationConverter Pattern
```csharp
// Domain/Station.cs - PURE POCO
public class Station
{
    public string Name { get; set; }
    public Workflow? Flow { get; set; }        // ← Navigation property
    public Guid? WorkflowId { get; set; }      // ← Foreign key
}

// Backend/Converter/StationConverter.cs
public override void WriteJson(...)
{
    // Serialize WorkflowId instead of Flow (prevent circular refs)
    writer.WritePropertyName("WorkflowId");
    serializer.Serialize(writer, value.Flow.Id);
}
```

**Key Principle:** Domain stays pure, converters handle serialization logic.

---

## 🔄 SessionState Pattern (Dec 2025)

**Principle:** Separate runtime state from domain objects to keep Domain pure.

### Architecture
```
Domain (Pure POCOs)       Backend (SessionState)       SharedUI (ViewModels)
Journey { Name, Stations } → JourneySessionState → JourneyViewModel
                            { Counter, CurrentPos,    reads from SessionState
                              CurrentStationName }
```

### Implementation

#### Backend/Services/JourneySessionState.cs
```csharp
public class JourneySessionState
{
    public Guid JourneyId { get; set; }
    public int Counter { get; set; }
    public int CurrentPos { get; set; }
    public string CurrentStationName { get; set; } = string.Empty;
    public DateTime? LastFeedbackTime { get; set; }
    public bool IsActive { get; set; } = true;
}
```

#### Backend/Manager/JourneyManager.cs
```csharp
public class JourneyManager : BaseFeedbackManager<Journey>
{
    private readonly Dictionary<Guid, JourneySessionState> _states = [];
    
    public event EventHandler<StationChangedEventArgs>? StationChanged;
    
    private async Task HandleFeedbackAsync(Journey journey)
    {
        var state = _states[journey.Id];  // ✅ Get SessionState
        state.Counter++;                   // ✅ Modify SessionState
        
        // Fire event for ViewModels
        OnStationChanged(new StationChangedEventArgs { 
            JourneyId = journey.Id, 
            SessionState = state 
        });
    }
    
    public JourneySessionState? GetState(Guid journeyId) 
        => _states.GetValueOrDefault(journeyId);
}
```

#### SharedUI/ViewModel/JourneyViewModel.cs
```csharp
public class JourneyViewModel : ObservableObject
{
    private readonly Journey _journey;           // Domain
    private readonly JourneySessionState _state; // Runtime
    private readonly JourneyManager _manager;
    
    public JourneyViewModel(Journey journey, JourneySessionState state, 
                           JourneyManager manager, IUiDispatcher dispatcher)
    {
        _journey = journey;
        _state = state;
        _manager = manager;
        
        // Subscribe to manager events
        _manager.StationChanged += OnStationChanged;
    }
    
    // Domain properties (setters modify domain)
    public string Name 
    { 
        get => _journey.Name; 
        set => SetProperty(_journey.Name, value, _journey, (m, v) => m.Name = v);
    }
    
    // SessionState properties (read-only from ViewModel)
    public int CurrentCounter => _state.Counter;
    public int CurrentPos => _state.CurrentPos;
    public string CurrentStation => _state.CurrentStationName;
    
    private void OnStationChanged(object? sender, StationChangedEventArgs e)
    {
        if (e.JourneyId != _journey.Id) return;
        _dispatcher.InvokeOnUi(() => {
            OnPropertyChanged(nameof(CurrentCounter));
            OnPropertyChanged(nameof(CurrentPos));
            OnPropertyChanged(nameof(CurrentStation));
        });
    }
}
```

### Factory Pattern for Creation
```csharp
// MainWindowViewModel.Journey.cs
private JourneyViewModel CreateJourneyViewModel(Journey journey)
{
    if (_journeyManager == null)
        return new JourneyViewModel(journey, _uiDispatcher); // Fallback for tests
    
    var state = _journeyManager.GetState(journey.Id);
    if (state == null)
        return new JourneyViewModel(journey, _uiDispatcher); // Journey not yet in manager
    
    return new JourneyViewModel(journey, state, _journeyManager, _uiDispatcher);
}
```

### Testing
```csharp
[Test]
public void JourneyViewModel_ReflectsSessionStateChanges()
{
    var journey = new Journey { Id = Guid.NewGuid() };
    var state = new JourneySessionState { Counter = 5, CurrentPos = 1 };
    var vm = new JourneyViewModel(journey, state);
    
    Assert.That(vm.CurrentCounter, Is.EqualTo(5));
    Assert.That(vm.CurrentPos, Is.EqualTo(1));
}
```

### Rules
- ✅ **Domain:** Pure POCOs, NO runtime state (Counter, CurrentPos)
- ✅ **Backend:** SessionState managed by Managers (JourneyManager)
- ✅ **ViewModels:** Read from SessionState, subscribe to Manager events
- ❌ **NEVER:** Put runtime state in Domain objects
- ❌ **NEVER:** Modify SessionState from ViewModel (read-only)

---

## 🎨 WinUI 3 EditorPage Pattern (Dec 2025)

**Principle:** Use SelectorBar for tabs with multi-column Grids inside each tab.

### Architecture
```
SelectorBar (Solution | Journeys | Workflows | Trains)
    ↓
Content Area (Visibility switching)
    ↓
Each Tab = Multi-Column Grid with multiple ListViews + PropertyGrid
```

### Example: Journeys Tab
```xml
<Grid x:Name="JourneysContent" Visibility="Collapsed">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="250" />  <!-- Journeys -->
        <ColumnDefinition Width="Auto" /> <!-- Separator -->
        <ColumnDefinition Width="250" />  <!-- Stations -->
        <ColumnDefinition Width="Auto" /> <!-- Separator -->
        <ColumnDefinition Width="250" />  <!-- City Library -->
        <ColumnDefinition Width="Auto" /> <!-- Separator -->
        <ColumnDefinition Width="*" />    <!-- PropertyGrid -->
    </Grid.ColumnDefinitions>
    
    <!-- Column 0: Journeys ListView -->
    <ListView ItemsSource="{x:Bind ViewModel.CurrentProjectViewModel.Journeys}" 
              SelectedItem="{x:Bind ViewModel.SelectedJourney, Mode=TwoWay}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="viewmodel:JourneyViewModel">
                <!-- Item content -->
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    
    <!-- Column 2: Stations ListView (sub-items) -->
    <ListView ItemsSource="{x:Bind ViewModel.SelectedJourney.Stations, Mode=OneWay}"
              AllowDrop="True" Drop="StationListView_Drop">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="viewmodel:StationViewModel">
                <!-- Item content -->
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    
    <!-- Column 4: City Library (Drag source) -->
    <ListView ItemsSource="{x:Bind ViewModel.CityLibrary}"
              CanDragItems="True" 
              DragItemsStarting="CityListView_DragItemsStarting">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="domain:City">
                <!-- Item content -->
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    
    <!-- Column 6: PropertyGrid -->
    <local:SimplePropertyGrid SelectedObject="{x:Bind ViewModel.SelectedJourney, Mode=OneWay}" />
</Grid>
```

### Code-Behind Pattern
```csharp
// Visibility switching
private void EditorSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
{
    SolutionContent.Visibility = Visibility.Collapsed;
    JourneysContent.Visibility = Visibility.Collapsed;
    WorkflowsContent.Visibility = Visibility.Collapsed;
    TrainsContent.Visibility = Visibility.Collapsed;

    if (selectedItem == JourneysSelector)
        JourneysContent.Visibility = Visibility.Visible;
    // ... other tabs
}

// Drag & Drop
private void CityListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
{
    if (e.Items.FirstOrDefault() is Domain.City city)
    {
        e.Data.SetData("City", city);
        e.Data.RequestedOperation = DataPackageOperation.Copy;
    }
}

private async void StationListView_Drop(object sender, DragEventArgs e)
{
    if (await e.DataView.GetDataAsync("City") is Domain.City city)
    {
        var station = new Domain.Station { Name = city.Name };
        var stationViewModel = new StationViewModel(station);
        ViewModel.SelectedJourney.Stations.Add(stationViewModel);
    }
}
```

### Critical DataTemplate Rules
- ✅ **Match ViewModel types:**
  - `Journeys` → `<DataTemplate x:DataType="viewmodel:JourneyViewModel">`
  - `Stations` → `<DataTemplate x:DataType="viewmodel:StationViewModel">`
  - `Workflows` → `<DataTemplate x:DataType="viewmodel:WorkflowViewModel">`
  - `Trains` → `<DataTemplate x:DataType="viewmodel:TrainViewModel">`
- ✅ **Use Mode=OneWay** for sub-collections
- ✅ **Drag source:** `CanDragItems="True"`, `DragItemsStarting` event
- ✅ **Drop target:** `AllowDrop="True"`, `DragOver` and `Drop` events

### City Library & Stations
**Important:** `City` and `Station` represent the same concept:
- **City Library** (`germany-stations.json`) contains all available stations/cities
- **Journey.Stations** is the list of stations a train visits on this journey
- **Drag & Drop:** User drags a `City` from City Library and drops it as a new `Station` in a Journey
- **Domain Model:** 
  - `City` has: `Name` (e.g., "Berlin Hbf")
  - `Station` has: `Name`, `InPort`, `Flow` (Workflow reference)
  - When adding City → Station: Create new `Station { Name = city.Name }`
  - Create `StationViewModel` wrapper for UI binding

---

## 🧪 Testing

### Fake Objects for Backend Tests
```csharp
// Test/Fakes/FakeUdpClientWrapper.cs
public class FakeUdpClientWrapper : IUdpClientWrapper
{
    public void SimulateFeedback(int inPort)
    {
        Received?.Invoke(CreateFeedbackPacket(inPort));
    }
}
```

**Never:** Mock hardware in production code. Use abstractions (IZ21, IUdpClientWrapper).

---

## 📊 Current Status (Dec 2025)

| Metric | Value |
|--------|-------|
| Projects | 9 |
| Build Success | ⚠️ IN PROGRESS (64+ errors - refactoring ViewModels) |
| Tests Passing | ⚠️ IN PROGRESS (11+ failing - refactoring in progress) |
| Architecture Violations | 0 |
| SessionState Pattern | ✅ Implemented (JourneyManager) |
| Reference-Based Domain | 🚧 72% complete (Domain + Backend done, ViewModels pending) |

**Active Refactoring**: Domain → Reference-Based Architecture  
**Status**: Domain layer complete, Backend complete, ViewModels in progress  
**See**: `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md`

---

## 🚨 Common Pitfalls

### 1. **Domain Pollution**
```csharp
// ❌ NEVER in Domain:
[JsonConverter(typeof(CustomConverter))]
[Required, StringLength(100)]
public string Name { get; set; }

// ✅ ALWAYS: Pure POCOs
public string Name { get; set; }
```

### 2. **Platform-Specific Code in Backend**
```csharp
// ❌ NEVER in Backend:
#if WINDOWS
    await DispatcherQueue.EnqueueAsync(...);
#endif

// ✅ ALWAYS: Use IUiDispatcher abstraction
await _uiDispatcher.InvokeOnUiAsync(...);
```

### 3. **Code-Behind Logic**
```csharp
// ❌ NEVER:
private void Button_Click(...)
{
    ViewModel.Property = newValue;
}

// ✅ ALWAYS: Command + Property binding
<Button Command="{x:Bind ViewModel.UpdateCommand}"
        CommandParameter="{x:Bind NewValue}" />
```

---

## 🔧 Manager Architecture (Feedback Processing)

### Principle: Different Perspectives on Z21 Feedback Events

MOBAflow processes track feedback from **different perspectives** using specialized Managers:

```
Z21 Feedback (InPort=5)
        ↓
┌───────┼────────┬────────────┐
│       │        │            │
Journey Workflow Station   (Future Managers)
Manager Manager  Manager
```

### 1️⃣ JourneyManager (Train Perspective) ✅ IMPLEMENTED
**Question:** "Where is the **train** right now?"

- **Purpose:** Track train position through stations
- **Entity:** `Journey` (with `Journey.InPort` = train sensor)
- **SessionState:** `JourneySessionState`
  - `Counter` - Lap counter
  - `CurrentPos` - Current station index
  - `CurrentStationName` - Station name
  - `LastFeedbackTime` - Last sensor trigger
  - `IsActive` - Journey active?
- **Event:** `StationChanged` (when train reaches station)
- **Trigger:** Execute `Station.Flow` workflow
- **Future:** Delay tracking (compare `Arrival`/`Departure` times)

**Example:**
```csharp
// Journey.InPort = 5 (train's sensor)
// When InPort=5 triggered → Train reached next station
// → state.Counter++ → Execute Station.Flow
```

### 2️⃣ WorkflowManager (Workflow Perspective) ⏸️ FUTURE
**Question:** "Which **workflow** is currently executing?"

- **Purpose:** Execute workflows **independent** of trains
- **Entity:** `Workflow` (with `Workflow.InPort` = trigger sensor)
- **SessionState:** `WorkflowSessionState` (to be created)
  - `WorkflowId` - Workflow ID
  - `CurrentActionIndex` - Current action
  - `ExecutionStartTime` - Start time
  - `IsRunning` - Execution status
- **Event:** `WorkflowCompleted` (when workflow finishes)
- **Trigger:** Workflow.InPort feedback (NOT tied to a train!)
- **Use Case:** Track-side automations (signals, announcements, turnouts)

**Example:**
```csharp
// Workflow.InPort = 3 (independent sensor)
// When InPort=3 triggered → Execute workflow actions
// → NOT related to any specific train!
```

### 3️⃣ StationManager (Platform Perspective) ⏸️ FUTURE
**Question:** "What is happening on **Platform 1**?"

- **Purpose:** Monitor platform status and schedules
- **Entity:** `Station` (with `Station.Platforms[].InPort` sensors)
- **SessionState:** `StationSessionState` (to be created)
  - `StationId` - Station ID
  - `CurrentTrainOnPlatform` - Train reference
  - `PlatformStatus` - Free/Occupied/Blocked
  - `ExpectedArrival` - Scheduled time
  - `ActualArrival` - Real arrival time
- **Event:** `TrainArrived`, `TrainDeparted`
- **Trigger:** Platform-specific workflows (announcements, signals)
- **Use Case:** "Achtung an Gleis 1. Ein Zug fährt durch."
- **Future:** Delay announcements, schedule conflicts

**Example:**
```csharp
// Station.Platforms[0].InPort = 7 (platform sensor)
// When InPort=7 triggered → "Train arriving at Platform 1"
// → Calculate delay: ActualArrival - ExpectedArrival
// → Announce: "ICE 401 arrives 5 minutes late"
```

### Key Principle
- ✅ **One Manager per Perspective** (Journey, Workflow, Station)
- ✅ **Each Manager has its own SessionState** (runtime data)
- ✅ **Managers are independent** (can run simultaneously)
- ✅ **All inherit from** `BaseFeedbackManager<TEntity>`

---

## 🔧 PowerShell 7 Command Guidelines

**Environment:** PowerShell 7.5.4 configured in Visual Studio

All PowerShell commands MUST follow these rules:

### Basic Structure
```powershell
$ErrorActionPreference='Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Set-Location -Path "C:\Repos\ahuelsmann\MOBAflow"

try {
    # Your commands here
    Write-Host "Success message" -ForegroundColor Green
    exit 0
} catch {
    Write-Error "Error: $_"
    exit 1
}
```

### Required Practices
1. **Always** set working directory first: `Set-Location -Path "<RepoRoot|ProjectPath>"`
2. **Quote all paths** including those with spaces: `"C:\Program Files\..."`
3. **Enforce UTF-8 with BOM**: Use `-Encoding utf8BOM` for XAML/XML files
4. **Error handling**: Use try-catch with `$ErrorActionPreference='Stop'`
5. **No interactive prompts**: Use parameters or defaults
6. **Idempotent**: Check before creating (e.g., `if (-not (Test-Path $file))`)
7. **Status messages**: `Write-Host` for progress, `Write-Error` for failures (**NO Unicode emojis or special characters!**)
8. **Exit codes**: Return 0 for success, 1 for failure
9. **Tool checks**: Verify availability with `Get-Command` before use:
   ```powershell
   if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
       Write-Error "dotnet CLI not found"
       exit 1
   }
   ```
10. **Here-strings**: MUST close with `'@` on own line (no leading spaces):
    ```powershell
    & {
        $content = @'
    <Page>
        <!-- XAML content -->
    </Page>
    '@
        Set-Content -Path $file -Value $content -Encoding utf8BOM
    }
    ```

### Cross-Platform Considerations
- Prefer `Join-Path` over string concatenation
- Use `$PSVersionTable.Platform` to detect OS
- Avoid Windows-specific cmdlets when possible

### Avoid
- ❌ Here-strings without proper closing (`'@` MUST be on own line, no spaces)
- ❌ Unquoted paths with spaces
- ❌ Interactive prompts (`Read-Host`)
- ❌ Hardcoded username paths (`C:\Users\JohnDoe\...`)
- ❌ Mixing BOM and non-BOM encodings (use `utf8BOM` for XAML/XML)

### PowerShell 7 Atomicity Pattern
For complex operations, wrap in script block:
```powershell
& {
    $ErrorActionPreference = 'Stop'
    Set-Location "C:\Repos\ahuelsmann\MOBAflow"
    # Your commands here
    exit 0
}
```

---

## 🔧 Quick Reference

### File Locations
- **City Library:** `WinUI/bin/Debug/germany-stations.json` (master data)
- **User Solutions:** `*.mobaflow` files (user projects)
- **Settings:** `appsettings.json` (Z21 IP, Speech config)

### Key Classes
- **MainWindowViewModel:** Central ViewModel (shared by all Pages)
- **Solution:** Root domain object (Projects → Journeys/Workflows/Trains)
- **IZ21:** Hardware abstraction (UDP → Z21 protocol)
- **StationConverter:** JSON serialization (Workflow references)

---

## 📚 Related Documentation

### Specific Instructions (Layer-Specific)
- **Backend:** `.github/instructions/backend.instructions.md` (Platform independence)
- **WinUI:** `.github/instructions/winui.instructions.md` (EditorPage patterns)
- **MAUI:** `.github/instructions/maui.instructions.md` (Mobile-specific)
- **Blazor:** `.github/instructions/blazor.instructions.md` (Web-specific)
- **Tests:** `.github/instructions/test.instructions.md` (Testing patterns)
- **State Management:** `.github/instructions/hasunsavedchanges-patterns.instructions.md`

### Architecture & Design
- **Refactoring Plan:** `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md` (Active!)
- **Build Status:** `docs/BUILD-ERRORS-STATUS.md`
- **Z21 Protocol:** `docs/Z21-PROTOCOL.md`
- **MVVM Analysis:** `docs/MVVM-ANALYSIS-MAINWINDOW-2025-12-02.md`

### Session Reports
- **Session Summaries:** `docs/SESSION-SUMMARY-*.md` (archive after 1 month)

---

**Last Updated:** 2025-12-08  
**Version:** 2.3 (Aggregat-Design Guidelines + Reference-Based Architecture)
