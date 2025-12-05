# MOBAflow

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android%20%7C%20Web-brightgreen)](#)
[![Build Status](https://dev.azure.com/ahuelsmann/MOBAflow/_apis/build/status/MOBAflow?branchName=main)](https://dev.azure.com/ahuelsmann/MOBAflow/_build)

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

## ✨ Features

- 🚂 **Z21 Direct UDP Control** - Real-time communication with Roco Z21
- 🎯 **Journey Management** - Define train routes with multiple stations
- 🔊 **Text-to-Speech** - Azure Cognitive Services & Windows Speech
- ⚡ **Workflow Automation** - Event-driven action sequences
- 🎨 **Multi-Platform** - WinUI (Windows), MAUI (Android), Blazor (Web)

## 📋 Quick Links

- 📖 **Documentation:** [`docs/README.md`](docs/README.md)
- 🏗️ **Architecture:** [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- 🤖 **AI Instructions:** [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- 🤝 **Contributing:** [`CONTRIBUTING.md`](CONTRIBUTING.md)
- 📜 **Third-Party Licenses:** [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)

## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK** (or later)
- **Visual Studio 2025** (recommended) or Rider/VS Code
- **Roco Z21 Digital Command Station** (optional, for testing)

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
- **.NET Foundation** for the amazing .NET ecosystem
- **CommunityToolkit** contributors for MVVM helpers
- **All contributors** who help improve MOBAflow

---

**Made with ❤️ for model railroad enthusiasts**
