# CLAUDE.md - MOBAflow

## Project Overview

MOBAflow is an event-driven automation solution for model railroads
built on .NET 10. It controls train workflows, station announcements,
and real-time feedback via direct UDP connection to the Roco Z21
Digital Command Station.

## Tech Stack

- **Language:** C# 14, .NET 10 (SDK pinned via `global.json`)
- **UI:** WinUI 3 (`MOBAflow`), MAUI for Android (`MOBAsmart`),
  shared web-facing components in `SharedUI.Web`
- **MVVM:** CommunityToolkit.Mvvm 8.4.2 (source generators)
- **DI:** Microsoft.Extensions.DependencyInjection
- **Logging:** Serilog
- **Speech:** Piper TTS, System.Speech
- **Testing:** NUnit 4.5.1, Moq 4.20.72, Coverlet

## Build & Run

```bash
dotnet restore MOBAflow/MOBAflow.csproj
dotnet restore MOBApi/MOBApi.csproj
dotnet build MOBAflow/MOBAflow.csproj     # Build Windows desktop app
dotnet build MOBApi/MOBApi.csproj         # Build REST API
dotnet test Test/Test.csproj
dotnet test Test/Test.csproj --settings Test/coverlet.runsettings \
  --results-directory TestResults
```

**Cross-platform subset (no WinUI / Android MAUI):**

```bash
dotnet restore Backend/Backend.csproj
dotnet build Backend/Backend.csproj
dotnet test Test/Test.csproj
```

**Build configurations:** Debug, FastDebug (no analyzers), Release (warnings-as-errors)

## Project Structure

```text
Domain/               Pure business logic, POCOs, domain models (no dependencies)
Backend/              Application services, Z21 protocol, action executors
Common/               Shared utilities, config, plugins, events, validation
SharedUI/             Cross-platform ViewModels (CommunityToolkit.Mvvm)
SharedUI.Web/         Shared ASP.NET Core-facing components
MOBAflow/             Windows desktop UI (WinUI 3)
MOBApi/               REST API host
MOBAsmart/            Android mobile UI (MAUI)
MAUI.Controls/        Shared MAUI controls
TrackPlan.Renderer/   Track geometry & rendering
TrackLibrary.Base/    Base track system interfaces
TrackLibrary.PikoA/   Piko A-Gleis track templates
Sound/                Audio resources & sound management
Test/                 Unit tests (NUnit)
docs/                 Documentation
```

**Dependency flow:** Domain -> Backend/Common -> SharedUI -> MOBAflow/MOBAsmart/SharedUI.Web

## Architecture

- **Clean Architecture** with strict layer separation
- **MVVM** with CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Constructor injection only** (no service locator)
- **Event-driven** via IEventBus for decoupled messaging
- **Async-first** - all I/O uses async/await, no `.Result` or `.Wait()`
- **Runtime:** SharedUI ViewModels depend on **`IMobaRuntime`** (`MobaRuntimeService`).
  There is no `IMobaClient` layer; WinUI and MAUI register the same singleton runtime.
- **Master data:** Shared cities/locomotives JSON is loaded into **`MasterDataStore`**
  (registered in `AddMobaBackendServices`).
- **Workflows:** `IWorkflowService.ExecuteAsync` accepts optional **`WorkflowExecutionOptions`**
  (e.g. `StopOnFirstActionFailure` for sequential runs).
- **MobaRuntimeService** is a **`partial`** type split across several files under
  `Backend/Service/` for maintainability (public API, Z21 handlers, auto-connect,
  status text helpers, core snapshot/ctor).

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
- **Test suite:** Run `dotnet test Test/Test.csproj`. Some
  `System.Speech` tests are Windows-only.
- **Coverage (local):** ReSharper includes **dotCover** – in Visual
  Studio use *ReSharper → Unit Tests → Run Unit Tests with Coverage*
  (or right-click test project → Cover). Coverage is shown in the
  Coverage tool window. For CLI/runsettings-based collection, use
  `dotnet test --settings Test/coverlet.runsettings` (Coverlet;
  output in `TestResults/.../coverage.cobertura.xml`) or
  `dotnet-coverage collect -s Test/dotnet-coverage.runsettings -f`
  `cobertura -o coverage.cobertura.xml dotnet test Test/Test.csproj`
  `--no-build`.
- **Coverage (CI):** The **quality.yml** pipeline (PRs to main) runs
  tests with **dotnet-coverage** and publishes results via
  `PublishCodeCoverageResults@2` (Cobertura). Coverage percentage and
  report are visible in the Azure DevOps build summary and the
  "Code Coverage" tab. Settings:
  `Test/dotnet-coverage.runsettings` (excludes test assemblies,
  platform-specific paths).

## Key Configuration Files

- **Moba.slnx**: Solution file
- **Directory.Build.props**: Central build config (C# 14, nullable, platform)
- **Directory.Packages.props**: Centralized NuGet package versions
- **global.json**: .NET SDK version pinning
- **.editorconfig**: Code style enforcement
- **version.json**: MinVer semantic versioning

## Important Notes

- Repository hosted on Azure DevOps: `dev.azure.com/ahuelsmann/MOBAflow`
- Release builds treat all warnings as errors
- CI and local validation should restore/build individual projects
  instead of assuming solution-wide restore works on every platform
- Protocol constants intentionally use UPPER_SNAKE_CASE to match Z21
  LAN Protocol spec
- ReSharper settings (`.sln.DotSettings`) contain 125+ documented suppression entries
