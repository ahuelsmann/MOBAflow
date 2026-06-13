// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Mvc;

using Models;
using Service;

/// <summary>
/// Register/unregister MAUI (or other) clients for the Overview page.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientRegistry _clientRegistry;

    public ClientsController(IClientRegistry clientRegistry)
    {
        _clientRegistry = clientRegistry;
    }

    /// <summary>
    /// Registers a client (e.g. MAUI app). Call when the app connects to the REST API.
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterClientRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.ClientId))
        {
            return BadRequest(new { error = "ClientId is required" });
        }

        var info = new ConnectedClientInfo
        {
            ClientId = request.ClientId.Trim(),
            DeviceName = request.DeviceName?.Trim() ?? "MOBAsmart",
            ConnectedAt = DateTime.UtcNow
        };
        _clientRegistry.Add(info);
        return Ok(new { registered = true, clientId = info.ClientId });
    }

    /// <summary>
    /// Unregisters a client. Call when the app disconnects or closes.
    /// </summary>
    [HttpPost("unregister")]
    public IActionResult Unregister([FromBody] UnregisterClientRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.ClientId))
        {
            return BadRequest(new { error = "ClientId is required" });
        }

        _clientRegistry.Remove(request.ClientId.Trim());
        return Ok(new { unregistered = true });
    }
}
