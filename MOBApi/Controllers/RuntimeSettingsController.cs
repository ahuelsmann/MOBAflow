// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Mvc;

using Models;
using Service;

using System.Net;

/// <summary>
/// Exposes MOBAflow runtime settings (Z21 endpoint) to MOBAsmart via MOBApi.
/// WinUI pushes via PUT (localhost only); mobile clients read via GET.
/// </summary>
[ApiController]
[Route("api/runtime-settings")]
public class RuntimeSettingsController : ControllerBase
{
    private readonly IRuntimeSettingsCache _runtimeSettingsCache;

    public RuntimeSettingsController(IRuntimeSettingsCache runtimeSettingsCache)
    {
        _runtimeSettingsCache = runtimeSettingsCache;
    }

    /// <summary>
    /// Returns the Z21 endpoint configured in MOBAflow when available.
    /// </summary>
    [HttpGet]
    public IActionResult GetRuntimeSettings()
    {
        if (!_runtimeSettingsCache.TryGetZ21Endpoint(out var ipAddress, out var port))
        {
            return NotFound(new { error = "No runtime settings available yet." });
        }

        return Ok(new { z21IpAddress = ipAddress, z21Port = port });
    }

    /// <summary>
    /// Receives runtime settings from MOBAflow WinUI (localhost only).
    /// </summary>
    [HttpPut]
    public IActionResult PutRuntimeSettings([FromBody] RuntimeSettingsRequest? request)
    {
        if (!IsLocalhostRequest())
        {
            return Forbid();
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Z21IpAddress))
        {
            return BadRequest(new { error = "Z21IpAddress is required." });
        }

        var port = request.Z21Port > 0 ? request.Z21Port : 21105;
        if (port >= 65536)
        {
            return BadRequest(new { error = "Z21Port is out of range." });
        }

        if (!IPAddress.TryParse(request.Z21IpAddress.Trim(), out _))
        {
            return BadRequest(new { error = "Z21IpAddress is not a valid IPv4 address." });
        }

        _runtimeSettingsCache.SetZ21Endpoint(request.Z21IpAddress, port);
        return Ok(new { z21IpAddress = request.Z21IpAddress.Trim(), z21Port = port });
    }

    private bool IsLocalhostRequest()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote == null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remote))
        {
            return true;
        }

        if (remote.IsIPv4MappedToIPv6)
        {
            remote = remote.MapToIPv4();
            return IPAddress.IsLoopback(remote);
        }

        return false;
    }
}
