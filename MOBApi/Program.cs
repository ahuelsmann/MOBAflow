// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net;
using Moba.Common.Security;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Security;
using Moba.MOBApi.Service;

var builder = WebApplication.CreateBuilder(args);
await using var hostBootstrapChannel = HostBootstrapChildChannel.TryOpenFromEnvironment();
HostBootstrapMaterial? hostBootstrapMaterial = null;
if (hostBootstrapChannel is not null)
{
    using var bootstrapTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var bootstrapRequest = await hostBootstrapChannel.ReadRequestAsync(bootstrapTimeout.Token).ConfigureAwait(false);
    var bootstrapLifetime = builder.Configuration.GetValue<TimeSpan?>(
        $"{ControlPlaneSecurityOptions.SectionName}:HostBootstrapLifetime") ?? TimeSpan.FromSeconds(30);
    hostBootstrapMaterial = HostBootstrapMaterial.Create(bootstrapRequest, TimeProvider.System, bootstrapLifetime);
}

var httpPort = ResolveHttpPort(builder.Configuration);
var httpsPort = ResolveHttpsPort(builder.Configuration, httpPort);
ServerIdentity? serverIdentity = null;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, httpPort);
    options.Listen(IPAddress.Any, httpsPort, listenOptions =>
    {
        listenOptions.UseHttps(httpsOptions =>
        {
            httpsOptions.ServerCertificateSelector = (_, _) =>
                serverIdentity?.Certificate ?? throw new InvalidOperationException("The MOBApi server identity is unavailable.");
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddControlPlaneSecurity(builder.Configuration, hostBootstrapMaterial);
builder.Services.AddSingleton<IClientRegistry, ClientRegistry>();
builder.Services.AddSingleton<ISolutionCache, SolutionCache>();
builder.Services.AddSingleton<IRuntimeSettingsCache, RuntimeSettingsCache>();
builder.Services.AddSingleton<IRuntimeSnapshotCache, RuntimeSnapshotCache>();
builder.Services.AddSingleton<IRuntimeHostRegistry, RuntimeHostRegistry>();
builder.Services.AddSingleton<IRuntimeRemoteRegistry, RuntimeRemoteRegistry>();
builder.Services.AddSingleton<IRuntimeBroadcastMetrics, RuntimeBroadcastMetrics>();
builder.Services.AddSingleton<IRuntimeCommandQueue, RuntimeCommandQueue>();

// When started by WinUI, discovery runs in WinUI (MOBAFLOW_DISCOVERY_IN_WINUI=1); otherwise run discovery here
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MOBAFLOW_DISCOVERY_IN_WINUI")))
    builder.Services.AddHostedService<UdpDiscoveryService>();

var app = builder.Build();
serverIdentity = await app.Services.GetRequiredService<IServerIdentityProvider>().GetAsync().ConfigureAwait(false);
if (hostBootstrapChannel is not null)
{
    using var bootstrapTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await hostBootstrapChannel.WriteResponseAsync(
        new HostBootstrapPipeResponse(serverIdentity.PublicKeyFingerprint, serverIdentity.InstanceId),
        bootstrapTimeout.Token).ConfigureAwait(false);
}

var hostCredentialService = app.Services.GetRequiredService<IHostCredentialService>();
app.Lifetime.ApplicationStopping.Register(hostCredentialService.Revoke);

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompatibilityReadObservationMiddleware>();

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

static int ResolveHttpsPort(IConfiguration configuration, int httpPort)
{
    var configuredPort = configuration.GetValue<int?>("MOBAFLOW_HTTPS_PORT") ??
                         configuration.GetValue<int?>("MOBAFLOW_HOST_HTTPS_PORT");
    if (configuredPort is > 0 and < 65536 && configuredPort != httpPort)
        return configuredPort.Value;
    if (httpPort >= 65535)
        throw new InvalidOperationException("MOBApi cannot allocate an HTTPS port after the configured HTTP port.");
    return httpPort + 1;
}
