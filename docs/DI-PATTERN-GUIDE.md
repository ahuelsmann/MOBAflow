# DI (Dependency Injection) Pattern Reference Guide

> Part of MOBAflow Architecture. See also: [.github/instructions/di-pattern-consistency.instructions.md](../.github/instructions/di-pattern-consistency.instructions.md)

## Quick Reference

**When in doubt, use Constructor Injection.**

### Three Types of Components

| Type | Registration | Creation | Example |
|------|--------------|----------|---------|
| **Service** | DI Container | `GetRequiredService<T>()` | `IZ21`, `WorkflowService`, `IUiDispatcher` |
| **Page** | DI Container (Transient) | `GetRequiredService<T>()` | `OverviewPage`, `JourneysPage` |
| **ViewModel** | DI Container (Singleton) | `GetRequiredService<T>()` | `MainWindowViewModel`, `TrainControlViewModel` |
| **ViewModel Wrapper** | NOT in DI | `new XxxViewModel(...)` | `JourneyViewModel`, `ProjectViewModel` |

---

## Registration Patterns

### Pattern 1: Standard Registration (Simple Case)

**Use When:** All dependencies are registered in the container.

```csharp
// Service
services.AddSingleton<IMyService, MyService>();

// ViewModel
services.AddSingleton<MyViewModel>();

// Page
services.AddTransient<MyPage>();
```

**Why:**
- Constructor injection automatically discovers dependencies
- Compiler checks for missing parameters
- Easy to test (all dependencies visible in constructor)

### Pattern 2: Custom Factory (Complex Case)

**Use When:** You need conditional logic, NullObject fallbacks, or optional services.

```csharp
// Service with fallback
services.AddSingleton<IConfigService>(sp =>
{
    try
    {
        var config = new JsonConfigService(sp.GetRequiredService<ILogger>());
        return config;
    }
    catch
    {
        return new NullConfigService();  // Fallback
    }
});

// ViewModel with many optional parameters
services.AddSingleton(sp => new MainWindowViewModel(
    sp.GetRequiredService<IZ21>(),
    sp.GetRequiredService<WorkflowService>(),
    sp.GetRequiredService<IUiDispatcher>(),
    sp.GetRequiredService<AppSettings>(),
    sp.GetRequiredService<Domain.Solution>(),
    sp.GetRequiredService<ActionExecutionContext>(),
    sp.GetRequiredService<ILogger<MainWindowViewModel>>(),
    sp.GetRequiredService<IIoService>(),
    sp.GetRequiredService<ICityService>(),
    sp.GetRequiredService<ISettingsService>(),
    sp.GetService<AnnouncementService>(),  // Optional
    sp.GetService<PhotoHubClient>()        // Optional (WinUI only)
));
```

**Document the reason:**
```csharp
// Reason: MainWindowViewModel has optional services that might not be available
// on all platforms (PhotoHubClient is WinUI-only). Using a factory allows
// graceful degradation rather than constructor injection requiring all params.
```

### Pattern 3: Special Pages with Custom Dependencies

**Use When:** A page needs platform-specific services or complex setup.

```csharp
// SignalBoxPage needs ISkinProvider for theme switching
services.AddSingleton<SignalBoxPage>(sp => new SignalBoxPage(
    sp.GetRequiredService<MainWindowViewModel>(),
    sp.GetRequiredService<ISkinProvider>(),         // ← Special dependency
    sp.GetRequiredService<AppSettings>(),
    sp.GetService<ISettingsService>()
));

// Reason: SignalBoxPage manages its own color themes via ISkinProvider
// and must respond to skin changes (SkinChanged event).
// Standard Constructor Injection is sufficient for this pattern.
```

---

## Registering Pages

### Standard Page Pattern

```csharp
public sealed partial class OverviewPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    public OverviewPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}

// In App.xaml.cs:
services.AddTransient<OverviewPage>();
navigationRegistry.Register("overview", "Overview", "\uE80F", typeof(OverviewPage), ...);
```

**Why Transient?**
- New instance per navigation
- Cleaner state (no stale fields from previous navigation)
- Pages are lightweight compared to ViewModels
- Each page gets its own event subscriptions

### Special Page Pattern (with custom factory)

```csharp
public sealed partial class SignalBoxPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ISkinProvider skinProvider,
        AppSettings settings,
        ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _skinProvider = skinProvider;
        _settings = settings;
        _settingsService = settingsService;
        InitializeComponent();
    }
}

// In App.xaml.cs:
services.AddSingleton<SignalBoxPage>(sp => new SignalBoxPage(
    sp.GetRequiredService<MainWindowViewModel>(),
    sp.GetRequiredService<ISkinProvider>(),
    sp.GetRequiredService<AppSettings>(),
    sp.GetService<ISettingsService>()
));
```

---

## Registering ViewModels

### Standard ViewModel (Singleton, reused across app)

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        IZ21 z21,
        WorkflowService workflowService,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Domain.Solution solution,
        ActionExecutionContext executionContext,
        ILogger<MainWindowViewModel> logger,
        IIoService? ioService = null,
        ICityService? cityLibraryService = null)
    {
        _z21 = z21;
        _workflowService = workflowService;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        // ...
    }
}

// In App.xaml.cs:
services.AddSingleton(sp => new MainWindowViewModel(
    sp.GetRequiredService<IZ21>(),
    sp.GetRequiredService<WorkflowService>(),
    sp.GetRequiredService<IUiDispatcher>(),
    sp.GetRequiredService<AppSettings>(),
    sp.GetRequiredService<Domain.Solution>(),
    sp.GetRequiredService<ActionExecutionContext>(),
    sp.GetRequiredService<ILogger<MainWindowViewModel>>(),
    sp.GetRequiredService<IIoService>(),
    sp.GetRequiredService<ICityService>()
));
```

### Specialized ViewModel (Singleton, specific to a feature)

```csharp
public partial class TrainControlViewModel : ObservableObject
{
    public TrainControlViewModel(
        IZ21 z21,
        IUiDispatcher uiDispatcher,
        ISettingsService settingsService,
        MainWindowViewModel parentViewModel,
        ILogger<TrainControlViewModel>? logger = null)
    {
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settingsService = settingsService;
        _parentViewModel = parentViewModel;
        _logger = logger;
    }
}

// In App.xaml.cs:
services.AddSingleton<TrainControlViewModel>();
// ← Simple: Let DI wire the constructor
```

### Wrapper ViewModel (NOT registered, created at runtime)

```csharp
public partial class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    private readonly Journey _journey;
    private readonly Project _project;
    
    public JourneyViewModel(Journey journey, Project project, IUiDispatcher? dispatcher = null)
    {
        _journey = journey;
        _project = project;
        _dispatcher = dispatcher;
    }
    
    public Journey Model => _journey;
}

// Usage: Created with 'new', NOT from DI
var journeyVM = new JourneyViewModel(journey, project, _dispatcher);
```

**Why Wrappers are NOT registered:**
- They wrap domain models (1:1 relationship)
- They're created dynamically based on user selection
- Multiple instances of same model possible
- Registration would pollute the DI container with collection wrappers

---

## Optional vs Required Dependencies

### Required Dependency (throw if missing)

```csharp
public MyViewModel(
    IZ21 z21,                          // ← Required
    WorkflowService workflowService)   // ← Required
{
    _z21 = ArgumentNullException.ThrowIfNull(z21);
    _workflowService = ArgumentNullException.ThrowIfNull(workflowService);
}

// Registration
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<WorkflowService>();
```

### Optional Dependency (with fallback)

```csharp
public MyViewModel(
    IZ21 z21,
    ISettingsService? settingsService = null)  // ← Optional with default
{
    _z21 = z21;
    _settingsService = settingsService ?? new NullSettingsService();  // ← Fallback
}

// Registration
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<ISettingsService>(sp =>
{
    try
    {
        return new SettingsService(sp.GetRequiredService<AppSettings>());
    }
    catch
    {
        return new NullSettingsService();  // ← Graceful fallback
    }
});
```

---

## Cross-Platform Registration (WinUI + MAUI)

### Shared Services (Register in both)

```csharp
// Backend/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddMobaBackendServices(this IServiceCollection services)
{
    // ✅ Register on BOTH platforms
    services.AddSingleton<IZ21, Z21>();
    services.AddSingleton<WorkflowService>();
    services.AddSingleton<ActionExecutionContext>();
    
    return services;
}

// WinUI/App.xaml.cs
services.AddMobaBackendServices();

// MAUI/MauiProgram.cs
builder.Services.AddMobaBackendServices();
```

### Platform-Specific Services (Register separately)

```csharp
// WinUI/App.xaml.cs
services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();  // ← WinUI only
services.AddSingleton<PhotoHubClient>();                     // ← WinUI only

// MAUI/MauiProgram.cs
builder.Services.AddSingleton<ISoundPlayer, NullSoundPlayer>(); // ← MAUI (not yet)
builder.Services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>(); // ← MAUI only
```

---

## DI Container Validation

### Automatic Startup Validation

```csharp
// In App.xaml.cs / ConfigureServices()
var provider = services.BuildServiceProvider();

// ✅ Validate that all critical services can be resolved
try
{
    provider.ValidateContainer();
}
catch (InvalidOperationException ex)
{
    Debug.WriteLine($"DI Validation failed: {ex.Message}");
    throw;  // Fail fast during startup
}

return provider;
```

### What Gets Validated

- All Pages (OverviewPage, JourneysPage, etc.)
- All ViewModels (MainWindowViewModel, TrainControlViewModel, etc.)
- All critical services (IZ21, WorkflowService, etc.)

### Benefits

- ✅ Catch missing registrations at startup (not at navigation)
- ✅ Detect circular dependencies early
- ✅ Fail fast with clear error messages

---

## Testing DI Configuration

### Unit Test Example

```csharp
[Fact]
public void MainWindowViewModel_ShouldResolve()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMobaBackendServices();
    services.AddSingleton<IUiDispatcher, NullUiDispatcher>();
    // ... register other services ...
    var provider = services.BuildServiceProvider();

    // Act
    var vm = provider.GetRequiredService<MainWindowViewModel>();

    // Assert
    Assert.NotNull(vm);
}
```

See: [`Test/DiContainerIntegrationTests.cs`](../Test/DiContainerIntegrationTests.cs)

---

## Common Mistakes (Anti-Patterns)

### ❌ Service Locator (NEVER)

```csharp
// BAD - Hard to test, hides dependencies, runtime errors
public class MyViewModel
{
    private IZ21 _z21 = ServiceLocator.GetService<IZ21>();
    
    public async Task ConnectAsync()
    {
        var service = ServiceLocator.GetService<WorkflowService>();
    }
}
```

**Better:**
```csharp
// GOOD - Constructor injection, clear dependencies
public class MyViewModel
{
    private readonly IZ21 _z21;
    private readonly WorkflowService _service;
    
    public MyViewModel(IZ21 z21, WorkflowService service)
    {
        _z21 = z21;
        _service = service;
    }
}
```

### ❌ Unnecessary Factories (NEVER)

```csharp
// BAD - Constructor injection does this automatically
services.AddSingleton(sp => new MyViewModel(
    sp.GetRequiredService<IZ21>()
));

// GOOD - Let DI do it
services.AddSingleton<MyViewModel>();
```

### ❌ Circular Dependencies (NEVER)

```csharp
// BAD - ServiceA needs ServiceB, ServiceB needs ServiceA
public class ServiceA
{
    public ServiceA(ServiceB serviceB) { }
}

public class ServiceB
{
    public ServiceB(ServiceA serviceA) { }
}

// This throws InvalidOperationException at runtime
```

**Better:** Refactor to break the cycle
```csharp
// Use an interface to break the dependency chain
public class ServiceA
{
    public ServiceA(IServiceBFacade facade) { }
}

public interface IServiceBFacade { }
public class ServiceB : IServiceBFacade { }
```

---

## Decision Tree

```
┌─ Is this a Page? ──→ AddTransient<T>()
│                        └─ Special dependencies? → Custom factory
│
├─ Is this a ViewModel? ──→ Singleton?
│  │                         ├─ YES: AddSingleton<T>()
│  │                         └─ NO: Custom factory (if complex)
│  │
│  └─ Is it a Wrapper? → new XxxViewModel(...) at runtime
│
├─ Is this a Service? ──→ AddSingleton or AddTransient
│  └─ Needs fallback? → Custom factory with try/catch
│
└─ Is this a Config? ──→ AddSingleton + IOptions<T>
   └─ Platform-specific? → Register in App.xaml.cs OR MauiProgram.cs separately
```

---

## References

- **Instructions:** [`.github/instructions/di-pattern-consistency.instructions.md`](../.github/instructions/di-pattern-consistency.instructions.md)
- **Tests:** [`Test/DiContainerIntegrationTests.cs`](../Test/DiContainerIntegrationTests.cs)
- **Validation:** [`WinUI/Extensions/DiValidationExtensions.cs`](../WinUI/Extensions/DiValidationExtensions.cs)
- **Microsoft Docs:** [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

## Summary

| Pattern | Registration | When | Example |
|---------|--------------|------|---------|
| Standard Constructor | `AddSingleton<T>()` or `AddTransient<T>()` | Simple, all deps in container | `MyViewModel`, `OverviewPage` |
| Custom Factory | `AddSingleton(sp => new T(...))` | Complex, conditional logic, NullObject | `MainWindowViewModel` |
| Wrapper | `new XxxViewModel(...)` | Domain model wrappers | `JourneyViewModel` |
| Service Locator | ❌ NEVER | — | — |
| Circular Dependency | Refactor | — | — |

