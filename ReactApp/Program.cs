// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Backend.Data;
using Moba.Backend.Extensions;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.ReactApp.Controllers;
using Moba.ReactApp.Service;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Sound;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// SERVICE CONFIGURATION
// ============================================================================

// Razor Components (Blazor Server - for validation/comparison)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// REST API Controllers with Swagger - explicitly add ReactApp assembly so controllers are discovered
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Z21Controller).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: allow Vite dev server (5173) and same-origin when serving SPA from backend (49913/49914)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "http://localhost:49913",
                  "https://localhost:49913",
                  "http://localhost:49914",
                  "https://localhost:49914")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configuration: AppSettings
var appSettings = new AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);
if (string.IsNullOrEmpty(appSettings.Z21.CurrentIpAddress))
{
    appSettings.Z21.CurrentIpAddress = "192.168.0.111";
    appSettings.Z21.DefaultPort = "21105";
    appSettings.Z21.RecentIpAddresses.Add("192.168.0.111");
}
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IIoService, NullIoService>();

// Blazor-specific services
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();

// Audio Services (NullObject - WebApp doesn't support audio)
builder.Services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
builder.Services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();

// Event bus infrastructure
builder.Services.AddSingleton<IEventBus, EventBus>();

// Backend services
builder.Services.AddMobaBackendServices();

// DataManager as Singleton
builder.Services.AddSingleton<DataManager>();

// WebAppViewModel as Singleton
builder.Services.AddSingleton<WebAppViewModel>();

var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

if (app.Environment.IsDevelopment())
{
    // CORS for React dev server
    app.UseCors("ReactDev");

    // Swagger UI for API documentation
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MOBAflow API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Avoid 404 for browser's default favicon.ico request (no file in wwwroot)
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 204;
        return;
    }
    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

// Map REST API Controllers under /api
app.MapControllers();

// React SPA routing:
// Fallback to index.html for all unmatched requests (client-side routing)
app.MapFallback(() => Results.File(
    Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html"),
    "text/html"));

await app.RunAsync();
