// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Common.Runtime;

using Common.Validation;
using Domain;

using Hubs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Moba.MOBApi.Security;

using Service;

using System.Net;
using System.Text.Json;

/// <summary>
/// REST API for syncing the MOBAflow solution to MOBAsmart clients.
/// WinUI pushes via PUT (localhost only); mobile clients read via GET.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SolutionController : ControllerBase
{
    private const int SolutionSchemaVersion = Solution.CurrentSchemaVersion;

    private readonly ISolutionCache _solutionCache;
    private readonly IHubContext<RuntimeHub> _hubContext;

    public SolutionController(ISolutionCache solutionCache, IHubContext<RuntimeHub> hubContext)
    {
        _solutionCache = solutionCache;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Returns metadata for polling without transferring the full solution JSON.
    /// </summary>
    [HttpGet("meta")]
    [Authorize(Policy = ControlPlaneCapabilities.Read)]
    public IActionResult GetMeta()
    {
        if (!_solutionCache.TryGet(out var entry))
        {
            return NotFound(new { error = "No solution available yet." });
        }

        return Ok(BuildMetaResponse(entry));
    }

    /// <summary>
    /// Returns the cached solution JSON for MOBAsmart.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ControlPlaneCapabilities.Read)]
    public IActionResult GetSolution()
    {
        if (!_solutionCache.TryGet(out var entry))
        {
            return NotFound(new { error = "No solution available yet." });
        }

        return Content(entry.Json, "application/json");
    }

    /// <summary>
    /// Receives solution JSON from MOBAflow WinUI (localhost only).
    /// </summary>
    [HttpPut]
    [Authorize(Policy = ControlPlaneCapabilities.HostPublish)]
    public async Task<IActionResult> PutSolution(CancellationToken cancellationToken)
    {
        if (!IsLocalhostRequest())
        {
            return Forbid();
        }

        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var validationResult = JsonValidationService.Validate(json, SolutionSchemaVersion);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { error = validationResult.ErrorMessage });
        }

        var sourcePath = Request.Headers.TryGetValue("X-MOBAflow-Solution-Path", out var pathValues)
            ? pathValues.FirstOrDefault()
            : null;
        var activeProjectName = Request.Headers.TryGetValue("X-MOBAflow-Active-Project", out var projectValues)
            ? projectValues.FirstOrDefault()
            : null;

        _solutionCache.Set(json, sourcePath, activeProjectName);

        if (!_solutionCache.TryGet(out var entry))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to cache solution." });
        }

        await _hubContext.Clients
            .Group("runtime-remote")
            .SendAsync(RuntimeHubMethods.SolutionUpdated, entry.UpdatedAt.ToString("O"), cancellationToken)
            .ConfigureAwait(false);

        return Ok(BuildMetaResponse(entry));
    }

    private static object BuildMetaResponse(SolutionCacheEntry entry)
    {
        string? name = null;
        int? schemaVersion = null;
        string? firstProjectName = null;

        try
        {
            using var document = JsonDocument.Parse(entry.Json);
            var root = document.RootElement;
            if (root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString();
            }

            if (root.TryGetProperty("schemaVersion", out var versionElement) &&
                versionElement.ValueKind == JsonValueKind.Number &&
                versionElement.TryGetInt32(out var version))
            {
                schemaVersion = version;
            }

            if (root.TryGetProperty("projects", out var projectsElement) &&
                projectsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var project in projectsElement.EnumerateArray())
                {
                    if (project.TryGetProperty("name", out var projectNameElement) &&
                        projectNameElement.ValueKind == JsonValueKind.String)
                    {
                        firstProjectName = projectNameElement.GetString();
                        break;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Meta is best-effort; full JSON was already validated on PUT.
        }

        return new
        {
            updatedAt = entry.UpdatedAt,
            sourcePath = entry.SourcePath,
            activeProjectName = entry.ActiveProjectName,
            name,
            schemaVersion,
            firstProjectName = entry.ActiveProjectName ?? firstProjectName
        };
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
