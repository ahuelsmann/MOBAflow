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
6. **INotifyPropertyChanged in Domain** → Architecture violation
7. **DispatcherQueue in Backend** → Platform dependency (use IUiDispatcher)
8. **Event Handlers in Code-Behind** → Use XAML Behaviors (Event-to-Command) instead
   - **Exception:** Drag & Drop handlers are OK in code-behind (WinUI limitation)
9. **Static Collections** → Memory leak risk

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
### **Domain Layer (Pure POCOs)**
- ✅ **YES:** Pure C# classes, GUID references for shared entities (`List<Guid> LocomotiveIds`), Value Objects
- ✅ **YES:** Embedded objects for owned entities (`Journey.Stations = List<Station>` - Stations belong to Journey)
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
- ❌ **NO:** `Binding` (slow), Custom PropertyGrids (use DataTemplates!), Business logic in code-behind

### **MAUI Layer (Mobile UI)**
- ✅ **YES:** MainThread.BeginInvokeOnMainThread, ContentView, MAUI-specific controls
- ❌ **NO:** WinUI-specific APIs, Desktop-only patterns

### **Test Layer**
- ✅ **YES:** Fake objects (FakeUdpClient), Dependency Injection, NUnit
- ❌ **NO:** Mocks in production code, Hardware in tests, Static dependencies

---

## 🎯 Current Project Status (Dec 2025)

### **Active Refactoring**
- ✅ **Reference-Based Domain Architecture** (100% complete)
  - Domain: GUID refs ✅ | Backend: Complete ✅ | ViewModels: Complete ✅
  - See: `docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md`
- ✅ **Event-to-Command Migration** (100% complete)
  - All ListViews use XAML Behaviors ✅ | Code-behind handlers removed ✅
  - Version 3.0.0 unified namespaces ✅

### **Known Issues**
- ✅ **Resolved:** Build errors fixed (Event-to-Command + namespace unification)
- ✅ **Resolved:** ItemClick refresh issues (wiederholte Klicks funktionieren jetzt)

### **Recent Wins (Dec 2025)**
- ✅ **PropertyGrid Modernization** → -70% code, native WinUI 3 patterns
  - Old: SimplePropertyGrid (350 LOC, Reflection)
  - New: ContentControl + DataTemplateSelector (200 LOC XAML)
  - See: `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md`
- ✅ **Event-to-Command Pattern** → -200 LOC code-behind, clean MVVM
  - Old: `ListView_ItemClick` handlers in code-behind
  - New: XAML Behaviors with `ItemClickedCommand`
  - Migration to v3.0: Unified `Microsoft.Xaml.Interactivity` namespace

---

## 🚨 Past Mistakes (Never Repeat!)

### **1. PropertyGrid Anti-Pattern (Dec 2025)**
- ❌ **Mistake:** Custom Reflection-based PropertyGrid (350 LOC)
- ✅ **Solution:** ContentControl + DataTemplateSelector (native WinUI 3)
- 📉 **Impact:** -480 LOC (-70%), compiled bindings, native patterns
- 📖 **Details:** `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md`

### **2. ClearOtherSelections Complexity**
- ❌ **Mistake:** Manual selection cleanup logic (35 LOC)
- ✅ **Solution:** ContentControl automatic template switching
- 📉 **Impact:** -35 LOC, automatic behavior, simpler code

### **3. Event-to-Command**
- ❌ **Mistake:** `ListView_ItemClick` code-behind handlers (complex fallback logic, 40+ LOC per handler)
- ✅ **Solution:** Custom Behavior that directly extracts items from `ItemClickEventArgs`
- 📉 **Impact:** -200 LOC code-behind, clean MVVM separation, reusable patterns
- 🔧 **Pattern (v3.1 - Fixed):**
  ```xml
  <ListView IsItemClickEnabled="True" ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
      <i:Interaction.Behaviors>
          <local:ListViewItemClickBehavior Command="{x:Bind ViewModel.ItemClickedCommand}" />
      </i:Interaction.Behaviors>
      <ListView.ItemTemplate>
          <DataTemplate x:DataType="viewmodel:ItemViewModel">
              <!-- Template content -->
          </DataTemplate>
      </ListView.ItemTemplate>
  </ListView>
  ```
  
  **Why this works:**
  - ✅ **Direct EventArgs Extraction:** `e.ClickedItem` passed directly (no binding delays)
  - ✅ **No Synchronization Issues:** Avoids `CommandParameter="{Binding SelectedItem, ...}"` null problems
  - ✅ **Reusable:** One behavior for all ListView ItemClick scenarios
  
  **Custom Behavior Implementation:**
  ```csharp
  // WinUI/Behavior/ListViewItemClickBehavior.cs
  public sealed class ListViewItemClickBehavior : Behavior<ListView>
  {
      public static readonly DependencyProperty CommandProperty =
          DependencyProperty.Register(
              nameof(Command), typeof(ICommand), typeof(ListViewItemClickBehavior),
              new PropertyMetadata(null));

      public ICommand? Command
      {
          get => (ICommand?)GetValue(CommandProperty);
          set => SetValue(CommandProperty, value);
      }

      protected override void OnAttached()
      {
          base.OnAttached();
          if (AssociatedObject != null)
              AssociatedObject.ItemClick += OnListViewItemClick;
      }

      protected override void OnDetaching()
      {
          base.OnDetaching();
          if (AssociatedObject != null)
              AssociatedObject.ItemClick -= OnListViewItemClick;
      }

      private void OnListViewItemClick(object sender, ItemClickEventArgs e)
      {
          Command?.Execute(e.ClickedItem);  // ✅ Direct item from event
      }
  }
  ```

- 📖 **NuGet:** `Microsoft.Xaml.Behaviors.WinUI.Managed` Version **3.0.0** (unified namespaces, NativeAOT support)
- ⚠️ **Breaking Change 2.x→3.x:** Namespace unified to `Microsoft.Xaml.Interactivity` (remove `xmlns:ic="...Interactions.Core"`)

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
- `docs/XAML-BEHAVIORS-EVENT-TO-COMMAND.md` - Event-to-Command pattern (XAML Behaviors v3.0)
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
# ⚡ PowerShell 7 Terminal Rules (Copilot‑Specific)

> **Update focus:** Prevent one‑liner parser errors (e.g., `foreach` after an expression), index out‑of‑range on line slicing, and inconsistent writes. Prefer pipeline‑safe loops and idempotent edits.

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

## 7) File I/O & encoding (project policy)
- **Read:** `Get-Content -Raw`
- **Write:** UTF‑8 **with BOM** (pipeline requirement)
  ```powershell
  Set-Content $file $text -Encoding UTF8BOM
  # or
  Out-File $file -InputObject $text -Encoding UTF8BOM
  ```
- **Line Endings:** Windows (CR LF) - ensure all files use `\r\n`
- **No trailing empty lines:** No extra blank lines after the final closing brace `}`
- Quote paths with spaces; use double quotes when interpolating variables.

---

## 8) Robust edit templates (idempotent & error‑safe)

### 8.1 Insert a line **after a match** (multi‑line safe)
```powershell
$f = "path/to/MainWindowViewModel.cs"
$add = 'public event EventHandler? ExitApplicationRequested;'
$text = Get-Content $f -Raw
if ($text -notmatch [regex]::Escape($add)) {
  $nl   = [Environment]::NewLine
  $text = $text -replace '(?m)(^\s*#endregion\s*$)', $nl + $add + $nl + '$1'
  Set-Content $f $text -Encoding UTF8BOM
  Write-Host 'Added ExitApplicationRequested event'
} else { Write-Host 'Event already present' }
```

### 8.2 Insert at a specific **line index** (clamped, no out‑of‑range)
```powershell
$f = "path/to/MainWindowViewModel.cs"
$lines = Get-Content $f
$idx = 145
$idx = [math]::Min([math]::Max($idx,0), $lines.Count)   # clamp
$newLine = 'public event EventHandler? ExitApplicationRequested;'
$pre  = $lines | Select-Object -First $idx
$post = $lines | Select-Object -Skip  $idx
Set-Content $f ($pre + $newLine + $post) -Encoding UTF8BOM
Write-Host ("Inserted line at index {0}" -f $idx)
```

### 8.3 Replace literal text (whole‑file, no regex pitfalls)
```powershell
$f = "path/to/MainWindowViewModel.cs"
$sold = "[ObservableProperty]`r`nprivate City? selectedCity;`r`nregion"
$snew = "[ObservableProperty]`r`nprivate City? selectedCity;`r`npublic event EventHandler? ExitApplicationRequested;`r`nregion"
$text = Get-Content $f -Raw
$text = $text.Replace($sold, $snew)  # literal replace
Set-Content $f $text -Encoding UTF8BOM
Write-Host 'Replaced block and added event'
```

> Use **literal** `.Replace` when you can; prefer regex only when structure demands it.

---

## 9) Project root & solution fallback
```powershell
$gitRoot = (git rev-parse --show-toplevel 2>$null); if ($gitRoot -and (Test-Path $gitRoot)) { Set-Location $gitRoot }
$sln = Get-ChildItem -Path . -Filter *.sln -ErrorAction SilentlyContinue | Select-Object -First 1; if ($sln) { Set-Location $sln.Directory.FullName }
```

---

## 10) Quick reset for a “stuck” session
```powershell
try { Remove-Module PSReadLine -ErrorAction SilentlyContinue } catch {}
$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'
[Console]::OutputEncoding=[Text.Encoding]::UTF8; [Console]::InputEncoding=[Text.Encoding]::UTF8
if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) { $PSStyle.OutputRendering='Ansi' }
$env:GIT_PAGER='cat'
Write-Host 'Copilot terminal has been reset.' -ForegroundColor Green
```

---

## 11) Do **not** generate
- No reference to **`$PStyle`** (only **`$PSStyle`**).
- No Bash syntax in pwsh snippets unless explicitly asked to use *Git Bash*.
- No destructive one‑liners without a prior `Select-String` check.
- Do **not** place `foreach (...) {}` after an expression in one‑liners. Use `; foreach` or `... | ForEach-Object {}`.
- Do **not** slice arrays with `[0..N]` unless clamped; prefer `Select-Object -First / -Skip`.

---

## 12) Quick copy‑and‑use examples
```powershell
# Diff without pager, first 100 lines
git --no-pager diff .github/copilot-instructions.md | Select-Object -First 100

# Count matches and print if found
$count=(Select-String -Path . -Pattern 'EventTriggerBehavior' -List | Measure-Object).Count; if ($count -gt 0) { Write-Host "$count instances" }

# Pipeline-safe loop
git ls-files *.cs | ForEach-Object { $f = $_; if ((Select-String -Path $f -Pattern 'INotifyPropertyChanged' -List)) { Write-Host $f } }
```

---

**Last Updated:** 2025-12-10
**Version:** 3.3