# AGENTS.md

## Cursor Cloud specific instructions

### Platform scope

This is a .NET 10 multi-platform solution. On the Linux Cloud VM **only cross-platform projects** can build and run:

| Buildable on Linux | NOT buildable (platform-specific) |
|---|---|
| Domain, Common, Backend, Sound, SharedUI, TrackLibrary.Base, TrackLibrary.PikoA, TrackPlan.Renderer, WebApp, ReactApp, Test | WinUI (`net10.0-windows10.0.22621.0`), MAUI (`net10.0-android`) |

### Build & test commands

Standard commands are documented in `docs/CLAUDE.md` and `README.md`. Key cross-platform commands:

```bash
# Restore & build individual projects (solution-level restore fails due to WinUI TFM)
dotnet restore <project>.csproj
dotnet build <project>.csproj

# Run tests (246/248 pass on Linux; 2 System.Speech tests fail with PlatformNotSupportedException)
dotnet test Test/Test.csproj

# React SPA (Vite dev server, independent of .NET backend)
cd ReactApp/ClientApp && npm run dev    # http://localhost:5173
cd ReactApp/ClientApp && npm run build  # TypeScript + Vite production build
cd ReactApp/ClientApp && npx tsc --noEmit  # TypeScript type check only
```

### Known issues on Linux Cloud VM

- **WebApp and ReactApp .NET backends crash at startup** due to a missing `IEventBus` DI registration in `Program.cs`. Both call `AddMobaBackendServices()` which requires `IEventBus`, but neither calls `AddEventBus()` or `AddEventBusWithUiDispatch()` beforehand. The React SPA (Vite dev server on port 5173) works independently.
- **ESLint config missing**: `ReactApp/ClientApp` has no `eslint.config.js` (required by ESLint v9+). The `npm run lint` command fails. Use `npx tsc --noEmit` for type checking instead.
- **System.Speech tests**: 2 tests in `SystemSpeechEngineTest` always fail on Linux (`PlatformNotSupportedException`). This is expected.
- **Solution-level restore**: `dotnet restore Moba.slnx` fails because WinUI targets `net10.0-windows10.0.22621.0`. Restore individual `.csproj` files instead.

### .NET SDK

The project requires .NET 10 SDK (pinned in `global.json` to 10.0.103 with `latestFeature` rollForward). Installed at `/usr/share/dotnet`.

### React SPA

`ReactApp/ClientApp` uses npm (lockfile: `package-lock.json`). Node modules must be installed via `npm install`.
