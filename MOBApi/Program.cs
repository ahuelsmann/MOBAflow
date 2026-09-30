// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Service;

var builder = WebApplication.CreateBuilder(args);

var httpPort = ResolveHttpPort(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, httpPort));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IClientRegistry, ClientRegistry>();
builder.Services.AddSingleton<ISolutionCache, SolutionCache>();
builder.Services.AddSingleton<IRuntimeSettingsCache, RuntimeSettingsCache>();
builder.Services.AddSingleton<IRuntimeSnapshotCache, RuntimeSnapshotCache>();
builder.Services.AddSingleton<IRuntimeHostRegistry, RuntimeHostRegistry>();
builder.Services.AddSingleton<IRuntimeRemoteRegistry, RuntimeRemoteRegistry>();
builder.Services.AddSingleton<IRuntimeBroadcastMetrics, RuntimeBroadcastMetrics>();
builder.Services.AddSingleton<IRuntimeCommandQueue, RuntimeCommandQueue>();
builder.Services.AddSingleton<IRuntimeCommandAdmission, RuntimeCommandAdmission>();

// When started by WinUI, discovery runs in WinUI (MOBAFLOW_DISCOVERY_IN_WINUI=1); otherwise run discovery here
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MOBAFLOW_DISCOVERY_IN_WINUI")))
    builder.Services.AddHostedService<UdpDiscoveryService>();

var app = builder.Build();

app.MapControllers();
app.MapHub<PhotoHub>("/photos-hub");
app.MapHub<RuntimeHub>("/runtime-hub");

await app.RunAsync().ConfigureAwait(false);

static int ResolveHttpPort(IConfiguration configuration)
{
    var configuredPort = configuration.GetValue<int?>("MOBAFLOW_HTTP_PORT");
    if (configuredPort is > 0 and < 65536)
        return configuredPort.Value;

    var urls = configuration["urls"];
    if (!string.IsNullOrWhiteSpace(urls))
    {
        foreach (var value in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp)
                return uri.Port;
        }
    }

    return 5001;
}
