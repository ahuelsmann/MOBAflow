// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Backend.Network;
using Moba.Backend.Service;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WebApp.Components;
using Moba.WebApp.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuration: AppSettings (required by ViewModels)
var appSettings = new Moba.Common.Configuration.AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);
// Initialize Z21 settings with defaults if not configured
if (string.IsNullOrEmpty(appSettings.Z21.CurrentIpAddress))
{
    appSettings.Z21.CurrentIpAddress = "192.168.0.111"; // Default Z21 IP
    appSettings.Z21.DefaultPort = "21105"; // Default Z21 port
    appSettings.Z21.RecentIpAddresses.Add("192.168.0.111");
}
builder.Services.AddSingleton(appSettings);

// Blazor-specific services
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();

// Backend services - Register in dependency order
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton(sp =>
{
    var z21 = sp.GetRequiredService<Moba.Backend.Interface.IZ21>();
    return new Moba.Backend.Service.ActionExecutor(z21);
});
builder.Services.AddSingleton<WorkflowService>();

// ✅ DataManager as Singleton (master data - simplified for Blazor Server)
builder.Services.AddSingleton(sp => new Moba.Backend.Data.DataManager());

// ✅ Solution as Singleton (initialized empty, can be loaded later by user)
builder.Services.AddSingleton(sp => new Moba.Domain.Solution());

// ✅ CounterViewModel as Singleton (requires all dependencies above)
builder.Services.AddSingleton(sp =>
{
    var z21 = sp.GetRequiredService<Moba.Backend.Interface.IZ21>();
    var dispatcher = sp.GetRequiredService<IUiDispatcher>();
    var settings = sp.GetRequiredService<Moba.Common.Configuration.AppSettings>();
    var solution = sp.GetRequiredService<Moba.Domain.Solution>();
    return new CounterViewModel(z21, dispatcher, settings, solution);
});

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