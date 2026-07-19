// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Domain;
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Service;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/projects/{projectId:guid}/journeys/{journeyId:guid}/feedback-sequence")]
public sealed class FeedbackSequencesController(ISolutionCache solutionCache) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(Guid projectId, Guid journeyId)
    {
        if (!TryLoad(out var solution, out _, out var error)) return error!;
        var journey = FindJourney(solution!, projectId, journeyId);
        if (journey == null) return NotFound(new { error = "Project or journey not found." });
        Response.Headers.ETag = BuildEtag(journey.FeedbackSequence);
        return Ok(journey.FeedbackSequence);
    }

    [HttpPut]
    public IActionResult Put(Guid projectId, Guid journeyId, [FromBody] List<JourneyFeedbackStep>? steps)
    {
        if (!IsLocalhostRequest()) return Forbid();
        if (steps == null) return BadRequest(new { error = "Feedback sequence is required." });
        if (!TryLoad(out var solution, out var entry, out var error)) return error!;
        var journey = FindJourney(solution!, projectId, journeyId);
        if (journey == null) return NotFound(new { error = "Project or journey not found." });
        var currentEtag = BuildEtag(journey.FeedbackSequence);
        if (Request.Headers.IfMatch.Count > 0 && !Request.Headers.IfMatch.Any(value => value == currentEtag))
            return StatusCode(StatusCodes.Status412PreconditionFailed, new { error = "Feedback sequence has changed." });

        var validationError = Validate(steps, solution!.Projects.Single(project => project.Id == projectId), journey);
        if (validationError != null) return UnprocessableEntity(new { error = validationError });
        journey.FeedbackSequence = steps;
        solution.SchemaVersion = Solution.CurrentSchemaVersion;
        var json = JsonSerializer.Serialize(solution, Moba.Domain.JsonOptions.Default);
        solutionCache.Set(json, entry!.SourcePath, entry.ActiveProjectName);
        Response.Headers.ETag = BuildEtag(steps);
        return Ok(steps);
    }

    private bool TryLoad(out Solution? solution, out SolutionCacheEntry? entry, out IActionResult? error)
    {
        solution = null; entry = null; error = null;
        if (!solutionCache.TryGet(out var cached)) { error = NotFound(new { error = "No solution available yet." }); return false; }
        entry = cached;
        solution = JsonSerializer.Deserialize<Solution>(cached.Json, Moba.Domain.JsonOptions.Default);
        if (solution == null) { error = StatusCode(500, new { error = "Cached solution is invalid." }); return false; }
        SolutionMigrator.MigrateToCurrent(solution);
        return true;
    }

    private static Journey? FindJourney(Solution solution, Guid projectId, Guid journeyId) =>
        solution.Projects.FirstOrDefault(project => project.Id == projectId)?.Journeys.FirstOrDefault(journey => journey.Id == journeyId);

    private static string? Validate(IEnumerable<JourneyFeedbackStep> steps, Project project, Journey journey)
    {
        foreach (var step in steps)
        {
            if (step.InPort is < 1 or > 512) return "InPort must be between 1 and 512.";
            if (step.RepeatCount < 1) return "RepeatCount must be at least 1.";
            if (step.DelayMs < 0) return "DelayMs cannot be negative.";
            if (step.WorkflowId.HasValue && project.Workflows.All(workflow => workflow.Id != step.WorkflowId)) return "Workflow does not exist.";
            if (step.StopTransition.StationId.HasValue && journey.Stations.All(station => station.Id != step.StopTransition.StationId)) return "Target stop does not exist.";
        }
        return null;
    }

    private static string BuildEtag(object value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Moba.Domain.JsonOptions.Compact)));
        return $"\"{Convert.ToHexString(hash)}\"";
    }

    private bool IsLocalhostRequest() => HttpContext.Connection.RemoteIpAddress is { } remote
        && (IPAddress.IsLoopback(remote) || remote.Equals(HttpContext.Connection.LocalIpAddress));
}
