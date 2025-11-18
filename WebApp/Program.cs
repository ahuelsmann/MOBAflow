using Moba.WebApp.Components;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface; // ✅ Factory interfaces
using Moba.Backend.Network;
using Moba.WebApp.Service;
using Moba.WebApp.Factory; // ✅ WebApp factories

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

// ✅ DataManager as Singleton (master data - simplified for Blazor Server)
builder.Services.AddSingleton(sp => new Moba.Backend.Data.DataManager());

// ✅ All ViewModel Factories (Blazor-specific) - NEW NAMESPACES
builder.Services.AddSingleton<IJourneyViewModelFactory, WebJourneyViewModelFactory>();
builder.Services.AddSingleton<IStationViewModelFactory, WebStationViewModelFactory>();
builder.Services.AddSingleton<IWorkflowViewModelFactory, WebWorkflowViewModelFactory>();
builder.Services.AddSingleton<ILocomotiveViewModelFactory, WebLocomotiveViewModelFactory>();
builder.Services.AddSingleton<ITrainViewModelFactory, WebTrainViewModelFactory>();
builder.Services.AddSingleton<IWagonViewModelFactory, WebWagonViewModelFactory>();

// TreeViewBuilder service (now with all factories)
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