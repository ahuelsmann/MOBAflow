# MOBAflow - Master Instructions (Ultra-Compact)

> Model railway application (MOBA) with focus on use of track feedback points. 
> Journeys (with stops or stations) can be linked to feedback points so that any actions within the application can then be performed based on the feedbacks.
> **Multi-platform system (.NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)
> 
> **Last Updated:** 2025-12-18 | **Version:** 3.6

---

## 🎯 Current Session Status (Dec 11, 2025)

### ✅ Completed This Session
- ✅ **MAUI Null-Safety Warnings** (CS8602/CS8604) - Fixed in BackgroundService.cs
- ✅ **WinUI Button Styling** - Compact symbol buttons with `MinWidth="0"`
- ✅ **RedundantStringInterpolation** - 3x fixed (CounterViewModel, ActionExecutor, App.xaml.cs)
- ✅ **MergeIntoPattern** - 4x fixed (UdpWrapper, Z21MessageParser)
- ✅ **ArrangeObjectCreationWhenTypeEvident** - 2x fixed (`new object()` → `new()`)
- ✅ **Collection Expression Syntax** - 3x fixed (Domain/Project.cs, Solution.cs, Train.cs)
- ✅ **Primary Constructor Migration** - WorkflowService, SoundManager, SpeechHealthCheck
- ✅ **Code Cleanup** - Minor warning reductions

### 📊 ReSharper Warnings Progress
- **Start:** 653 warnings
- **Current:** ~640 warnings (estimated, after 9+ fixes)
- **Target:** <100 warnings (Phase 2)

---

## 🚨 MANDATORY PRE-ANALYSIS (Run FIRST!)

### **Red Flags Checklist (Check Before ANY Work)**

Execute these checks before code reviews, refactoring, or architecture discussions:

#### 🔴 **Critical Red Flags (Stop & Question)**
1. **Custom Control >200 LOC** → Ask: "Platform alternative exists?"
2. **Reflection in Loops** (`GetType()`, `GetProperties()`) → Performance killer
3. **Code-Behind >50 LOC** (excluding constructor) → MVVM violation
4. **Manager/Helper >100 LOC** → Could Binding/MVVM solve it?
5. **No `x:Bind` in WinUI XAML** → Missing compiled bindings (slow!)
6. **INotifyPropertyChanged in Domain** → Architecture violation
7. **DispatcherQueue in Backend** → Platform dependency (use IUiDispatcher)
8. **Event Handlers in Code-Behind** → Use XAML Behaviors (Event-to-Command) instead
   - **Exception:** Drag & Drop handlers are OK in code-behind (WinUI limitation)
9. **Static Collections** → Memory leak risk
10. **Primary Constructors with Interface Implementation** → May cause DI issues (revert to traditional)

**Action:** If >3 Red Flags found → Deep-dive analysis required.

---

## 🤖 Context-Aware Loading (AI: Auto-Trigger)

**Pattern:** Detect keywords in user request → Load matching instruction file.

| User Mentions | Auto-Load File | Trigger Keywords |
|---------------|----------------|------------------|
| **Backend Work** | `.github/instructions/backend.instructions.md` | *Manager.cs, *Service.cs, SessionState, IUiDispatcher, JourneyManager |
| **WinUI UI** | `.github/instructions/winui.instructions.md` | .xaml, EditorPage, ContentControl, x:Bind, DataTemplate, SelectorBar |
| **ViewModel** | `.github/instructions/winui.instructions.md` | *ViewModel.cs, ObservableProperty, RelayCommand, MVVM |
| **Domain Entities** | Inline rules below | Journey, Station, Train, Locomotive, Workflow, FeedbackPointOnTrack (check: GUID refs?) |
| **Tests** | `.github/instructions/test.instructions.md` | Test.csproj, [Test], FakeUdpClient, NUnit |
| **State Management** | `.github/instructions/hasunsavedchanges-patterns.instructions.md` | UndoRedo, HasUnsavedChanges, StateManager |
| **MAUI Mobile** | `.github/instructions/maui.instructions.md` | .razor, MainThread, MOBAsmart |
| **Blazor Web** | `.github/instructions/blazor.instructions.md` | .razor, MOBAdash, @code |
| **Warnings/Cleanup** | Inline section below | ReSharper, warnings, refactor, cleanup |

**Execution:** Before answering, scan keywords → Execute `get_file(<instruction_file>)` → Apply rules.

---

## 🏗️ Architecture Quick Reference (Ultra-Compact)

### **Domain Layer (Pure POCOs)**
- ✅ **YES:** Pure C# classes, GUID references for shared entities (`List<Guid> LocomotiveIds`), Value Objects
- ✅ **YES:** Embedded objects for owned entities (`Journey.Stations = List<Station>` - Stations belong to Journey)
- ✅ **YES:** Natural keys where appropriate (`FeedbackPointOnTrack.InPort` as unique ID - each value once per project)
- ❌ **NO:** INotifyPropertyChanged, Attributes, UI code

### **Backend Layer (Platform-Independent)**
- ✅ **YES:** Business logic, IUiDispatcher abstraction, SessionState (runtime data)
- ❌ **NO:** DispatcherQueue, MainThread, UI thread code, platform dependencies

### **SharedUI Layer (ViewModels)**
- ✅ **YES:** CommunityToolkit.Mvvm, Resolve GUID refs at runtime, Commands, ObservableProperty
- ✅ **YES:** Direct service injection (no Factory classes - use DI directly)
- ❌ **NO:** Platform-specific code (DispatcherQueue, MainThread)
- ❌ **NO:** Factory classes (inject services directly, create objects with `new`)

### **WinUI Layer (Desktop UI)**
- ✅ **YES:** `x:Bind` (compiled), ContentControl + DataTemplateSelector, Commands, Fluent Design 2, XAML Behaviors (Event-to-Command)
- ✅ **YES:** Drag & Drop handlers in code-behind (WinUI limitation - no good XAML Behavior support)
- ✅ **YES:** ListView with custom ItemTemplate for grid-like layouts (native, performant)
- ❌ **NO:** `Binding` (slow), Custom PropertyGrids (use DataTemplates!), Business logic in code-behind
- ❌ **NO:** Third-party DataGrid packages (most are incompatible with WinUI 3 + .NET 10)
- ⚠️ **BEFORE adding packages:** Verify WinUI 3 AND .NET 10 compatibility on NuGet.org

### **MAUI Layer (Mobile UI)**
- ✅ **YES:** MainThread.BeginInvokeOnMainThread, ContentView, MAUI-specific controls
- ❌ **NO:** WinUI-specific APIs, Desktop-only patterns

### **Test Layer**
- ✅ **YES:** Fake objects (FakeUdpClient), Dependency Injection, NUnit
- ❌ **NO:** Mocks in production code, Hardware in tests, Static dependencies

### **Package Management (Central)**
- ✅ **YES:** All package versions in `Directory.Packages.props` (Central Package Management enabled)
- ✅ **YES:** When adding NuGet packages, ALWAYS update BOTH:
  1. Project file (`.csproj`) → `<PackageReference Include="PackageName" />` (no Version attribute)
  2. `Directory.Packages.props` → `<PackageVersion Include="PackageName" Version="X.Y.Z" />`
- ✅ **YES:** BEFORE adding: Check package compatibility with target framework (.NET 10, WinUI 3)
- ❌ **NO:** Version attributes in project files (managed centrally)
- ⚠️ **CRITICAL:** Forgetting `Directory.Packages.props` will cause build failures!
- ⚠️ **CRITICAL:** Adding incompatible packages will cause restore failures!

### **Code Changes & Validation**
- ✅ **YES:** Make minimal modifications to achieve the goal
- ✅ **YES:** Always validate changes using `run_build` tool after edits
- ✅ **YES:** Run build BEFORE declaring task complete
- ❌ **NO:** Skip build validation to save time
- ⚠️ **CRITICAL:** Build validation catches errors immediately (package issues, syntax errors, missing references)

---

## 🎯 Current Project Status (Dec 2025)

### **Active Refactoring**
- ✅ **Reference-Based Domain Architecture** (100% complete)
  - Domain: GUID refs ✅ | Backend: Complete ✅ | ViewModels: Complete ✅
- ✅ **Event-to-Command Migration** (100% complete)
  - All ListViews use XAML Behaviors ✅ | Code-behind handlers removed ✅
  - Version 3.0.0 unified namespaces ✅
- ✅ **AnyRail Import** (100% complete)
  - XML parsing ✅ | SVG path generation ✅ | Article code detection ✅

### **Known Issues**
- ✅ **Resolved:** Build errors fixed (Event-to-Command + namespace unification)
- ✅ **Resolved:** ItemClick refresh issues (wiederholte Klicks funktionieren jetzt)
- ✅ **Resolved:** AnyRail curves direction (sweep-flag corrected)

### **Recent Wins (Dec 2025)**
- ✅ **AnyRail Track Plan Import** (Dec 11) → Full XML import with article code detection
  - Old: Manual track layout factory methods (400+ LOC)
  - New: AnyRail XML import with automatic geometry parsing
  - **Files:** `Domain/TrackPlan/AnyRailLayout.cs`, `TrackPlanViewModel.cs`
  - **Features:**
    - Parse AnyRail XML (lines, arcs, endpoints, connections)
    - Generate SVG path data for WinUI rendering
    - Detect article codes from geometry (R1/R2/R3 from radius, G62/G119/G231 from length)
    - Support for: Straight, Curve, WL, WR, DWW (3-way), DKW (double slip)
  - **Legacy Cleanup:** Removed `TrackLayout.CreateHundeknochenMittelstadt()` (400 LOC → 47 LOC)
- ✅ **PropertyGrid Modernization** → -70% code, native WinUI 3 patterns
  - Old: SimplePropertyGrid (350 LOC, Reflection)
  - New: ContentControl + DataTemplateSelector (200 LOC XAML)
- ✅ **Event-to-Command Pattern** → -200 LOC code-behind, clean MVVM
  - Old: `ListView_ItemClick` handlers in code-behind
  - New: XAML Behaviors with `ItemClickedCommand`
  - Migration to v3.0: Unified `Microsoft.Xaml.Interactivity` namespace
- ✅ **CommandBar Overflow Support** (Dec 10) → Responsive UI at all window sizes
  - Added: `OverflowButtonVisibility="Auto"` + `DynamicOverflowOrder` priorities
  - Impact: Buttons automatically move to overflow menu when space is limited
- ✅ **Event-Driven State Management** (Dec 10) → Eliminated race conditions
  - Old: Manual state reset in commands (race conditions)
  - New: Filter events based on state (Single Source of Truth)

---

## 🚨 Past Mistakes (Never Repeat!)

### **1. Manual Track Layout Factory Methods (Dec 2025)**
- ❌ **Mistake:** Hand-coded track layout with hardcoded coordinates (400+ LOC)
- ✅ **Solution:** Import from AnyRail XML (structured data with exact geometry)
- 📉 **Impact:** -350 LOC, precise rendering, maintainable

### **2. PropertyGrid Anti-Pattern (Dec 2025)**
- ❌ **Mistake:** Custom Reflection-based PropertyGrid (350 LOC)
- ✅ **Solution:** ContentControl + DataTemplateSelector (native WinUI 3)
- 📉 **Impact:** -480 LOC (-70%), compiled bindings, native patterns

### **3. ClearOtherSelections Complexity**
- ❌ **Mistake:** Manual selection cleanup logic (35 LOC)
- ✅ **Solution:** ContentControl automatic template switching
- 📉 **Impact:** -35 LOC, automatic behavior, simpler code

### **4. Manual State Override in Commands (Dec 2025)**
- ❌ **Mistake:** Manually resetting system state values in commands (race conditions)
- ✅ **Solution:** Filter events based on state in event handlers
- 📉 **Impact:** Eliminated race conditions, deterministic behavior

**Anti-Pattern:**
```csharp
// ❌ WRONG: Manual override (timing issue!)
await _z21.SetTrackPowerOffAsync();
MainCurrent = 0;  // Race condition if Z21 sends update after this!
```

**Correct Pattern:**
```csharp
// ✅ CORRECT: Filter in event handler
private void UpdateSystemState(SystemState state)
{
    if (state.IsTrackPowerOn)
        MainCurrent = state.MainCurrent;  // Real values
    else
        MainCurrent = 0;  // Filtered based on state
}
```

### **5. Event-to-Command** (Already Fixed - See `docs/XAML-BEHAVIORS-EVENT-TO-COMMAND.md`)
- ❌ **Mistake:** `ListView_ItemClick` code-behind handlers (complex fallback logic, 40+ LOC per handler)
- ✅ **Solution:** Custom `ListViewItemClickBehavior` with direct EventArgs extraction
- 📉 **Impact:** -200 LOC code-behind, clean MVVM separation, reusable patterns
- 📖 **NuGet:** `Microsoft.Xaml.Behaviors.WinUI.Managed` Version **3.0.0**

---

## 🛤️ Track Plan Import (AnyRail)

### **⚖️ Legal Compliance (IMPORTANT)**
- ✅ **AnyRail:** Proprietary software by Carsten Kühling & Paco Ahlqvist
- ✅ **MOBAflow:** Supports import of **user-exported** AnyRail XML files (fair use / interoperability)
- ✅ **Transparency:** See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for legal disclaimers
- ❌ **NOT:** Distributing, modifying, or replicating AnyRail
- ⚠️ **When documenting:** Always mention "AnyRail is a third-party tool. MOBAflow is independent and not affiliated with AnyRail."

### **Architecture**
```
AnyRail XML → AnyRailLayout.Parse() → AnyRailPart[] → TrackSegmentViewModel[]
                                           ↓
                              GetArticleCode() (R1/R2/R3, G62/G119/G231)
                              ToPathData() (SVG M/L/A commands)
```

### **Article Code Detection (from Geometry)**
| Type | Detection Method | Values |
|------|------------------|--------|
| **Curve** | `arc.Radius` | R1 (<600), R2 (<700), R3 (<800), R9 (>1000) |
| **Straight** | Line length | G62 (<100), G119 (<200), G231 (>200) |
| **Turnout** | `part.Type` | WL, WR, DWW, DKW |

### **SVG Arc Sweep-Flag**
- `sweep = 0` for all curves (curves outward from center)
- AnyRail `direction` attribute is in degrees (0-360) but not used for sweep

### **Key Files**
- `Domain/TrackPlan/AnyRailLayout.cs` - XML parser + article code detection
- `Domain/TrackPlan/TrackLayout.cs` - Minimal POCO (47 LOC)
- `SharedUI/ViewModel/TrackPlanViewModel.cs` - Import command + conversion
- `WinUI/View/TrackPlanPage.xaml` - Viewbox + Path rendering

---

## 🔍 Systematic Analysis Method (5 Steps)

**Use when:** Architecture review, refactoring planning, code quality audit.

### **Step 1: Custom Controls Scan**
```powershell
Get-ChildItem WinUI/Controls/*.cs,WinUI/View/*.xaml.cs -Recurse |
  Where-Object {(Get-Content $_ | Measure-Object).Lines -gt 100}
```
**For each found:** Apply Red Flags checklist → Check platform alternatives.

### **Step 2: Manager/Helper Audit**
```powershell
Get-ChildItem SharedUI/*Manager.cs,Backend/*Manager.cs,*Helper.cs -Recurse |
  Where-Object {(Get-Content $_ | Measure-Object).Lines -gt 50}
```
**Ask:** Could MVVM Binding (Converters, Triggers) replace this?

### **Step 3: Reflection Search**
```powershell
Get-ChildItem -Recurse -Include *.cs | Select-String "GetType\(\)|GetProperties\(\)" |
  Where-Object {$_.Path -notmatch "\\obj\\|\\bin\\"}
```
**Red Flag:** Reflection in performance-critical code → Refactor to x:Bind.

### **Step 4: XAML Code-Behind Check**
```powershell
Get-ChildItem -Recurse -Include *.xaml.cs |
  Where-Object {(Select-String -Path $_ -Pattern "private void.*Click").Count -gt 2}
```
**Ask:** Commands + Binding instead of event handlers?

### **Step 5: Architecture Layer Violations**
- **Domain has INotifyPropertyChanged?** → Move to ViewModel
- **Backend has DispatcherQueue?** → Use IUiDispatcher abstraction
- **ViewModel has MainThread?** → Should be platform-agnostic

**Full methodology:** `docs/CODE-ANALYSIS-BEST-PRACTICES.md`

---

## 🎨 UI Patterns (WinUI 3 Best Practices)

### **Property Editing Pattern**
```csharp
// ✅ CORRECT: ContentControl + DataTemplateSelector
public class EntityTemplateSelector : DataTemplateSelector {
    public DataTemplate? JourneyTemplate { get; set; }
    protected override DataTemplate? SelectTemplateCore(object item, ...)
        => item switch {
            JourneyViewModel => JourneyTemplate,
            _ => DefaultTemplate
        };
}
```

```xaml
<!-- ✅ CORRECT: Type-specific templates -->
<ContentControl Content="{Binding CurrentSelectedObject, Mode=OneWay}"
                ContentTemplateSelector="{StaticResource EntityTemplateSelector}" />

<DataTemplate x:Key="JourneyTemplate" x:DataType="vm:JourneyViewModel">
    <StackPanel Padding="16" Spacing="16">
        <TextBox Header="Name" Text="{x:Bind Name, Mode=TwoWay}"/>
        <NumberBox Header="InPort" Value="{x:Bind InPort, Mode=TwoWay}" 
                   SpinButtonPlacementMode="Inline"/>
    </StackPanel>
</DataTemplate>
```

### **Selection Pattern**
```csharp
// ✅ CORRECT: Single CurrentSelectedObject
public object? CurrentSelectedObject {
    get {
        if (SelectedStation != null) return SelectedStation;  // Priority
        if (SelectedJourney != null) return SelectedJourney;
        return null;
    }
}
```
**No manual cleanup needed** → Template selector handles automatically.

### **CommandBar Responsive Design**
```xaml
<!-- ✅ CORRECT: Explicit overflow configuration -->
<CommandBar OverflowButtonVisibility="Auto" DefaultLabelPosition="Right">
    <AppBarButton Command="{x:Bind ViewModel.ConnectCommand}"
                  CommandBar.DynamicOverflowOrder="0"
                  Label="Connect" />  <!-- Priority 0 = Always visible -->
    <AppBarButton Command="{x:Bind ViewModel.LoadCommand}"
                  CommandBar.DynamicOverflowOrder="1"
                  Label="Load" />     <!-- Priority 1 = High priority -->
</CommandBar>
```
**Key:** Lower `DynamicOverflowOrder` = Higher priority (stays visible longer)

### **Fluent Design 2**
- ✅ **Spacing:** Padding="16" Spacing="16" (consistent 16px)
- ✅ **Typography:** `{ThemeResource SubtitleTextBlockStyle}`
- ✅ **Theme-Aware:** `{ThemeResource TextFillColorSecondaryBrush}`
- ✅ **Modern Controls:** NumberBox (SpinButtonPlacementMode="Inline"), TimePicker

---

## 📚 Deep-Dive Documentation (Load on Demand)

### **Layer-Specific Instructions**
- `.github/instructions/backend.instructions.md` - Platform-independent patterns
- `.github/instructions/winui.instructions.md` - WinUI 3 UI patterns
- `.github/instructions/maui.instructions.md` - Mobile patterns
- `.github/instructions/blazor.instructions.md` - Web patterns
- `.github/instructions/test.instructions.md` - Testing guidelines
- `.github/instructions/hasunsavedchanges-patterns.instructions.md` - State management

### **Architecture Documentation**
- `docs/ARCHITECTURE-INSIGHTS-2025-12-09.md` - Journey execution flow, SessionState, ViewModel 1:1 mapping
- `docs/XAML-BEHAVIORS-EVENT-TO-COMMAND.md` - Event-to-Command pattern (XAML Behaviors v3.0)

---

## 🔧 Manager Architecture (Feedback Processing)

### **Principle:** Different Perspectives on Z21 Feedback Events

```
Z21 Feedback (InPort=5)
        ↓
┌───────┼────────┬────────────┐
│       │        │            │
Journey Workflow Station   (Future)
Manager Manager  Manager
```

### **1. JourneyManager (Train Perspective)** ✅ Implemented
- **Question:** "Where is the **train** right now?"
- **Entity:** `Journey` (Journey.InPort = train sensor)
- **SessionState:** `JourneySessionState` (Counter, CurrentPos, CurrentStationName)
- **Event:** `StationChanged` (train reached station)
- **Trigger:** Execute Station.Flow workflow

### **2. WorkflowManager (Workflow Perspective)** ⏸️ Future
- **Question:** "Which **workflow** is executing?"
- **Entity:** `Workflow` (Workflow.InPort = trigger sensor)
- **Independent:** Not tied to train movements

### **3. StationManager (Platform Perspective)** ⏸️ Future
- **Question:** "What's happening on **Platform 1**?"
- **Entity:** `Station` (Platform sensors)
- **Use Case:** Delay announcements, schedule conflicts

---

## 🎯 Key Principles (Always Remember)

### **Domain Architecture**
- ✅ **GUID References for Shared Entities:** `Train.LocomotiveIds = List<Guid>` (Locomotives are shared across trains)
- ✅ **Embedded Objects for Owned Entities:** `Journey.Stations = List<Station>` (Stations belong exclusively to one Journey)
- ✅ **Single Source of Truth:** Project aggregate root has master lists for shared entities
- ✅ **Pure POCOs:** No INotifyPropertyChanged, no attributes

### **ViewModel 1:1 Property Mapping Rule**

**Principle:** Every Domain property **MUST** have a corresponding ViewModel property with **the same name** and **compatible type**.

#### **Rule Details:**

1. **Same Name:** Domain property `InPort` → ViewModel property `InPort` (NOT `FeedbackInPort`!)
2. **Same/Compatible Type:** 
   - Domain `uint` → ViewModel `uint` ✅
   - Domain `string` → ViewModel `string?` ✅ (nullable OK)
   - Domain `List<Guid>` → ViewModel `List<Guid>` ✅
3. **Read-Only for IDs:** `Id` property should be read-only (`public Guid Id => Model.Id;`)
4. **Additional Runtime Properties OK:** ViewModel can have extra properties (e.g., `CurrentStation`, `IsCurrentStation`)

#### **Example: Train Entity**

```csharp
// Domain/Train.cs (Pure POCO)
public class Train
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public TrainType TrainType { get; set; }
    public List<Guid> LocomotiveIds { get; set; }  // ✅ GUID references
}

// SharedUI/ViewModel/TrainViewModel.cs (1:1 Mapping)
public partial class TrainViewModel : ObservableObject
{
    public Guid Id => Model.Id;  // ✅ Read-only
    public string Name { get => Model.Name; set => SetProperty(...); }
    public TrainType TrainType { get => Model.TrainType; set => SetProperty(...); }
    public List<Guid> LocomotiveIds { get => Model.LocomotiveIds; set => SetProperty(...); }
    
    // ✅ ADDITIONAL: Resolved Collections (ViewModel-Specific)
    public ObservableCollection<LocomotiveViewModel> Locomotives => /* resolve from IDs */;
}
```

#### **Why 1:1 Mapping?**

- **Maintainability:** Domain rename → ViewModel breaks (compile-time error)
- **Testability:** Easy to verify ViewModel wraps Domain correctly
- **Serialization:** Domain → JSON → Domain (no circular references)

#### **Anti-Pattern:**

```csharp
// ❌ WRONG: Different property name
public uint FeedbackInPort => _station.InPort;  // ❌ Name mismatch!

// ✅ CORRECT: Same property name
public uint InPort => _station.InPort;  // ✅ 1:1 mapping
```

### **ViewModel Resolution**
```csharp
// ✅ Resolve at runtime in ViewModel
public ObservableCollection<StationViewModel> Stations =>
    _journey.StationIds
        .Select(id => _project.Stations.FirstOrDefault(s => s.Id == id))
        .Where(s => s != null)
        .Select(s => new StationViewModel(s, _project))
        .ToObservableCollection();
```

### **SessionState Pattern**
- ✅ **Separate runtime data** from Domain
- ✅ **Manager owns SessionState** (JourneyManager has JourneySessionState)
- ✅ **ViewModels read SessionState** (read-only, subscribe to events)

### **Fields Region Pattern (ViewModel)**

**Rule:** Group fields logically, no empty lines after `#region` or before `#endregion`.

**This applies to ALL `#region` blocks in ALL classes, not just Fields!**

```csharp
#region Fields
// Core Services (required)
private readonly IZ21 _z21;
private readonly IUiDispatcher _uiDispatcher;

// Configuration
private readonly AppSettings _settings;

// Optional Services
private readonly ISettingsService? _settingsService;

// Runtime State
private JourneyManager? _journeyManager;
#endregion
```

**Grouping order:**
1. **Model** (if ViewModel wraps a domain object)
2. **Core Services** (required dependencies)
3. **Context** (Project, Solution references)
4. **Configuration** (AppSettings, etc.)
5. **Optional Services** (nullable dependencies)
6. **Runtime State** (mutable state, disposable objects)

### **UI Patterns**
- ✅ **x:Bind > Binding** (compiled vs runtime)
- ✅ **ContentControl + DataTemplateSelector** (not custom grids)
- ✅ **Commands > Click-Handlers** (MVVM-conform)

---

### **Pattern Comparison**

| Aspect | Computed Property ❌ | Direct Assignment ✅ |
|--------|---------------------|---------------------|
| **LOC per Command** | 5-10 lines | 1 line |
| **Manual Clearing** | Yes, everywhere | No, automatic |
| **Hidden Logic** | Yes (priority) | No, explicit |
| **Debuggability** | Hard | Easy |
| **Maintainability** | Low | High |
| **User Expectation** | Violated | Met |

---

### **KISS Principle for MVVM Properties**

**Rule:** Observable properties should be simple and setable. Complex logic belongs in `OnChanged` handlers.

#### **✅ DO:**
```csharp
[ObservableProperty]
private ItemViewModel? selectedItem;

partial void OnSelectedItemChanged(ItemViewModel? value)
{
    CurrentView = value;           // Explicit
    UpdateRelatedState(value);     // Clear intent
}
```

#### **❌ DON'T:**
```csharp
public object? CurrentView => A ?? B ?? C ?? D;  // Complex getter
public object? CurrentView
{
    get
    {
        if (complex_condition) return ComputeComplex();
        return Default;
    }
}
```

---

### **Lesson Learned: Question Complexity**

**If a solution requires:**
- Manual state clearing in every command
- Callbacks/Helpers for simple operations
- Priority hierarchies

→ **The design is probably wrong!** Ask: _"Can I simplify this?"_

**Simple is not simplistic. Simple is elegant.** 🎯

---

# ⚡ PowerShell 7 Terminal Rules (Copilot‑Specific) — Clean Version

> **Focus:** Avoid one‑liner parser errors (e.g., `foreach` directly after an expression), prevent `Select-String -Recurse` misuse, ensure UTF‑8 BOM + CRLF, and use pipeline‑safe loops and idempotent edits.

---

## 1) Always assume **PowerShell 7 (pwsh)** in Visual Studio DevShell
- VS DevShell is active (environment/modules loaded).
- PSReadLine may be disabled in this shell.

---

## 2) Mandatory Session Setup (prepend to every snippet)
```powershell
$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'
[Console]::OutputEncoding=[Text.Encoding]::UTF8; [Console]::InputEncoding=[Text.Encoding]::UTF8
if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) { $PSStyle.OutputRendering='Ansi' }
try { $PSNativeCommandArgumentPassing = 'Standard' } catch {}
```
> **Important:** The variable is **`$PSStyle`** (with **S**). Do **not** use `$PStyle`.

---

## 3) Git **without a pager**
```powershell
$env:GIT_PAGER='cat'; $env:LESS='-FRSX'; $env:LESSCHARSET='utf-8'
# Or per command: git --no-pager diff / log / show
# Preferred global: git config --global core.pager cat
```

---

## 4) One‑liner safety: separators and loops
- Always separate statements with **`;`** in one‑liners — before `if`, `foreach`, `for`, `while`, `try`, etc.
  - ❌ `... .Count foreach ($f in $files) { ... }`
  - ✅ `... .Count; foreach ($f in $files) { ... }`
- **Prefer `ForEach-Object`** over `foreach (...) {}` in one‑liners:
```powershell
Get-ChildItem src -Filter *.cs -Recurse | ForEach-Object { $f = $_.FullName; # ... }
```
- Use the `foreach` *keyword* only in **multi‑line blocks**.

---

## 5) Prefer **PowerShell‑native** commands over Unix tools
- `head` → `Select-Object -First N`
- `grep` → `Select-String`
- `sed -i` → `Get-Content -Raw` + `-replace` + `Set-Content`
- `wc -l` → `Measure-Object`
- Avoid `&&`; use semicolons or newlines.

---

## 6) Regex safety rules
- Use **single quotes** for static regex: `'pattern'`
- Escape literal specials if needed: `\?  \(  \)  \.  \+  \*  \[  \]  \{  \}  \|`
- For **dynamic** parts, always use `[regex]::Escape(...)`:
```powershell
$pattern = 'private\s+' + [regex]::Escape('TrainViewModel?') + '\s+selectedTrain;'
if ($line -match $pattern) { ... }
```
- Anchor with `$` where appropriate to avoid over‑matching.
- **Test before replacing:**
```powershell
Select-String -Pattern 'private\s+TrainViewModel\?\s+selectedTrain;' -Path $file
```

---

## 6a) Here‑String rules (PowerShell)
- **Never** start a here‑string in a **one‑liner**. The header `@'` or `@"` must be the **only token** on its line; same for the closing `'^@` / `"^@` — **no indentation**.
- Use **single‑quoted** here‑strings when no interpolation is needed:
```powershell
# Single-quoted here-string (no interpolation)
$xaml = @'
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- content -->
</ResourceDictionary>
'@
```
- Use **double‑quoted** here‑strings when interpolation is required:
```powershell
# Double-quoted here-string (allows interpolation)
$user = $env:USERNAME
$text = @"
Hello $user
"@
```
- For **one‑liners**, use these alternatives:
  - String concatenation with explicit newlines: `+ "`n" +`
  - Join an **array of lines** with `[Environment]::NewLine`.

---

## 6b) File search rule (Select‑String)
Do **not** use `-Recurse` with **Select-String** (it has no such parameter). To search recursively, **enumerate files first**, then pass the list to `-Path`:
```powershell
# Build file list first (exclude bin/obj), then search
$files = Get-ChildItem -Path . -Filter *.cs -Recurse -File |
         Where-Object { $_.FullName -notmatch '\\(?:bin|obj)\\' } |
         Select-Object -ExpandProperty FullName

Select-String -Path $files -Pattern 'your-regex'
```
Alternatively, search only **Git‑tracked** files (often faster & cleaner):
```powershell
$files = git ls-files "*.cs"
Select-String -Path $files -Pattern 'your-regex'
```

---

## 7) File I/O & encoding (project policy)
- **Read:** `Get-Content -Raw`
- **Write:** UTF‑8 **with BOM** (pipeline requirement)
```powershell
Set-Content $file $text -Encoding UTF8BOM
# or
Out-File $file -InputObject $text -Encoding UTF8BOM
```
- **Line Endings:** Windows (CR LF) — ensure all files use `\r\n`.
- **No trailing empty lines:** No extra blank lines after the final closing brace `}`.
- Quote paths with spaces; use double quotes when interpolating variables.

### 7b) Normalize CRLF & ensure UTF‑8 BOM (idempotent)
```powershell
function Write-NormalizedUtf8Bom {
  param(
    [Parameter(Mandatory)] [string] $Path,
    [Parameter(Mandatory)] [string] $Content
  )
  # Normalize line endings to CRLF
  $text = [regex]::Replace($Content, '(?<!\r)\n', "`r`n")
  # Trim excessive blank lines at EOF → keep exactly one terminal newline
  $text = [regex]::Replace($text, '(\r?\n)+\z', "`r`n")
  # Write with UTF‑8 BOM (pipeline requirement)
  Set-Content -Path $Path -Value $text -Encoding UTF8BOM
}
```
**Usage**
```powershell
$text = Get-Content $file -Raw
Write-NormalizedUtf8Bom -Path $file -Content $text
```

---

## 8) Quick copy‑and‑use examples
```powershell
# Diff without pager, first 100 lines
git --no-pager diff .github/copilot-instructions.md | Select-Object -First 100

# Count matches and print if found
$count=(Select-String -Path . -Pattern 'EventTriggerBehavior' -List | Measure-Object).Count; if ($count -gt 0) { Write-Host "$count instances" }

# Pipeline-safe loop
git ls-files *.cs | ForEach-Object { $f = $_; if ((Select-String -Path $f -Pattern 'INotifyPropertyChanged' -List)) { Write-Host $f } }
```

### Write XAML with BOM (safe)
```powershell
Set-Content -Path .\Theme.xaml -Value @'
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- content -->
</ResourceDictionary>
'@ -Encoding UTF8BOM
```

---

## 9) (Optional) Recommended VS Terminal Arguments (PowerShell 7)
Use this in **Visual Studio → Tools → Options → Environment → Terminal → PowerShell 7 → Arguments**.

```text
-NoProfile -NonInteractive -NoExit -NoLogo -ExecutionPolicy Bypass -Command "& { Import-Module \"$env:VSAPPIDDIR\..\Tools\Microsoft.VisualStudio.DevShell.dll\"; Enter-VsDevShell -SetDefaultWindowTitle -InstallPath \"$env:VSAPPIDDIR\..\..\"; $ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; [Console]::InputEncoding=[Text.Encoding]::UTF8; $env:POWERSHELL_TELEMETRY_OPTOUT='1'; $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'; $env:DOTNET_CLI_UI_LANGUAGE='en'; $env:TERM='xterm-256color'; try { Remove-Module PSReadLine -ErrorAction SilentlyContinue } catch {}; if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) { $PSStyle.OutputRendering='Ansi'; Set-Variable -Name PStyle -Value $PSStyle -Scope Global }; $env:GIT_PAGER='cat'; $env:LESS='-FRSX'; $env:LESSCHARSET='utf-8'; try { $PSNativeCommandArgumentPassing='Standard' } catch {}; try { $ErrorView='ConciseView' } catch {}; $gitRoot=(git rev-parse --show-toplevel 2>$null); if ($gitRoot -and (Test-Path $gitRoot)) { Set-Location $gitRoot } else { $sln=Get-ChildItem -Path . -Filter *.sln -ErrorAction SilentlyContinue | Select-Object -First 1; if ($sln) { Set-Location $sln.Directory.FullName } } }"
```

---

## 10) Do **not** generate
- No reference to **`$PStyle`** (only **`$PSStyle`**).
- No Bash syntax in pwsh snippets unless explicitly asked to use *Git Bash*.
- No destructive one‑liners without a prior `Select-String` check.
- Do **not** place `foreach (...) {}` after an expression in one‑liners. Use `; foreach` or `... | ForEach-Object {}`.
- Do **not** slice arrays with `[0..N]` unless clamped; prefer `Select-Object -First / -Skip`.
- Do **not** start a **here‑string** in a one‑liner; header and terminator must be on their own lines at column 0 (no indentation).

---