// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Models;
using Security;
using Service;
using System.Security.Claims;

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
    [Authorize(Policy = ControlPlaneCapabilities.ClientPresence)]
    public IActionResult Register([FromBody] RegisterClientRequest? request)
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(credentialId))
        {
            return Unauthorized();
        }

        var info = new ConnectedClientInfo
        {
            ClientId = credentialId,
            DeviceName = request?.DeviceName?.Trim() ?? "MOBAsmart",
            ConnectedAt = DateTime.UtcNow
        };
        _clientRegistry.Add(info);
        return Ok(new { registered = true, clientId = info.ClientId });
    }

    /// <summary>
    /// Unregisters a client. Call when the app disconnects or closes.
    /// </summary>
    [HttpPost("unregister")]
    [Authorize(Policy = ControlPlaneCapabilities.ClientPresence)]
    public IActionResult Unregister([FromBody] UnregisterClientRequest? request)
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(credentialId))
        {
            return Unauthorized();
        }

        _clientRegistry.Remove(credentialId);
        return Ok(new { unregistered = true });
    }
}
