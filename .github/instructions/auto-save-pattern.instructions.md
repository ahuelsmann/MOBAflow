---
description: 'Auto-Save Pattern für ViewModel-Wrappers'
applyTo: 'SharedUI/ViewModel/**/*.cs'
---

# Auto-Save Pattern

> ViewModels speichern automatisch bei PropertyChanged

---

## Pattern: SetProperty für ViewModel-Wrappers

```csharp
public class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    private readonly Journey _model;

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        // ✅ Triggert PropertyChanged → Auto-Save
    }
}
```

## Nested ViewModels: PropertyChanged propagieren

```csharp
// Child ViewModel
public ObservableCollection<StationViewModel> Stations { get; }

private void OnStationPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(nameof(Stations));  // ✅ Propagiert nach oben
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

## Regel

- ✅ **SetProperty()** für alle ViewModel-Wrapper Properties
- ✅ **Nested ViewModels** propagieren PropertyChanged nach oben
- ✅ **MainWindowViewModel** subscribed zu PropertyChanged (NICHT zu custom Events)

---

**Letzte Aktualisierung:** 2026-01-17
