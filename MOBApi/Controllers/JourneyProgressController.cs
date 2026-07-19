// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Common.Runtime;
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Service;
using System.Net;

[ApiController]
[Route("api/runtime/journeys/{journeyId:guid}/feedback-progress")]
public sealed class JourneyProgressController(IRuntimeSnapshotCache snapshotCache, IRuntimeCommandQueue commandQueue) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(Guid journeyId)
    {
        if (!snapshotCache.TryGet(out var entry)) return NotFound(new { error = "No runtime snapshot available yet." });
        var snapshot = RuntimeJsonSerializer.Deserialize(entry.Json);
        if (snapshot == null || !snapshot.JourneyStates.TryGetValue(journeyId, out var state))
            return NotFound(new { error = "Journey runtime state not found." });
        return Ok(state);
    }

    [HttpPost("reset")]
    public IActionResult Reset(Guid journeyId)
    {
        if (!IsLocalhostRequest()) return Forbid();
        commandQueue.Enqueue(new RuntimeCommandEnvelope { Type = RuntimeCommandType.ResetJourney, JourneyId = journeyId });
        return Accepted();
    }

    private bool IsLocalhostRequest() => HttpContext.Connection.RemoteIpAddress is { } remote
        && (IPAddress.IsLoopback(remote) || remote.Equals(HttpContext.Connection.LocalIpAddress));
}
