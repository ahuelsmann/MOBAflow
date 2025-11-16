# Dependency Injection & Service Registration

## Principles
- Use `Microsoft.Extensions.DependencyInjection` for all projects.
- Prefer constructor injection. Avoid service locator pattern inside ViewModels.
- Register low-level services as `Singleton` (e.g., `IIoService`, `Backend.Z21`), UI/ViewModels as `Transient`.
- `Scoped` lifetime applies to server apps (Blazor Server, WebApp) — not applicable for WinUI/MAUI.

## Recommended patterns
- Host root composition in `App.xaml.cs` (WinUI), `MauiProgram.cs` (MAUI), `Program.cs` (WebApp).
- Register implementation interfaces, not concrete types when possible.

```csharp
// WinUI - App.xaml.cs
services.AddSingleton<IIoService, IoService>();
services.AddSingleton<Backend.Z21>();
services.AddTransient<WinUI.ViewModels.MainWindowViewModel>();
services.AddTransient<WinUI.Views.MainWindow>();

// MAUI - MauiProgram.cs
builder.Services.AddSingleton<IIoService, IoService>();
builder.Services.AddTransient<MainViewModel>();
```

## ViewModel factories
- For ViewModels that need runtime parameters, register a factory:
```csharp
services.AddTransient<Func<Journey, JourneyViewModel>>(sp => journey => new JourneyViewModel(journey));
```

## HttpClient
- Use `IHttpClientFactory` in WebApp and services that call external APIs.

```csharp
services.AddHttpClient("speech", client => {
    client.BaseAddress = new Uri("https://api.speech.example/");
});
```

## Testing
- Register test doubles in test startup or use `HostBuilder` to override registrations.
- Keep constructors small and explicit to ease mocking.

---

Keep registrations consistent across projects and document any deviation in `docs/`.