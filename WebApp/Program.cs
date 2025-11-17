using Moba.WebApp.Components;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.Service;
using Moba.Backend.Network;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Explicit backend registrations
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

// Factories
builder.Services.AddSingleton<IJourneyViewModelFactory, Moba.WebApp.Service.WebJourneyViewModelFactory>();

// TreeViewBuilder service
builder.Services.AddSingleton<TreeViewBuilder>();

// ⚠️ Note: Blazor Server doesn't use IUiDispatcher (InvokeAsync on ComponentBase instead)
// CounterViewModel is NOT registered here as it requires IUiDispatcher (MAUI/WinUI only)
// For Blazor, use separate ViewModel or handle UI updates differently

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();