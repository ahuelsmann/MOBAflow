# MOBAflow - Master Instructions (Ultra-Compact)

> Model railway application (MOBA) with focus on use of track feedback points. 
> Journeys (with stops or stations) can be linked to feedback points so that any actions within the application can then be performed based on the feedbacks.
> **Multi-platform system (.NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)
> 
> **Last Updated:** 2025-01-29 | **Version:** 3.12

---

## 🎯 CORE PRINCIPLES (Always Follow!)

### **1. Fluent Design First**
- **Always** follow Microsoft Fluent Design 2 principles and best practices
- Use native WinUI 3 controls and patterns (no custom implementations unless absolutely necessary)
- Consistent spacing: `Padding="8"` or `Padding="16"`, `Spacing="8"` or `Spacing="16"`
- Theme-aware colors: `{ThemeResource TextFillColorSecondaryBrush}`, `{ThemeResource DividerStrokeColorDefaultBrush}`
- Typography: `{StaticResource SubtitleTextBlockStyle}`, `{StaticResource BodyTextBlockStyle}`

### **2. Holistic Thinking - Never Implement in Isolation**
- **When changing ONE Page, check ALL Pages** for consistency
- **When adding a feature (e.g., Add/Delete buttons), check if it applies to other entity types**
- **When fixing a pattern, fix it everywhere** - not just the current file
- **Think "Application-wide"** - never "just this one page"

**Checklist before ANY UI change:**
1. Does this pattern exist on other Pages? → Apply consistently
2. Does this feature make sense for other entities? → Implement everywhere
3. Am I following the same layout as sibling Pages? → Match exactly
4. Have I checked EntityTemplates.xaml for similar templates? → Reuse patterns

### **3. Pattern Consistency is Non-Negotiable**
- If JourneysPage has Add/Delete buttons → WorkflowsPage, SolutionPage, FeedbackPointsPage MUST have them too
- If one ListView has a header layout → ALL ListViews follow the same layout
- Deviation from established patterns = bugs + extra work + user frustration

### **4. Copy Existing Code - Don't Invent**
- Before implementing anything new: **Search for existing implementations**
- Copy working patterns exactly, then adapt for the new entity
- If it works on JourneysPage, it should work the same way on WorkflowsPage

### **5. Warning-Free Code**
- **NEVER introduce new warnings** when implementing features
- **Fix warnings immediately** - don't defer to "later"
- **Partial method signatures MUST match** the generated code exactly:
  - ✅ `partial void OnXxxChanged(Type value)` → Use `_ = value;` to suppress if unused
  - ❌ `partial void OnXxxChanged(Type _)` → Parameter name mismatch warning!
- **Event handlers must suppress unused parameters**: `_ = e;` or `_ = sender;`
- **IValueConverter parameters are nullable at runtime**: Use `object? value` not `object value`
- **Run build validation** before declaring any task complete

**Warning Patterns to Avoid:**
```csharp
// ❌ WRONG: Parameter name mismatch (CS8826)
partial void OnSelectedItemChanged(ItemViewModel? _) { }

// ✅ CORRECT: Match generated signature, suppress unused
partial void OnSelectedItemChanged(ItemViewModel? value)
{
    _ = value; // Suppress unused parameter warning
    UpdateRelatedState();
}

// ❌ WRONG: Nullable annotation mismatch in converters
public object Convert(object value, ...) // CS8602 at runtime

// ✅ CORRECT: Runtime nullable
public object Convert(object? value, ...)
{
    return value != null ? Visibility.Visible : Visibility.Collapsed;
}
```

### **6. Always Create a Plan (MANDATORY!)**
- **EVERY user request MUST start with a plan** using the `plan` tool
- **No exceptions** - even for "simple" tasks
- Plans ensure systematic approach and prevent oversights
- Use `update_plan_progress` to track completion
- Call `finish_plan` when all steps are done

**Plan Structure:**
```markdown
# Task Title

## Steps
1. Analyze current state
2. Identify required changes
3. Implement changes
4. Verify with build
5. Update documentation
```

### **7. Before/After Analysis (MANDATORY!)**
- **ALWAYS analyze the situation BEFORE making changes**
  - What is the current state?
  - What files are affected?
  - What are the dependencies?
  - Are there similar patterns elsewhere?
  
- **ALWAYS verify the result AFTER changes**
  - Build successful?
  - No new warnings?
  - Consistent with existing patterns?
  - Documentation updated?

**Template:**
```
## BEFORE:
- Current state description
- Problems identified
- Affected components

## CHANGES:
- File 1: Change description
- File 2: Change description

## AFTER:
- Build status
- Warnings fixed/introduced
- Patterns validated
- Documentation updated
```

### **8. Auto-Update Instructions (CRITICAL!)**
- **When discovering important architectural decisions** → Update this file IMMEDIATELY
- **When fixing critical bugs** → Document in "Current Session Status"
- **When establishing new patterns** → Add to relevant section
- **When deprecating old approaches** → Mark as deprecated with alternatives

**Triggers for instruction updates:**
- Protocol reverse-engineering (e.g., Z21 packet structures)
- Breaking changes to core classes
- New best practices discovered
- Critical bug fixes with broad impact
- Architectural decisions affecting multiple projects

**Update Format:**
```markdown
- ✅ **Feature Name (Date)**
  - Problem: Brief description
  - Solution: Implementation details
  - Impact: Affected components
  - Files: Changed file list
```

---

## 🎯 Current Session Status (Dec 29, 2025)

### ✅ Completed This Session
- ✅ **Workflow Action Order & Execution Mode Fixes**
  - Actions sortiert nach `Number` beim Laden (Fix: Reihenfolge wurde nicht beachtet)
  - `SoundPlayer.PlaySync()` statt `Play()` (Fix: Sequential wartete nicht auf Audio-Ende)
  - Direkte Enum-Bindung ohne Converter (Fix: ExecutionMode wurde nicht gespeichert)
  - EnumToIntConverter entfernt (obsolet durch native WinUI 3 Enum-Bindung)

- ✅ **Parallel Mode: Staggered Start mit DelayAfterMs**
  - **Sequential:** DelayAfterMs = Pause NACH Action-Ende
  - **Parallel:** DelayAfterMs = Start-Offset (kumulativ)
  - Beispiel Parallel: Gong (t=0) → Ansage (t=500ms) → Licht (t=2s)
  - Ermöglicht präzise Timing-Kontrolle in beiden Modi

- ✅ **Clean Architecture: Workflow Execution**
  - WorkflowExecutionMode.cs: Dokumentation aktualisiert
  - WorkflowService: Staggered Parallel implementiert
  - WorkflowViewModel: ExecutionModeValues Property für ComboBox-Bindung

- ✅ **Code Quality & Logging**
  - Console.WriteLine aus SoundManager.cs entfernt (ILogger reicht)
  - WorkflowService: ILogger injiziert (ersetzt Debug.WriteLine)
  - UdpWrapper.Dispose(): Race Condition Fix (ruft jetzt StopAsync() auf)
  - WorkflowAction.DelayAfterMs: Dokumentation für beide Modi präzisiert

### 📊 Fortschritt
- **Action Ordering:** ✅ Korrekt geladen & gespeichert
- **Audio Playback:** ✅ Sequential wartet auf Ende, Parallel startet gestaffelt
- **ExecutionMode:** ✅ Korrekt persistiert ohne Converter
- **Code Quality:** ✅ Warning-frei, type-safe Enum-Bindung
- **Logging:** ✅ Production-ready (ILogger statt Debug.WriteLine/Console.WriteLine)
- **Threading:** ✅ Race Condition in UdpWrapper.Dispose() behoben
  - Event-Chain vereinfacht: WorkflowService → ViewModel (direkt, ohne JourneyManager-Hop)
  - Action-Execution-Fehler werden in MonitorPage Application Log angezeigt

### 📊 Fortschritt
- **Backend Service Ownership:** ✅ Clean Architecture eingehalten
- **Sound-Bibliothek:** ✅ Plattform-unabhängig in Sound-Projekt
- **Workflow Timing:** ✅ Sequential/Parallel Modi voll funktionsfähig
- **Error-Handling:** ✅ File.Exists + UI-Feedback + Application Log

- ✅ **Z21 Traffic Monitor Improvements (Dec 29, 2025)**
  - Feedback-Pakete farblich hervorgehoben (goldgelber Hintergrund)
  - InPort-Anzeige für Feedback-Pakete
  - Richtungspfeile: ↓ = Eingehend, ↑ = Ausgehend
  - Auto-Scroll mit Pause/Resume-Toggles (Live/Paused-Modi)
  - FirstOrDefault() statt Items[0] (null-safe)

- ✅ **Serilog Integration (Dec 29, 2025)**
  - Custom InMemorySink für Real-Time UI Logs (MonitorPage)
  - File Logging: `bin/Debug/logs/mobaflow-*.log` (7 Tage Retention)
  - LoggingExtensions entfernt (deprecated, replaced by ILogger)
  - Structured Logging mit Properties statt String-Interpolation
  - Min. Level: Debug (Moba), Warning (Microsoft)

- ✅ **Z21 Feedback InPort Extraction Fix (Dec 29, 2025)**
  - **CRITICAL BUG FIX:** InPort-Extraktion war fundamental falsch
  - Problem: `data[5]` ist Feedback-ZUSTAND (Bit-Pattern), nicht InPort-Nummer
  - Lösung: Z21FeedbackParser mit korrekter Bit-zu-InPort-Konvertierung
  - Formel: InPort = (GroupNumber × 64) + (ByteIndex × 8) + BitPosition + 1
  - FeedbackResult.cs: Jetzt mit Z21FeedbackParser.ExtractFirstInPort()
  - Z21Monitor.cs: ExtractInPort() + ExtractAllInPorts() für Traffic Monitor
  - Z21TrafficPacket: AllInPorts Property für Multi-Bit-Anzeige (z.B. "1,2,5")
  - Betrifft: JourneyManager, BaseFeedbackManager, Counter/Statistics

---

## 📋 LOGGING BEST PRACTICES (Serilog)

### **Always Use ILogger via Constructor Injection**
```csharp
public class MyViewModel : ObservableObject
{
    private readonly ILogger<MyViewModel> _logger;

    public MyViewModel(ILogger<MyViewModel> logger)
    {
        _logger = logger;
    }
}
```

### **Structured Logging (DO THIS)**
```csharp
// ✅ CORRECT: Structured properties (searchable, indexable)
_logger.LogInformation("Feedback received: InPort={InPort}, Value={Value}", inPort, value);
_logger.LogWarning("Connection attempt {Attempt} failed for {IpAddress}", attemptCount, ip);
_logger.LogError(ex, "Failed to process journey {JourneyId}: {Reason}", id, ex.Message);

// ❌ WRONG: String interpolation (not searchable)
_logger.LogInformation($"Feedback received: InPort={inPort}, Value={value}");
```

### **Log Levels**
- `LogDebug()`: Development diagnostics (packet dumps, state changes)
- `LogInformation()`: Important events (connections, workflow execution, user actions)
- `LogWarning()`: Recoverable errors (retry attempts, fallbacks)
- `LogError()`: Failures requiring attention (exceptions, invalid state)

### **MonitorPage Application Log**
- Uses `InMemorySink` (custom Serilog sink)
- Real-time display in MonitorPage → Application Log panel
- Automatically formatted with severity icons (🔍 ℹ️ ⚠️ ❌)
- Auto-scroll with Pause/Resume toggle

### **File Logs**
- Location: `bin/Debug/logs/mobaflow-YYYYMMDD.log`
- Rolling: Daily (1 file per day)
- Retention: 7 days (older files auto-deleted)
- Format: `[HH:mm:ss.fff LEVEL] [SourceContext] Message`

### **Never Use**
- ❌ `Console.WriteLine()` - use `_logger.LogInformation()`
- ❌ `Debug.WriteLine()` - use `_logger.LogDebug()`
- ❌ `this.Log()` - deprecated, removed
- ❌ String interpolation in log messages - use structured properties

---

## ERROR HANDLING BEST PRACTICES

- Fail fast on invalid inputs (`ArgumentException`, `ArgumentNullException`) before side effects.
- Catch narrowly; never swallow exceptions. Always log with structured properties via `ILogger`.
- Prefer domain-level results/validation over generic catches; only wrap to add context.
- In UI flows: show concise, user-safe feedback and keep technical details in logs.
- For I/O (files/network): check existence/state (`File.Exists`, connectivity) before acting and log paths/IDs.
- Always rethrow with `throw;` (not `throw ex;`) when bubbling to keep stack traces intact.

```csharp
try
{
    await _workflowService.ExecuteAsync(workflow, cancellationToken);
}
catch (OperationCanceledException)
{
    _logger.LogWarning("Workflow execution canceled: {WorkflowId}", workflow.Id);
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to execute workflow {WorkflowId}", workflow.Id);
    throw;
}
```

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

## ✅ Existing Patterns Checklist (BEFORE Implementing New Features)

**CRITICAL RULE: Check existing patterns first - never create new approaches!**

### **How to Use This Checklist**

Before implementing ANY new feature (Page, ViewModel, Service, etc.), execute this checklist:

1. **Does this feature type already exist?**
   - Example: "Need a new page for managing FeedbackPoints" → Check existing Pages (JourneysPage, WorkflowsPage, SettingsPage)
   - Search similar implementations in the codebase

2. **What pattern does it follow?**
   - Example: JourneysPage + WorkflowsPage both inject `MainWindowViewModel` in constructor
   - All Pages use: `public MainWindowViewModel ViewModel { get; }` pattern
   - All Pages use: `DataContext="{x:Bind ViewModel}"` in XAML

3. **Could I replicate this pattern?**
   - Don't create custom factory methods for pages
   - Don't create separate PageViewModels for simple pages
   - Use DI consistently: `_serviceProvider.GetRequiredService<PageName>()`

4. **Check these sources for patterns:**
   - **Pages:** WinUI/View/*.xaml.cs (JourneysPage, WorkflowsPage, TrackPlanEditorPage, SettingsPage)
   - **ViewModels:** SharedUI/ViewModel/ (MainWindowViewModel, TrackPlanEditorViewModel)
   - **Navigation:** WinUI/Service/NavigationService.cs (how pages are created)
   - **DI Setup:** WinUI/App.xaml.cs (how services/pages are registered)

### **🔴 Anti-Pattern Examples (Don't Do This)**

```csharp
// ❌ WRONG: Custom factory method for Page creation
private FeedbackPointsPage CreateFeedbackPointsPage()
{
    var page = _serviceProvider.GetRequiredService<FeedbackPointsPage>();
    var mainWindowVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
    if (mainWindowVm.SelectedProject?.Model != null)
    {
        var viewModel = new FeedbackPointsPageViewModel(mainWindowVm.SelectedProject.Model);
        page.ViewModel = viewModel;  // ❌ Separate PageViewModel
    }
    return page;
}

// Navigation: "feedbackpoints" => CreateFeedbackPointsPage(),  // ❌ Inconsistent!

```
**Problems:**
- ❌ Inconsistent with other pages (WorkflowsPage doesn't have custom factory)
- ❌ Creates unnecessary PageViewModel wrapper
- ❌ Requires workarounds like `ToObservableCollection()` extension
- ❌ More code to maintain
- ❌ Navigation pattern differs from all other pages

### **✅ Correct Pattern (Copy Existing Code)**

```csharp
// ✅ CORRECT: Consistent DI registration in App.xaml.cs
services.AddTransient<View.FeedbackPointsPage>();

// ✅ CORRECT: Simple GetRequiredService like all other pages
"feedbackpoints" => _serviceProvider.GetRequiredService<FeedbackPointsPage>(),

// ✅ CORRECT: Code-behind injects MainWindowViewModel (like WorkflowsPage)
public sealed partial class FeedbackPointsPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public FeedbackPointsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
    }
}

// ✅ CORRECT: XAML binds to MainWindowViewModel's SelectedProject
<Page DataContext="{x:Bind ViewModel}">
    <ListView ItemsSource="{x:Bind ViewModel.SelectedProject.FeedbackPoints, Mode=OneWay}" />
</Page>

```
### **Pattern Reference Quick Lookup**

| Need | Source File | Pattern |
|------|------|---------|
| **New Page UI** | WinUI/View/JourneysPage.xaml | Copy structure, adapt entity names |
| **New Page Code-Behind** | WinUI/View/JourneysPage.xaml.cs | Inject MainWindowViewModel, no custom logic |
| **Page Registration** | WinUI/App.xaml.cs (line ~130) | `services.AddTransient<View.PageName>()` |
| **Navigation Entry** | WinUI/Service/NavigationService.cs (line ~45) | `"tag" => _serviceProvider.GetRequiredService<PageName>()` |
| **ViewModel Wrapper** | SharedUI/ViewModel/JourneyViewModel.cs | 1:1 property mapping with Domain model |
| **Entity List Template** | WinUI/Resources/EntityTemplates.xaml | DataTemplate per entity type |

### **Decision Tree**

```
Implementing New Feature?
├─ Is it a Page? 
│  ├─ Simple (readonly or list)? → Use MainWindowViewModel directly
│  └─ Complex (editor/designer)? → Check TrackPlanEditorViewModel pattern
├─ Is it a Domain object wrapper?
│  └─ Create XxxViewModel with 1:1 property mapping
├─ Is it a singleton service?
│  └─ Create specialized service (like WorkflowService)
└─ NEVER: Create PageViewModel for every new page

```
---

## 🤖 Context-Aware Loading (AI: Auto-Trigger)

**Pattern:** Detect keywords in user request → Load matching instruction file.

| User Mentions | Auto-Load File | Trigger Keywords |
|---------------|----------------|------------------|
| **Backend Work** | `docs/copilot/backend.instructions.md` | *Manager.cs, *Service.cs, SessionState, IUiDispatcher, JourneyManager |
| **WinUI UI** | `docs/copilot/winui.instructions.md` | .xaml, EditorPage, ContentControl, x:Bind, DataTemplate, SelectorBar |
| **ViewModel** | `docs/copilot/winui.instructions.md` | *ViewModel.cs, ObservableProperty, RelayCommand, MVVM |
| **Domain Entities** | Inline rules below | Journey, Station, Train, Locomotive, Workflow, FeedbackPointOnTrack (check: GUID refs?) |
| **Tests** | `docs/copilot/test.instructions.md` | Test.csproj, [Test], FakeUdpClient, NUnit |
| **State Management** | `docs/copilot/hasunsavedchanges-patterns.instructions.md` | UndoRedo, HasUnsavedChanges, StateManager |
| **MAUI Mobile** | `docs/copilot/maui.instructions.md` | .razor, MainThread, MOBAsmart |
| **MAUI .NET 9 Constraint** | Read ARCHITECTURE section above! | MAUI update, .NET 10, UraniumUI, CommunityToolkit.Maui version |
| **Blazor Web** | `docs/copilot/blazor.instructions.md` | .razor, MOBAdash, @code |
| **DI Pattern & Pages** | `docs/copilot/di-pattern-consistency.instructions.md` | New page, factory method, inconsistent pattern, GetRequiredService, NavigationService |
| **UI Layout Changes** | Read CORE PRINCIPLES section above! | Layout, buttons, Add/Delete, header, CommandBar, ListView, Page design |
| **Warnings/Cleanup** | Inline section below | ReSharper, warnings, refactor, cleanup |
| **User Documentation** | `docs/wiki/` | User guide, wiki, documentation, help |
| **MAUI Persistence** | `docs/copilot/MAUI-PERSISTIERUNG-TEST.md` | Settings persistence, Android storage, appsettings.json |
| **MAUI Background** | `docs/copilot/MAUI-BACKGROUND-SERVICE-CONCEPT.md` | Background service, foreground service, Android service |
| **Icon Update** | `docs/copilot/ICON-UPDATE-GUIDE.md` | WinUI 3 app icon, ICO generation, Package.appxmanifest |

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

### **⚠️ MAUI .NET 9 Constraint (WICHTIG!)**
**MOBAsmart (MAUI) bleibt auf .NET 9** - NICHT auf .NET 10 upgraden!

**Gründe:**
- ✅ **UraniumUI 2.14.0** - Keine .NET 10 Unterstützung bestätigt
- ✅ **MAUI-Stabilität** - .NET 9 ist production-ready
- ✅ **Build-Optimierungen** - Debug-Settings getestet auf .NET 9

**NICHT updaten (bis Migration geplant):**
| Package | Aktuelle Version | Grund |
|---------|------------------|-------|
| `Microsoft.Maui.Controls` | 9.0.100 | .NET 9 MAUI |
| `CommunityToolkit.Maui` | 9.1.1 | Kompatibel mit .NET 9 |
| `CommunityToolkit.Maui.MediaElement` | 4.1.1 | Kompatibel mit .NET 9 |

**Erlaubt zu updaten:**
- ✅ `Microsoft.Extensions.*` (plattformunabhängig)
- ✅ `CommunityToolkit.Mvvm` (SharedUI, kein MAUI-Bezug)
- ✅ Andere Backend/Domain Packages

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
### **5. Event-to-Command** (Already Fixed - See `docs/XAML-BEHAVIORSEVENT-TO-COMMAND.md`)
- ❌ **Mistake:** `ListView_ItemClick` code-behind handlers (complex fallback logic, 40+ LOC per handler)
- ✅ **Solution:** Custom `ListViewItemClickBehavior` with direct EventArgs extraction
- 📉 **Impact:** -200 LOC code-behind, clean MVVM separation, reusable patterns
- 📖 **NuGet:** `Microsoft.Xaml.Behaviors.WinUI.Managed` Version **3.0.0**

### **6. Coordinate-Based Track Plan (Dec 2025)** ⭐ NEW
- ❌ **Mistake:** Storing absolute coordinates (PathData, CenterX/Y) in Domain model
  - 1000+ LOC ViewModels with coordinate calculations
  - Zoom/Pan logic causing flicker
  - Endpoint tracking in code-behind
- ✅ **Solution:** Topologie-basiertes Design
  - **Domain:** Only ArticleCode, Rotation, Connections (no coordinates)
  - **Renderer:** Calculates positions from topology + Piko geometry at runtime
  - **Import:** Use AnyRail PathData directly (already has correct coordinates)
- 📉 **Impact:** -700 LOC, simpler architecture, no flicker

**Topologie-basiertes Design Pattern:**
```
┌─────────────────────────────────────────────────────────────────┐
│ JSON (what we store)           │ Runtime (what we calculate)   │
├─────────────────────────────────────────────────────────────────┤
│ TrackLayout:                   │ TrackLayoutRenderer:           │
│   Segments:                    │   - BFS traversal of graph     │
│     - ArticleCode: "G231"      │   - PikoATrackLibrary lookup   │
│     - Rotation: 30             │   - Endpoint alignment         │
│     - AssignedInPort: 1        │   → X, Y, PathData (computed)  │
│   Connections:                 │                                │
│     - Segment1Id + Endpoint    │                                │
│     - Segment2Id + Endpoint    │                                │
└─────────────────────────────────────────────────────────────────┘

```
**Hybrid Approach for Imports:**
```csharp
// AnyRail Import: Use original coordinates directly
vm.PathData = part.ToPathData();  // Already has absolute coords
vm.X = 0; vm.Y = 0;               // No offset needed

// New tracks from Library: Calculate from topology
var rendered = _renderer.Render(layout);
vm.PathData = rendered.PathData;  // Computed from connections

```
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

### **List Header Pattern (Title + Actions)**
```xaml
<!-- ✅ CORRECT: Title and Actions on same row -->
<Grid Margin="0,0,0,8">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <TextBlock
        Grid.Column="0"
        VerticalAlignment="Center"
        FontWeight="SemiBold"
        Style="{StaticResource SubtitleTextBlockStyle}"
        Text="Journeys" />
    <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="2">
        <Button Command="{x:Bind ...}" Padding="4" ToolTipService.ToolTip="Add">
            <FontIcon FontSize="14" Glyph="&#xE710;" />
        </Button>
        <Button Command="{x:Bind ...}" Padding="4" ToolTipService.ToolTip="Delete">
            <FontIcon FontSize="14" Glyph="&#xE74D;" />
        </Button>
    </StackPanel>
</Grid>

```
**Key:** 
- Title left, Actions right (compact header)
- Use `Button` + `FontIcon` for desktop apps (compact, ~30% smaller than AppBarButton)
- Use `AppBarButton` only for touch-first apps or CommandBar
- `Padding="4"` + `Spacing="2"` + `FontSize="14"` for tight layout
- Use CommandBar only when >3 buttons with labels needed (e.g., Actions: Announcement, Command, Audio, Delete)

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
**Key:**
Lower `DynamicOverflowOrder` = Higher priority (stays visible longer)

### **Library/Lookup List Pattern (Read-Only)**
```xaml
<!-- ✅ CORRECT: Visually distinct from editable lists -->
<Grid Padding="12"
      Margin="8"
      Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
      CornerRadius="8">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="6">
        <FontIcon FontSize="14" Glyph="&#xE8F1;"
                  Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
        <TextBlock Text="City Library"
                   Style="{StaticResource BodyStrongTextBlockStyle}"
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
    </StackPanel>
    <TextBlock Grid.Row="1" Margin="0,4,0,12"
               Text="Drag or double-click to add"
               Style="{StaticResource CaptionTextBlockStyle}"
               Foreground="{ThemeResource TextFillColorTertiaryBrush}" />
    <ListView Grid.Row="2" ... />
</Grid>

```
**Key:**
- Background: `CardBackgroundFillColorDefaultBrush` (subtle distinction)
- Header: Secondary color, smaller font (`BodyStrongTextBlockStyle`)
- Icon: Library icon `&#xE8F1;`
- Hint text: Tertiary color, explains interaction
- `Margin="8"` for breathing room around the card
- `Padding="12"` for comfortable inner spacing
- `CornerRadius="8"` for modern card appearance

### **Fluent Design 2**
- ✅ **Spacing:** Padding="16" Spacing="16" (consistent 16px)
- ✅ **Typography:** `{ThemeResource SubtitleTextBlockStyle}`
- ✅ **Theme-Aware:** `{ThemeResource TextFillColorSecondaryBrush}`
- ✅ **Modern Controls:** NumberBox (SpinButtonPlacementMode="Inline"), TimePicker

---

## 📚 Deep-Dive Documentation (Load on Demand)

### **Layer-Specific Instructions**
- `docs/copilot/backend.instructions.md` - Platform-independent patterns
- `docs/copilot/winui.instructions.md` - WinUI 3 UI patterns
- `docs/copilot/maui.instructions.md` - Mobile patterns
- `docs/copilot/blazor.instructions.md` - Web patterns
- `docs/copilot/test.instructions.md` - Testing guidelines
- `docs/copilot/hasunsavedchanges-patterns.instructions.md` - State management

### **Architecture Documentation**
- `docs/ARCHITECTURE-INSIGHTS-2025-12-09.md` - Journey execution flow, SessionState, ViewModel 1:1 mapping
- `docs/XAML-BEHAVIORSEVENT-TO-COMMAND.md` - Event-to-Command pattern (XAML Behaviors v3.0)

### **User Documentation (Wiki)**
- `docs/wiki/INDEX.md` - Platform Overview & Quick Start
- `docs/wiki/MOBAFLOW-USER-GUIDE.md` - Windows Desktop Guide
- `docs/wiki/MOBASMART-USER-GUIDE.md` - Android Mobile Guide  
- `docs/wiki/MOBASMART-WIKI.md` - Android Extended Documentation
- `docs/wiki/MOBADASH-USER-GUIDE.md` - Blazor Web Guide

### **Technical Documentation (Copilot)**
- `docs/copilot/MAUI-PERSISTIERUNG-TEST.md` - Settings persistence testing
- `docs/copilot/MAUI-BACKGROUND-SERVICE-CONCEPT.md` - Background service concept
- `docs/copilot/ICON-UPDATE-GUIDE.md` - WinUI 3 App Icon Update Guide
