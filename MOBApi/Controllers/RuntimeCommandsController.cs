// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Common.Runtime;

using Domain;

using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Service;
using System.Text.Json.Serialization;

/// <summary>
/// REST fallback for remote runtime commands when SignalR forwarding is unavailable.
/// </summary>
[ApiController]
[Route("api/runtime/commands")]
public class RuntimeCommandsController : ControllerBase
{
    private readonly IRuntimeCommandAdmission _commandAdmission;

    public RuntimeCommandsController(IRuntimeCommandAdmission commandAdmission)
    {
        _commandAdmission = commandAdmission;
    }

    [HttpPost("signal-aspect")]
    public IActionResult EnqueueSignalAspect([FromBody] SetSignalAspectRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "SignalId is required." });
        }

        return _commandAdmission.Enqueue(new RuntimeCommandEnvelope
        {
            ProjectId = request.ProjectId,
            Type = RuntimeCommandType.SetSignalAspect,
            SignalId = request.SignalId,
            SignalAspect = request.Aspect
        }).ToActionResult(this);
    }

    [HttpPost("locomotive/drive")]
    public IActionResult EnqueueLocomotiveDrive([FromBody] SetLocomotiveDriveRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Address is required." });
        }

        return _commandAdmission.Enqueue(new RuntimeCommandEnvelope
        {
            ProjectId = request.ProjectId,
            Type = RuntimeCommandType.SetLocomotiveDrive,
            LocomotiveAddress = request.Address,
            Speed = request.Speed,
            Forward = request.Forward
        }).ToActionResult(this);
    }

    [HttpPost("locomotive/function")]
    public IActionResult EnqueueLocomotiveFunction([FromBody] SetLocomotiveFunctionRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Address is required." });
        }

        return _commandAdmission.Enqueue(new RuntimeCommandEnvelope
        {
            ProjectId = request.ProjectId,
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = request.Address,
            FunctionIndex = request.FunctionIndex,
            FunctionIsOn = request.IsOn
        }).ToActionResult(this);
    }

    [HttpGet("pending")]
    public IActionResult DequeuePending([FromServices] IRuntimeCommandQueue commandQueue)
    {
        ArgumentNullException.ThrowIfNull(commandQueue);

        if (!commandQueue.TryDequeue(out var command) || command == null)
        {
            return NoContent();
        }

        return Ok(command);
    }

    // Every field is required: a value left out of the request must not silently become 0, false or an empty id.
    public sealed record SetSignalAspectRequest(
        [property: JsonRequired] Guid ProjectId,
        [property: JsonRequired] Guid SignalId,
        [property: JsonRequired] SignalAspect Aspect);

    public sealed record SetLocomotiveDriveRequest(
        [property: JsonRequired] Guid ProjectId,
        [property: JsonRequired] int Address,
        [property: JsonRequired] int Speed,
        [property: JsonRequired] bool Forward);

    public sealed record SetLocomotiveFunctionRequest(
        [property: JsonRequired] Guid ProjectId,
        [property: JsonRequired] int Address,
        [property: JsonRequired] int FunctionIndex,
        [property: JsonRequired] bool IsOn);
}
