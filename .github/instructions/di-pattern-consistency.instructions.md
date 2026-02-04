---
description: 'DI pattern consistency for WinUI, MAUI, and shared ViewModels'
applyTo: 'WinUI/**/*.cs,MAUI/**/*.cs,SharedUI/**/*.cs'
---

# DI Pattern Consistency

## 🎯 Core Principles

1. **Constructor Injection Only** - No Service Locator pattern
2. **Transient Pages** - New instance per navigation
3. **Singleton ViewModels** - Shared across application
4. **Custom Factories Only When Necessary** - Document why they exist

---

## Page Registration Pattern (ALL pages follow this)

### Standard Pages
```csharp
// ✅ CORRECT - WinUI/View/MyPage.xaml.cs
public sealed partial class MyPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    public MyPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}

// Registration in WinUI/App.xaml.cs
services.AddTransient<MyPage>();
navigationRegistry.Register("mytag", "My Page", "\uE123", typeof(MyPage), "Shell", ...);
```

### Special Pages with Custom Dependencies
```csharp
// ⚠️ ONLY IF NECESSARY - WinUI/View/SignalBoxPage.xaml.cs
public sealed partial class SignalBoxPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ISkinProvider skinProvider,           // ← Custom dependency
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

// Registration with custom factory (Document the reason!)
// Reason: SignalBoxPage requires ISkinProvider for dynamic theme switching
services.AddSingleton<SignalBoxPage>(sp => new SignalBoxPage(
    sp.GetRequiredService<MainWindowViewModel>(),
    sp.GetRequiredService<ISkinProvider>(),
    sp.GetRequiredService<AppSettings>(),
    sp.GetService<ISettingsService>()
));
```

---

## ViewModel Registration Patterns

### Singleton ViewModels (Shared across app)

#### Simple Case - Use Constructor Injection
```csharp
// ✅ PREFERRED - If all dependencies are registered
services.AddSingleton<MyViewModel>();
// All constructor parameters are auto-injected
```

#### Complex Case - Manual Factory (with documentation)
```csharp
// ⚠️ ONLY IF constructor injection doesn't work
// Reason: MainWindowViewModel has optional services that need fallback to NullObject pattern
services.AddSingleton(sp => new MainWindowViewModel(
    sp.GetRequiredService<IZ21>(),
    sp.GetRequiredService<WorkflowService>(),
    // ... required services ...
    sp.GetRequiredService<IIoService?>()  // ← optional
));
```

### Wrapper ViewModels (Created at runtime, not registered)

```csharp
// ✅ Domain model wrappers are created with 'new', not registered
// They implement IViewModelWrapper<T> interface
public class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    public Journey Model { get; }
    
    public JourneyViewModel(Journey journey, Project project, ...)
    {
        // Created at runtime: new JourneyViewModel(journey, project, ...)
        // NOT registered in DI container
    }
}

// Usage in UI code
var journeyVM = new JourneyViewModel(journey, _project, ...);
```

---

## When to Create ViewModel vs Reuse MainWindowViewModel

| Scenario | Decision | Example |
|----------|----------|---------|
| Simple page (readonly list) | ✅ Reuse MainWindowViewModel | OverviewPage, HelpPage |
| Domain model wrapper (1:1) | ✅ Create XxxViewModel | JourneyViewModel, TrainViewModel |
| Complex editor/multi-state | ✅ Create specialized VM | TrackPlanViewModel, JourneyMapViewModel |
| Page-specific UI state | ✅ Create specialized VM | TrainControlViewModel (color themes) |
| Optional platform service | ✅ Add to MainWindowViewModel | PhotoHubClient (WinUI only), ISettingsService |

---

## Constructor Parameter Guidelines

### Required Parameters
```csharp
// ✅ CORRECT - Specify required dependencies
public MyViewModel(
    IZ21 z21,                      // Always required
    WorkflowService workflowService,
    IUiDispatcher uiDispatcher)    // Always required
{
    _z21 = z21;
    _workflowService = workflowService;
    _uiDispatcher = uiDispatcher;
}
```

### Optional Parameters (with fallback)
```csharp
// ✅ CORRECT - Optional with null-coalescing
public MyViewModel(
    IZ21 z21,
    ILogger<MyViewModel> logger,
    ISettingsService? settingsService = null)  // ← Optional
{
    _z21 = z21;
    _logger = logger;
    _settingsService = settingsService ?? new NullSettingsService();  // ← Fallback
}
```

---

## DI Container Validation

### Startup Validation (WinUI/App.xaml.cs)
```csharp
private static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    // ... registrations ...
    var provider = services.BuildServiceProvider();
    
    // ✅ Validate that all Pages can be resolved
    ValidateDiContainer(provider);
    
    return provider;
}

private static void ValidateDiContainer(IServiceProvider provider)
{
    // Verify critical services resolve without error
    try
    {
        _ = provider.GetRequiredService<MainWindowViewModel>();
        _ = provider.GetRequiredService<OverviewPage>();
        _ = provider.GetRequiredService<SignalBoxPage>();
        // ... etc ...
        Debug.WriteLine("[DI] Container validation passed");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[DI] Container validation FAILED: {ex.Message}");
        throw;
    }
}
```

---

## Checklist: BEFORE Creating New Page

- [ ] **Analyze:** Does this page need its own ViewModel or can it use MainWindowViewModel?
- [ ] **Name:** Use `XxxPage.xaml.cs` (PascalCase, 'Page' suffix)
- [ ] **Constructor:** Accept `MainWindowViewModel` or specialized `XxxViewModel`
- [ ] **Register:** Add `services.AddTransient<XxxPage>()` in `App.xaml.cs`
- [ ] **Navigate:** Register tag in `NavigationRegistry` (WinUI) or use Shell (MAUI)
- [ ] **DataContext:** Bind `DataContext="{x:Bind ViewModel}"` in XAML
- [ ] **Comment:** If using custom factory, document WHY (e.g., "Requires ISkinProvider")

---

## Checklist: BEFORE Creating New ViewModel

- [ ] **Exists:** Check if MainWindowViewModel already handles this concern
- [ ] **Type:** Is it a wrapper (IViewModelWrapper<T>) or standalone?
- [ ] **Dependencies:** List all required/optional services
- [ ] **Singleton:** Will this VM be reused, or created per-page?
- [ ] **Register:** Add to `WinUI/App.xaml.cs` + `MAUI/MauiProgram.cs`
- [ ] **Comment:** Document why this ViewModel was created
- [ ] **Test:** Verify it resolves from DI container

---

## Anti-Patterns (NEVER do)

❌ **Custom factory for simple ViewModel**
```csharp
// BAD - Unnecessary factory
services.AddSingleton(sp => new MyViewModel(sp.GetRequiredService<IZ21>()));

// GOOD - Let DI wire constructor
services.AddSingleton<MyViewModel>();
```

❌ **Service Locator pattern**
```csharp
// BAD - Hard to test, runtime errors
var service = ServiceLocator.GetService<IMyService>();

// GOOD - Constructor injection
public MyViewModel(IMyService service) => _service = service;
```

❌ **Separate ViewModel per simple page**
```csharp
// BAD - Unnecessary proliferation
public OverviewPageViewModel { }  // <- Page can just use MainWindowViewModel

// GOOD - Reuse what exists
public OverviewPage(MainWindowViewModel viewModel) { ViewModel = viewModel; }
```

---

## Cross-Platform Consistency (WinUI + MAUI + WebApp)

Both platforms must register core services identically:

```csharp
// WinUI/App.xaml.cs
private static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    // ... add services ...
    services.AddMobaBackendServices();  // ← Shared across platforms
}

// MAUI/MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.Services.AddMobaBackendServices();  // ← Same extension method
}
```

---

## Reference Examples

- **Standard Page:** `WinUI/View/OverviewPage.xaml.cs` - Simple MainWindowVM
- **Custom Page:** `WinUI/View/SignalBoxPage.xaml.cs` - With ISkinProvider factory
- **Simple ViewModel:** `SharedUI/ViewModel/LocomotiveViewModel.cs` - Wrapper model
- **Complex ViewModel:** `SharedUI/ViewModel/MainWindowViewModel.cs` - Partial classes, multi-concern
- **Specialized ViewModel:** `SharedUI/ViewModel/TrainControlViewModel.cs` - UI-state-specific

---

## Troubleshooting

### "The type has no constructors defined"
**Problem:** ViewModel has multiple constructors or optional parameters not in DI
**Solution:** Ensure DI container knows which constructor to use
```csharp
// If multiple constructors, make one [ImportingConstructor] or simplify
public MyViewModel(IZ21 z21)  // ← Single, clear constructor
```

### "Unable to resolve service for type..."
**Problem:** A dependency isn't registered
**Solution:** Check `WinUI/App.xaml.cs` or `MAUI/MauiProgram.cs`
```csharp
// Add missing registration
services.AddSingleton<IMissingService, MissingServiceImpl>();
```

### "Circular dependency detected"
**Problem:** ServiceA needs ServiceB which needs ServiceA
**Solution:** Refactor to separate concerns or use factory pattern
```csharp
// Instead of circular: ServiceA → ServiceB → ServiceA
// Use interfaces and lazy evaluation or split services
```

---

## 🎓 Lesson Learned

**Consistency matters more than perfection.**

- If existing patterns work, replicate them
- Document exceptions (custom factories, optional services)
- Validate container at startup
- Test that all Pages/ViewModels resolve without errors
