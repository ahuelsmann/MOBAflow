// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.RestApi.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// When started by WinUI, discovery runs in WinUI (MOBAFLOW_DISCOVERY_IN_WINUI=1); otherwise run discovery here
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MOBAFLOW_DISCOVERY_IN_WINUI")))
    builder.Services.AddHostedService<UdpDiscoveryService>();

var app = builder.Build();

app.MapControllers();

app.Run();
