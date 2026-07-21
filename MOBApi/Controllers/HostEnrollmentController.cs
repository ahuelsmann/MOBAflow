// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net;
using Moba.Common.Security;
using Moba.MOBApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Moba.MOBApi.Controllers;

[ApiController]
[AllowAnonymous]
[RequireHttps]
[Route("api/control-plane/host")]
public sealed class HostEnrollmentController : ControllerBase
{
    private readonly IControlPlaneAccessTokenService _accessTokenService;
    private readonly IHostCredentialService _hostCredentialService;

    public HostEnrollmentController(
        IControlPlaneAccessTokenService accessTokenService,
        IHostCredentialService hostCredentialService)
    {
        _accessTokenService = accessTokenService;
        _hostCredentialService = hostCredentialService;
    }

    [HttpPost("bootstrap")]
    public async Task<ActionResult<HostTokenResponse>> Bootstrap(
        HostBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLoopbackRequest())
            return Forbid();

        var exchange = await _hostCredentialService
            .BootstrapAsync(request.Secret, cancellationToken)
            .ConfigureAwait(false);
        return await CreateResponseAsync(exchange, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("renew")]
    public async Task<ActionResult<HostTokenResponse>> Renew(
        HostRenewalRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLoopbackRequest())
            return Forbid();

        var exchange = await _hostCredentialService
            .RenewAsync(request.CredentialId, request.RenewalToken, cancellationToken)
            .ConfigureAwait(false);
        return await CreateResponseAsync(exchange, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ActionResult<HostTokenResponse>> CreateResponseAsync(
        HostCredentialExchangeResult exchange,
        CancellationToken cancellationToken)
    {
        if (exchange.Status == HostCredentialExchangeStatus.RateLimited)
            return StatusCode(StatusCodes.Status429TooManyRequests);
        if (exchange.Status != HostCredentialExchangeStatus.Succeeded ||
            exchange.CredentialId is null ||
            exchange.RenewalToken is null)
            return Unauthorized();

        var accessToken = await _accessTokenService
            .IssueAsync(exchange.CredentialId, cancellationToken)
            .ConfigureAwait(false);
        if (accessToken is null)
            return Unauthorized();

        return Ok(new HostTokenResponse(
            exchange.CredentialId,
            accessToken.Token,
            accessToken.ExpiresAt,
            exchange.RenewalToken));
    }

    private bool IsLoopbackRequest()
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
            return false;

        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();
        return IPAddress.IsLoopback(remoteIp);
    }
}