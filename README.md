# MOBAflow

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

> ⚖️ **Legal Notice:** MOBAflow is an independent open-source project. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details on third-party software, formats, and trademarks (AnyRail, Piko, Roco).

## 📊 Current Status (2025-01-24)

**Build:** ✅ SUCCESS (0 errors)  
**Track Plan Editor:** ✅ FUNCTIONAL  
- ✅ Drag & Drop with ghost track preview
- ✅ Snap-to-connect with visual indicators
- ✅ Grid alignment and zoom controls
- ✅ Validation framework integrated
- 🚧 Sections & Isolators (planned - stubs ready)

**Recent Fixes:**
- Fixed NullReferenceException in keyboard state detection
- Implemented Validate, ZoomFit, ZoomReset button handlers
- Resolved API mismatches between UI and TopologyGraph
- Added null-safety checks for CoreWindow operations

## ✨ Features

- 🚂 **Z21 Direct UDP Control** - Real-time communication with Roco Z21
- 🎯 **Journey Management** - Define train routes with multiple stations
- 🔊 **Text-to-Speech** - Azure Cognitive Services & Windows Speech
- ⚡ **Workflow Automation** - Event-driven action sequences
- 🎨 **MOBAtps Track Plan System** - Visual track layout editor with drag & drop
- 🛤️ **Track Libraries** - Extensible track system support (Piko A-Gleis, more coming)
- 📱 **Multi-Platform** - WinUI (Windows), MAUI (Android), Blazor (Web)

## 🛤️ Track Plan System (MOBAtps)

MOBAflow includes a full-featured **Track Plan System** for designing model railroad layouts:

### Features
- ✅ Drag & Drop track placement from toolbox
- ✅ Snap-to-connect for easy track joining
- ✅ Grid alignment and rotation controls
- ✅ Light & Dark theme support
- ✅ Zoom & Pan navigation
- ✅ Feedback point assignment
- ✅ Validation constraints

### Track Libraries
Track systems are modular - each manufacturer's track system is a separate library:

| Library | Status | Templates |
|---------|--------|-----------|
| **TrackLibrary.PikoA** | ✅ Active | G231, G119, G62, G56, G31, R1-R9, BWL, BWR, K30 |
| TrackLibrary.RocoLine | 🚧 Planned | Coming soon |
| TrackLibrary.Tillig | 🚧 Planned | Coming soon |
| TrackLibrary.Maerklin | 🚧 Planned | Coming soon |

### Architecture
```
TrackPlan (Domain)
  ↑
TrackPlan.Renderer (Geometry/Layout)
  ↑
TrackPlan.Editor (ViewModels/Commands)
  ↑
TrackLibrary.PikoA (Track Templates)
```

## 🛤️ AnyRail Integration

MOBAflow supports **importing track layouts from AnyRail** (user-exported XML files for personal use). This feature enables:
- ✅ Import of user-created AnyRail track plans (XML format)
- ✅ Automatic detection of track geometry and article codes
- ✅ SVG path generation for visualization

**Important:** AnyRail is proprietary software by Carsten Kühling & Paco Ahlqvist. MOBAflow is **independent** and **not affiliated** with AnyRail. The import feature is provided for **interoperability** purposes (fair use) and allows users to import their **own exported track plans**. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for legal details.

## 📋 Quick Links

- 📖 **Documentation:** [`docs/wiki/INDEX.md`](docs/wiki/INDEX.md)
- 🏗️ **Architecture:** [`ARCHITECTURE.md`](ARCHITECTURE.md)
- 📚 **API Documentation:** [`docs/DOXYGEN.md`](docs/DOXYGEN.md) - Generate with Doxygen
- 🧪 **Test Coverage:** [`docs/TEST-COVERAGE.md`](docs/TEST-COVERAGE.md) - Unit test status
- 📝 **Documentation Status:** [`docs/DOCUMENTATION-STATUS.md`](docs/DOCUMENTATION-STATUS.md) - XML doc coverage
- 🎯 **Quality Roadmap:** [`docs/QUALITY-ROADMAP.md`](docs/QUALITY-ROADMAP.md) - 6-week improvement plan
- 📝 **Changelog:** [`CHANGELOG.md`](CHANGELOG.md)
- 📜 **Code of Conduct:** [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md)
- 🤝 **Contributing:** [`CONTRIBUTING.md`](CONTRIBUTING.md)
- 🔒 **Security Policy:** [`SECURITY.md`](SECURITY.md)
- ⚖️ **Third-Party Notices:** [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)
- 🤖 **AI Instructions:** [`.github/copilot-instructions.md`](.github/copilot-instructions.md)

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

## 🔧 Azure Speech Configuration

MOBAflow uses **Azure Cognitive Services Speech** for text-to-speech announcements. You need to configure your own Azure Speech API key.

> 💡 **For Developer Teams:** We provide PowerShell scripts for automated setup! See [🔧 Setup Scripts](#-setup-scripts) below.

### Option A: For Core Team (Azure App Configuration)

**Quick Setup with Scripts:**

```powershell
# 1. Create Azure App Config (once)
.\scripts\setup-azure-appconfig.ps1 -SpeechKey "YOUR-KEY" -SpeechRegion "germanywestcentral"

# 2. Install on all systems
.\scripts\install-appconfig-connection.ps1 -ConnectionString "YOUR-CONNECTION-STRING"

# 3. Restart IDE
```

See [🔧 Setup Scripts](#-setup-scripts) section below for detailed instructions.

**Manual Setup:**

1. **Set Environment Variable:**
   ```bash
   # Windows (PowerShell)
   [System.Environment]::SetEnvironmentVariable('AZURE_APPCONFIG_CONNECTION', 'your-connection-string', 'User')
   
   # Windows (Command Prompt)
   setx AZURE_APPCONFIG_CONNECTION "your-connection-string"
   ```

2. **Restart your IDE** to pick up the new environment variable

3. **Verify:** Speech settings are automatically loaded from Azure App Configuration

### Option B: For Contributors/Developers (User Secrets)

1. **Get Azure Speech Key:**
   - Go to [Azure Portal](https://portal.azure.com)
   - Create a **Cognitive Services** → **Speech** resource
   - Copy your **Key** and **Region**

2. **Configure User Secrets:**
   ```bash
   cd WinUI
   dotnet user-secrets set "Speech:Key" "YOUR-AZURE-SPEECH-KEY"
   dotnet user-secrets set "Speech:Region" "germanywestcentral"
   ```

3. **Verify:** Run the app - speech should work ✅

### Option C: For End Users (Settings UI)

1. **Launch the app**
2. **Navigate to Settings** → **Speech Synthesis**
3. **Enter your Azure Speech Key** in the text box
4. **Select Region:** germanywestcentral (or your Azure region)
5. **Click Save** - settings are stored in `appsettings.json`

> ⚠️ **Note:** The Speech Key field in the Settings UI is password-protected and automatically saved. Never commit `appsettings.json` with your personal key to Git.

### Configuration Priority

The app loads configuration in this order (first found wins):

1. **Azure App Configuration** (if `AZURE_APPCONFIG_CONNECTION` env var is set)
2. **User Secrets** (Development mode only)
3. **Settings UI** → saved to `appsettings.json`
4. **Fallback:** Empty key → Speech features disabled

---

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

## 🎨 Control Libraries

MOBAflow provides **platform-specific control libraries** for building consistent, reusable UI components across different platforms.

### Overview

| Project | Platform | Technology | Purpose |
|---------|----------|------------|---------|
| **WinUI.Controls** | Windows Desktop | WinUI 3 XAML | Controls for WinUI app & plugins |
| **MAUI.Controls** | Android Mobile | .NET MAUI XAML | Controls for MAUI app |
| **SharedUI** | Platform-agnostic | CommunityToolkit.Mvvm | ViewModels (shared) |

### Architecture

```
WinUI.Controls/          ← WinUI 3 XAML Controls (Windows)
    ↓
MAUI.Controls/           ← MAUI XAML Controls (Android)
    ↓
SharedUI/                ← ViewModels (CommunityToolkit.Mvvm)
    ↓
Domain/                  ← Business Models
```

### 🪟 WinUI.Controls (Windows Desktop)

Reusable **WinUI 3 User Controls** for Windows Desktop application and plugins.

#### Technology
- **Framework:** .NET 10 + WinUI 3
- **Platform:** Windows (10.0.17763.0+)
- **UI:** Windows App SDK XAML

#### Usage in WinUI App

```xml
<!-- MainWindow.xaml -->
<Page xmlns:controls="using:Moba.WinUI.Controls">
    <controls:TrainCard 
        TrainName="ICE 1" 
        Speed="120" 
        IsForward="True" />
</Page>
```

#### Usage in Plugins

```csharp
// Plugin ConfigureServices
services.AddTransient<TrainCard>();

// Plugin Page
public sealed partial class MyPluginPage : Page
{
    public MyPluginPage()
    {
        InitializeComponent();
        // TrainCard kann verwendet werden
    }
}
```

#### Guidelines

- **DependencyProperty** für Bindable Properties verwenden
- **x:Bind** bevorzugen (compiled bindings)
- **ThemeResource** für Farben/Styles nutzen
- Konsistent mit WinUI 3 Design System
- Controls sollten mit ViewModels aus `SharedUI` funktionieren

### 📱 MAUI.Controls (Android Mobile)

Reusable **.NET MAUI Controls** for Android mobile application.

#### Technology
- **Framework:** .NET 10 + .NET MAUI
- **Platform:** Android 26+ (Oreo)
- **UI:** MAUI XAML

#### Usage in MAUI App

```xml
<!-- MainPage.xaml -->
<ContentPage xmlns:controls="clr-namespace:Moba.MAUI.Controls;assembly=MAUI.Controls">
    <controls:TrainCard 
        TrainName="ICE 1" 
        Speed="120" 
        IsForward="True" />
</ContentPage>
```

#### Guidelines

- **BindableProperty** für Bindable Properties verwenden
- **RelativeSource** für Binding zu Control Properties
- **AppThemeBinding** für Light/Dark Mode
- Konsistent mit MAUI Design Patterns
- Controls sollten mit ViewModels aus `SharedUI` funktionieren
- Touch-optimiert für Android (mindestens 44x44 dp)

### Platform Differences

| Feature | WinUI.Controls | MAUI.Controls |
|---------|----------------|---------------|
| Bindable Properties | `DependencyProperty` | `BindableProperty` |
| Binding Syntax | `{x:Bind}` | `{Binding}` |
| Base Class | `UserControl` | `ContentView` |
| Icons | `FontIcon` | `FontImageSource` |
| Theming | `ThemeResource` | `AppThemeBinding` |

### Available Controls

- `TrainCard` - Lok-Anzeige mit Geschwindigkeit und Richtung
- *(weitere Controls werden hier ergänzt)*

## 🎵 Audio Library

MOBAflow includes an audio system for workflow actions. Sound files are stored in `Sound/Resources/Sounds/`.

### Directory Structure

```
Sound/Resources/Sounds/
├── Station/          # Station bells, gongs, platform warnings
├── Train/            # Whistles, horns, brake sounds
├── Signals/          # Warning beeps, crossing bells
└── Ambient/          # Background ambience (optional)
```

### Audio File Requirements

| Requirement | Value |
|-------------|-------|
| **Format** | `.wav` (PCM) |
| **Sample Rate** | 44100 Hz or 48000 Hz |
| **Bit Depth** | 16-bit |
| **Channels** | Mono or Stereo |
| **Not Supported** | .mp3, .ogg, .flac |

### Duration Recommendations

| Sound Type | Duration |
|------------|----------|
| Station bells | 2-4 seconds |
| Train whistles | 1-3 seconds |
| Warning signals | 1-2 seconds |
| Gongs/Chimes | 0.5-1 seconds |
| Ambient loops | 10-30 seconds |

### Adding Sounds

1. **Download** from [Freesound.org](https://freesound.org) (filter by CC0 license)
2. **Copy** to appropriate subfolder:
   ```powershell
   copy C:\Downloads\arrival_bell.wav Sound\Resources\Sounds\Station\
   ```
3. **Use in Workflow:**
   - Create Audio Action
   - Set FilePath: `Resources\Sounds\Station\arrival_bell.wav`

### Naming Conventions

| ✅ Good | ❌ Bad |
|---------|--------|
| `arrival_bell.wav` | `sound1.wav` |
| `whistle_short.wav` | `ArrivalBell.wav` |
| `crossing_warning.wav` | `My Sound.wav` |

### Licensing

- ✅ **CC0 (Public Domain)** - No attribution required
- ✅ **CC-BY 4.0** - Attribution required (add to `ATTRIBUTION.md`)
- ❌ **CC-BY-NC** - Avoid (non-commercial only)

See [`Sound/Resources/Sounds/ATTRIBUTION.md`](Sound/Resources/Sounds/ATTRIBUTION.md) for sound attributions.

### Example Workflow

```
Action #1: Audio
  └─ FilePath: Resources\Sounds\Train\whistle_short.wav

Action #2: Announcement
  └─ Message: "{TrainName} erreicht {StationName}"

Action #3: Audio
  └─ FilePath: Resources\Sounds\Station\arrival_bell.wav
```

---

## 🔧 Setup Scripts

MOBAflow provides PowerShell scripts for automated Azure App Configuration setup. These scripts are designed for **developer teams** managing multiple development systems.

> 💡 **For End Users:** You don't need these scripts! Simply enter your Azure Speech Key in the Settings UI (see [Wiki](docs/wiki/AZURE-SPEECH-SETUP.md)).

> 👨‍💻 **For Developer Teams:** Use these scripts to create a centralized configuration store shared across multiple systems.

### Available Scripts

| Script | Purpose | Run Where |
|--------|---------|-----------|
| `scripts/setup-azure-appconfig.ps1` | Create Azure resource | **Once on ONE system** |
| `scripts/install-appconfig-connection.ps1` | Set environment variable | **On ALL systems** |

---

### Quick Setup

**1. Create Azure App Config (once):**

```powershell
cd C:\Repos\ahuelsmann\MOBAflow
.\scripts\setup-azure-appconfig.ps1 -SpeechKey "YOUR-KEY" -SpeechRegion "germanywestcentral"
```

**Output:** Connection String → Copy it!

**2. Install on all systems:**