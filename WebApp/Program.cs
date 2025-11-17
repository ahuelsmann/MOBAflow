using Moba.WebApp.Components;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.Service;
using Moba.Backend.Network;
using Moba.WebApp.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor-specific services
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();

// ViewModels (CounterViewModel now requires IUiDispatcher)
builder.Services.AddSingleton<CounterViewModel>();

// Explicit backend registrations
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

// Factories
builder.Services.AddSingleton<IJourneyViewModelFactory, Moba.WebApp.Service.WebJourneyViewModelFactory>();

// TreeViewBuilder service
builder.Services.AddSingleton<TreeViewBuilder>();

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