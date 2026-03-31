# AGENTS.md

## Cursor Cloud specific instructions

### Platform scope

This is a .NET 10 multi-platform solution. On the Linux Cloud VM **only cross-platform projects** can build and run:

| Buildable on Linux | NOT buildable (platform-specific) |
|---|---|
| Domain, Common, Backend, Sound, SharedUI, SharedUI.Web, TrackLibrary.Base, TrackLibrary.PikoA, TrackPlan.Renderer, MOBApi, Test | MOBAflow (`net10.0-windows10.0.22621.0`), MOBAsmart (`net10.0-android`), MAUI.Controls (`net10.0;net10.0-android`) |

### Build & test commands

Standard commands are documented in `docs/CLAUDE.md` and `README.md`. Key cross-platform commands:

```bash
# Restore & build individual projects (solution-level restore fails due to Windows/Android TFMs)
dotnet restore <project>.csproj
dotnet build <project>.csproj

# Typical cross-platform host build
dotnet build MOBApi/MOBApi.csproj

# Run tests
dotnet test Test/Test.csproj
```

### Known issues on Linux Cloud VM

- **System.Speech tests**: 2 tests in `SystemSpeechEngineTest` always fail on Linux (`PlatformNotSupportedException`). This is expected.
- **Solution-level restore**: `dotnet restore Moba.slnx` fails because the solution contains Windows and Android target frameworks. Restore individual `.csproj` files instead.
- **MOBAflow desktop app**: `MOBAflow/MOBAflow.csproj` requires Windows/WinUI tooling and cannot be built on Linux.
- **MOBAsmart**: `MOBAsmart/MOBAsmart.csproj` targets Android and requires MAUI/Android workloads that are not available on the Linux Cloud VM.

### .NET SDK

The project requires .NET 10 SDK (pinned in `global.json` to 10.0.103 with `latestFeature` rollForward). Installed at `/usr/share/dotnet`.
