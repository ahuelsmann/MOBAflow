// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Common.Runtime;
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Service;

[ApiController]
[Route("api/runtime/journeys/{journeyId:guid}/feedback-progress")]
public sealed class JourneyProgressController(IRuntimeSnapshotCache snapshotCache, IRuntimeCommandAdmission commandAdmission) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(Guid journeyId)
    {
        var found = FindJourney(journeyId);
        return found is null
            ? NotFound(new { error = "Journey runtime state not found." })
            : Ok(found.Value.State);
    }

    [HttpPost("reset")]
    public IActionResult Reset(Guid journeyId)
    {
        // Journeys belong to exactly one project; its runtime resets the journey.
        var found = FindJourney(journeyId);
        if (found is null)
            return NotFound(new { error = "Journey runtime state not found." });

        return commandAdmission
            .Enqueue(new RuntimeCommandEnvelope
            {
                ProjectId = found.Value.ProjectId,
                Type = RuntimeCommandType.ResetJourney,
                JourneyId = journeyId
            })
            .ToActionResult(this);
    }

    private (Guid ProjectId, JourneyRuntimeSnapshot State)? FindJourney(Guid journeyId)
    {
        foreach (var entry in snapshotCache.GetAll())
        {
            var snapshot = RuntimeJsonSerializer.Deserialize(entry.Json);
            if (snapshot != null && snapshot.JourneyStates.TryGetValue(journeyId, out var state))
                return (entry.ProjectId, state);
        }

        return null;
    }
}
