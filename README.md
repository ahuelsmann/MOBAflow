# MOBAflow

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

## Key Features

- ✅ **Workflow Automation**: Event-driven action sequences based on track feedback
- ✅ **Train Management**: Journey-based control with stations and platforms
- ✅ **Audio Integration**: Station announcements and sound effects
- ✅ **Direct Z21 Communication**: UDP-based real-time control (Port 21105)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      MOBAflow Solution                      │
├─────────────────┬───────────────┬──────────────┬────────────┤
│   WinUI App     │  MOBAsmart    │   Backend    │   Sound    │
│  (Management)   │  (Monitor)    │  (Core)      │  (Audio)   │
└────────┬────────┴───────┬───────┴──────┬───────┴─────┬──────┘
         │                │              │             │
         └────────────────┴──────────────┴─────────────┘
                          │
                    ┌─────▼─────┐
                    │    Z21    │ UDP Port 21105
                    │  Digital  │
                    │  Station  │
                    └───────────┘
                          │
                    ══════╪══════  DCC Protocol
                     Model Railroad
```

---

## Projects

| Project | Description | Platform |
|---------|-------------|----------|
| **Backend** | Core logic: Z21 communication, workflow execution, journey management | .NET 10 |
| **SharedUI** | Shared ViewModels (MVVM pattern) for WinUI and MAUI | .NET 10 |
| **WinUI** | Desktop management application with visual workflow designer | Windows 10/11 |
| **MOBAsmart** | Mobile lap counter and feedback monitor | Android 16+ |
| **Sound** | Text-to-Speech and audio playback engine | .NET 10 |
| **Test** | Unit and integration tests (NUnit) | .NET 10 |

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022 (17.13+)
- Roco Z21 Digital Command Station
- Windows 10/11 (WinUI) or Android 16+ (MOBAsmart)

### Quick Start

```bash
# Clone repository
git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
cd MOBAflow

# Restore and build
dotnet restore
dotnet build

# Run WinUI app
dotnet run --project WinUI

# Deploy MOBAsmart to Android device via Visual Studio
```

### Configuration

- **WinUI**: Configure Z21 IP in app → Project → IP Address List
- **MOBAsmart**: Enter Z21 IP in app (default: 192.168.0.111)

---

## Z21 Protocol

MOBAflow implements the **Z21 LAN Protocol Specification V1.13** for direct UDP communication (Port 21105).

**Documentation**: [Z21 LAN Protocol (PDF)](https://www.z21.eu/media/Kwc_Basic_DownloadTag_Component/47-1652-959-downloadTag/default/69bad87e/1699290251/z21-lan-protokoll.pdf)

**Key Features**:
- Bidirectional UDP communication
- Real-time feedback events (R-BUS)
- Command station control (track power, locomotive speed, etc.)
- Automatic keep-alive (ping every 60s)

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`feature/my-feature`)
3. Commit with meaningful messages
4. Create a Pull Request

**Code Standards**:
- ✅ English for all code and documentation
- ✅ MVVM pattern for UI projects
- ✅ XML documentation for public APIs
- ✅ Follow [.copilot-instructions.md](.copilot-instructions.md)

---

## Troubleshooting

### Z21 Connection Failed

**Check**:
- Z21 is powered on and connected to network
- Device is on same network (192.168.0.x)
- Firewall allows UDP Port 21105

**Windows Firewall**:
```powershell
New-NetFirewallRule -DisplayName "Z21" -Direction Inbound -LocalPort 21105 -Protocol UDP -Action Allow
```

---

## Resources

- [Z21 LAN Protocol Specification](https://www.z21.eu/media/Kwc_Basic_DownloadTag_Component/47-1652-959-downloadTag/default/69bad87e/1699290251/z21-lan-protokoll.pdf)
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
- [WinUI 3 Documentation](https://learn.microsoft.com/windows/apps/winui/)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## Trademark Notice

**Z21®** and **Roco®** are registered trademarks of Modelleisenbahn GmbH, Plainbachstraße 4, A-5101 Bergheim, Austria.

This project is an independent software application and is not affiliated with, endorsed by, or sponsored by Modelleisenbahn GmbH. The use of these trademarks is solely for the purpose of describing compatibility with the Z21 Digital Command Station and does not imply any commercial relationship or endorsement.

---

## Author

**Andreas Huelsmann**

- 🐙 GitHub: [ahuelsmann](https://github.com/ahuelsmann)
- 💼 Azure DevOps: [MOBAflow Project](https://dev.azure.com/ahuelsmann/MOBAflow)

---

## Version History

- **v2.0.0** (2025-11): Direct Z21 communication, MOBAsmart app, improved stability
- **v1.0.0** (2024): Initial release with workflow system

---

<div align="center">

**Built with ❤️ for the Model Railroad Community**

[Report Bug](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems) · [Request Feature](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems)

</div>