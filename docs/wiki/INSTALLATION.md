# 🚀 Installation & Setup Guide

**Status:** Source builds are supported; verified binaries are produced by Release Studio
**Last Updated:** July 2026

---

## ⚠️ Current Status: Manual Installation Required

```text
✅ Release Studio ZIP (after signed-tag acceptance)
❌ Automated PowerShell setup scripts (planned)
❌ Docker containers (planned)
✅ Manual dotnet build & run (current)
```

Contributors can compile MOBAflow from source. End users should prefer a signed,
checksum-verified Release Studio ZIP once the first public release is accepted.

---

## 📋 Prerequisites (Requirements)

### Software Requirements

- **.NET SDK:** `10.0.302` as pinned by `global.json`
- **Visual Studio:** a release with the .NET 10 SDK and WinUI 3 workload, or the `dotnet` CLI
- **Windows:** Minimum `10 (1809+)`, recommended `11`
- **Git:** Minimum `2.30+`, recommended `Latest`

### Hardware Requirements

- **Roco Z21:** Digital Command Station (latest firmware)
- **Network:** LAN/WLAN, Z21 in same subnet
- **PC:** Windows 10/11, x64
- **Storage:** Minimum 4GB RAM, 2GB free disk

### Network Setup

```text
🖥️ Windows PC                     📡 Z21 Digital Station
   ↓                                  ↓
   └─────── LAN/WLAN ─────────────────┘
   
   • Z21 IP: e.g. 192.168.1.100
   • PC in same network
   • UDP Port 21105 open (locally, not over Internet!)
```

---

## 🔧 Manual Installation from Source

### Step 1: Install Prerequisites

**Windows:**

```powershell
# Download & install .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Visual Studio 2026 (optional, but recommended)
winget install Microsoft.VisualStudio.2026.Community
```

**macOS/Linux:**

```bash
# Install .NET SDK
curl https://dot.net/v1/dotnet-install.sh | bash

# Or via Homebrew (macOS)
brew install dotnet
```

### Step 2: Clone Repository

```bash
# GitHub is the canonical source repository
git clone https://github.com/ahuelsmann/MOBAflow.git

cd MOBAflow
```

### Step 3: Restore Dependencies

```bash
# Restore the projects you actually want to build
dotnet restore MOBAflow/MOBAflow.csproj
dotnet restore MOBApi/MOBApi.csproj

# Android app
dotnet restore MOBAsmart/MOBAsmart.csproj
```

### Step 4: Compile Project

```bash
# Windows desktop app
dotnet build MOBAflow/MOBAflow.csproj -c Release

# REST API
dotnet build MOBApi/MOBApi.csproj -c Release

# Android app
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release
```

**Output:**

```text
Build succeeded with 0 errors
...
```

### Step 5: Run Applications

#### 🖥️ WinUI Desktop App (Windows)

```bash
dotnet run --project MOBAflow/MOBAflow.csproj --configuration Release
```

#### 🌐 MOBApi (REST API)

```bash
dotnet run --project MOBApi/MOBApi.csproj --configuration Release
```

#### 📱 MAUI Android (Android Phone/Emulator)

```bash
# Emulator must be running or Android device connected
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release

# Or direct run:
dotnet run --project MOBAsmart/MOBAsmart.csproj -f net10.0-android
```

---

## 🔗 Establish Z21 Connection

### 1. Prepare Z21

```text
1. Check Z21 power button → green LED should be on
2. Find Z21 IP address:
   - Roco Mobile App → Settings → check Z21 IP
   - OR: Scan ARP table: arp -a | findstr "roco"
   - OR: Open router admin panel and find Z21
3. Note the IP, e.g. 192.168.1.100
```

### 2. Connect MOBAflow to Z21

**In the application:**

```text
1. Start MOBAflow
2. Open Overview Page
3. "Z21 Connection" widget
4. Enter IP address: 192.168.1.100
5. Click "Connect" button
6. Status should turn green ✅
```

**If connection fails:**

```text
❌ "Connection refused" 
   → Z21 IP wrong or Z21 not in network

❌ "Timeout"
   → Z21 not reachable, check WLAN/LAN

❌ "Connection OK, but no data"
   → UDP port 21105 blocked (check firewall)
```

### 3. Windows Firewall Configuration

If MOBAflow doesn't receive Z21 data:

**Open Windows Defender Firewall:**

```powershell
# PowerShell as Administrator:

# Add inbound rule for MOBAflow
New-NetFirewallRule `
  -DisplayName "MOBAflow Z21 UDP" `
  -Direction Inbound `
  -Action Allow `
  -Protocol UDP `
  -LocalPort 21105
```

---

## 🧪 Run Tests

### Start Unit Tests

```bash
dotnet test Test/Test.csproj --configuration Release
```

**Expected Output:**

```text
Test Run Successful.
Total tests: > 0
Passed: most tests
Failed: 0 on supported platforms
```

### Test Project

```bash
dotnet test Test/Test.csproj
```

---

## 📦 Publishing (Self-Hosted)

### Self-Host (Local/Private)

```bash
# Create MOBAflow desktop build
dotnet publish MOBAflow/MOBAflow.csproj -c Release -o ./publish/MOBAflow

# Create MOBApi release build
dotnet publish MOBApi/MOBApi.csproj -c Release -o ./publish/MOBApi

# Files are now in ./publish/
```

### Docker Support (planned)

```bash
# Not available in v0.1.0 yet
# Planned for v0.2.0
```

---

## 🛠️ Troubleshooting

### Build Errors

#### Error: "The specified framework version 10.0 was not found"

```bash
# Check .NET 10 SDK
dotnet --list-sdks

# If missing: Install .NET 10 SDK
# https://dotnet.microsoft.com/download
```

#### Error: "NuGet restore failed"

```bash
# Clear NuGet cache & restore
dotnet nuget locals all --clear
dotnet restore <project>.csproj
```

#### Error: "WinUI not available on this OS"

```text
WinUI is Windows only!
- For macOS: the WinUI desktop app is not available in this repo
- For Linux: use MOBApi or the cross-platform library/test projects
- For Android: use MOBAsmart
```

### Runtime Errors

#### Error: "Z21 Connection failed"

```text
1. Check Z21 power
2. Test network: ping <z21-ip>
3. Check firewall rule (see above)
4. Restart Z21 (Power OFF → ON)
```

#### Error: "Piper TTS not working"

```text
1. Check piper.exe path
2. Check .onnx model path
3. See: docs/wiki/PIPER-TTS-SETUP.md
```

### Performance Issues

```bash
# Use Release build (faster than Debug)
dotnet run --project MOBAflow/MOBAflow.csproj -c Release

# Use profiler
# Visual Studio → Analyze → Performance Profiler
```

---

## 📞 Further Help

- 📖 **Wiki:** [INDEX.md](INDEX.md)
- 🐛 **Issues:** [GitHub Issues](https://github.com/ahuelsmann/MOBAflow/issues)
- 💬 **Discussions:** [GitHub Discussions](https://github.com/ahuelsmann/MOBAflow/discussions)
- ⚖️ **Liability:** [HARDWARE-DISCLAIMER.md](../HARDWARE-DISCLAIMER.md)

---

## 🚀 Planned Features (Roadmap)

- **0.2.0:** Automated Setup Scripts (PowerShell) — 🚧 Planned
- **0.3.0:** Docker Container Support — 🚧 Planned
- **0.4.0:** Windows Installer (.MSI) — 🚧 Planned
- **1.0.0:** Commercial Plugin Support — 🚧 Planned

---

> Note: This is a preview version (`0.1.0`). Installation and setup
> will be automated in future versions.
