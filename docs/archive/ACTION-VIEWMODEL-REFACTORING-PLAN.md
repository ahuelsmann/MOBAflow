---
title: Action ViewModel Refactoring Plan
date: 2025-12-01
status: Implementation Guide
---

# Action ViewModel Refactoring - Detailed Plan

## 🎯 Goal
Replace old polymorphic `Backend.Model.Action.*` classes with adapter ViewModels that wrap `Domain.WorkflowAction`.

---

## 📐 Architecture Decision: Option B (Adapter Pattern)

**Why?**
- ✅ Minimal XAML changes (existing bindings mostly work)
- ✅ Type-safe properties (`Address`, `Speed`, `FilePath`)
- ✅ Clear separation: ViewModel = UI concerns, WorkflowAction = data
- ✅ Easy to extend with validation logic

**Pattern:**
```
UI Binding → CommandViewModel.Address → WorkflowAction.Parameters["Address"]
```

---

## 📝 Step-by-Step Implementation

### Step 1: Create Base Class (Optional but Recommended)

**File:** `SharedUI\ViewModel\Action\WorkflowActionViewModel.cs` (NEW)

```csharp
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// Base class for Action ViewModels that wrap WorkflowAction.
/// Provides common functionality for parameter management.
/// </summary>
public abstract class WorkflowActionViewModel : ObservableObject
{
    protected readonly WorkflowAction _action;

    protected WorkflowActionViewModel(WorkflowAction action, ActionType type)
    {
        _action = action;
        _action.Type = type;
        _action.Parameters ??= new Dictionary<string, object>();
    }

    public Guid Id
    {
        get => _action.Id;
        set => SetProperty(_action.Id, value, _action, (a, v) => a.Id = v);
    }

    public string Name
    {
        get => _action.Name;
        set => SetProperty(_action.Name, value, _action, (a, v) => a.Name = v);
    }

    public int Number
    {
        get => _action.Number;
        set => SetProperty(_action.Number, value, _action, (a, v) => a.Number = v);
    }

    public ActionType Type => _action.Type;

    /// <summary>
    /// Gets the underlying WorkflowAction (for serialization).
    /// </summary>
    public WorkflowAction ToWorkflowAction() => _action;

    protected T? GetParameter<T>(string key)
    {
        if (_action.Parameters?.TryGetValue(key, out var value) == true)
        {
            if (value is T typedValue)
                return typedValue;
            
            // Handle type conversions (e.g., long → int, string → enum)
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    protected void SetParameter<T>(string key, T value)
    {
        _action.Parameters ??= new Dictionary<string, object>();
        
        if (EqualityComparer<T>.Default.Equals(value, GetParameter<T>(key)))
            return;
        
        if (value != null)
            _action.Parameters[key] = value;
        else
            _action.Parameters.Remove(key);
        
        OnPropertyChanged(key);
    }
}
```

---

### Step 2: Refactor CommandViewModel

**File:** `SharedUI\ViewModel\Action\CommandViewModel.cs`

**Replace entire file with:**

```csharp
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// ViewModel for Z21 Command actions (loco control).
/// Wraps WorkflowAction with typed properties for Address, Speed, Direction.
/// </summary>
public class CommandViewModel : WorkflowActionViewModel
{
    public CommandViewModel(WorkflowAction action) 
        : base(action, ActionType.Command)
    {
    }

    /// <summary>
    /// Locomotive address (DCC address).
    /// </summary>
    public int Address
    {
        get => GetParameter<int>("Address");
        set => SetParameter("Address", value);
    }

    /// <summary>
    /// Speed (0-127 for DCC).
    /// </summary>
    public int Speed
    {
        get => GetParameter<int>("Speed");
        set => SetParameter("Speed", value);
    }

    /// <summary>
    /// Direction: "Forward" or "Backward".
    /// </summary>
    public string Direction
    {
        get => GetParameter<string>("Direction") ?? "Forward";
        set => SetParameter("Direction", value);
    }

    /// <summary>
    /// Raw command bytes (optional, for advanced users).
    /// </summary>
    public byte[]? Bytes
    {
        get => GetParameter<byte[]>("Bytes");
        set => SetParameter("Bytes", value);
    }
}
```

---

### Step 3: Refactor AudioViewModel

**File:** `SharedUI\ViewModel\Action\AudioViewModel.cs`

**Replace entire file with:**

```csharp
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// ViewModel for Audio playback actions.
/// Wraps WorkflowAction with typed properties for FilePath, Volume.
/// </summary>
public class AudioViewModel : WorkflowActionViewModel
{
    public AudioViewModel(WorkflowAction action) 
        : base(action, ActionType.Audio)
    {
    }

    /// <summary>
    /// Path to audio file (relative or absolute).
    /// </summary>
    public string FilePath
    {
        get => GetParameter<string>("FilePath") ?? string.Empty;
        set => SetParameter("FilePath", value);
    }

    /// <summary>
    /// Volume (0.0 - 1.0).
    /// </summary>
    public double Volume
    {
        get => GetParameter<double>("Volume");
        set => SetParameter("Volume", value);
    }

    /// <summary>
    /// Loop playback.
    /// </summary>
    public bool Loop
    {
        get => GetParameter<bool>("Loop");
        set => SetParameter("Loop", value);
    }
}
```

---

### Step 4: Refactor AnnouncementViewModel

**File:** `SharedUI\ViewModel\Action\AnnouncementViewModel.cs`

**Replace entire file with:**

```csharp
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// ViewModel for TTS Announcement actions.
/// Wraps WorkflowAction with typed properties for Message, VoiceName, etc.
/// </summary>
public class AnnouncementViewModel : WorkflowActionViewModel
{
    public AnnouncementViewModel(WorkflowAction action) 
        : base(action, ActionType.Announcement)
    {
    }

    /// <summary>
    /// Text to be spoken (supports templates: {JourneyName}, {StationName}).
    /// </summary>
    public string Message
    {
        get => GetParameter<string>("Message") ?? string.Empty;
        set => SetParameter("Message", value);
    }

    /// <summary>
    /// Azure TTS voice name (e.g., "de-DE-KatjaNeural").
    /// </summary>
    public string VoiceName
    {
        get => GetParameter<string>("VoiceName") ?? "de-DE-KatjaNeural";
        set => SetParameter("VoiceName", value);
    }

    /// <summary>
    /// Speech rate (-50% to +200%).
    /// </summary>
    public int Rate
    {
        get => GetParameter<int>("Rate");
        set => SetParameter("Rate", value);
    }

    /// <summary>
    /// Volume (0.0 - 1.0).
    /// </summary>
    public double Volume
    {
        get => GetParameter<double>("Volume");
        set => SetParameter("Volume", value);
    }
}
```

---

### Step 5: Update WorkflowEditorViewModel

**File:** `SharedUI\ViewModel\WorkflowEditorViewModel.cs`

**Changes needed:**

1. **Remove old using alias:**
```csharp
// ❌ DELETE THIS
using BackendAction = Moba.Backend.Model.Action.Base;
```

2. **Update SelectedAction property type:**
```csharp
// OLD
private BackendAction? _selectedAction;
public BackendAction? SelectedAction { ... }

// NEW
private WorkflowActionViewModel? _selectedAction;
public WorkflowActionViewModel? SelectedAction { ... }
```

3. **Update Actions collection:**
```csharp
// OLD
public ObservableCollection<BackendAction> Actions { get; }

// NEW
public ObservableCollection<WorkflowActionViewModel> Actions { get; }
```

4. **Add factory method for creating ViewModels:**
```csharp
private WorkflowActionViewModel CreateActionViewModel(WorkflowAction action)
{
    return action.Type switch
    {
        ActionType.Command => new CommandViewModel(action),
        ActionType.Audio => new AudioViewModel(action),
        ActionType.Announcement => new AnnouncementViewModel(action),
        _ => throw new ArgumentException($"Unknown action type: {action.Type}")
    };
}
```

5. **Update AddActionCommand:**
```csharp
[RelayCommand]
private void AddAction()
{
    if (SelectedWorkflow == null) return;

    // Create new WorkflowAction
    var newAction = new WorkflowAction
    {
        Number = SelectedWorkflow.Actions.Count + 1,
        Type = ActionType.Command, // Default
        Name = "New Action"
    };

    // Wrap in ViewModel
    var viewModel = CreateActionViewModel(newAction);
    
    Actions.Add(viewModel);
    SelectedWorkflow.Actions.Add(newAction);
    SelectedAction = viewModel;
    
    HasUnsavedChanges = true;
}
```

6. **Update DeleteActionCommand:**
```csharp
[RelayCommand(CanExecute = nameof(CanDeleteAction))]
private void DeleteAction()
{
    if (SelectedAction == null || SelectedWorkflow == null) return;

    // Remove ViewModel
    Actions.Remove(SelectedAction);
    
    // Remove underlying WorkflowAction
    var action = SelectedAction.ToWorkflowAction();
    SelectedWorkflow.Actions.Remove(action);
    
    SelectedAction = null;
    HasUnsavedChanges = true;
}
```

7. **Update LoadActions helper:**
```csharp
private void LoadActions()
{
    Actions.Clear();
    
    if (SelectedWorkflow?.Actions != null)
    {
        foreach (var action in SelectedWorkflow.Actions)
        {
            var viewModel = CreateActionViewModel(action);
            Actions.Add(viewModel);
        }
    }
}
```

---

### Step 6: Update WorkflowViewModel

**File:** `SharedUI\ViewModel\WorkflowViewModel.cs`

**Changes needed:**

1. **Add using:**
```csharp
using Moba.Backend.Services;
```

2. **Fix IZ21 reference (currently broken):**
```csharp
// OLD (broken)
private readonly Z21 _z21;

// NEW
private readonly IZ21 _z21;
```

3. **Update constructor signature:**
```csharp
public WorkflowViewModel(Workflow workflow, IZ21 z21, WorkflowService workflowService)
{
    // ...
}
```

4. **Fix Base reference:**
```csharp
// OLD (line ~144)
private async Task ExecuteActionAsync(Base action)

// NEW
private async Task ExecuteActionAsync(WorkflowAction action)
{
    await _workflowService.ExecuteAsync(
        new Workflow { Actions = new List<WorkflowAction> { action } }, 
        _executionContext
    );
}
```

---

## ✅ Verification Checklist

After implementing all changes:

1. **Build SharedUI:**
```bash
dotnet build SharedUI/SharedUI.csproj
```
Expected: 0 errors

2. **Test JSON serialization:**
- Load existing `.json` file with workflows
- Verify Actions are deserialized
- Verify ViewModels wrap WorkflowActions correctly

3. **UI Binding Test:**
- Open WorkflowEditor in WinUI
- Add new Command action
- Verify properties (Address, Speed) are editable
- Save and reload

4. **Runtime Test:**
- Execute workflow
- Verify ActionExecutor receives correct Parameters

---

## 🚨 Breaking Changes

**For UI (XAML):**
- Bindings to `SelectedAction.Property` should still work
- DataTemplates may need `WorkflowActionViewModel` type instead of `BackendAction`

**For Tests:**
- Tests creating actions directly need to use `WorkflowAction` + wrap in ViewModel

---

## 📦 Files to Create/Modify

**New Files (1):**
- ✨ `SharedUI\ViewModel\Action\WorkflowActionViewModel.cs`

**Modified Files (5):**
- 🔄 `SharedUI\ViewModel\Action\CommandViewModel.cs`
- 🔄 `SharedUI\ViewModel\Action\AudioViewModel.cs`
- 🔄 `SharedUI\ViewModel\Action\AnnouncementViewModel.cs`
- 🔄 `SharedUI\ViewModel\WorkflowEditorViewModel.cs`
- 🔄 `SharedUI\ViewModel\WorkflowViewModel.cs`

---

## ⏱️ Estimated Time

- Step 1 (Base class): 30 min
- Step 2-4 (3 adapters): 60 min
- Step 5 (WorkflowEditorViewModel): 60 min
- Step 6 (WorkflowViewModel): 30 min
- Testing & Debugging: 60 min

**Total: ~4 hours**

---

**Ready to start? Begin with Step 1!** 🚀
