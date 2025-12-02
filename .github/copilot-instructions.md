---
description: MOBAflow coding standards, architecture rules, and platform-specific guidelines for WinUI, MAUI, and Blazor development
---

# MOBAflow - Copilot Instructions

> Multi-platform railway automation control system (.NET 10)
> - **MOBAflow** (WinUI) - Desktop control center
> - **MOBAsmart** (MAUI) - Android mobile app  
> - **MOBAdash** (Blazor) - Web dashboard

---

## 🗂️ Project Structure

| Project | Purpose | Framework |
|---------|---------|-----------|
| **Domain** | Pure POCOs - Domain models (NO dependencies!) | `net10.0` |
| **Backend** | Z21 protocol, business logic (platform-independent!) | `net10.0` |
| **SharedUI** | Base ViewModels, shared UI logic | `net10.0` |
| **WinUI** | Windows desktop app | `net10.0-windows10.0.17763.0` |
| **MAUI** | Android mobile app | `net10.0-android36.0` |
| **WebApp** | Blazor Server dashboard | `net10.0` |
| **Sound** | Audio/TTS functionality | `net10.0` |
| **Common** | Shared utilities and helpers | `net10.0` |
| **Test** | Unit tests | `net10.0` |

### Key Paths
- MAUI Styles: `MAUI/Resources/Styles/Styles.xaml`
- MAUI Colors: `MAUI/Resources/Styles/Colors.xaml`
- MAUI Entry Point: `MAUI/MauiProgram.cs`
- WinUI Entry Point: `WinUI/App.xaml.cs`
- Blazor Entry Point: `WebApp/Program.cs`

### Dependency Flow
```
WinUI   ──→ SharedUI ──→ Backend ──→ Domain
MAUI    ──→ SharedUI ──→ Backend ──→ Domain
WebApp  ──→ SharedUI ──→ Backend ──→ Domain
```

---

## 🏗️ Architecture Rules (CRITICAL!)

### ✅ Domain Models MUST be Pure POCOs

**The `Domain` project contains ONLY pure data classes — NO logic, NO attributes!**

```csharp
// ✅ CORRECT: Pure POCO in Domain
namespace Moba.Domain;

public class Station
{
    public string Name { get; set; }
    public int Track { get; set; } = 1;
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public Workflow? Flow { get; set; }
    public Guid? WorkflowId { get; set; }
}

// ❌ NEVER: Serialization attributes in Domain
using System.Text.Json.Serialization;

public class Station
{
    [JsonConverter(typeof(CustomConverter))]  // ❌ WRONG!
    public string Name { get; set; }
    
    [JsonPropertyName("track_number")]  // ❌ WRONG!
    public int Track { get; set; }
}

// ❌ NEVER: Validation attributes in Domain
using System.ComponentModel.DataAnnotations;

public class Station
{
    [Required]  // ❌ WRONG!
    [StringLength(100)]  // ❌ WRONG!
    public string Name { get; set; }
}
```

**Forbidden in Domain:**
- ❌ `[JsonConverter]`, `[JsonPropertyName]`, `[JsonIgnore]`
- ❌ `[Required]`, `[Range]`, `[StringLength]`
- ❌ `INotifyPropertyChanged`, `ObservableObject`
- ❌ Business logic methods
- ❌ Dependencies on other projects (except `System.*`)

**Where to put serialization logic:**
- **Custom Converters** → `Backend.Converters` or `Common.Converters`
- **Validation** → `Backend.Services.ValidationService` or ViewModels
- **Property change notifications** → `SharedUI.ViewModel.*ViewModel`

**Example: Handling complex JSON serialization**

```csharp
// ✅ CORRECT: Custom converter in Backend/Common
namespace Moba.Backend.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;
using Moba.Domain;

public class WorkflowReferenceConverter : JsonConverter<Workflow?>
{
    public override Workflow? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Complex deserialization logic here
        var workflowId = reader.GetGuid();
        return ResolveWorkflowById(workflowId);
    }
    
    public override void Write(Utf8JsonWriter writer, Workflow? value, JsonSerializerOptions options)
    {
        // Serialize as WorkflowId instead of full object
        writer.WriteStringValue(value?.Id.ToString());
    }
}

// Register in JsonSerializerOptions (NOT in Domain class!)
var options = new JsonSerializerOptions();
options.Converters.Add(new WorkflowReferenceConverter());
```

### ✅ Backend Must Stay Platform-Independent

**The `Backend` project MUST remain 100% platform-independent!**

```csharp
// ❌ NEVER: Platform-specific code in Backend
namespace Moba.Backend.Manager;
public class JourneyManager
{
#if WINDOWS
    await DispatchToUIThreadAsync(...);  // ❌ BREAKS CROSS-PLATFORM!
#endif
}

// ✅ ALWAYS: Handle threading in platform-specific ViewModels
namespace Moba.WinUI.ViewModel;
public class JourneyViewModel : SharedUI.ViewModel.JourneyViewModel
{
    private readonly DispatcherQueue _dispatcher;
    
    protected override void OnModelPropertyChanged()
    {
        _dispatcher.TryEnqueue(() => NotifyPropertyChanged());  // ✅
    }
}
```

**Forbidden in Backend:**
- ❌ `DispatcherQueue` (WinUI)
- ❌ `MainThread.BeginInvokeOnMainThread()` (MAUI)
- ❌ `#if WINDOWS`, `#if ANDROID`
- ❌ Any UI framework references

---

### ✅ City Library Architecture

**City Library** (`germany-stations.json`) is **NOT part of user Solution/Project structure**:

```
📁 germany-stations.json         ← Master data (read-only)
   └── Cities[]
       └── Stations[]            ← Templates for Station creation

📁 User Solution (.mobaflow)    ← User's project data
   └── Projects[]
       └── Journeys[]
           └── Stations[]        ← Created from City templates
```

**Key Principles**:
1. **City = Master Data** (read-only, not saved in .mobaflow)
2. **Station = User Data** (created in Journey, saved in .mobaflow)
3. **City → Station**: User selects City, creates Station copy in Journey
4. **Domain.City**: Only used as selection helper, NOT in Project structure

**JSON Serialization**:
- ✅ **ALWAYS use Newtonsoft.Json** for all JSON operations
- ❌ **NEVER use System.Text.Json** (inconsistency with StationConverter)
- ✅ Simple POCOs: `JsonConvert.DeserializeObject<T>(json)` (no complex options!)
- ✅ Custom converters: Only in `Backend.Converter` for complex scenarios

**Example: Simple deserialization**
```csharp
// ✅ CORRECT: Simple Newtonsoft.Json deserialization
var cities = JsonConvert.DeserializeObject<List<City>>(json);

// ❌ WRONG: Complex System.Text.Json options
var options = new JsonSerializerOptions { /* many options */ };
var cities = JsonSerializer.Deserialize<List<City>>(json, options);
```

**City Library Service**:
- Located in platform projects (WinUI/MAUI/WebApp)
- Loads `germany-stations.json` via `AppSettings.CityLibrary.FilePath`
- Provides `LoadCitiesAsync()` for UI selection dialogs
- Cached after first load (master data rarely changes)

**Station Creation Flow**:
```
1. User opens Journey Editor
2. Clicks "Add Station from City Library"
3. CityLibraryService.LoadCitiesAsync() → Shows selection dialog
4. User selects "München Hauptbahnhof"
5. Copy City.Stations[0] → Create new Station(Name, Track, etc.)
6. Add Station to Journey.Stations
7. Save Journey (Station is now part of .mobaflow file)
```

---

## 📁 File Organization

### One Class Per File
- File name MUST match class name: `JourneyManager.cs` ← `class JourneyManager`
- ❌ Never put multiple public classes in one file

### Namespace Conventions

**Rule:** Namespace = RootNamespace + folder structure

```csharp
// ✅ CORRECT
// File: Backend/Manager/JourneyManager.cs
namespace Moba.Backend.Manager;

// File: WinUI/Service/IoService.cs  
namespace Moba.WinUI.Service;

// File: SharedUI/ViewModel/JourneyViewModel.cs
namespace Moba.SharedUI.ViewModel;
```

| Project | RootNamespace | Example |
|---------|---------------|---------|
| Backend | `Moba.Backend` | `Moba.Backend.Manager` |
| SharedUI | `Moba.SharedUI` | `Moba.SharedUI.ViewModel` |
| WinUI | `Moba.WinUI` | `Moba.WinUI.Service` |
| MAUI | `Moba.MAUI` | `Moba.MAUI.Service` |
| WebApp | `Moba.WebApp` | `Moba.WebApp.Service` |
| Common | `Moba.Common` | `Moba.Common.Extensions` |

### Using Directives

```csharp
// ✅ ALWAYS use absolute namespaces
using Moba.Backend.Model;
using Moba.SharedUI.Service;

// ❌ NEVER use relative namespaces
using Backend.Model;  // Ambiguous!
```

---

## 💉 Dependency Injection

📄 **Detailed DI Guidelines**: See [docs/DI-INSTRUCTIONS.md](docs/DI-INSTRUCTIONS.md)

### Core Principles
1. **Backend stays platform-independent** - no platform-specific APIs
2. **All I/O abstracted** behind interfaces (`IUdpClientWrapper`, `IZ21`)
3. **Prefer constructor injection** - avoid `new` in UI layers
4. **DI per platform** - register explicitly in each app's entry point

### Registration Example

```csharp
// WinUI: App.xaml.cs
services.AddSingleton<IUiDispatcher, WinUIDispatcher>();
services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<Backend.Model.Solution>();
services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();

// MAUI: MauiProgram.cs  
builder.Services.AddSingleton<IUiDispatcher, MauiDispatcher>();
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<IZ21, Z21>();
builder.Services.AddSingleton<Backend.Model.Solution>();
```

### Lifetime Selection
- **Singleton**: Application-wide state, shared instances (`Solution`, `IZ21`)
- **Scoped**: Per-request state (Blazor only)
- **Transient**: Stateless, cheap to create

### ViewModel Factory Pattern

```csharp
// Interface in SharedUI
public interface IJourneyViewModelFactory
{
    JourneyViewModel Create(Journey model);
}

// Platform-specific implementation in WinUI
public class WinUIJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;
    
    public WinUIJourneyViewModelFactory(IUiDispatcher dispatcher)
        => _dispatcher = dispatcher;
    
    public JourneyViewModel Create(Journey model)
        => new WinUI.ViewModel.JourneyViewModel(model, _dispatcher);
}
```

---

## 🧪 Testing

### Unit Test Structure
```
Test/
├── Backend/         # Backend logic tests
├── SharedUI/        # ViewModel tests  
├── WinUI/          # WinUI-specific tests (DI, etc.)
├── WebApp/         # Blazor-specific tests
└── TestBase/       # Shared test utilities
```

### Testing Checklist
- ✅ Use `FakeUdpClientWrapper` to avoid real UDP
- ✅ Prefer typed events over byte-array assertions
- ✅ Update DI tests when adding services
- ✅ Mock backend interfaces, not concrete classes
- ✅ Tests must run without UI framework

---

## 🚀 Pre-Commit Checklist

### Build
- [ ] `run_build` successful
- [ ] No compiler warnings
- [ ] All `using` statements present
- [ ] Test stubs updated for interface changes

### Technical
- [ ] Backend has NO platform-specific code
- [ ] UI updates dispatched correctly (MAUI: `MainThread`, WinUI: `DispatcherQueue`)
- [ ] All I/O uses async/await
- [ ] No `.Result` or `.Wait()` on async code
- [ ] File names match class names
- [ ] Namespaces follow folder structure
- [ ] XML documentation in English

### Quality
- [ ] Responsive layout tested
- [ ] Loading states visible for async operations
- [ ] Error messages user-friendly
- [ ] Keyboard navigation works
- [ ] Unit tests pass

---

## 📚 Additional Documentation

- **Architecture**: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System design, layer separation
- **Clean Architecture**: [docs/CLEAN-ARCHITECTURE-FINAL-STATUS.md](docs/CLEAN-ARCHITECTURE-FINAL-STATUS.md) - Clean Architecture implementation status
- **DI Details**: [docs/DI-INSTRUCTIONS.md](docs/DI-INSTRUCTIONS.md) - Full dependency injection guidelines
- **Threading**: [docs/THREADING.md](docs/THREADING.md) - UI thread dispatching patterns
- **Best Practices**: [docs/BESTPRACTICES.md](docs/BESTPRACTICES.md) - C# coding standards
- **UX Guidelines**: [docs/UX-GUIDELINES.md](docs/UX-GUIDELINES.md) - Detailed usability patterns
- **MAUI Guidelines**: [docs/MAUI-GUIDELINES.md](docs/MAUI-GUIDELINES.md) - MAUI-specific development guidelines
- **Z21 Protocol**: [docs/Z21-PROTOCOL.md](docs/Z21-PROTOCOL.md) - Z21 communication reference
- **Build Status**: [docs/BUILD-ERRORS-STATUS.md](docs/BUILD-ERRORS-STATUS.md) - Current build status and known issues
- **Testing**: [docs/TESTING-SIMULATION.md](docs/TESTING-SIMULATION.md) - Testing with fakes and simulation

---

## 🔄 State Management Best Practices

### HasUnsavedChanges & Undo/Redo Integration

**Always pair HasUnsavedChanges with UndoRedoManager:**

```csharp
// ✅ CORRECT
private void OnPropertyChanged()
{
    _undoRedoManager.SaveStateThrottled(Solution);
    HasUnsavedChanges = true;  // Mark as modified
}

[RelayCommand]
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null && Solution != null)
    {
        Solution.UpdateFrom(previous);
        HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();  // Check saved state
    }
}
```

**Clear history on New/Load:**

```csharp
// ✅ CORRECT
[RelayCommand]
private async Task NewSolutionAsync()
{
    _undoRedoManager.ClearHistory();  // Clear old history
    Solution.UpdateFrom(newSolution);
    await _undoRedoManager.SaveStateImmediateAsync(Solution);
    HasUnsavedChanges = true;
}
```

**Mark saved state after Save:**

```csharp
// ✅ CORRECT
[RelayCommand]
private async Task SaveSolutionAsync()
{
    await _ioService.SaveAsync(Solution, path);
    HasUnsavedChanges = false;
    _undoRedoManager.MarkCurrentAsSaved();  // Mark saved point
}
```

### NULL Checks

**Always check Solution and nested properties:**

```csharp
// ✅ CORRECT
if (Solution?.Settings != null && 
    !string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))
{
    await _z21.ConnectAsync(Solution.Settings.CurrentIpAddress);
}

// ❌ WRONG
if (!string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))  // Can throw!
{
    // ...
}
```

---

## 🎯 Domain Model Overview

### Core Entities
```
Solution
├── Journeys (sequence of stations)
│   └── Stations (stops with entry/exit tracks)
├── Workflows (automation sequences)  
│   └── Actions (Z21 commands)
└── Trains
    ├── Locomotives (with addresses)
    └── Wagons (rolling stock)
```

### Data Flow
1. **User Input** (WinUI/MAUI/Blazor) → ViewModel
2. **ViewModel** → Backend Model
3. **Backend Model** → Z21 Protocol (`IZ21`)
4. **Z21** → UDP Network (`IUdpClientWrapper`)
5. **Feedback** ← Events ← Backend → ViewModel → UI

---

# Copilot Instructions - Additional Rules

## 🔧 Technical Guidelines for AI Assistant

### File Encoding & Line Endings

**CRITICAL**: Always ensure consistent file encoding and line endings

```
- Use UTF-8 encoding for all text files
- Use CRLF (Windows) line endings for .cs, .xaml, .md files
- Never mix line endings within a file
```

**Implementation**:
- When using `create_file`: Content should have consistent CRLF line endings
- When using `replace_string_in_file`: Match existing line ending style
- Never use PowerShell piping in terminal for file operations (causes encoding issues)

### Terminal Command Restrictions

**AVOID** the following in `run_command_in_terminal`:

```powershell
# ❌ NEVER: foreach loops (user cancellation issues)
foreach ($file in $files) { ... }

# ❌ NEVER: Long-running commands with piping
Get-Content file.txt | Where-Object { ... } | Set-Content ...

# ❌ NEVER: Multiple commands in one line with semicolons
cmd1; cmd2; cmd3
```

**INSTEAD USE**:

```powershell
# ✅ ALWAYS: Simple, direct commands
Get-ChildItem "path" | Select-Object Name

# ✅ ALWAYS: BAT files for complex operations
# Create a .bat file and execute it

# ✅ ALWAYS: Direct file operations
Move-Item "source" "destination" -Force
```

**Best Practice**:
1. For file operations with multiple files → Create a `.bat` file
2. For simple queries → Use direct PowerShell commands
3. For encoding-sensitive operations → Use `[System.IO.File]::WriteAllText()` with explicit encoding

### Documentation Cleanup Automation

**RULE**: After each session, automatically archive completed documentation

**Trigger**: When session summary is created or major task completed

**Action**:
1. Identify documentation files with patterns:
   - `SESSION-SUMMARY-*.md` (except current)
   - `*-COMPLETE.md` (completed tasks)
   - `*-FIX.md` (completed fixes)
   - `*-MIGRATION.md` (completed migrations)
   - `*-STATUS.md` (old status reports, except BUILD-ERRORS-STATUS.md)

2. Move to `docs/archive/` using `.bat` file approach

3. Update `docs/DOCS-STRUCTURE-FINAL.md` if needed

**Exception**: Keep these files:
- Current session summary
- BUILD-ERRORS-STATUS.md
- CLEAN-ARCHITECTURE-FINAL-STATUS.md
- Any *-PLAN.md for ongoing work

### Build & Test Verification

**MANDATORY** before completing any code changes:

```
1. Run `run_build` to verify compilation
2. Check for compiler warnings (should be 0)
3. If tests affected:
   - Verify test stubs updated
   - Run affected tests if possible
4. Document known issues in BUILD-ERRORS-STATUS.md
```

**If build fails**:
1. Record observation with error details
2. Fix critical errors (DI, namespaces)
3. Document test errors if extensive refactoring needed
4. Create follow-up task in TODO-*.md

**Test Refactoring Rules**:
- If > 10 test errors: Defer to dedicated session
- If < 10 test errors: Fix immediately
- Always document deferred test work

### Session Completion Checklist

Before finishing work session:

- [ ] `run_build` executed (even if tests fail)
- [ ] Build errors documented in BUILD-ERRORS-STATUS.md
- [ ] Session summary created
- [ ] Old session reports moved to archive
- [ ] Copilot instructions updated if new patterns discovered
- [ ] TODO file updated with next steps

---

## 🎯 Priority Rules

### High Priority (Fix Immediately)
1. DI registration errors
2. Namespace errors
3. Build-blocking compilation errors
4. Production code errors

### Medium Priority (Fix or Document)
1. Test compilation errors (< 10)
2. Warnings in production code
3. Missing XML documentation

### Low Priority (Defer to Dedicated Session)
1. Test refactoring (> 10 errors)
2. Performance optimizations
3. Code cleanup tasks

---

## 📝 Documentation Maintenance

### Monthly Tasks
- Archive old SESSION-SUMMARY-*.md files (keep only current month)
- Remove completed *-PLAN.md files
- Update BUILD-ERRORS-STATUS.md

### After Each Session
- Create session summary if significant work done
- Move completed task docs to archive
- Update relevant documentation references

### Annual Tasks
- Review and consolidate guidelines
- Archive old year's session summaries
- Major documentation restructure if needed

---

**These rules should be followed by AI assistants to maintain code quality and documentation hygiene.**
