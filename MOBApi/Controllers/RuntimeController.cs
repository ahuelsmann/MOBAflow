// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Common.Runtime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Security;
using Moba.MOBApi.Service;

using System.Net;
using System.Text.Json;

/// <summary>
/// REST fallback for MOBAflow runtime snapshot sync.
/// </summary>
[ApiController]
[Route("api/runtime")]
public class RuntimeController : ControllerBase
{
    private readonly IRuntimeSnapshotCache _snapshotCache;

    public RuntimeController(IRuntimeSnapshotCache snapshotCache)
    {
        _snapshotCache = snapshotCache;
    }

    [HttpGet("meta")]
    [Authorize(Policy = ControlPlaneCapabilities.Read)]
    public IActionResult GetMeta()
    {
        if (!_snapshotCache.TryGet(out var entry))
        {
            return NotFound(new { error = "No runtime snapshot available yet." });
        }

        return Ok(new
        {
            updatedAt = entry.UpdatedAt,
            isConnected = entry.IsConnected
        });
    }

    [HttpGet("snapshot")]
    [Authorize(Policy = ControlPlaneCapabilities.Read)]
    public IActionResult GetSnapshot()
    {
        if (!_snapshotCache.TryGet(out var entry))
        {
            return NotFound(new { error = "No runtime snapshot available yet." });
        }

        return Content(entry.Json, "application/json");
    }

    [HttpPut("snapshot")]
    [Authorize(Policy = ControlPlaneCapabilities.HostPublish)]
    public IActionResult PutSnapshot([FromBody] JsonElement? body)
    {
        if (!IsLocalhostRequest())
        {
            return Forbid();
        }

        if (body == null)
        {
            return BadRequest(new { error = "Snapshot body is required." });
        }

        var json = body.Value.GetRawText();
        var snapshot = RuntimeJsonSerializer.Deserialize(json);
        if (snapshot == null)
        {
            return BadRequest(new { error = "Invalid runtime snapshot JSON." });
        }

        _snapshotCache.Set(json, snapshot.IsConnected);
        return Ok(new { updatedAt = DateTimeOffset.UtcNow, isConnected = snapshot.IsConnected });
    }

    private bool IsLocalhostRequest()
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp == null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remoteIp))
        {
            return true;
        }

        return remoteIp.Equals(HttpContext.Connection.LocalIpAddress);
    }
}
