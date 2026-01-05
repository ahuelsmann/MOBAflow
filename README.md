# MOBAflow

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android%20%7C%20Web-brightgreen)](#)
[![Build Status](https://dev.azure.com/ahuelsmann/MOBAflow/_apis/build/status/MOBAflow?branchName=main)](https://dev.azure.com/ahuelsmann/MOBAflow/_build)

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

> ⚖️ **Legal Notice:** MOBAflow is an independent open-source project. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details on third-party software, formats, and trademarks (AnyRail, Piko, Roco).

## ✨ Features

- 🚂 **Z21 Direct UDP Control** - Real-time communication with Roco Z21
- 🎯 **Journey Management** - Define train routes with multiple stations
- 🔊 **Text-to-Speech** - Azure Cognitive Services & Windows Speech
- ⚡ **Workflow Automation** - Event-driven action sequences
- 🎨 **Track Plan Import** - Import layouts from AnyRail (user-exported XML files)
- 📱 **Multi-Platform** - WinUI (Windows), MAUI (Android), Blazor (Web)

## 🛤️ AnyRail Integration

MOBAflow supports **importing track layouts from AnyRail** (user-exported XML files for personal use). This feature enables:
- ✅ Import of user-created AnyRail track plans (XML format)
- ✅ Automatic detection of track geometry and article codes
- ✅ SVG path generation for visualization

**Important:** AnyRail is proprietary software by Carsten Kühling & Paco Ahlqvist. MOBAflow is **independent** and **not affiliated** with AnyRail. The import feature is provided for **interoperability** purposes (fair use) and allows users to import their **own exported track plans**. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for legal details.

## 📋 Quick Links

- 📖 **Documentation:** [`docs/wiki/INDEX.md`](docs/wiki/INDEX.md)
- 🏗️ **Architecture:** [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- 🤖 **AI Instructions:** [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- 🤝 **Contributing:** [`CONTRIBUTING.md`](CONTRIBUTING.md)
- 📜 **Third-Party Licenses:** [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)

## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK** (or later)
- **Visual Studio 2026** (recommended)
- **Roco Z21 Digital Command Station**

### Clone & Build


```bash
git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
cd MOBAflow
dotnet restore
dotnet build
```

### Run Applications

**WinUI (Windows Desktop):**
```bash
dotnet run --project WinUI
```

**WebApp (Blazor Dashboard):**
```bash
dotnet run --project WebApp
```

**MAUI (Android):**
```bash
dotnet build MAUI -f net10.0-android
```

### Run Tests
```bash
dotnet test
```

## 📦 Architecture

MOBAflow follows **Clean Architecture** principles:

```
Domain (Pure POCOs)
  ↑
Backend (Platform-independent logic)
  ↑
SharedUI (Base ViewModels)
  ↑
WinUI / MAUI / Blazor (Platform-specific)
```

### Technology Stack

- **Framework:** .NET 10
- **UI Frameworks:** WinUI 3, .NET MAUI, Blazor Server
- **MVVM:** CommunityToolkit.Mvvm
- **Logging:** Serilog (File + In-Memory Sink for real-time UI)
- **Speech:** Azure Cognitive Services, Windows Speech API
- **Networking:** Direct UDP to Z21 (no external dependencies)
- **Testing:** NUnit

### Logging Infrastructure

MOBAflow uses **Serilog** for centralized, structured logging:

- **File Logs:** `bin/Debug/logs/mobaflow-YYYYMMDD.log` (rolling, 7-day retention)
- **In-Memory Sink:** Real-time log streaming to MonitorPage UI
- **Structured Logging:** Searchable properties instead of string interpolation
- **Log Levels:** Debug (Moba namespace), Warning (Microsoft namespace)

**Example:**
```csharp
_logger.LogInformation("Feedback received: InPort={InPort}, Value={Value}", inPort, value);
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for detailed architecture documentation.

## 🔌 Plugin Development

MOBAflow includes a **flexible and extensible plugin system** that allows developers to add custom pages, features, and integrations without modifying the core application. Plugins are automatically discovered, validated, and loaded at runtime.

### Overview

The plugin framework provides:
- 🎯 **Easy Discovery** - Plugins in `WinUI/bin/Debug/Plugins/` are auto-discovered
- ✅ **Validation** - Automatic plugin validation on startup
- 🔄 **Lifecycle Hooks** - Initialize and cleanup at app startup/shutdown
- 💉 **Dependency Injection** - Full DI container support for plugin services
- 📦 **Metadata** - Version tracking, author info, and dependency declarations
- 🛡️ **Robustness** - App runs fine even if plugins are missing or broken

### Architecture

```
IPlugin Interface (Contract)
    ↓
PluginBase (Abstract Base Class)
    ↓
YourPlugin : PluginBase
    ↓
PluginDiscoveryService (Auto-Discovery)
    ↓
PluginValidator (Validation)
    ↓
PluginLoader (Registration & Lifecycle)
    ↓
DI Container (Service Resolution)
```

### Plugin Lifecycle

1. **Discovery** (Startup)
   - Plugin DLL is found in `WinUI/bin/Debug/Plugins/`
   - Reflected for `IPlugin` implementations

2. **Validation** (Startup)
   - Plugin.Name is checked
   - Page tags validated for duplicates
   - Reserved tags trigger warnings

3. **Configuration** (Startup)
   - `ConfigureServices()` called
   - Plugin services registered with DI

4. **Initialization** (After app startup)
   - `OnInitializedAsync()` called
   - Resource loading, logging, setup

5. **Runtime** (During app execution)
   - Pages accessible in NavigationView
   - ViewModels respond to user actions

6. **Unloading** (App shutdown)
   - `OnUnloadingAsync()` called
   - Cleanup, state saving, resource disposal

### Quick Start: Creating Your First Plugin

#### Step 1: Copy the Minimal Plugin Template

```bash
cp -r Plugins/MinimalPlugin Plugins/MyAwesomePlugin
```

#### Step 2: Update Project File

Edit `Plugins/MyAwesomePlugin/MyAwesomePlugin.csproj`:

```xml
<!-- RootNamespace stays as Moba.Plugin (class naming follows folder structure) -->
<RootNamespace>Moba.Plugin</RootNamespace>

<!-- EnableDynamicLoading ensures correct .deps.json generation -->
<EnableDynamicLoading>true</EnableDynamicLoading>
```

#### Step 3: Rename Classes

- `MinimalPlugin` → `MyAwesomePlugin`
- `MinimalPluginViewModel` → `MyAwesomePluginViewModel`
- `MinimalPluginPage` → `MyAwesomePluginPage`

#### Step 4: Implement Your Plugin

```csharp
public sealed class MyAwesomePlugin : PluginBase
{
    public override string Name => "My Awesome Plugin";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Your Name",
        Description: "What your plugin does",
        MinimumHostVersion: "3.15"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "myawesomeplugin",
            Title: "My Awesome Plugin",
            IconGlyph: "\uECCD",
            PageType: typeof(MyAwesomePluginPage)
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MyAwesomePluginViewModel>();
        services.AddTransient<MyAwesomePluginPage>();
    }
}
```

#### Step 5: Build

```bash
dotnet build Plugins/MyAwesomePlugin
# DLL is automatically copied to WinUI/bin/Debug/Plugins/
```

### Plugin Framework Classes

#### IPlugin Interface

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

#### PluginBase Class

Base class that implements IPlugin with sensible defaults:

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

Simply inherit and override only what you need!

#### PluginMetadata Record

```csharp
public sealed record PluginMetadata(
    string Name,
    string? Version = null,
    string? Author = null,
    string? Description = null,
    string? MinimumHostVersion = null,
    IEnumerable<string>? Dependencies = null
);
```

#### PluginPageDescriptor Record

```csharp
public sealed record PluginPageDescriptor(
    string Tag,           // Unique page identifier
    string Title,         // NavigationView menu text
    string? IconGlyph,    // Fluent Icon (optional)
    Type PageType         // Your WinUI Page type
);
```

### ViewModel Best Practices

All plugin ViewModels should:
1. **Inherit from `ObservableObject`** (CommunityToolkit.Mvvm)
2. **Use `[ObservableProperty]` attributes** for reactive properties
3. **Use `[RelayCommand]` attributes** for command handlers
4. **Accept dependencies via constructor** (DI)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.SharedUI.ViewModel;

public sealed partial class MyAwesomePluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    [ObservableProperty]
    private string title = "My Awesome Plugin";

    [ObservableProperty]
    private bool isConnected;

    public MyAwesomePluginViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        IsConnected = mainWindowViewModel.IsConnected;
        _mainWindowViewModel.PropertyChanged += OnMainWindowPropertyChanged;
    }

    [RelayCommand]
    private void DoSomething()
    {
        // Your implementation here
    }

    private void OnMainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected))
            IsConnected = _mainWindowViewModel.IsConnected;
    }
}
```

### Dependency Injection

Plugins can inject **any host service** automatically:

#### Available Host Services

- **MainWindowViewModel** - Main app state and commands
- **IZ21** - Z21 control station interface
- **Solution** - Current solution model
- **WorkflowService** - Workflow management
- **IIoService** - File operations
- **ICityService** - City/station library
- **ISettingsService** - Application settings
- Any other registered host service

#### Example: Accessing Host Services

```csharp
public sealed partial class MyAwesomePluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly IZ21 _z21;
    private readonly Solution _solution;

    public MyAwesomePluginViewModel(
        MainWindowViewModel mainWindow,
        IZ21 z21,
        Solution solution)
    {
        _mainWindow = mainWindow;
        _z21 = z21;
        _solution = solution;
    }

    [RelayCommand]
    private async Task ConnectToZ21()
    {
        var isConnected = await _z21.ConnectAsync(_mainWindow.Z21IpAddress);
        // ... handle result
    }
}
```

### Configuration & Versioning

Each plugin should declare metadata for tracking:

```csharp
public override PluginMetadata Metadata => new(
    Name: "My Awesome Plugin",
    Version: "1.0.0",                       // Semantic versioning
    Author: "Your Name",                    // Plugin author
    Description: "What your plugin does",   // Short description
    MinimumHostVersion: "3.15",             // Host compatibility
    Dependencies: new[] {                   // Optional dependencies
        "SharedUI",
        "Some.NuGet.Package"
    }
);
```

### Lifecycle Hooks

Use lifecycle hooks for setup and cleanup:

```csharp
public override async Task OnInitializedAsync()
{
    // Called after plugin loads and DI is set up
    // Use for resource loading, initialization, logging, etc.
    _logger.LogInformation("Plugin initialized");
    await Task.CompletedTask;
}

public override async Task OnUnloadingAsync()
{
    // Called when app is shutting down
    // Use for cleanup, saving state, disposing resources, etc.
    _logger.LogInformation("Plugin shutting down");
    await Task.CompletedTask;
}
```

### Robustness & Error Handling

The plugin system is **production-ready**:

| Scenario | Result |
|----------|--------|
| Plugins directory doesn't exist | ✅ Created automatically, app continues |
| No plugin DLLs found | ✅ Info log, app runs without plugins |
| Plugin DLL corrupted | ✅ Error log, other plugins load normally |
| Plugin validation fails | ✅ Warning log, plugin skipped |
| Plugin.OnInitializedAsync() throws | ✅ Error log, app continues |

**The app always runs**, even with no plugins or all broken plugins.

### Troubleshooting

#### Plugin not loading?

1. **Check the logs:**
   ```
   WinUI/bin/Debug/logs/mobaflow-YYYYMMDD.log
   ```

2. **Verify plugin DLL location:**
   ```
   WinUI/bin/Debug/Plugins/MyPlugin.dll
   ```

3. **Ensure plugin class is public:**
   ```csharp
   public sealed class MyPlugin : PluginBase  // ← Must be public!
   ```

#### "No IPlugin implementations found"?

- Class must inherit from `PluginBase` or implement `IPlugin`
- Class must not be `abstract`
- Namespace must match `RootNamespace` in `.csproj`

#### Duplicate page tag error?

- Each page must have a **unique** `Tag`
- Don't use reserved tags: `overview`, `solution`, `journeys`, `workflows`, `trains`, `trackplaneditor`, `journeymap`, `monitor`, `settings`

#### MainWindowViewModel not injected?

- Constructor parameter must be exactly: `MainWindowViewModel mainWindowViewModel`
- Cannot be optional or nullable
- MainWindowViewModel must be properly registered in host

### Best Practices

✅ **DO:**
- Inherit from `PluginBase` for cleaner code
- Use `CommunityToolkit.Mvvm` for reactive properties
- Implement lifecycle hooks if needed
- Provide metadata for version tracking
- Validate input in ViewModels
- Use proper error handling
- Follow naming conventions: `[Name]Plugin`, `[Name]PluginViewModel`, `[Name]PluginPage`

❌ **DON'T:**
- Use reserved page tags
- Create duplicate page tags
- Access MainWindowViewModel properties without null checks
- Forget to call base methods when overriding
- Use synchronous I/O operations
- Store host service references beyond plugin lifetime
- Hardcode configuration values (use Metadata instead)

### Plugin Template

A complete **Minimal Plugin** template is included in [`Plugins/MinimalPlugin/`](Plugins/MinimalPlugin/). Use this as a reference when creating new plugins.

Features demonstrated:
- ✅ PluginBase inheritance
- ✅ Metadata declaration
- ✅ Page registration
- ✅ ViewModel with MainWindowViewModel injection
- ✅ Observable properties and relay commands
- ✅ Lifecycle hooks
- ✅ Complete documentation

### Resources

- **Plugin Base Class:** [`Common/Plugins/PluginBase.cs`](Common/Plugins/PluginBase.cs)
- **Plugin Interface:** [`Common/Plugins/IPlugin.cs`](Common/Plugins/IPlugin.cs)
- **Plugin Loader:** [`WinUI/Service/PluginLoader.cs`](WinUI/Service/PluginLoader.cs)
- **Minimal Plugin Example:** [`Plugins/MinimalPlugin/`](Plugins/MinimalPlugin/)
- **MVVM Toolkit Docs:** https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- **WinUI 3 Docs:** https://learn.microsoft.com/windows/apps/winui/

## 🤝 Contributing

We welcome contributions! Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines.

**Quick Start:**
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📜 License

This project is licensed under the **MIT License** - see the [`LICENSE`](LICENSE) file for details.

### Third-Party Dependencies

MOBAflow uses several open-source packages. See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for a complete list of dependencies and their licenses.

## 📞 Contact & Support

- **Repository:** [Azure DevOps](https://dev.azure.com/ahuelsmann/MOBAflow)
- **Issues:** [Report a Bug](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems)
- **Maintainer:** Andreas Huelsmann ([@ahuelsmann](https://dev.azure.com/ahuelsmann))

## 🙏 Acknowledgments

- **Roco** for the Z21 Digital Command Station and protocol documentation
- **AnyRail** (Carsten Kühling & Paco Ahlqvist) - MOBAflow supports importing user-exported track plans (XML format) for interoperability
- **Piko** for the A-Track system geometry specifications
- **Freesound.org** - Audio library uses sound effects from [Freesound.org](https://freesound.org/) (licensed under Creative Commons 0 and Creative Commons Attribution). See [`Sound/Resources/Sounds/ATTRIBUTION.md`](Sound/Resources/Sounds/ATTRIBUTION.md) for detailed sound attributions.
- **.NET Foundation** for the amazing .NET ecosystem
- **CommunityToolkit** contributors for MVVM helpers
- **GitHub Copilot** for AI-assisted development and code quality improvements
- **All contributors** who help improve MOBAflow

---

**Made with ❤️ for model railroad enthusiasts**