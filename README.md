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

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for detailed architecture documentation.

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
- **.NET Foundation** for the amazing .NET ecosystem
- **CommunityToolkit** contributors for MVVM helpers
- **GitHub Copilot** for AI-assisted development and code quality improvements
- **All contributors** who help improve MOBAflow

---

**Made with ❤️ for model railroad enthusiasts**