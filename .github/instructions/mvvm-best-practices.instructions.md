---
description: 'MVVM patterns with CommunityToolkit.Mvvm for MOBAflow - attributes, commands, property notifications, and ViewModel lifecycle.'
applyTo: '**/*.cs'
---

# MVVM Best Practices (CommunityToolkit.Mvvm)

> MOBAflow uses **CommunityToolkit.Mvvm** for MVVM implementation with source generators.

---

## CommunityToolkit.Mvvm Attributes

### [ObservableProperty] - Auto-Generated Properties

```csharp
public partial class TrainViewModel : ObservableObject
{
    // Source generator creates: public string Name { get; set; } with INotifyPropertyChanged
    [ObservableProperty]
    private string _name = string.Empty;

    // With validation
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Address is required")]
    [Range(1, 9999, ErrorMessage = "Address must be 1-9999")]
    private int _address;

    // Notify dependent properties when this changes
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullDisplayName))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _description = string.Empty;

    // Computed property (not observable, but notified via NotifyPropertyChangedFor)
    public string FullDisplayName => $"{Name} ({Address})";
    public bool IsValid => !string.IsNullOrEmpty(Name) && Address > 0;
}
```

### Partial Methods for Property Changes

```csharp
public partial class JourneyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    // Called BEFORE property changes (can cancel)
    partial void OnNameChanging(string value)
    {
        _logger.LogDebug("Name changing from {Old} to {New}", _name, value);
    }

    // Called AFTER property changes
    partial void OnNameChanged(string value)
    {
        _logger.LogInformation("Journey renamed to: {Name}", value);
        // Trigger auto-save, validation, etc.
    }
}
```

### [RelayCommand] - Auto-Generated Commands

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    // Simple command
    [RelayCommand]
    private void Save()
    {
        // Generates: public IRelayCommand SaveCommand { get; }
    }

    // Async command with automatic busy state
    [RelayCommand]
    private async Task ConnectAsync()
    {
        // Generates: public IAsyncRelayCommand ConnectCommand { get; }
        // Automatically disables button while executing
        await _z21.ConnectAsync(IpAddress);
    }

    // Command with parameter
    [RelayCommand]
    private void SelectTrain(TrainViewModel train)
    {
        SelectedTrain = train;
    }

    // Command with CanExecute
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await _ioService.SaveAsync(Solution, CurrentPath);
    }

    private bool CanSave() => HasUnsavedChanges && !IsBusy;

    // Notify CanExecute when properties change
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasUnsavedChanges;
}
```

### [RelayCommand] with CancellationToken

```csharp
public partial class WorkflowViewModel : ObservableObject
{
    // Async command with cancellation support
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ExecuteWorkflowAsync(CancellationToken cancellationToken)
    {
        // Generates: ExecuteWorkflowCommand AND ExecuteWorkflowCancelCommand
        foreach (var action in Actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteActionAsync(action, cancellationToken);
        }
    }
}
```

---

## ViewModel Patterns

### Wrapper ViewModel Pattern (Domain Model Binding)

```csharp
// ✅ CORRECT: Wrap domain model, expose bindable properties
public partial class StationViewModel : ObservableObject
{
    private readonly Station _model;

    public StationViewModel(Station model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    // Wrap model properties for two-way binding
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    public int FeedbackPointId
    {
        get => _model.FeedbackPointId;
        set => SetProperty(_model.FeedbackPointId, value, _model, (m, v) => m.FeedbackPointId = v);
    }

    public bool IsExitOnLeft
    {
        get => _model.IsExitOnLeft;
        set => SetProperty(_model.IsExitOnLeft, value, _model, (m, v) => m.IsExitOnLeft = v);
    }

    // Expose underlying model for serialization
    public Station Model => _model;
}
```

### Nested ViewModel Collections

```csharp
public partial class JourneyViewModel : ObservableObject
{
    private readonly Journey _model;

    public ObservableCollection<StationViewModel> Stations { get; }

    public JourneyViewModel(Journey model)
    {
        _model = model;
        
        // Wrap nested collection
        Stations = new ObservableCollection<StationViewModel>(
            model.Stations.Select(s => new StationViewModel(s)));

        // Subscribe to collection changes for auto-save
        Stations.CollectionChanged += OnStationsChanged;
    }

    private void OnStationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Sync back to domain model
        _model.Stations.Clear();
        foreach (var vm in Stations)
        {
            _model.Stations.Add(vm.Model);
        }
        
        // Trigger parent notification for auto-save
        OnPropertyChanged(nameof(Stations));
    }
}
```

### PropertyChanged Auto-Save Pattern

```csharp
// MainWindowViewModel subscribes to child ViewModels
private void SubscribeToProjectViewModels(ProjectViewModel project)
{
    foreach (var journey in project.Journeys)
    {
        journey.PropertyChanged += OnViewModelPropertyChanged;
        foreach (var station in journey.Stations)
        {
            station.PropertyChanged += OnViewModelPropertyChanged;
        }
    }
}

private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    // Ignore UI-only properties
    var ignoredProperties = new[] { "IsSelected", "IsExpanded", "IsHighlighted" };
    if (!ignoredProperties.Contains(e.PropertyName))
    {
        HasUnsavedChanges = true;
        _ = SaveSolutionInternalAsync();
    }
}
```

---

## ViewModel Lifetime (DI Registration)

### Singleton vs Transient

```csharp
// App.xaml.cs or MauiProgram.cs

// ✅ SINGLETON: Shared state across entire app
services.AddSingleton<MainWindowViewModel>();  // App state, solution, connection
services.AddSingleton<IZ21, Z21>();            // Single connection to hardware

// ✅ TRANSIENT: New instance per request
services.AddTransient<JourneyViewModel>();     // Created per journey
services.AddTransient<TrainViewModel>();       // Created per train
services.AddTransient<StationViewModel>();     // Created per station

// ✅ TRANSIENT: Pages (created per navigation)
services.AddTransient<JourneysPage>();
services.AddTransient<SettingsPage>();
```

### ViewModel Factory Pattern

```csharp
// When ViewModels need parameters not from DI
public class ViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public StationViewModel CreateStationViewModel(Station model)
    {
        return new StationViewModel(model);
    }

    public JourneyViewModel CreateJourneyViewModel(Journey model)
    {
        var vm = new JourneyViewModel(model);
        // Inject additional services if needed
        return vm;
    }
}
```

---

## Anti-Patterns to Avoid

### ❌ Direct Model Binding

```csharp
// ❌ WRONG: Binding directly to domain model
public Station Model { get; set; }  // No INotifyPropertyChanged!

// ✅ CORRECT: Wrap with ViewModel
public StationViewModel StationVM { get; set; }
```

### ❌ Service Locator in ViewModel

```csharp
// ❌ WRONG: Service locator pattern
public class BadViewModel
{
    private readonly IZ21 _z21 = App.ServiceProvider.GetRequiredService<IZ21>();
}

// ✅ CORRECT: Constructor injection
public class GoodViewModel
{
    private readonly IZ21 _z21;
    
    public GoodViewModel(IZ21 z21)
    {
        _z21 = z21;
    }
}
```

### ❌ Async Void in Commands

```csharp
// ❌ WRONG: async void loses exceptions
[RelayCommand]
private async void Save()  // BAD!
{
    await _ioService.SaveAsync(...);
}

// ✅ CORRECT: async Task
[RelayCommand]
private async Task SaveAsync()
{
    await _ioService.SaveAsync(...);
}
```

### ❌ UI Code in ViewModel

```csharp
// ❌ WRONG: UI framework code in ViewModel
public class BadViewModel
{
    public void ShowDialog()
    {
        var dialog = new ContentDialog();  // WinUI-specific!
        dialog.ShowAsync();
    }
}

// ✅ CORRECT: Abstract via interface
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message);
}

public class GoodViewModel
{
    private readonly IDialogService _dialogService;
    
    public async Task DeleteAsync()
    {
        if (await _dialogService.ShowConfirmationAsync("Delete?"))
        {
            // Delete logic
        }
    }
}
```

---

## Quick Reference

| Attribute | Purpose | Generates |
|-----------|---------|-----------|
| `[ObservableProperty]` | Auto-property with INotifyPropertyChanged | Property + OnXxxChanged/Changing |
| `[RelayCommand]` | Auto-command from method | IRelayCommand property |
| `[NotifyPropertyChangedFor]` | Notify dependent properties | Additional PropertyChanged calls |
| `[NotifyCanExecuteChangedFor]` | Refresh command CanExecute | Command.NotifyCanExecuteChanged() |
| `[NotifyDataErrorInfo]` | Enable validation | INotifyDataErrorInfo implementation |

---

## MOBAflow-Specific Patterns

### MainWindowViewModel Partial Classes

```csharp
// Split large ViewModel into partial classes by responsibility
MainWindowViewModel.cs           // Core state, constructor, DI
MainWindowViewModel.Settings.cs  // Settings properties
MainWindowViewModel.Z21.cs       // Z21 connection logic
MainWindowViewModel.Journey.cs   // Journey management
MainWindowViewModel.Workflow.cs  // Workflow execution
MainWindowViewModel.Counter.cs   // Lap counter statistics
```

### Solution ↔ ViewModel Sync

```csharp
// When loading solution
public void LoadSolution(Solution solution)
{
    _solution = solution;
    
    // Create ViewModel wrappers
    Projects.Clear();
    foreach (var project in solution.Projects)
    {
        var projectVm = new ProjectViewModel(project);
        SubscribeToProjectViewModels(projectVm);
        Projects.Add(projectVm);
    }
    
    SelectedProject = Projects.FirstOrDefault();
}

// When saving solution
public async Task SaveSolutionAsync()
{
    // ViewModels already sync to domain models via SetProperty
    await _ioService.SaveAsync(_solution, CurrentPath);
    HasUnsavedChanges = false;
}
```
