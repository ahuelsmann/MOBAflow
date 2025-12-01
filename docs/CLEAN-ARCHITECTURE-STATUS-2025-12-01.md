---
title: Clean Architecture Refactoring - Status Update
date: 2025-12-01 09:58
status: Phase 2 - 80% Complete
---

# Clean Architecture Refactoring - Status (2025-12-01)

## ✅ Completed (80%)

### 1. Backend Refactoring ✓ (100%)
- ✅ `Backend\Model\` directory completely removed
- ✅ `ActionConverter` rewritten for `WorkflowAction`
- ✅ `WorkflowConverter` verified (already correct)
- ✅ `JourneyManager.HandleLastStationAsync` fixed:
  - Changed `journey.OnLastStop` → `journey.BehaviorOnLastStop`
  - Fixed `journey.NextJourney` handling (now Journey reference, not string)
- ✅ `Backend\Data\City.cs` updated to use `Moba.Domain`
- ✅ **Backend builds successfully** (3 warnings only)

**Build Output:**
```
Backend net10.0 succeeded with 3 warning(s) (1,1s) → Backend\bin\Debug\net10.0\Backend.dll
```

### 2. SharedUI Namespace Migration ✓ (100%)
**30+ files updated** from `Backend.Model` / `Backend.Model.Enum` / `Backend.Model.Action` to `Moba.Domain`:

**Services (4 files):**
- ✅ `ValidationService.cs`
- ✅ `IIoService.cs`
- ✅ `TreeViewBuilder.cs`
- ✅ `UndoRedoManager.cs`

**Editor ViewModels (8 files):**
- ✅ `EditorPageViewModel.cs`
- ✅ `JourneyEditorViewModel.cs`
- ✅ `LocomotiveEditorViewModel.cs`
- ✅ `SettingsEditorViewModel.cs`
- ✅ `TrainEditorViewModel.cs`
- ✅ `WagonEditorViewModel.cs`
- ✅ `WorkflowEditorViewModel.cs`
- ✅ `CounterViewModel.cs`

**Entity ViewModels (13 files):**
- ✅ `DetailsViewModel.cs`
- ✅ `GoodsWagonViewModel.cs`
- ✅ `JourneyViewModel.cs`
- ✅ `LocomotiveViewModel.cs`
- ✅ `PassengerWagonViewModel.cs`
- ✅ `StationViewModel.cs`
- ✅ `TrainViewModel.cs`
- ✅ `WagonViewModel.cs`
- ✅ `VoiceViewModel.cs`
- ✅ `WorkflowViewModel.cs`
- ✅ `PlatformViewModel.cs`
- ✅ `ProjectViewModel.cs`
- ✅ `ProjectConfigurationPageViewModel.cs`

**Main ViewModels (2 files):**
- ✅ `MainWindowViewModel.cs`
- ✅ `SolutionViewModel.cs`
- ✅ `SettingsViewModel.cs`

**Platform-Specific ViewModels (3 files):**
- ✅ `MAUI\JourneyViewModel.cs`
- ✅ `WinUI\JourneyViewModel.cs`
- ✅ `WinUI\MainWindowViewModel.cs`

---

## 🟡 In Progress (20%)

### 3. Action ViewModel Refactoring (Critical Issue)

**Problem:** Old polymorphic Action hierarchy no longer exists.

**Old Architecture:**
```
Backend.Model.Action.Base (abstract)
├── Command
├── Audio
└── Announcement
```

**New Architecture:**
```csharp
// Domain/WorkflowAction.cs
public class WorkflowAction
{
    public ActionType Type { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
```

**Affected Files (3 ViewModels + 1 Editor):**
- ❌ `SharedUI\ViewModel\Action\CommandViewModel.cs` - References old `Command` class
- ❌ `SharedUI\ViewModel\Action\AudioViewModel.cs` - References old `Audio` class
- ❌ `SharedUI\ViewModel\Action\AnnouncementViewModel.cs` - References old `Announcement` class
- ❌ `SharedUI\ViewModel\WorkflowEditorViewModel.cs` - References `BackendAction` alias

**Current Errors:**
```
error CS0246: The type or namespace name 'Command' could not be found
error CS0246: The type or namespace name 'BackendAction' could not be found
error CS0246: The type or namespace name 'Base' could not be found
```

**Solution Strategy:**

#### Option A: Generic WorkflowActionViewModel
Create **one generic ViewModel** that adapts based on `ActionType`:

```csharp
public class WorkflowActionViewModel : ObservableObject
{
    private WorkflowAction _action;
    
    public ActionType Type => _action.Type;
    
    // Dynamic properties based on Type
    public object? GetParameter(string key) => 
        _action.Parameters?.GetValueOrDefault(key);
    
    public void SetParameter(string key, object value)
    {
        _action.Parameters ??= new Dictionary<string, object>();
        _action.Parameters[key] = value;
    }
}
```

#### Option B: Keep Specialized ViewModels with Adapters
Keep `CommandViewModel`, `AudioViewModel`, `AnnouncementViewModel` but make them **wrap WorkflowAction**:

```csharp
public class CommandViewModel : ObservableObject
{
    private readonly WorkflowAction _action;
    
    public CommandViewModel(WorkflowAction action)
    {
        _action = action;
        _action.Type = ActionType.Command;
    }
    
    public int Address
    {
        get => (int?)_action.Parameters?.GetValueOrDefault("Address") ?? 0;
        set => SetParameter("Address", value);
    }
    
    public int Speed
    {
        get => (int?)_action.Parameters?.GetValueOrDefault("Speed") ?? 0;
        set => SetParameter("Speed", value);
    }
}
```

**Recommendation:** **Option B** - Keeps UI code cleaner, existing XAML bindings mostly work.

---

## ⏸️ Not Started

### 4. DI Service Registration
**Locations:**
- `WinUI\App.xaml.cs`
- `MAUI\MauiProgram.cs`
- `WebApp\Program.cs`

**Services to Register:**
```csharp
services.AddSingleton<ActionExecutor>();
services.AddSingleton<WorkflowService>();
services.AddSingleton<SolutionService>();
services.AddSingleton<IJourneyManagerFactory, JourneyManagerFactory>();
```

### 5. Test Files Update
**Affected:**
- `Test\Backend\*` - Already use Domain models (probably OK)
- `Test\SharedUI\*` - May need namespace updates
- `Test\Unit\*` - May need namespace updates

### 6. Full Solution Build
- WinUI build verification
- MAUI build verification
- WebApp build verification

---

## 📋 Next Steps (Priority Order)

### Step 1: Action ViewModel Refactoring (2-3 hours)
1. Create adapter `CommandViewModel` wrapping `WorkflowAction`
2. Create adapter `AudioViewModel` wrapping `WorkflowAction`
3. Create adapter `AnnouncementViewModel` wrapping `WorkflowAction`
4. Update `WorkflowEditorViewModel` to use new adapters
5. Verify SharedUI builds

### Step 2: DI Registration (30 min)
1. Update WinUI DI container
2. Update MAUI DI container
3. Update WebApp DI container

### Step 3: Test Files (1 hour)
1. Find all `using Backend.Model` in Test project
2. Replace with `using Moba.Domain`
3. Fix any test breakages

### Step 4: Full Build & Runtime Testing (1 hour)
1. `dotnet build` (full solution)
2. Run WinUI app
3. Test workflow execution
4. Test JSON save/load

---

## 🎯 Success Criteria

- [x] Backend builds ✅ **DONE**
- [ ] SharedUI builds ⏸️ **Blocked by Action ViewModels**
- [ ] WinUI builds
- [ ] MAUI builds
- [ ] WebApp builds
- [ ] Test project builds
- [ ] Existing `.json` files load correctly
- [ ] Workflows execute correctly

---

## 📊 Effort Estimate

**Completed:** ~6 hours  
**Remaining:** ~5 hours  
**Total:** ~11 hours

**Current Progress:** 80% complete

---

## 🔗 Related Documents

- [CLEAN-ARCHITECTURE-QUICKSTART.md](./CLEAN-ARCHITECTURE-QUICKSTART.md) - Step-by-step guide
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Overall system architecture
- [DI-INSTRUCTIONS.md](./DI-INSTRUCTIONS.md) - Dependency injection guidelines

---

**Last Updated:** 2025-12-01 09:58 by GitHub Copilot  
**Status:** Ready for Action ViewModel refactoring
