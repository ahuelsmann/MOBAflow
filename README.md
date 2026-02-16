# MOBAflow

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

> ⚖️ **Legal Notice:** MOBAflow is an independent open-source project. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details on third-party software, formats, and trademarks (AnyRail, Piko, Roco).

## 📊 Documentation Index
[ARCHITECTURE.md](docs/ARCHITECTURE.md)
[CHANGELOG.md](docs/CHANGELOG.md)
[CLAUDE.md](docs/CLAUDE.md)
[HARDWARE-DISCLAIMER.md](docs/HARDWARE-DISCLAIMER.md)
[JSON-VALIDATION.md](docs/JSON-VALIDATION.md)
[MINVER-SETUP.md](docs/MINVER-SETUP.md)
[SECURITY.md](docs/SECURITY.md)
[THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md)

## ✨ Features

- 🚂 **Z21 Direct UDP Control** - Real-time communication with Roco Z21
- 🎯 **Journey Management** - Define train routes with multiple stations
- 🧭 **Journeys Page Layout** - Toggle City and Workflow libraries to free space for properties
- 🔊 **Text-to-Speech** - Azure Cognitive Services & Windows Speech
- ⚡ **Workflow Automation** - Event-driven action sequences
- 🎨 **Track Plan** - Visual track layout editor with drag & drop
- 
- 🛤️ **Track Libraries** - Extensible track system support (Piko A-Gleis, more coming)
- 📱 **Multi-Platform** - WinUI (Windows), MAUI (Android), Blazor (Web)
- 🟢 **Startup Status Indicator** - WinUI status bar shows deferred initialization progress with logs

## ⚠️ Hardware & Safety

MOBAflow controls model train layouts via UDP communication with the **Roco Z21 Digital Command Station**. 

**IMPORTANT:** Please read [`HARDWARE-DISCLAIMER.md`](HARDWARE-DISCLAIMER.md) for:
- ✅ Safety requirements and prerequisites
- ✅ Network configuration
- ✅ Liability & disclaimer information
- ✅ Emergency procedures

**Current Status:** ℹ️ *Setup automation scripts are not yet available. Manual installation required.*

## 🛤️ Track Plan

MOBAflow includes a track plan system for designing model railroad layouts:

### Features
- ✅ Drag & Drop track placement from toolbox
- ✅ Snap-to-connect for easy track joining
- ✅ Grid alignment and rotation controls
- ✅ Light & Dark theme support
- ✅ Zoom & Pan navigation
- ✅ Feedback point assignment
- ✅ Validation constraints
- ✅ Signal switching requires an active Z21 connection
- ✅ **Win2D GPU-Rendering** – Gleissegmente werden via Microsoft.Graphics.Win2D gezeichnet (Phase 1)

### Track Libraries
Track systems are modular - each manufacturer's track system is a separate library:

| Library | Status | Templates |
|---------|--------|-----------|
| **TrackLibrary.PikoA** | ✅ Active |
| TrackLibrary.RocoLine | 🚧 Planned |
| TrackLibrary.Tillig | 🚧 Planned |
| TrackLibrary.Maerklin | 🚧 Planned |

### Quick Build from Source

**Prerequisites:**
- .NET 10 SDK
- Visual Studio 2026 (recommended)
- Roco Z21 (for Z21 connectivity)

**Clone & Build:**
```bash
git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow
dotnet restore
dotnet build
```

**Run Applications:**

WinUI (Windows Desktop):
```bash
dotnet run --project WinUI
```

WebApp (Blazor/Web):
```bash
dotnet run --project WebApp
```

MAUI (Android):
```bash
dotnet build MAUI -f net10.0-android
```

**Run Tests:**
```bash
dotnet test
```

## 🔧 Configuration

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
- **Graphics:** Microsoft.Graphics.Win2D (Track Plan GPU-Rendering)
- **MVVM:** CommunityToolkit.Mvvm
- **Logging:** Serilog (File + In-Memory Sink for real-time UI)
- **Speech:** Azure Cognitive Services, Windows Speech API
- **Networking:** Direct UDP to Z21 (no external dependencies)
- **Testing:** NUnit

### Solution File Format & Validation

MOBAflow solution files (`.json`) use **System.Text.Json** with schema validation to ensure data integrity.

#### Schema Version

Solution files include a `schemaVersion` property to detect incompatible formats:

```json
{
  "name": "My Model Railroad",
  "schemaVersion": 1,
  "projects": [...]
}
```

**Current Schema Version:** `1`

#### Validation

When loading a solution file, MOBAflow validates:

✅ **JSON Structure** - Valid JSON syntax  
✅ **Required Properties** - `name` and `projects` must be present  
✅ **Schema Version** - Detects incompatible file versions  
✅ **Project Integrity** - All projects have valid structure  

**Invalid files are rejected with clear error messages:**

```
❌ Invalid solution file: Missing required property: 'projects'
❌ Invalid solution file: Incompatible schema version. Expected 1, found 999.
❌ Failed to parse JSON: Unexpected character at position 42.
```

#### Migration

When the schema version changes in future releases:
- Old files (v1) will be auto-migrated or rejected with upgrade instructions
- `Solution.CurrentSchemaVersion` constant tracks the latest version
- Breaking changes increment the version number

See `Common/Validation/JsonValidationService.cs` for implementation details.

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
- **Platform:** Windows (10.0.22621.0+)
- **UI:** Windows App SDK XAML
- **Graphics:** Win2D (Track Plan Canvas)

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


#### Guidelines

- **DependencyProperty** für Bindable Properties verwenden
- **x:Bind** bevorzugen (compiled bindings)
- **ThemeResource** für Farben/Styles nutzen
- Konsistent mit Fluent Design System
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
```powershell
# On Developer System 1, 2, 3, ...
.\scripts\install-appconfig-connection.ps1 -ConnectionString "YOUR-CONNECTION-STRING"
```

**3. Restart IDE:**
```powershell
# Close and reopen Visual Studio / VS Code
```

**4. Verify:**
```csharp
// Speech settings are automatically loaded from Azure App Configuration
// No appsettings.json or user-secrets needed!
```

---

### Script Details

#### setup-azure-appconfig.ps1

**Purpose:** Creates Azure App Configuration resource and stores Speech API key.

**Parameters:**
- `-SpeechKey` (required): Your Azure Speech API Key
- `-SpeechRegion` (required): Azure region (e.g., `germanywestcentral`)
- `-ResourceGroupName` (optional): Default `MOBAflow-RG`
- `-ConfigStoreName` (optional): Default `mobaflow-config`
- `-Location` (optional): Default `germanywestcentral`

**What it does:**
1. Creates Resource Group (if not exists)
2. Creates App Configuration store
3. Stores Speech:Key and Speech:Region as key-values
4. Returns Connection String

**Example:**
```powershell
.\scripts\setup-azure-appconfig.ps1 `
    -SpeechKey "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6" `
    -SpeechRegion "germanywestcentral" `
    -ResourceGroupName "MyTeam-RG" `
    -ConfigStoreName "myteam-mobaflow-config"
```

**Requirements:**
- Azure CLI installed (`az --version`)
- Logged in to Azure (`az login`)
- Subscription selected (`az account set`)

---

#### install-appconfig-connection.ps1

**Purpose:** Sets environment variable on local system.

**Parameters:**
- `-ConnectionString` (required): From `setup-azure-appconfig.ps1` output

**What it does:**
1. Sets `AZURE_APPCONFIG_CONNECTION` user environment variable
2. Validates format
3. Instructions to restart IDE

**Example:**
```powershell
.\scripts\install-appconfig-connection.ps1 `
    -ConnectionString "Endpoint=https://mobaflow-config.azconfig.io;Id=xxx;Secret=xxx"
```

**Requirements:**
- Run as normal user (not Admin)
- Restart IDE after running

---

### Configuration Priority (with Scripts)

When Azure App Configuration is set up:

1. ✅ **Azure App Configuration** (if `AZURE_APPCONFIG_CONNECTION` env var exists)
2. ⏭️ User Secrets (skipped)
3. ⏭️ Settings UI (skipped)
4. ⏭️ Fallback (skipped)

**Benefits:**
- ✅ Centralized configuration for entire team
- ✅ No `appsettings.json` commits
- ✅ No user-secrets management
- ✅ Easy key rotation (update once in Azure)
- ✅ Same config on CI/CD pipelines

---

### Troubleshooting

---