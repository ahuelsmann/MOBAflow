// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Security;
using Service;

/// <summary>
/// REST API for server status and connected MAUI clients.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private const int ClientExpiryMinutes = 10;
    private readonly IClientRegistry _clientRegistry;
    private readonly IRuntimeHostRegistry _hostRegistry;
    private readonly IRuntimeRemoteRegistry _remoteRegistry;
    private readonly IRuntimeBroadcastMetrics _broadcastMetrics;
    private readonly IRuntimeSnapshotCache _snapshotCache;
    private readonly ISolutionCache _solutionCache;

    public StatusController(
        IClientRegistry clientRegistry,
        IRuntimeHostRegistry hostRegistry,
        IRuntimeRemoteRegistry remoteRegistry,
        IRuntimeBroadcastMetrics broadcastMetrics,
        IRuntimeSnapshotCache snapshotCache,
        ISolutionCache solutionCache)
    {
        _clientRegistry = clientRegistry;
        _hostRegistry = hostRegistry;
        _remoteRegistry = remoteRegistry;
        _broadcastMetrics = broadcastMetrics;
        _snapshotCache = snapshotCache;
        _solutionCache = solutionCache;
    }

    /// <summary>
    /// Returns REST API status and list of connected clients (e.g. MAUI app).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ControlPlaneCapabilities.Read)]
    public IActionResult GetStatus([FromServices] IConfiguration configuration)
    {
        var port = GetPortFromConfig(configuration);

        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return Ok(new
            {
                status = "running",
                port
            });
        }

        _clientRegistry.PruneExpired(ClientExpiryMinutes);

        return Ok(new
        {
            status = "running",
            port,
            connectedClients = _clientRegistry.GetAll()
                .OrderBy(c => c.ConnectedAt)
                .Select(c => new { c.ClientId, c.DeviceName, c.ConnectedAt })
                .ToList(),
            runtime = RuntimeStatusBuilder.BuildRuntimeStatus(
                _hostRegistry,
                _remoteRegistry,
                _broadcastMetrics,
                _snapshotCache),
            solution = RuntimeStatusBuilder.BuildSolutionStatus(_solutionCache)
        });
    }

    private static int GetPortFromConfig(IConfiguration configuration)
    {
        var url = configuration["Kestrel:Endpoints:Http:Url"];
        if (!string.IsNullOrEmpty(url) && url.Contains(':', StringComparison.Ordinal))
        {
            var part = url.Split(':').LastOrDefault()?.TrimEnd('/');
            if (part != null && int.TryParse(part, out var p))
            {
                return p;
            }
        }

        return 5001;
    }
}
