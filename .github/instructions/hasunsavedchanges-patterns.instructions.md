---
description: Patterns for HasUnsavedChanges tracking and Undo/Redo integration
applyTo: "SharedUI/ViewModel/**/*.cs"
---

# HasUnsavedChanges & Undo/Redo Integration Patterns

## 🎯 Core Principle

**HasUnsavedChanges** and **Undo/Redo** must work together to provide accurate tracking of solution state.

---

## ✅ Pattern 1: Set HasUnsavedChanges on Modifications

```csharp
// ✅ CORRECT: Set HasUnsavedChanges when data changes
private void OnPropertyValueChanged(object? sender, EventArgs e)
{
    CurrentSelectedNode?.RefreshDisplayName();
    _undoRedoManager.SaveStateThrottled(Solution);
    
    // Mark as having unsaved changes
    HasUnsavedChanges = true;  // ✅
}

// ❌ WRONG: Forget to set HasUnsavedChanges
private void OnPropertyValueChanged(object? sender, EventArgs e)
{
    _undoRedoManager.SaveStateThrottled(Solution);
    // ❌ Missing: HasUnsavedChanges = true
}
```

---

## ✅ Pattern 2: Check Saved State After Undo/Redo

```csharp
// ✅ CORRECT: Check if we're back at saved point
[RelayCommand]
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null)
    {
        Solution.UpdateFrom(previous);
        
        // Check if we're back at the saved state
        HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();  // ✅
    }
}

// ❌ WRONG: Always set to true/false
[RelayCommand]
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null)
    {
        Solution.UpdateFrom(previous);
        HasUnsavedChanges = true;  // ❌ Wrong! Could be at saved state
    }
}
```

---

## ✅ Pattern 3: Clear History on New/Load

```csharp
// ✅ CORRECT: Clear old history when loading/creating solution
[RelayCommand]
private async Task NewSolutionAsync()
{
    // ... dialog, create new solution
    
    Solution.UpdateFrom(newSolution);
    
    // Clear old history (important!)
    _undoRedoManager.ClearHistory();  // ✅
    
    // Save initial state
    await _undoRedoManager.SaveStateImmediateAsync(Solution);
    UpdateUndoRedoState();
    
    HasUnsavedChanges = true;  // New solution not yet saved
}

// ❌ WRONG: Don't clear history
[RelayCommand]
private async Task NewSolutionAsync()
{
    Solution.UpdateFrom(newSolution);
    // ❌ Old history remains - user could undo to old solution!
    HasUnsavedChanges = true;
}
```

---

## ✅ Pattern 4: Mark Saved State After Save

```csharp
// ✅ CORRECT: Mark current undo/redo state as saved
[RelayCommand]
private async Task SaveSolutionAsync()
{
    var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
    if (success && path != null)
    {
        CurrentSolutionPath = path;
        HasUnsavedChanges = false;
        
        // Mark current state as saved in undo/redo history
        _undoRedoManager.MarkCurrentAsSaved();  // ✅
    }
}

// ❌ WRONG: Forget to mark saved state
[RelayCommand]
private async Task SaveSolutionAsync()
{
    var (success, path, error) = await _ioService.SaveAsync(Solution, path);
    if (success)
    {
        HasUnsavedChanges = false;
        // ❌ Missing: _undoRedoManager.MarkCurrentAsSaved()
        // Result: After save, undo will set HasUnsavedChanges = true incorrectly
    }
}
```

---

## ✅ Pattern 5: Load Solution with Clean State

```csharp
// ✅ CORRECT: Proper load sequence
[RelayCommand]
private async Task LoadSolutionAsync()
{
    var (loaded, path, error) = await _ioService.LoadAsync();
    
    if (loaded != null)
    {
        Solution.UpdateFrom(loaded);
        CurrentSolutionPath = path;
        
        // Clear old history
        _undoRedoManager.ClearHistory();
        
        // Save initial state and mark as saved
        await _undoRedoManager.SaveStateImmediateAsync(Solution);
        _undoRedoManager.MarkCurrentAsSaved();  // ✅ Mark as saved immediately
        UpdateUndoRedoState();
        
        HasUnsavedChanges = false;  // Just loaded = saved
    }
}

// ❌ WRONG: Missing saved state marking
[RelayCommand]
private async Task LoadSolutionAsync()
{
    var (loaded, path, error) = await _ioService.LoadAsync();
    
    if (loaded != null)
    {
        Solution.UpdateFrom(loaded);
        _undoRedoManager.ClearHistory();
        await _undoRedoManager.SaveStateImmediateAsync(Solution);
        // ❌ Missing: _undoRedoManager.MarkCurrentAsSaved()
        HasUnsavedChanges = false;
    }
}
```

---

## ✅ Pattern 6: NULL Checks Before Access

```csharp
// ✅ CORRECT: Check for NULL before accessing properties
[RelayCommand]
private async Task ConnectToZ21Async()
{
    // Check Solution and Settings are not null
    if (Solution?.Settings != null && 
        !string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))
    {
        await _z21.ConnectAsync(Solution.Settings.CurrentIpAddress);
    }
    else
    {
        Z21StatusText = "No IP address configured";
    }
}

// ❌ WRONG: Direct access without NULL check
[RelayCommand]
private async Task ConnectToZ21Async()
{
    // ❌ Could throw NullReferenceException
    if (!string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))
    {
        await _z21.ConnectAsync(Solution.Settings.CurrentIpAddress);
    }
}
```

---

## ✅ Pattern 7: NULL Checks in Undo/Redo

```csharp
// ✅ CORRECT: Check both returned solution and current solution
[RelayCommand]
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null && Solution != null)  // ✅ Check both
    {
        Solution.UpdateFrom(previous);
        HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();
    }
}

// ❌ WRONG: Only check returned solution
[RelayCommand]
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null)  // ❌ What if Solution is null?
    {
        Solution.UpdateFrom(previous);  // Could throw!
    }
}
```

---

## 🔄 Complete Flow Example

```csharp
// Complete example showing all patterns
public class MyViewModel
{
    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        // 1. Clear old history
        _undoRedoManager.ClearHistory();
        
        // 2. Create and update solution
        var newSolution = CreateNewSolution();
        Solution.UpdateFrom(newSolution);
        
        // 3. Save initial state
        await _undoRedoManager.SaveStateImmediateAsync(Solution);
        UpdateUndoRedoState();
        
        // 4. Mark as unsaved (new)
        HasUnsavedChanges = true;
    }
    
    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        var (loaded, path, error) = await _ioService.LoadAsync();
        if (loaded != null)
        {
            // 1. Clear old history
            _undoRedoManager.ClearHistory();
            
            // 2. Update solution
            Solution.UpdateFrom(loaded);
            CurrentSolutionPath = path;
            
            // 3. Save initial state and mark as saved
            await _undoRedoManager.SaveStateImmediateAsync(Solution);
            _undoRedoManager.MarkCurrentAsSaved();
            UpdateUndoRedoState();
            
            // 4. Mark as saved (just loaded)
            HasUnsavedChanges = false;
        }
    }
    
    [RelayCommand]
    private async Task SaveSolutionAsync()
    {
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
            
            // Mark as saved in both systems
            HasUnsavedChanges = false;
            _undoRedoManager.MarkCurrentAsSaved();
        }
    }
    
    [RelayCommand]
    private async Task UndoAsync()
    {
        var previous = await _undoRedoManager.UndoAsync();
        if (previous != null && Solution != null)
        {
            Solution.UpdateFrom(previous);
            
            // Check if we're back at saved state
            HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();
            
            UpdateUndoRedoState();
        }
    }
    
    private void OnPropertyChanged()
    {
        _undoRedoManager.SaveStateThrottled(Solution);
        HasUnsavedChanges = true;  // Mark as modified
    }
}
```

---

## 📋 Checklist

When implementing solution state management:

- [ ] ✅ Set `HasUnsavedChanges = true` on any modification
- [ ] ✅ Clear history on New/Load (`ClearHistory()`)
- [ ] ✅ Save initial state after New/Load
- [ ] ✅ Mark saved state after Save (`MarkCurrentAsSaved()`)
- [ ] ✅ Check saved state in Undo/Redo (`IsCurrentStateSaved()`)
- [ ] ✅ NULL check `Solution?.Settings` before access
- [ ] ✅ NULL check both solutions in Undo/Redo
- [ ] ✅ Update `UpdateUndoRedoState()` after Undo/Redo

---

## 🚫 Common Anti-Patterns

### ❌ Anti-Pattern 1: Forgetting to Clear History

```csharp
// ❌ BAD
private async Task NewSolutionAsync()
{
    Solution.UpdateFrom(new Solution());
    // User can still undo to old solution!
}
```

### ❌ Anti-Pattern 2: Not Marking Saved State

```csharp
// ❌ BAD
private async Task SaveAsync()
{
    await _ioService.SaveAsync(Solution, path);
    HasUnsavedChanges = false;
    // After save, any undo will incorrectly set HasUnsavedChanges = true
}
```

### ❌ Anti-Pattern 3: Missing NULL Checks

```csharp
// ❌ BAD - Will throw if Solution or Settings is null
if (!string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))
{
    // ...
}
```

### ❌ Anti-Pattern 4: Not Setting HasUnsavedChanges on Edit

```csharp
// ❌ BAD
private void OnEdit()
{
    _undoRedoManager.SaveStateThrottled(Solution);
    // Forgot: HasUnsavedChanges = true
    // Result: "Unsaved Changes" dialog won't trigger
}
```

---

## 🎯 Summary

**Key Takeaways:**

1. **Always pair** `HasUnsavedChanges` with `UndoRedoManager` operations
2. **Clear history** on New/Load to start fresh
3. **Mark saved state** after Save to track position in history
4. **Check saved state** in Undo/Redo for accurate tracking
5. **NULL check** Solution and Settings before access
6. **Set HasUnsavedChanges** on any modification

This ensures consistent, accurate tracking of solution state across the entire application.
