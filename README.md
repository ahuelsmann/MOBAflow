# MOBAflow

**MOBAflow** is an event-driven automation solution for model railroads.
The system enables complex workflow sequences, train control with station
announcements, and real-time feedback monitoring via direct UDP connection to
the Roco Z21 Digital Command Station.

> ⚖️ **Legal Notice:** MOBAflow is an independent open-source project.
> See [THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md) for details on
> third-party software, formats, and trademarks (AnyRail, Piko, Roco).

---

## 📑 Table of Contents

- [✨ Features](#-features)
- [📸 Screenshots](#-screenshots)
- [⚠️ Hardware & Safety](#️-hardware--safety)
- [🚀 Quick Start](#-quick-start)
  - [Prerequisites](#prerequisites)
  - [Clone & Build](#clone--build)
  - [Run Applications](#run-applications)
- [🔐 Trust Model & Signatures](#-trust-model--signatures)
- [🔧 Configuration](#-configuration)
- [🛤️ Track Plan](#️-track-plan)
- [🧩 Project Reference](#-project-reference)
- [🎵 Audio Library](#-audio-library)
- [🎨 Control Libraries](#-control-libraries)
- [📦 Architecture](#-architecture)
- [🔧 Team Setup (Planned)](#-team-setup-planned)
- [📚 Documentation](#-documentation)

---

## ✨ Features

- 🚂 **Z21 Direct UDP Control** – Real-time communication with Roco Z21
- 🎯 **Journey Management** – Define train routes with multiple stations
- 🧭 **Flexible Layout** – Toggle City and Workflow libraries to maximize workspace
- 🔊 **Text-to-Speech** – Piper TTS & Windows Speech API
- ⚡ **Workflow Automation** – Event-driven action sequences
- 🎨 **Visual Track Plan** – Drag & drop track editor with snap-to-connect
- 🟢 **Win2D GPU Rendering** – High-performance track visualization
- 🛤️ **Track Libraries** – Extensible support (Piko A-Gleis, Roco Line,
  Tillig, Märklin)
- 📱 **Multi-Host** – MOBAflow (Windows), MOBAsmart (Android), MOBApi (REST)
- 🟢 **Status Monitoring** – Real-time startup progress with log streaming

---

## 📸 Screenshots

A visual tour of MOBAflow's main features. Click any image to view it in full
resolution.

<!--
  Add new screenshots to docs/images/ (PNG, optimized via tinypng.com,
  target width ~1280 px). Use descriptive, kebab-case filenames and keep
  alt texts meaningful for accessibility.
-->

### 🪟 MOBAflow Desktop (WinUI)

<p align="center">
  <a href="docs/images/mobaflow-overview.png">
    <img src="docs/images/mobaflow-overview.png" alt="MOBAflow main window with live Z21 status and journey overview" width="800" />
  </a>
  <br />
  <em>Main window – live Z21 status, active journeys and feedback monitoring.</em>
</p>

### 🎛️ Train Control

<p align="center">
  <a href="docs/images/train-control.png">
    <img src="docs/images/train-control.png" alt="Train Control with locomotive presets, timetable, speedometer, ammeter and F0-F31 functions" width="800" />
  </a>
  <br />
  <em>Locomotive presets, live timetable, speed &amp; current dials and F0–F31 function keys.</em>
</p>

### 🛤️ Visual Track Plan Editor

<p align="center">
  <a href="docs/images/trackplan-editor.png">
    <img src="docs/images/trackplan-editor.png" alt="Drag and drop track plan editor with Piko A-Gleis library" width="800" />
  </a>
  <br />
  <em>Drag &amp; drop track editor with snap-to-connect and Win2D rendering.</em>
</p>

### 🎯 Journey Management

<p align="center">
  <a href="docs/images/journey-management.png">
    <img src="docs/images/journey-management.png" alt="Journey editor with stations, city library, workflow library and journey properties" width="800" />
  </a>
  <br />
  <em>Compose journeys from stations, cities and workflows with live runtime state.</em>
</p>

### 🖥️ ESP32 Remote Display

<p align="center">
  <a href="docs/images/display-page.png">
    <img src="docs/images/display-page.png" alt="Display page streaming track number and clock via UDP to an ESP32 remote display" width="800" />
  </a>
  <br />
  <em>Stream track numbers and live clock via UDP to an ESP32-based remote display.</em>
</p>

The Display page also includes an interactive 5x5 LED matrix editor:

- **Color palette:** Pick the active LED color with the WinUI color picker.
- **Left click / tap:** Paint a matrix cell with the selected color.
- **Right click:** Clear a matrix cell back to the off state.
- **MVVM interaction:** Matrix input is routed through ViewModel commands, so the cell-state logic is covered by platform-neutral unit tests.

---

> 📚 **Need Help?** Check out our comprehensive [**Wiki Documentation**](docs/wiki/INDEX.md)
>
> - [WinUI User Guide](docs/wiki/MOBAFLOW-USER-GUIDE.md) – Learn how to use MOBAflow
> - [Piper TTS Setup](docs/wiki/PIPER-TTS-SETUP.md) – Configure local text-to-speech
> - [Installation Guide](docs/wiki/INSTALLATION.md) – Set up MOBAflow,
>   MOBApi and MOBAsmart

---

## ⚠️ Hardware & Safety

MOBAflow controls model train layouts via UDP communication with the
**Roco Z21 Digital Command Station**.

### ⚠️ Important Safety Information

> **Before using MOBAflow, please read:**
> 📖 [`HARDWARE-DISCLAIMER.md`](docs/HARDWARE-DISCLAIMER.md)
>
> This document covers:
>
> - ✅ Safety requirements and prerequisites
> - ✅ Network configuration
> - ✅ Liability & disclaimer
> - ✅ Emergency procedures

**Current Status:** ℹ️ *Automated setup scripts are planned for v0.2.0.
Hardware setup, device pairing, and layout integration are still manual.*

---

## 🚀 Quick Start

### Prerequisites

- ✅ [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) matching [`global.json`](global.json)
- ✅ [Visual Studio 2026](https://visualstudio.microsoft.com/) (recommended)
  or VS Code
- ✅ Roco Z21 Digital Command Station (for Z21 connectivity)

### Clone & Build

```bash
git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj
```

**Optional (full solution):** `dotnet build Moba.slnx` requires Windows with WinUI and
Android MAUI workloads. Prefer per-project builds when solution restore fails.

**Fast local WinUI compile check (Windows):**

```bash
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Use the fast command for everyday UI edit/build cycles. It skips the REST API build
dependency and post-build copy; use the normal build or run command when validating
the full desktop app startup. See [`docs/BUILD-PERFORMANCE.md`](docs/BUILD-PERFORMANCE.md)
for build timing and binary log guidance.

**Cross-platform subset (library and backend projects only):**

```bash
dotnet build Backend/Backend.csproj
dotnet build Common/Common.csproj
dotnet build Domain/Domain.csproj
dotnet build SharedUI/SharedUI.csproj
dotnet test Test/Test.csproj
```

> Note: On non-Windows systems, the WinUI (`MOBAflow.csproj`) and MAUI (`MOBAsmart.csproj`)
> projects are not buildable due to platform-specific dependencies.
> Build individual cross-platform `.csproj` files instead of the full solution.
> Some `System.Speech` tests are Windows-only and will skip on Linux.

### Run Applications

**🪟 MOBAflow (Windows Desktop):**

```bash
dotnet run --project MOBAflow/MOBAflow.csproj
```

**🌐 MOBApi (REST API, Port 5001):**

```bash
dotnet run --project MOBApi/MOBApi.csproj
```

MOBApi listens on **port 5001** (all interfaces). It provides the REST API for
the MOBAflow Overview status and for MOBAsmart (client list, health).

You can **start MOBApi in two ways**:

1. **Standalone** – run the command above.
2. **Together with MOBAflow** – enable "Auto-start REST API with MOBAflow"
   in MOBAflow Settings so MOBAflow starts the MOBApi process automatically.

MOBAsmart discovers the server via UDP multicast; ensure PC and phone are on
the same network.

**📱 MOBAsmart (Android):**

```bash
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android
```

**🧪 Run Tests:**

```bash
dotnet test Test/Test.csproj
```

---

## 🔐 Trust Model & Signatures

Official MOBAflow releases are identified by **signed Git tags** in this repository.

- Release versions are tagged as `X.Y.Z` (e.g. `0.1.0`).
- Starting with version `0.1.0`, maintainers sign these tags with their GPG
  keys so you can verify that a given version really comes from us and was not
  modified.

### How to use signed versions as a user

Typical workflow for installing a specific version:

1. **Select a version**: Pick a tag from the GitHub *Tags* / *Releases* list
   (e.g. `0.1.0`).
2. **Fetch tags & verify**:

   ```bash
   git fetch origin --tags
   git tag -v 0.1.0
   ```

   Only continue if GPG reports a **valid signature** from a maintainer key
   listed in `docs/legal/MAINTAINERS.md`.
3. **Check out the tag**:

   ```bash
   git checkout 0.1.0
   ```

4. **Build & run** using the commands from the [Quick Start](#-quick-start) section.

### Verifying a Release Tag

```bash
git fetch origin --tags
git tag -v 1.2.3
```

- Only trust tags whose signature matches one of the maintainer keys
  documented in `docs/legal/MAINTAINERS.md` (e.g. key ID
  `7DAD81238FEE2F49`).
- If verification fails, do **not** use that release and contact the maintainers.

### Maintainer Keys

The current list of GPG keys and fingerprints used for signing release tags
is maintained in:

- `docs/legal/MAINTAINERS.md`

---

## 🔧 Configuration

MOBAflow uses **local text-to-speech** for announcements. The recommended
open-source engine is Piper TTS; Windows Speech API remains available as an
offline fallback.

### Piper TTS Setup

1. Install the recommended Piper distribution from [`OHF-Voice/piper1-gpl`](https://github.com/OHF-Voice/piper1-gpl): `py -m pip install piper-tts`.
2. Use the generated `piper.exe` from your Python environment, for example `.venv\Scripts\piper.exe`.
3. Download a compatible German `.onnx` voice model from <https://huggingface.co/rhasspy/piper-voices>.
4. Launch MOBAflow and open **Settings → Speech Synthesis**.
5. Select **Piper TTS**.
6. Configure the path to `piper.exe`, the `.onnx` model and optionally the model `.json` file.
7. Click **Test Speech**.

### Windows Speech Setup

**1. Launch** MOBAflow  
**2. Navigate:** Settings → Speech Synthesis  
**3. Select** System Speech (Windows SAPI)  
**4. Click** Test Speech

---

### Configuration Priority

The app loads settings in this order (first found wins):

1. ☁️ **Azure App Configuration** (if `AZURE_APPCONFIG_CONNECTION` env var exists)
2. 🔐 **User Secrets** (Development mode only)
3. ⚙️ **Settings UI** – committed defaults in `MOBAflow/appsettings.json`; local overrides in
   `appsettings.Development.json` (gitignored; use `appsettings.Development.template.json` as a starting point)
4. 🚫 **Fallback:** Speech features disabled

---

## 🛤️ Track Plan

Design your model railroad layout with MOBAflow's visual track planning system.

### ✨ Track Plan Features

- ✅ **Drag & Drop** – Place tracks from toolbox
- ✅ **Snap-to-Connect** – Automatic track joining
- ✅ **Grid Alignment** – Rotation & positioning controls
- ✅ **Theming** – Light & Dark mode support
- ✅ **Navigation** – Zoom & Pan
- ✅ **Feedback Points** – Assign detection sensors
- ✅ **Validation** – Real-time constraint checking
- ✅ **Signal Control** – Requires active Z21 connection
- ✅ **Win2D Rendering** – GPU-accelerated graphics (Phase 1)

### 🛤️ Supported Track Systems

| Library | Status | Description |
| --------- | -------- | ------------- |
| **TrackLibrary.PikoA** | ✅ Active | Piko A-Gleis |
| TrackLibrary.Base | ✅ Active | Base classes and geometry primitives |
| TrackPlan.Renderer | ✅ Active | Win2D-based track plan rendering |
| TrackLibrary.RocoLine | 🚧 Planned | Roco Line |
| TrackLibrary.Tillig | 🚧 Planned | Tillig |
| TrackLibrary.Maerklin | 🚧 Planned | Märklin |

---

## 🎵 Audio Library

Play sound effects in workflows (station bells, train whistles, crossing signals).

### 📂 Directory Structure

```text
Sound/Resources/Sounds/
├── Station/          # Station bells, gongs, platform warnings
├── Train/            # Whistles, horns, brake sounds
├── Signals/          # Warning beeps, crossing bells
└── Ambient/          # Background ambience (optional)
```

### 📋 Requirements

| Property | Value |
| ---------- | ------- |
| **Format** | `.wav` (PCM only) |
| **Sample Rate** | 44100 Hz or 48000 Hz |
| **Bit Depth** | 16-bit |
| **Channels** | Mono or Stereo |
| **Not Supported** | ❌ .mp3, .ogg, .flac |

### 🎯 Naming Conventions

| ✅ Good | ❌ Bad |
| --------- | -------- |
| `arrival_bell.wav` | `sound1.wav` |
| `whistle_short.wav` | `ArrivalBell.wav` |
| `crossing_warning.wav` | `My Sound.wav` |

### 📥 Adding Sounds

1. **Download** from [Freesound.org](https://freesound.org) (CC0 license recommended)
2. **Copy** to appropriate subfolder
3. **Use in Workflow:** Create Audio Action → Set FilePath

### ⚖️ Licensing

- ✅ **CC0 (Public Domain)** – No attribution required
- ✅ **CC-BY 4.0** – Attribution required (add to `ATTRIBUTION.md`)
- ❌ **CC-BY-NC** – Avoid (non-commercial restriction)

📖 **Attribution File:** [`Sound/Resources/Sounds/ATTRIBUTION.md`](Sound/Resources/Sounds/ATTRIBUTION.md)

---

## 🎨 Control Libraries

Platform-specific UI control libraries for consistent, reusable components.

### 🏗️ Architecture

```text
MOBAflow/Controls/       ← WinUI 3 XAML controls inside the desktop app
MOBAsmart/Controls/      ← MAUI XAML controls inside the Android app
    ↓
SharedUI/                ← ViewModels (Platform-agnostic)
    ↓
Domain/                  ← Business Models
```

### 📦 Available Libraries

| Project | Platform | Technology | Target |
| --------- | ---------- | ------------ | -------- |
| **MOBAflow/Controls** | Windows | WinUI 3 XAML | Desktop app control set |
| **MOBAsmart/Controls** | Android | .NET MAUI XAML | Mobile app control set |
| **SharedUI** | Cross-platform | CommunityToolkit.Mvvm | Shared ViewModels |

### 🪟 Windows Controls in MOBAflow

```xml
<Page xmlns:controls="using:Moba.WinUI.Controls">
    <controls:TrainCard 
        TrainName="ICE 1" 
        Speed="120" 
        IsForward="True" />
</Page>
```

**Guidelines:**

- Use `DependencyProperty` for bindable properties
- Prefer `x:Bind` (compiled bindings)
- Use `ThemeResource` for colors/styles
- Follow Fluent Design System

### 📱 MOBAsmart Controls (Android)

```xml
<ContentPage xmlns:controls="clr-namespace:Moba.MAUI.Controls">
    <controls:SpeedGaugeView />
</ContentPage>
```

**Guidelines:**

- Use `BindableProperty` for bindable properties
- Use `AppThemeBinding` for Light/Dark mode
- Touch-optimized (minimum 44x44 dp)
- Follow MAUI design patterns

### ⚖️ Platform Differences

| Feature | MOBAflow/Controls | MOBAsmart/Controls |
| --------- | ---------------- | ------------------ |
| Bindable Properties | `DependencyProperty` | `BindableProperty` |
| Binding Syntax | `{x:Bind}` | `{Binding}` |
| Base Class | `UserControl` | `ContentView` |
| Icons | `FontIcon` | `FontImageSource` |
| Theming | `ThemeResource` | `AppThemeBinding` |

---

## 📦 Architecture

MOBAflow follows **Clean Architecture** principles with strict layer separation.

### 🏗️ Layer Structure

```text
┌─────────────────────────────────────┐
│  MOBAflow / MOBAsmart / MOBApi      │  ← Platform UI & API
├─────────────────────────────────────┤
│  SharedUI                           │  ← MVVM Layer
├─────────────────────────────────────┤
│  Backend (Services, Logic)          │  ← Business Logic
├─────────────────────────────────────┤
│  Common (Configuration, Events)     │  ← Shared Infrastructure
├─────────────────────────────────────┤
│  Domain (Models, POCOs)             │  ← Core Entities
└─────────────────────────────────────┘
```

### Runtime Boundary (Current Status)

Shared ViewModels talk to the in-process runtime only; there is no separate UI client interface:

```text
MainWindowViewModel / TrainControlViewModel / MauiViewModel
  ↓
IMobaRuntime  (MobaRuntimeService)
  ↓
IZ21 / JourneyManager / WorkflowService
```

Current scope:

- `MainWindowViewModel`, `TrainControlViewModel`, and `MauiViewModel` inject
  `IMobaRuntime` (singleton `MobaRuntimeService`) instead of using `IZ21` or
  `JourneyManager` directly for commands and snapshots
- The runtime publishes `MobaRuntimeSnapshot` (and related events such as
  traffic and feedback) back to the shell
- Project activation is performed inside `MobaRuntimeService.ActivateProjectAsync`,
  which owns `ActiveProjectContext` and a `JourneyManager` per active project
- The active runtime still uses the live `Project` instance from the loaded
  `Solution`; a dedicated runtime copy remains a possible future step
- Shared master data (`data.json`) is held in **`MasterDataStore`** (Backend DI);
  WinUI services expose cities and locomotives to the shell

### 🛠️ Technology Stack

| Layer | Technology |
| ------- | ------------ |
| **Framework** | .NET 10 |
| **UI** | WinUI 3 (MOBAflow), .NET MAUI (MOBAsmart) |
| **API** | ASP.NET Core REST + SignalR (MOBApi) |
| **Graphics** | Microsoft.Graphics.Win2D |
| **Display rendering** | SkiaSharp RGB565 rendering, UDP transport to ESP32-S3 displays |
| **MVVM** | CommunityToolkit.Mvvm |
| **Logging** | Serilog (Async File Sink + In-Memory + Environment/Process/Thread Enrichers) |
| **Speech** | Piper TTS, Windows Speech |
| **Networking** | Direct UDP (Z21 Protocol) |
| **Testing** | NUnit |
| **Communication** | EventBus with UiThreadEventBusDecorator |

### 📄 Solution File Format

MOBAflow uses **System.Text.Json** with schema validation.

#### Schema Version

```json
{
  "name": "My Model Railroad",
  "schemaVersion": 1,
  "projects": [...]
}
```

**Current Schema Version:** `1`

#### Validation Rules

- ✅ **JSON Structure** – Valid syntax
- ✅ **Required Properties** – `name`, `projects`
- ✅ **Schema Version** – Compatibility check
- ✅ **Project Integrity** – Valid structure

### 📊 Logging Infrastructure

**Serilog Configuration:**

- 📁 **File Logs:** `bin/Debug/logs/mobaflow-YYYYMMDD.log` (async, rolling, 7-day retention)
- 💾 **In-Memory Sink:** Real-time log streaming to MonitorPage UI
- 🔍 **Structured Logging:** Searchable properties with context enrichment
- 🏷️ **Enrichers:** MachineName, ProcessId, ProcessName, ThreadId
- 📊 **Log Levels:** Debug (Moba), Warning (Microsoft)

**Example:**

```csharp
_logger.LogInformation(
    "Feedback received: InPort={InPort}, Value={Value}",
    inPort,
    value);
```

**Sample Output:**

```text
[14:32:15.123 INF] [MY-PC] [12345:MOBAflow.exe] [12] [Moba.Z21] Feedback received: InPort=1, Value=255
```

📖 **Details:** See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)

---

## 🧩 Project Reference

For a developer-oriented, repository-wide reference covering structure, current
models, services, pages, workflow execution, display rendering, ESP32-S3
communication, configuration, build/deploy processes, and known documentation
gaps, see:

- [`docs/PROJECT-REFERENCE.md`](docs/PROJECT-REFERENCE.md)

This document is the preferred onboarding companion for new contributors after
reading this README.

---

## 🔧 Team Setup (Planned)

> **Planned for v0.2.0:** PowerShell setup scripts for Azure App Configuration
> (`scripts/setup-azure-appconfig.ps1`, `scripts/install-appconfig-connection.ps1`)
> are not yet in the repository. Until then, configure MOBAflow via the
> [Settings UI](#-configuration) or local `appsettings.Development.json`
> (gitignored; see [Configuration](#-configuration)).

---

## 📚 Documentation

### 📖 Core Documentation

**Location:** `docs/`

| Document | Description |
| ---------- | ------------- |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Architecture & design patterns |
| [PROJECT-REFERENCE.md](docs/PROJECT-REFERENCE.md) | Repository-wide technical reference and onboarding map |
| [CHANGELOG.md](CHANGELOG.md) | Version history & release notes |
| [SECURITY.md](docs/SECURITY.md) | Security policy & reporting |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community conduct guidelines |
| [JSON-VALIDATION.md](docs/JSON-VALIDATION.md) | Solution JSON validation |
| [MINVER-SETUP.md](docs/MINVER-SETUP.md) | MinVer versioning setup |
| [HARDWARE-DISCLAIMER.md](docs/HARDWARE-DISCLAIMER.md) | Hardware safety & liability |
| [THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md) | Third-party licenses |
| [CLAUDE.md](docs/CLAUDE.md) | AI assistant instructions |
| [CLA.md](docs/legal/CLA.md) | Contributor License Agreement (CLA) |

Azure DevOps MCP integration is configured in [`.mcp.json`](.mcp.json) at the repository root.

### 📚 Wiki (User & Feature Guides)

**Location:** `docs/wiki/`

| Guide | Description |
| ------- | ------------- |
| [INDEX.md](docs/wiki/INDEX.md) | Wiki index & platform overview |
| [INSTALLATION.md](docs/wiki/INSTALLATION.md) | Installation & setup guide |
| [MOBAFLOW-USER-GUIDE.md](docs/wiki/MOBAFLOW-USER-GUIDE.md) | WinUI desktop app user guide |
| [MOBASMART-USER-GUIDE.md](docs/wiki/MOBASMART-USER-GUIDE.md) | MOBAsmart Android app user guide |
| [MOBASMART-WIKI.md](docs/wiki/MOBASMART-WIKI.md) | Detailed MOBAsmart documentation |
| [PIPER-TTS-SETUP.md](docs/wiki/PIPER-TTS-SETUP.md) | Piper TTS setup |
| [QUICK-START-TRACK-STATISTICS.md](docs/wiki/QUICK-START-TRACK-STATISTICS.md) | Track statistics quick start |
| [VIESSMANN-SIGNAL-MAPPING.md](docs/wiki/VIESSMANN-SIGNAL-MAPPING.md) | Viessmann signal mapping |
| [MOBATPS.md](docs/wiki/MOBATPS.md) | MOBAtps track plan system architecture |

---

## 📄 License

This project is licensed under the **MIT License**.
See [LICENSE](LICENSE) for details.

---

Made with ❤️ and ai for model railroad enthusiasts.
