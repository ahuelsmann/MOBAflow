// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Mvc;

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

    public StatusController(IClientRegistry clientRegistry)
    {
        _clientRegistry = clientRegistry;
    }

    /// <summary>
    /// Returns REST API status and list of connected clients (e.g. MAUI app).
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus([FromServices] IConfiguration configuration)
    {
        var port = GetPortFromConfig(configuration);

        _clientRegistry.PruneExpired(ClientExpiryMinutes);

        return Ok(new
        {
            status = "running",
            port,
            connectedClients = _clientRegistry.GetAll()
                .OrderBy(c => c.ConnectedAt)
                .Select(c => new { c.ClientId, c.DeviceName, c.ConnectedAt })
                .ToList()
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
