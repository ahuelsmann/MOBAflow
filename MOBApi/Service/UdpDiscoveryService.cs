// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

using Moba.Common.Discovery;

/// <summary>
/// UDP Discovery Service for MOBAflow MOBApi.
/// Listens for UDP Multicast from MAUI clients and responds with server IP + Port.
/// </summary>
internal class UdpDiscoveryService : BackgroundService
{
    private readonly ILogger<UdpDiscoveryService> _logger;
    private readonly IConfiguration _configuration;
    private MobApiUdpDiscoveryResponder? _responder;

    public UdpDiscoveryService(ILogger<UdpDiscoveryService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var restPort = ResolveRestApiPort();
        _logger.LogInformation(
            "UDP Discovery starting on Multicast {MulticastAddress}:{Port}",
            DiscoveryResponseParser.MulticastAddress,
            DiscoveryResponseParser.MulticastPort);

        _responder = new MobApiUdpDiscoveryResponder(restPort);
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

    private int ResolveRestApiPort()
    {
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
