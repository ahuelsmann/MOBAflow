# MOBAflow

See full documentation in `docs/README.md` and `docs/ARCHITECTURE.md`.

**MOBAflow** is an event-driven automation solution for model railroads. The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

## Quick Links
- Documentation: docs/README.md
- Architecture: docs/ARCHITECTURE.md
- Instructions: .copilot-instructions.md

## Getting Started

```
git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
cd MOBAflow
dotnet restore
dotnet build
```

Run WinUI: `dotnet run --project WinUI`