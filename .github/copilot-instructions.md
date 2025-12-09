# MOBAflow - Master Instructions (Ultra-Compact)

> **Multi-platform railway automation control system (.NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)

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
6. **Nested Objects in Domain** (`Journey.Stations = List<Station>`) → Circular refs
7. **INotifyPropertyChanged in Domain** → Architecture violation
8. **DispatcherQueue in Backend** → Platform dependency (use IUiDispatcher)
9. **Click-Handlers in XAML** → Use Commands instead
10. **Static Collections** → Memory leak risk

**Action:** If >3 Red Flags found → Deep-dive analysis required.

---

## 🤖 Context-Aware Loading (AI: Auto-Trigger)

**Pattern:** Detect keywords in user request → Load matching instruction file.

| User Mentions | Auto-Load File | Trigger Keywords |
|---------------|----------------|------------------|
| **Backend Work** | `.github/instructions/backend.instructions.md` | *Manager.cs, *Service.cs, SessionState, IUiDispatcher, JourneyManager |
| **WinUI UI** | `.github/instructions/winui.instructions.md` | .xaml, EditorPage, ContentControl, x:Bind, DataTemplate, SelectorBar |
| **ViewModel** | `.github/instructions/winui.instructions.md` | *ViewModel.cs, ObservableProperty, RelayCommand, MVVM |
| **Domain Entities** | Inline rules below | Journey, Station, Train, Locomotive, Workflow (check: GUID refs?) |
| **Tests** | `.github/instructions/test.instructions.md` | Test.csproj, [Test], FakeUdpClient, NUnit |
| **State Management** | `.github/instructions/hasunsavedchanges-patterns.instructions.md` | UndoRedo, HasUnsavedChanges, StateManager |
| **MAUI Mobile** | `.github/instructions/maui.instructions.md` | .razor, MainThread, MOBAsmart |
| **Blazor Web** | `.github/instructions/blazor.instructions.md` | .razor, MOBAdash, @code |

**Execution:** Before answering, scan keywords → Execute `get_file(<instruction_file>)` → Apply rules.

---

## 🏗️ Architecture Quick Reference (Ultra-Compact)

### **Domain Layer (Pure POCOs)**
- ✅ **YES:** Pure C# classes, GUID references (`List<Guid> StationIds`), Value Objects
- ❌ **NO:** INotifyPropertyChanged, Attributes, Nested objects (`List<Station>`), UI code

### **Backend Layer (Platform-Independent)**
- ✅ **YES:** Business logic, IUiDispatcher abstraction, SessionState (runtime data)
- ❌ **NO:** DispatcherQueue, MainThread, UI thread code, platform dependencies

### **SharedUI Layer (ViewModels)**
- ✅ **YES:** CommunityToolkit.Mvvm, Resolve GUID refs at runtime, Commands, ObservableProperty
- ❌ **NO:** Platform-specific code (DispatcherQueue, MainThread)

### **WinUI Layer (Desktop UI)**
- ✅ **YES:** `x:Bind` (compiled), ContentControl + DataTemplateSelector, Commands, Fluent Design 2
- ❌ **NO:** `Binding` (slow), Custom PropertyGrids (use DataTemplates!), Click-Handlers, Code-Behind logic

### **MAUI Layer (Mobile UI)**
- ✅ **YES:** MainThread.BeginInvokeOnMainThread, ContentView, MAUI-specific controls
- ❌ **NO:** WinUI-specific APIs, Desktop-only patterns

### **Test Layer**
- ✅ **YES:** Fake objects (FakeUdpClient), Dependency Injection, NUnit
- ❌ **NO:** Mocks in production code, Hardware in tests, Static dependencies

---

## 🎯 Current Project Status (Dec 2025)

### **Active Refactoring**
- ⚠️ **Reference-Based Domain Architecture** (72% complete)
  - Domain: GUID refs ✅ | Backend: Complete ✅ | ViewModels: In progress 🚧
  - See: `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md`

### **Known Issues**
- 🚨 **64+ Build Errors** (ViewModel refactoring in progress)
- ⚠️ **11+ Failing Tests** (Reference resolution changes)

### **Recent Wins (Dec 2025)**
- ✅ **PropertyGrid Modernization** → -70% code, native WinUI 3 patterns
  - Old: SimplePropertyGrid (350 LOC, Reflection)
  - New: ContentControl + DataTemplateSelector (200 LOC XAML)
  - See: `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md`

---

## 🚨 Past Mistakes (Never Repeat!)

### **1. PropertyGrid Anti-Pattern (Dec 2025)**
- ❌ **Mistake:** Custom Reflection-based PropertyGrid (350 LOC)
- ✅ **Solution:** ContentControl + DataTemplateSelector (native WinUI 3)
- 📉 **Impact:** -480 LOC (-70%), compiled bindings, native patterns
- 📖 **Details:** `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md`

### **2. Nested Objects in Domain (Dec 2025)**
- ❌ **Mistake:** `Journey.Stations = List<Station>` (Circular refs, JSON hell)
- ✅ **Solution:** `Journey.StationIds = List<Guid>` + ViewModel resolution
- 📉 **Impact:** Clean JSON, no circular refs, testable
- 📖 **Details:** `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md`

### **3. ClearOtherSelections Complexity**
- ❌ **Mistake:** Manual selection cleanup logic (35 LOC)
- ✅ **Solution:** ContentControl automatic template switching
- 📉 **Impact:** -35 LOC, automatic behavior, simpler code

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

### **Fluent Design 2**
- ✅ **Spacing:** Padding="16" Spacing="16" (consistent 16px)
- ✅ **Typography:** `{ThemeResource SubtitleTextBlockStyle}`
- ✅ **Theme-Aware:** `{ThemeResource TextFillColorSecondaryBrush}`
- ✅ **Modern Controls:** NumberBox (SpinButtonPlacementMode="Inline"), TimePicker

---



## 📚 Deep-Dive Architecture

- `docs/ARCHITECTURE-INSIGHTS-2025-12-09.md` - Journey execution flow, SessionState, ViewModel 1:1 mapping
## 📚 Deep-Dive Documentation (Load on Demand)

### **Layer-Specific Instructions**
- `.github/instructions/backend.instructions.md` - Platform-independent patterns
- `.github/instructions/winui.instructions.md` - WinUI 3 UI patterns
- `.github/instructions/maui.instructions.md` - Mobile patterns
- `.github/instructions/blazor.instructions.md` - Web patterns
- `.github/instructions/test.instructions.md` - Testing guidelines
- `.github/instructions/hasunsavedchanges-patterns.instructions.md` - State management

### **Architecture & Analysis**
- `docs/CODE-ANALYSIS-BEST-PRACTICES.md` - Full 5-step analysis methodology
- `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md` - PropertyGrid case study
- `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md` - Ongoing refactoring
- `docs/BUILD-ERRORS-STATUS.md` - Current build status
- `docs/Z21-PROTOCOL.md` - Hardware protocol docs

### **Session Reports** (Archive after 1 month)
- `docs/SEssION-SUMMARY-*.md` - Past session learnings

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
- ✅ **GUID References Only:** `Journey.StationIds = List<Guid>` (not `List<Station>`)
- ✅ **Single Source of Truth:** Project aggregate root has master lists
- ✅ **Pure POCOs:** No INotifyPropertyChanged, no attributes

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

### **UI Patterns**
- ✅ **x:Bind > Binding** (compiled vs runtime)
- ✅ **ContentControl + DataTemplateSelector** (not custom grids)
- ✅ **Commands > Click-Handlers** (MVVM-conform)

---

## ⚡ PowerShell 7 Terminal Rules (Copilot-Specific)

### ✅ Always use PowerShell 7 (pwsh)
- Assume Visual Studio DevShell with pwsh is active.

### ✅ Mandatory Session Setup
Start every snippet with:
```powershell
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
[Console]::InputEncoding=[Text.Encoding]::UTF8
$ProgressPreference='SilentlyContinue'
if ($Psstyle) { $Psstyle.OutputRendering='Ansi' }
```

### 🔍 Regex Safety Rules for Copilot
- **Immer Single-Quotes fuer Regex verwenden** (`'pattern'`), um String-Escapes zu vermeiden.
- **Escape korrekt setzen**:
  - `?` → `\?`
  - `(` → `\(`
  - `)` → `\)`
  - `.` → `\.`
- **Zeilenende matchen**: Nutze `$` fuer End-of-Line, um falsche Matches zu verhindern.
- **Beispiel fuer sicheres Matching**:
```powershell
if ($line -match 'private\s+TrainViewModel\?\s+selectedTrain;') { ... }
```
- **Vor komplexen Ersetzungen testen**:
```powershell
Select-String -Pattern 'private\s+TrainViewModel\?\s+selectedTrain;' -Path $file
```
- **Fuer einfache Ersetzungen**:
```powershell
$line -replace '\)$', ', IServiceProvider serviceProvider)'
```
- **Nie ungetestet in Einzeiler**: Bei komplexen Patterns → erst mit `Select-String` validieren.

---

**Last Updated:** 2025-12-09
**Version:** 3.1 (Ultra-Compact Master + Context-Aware Loading + PowerShell Terminal Rules + Regex Safety)