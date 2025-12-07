// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Backend.Network;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WebApp.Components;
using Moba.WebApp.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor-specific services
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();

// ViewModels (CounterViewModel now requires IUiDispatcher)
builder.Services.AddSingleton<CounterViewModel>();

// Backend services - Register in dependency order
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton(sp =>
{
    var z21 = sp.GetRequiredService<Moba.Backend.Interface.IZ21>();
    return new Moba.Backend.Services.ActionExecutor(z21);
});
builder.Services.AddSingleton<Moba.Backend.Services.WorkflowService>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

// ✅ DataManager as Singleton (master data - simplified for Blazor Server)
builder.Services.AddSingleton(sp => new Moba.Backend.Data.DataManager());

// ✅ Solution as Singleton (initialized empty, can be loaded later by user)
builder.Services.AddSingleton(sp => new Moba.Domain.Solution());

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
