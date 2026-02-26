# 🚀 Installation & Setup Guide

**Status:** ℹ️ Automated setup scripts are planned for future releases  
**Last Updated:** February 2026

---

## ⚠️ Current Status: Manual Installation Required

```
❌ Windows Setup.exe (planned)
❌ Automated PowerShell setup scripts (planned)
❌ Docker containers (planned)
✅ Manual dotnet build & run (current)
```

**Currently you must compile MOBAflow manually from source code.**

---

## 📋 Prerequisites (Requirements)

### Software Requirements

| Requirement | Minimum | Recommended |
|------------|---------|-------------|
| .NET SDK   | 10.0    | 10.0+       |
| Visual Studio | 2022 v17.10 | 2026 |
| Windows    | 10 (1809+) | 11 |
| Git        | 2.30+  | Latest |

### Hardware Requirements

| Component | Requirement |
|-----------|------------|
| **Roco Z21** | Digital Command Station (latest firmware) |
| **Network** | LAN/WLAN, Z21 in same subnet |
| **PC** | Windows 10/11, x64 |
| **Storage** | Minimum 4GB RAM, 2GB free disk |

### Network Setup

```
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
# GitHub Public Repo (after migration)
git clone https://github.com/ahuelsmann/MOBAflow.git

# OR currently: Azure DevOps
git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow

cd MOBAflow
```

### Step 3: Restore Dependencies

```bash
dotnet restore
```

### Step 4: Compile Project

```bash
# Release build (optimized)
dotnet build -c Release

# Or Debug build (with debugging info)
dotnet build -c Debug
```

**Output:**
```
Build succeeded with 0 errors
...
```

### Step 5: Run Applications

#### 🖥️ WinUI Desktop App (Windows)
```bash
dotnet run --project WinUI --configuration Release
```

#### 🌐 Web App (Blazor)
```bash
dotnet run --project WebApp --configuration Release
# Open http://localhost:5000
```

#### 📱 MAUI Android (Android Phone/Emulator)
```bash
# Emulator must be running or Android device connected
dotnet build MAUI -f net10.0-android -c Release

# Or direct run:
dotnet run --project MAUI -f net10.0-android
```

---

## 🔗 Establish Z21 Connection

### 1. Prepare Z21

```
1. Check Z21 power button → green LED should be on
2. Find Z21 IP address:
   - Roco Mobile App → Settings → check Z21 IP
   - OR: Scan ARP table: arp -a | findstr "roco"
   - OR: Open router admin panel and find Z21
3. Note the IP, e.g. 192.168.1.100
```

### 2. Connect MOBAflow to Z21

**In the application:**
```
1. Start MOBAflow
2. Open Overview Page
3. "Z21 Connection" widget
4. Enter IP address: 192.168.1.100
5. Click "Connect" button
6. Status should turn green ✅
```

**If connection fails:**
```
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
dotnet test --configuration Release
```

**Expected Output:**
```
Test Run Successful.
Total tests: 150
Passed: 150
Failed: 0
```

### Test Specific Projects

```bash
# Only Domain Tests
dotnet test Test/Domain.Tests.csproj

# Only Backend Tests
dotnet test Backend/Backend.Tests.csproj
```

---

## 📦 Publishing (Self-Hosted)

### Self-Host (Local/Private)

```bash
# Create WinUI release package
dotnet publish WinUI -c Release -o ./publish/WinUI

# Create WebApp release package
dotnet publish WebApp -c Release -o ./publish/WebApp

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

**Error: "The specified framework version 10.0 was not found"**
```bash
# Check .NET 10 SDK
dotnet --list-sdks

# If missing: Install .NET 10 SDK
# https://dotnet.microsoft.com/download
```

**Error: "NuGet restore failed"**
```bash
# Clear NuGet cache & restore
dotnet nuget locals all --clear
dotnet restore
```

**Error: "WinUI not available on this OS"**
```
WinUI is Windows only!
- For macOS: Use WebApp (Blazor)
- For Linux: Use WebApp (Blazor)
- For Android: Use MAUI App
```

### Runtime Errors

**Error: "Z21 Connection Failed"**
```
1. Check Z21 power
2. Test network: ping <z21-ip>
3. Check firewall rule (see above)
4. Restart Z21 (Power OFF → ON)
```

**Error: "Azure Speech not working"**
```
1. Check Azure Speech API Key
2. Region correctly set?
3. See: docs/wiki/AZURE-SPEECH-SETUP.md
```

### Performance Issues

```bash
# Use Release build (faster than Debug)
dotnet run --project WinUI -c Release

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

| Version | Feature | Status |
|---------|---------|--------|
| 0.2.0 | Automated Setup Scripts (PowerShell) | 🚧 Planned |
| 0.3.0 | Docker Container Support | 🚧 Planned |
| 0.4.0 | Windows Installer (.MSI) | 🚧 Planned |
| 1.0.0 | Commercial Plugin Support | 🚧 Planned |

---

*Note: This is a preview version (0.1.0). Installation and setup will be automated in future versions.*
