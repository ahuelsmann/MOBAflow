# Plugin Development Guide

> **Complete guide to extending MOBAflow with plugins**

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Architecture](#architecture)
4. [Plugin Structure](#plugin-structure)
5. [Framework Classes](#framework-classes)
6. [Dependency Injection](#dependency-injection)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)
9. [Advanced Topics](#advanced-topics)

---

## Overview

The MOBAflow plugin system allows developers to extend the application with custom pages, features, and integrations **without modifying core code**. Plugins are:

- **Automatically discovered** from the `WinUI/bin/Debug/Plugins/` directory
- **Dynamically validated** on application startup
- **Fully integrated** with the DI container
- **Lifecycle-managed** with init/cleanup hooks
- **Robust** - broken plugins don't crash the app

### Why Plugins?

| Scenario | Without Plugins | With Plugins |
|----------|-----------------|--------------|
| Add a new page | Modify core code | Drop plugin DLL |
| Use host services | Tight coupling | Loose coupling via DI |
| Version independently | Force full rebuild | Independent versioning |
| Share functionality | Copy-paste code | NuGet package |
| Test in isolation | Mock everything | Real dependencies via DI |

---

## Quick Start

### Step 1: Copy Template

```bash
cp -r Plugins/MinimalPlugin Plugins/MyAwesomePlugin
```

### Step 2: Update .csproj

Edit `Plugins/MyAwesomePlugin/MyAwesomePlugin.csproj`:

```xml
<RootNamespace>Moba.Plugin</RootNamespace>
<EnableDynamicLoading>true</EnableDynamicLoading>
```

### Step 3: Rename Classes

- `MinimalPlugin` → `MyAwesomePlugin`
- `MinimalPluginViewModel` → `MyAwesomePluginViewModel`
- `MinimalPluginPage` → `MyAwesomePluginPage`

### Step 4: Implement

```csharp
public sealed class MyAwesomePlugin : PluginBase
{
    public override string Name => "My Awesome Plugin";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Your Name",
        Description: "What it does"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            "myawesome", "My Awesome", "\uECCD", typeof(MyAwesomePluginPage)
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MyAwesomePluginViewModel>();
        services.AddTransient<MyAwesomePluginPage>();
    }
}
```

### Step 5: Build

```bash
dotnet build Plugins/MyAwesomePlugin
# DLL → WinUI/bin/Debug/Plugins/
```

**Done!** Restart WinUI, see your plugin in the menu.

---

## Architecture

### Plugin Lifecycle

```
[1] DISCOVERY (App Startup)
    ↓ Scan Plugins folder
[2] VALIDATION
    ↓ Check names, tags, etc.
[3] CONFIGURATION
    ↓ ConfigureServices(services)
[4] INITIALIZATION
    ↓ OnInitializedAsync()
[5] RUNTIME
    ↓ User interactions
[6] UNLOADING (App Shutdown)
    ↓ OnUnloadingAsync()
```

### Plugin Dependency Resolution

```
DI Container has:
  ├─ MainWindowViewModel (from host)
  ├─ IZ21 (from host)
  ├─ WorkflowService (from host)
  └─ MyAwesomePluginViewModel (from plugin)

MyAwesomePluginPage needs:
  ├─ MyAwesomePluginViewModel
  │   └─ MainWindowViewModel ✅ Available from host
  └─ MainWindowViewModel ✅ Available from host
```

---

## Plugin Structure

### Minimal Plugin Layout

```
Plugins/MyAwesomePlugin/
├── MyAwesomePlugin.csproj           # Project configuration
├── MyAwesomePlugin.cs               # Plugin entry point
├── MyAwesomePluginViewModel.cs      # MVVM ViewModel
├── MyAwesomePluginPage.cs           # WinUI Page
└── README.md                        # Documentation (optional)
```

### File Purposes

#### MyAwesomePlugin.cs (Entry Point)

```csharp
using Moba.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

public sealed class MyAwesomePlugin : PluginBase
{
    // Plugin identity
    public override string Name => "My Awesome Plugin";

    // Version, author, dependencies
    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Your Name",
        Description: "Short description"
    );

    // Pages exposed by plugin
    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "myawesome",                    // Navigation tag (unique!)
            Title: "My Awesome Plugin",          // Menu display text
            IconGlyph: "\uECCD",                // Fluent Icon (optional)
            PageType: typeof(MyAwesomePluginPage)
        );
    }

    // DI registration
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MyAwesomePluginViewModel>();
        services.AddTransient<MyAwesomePluginPage>();
    }

    // Initialization hook
    public override async Task OnInitializedAsync()
    {
        // Called after plugin loads, before runtime
        // Use for setup, resource loading, etc.
        await Task.CompletedTask;
    }

    // Cleanup hook
    public override async Task OnUnloadingAsync()
    {
        // Called on app shutdown
        // Use for cleanup, saving state, etc.
        await Task.CompletedTask;
    }
}
```

#### MyAwesomePluginViewModel.cs (MVVM Logic)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.SharedUI.ViewModel;

public sealed partial class MyAwesomePluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindow;

    // Reactive property (auto INotifyPropertyChanged)
    [ObservableProperty]
    private string title = "My Awesome Plugin";

    // Constructor injection
    public MyAwesomePluginViewModel(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        
        // Listen to host state changes
        _mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
    }

    // Command (auto ICommand implementation)
    [RelayCommand]
    private async Task DoSomething()
    {
        // Execute logic here
        Title = "Done!";
        await Task.CompletedTask;
    }

    private void OnMainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected))
        {
            // React to host state changes
        }
    }
}
```

#### MyAwesomePluginPage.cs (UI)

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed class MyAwesomePluginPage : Page
{
    public MyAwesomePluginViewModel ViewModel { get; }

    public MyAwesomePluginPage(
        MyAwesomePluginViewModel viewModel,
        MainWindowViewModel mainWindowViewModel)
    {
        _ = mainWindowViewModel; // Dependency for DI (not directly used)
        
        ViewModel = viewModel;
        DataContext = viewModel; // All bindings use ViewModel
        
        // Build UI programmatically or with XAML
        Content = new Grid
        {
            Padding = new Thickness(16),
            Children =
            {
                new TextBlock
                {
                    Text = "My Awesome Plugin",
                    Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
                }
            }
        };
    }
}
```

---

## Framework Classes

All plugin framework classes are in `Common/Plugins/`:

### IPlugin Interface

Contract that all plugins must implement (via `PluginBase`):

```csharp
public interface IPlugin
{
    string Name { get; }
    PluginMetadata Metadata { get; }
    IEnumerable<PluginPageDescriptor> GetPages();
    void ConfigureServices(IServiceCollection services);
    Task OnInitializedAsync();
    Task OnUnloadingAsync();
}
```

### PluginBase Class

Abstract base class (recommended to use instead of implementing IPlugin):

```csharp
public abstract class PluginBase : IPlugin
{
    public abstract string Name { get; }
    public virtual PluginMetadata Metadata => new(Name);
    public virtual IEnumerable<PluginPageDescriptor> GetPages() => [];
    public virtual void ConfigureServices(IServiceCollection services) { }
    public virtual Task OnInitializedAsync() => Task.CompletedTask;
    public virtual Task OnUnloadingAsync() => Task.CompletedTask;
}
```

**Benefits of inheriting from PluginBase:**
- Only override what you need
- Default implementations provided
- Cleaner, more readable code

### PluginMetadata Record

Metadata about your plugin:

```csharp
public sealed record PluginMetadata(
    string Name,                          // Display name
    string? Version = null,               // Semantic version (1.0.0)
    string? Author = null,                // Your name
    string? Description = null,           // Short description
    string? MinimumHostVersion = null,    // Host compatibility (3.15)
    IEnumerable<string>? Dependencies = null  // Optional dependencies
);
```

### PluginPageDescriptor Record

Describes a page exposed by your plugin:

```csharp
public sealed record PluginPageDescriptor(
    string Tag,           // Unique ID (used in navigation)
    string Title,         // Display name in menu
    string? IconGlyph,    // Fluent Icon Unicode (optional)
    Type PageType         // Your Page class
);
```

### PluginDiscoveryService

Discovers plugins in a directory:

```csharp
var plugins = PluginDiscoveryService.DiscoverPlugins(
    pluginDirectory: "WinUI/bin/Debug/Plugins",
    logger: loggerInstance
);
// Returns: IReadOnlyList<IPlugin>
```

### PluginValidator

Validates plugins for common issues:

```csharp
// Validate single plugin
bool isValid = PluginValidator.ValidatePlugin(plugin, logger);

// Validate multiple plugins
int validCount = PluginValidator.ValidatePlugins(plugins, logger);
```

### PluginLoader

Manages plugin lifecycle:

```csharp
var loader = new PluginLoader(pluginDirectory, registry);

// Load and register plugins
await loader.LoadPluginsAsync(services, logger);

// Initialize plugins after app startup
await loader.InitializePluginsAsync(logger);

// Cleanup on shutdown
await loader.UnloadPluginsAsync(logger);

// Check loaded plugins
var loaded = loader.LoadedPlugins; // IReadOnlyList<IPlugin>
```

---

## Dependency Injection

### Available Host Services

Your plugin can inject **any registered host service**:

```csharp
// Core Services
MainWindowViewModel      // App state, commands
IZ21                    // Z21 control station
Solution                // Current solution model
WorkflowService         // Workflow management

// Utilities
IIoService             // File operations
ICityService           // City/station library
ISettingsService       // Application settings
ILogger<T>             // Structured logging
IConfiguration         // App configuration
```

### Injection Examples

```csharp
// ✅ CORRECT: Multiple dependencies
public MyAwesomePluginViewModel(
    MainWindowViewModel mainWindow,
    IZ21 z21,
    WorkflowService workflow,
    ILogger<MyAwesomePluginViewModel> logger)
{
    _mainWindow = mainWindow;
    _z21 = z21;
    _workflow = workflow;
    _logger = logger;
}

// ✅ CORRECT: Only what you need
public MyAwesomePluginViewModel(IZ21 z21)
{
    _z21 = z21;
}

// ❌ WRONG: Service Locator (hidden dependencies)
public MyAwesomePluginViewModel()
{
    var z21 = ServiceLocator.Get<IZ21>();  // Don't do this!
}
```

### Example: Using Host Services

```csharp
[RelayCommand]
private async Task ConnectToZ21()
{
    try
    {
        var connected = await _z21.ConnectAsync(_mainWindow.Z21IpAddress);
        if (connected)
        {
            Title = "Z21 Connected!";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Z21 connection failed");
    }
}
```

---

## Best Practices

### ✅ DO

- **Inherit from PluginBase** - Cleaner, less boilerplate
- **Use MVVM with CommunityToolkit.Mvvm** - Observable properties & commands
- **Inject dependencies** - Via constructor, not service locator
- **Implement lifecycle hooks** - For setup/cleanup
- **Provide metadata** - Version, author, description
- **Log important events** - Use ILogger<T>
- **Handle errors gracefully** - Try-catch, user feedback
- **Use unique page tags** - Avoid conflicts
- **Write inline documentation** - XML comments on public members

### ❌ DON'T

- **Use reserved page tags** - overview, solution, journeys, workflows, trains, trackplaneditor, journeymap, monitor, settings
- **Create duplicate page tags** - Each tag must be unique
- **Access host state without null checks** - Defensive programming
- **Ignore lifecycle hooks** - Always implement cleanup
- **Store host service references after use** - Release them
- **Use synchronous I/O** - Always async
- **Hardcode configuration** - Use metadata and DI
- **Modify core plugin framework** - Use it as-is
- **Mix navigation concerns** - Let PluginRegistry handle it

### Naming Conventions

```csharp
// Plugin class
public sealed class MyAwesomePlugin : PluginBase { }

// ViewModel
public sealed partial class MyAwesomePluginViewModel : ObservableObject { }

// Page
public sealed class MyAwesomePluginPage : Page { }

// Project/Folder
Plugins/MyAwesomePlugin/
```

---

## Troubleshooting

### Plugin not loading?

**Check:**
1. DLL location: `WinUI/bin/Debug/Plugins/MyPlugin.dll`
2. Plugin class is `public` and not `abstract`
3. Logs: `WinUI/bin/Debug/logs/mobaflow-*.log`
4. Namespace matches RootNamespace in `.csproj`

**Common causes:**
- ❌ Internal class → Make it `public`
- ❌ Wrong namespace → Should be `Moba.Plugin`
- ❌ Build configuration → Use Debug, not Release
- ❌ Missing references → Add `<Private>false</Private>`

### "No IPlugin implementations found"

- Must inherit from `PluginBase` or implement `IPlugin`
- Must be `public sealed class` (not abstract)
- Must be in correct namespace (`Moba.Plugin`)
- DLL must be in plugins folder

### Duplicate page tag error

- Each page must have **unique** tag
- Check against core pages: overview, solution, journeys, workflows, trains, trackplaneditor, journeymap, monitor, settings
- Check against other plugins

### MainWindowViewModel not injected

- Constructor parameter must be exactly: `MainWindowViewModel mainWindowViewModel`
- Cannot be optional `MainWindowViewModel?`
- Must be registered by host (it is by default)

### DLL dependency resolution fails

- Enable `<EnableDynamicLoading>true</EnableDynamicLoading>` in .csproj
- Use `<Private>false</Private>` for shared references
- Check `.deps.json` file is in plugins folder

---

## Advanced Topics

### Accessing Multiple Host Services

```csharp
public sealed partial class AdvancedPluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly IZ21 _z21;
    private readonly WorkflowService _workflow;
    private readonly ILogger<AdvancedPluginViewModel> _logger;

    public AdvancedPluginViewModel(
        MainWindowViewModel mainWindow,
        IZ21 z21,
        WorkflowService workflow,
        ILogger<AdvancedPluginViewModel> logger)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _z21 = z21 ?? throw new ArgumentNullException(nameof(z21));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand]
    private async Task ExecuteWorkflow()
    {
        try
        {
            _logger.LogInformation("Executing workflow from plugin");
            await _workflow.ExecuteAsync(_mainWindow.SelectedWorkflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed");
        }
    }
}
```

### Lifecycle Hooks with Resource Management

```csharp
public override async Task OnInitializedAsync()
{
    _logger.LogInformation("Plugin initializing");
    
    // Load resources
    _data = await _service.LoadDataAsync();
    
    // Setup event handlers
    _mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
    
    _logger.LogInformation("Plugin initialized successfully");
}

public override async Task OnUnloadingAsync()
{
    _logger.LogInformation("Plugin unloading");
    
    // Cleanup event handlers
    _mainWindow.PropertyChanged -= OnMainWindowPropertyChanged;
    
    // Save state
    await _service.SaveStateAsync(_data);
    
    // Dispose resources
    _data?.Dispose();
    
    _logger.LogInformation("Plugin unloaded");
}
```

### Multiple Pages from Single Plugin

```csharp
public override IEnumerable<PluginPageDescriptor> GetPages()
{
    yield return new PluginPageDescriptor(
        "myawesome-dashboard",
        "Dashboard",
        "\uE7FC",
        typeof(MyAwesomePluginDashboardPage)
    );
    
    yield return new PluginPageDescriptor(
        "myawesome-settings",
        "Settings",
        "\uE115",
        typeof(MyAwesomePluginSettingsPage)
    );
    
    yield return new PluginPageDescriptor(
        "myawesome-logs",
        "Logs",
        "\uEA37",
        typeof(MyAwesomePluginLogsPage)
    );
}

public override void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<MyAwesomePluginDashboardViewModel>();
    services.AddTransient<MyAwesomePluginDashboardPage>();
    
    services.AddTransient<MyAwesomePluginSettingsViewModel>();
    services.AddTransient<MyAwesomePluginSettingsPage>();
    
    services.AddTransient<MyAwesomePluginLogsViewModel>();
    services.AddTransient<MyAwesomePluginLogsPage>();
}
```

---

## Resources

- **Main Documentation:** [`README.md`](../../README.md#-plugin-development)
- **Architecture Details:** [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md)
- **Minimal Plugin Template:** [`Plugins/MinimalPlugin/`](../../Plugins/MinimalPlugin/)
- **Framework Source:** [`Common/Plugins/`](../../Common/Plugins/)
- **MVVM Toolkit:** https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- **WinUI 3 Docs:** https://learn.microsoft.com/windows/apps/winui/
- **Fluent Icons:** https://www.microsoft.com/design/fluent-system/

---

**Happy Plugin Development!** 🚀

If you have questions, check the troubleshooting section or open an issue on the repository.
