# Master-Detail Binding Troubleshooting Guide

## 🔍 Identifizierte Probleme

### 1. **x:Bind OneWay Update Problem**
**Symptom**: PropertyGrid zeigt keine Properties an, wenn ein Item ausgewählt wird

**Root Cause**: `x:Bind Mode=OneWay` tracked keine Property-Changed-Events für berechnete Properties

**Files Affected**:
- `EditorPage.xaml`: Alle `Visibility="{x:Bind ViewModel.ShowXxxProperties, Mode=OneWay}"`
- `MainWindowViewModel.cs`: Berechnete Properties `ShowSolutionProperties`, `ShowProjectProperties`, etc.

**Solution Applied**:
- ✅ `NotifySelectionPropertiesChanged()` ruft `OnPropertyChanged(nameof(ShowXxxProperties))` auf
- ✅ Wird in allen `OnSelectedXxxChanged` Partial-Handlers aufgerufen

### 2. **CurrentProjectViewModel Not Updating**
**Symptom**: Master-Liste zeigt keine Items (Journeys, Workflows, Trains)

**Root Cause**: `CurrentProjectViewModel` ist eine berechnete Property, aber Change-Notification fehlt

**Solution Applied**:
```csharp
// In MainWindowViewModel.Selection.cs
partial void OnSelectedProjectChanged(ProjectViewModel? value)
{
    if (value != null)
    {
        ClearOtherSelections(MobaType.Project);
        CurrentSelectedEntityType = MobaType.Project;
    }
    
    OnPropertyChanged(nameof(CurrentProjectViewModel)); // ✅ ADDED
    NotifySelectionPropertiesChanged();
}
```

**Also Needed in**:
- `MainWindowViewModel.Solution.cs` → `OnSolutionChanged()` ✅ Already present

### 3. **SolutionViewModel Initialization**
**Symptom**: `ViewModel.SolutionViewModel.Projects` binding fails on startup

**Root Cause**: `SolutionViewModel` was initialized in `InitializeAsync()` which was never called

**Solution Applied**:
```csharp
// In MainWindowViewModel.Solution.cs
partial void OnSolutionChanged(Solution? value)
{
    if (value == null)
    {
        HasSolution = false;
        SolutionViewModel = null;
        AvailableCities.Clear();
        OnPropertyChanged(nameof(CurrentProjectViewModel));
        return;
    }

    // Ensure Solution always has at least one project
    if (value.Projects.Count == 0)
    {
        value.Projects.Add(new Project { Name = "(Untitled Project)" });
    }

    SolutionViewModel = new SolutionViewModel(value); // ✅ Auto-init
    HasSolution = value.Projects.Count > 0;

    SaveSolutionCommand.NotifyCanExecuteChanged();
    ConnectToZ21Command.NotifyCanExecuteChanged();
    OnPropertyChanged(nameof(CurrentProjectViewModel));
    
    LoadCities();
}
```

### 4. **Detail Binding Null-Safety**
**Symptom**: Binding errors when no item selected (e.g., `ViewModel.SelectedJourney.Name` when `SelectedJourney` is null)

**Current XAML Pattern**:
```xml
<TextBox Text="{x:Bind ViewModel.SelectedJourney.Name, Mode=TwoWay}" />
```

**Problem**: If `SelectedJourney` is null, binding fails

**Solution Options**:

#### Option A: Use Visibility Binding (Current Approach)
```xml
<StackPanel Visibility="{x:Bind ViewModel.ShowJourneyProperties, Mode=OneWay}">
    <TextBox Text="{x:Bind ViewModel.SelectedJourney.Name, Mode=TwoWay}" />
</StackPanel>
```
✅ **Status**: Already implemented in PropertyGrid

#### Option B: Use FallbackValue
```xml
<TextBox Text="{x:Bind ViewModel.SelectedJourney.Name, Mode=TwoWay, FallbackValue=''}" />
```

#### Option C: Use x:Bind with null-conditional (WinUI 3 limitation)
❌ Not supported in x:Bind

---

## 🔧 Quick Fix Checklist

### If Master-Detail Still Not Working:

1. **Stop Running Application**
   ```powershell
   taskkill /IM MOBAflow.exe /F
   ```

2. **Clean Solution**
   ```powershell
   dotnet clean "C:\Repo\ahuelsmann\MOBAflow\MOBAflow.sln"
   ```

3. **Delete obj/bin Folders**
   ```powershell
   Remove-Item "C:\Repo\ahuelsmann\MOBAflow\WinUI\obj" -Recurse -Force
   Remove-Item "C:\Repo\ahuelsmann\MOBAflow\WinUI\bin" -Recurse -Force
   Remove-Item "C:\Repo\ahuelsmann\MOBAflow\SharedUI\obj" -Recurse -Force
   Remove-Item "C:\Repo\ahuelsmann\MOBAflow\SharedUI\bin" -Recurse -Force
   ```

4. **Rebuild Solution**
   ```powershell
   dotnet build "C:\Repo\ahuelsmann\MOBAflow\MOBAflow.sln"
   ```

5. **Verify x:Bind Code Generation**
   Check that `EditorPage.xaml.g.cs` exists:
   ```
   WinUI\obj\Debug\net10.0-windows10.0.17763.0\win-x64\View\EditorPage.xaml.g.cs
   ```

---

## 🎯 Expected Behavior After Fixes

### Master-Detail Flow:

1. **Select Project** → `SelectedProject` changes
   - ✅ `OnSelectedProjectChanged` fires
   - ✅ `OnPropertyChanged(nameof(CurrentProjectViewModel))` fires
   - ✅ `CurrentProjectViewModel.Journeys` binding updates
   - ✅ Journeys list populates

2. **Select Journey** → `SelectedJourney` changes
   - ✅ `OnSelectedJourneyChanged` fires
   - ✅ `NotifySelectionPropertiesChanged()` fires
   - ✅ `ShowJourneyProperties` → `true`
   - ✅ PropertyGrid shows Journey properties
   - ✅ `SelectedJourney.Stations` binding updates
   - ✅ Stations list populates

3. **Select Station** → `SelectedStation` changes
   - ✅ `OnSelectedStationChanged` fires
   - ✅ `NotifySelectionPropertiesChanged()` fires
   - ✅ `ShowStationProperties` → `true`
   - ✅ PropertyGrid switches to Station properties

### PropertyGrid Visibility:

- ✅ Only **one** section visible at a time
- ✅ Section switches based on `CurrentSelectedEntityType`
- ✅ Properties are editable via `TwoWay` bindings
- ✅ Changes trigger `HasUnsavedChanges = true`

---

## 🐛 Debugging Tips

### Enable Debug Output:
```csharp
// In MainWindowViewModel.Selection.cs
partial void OnSelectedProjectChanged(ProjectViewModel? value)
{
    System.Diagnostics.Debug.WriteLine($"🔹 SelectedProject changed: {value?.Model.Name ?? "null"}");
    System.Diagnostics.Debug.WriteLine($"🔹 CurrentProjectViewModel: {CurrentProjectViewModel?.Model.Name ?? "null"}");
    System.Diagnostics.Debug.WriteLine($"🔹 Journeys count: {CurrentProjectViewModel?.Journeys.Count ?? 0}");
    
    // ...existing code...
}
```

### Check Binding Errors:
1. Open **Output Window** in Visual Studio
2. Select **Debug** from dropdown
3. Look for binding errors:
   ```
   Error: BindingExpression path error: 'CurrentProjectViewModel' property not found
   ```

### Verify XAML Compilation:
```powershell
# Check generated code
Get-Content "WinUI\obj\Debug\net10.0-windows10.0.17763.0\win-x64\View\EditorPage.xaml.g.cs" | 
    Select-String -Pattern "ShowJourneyProperties"
```

---

## 📝 Related Files Modified

| File | Changes | Status |
|------|---------|--------|
| `MainWindowViewModel.cs` | Removed `InitializeAsync`, Added event cleanup in `OnWindowClosing` | ✅ Complete |
| `MainWindowViewModel.Selection.cs` | Added `OnPropertyChanged(nameof(CurrentProjectViewModel))` | ✅ Complete |
| `MainWindowViewModel.Solution.cs` | Added `OnSolutionChanged` with auto-init | ✅ Complete |
| `MainWindowViewModel.Z21.cs` | Moved event subscriptions to Connect/Disconnect | ✅ Complete |
| `EditorPage.xaml` | No changes needed (uses existing bindings) | ✅ OK |

---

**Last Updated**: 2025-01-XX  
**Status**: Fixes applied, awaiting runtime verification
