# CLAUDE.md - MOBAflow

## Project Overview

MOBAflow is an event-driven automation solution for model railroads built on .NET 10. It controls train workflows, station announcements, and real-time feedback via direct UDP connection to the Roco Z21 Digital Command Station.

## Tech Stack

- **Language:** C# 14, .NET 10 (SDK 10.0.100+)
- **UI:** WinUI 3 (Windows), MAUI (Android), Blazor (Web)
- **MVVM:** CommunityToolkit.Mvvm 8.4.0 (source generators)
- **DI:** Microsoft.Extensions.DependencyInjection
- **Logging:** Serilog
- **Speech:** Azure Cognitive Services, System.Speech
- **Testing:** NUnit 4.4.0, Moq 4.20.72, Coverlet
- **CI/CD:** Azure DevOps Pipelines

## Build & Run

```bash
dotnet restore
dotnet build                              # Build all
dotnet run --project WinUI                # Run Windows app
dotnet run --project WebApp               # Run Blazor web app
dotnet test                               # Run all tests (262/263 passing)
dotnet test /p:CollectCoverage=true       # Run tests with coverage
```

**Build configurations:** Debug, FastDebug (no analyzers), Release (warnings-as-errors)

## Project Structure

```
Domain/          Pure business logic, POCOs, domain models (no dependencies)
Backend/         Application services, Z21 protocol, action executors
Common/          Shared utilities, config, plugins, events, validation
SharedUI/        Cross-platform ViewModels (CommunityToolkit.Mvvm)
WinUI/           Windows Desktop UI (WinUI 3)
MAUI/            Android Mobile UI
WebApp/          Blazor Server web application
TrackPlan.Renderer/    Track geometry & SVG rendering
TrackLibrary.Base/     Base track system interfaces
TrackLibrary.PikoA/    Piko A-Gleis track templates
Sound/           Audio resources & sound management
Test/            Unit tests (NUnit)
docs/            Documentation (20+ markdown files)
```

**Dependency flow:** Domain -> Backend/Common -> SharedUI -> WinUI/MAUI/WebApp

## Architecture

- **Clean Architecture** with strict layer separation
- **MVVM** with CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Constructor injection only** (no service locator)
- **Event-driven** via IEventBus for decoupled messaging
- **Async-first** - all I/O uses async/await, no `.Result` or `.Wait()`

## Coding Conventions

### Naming
- **Namespaces:** `Moba.{Layer}.{Feature}` (e.g., `Moba.Backend.Service`)
- **Classes/Methods/Properties:** PascalCase
- **Private fields:** `_camelCase` (underscore prefix)
- **Z21 protocol constants:** UPPER_SNAKE_CASE (matches spec docs, e.g., `LAN_GET_SERIAL_NUMBER`)

### Style (enforced via .editorconfig)
- 4-space indentation, CRLF line endings, max 140 chars per line
- Using directives inside namespace
- Nullable reference types enabled globally
- `var` for built-in types; explicit types elsewhere
- Expression bodies for properties and accessors, not for methods/constructors
- Braces optional (silent preference)
- Primary constructors preferred where appropriate

### Patterns
- `ArgumentNullException.ThrowIfNull()` for argument validation
- Structured Serilog logging with properties (not string interpolation)
- Records for immutable domain models
- `sealed partial class` for ViewModels with source generators

### Testing
- NUnit with Arrange-Act-Assert structure
- Naming: `[TestClass]_Should_[Behavior]_When_[Condition]`
- Moq for interface mocking, FakeUdpClientWrapper for Z21 simulation
- Async test support throughout

## Key Configuration Files

| File | Purpose |
|------|---------|
| `Moba.slnx` | Solution file |
| `Directory.Build.props` | Central build config (C# 14, nullable, platform) |
| `Directory.Packages.props` | Centralized NuGet package versions |
| `global.json` | .NET SDK version pinning |
| `.editorconfig` | Code style enforcement |
| `version.json` | MinVer semantic versioning |

## Important Notes

- Repository hosted on Azure DevOps: `dev.azure.com/ahuelsmann/MOBAflow`
- Release builds treat all warnings as errors
- MAUI builds are excluded from CI pipeline (WinUI, WebApp, Test only)
- Protocol constants intentionally use UPPER_SNAKE_CASE to match Z21 LAN Protocol spec
- ReSharper settings (`.sln.DotSettings`) contain 125+ documented suppression entries
