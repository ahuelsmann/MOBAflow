// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Common.Runtime;

using Domain;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Security;
using Moba.MOBApi.Service;

using System.Net;

/// <summary>
/// REST fallback for remote runtime commands when SignalR forwarding is unavailable.
/// </summary>
[ApiController]
[Route("api/runtime/commands")]
public class RuntimeCommandsController : ControllerBase
{
    private readonly IRuntimeCommandQueue _commandQueue;

    public RuntimeCommandsController(IRuntimeCommandQueue commandQueue)
    {
        _commandQueue = commandQueue;
    }

    [HttpPost("signal-aspect")]
    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public IActionResult EnqueueSignalAspect([FromBody] SetSignalAspectRequest? request)
    {
        if (request == null || request.SignalId == Guid.Empty)
        {
            return BadRequest(new { error = "SignalId is required." });
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetSignalAspect,
            SignalId = request.SignalId,
            SignalAspect = request.Aspect
        });

        return Accepted();
    }

    [HttpPost("locomotive/drive")]
    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public IActionResult EnqueueLocomotiveDrive([FromBody] SetLocomotiveDriveRequest? request)
    {
        if (request == null || request.Address <= 0)
        {
            return BadRequest(new { error = "Address is required." });
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveDrive,
            LocomotiveAddress = request.Address,
            Speed = request.Speed,
            Forward = request.Forward
        });

        return Accepted();
    }

    [HttpPost("locomotive/function")]
    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public IActionResult EnqueueLocomotiveFunction([FromBody] SetLocomotiveFunctionRequest? request)
    {
        if (request == null || request.Address <= 0)
        {
            return BadRequest(new { error = "Address is required." });
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = request.Address,
            FunctionIndex = request.FunctionIndex,
            FunctionIsOn = request.IsOn
        });

        return Accepted();
    }

    [HttpGet("pending")]
    [Authorize(Policy = ControlPlaneCapabilities.HostConsume)]
    public IActionResult DequeuePending()
    {
        if (!IsLocalhostRequest())
        {
            return Forbid();
        }

        if (!_commandQueue.TryDequeue(out var command) || command == null)
        {
            return NoContent();
        }

        return Ok(command);
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

    public sealed record SetSignalAspectRequest(Guid SignalId, SignalAspect Aspect);

    public sealed record SetLocomotiveDriveRequest(int Address, int Speed, bool Forward);

    public sealed record SetLocomotiveFunctionRequest(int Address, int FunctionIndex, bool IsOn);
}
