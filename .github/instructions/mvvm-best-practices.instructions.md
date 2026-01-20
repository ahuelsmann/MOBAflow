---
description: 'MVVM patterns with CommunityToolkit.Mvvm'
applyTo: 'SharedUI/**/*.cs,WinUI/ViewModel/**/*.cs'
---

# MVVM (CommunityToolkit.Mvvm)

## Attributes

```csharp
public partial class TrainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    public string DisplayName => $"{Name} ({Address})";

    // Partial method after property change
    partial void OnNameChanged(string value) => _logger.LogInformation("Renamed: {Name}", value);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() => await _ioService.SaveAsync(...);

    private bool CanSave() => HasUnsavedChanges && !IsBusy;

    // Async with cancellation
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ExecuteAsync(CancellationToken ct) { }
}
```

## Domain Model Wrapper

```csharp
public partial class StationViewModel : ObservableObject
{
    private readonly Station _model;
    public Station Model => _model;

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }
}
```

## DI Registration

```csharp
// Singleton: shared state
services.AddSingleton<MainWindowViewModel>();

// Transient: per-instance
services.AddTransient<JourneyViewModel>();
services.AddTransient<JourneysPage>();
```

## Anti-Patterns

- `async void` in commands → Use `async Task`
- Service locator → Constructor injection
- UI code in ViewModel → Use IDialogService interface
- Direct domain binding → Wrap with ViewModel
