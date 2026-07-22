// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

using Moba.Common.Discovery;
using Moba.MOBApi.Security;

/// <summary>
/// UDP Discovery Service for MOBAflow MOBApi.
/// Listens for UDP Multicast from MAUI clients and responds with server IP + Port.
/// </summary>
internal class UdpDiscoveryService : BackgroundService
{
    private readonly ILogger<UdpDiscoveryService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServerIdentityProvider _serverIdentityProvider;
    private MobApiUdpDiscoveryResponder? _responder;

    public UdpDiscoveryService(
        ILogger<UdpDiscoveryService> logger,
        IConfiguration configuration,
        IServerIdentityProvider serverIdentityProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serverIdentityProvider = serverIdentityProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var restPort = ResolveRestApiPort();
        var httpsPort = ResolveHttpsPort(restPort);
        var identity = await _serverIdentityProvider.GetAsync(stoppingToken).ConfigureAwait(false);
        _logger.LogInformation(
            "UDP Discovery starting on Multicast {MulticastAddress}:{Port}",
            DiscoveryResponseParser.MulticastAddress,
            DiscoveryResponseParser.MulticastPort);

        _responder = new MobApiUdpDiscoveryResponder(
            restPort,
            httpsPort,
            identity.InstanceId,
            identity.PublicKeyFingerprint);
        try
        {
            await _responder.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "UDP Discovery failed");
        }
        finally
        {
            _logger.LogInformation("UDP Discovery stopped");
        }
    }

    private int ResolveHttpsPort(int httpPort)
    {
        var configuredPort = _configuration.GetValue<int?>("MOBAFLOW_HTTPS_PORT") ??
                             _configuration.GetValue<int?>("MOBAFLOW_HOST_HTTPS_PORT");
        if (configuredPort is > 0 and < 65536 && configuredPort != httpPort)
            return configuredPort.Value;
        if (httpPort >= 65535)
            throw new InvalidOperationException("MOBApi cannot allocate an HTTPS port after the configured HTTP port.");
        return httpPort + 1;
    }

    private int ResolveRestApiPort()
    {
        var configuredPort = _configuration.GetValue<int?>("MOBAFLOW_HTTP_PORT");
        if (configuredPort is > 0 and < 65536)
            return configuredPort.Value;

        var kestrelUrl = _configuration["Kestrel:Endpoints:Http:Url"] ?? "http://localhost:5001";
        if (Uri.TryCreate(kestrelUrl, UriKind.Absolute, out var uri))
        {
            return uri.Port;
        }

        return 5001;
    }

    public override void Dispose()
    {
        _responder?.Dispose();
        base.Dispose();
    }
}