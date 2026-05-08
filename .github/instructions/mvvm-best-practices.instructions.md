---

description: 'MVVM patterns with CommunityToolkit.Mvvm'
applyTo: 'SharedUI/**/*.cs,MOBAflow/ViewModel/**/*.cs'
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
    partial void OnNameChanged(string value) =>
        _logger.LogInformation("Renamed: {Name}", value);

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

## Page-Specific ViewModels (Code-Behind vs. ViewModel)

### What Belongs in Code-Behind (XAML.cs)

```csharp
// ✅ CORRECT: Pure view coordination
internal sealed partial class SolutionPage
{
    public MainWindowViewModel ViewModel { get; }

    public SolutionPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}

// ✅ CORRECT: Drag-and-drop visual handling (WinUI-specific)
private void OnDragOver(object sender, DragEventArgs e)
{
    // Visual feedback only - actual logic in ViewModel
    e.AcceptedOperation = DataPackageOperation.Move;
}
```

### What Belongs in ViewModel

```csharp
// ✅ CORRECT: Commands with dialog confirmation
[RelayCommand(CanExecute = nameof(CanDeleteProject))]
private async Task DeleteProjectAsync()
{
    if (_dialogService == null) return;

    var confirmed = await _dialogService.ShowConfirmationAsync(
        title: "Delete Project",
        message: "Do you really want to delete the project?");

    if (!confirmed) return;

    // ... actual deletion logic
}

// ✅ CORRECT: Layout state persistence via observable properties
[ObservableProperty]
private bool _isLeftPanelExpanded = true;

[ObservableProperty]
private double _leftPanelWidth = 250;
```

### Platform-Specific Boundaries

| Concern | Code-Behind | ViewModel |
|---------|-------------|-----------|
| Dialog display | ❌ (UI-specific) | ✅ (via IDialogService) |
| File I/O | ❌ | ✅ (via IIoService) |
| Drag-drop visuals | ✅ | ❌ (WinUI events) |
| Data model changes | ❌ | ✅ |
| Navigation | ❌ (view-specific) | ✅ (via INavigationService) |

## Anti-Patterns

- `async void` in commands → Use `async Task`
- Service locator → Constructor injection
- UI code in ViewModel → Use IDialogService interface
- Direct domain binding → Wrap with ViewModel
- **Commands in code-behind** → Move to ViewModel with IDialogService
