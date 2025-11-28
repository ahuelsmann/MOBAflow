---
description: Diagnostic and fix template for threading issues across WinUI, MAUI, and Blazor
---

# Fix Threading Issue

I'm experiencing a threading issue. Please help me diagnose and fix it.

## 🔍 Symptom Description

**Error Message** (if any):  
[PASTE_ERROR_MESSAGE]

**Behavior**:
- [ ] UI not updating
- [ ] App crashes on property change
- [ ] Cross-thread exception
- [ ] Race condition
- [ ] Deadlock

**When does it occur**:
- [ ] When Z21 event fires
- [ ] When loading data
- [ ] When saving data
- [ ] When navigating
- [ ] Other: [DESCRIBE]

---

## 🎯 Platform-Specific Solutions

### WinUI: Use DispatcherQueue

```csharp
// ❌ WRONG: Direct UI update from background thread
private void OnZ21Event(object sender, EventArgs e)
{
    SomeProperty = newValue; // Crashes if called from background thread
}

// ✅ CORRECT: Dispatch to UI thread
private void OnZ21Event(object sender, EventArgs e)
{
    _dispatcher.TryEnqueue(() =>
    {
        SomeProperty = newValue;
    });
}

// ✅ CORRECT: Async version
private async void OnZ21Event(object sender, EventArgs e)
{
    await _dispatcher.EnqueueAsync(() =>
    {
        SomeProperty = newValue;
    });
}
```

### MAUI: Use MainThread

```csharp
// ❌ WRONG: Direct UI update
private void OnModelChanged(object sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(e.PropertyName); // Crashes from background thread
}

// ✅ CORRECT: MainThread dispatch
private async void OnModelChanged(object sender, PropertyChangedEventArgs e)
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        OnPropertyChanged(e.PropertyName);
    });
}

// ✅ CORRECT: Synchronous version (use with caution)
private void OnModelChanged(object sender, PropertyChangedEventArgs e)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        OnPropertyChanged(e.PropertyName);
    });
}
```

### Blazor: Use InvokeAsync + StateHasChanged

```csharp
// ❌ WRONG: Direct UI update
private void OnZ21Event(object sender, EventArgs e)
{
    SomeProperty = newValue; // UI won't update
}

// ✅ CORRECT: InvokeAsync + StateHasChanged
private void OnZ21Event(object sender, EventArgs e)
{
    InvokeAsync(() =>
    {
        SomeProperty = newValue;
        StateHasChanged();
    });
}
```

---

## 🚨 Backend Must NOT Dispatch

```csharp
// ❌ WRONG: Backend dispatches to UI thread
namespace Moba.Backend.Manager;

public class JourneyManager
{
#if WINDOWS
    private readonly DispatcherQueue _dispatcher; // ❌ Platform-specific!
#endif
}

// ✅ CORRECT: Backend raises event, UI handles dispatching
namespace Moba.Backend.Manager;

public class JourneyManager
{
    public event EventHandler<JourneyChangedEventArgs> JourneyChanged;
    
    private void OnDataChanged()
    {
        JourneyChanged?.Invoke(this, new JourneyChangedEventArgs(...));
    }
}

// Platform-specific ViewModel handles threading
namespace Moba.WinUI.ViewModel;

public class JourneyViewModel
{
    private readonly DispatcherQueue _dispatcher;
    
    public JourneyViewModel(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        _manager.JourneyChanged += OnJourneyChanged;
    }
    
    private void OnJourneyChanged(object sender, JourneyChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() => UpdateUI());
    }
}
```

---

## 🔧 Common Patterns

### Pattern 1: Property Update from Backend Event

```csharp
// Backend raises event
public class Solution
{
    public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
    
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }
}

// ViewModel dispatches to UI thread
public class SolutionViewModel : ObservableObject
{
    private readonly Solution _model;
    private readonly DispatcherQueue _dispatcher; // WinUI
    
    public SolutionViewModel(Solution model, DispatcherQueue dispatcher)
    {
        _model = model;
        _dispatcher = dispatcher;
        _model.PropertyChanged += OnModelPropertyChanged;
    }
    
    public string Name => _model.Name;
    
    private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() => OnPropertyChanged(e.PropertyName));
    }
}
```

### Pattern 2: Async Command Execution

```csharp
// ✅ CORRECT: Async command with UI updates
[RelayCommand]
private async Task SaveAsync()
{
    IsSaving = true; // UI thread - OK
    
    try
    {
        await _backend.SaveAsync(); // Background thread - OK
        
        StatusMessage = "Saved successfully"; // UI thread - OK
    }
    catch (Exception ex)
    {
        ErrorMessage = ex.Message; // UI thread - OK
    }
    finally
    {
        IsSaving = false; // UI thread - OK
    }
}
```

### Pattern 3: Collection Updates

```csharp
// ✅ CORRECT: ObservableCollection on UI thread
private void OnItemsChanged(IEnumerable<Item> items)
{
    _dispatcher.TryEnqueue(() =>
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    });
}
```

---

## 🧪 Debugging Checklist

- [ ] Check if error occurs on UI thread or background thread
- [ ] Verify DispatcherQueue/MainThread is not null
- [ ] Ensure event handlers dispatch to UI thread
- [ ] Check for `.Result` or `.Wait()` causing deadlocks
- [ ] Verify Backend has no platform-specific code
- [ ] Test with debugger attached (threading issues may behave differently)
- [ ] Add logging to trace thread IDs

## 📝 Diagnostic Logging

```csharp
// Add to identify thread issues
_logger.LogDebug($"Thread: {Thread.CurrentThread.ManagedThreadId}, " +
                 $"IsBackground: {Thread.CurrentThread.IsBackground}, " +
                 $"Method: {nameof(OnDataReceived)}");
```

---

## 🎯 Issue Details

**File with Issue**: [FILE_PATH]  
**Method/Property**: [METHOD_NAME]  
**Platform**: 
- [ ] WinUI
- [ ] MAUI
- [ ] Blazor
- [ ] Backend (should not dispatch!)

**Stack Trace** (if available):
```
[PASTE_STACK_TRACE]
```

#file:.github/instructions/backend.instructions.md
#file:.github/instructions/winui.instructions.md
#file:.github/instructions/maui.instructions.md
#file:docs/THREADING.md
