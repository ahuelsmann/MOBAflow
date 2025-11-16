# MOBAflow Documentation

This is the consolidated documentation hub for MOBAflow.

- Architecture: see docs/ARCHITECTURE.md
- Project overview, setup and guides: below

---

# MOBAflow

MOBAflow is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

## Key Features

- Workflow Automation: Event-driven action sequences based on track feedback
- Train Management: Journey-based control with stations and platforms
- Audio Integration: Station announcements and sound effects
- Direct Z21 Communication: UDP-based real-time control (Port 21105)

## Architecture (summary)

See docs/ARCHITECTURE.md for full details.

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
```

## Projects

| Project | Description | Platform |
|---------|-------------|----------|
| Backend | Core logic: Z21 communication, workflow execution, journey management | .NET 10 |
| SharedUI | Shared ViewModels (MVVM) and thin platform adapters | .NET 10 |
| WinUI | Desktop management application | Windows |
| MAUI | Mobile app (MOBAsmart) | Android |
| WebApp | Blazor Server dashboard | .NET 10 |
| Sound | Text-to-Speech and audio playback | .NET 10 |
| Test | Unit and integration tests (NUnit) | .NET 10 |

## Getting Started

Prerequisites: .NET 10 SDK, Visual Studio, Z21.

```
git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
cd MOBAflow
dotnet restore
dotnet build
```

Run WinUI: `dotnet run --project WinUI`

## DI & Factories

- Register backend services via DI: IZ21 (singleton), IJourneyManagerFactory (singleton)
- Use IJourneyViewModelFactory per platform to create platform adapters
- No `new` for services/viewmodels in UI layers; use DI/factories

## Z21 Protocol

Z21 LAN Protocol V1.13 (UDP 21105). See link in main README or vendor site.

## License

MIT (see LICENSE)
