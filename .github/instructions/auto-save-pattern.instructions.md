---
description: 'Auto-Save Pattern for ViewModel wrappers'
applyTo: 'SharedUI/ViewModel/**/*.cs'
---

# Auto-Save Pattern

> ViewModels save automatically on PropertyChanged

---

## Pattern: SetProperty for ViewModel wrappers

```csharp
public class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    private readonly Journey _model;

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        // ✅ Triggers PropertyChanged → Auto-Save
    }
}
```

## Nested ViewModels: propagate PropertyChanged

```csharp
// Child ViewModel
public ObservableCollection<StationViewModel> Stations { get; }

private void OnStationPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(nameof(Stations));  // ✅ Propagates upward
}
```

## MainWindowViewModel: Generic Handler

```csharp
partial void OnSelectedJourneyChanged(JourneyViewModel? value)
{
    if (value != null)
        value.PropertyChanged += OnViewModelPropertyChanged;
}

private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (!ignoredProperties.Contains(e.PropertyName))
        _ = SaveSolutionInternalAsync();  // ✅ Auto-Save!
}
```

## Rules

- ✅ **SetProperty()** for all ViewModel wrapper properties
- ✅ **Nested ViewModels** propagate PropertyChanged upward
- ✅ **MainWindowViewModel** subscribes to PropertyChanged (NOT to custom events)

---

**Last updated:** 2026-01-17
