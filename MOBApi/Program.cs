// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.MOBApi.Hubs;
using Moba.MOBApi.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IClientRegistry, ClientRegistry>();
builder.Services.AddSingleton<ISolutionCache, SolutionCache>();
builder.Services.AddSingleton<IRuntimeSettingsCache, RuntimeSettingsCache>();
builder.Services.AddSingleton<IRuntimeSnapshotCache, RuntimeSnapshotCache>();
builder.Services.AddSingleton<IRuntimeHostRegistry, RuntimeHostRegistry>();
builder.Services.AddSingleton<IRuntimeCommandQueue, RuntimeCommandQueue>();

// When started by WinUI, discovery runs in WinUI (MOBAFLOW_DISCOVERY_IN_WINUI=1); otherwise run discovery here
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MOBAFLOW_DISCOVERY_IN_WINUI")))
    builder.Services.AddHostedService<UdpDiscoveryService>();

var app = builder.Build();

app.MapControllers();
app.MapHub<PhotoHub>("/photos-hub");
app.MapHub<RuntimeHub>("/runtime-hub");

app.Run();