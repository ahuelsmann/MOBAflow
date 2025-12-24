// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Backend;
using Moba.Backend.Data;
using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WebApp.Components;
using Moba.WebApp.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuration: AppSettings (required by ViewModels)
var appSettings = new AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);
// Initialize Z21 settings with defaults if not configured
if (string.IsNullOrEmpty(appSettings.Z21.CurrentIpAddress))
{
    appSettings.Z21.CurrentIpAddress = "192.168.0.111"; // Default Z21 IP
    appSettings.Z21.DefaultPort = "21105"; // Default Z21 port
    appSettings.Z21.RecentIpAddresses.Add("192.168.0.111");
}
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton<ISettingsService, SettingsService>();

// Blazor-specific services
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();

// Backend services - Register in dependency order
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<IZ21, Z21>();
builder.Services.AddSingleton<ActionExecutor>();
builder.Services.AddSingleton<WorkflowService>();

// ✅ DataManager as Singleton (master data - simplified for Blazor Server)
builder.Services.AddSingleton<DataManager>();

// ✅ Solution as Singleton (initialized empty, can be loaded later by user)
builder.Services.AddSingleton<Solution>();

// ✅ MainWindowViewModel as Singleton (unified cross-platform ViewModel)
builder.Services.AddSingleton<MainWindowViewModel>();

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