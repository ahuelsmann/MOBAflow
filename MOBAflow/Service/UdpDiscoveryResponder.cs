// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Discovery;
using Common.Extension;

using Microsoft.Extensions.Logging;

/// <summary>
/// UDP Discovery Responder for MOBAflow REST-API.
/// Listens for UDP Multicast discovery requests from MAUI clients and responds with server IP + Port.
/// Runs alongside the MOBApi REST host to enable automatic server discovery on the local network.
/// </summary>
public sealed partial class UdpDiscoveryResponder : IDisposable
{
    private readonly ILogger<UdpDiscoveryResponder> _logger;
    private readonly MobApiUdpDiscoveryResponder _responder;

    public UdpDiscoveryResponder(ILogger<UdpDiscoveryResponder> logger, int restApiPort)
    {
        _logger = logger;
        _responder = new MobApiUdpDiscoveryResponder(restApiPort);
    }

    /// <summary>
    /// Starts the UDP Discovery responder.
    /// </summary>
    public void Start()
    {
        _responder.Start();
        _logger.LogInformation(
            "UDP Discovery responder started on Multicast {MulticastAddress}:{Port}",
            DiscoveryResponseParser.MulticastAddress,
            DiscoveryResponseParser.MulticastPort);
    }

    /// <summary>
    /// Stops the UDP Discovery responder.
    /// </summary>
    public void Stop()
    {
        _responder.Stop();
        _logger.LogInformation("UDP Discovery responder stopped");
    }

    public void Dispose()
    {
        _responder.Dispose();
    }
}
