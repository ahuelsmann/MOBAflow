# Dependency Injection Instructions (MOBAflow)

Scope: DI conventions and registrations across MOBAflow (Backend, SharedUI, WinUI, MAUI, WebApp).

## Principles
- Backend remains platform-independent: no platform-specific APIs in `Backend`.
- All external I/O is abstracted behind DI-friendly interfaces (e.g., `IUdpClientWrapper`).
- Prefer constructor injection; avoid `new` in UI layers (use DI/factories).
- DI remains explicit per app (WinUI/MAUI/WebApp). No central backend DI extension inside `Backend`.
- 
## Core Contracts
- `Backend.Interface.IZ21` (Singleton)
- `Backend.Interface.IJourneyManagerFactory` (Singleton)
- `Backend.Network.IUdpClientWrapper` (Singleton)

## DI-based wrappers for external I/O (MANDATORY)
- All external I/O (UDP/Z21, file, sockets, etc.) must be abstracted behind DI-injected interfaces.
- Use `Backend.Network.IUdpClientWrapper` for UDP; `Backend.Z21` must receive it via constructor injection.
- No direct `UdpClient` usage in backend classes beyond the wrapper.
- Register `IUdpClientWrapper` and `IZ21` in DI per platform (WinUI, MAUI, WebApp).

## DI & Factories (MANDATORY)
- Register backend services via DI: `IZ21` (singleton), `IJourneyManagerFactory` (singleton).
- Use `IJourneyViewModelFactory` per platform to create `JourneyViewModel` adapters.
- Never instantiate services or viewmodels with `new` in UI layers; resolve via DI/factory.
- Register `IUdpClientWrapper` and inject into `Z21` (DI-based UDP handling).
- DI remains explicit per app (WinUI/MAUI/WebApp). No central backend DI extension inside the `Backend` project.

## Platform Registrations (Examples)

### WinUI (App.xaml.cs)
```csharp
services.AddSingleton<Moba.SharedUI.Service.IIoService, Moba.WinUI.Service.IoService>();
services.AddSingleton<Moba.Backend.Network.IUdpClientWrapper, Moba.Backend.Network.UdpWrapper>();
services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
services.AddSingleton<Moba.SharedUI.Service.IJourneyViewModelFactory, Moba.WinUI.Service.WinUIJourneyViewModelFactory>();
services.AddTransient<Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel>();
services.AddTransient<MainWindow>();
```

### MAUI (MauiProgram.cs)
```csharp
builder.Services.AddSingleton<Moba.Backend.Network.IUdpClientWrapper, Moba.Backend.Network.UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
// ViewModels and Views as needed
```

### WebApp (Program.cs)
```csharp
builder.Services.AddSingleton<Moba.Backend.Network.IUdpClientWrapper, Moba.Backend.Network.UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
```

## Testing
- Use `FakeUdpClientWrapper` in tests to avoid real UDP.
- Construct `Z21` with `new Z21(fakeUdp, null)` to provide the fake and no logger.
- Prefer typed events in tests (`OnXBusStatusChanged`, `OnSystemStateChanged`) and avoid byte-array parsing assertions.

## Checklist
- Backend has no platform-specific code.
- All I/O wrapped behind interfaces.
- DI registrations present per app (WinUI/MAUI/WebApp).
- No `new` in UI layers for services/viewmodels.
- Unit tests can substitute fakes/mocks via interfaces.

## Notes
- For additional backends/services, follow the same pattern: interface + implementation + per-app registration.
- Keep DI surface minimal to reduce coupling.
