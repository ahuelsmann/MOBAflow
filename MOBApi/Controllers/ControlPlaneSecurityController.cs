// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Security;

namespace Moba.MOBApi.Controllers;

[ApiController]
[Route("api/control-plane/token")]
public sealed class ControlPlaneTokenController : ControllerBase
{
    private readonly IControlPlaneAccessTokenService _accessTokenService;
    private readonly ICredentialRegistry _credentialRegistry;

    public ControlPlaneTokenController(
        IControlPlaneAccessTokenService accessTokenService,
        ICredentialRegistry credentialRegistry)
    {
        _accessTokenService = accessTokenService;
        _credentialRegistry = credentialRegistry;
    }

    [AllowAnonymous]
    [RequireHttps]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var rotation = await _credentialRegistry
            .RotateAsync(request.CredentialId, request.RefreshToken, cancellationToken)
            .ConfigureAwait(false);
        if (rotation.Status != RefreshRotationStatus.Succeeded || rotation.Credential is null || rotation.RefreshToken is null)
            return Unauthorized();

        return await CreateTokenResponseAsync(rotation.Credential, rotation.RefreshToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ActionResult<TokenResponse>> CreateTokenResponseAsync(
        CredentialSnapshot credential,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var accessToken = await _accessTokenService
            .IssueAsync(credential.CredentialId, cancellationToken)
            .ConfigureAwait(false);
        if (accessToken is null)
            return Unauthorized();

        return Ok(TokenResponse.Create(credential, refreshToken, accessToken));
    }
}

[ApiController]
[Route("api/control-plane/pairing")]
public sealed class ControlPlanePairingController : ControllerBase
{
    private readonly IControlPlaneAccessTokenService _accessTokenService;
    private readonly IPairingService _pairingService;

    public ControlPlanePairingController(
        IControlPlaneAccessTokenService accessTokenService,
        IPairingService pairingService)
    {
        _accessTokenService = accessTokenService;
        _pairingService = pairingService;
    }

    [AllowAnonymous]
    [RequireHttps]
    [HttpPost("submit")]
    public async Task<ActionResult<PairingSubmissionResult>> Submit(
        PairingSubmission request,
        CancellationToken cancellationToken)
    {
        var result = await _pairingService.SubmitAsync(request, cancellationToken).ConfigureAwait(false);
        return result.Status == PairingSubmissionStatus.Accepted ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous]
    [RequireHttps]
    [HttpPost("claim")]
    public async Task<ActionResult<TokenResponse>> Claim(
        PairingClaimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _pairingService
            .ClaimAsync(request.RequestId, request.ClaimToken, cancellationToken)
            .ConfigureAwait(false);
        if (result.Status == PairingClaimStatus.PendingApproval)
            return Accepted(result);
        if (result.Status != PairingClaimStatus.Succeeded || result.Credential is null)
            return BadRequest(result);

        var accessToken = await _accessTokenService
            .IssueAsync(result.Credential.Credential.CredentialId, cancellationToken)
            .ConfigureAwait(false);
        if (accessToken is null)
            return Unauthorized();

        return Ok(TokenResponse.Create(result.Credential.Credential, result.Credential.RefreshToken, accessToken));
    }
}

[ApiController]
[Authorize(Policy = ControlPlaneCapabilities.SecurityManage)]
[RequireHttps]
[Route("api/control-plane/security")]
public sealed class ControlPlaneSecurityController : ControllerBase
{
    private readonly ICredentialRegistry _credentialRegistry;
    private readonly IPairingService _pairingService;
    private readonly ICompatibilityStatusProvider _compatibilityStatusProvider;

    public ControlPlaneSecurityController(
        ICredentialRegistry credentialRegistry,
        IPairingService pairingService,
        ICompatibilityStatusProvider compatibilityStatusProvider)
    {
        _credentialRegistry = credentialRegistry;
        _pairingService = pairingService;
        _compatibilityStatusProvider = compatibilityStatusProvider;
    }

    [HttpGet("compatibility")]
    [ProducesResponseType(typeof(CompatibilityStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetCompatibilityStatus() =>
        Ok(_compatibilityStatusProvider.GetStatus());

    [HttpPost("pairing/open")]
    public async Task<ActionResult<PairingWindowResult>> OpenPairing(
        OpenPairingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _pairingService.OpenAsync(request.AllowedRole, cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message });
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    [HttpPost("pairing/cancel")]
    public async Task<IActionResult> CancelPairing(CancellationToken cancellationToken)
    {
        await _pairingService.CancelAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("pairing/requests")]
    public async Task<ActionResult<IReadOnlyList<PendingPairingRequest>>> GetPairingRequests(
        CancellationToken cancellationToken) =>
        Ok(await _pairingService.ListPendingAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("pairing/requests/{requestId}/approve")]
    public async Task<IActionResult> ApprovePairing(string requestId, CancellationToken cancellationToken) =>
        await _pairingService.ApproveAsync(requestId, cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpPost("pairing/requests/{requestId}/reject")]
    public async Task<IActionResult> RejectPairing(string requestId, CancellationToken cancellationToken) =>
        await _pairingService.RejectAsync(requestId, cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpGet("credentials")]
    public async Task<ActionResult<IReadOnlyList<CredentialSnapshot>>> GetCredentials(CancellationToken cancellationToken) =>
        Ok(await _credentialRegistry.ListAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("credentials/{credentialId}/revoke")]
    public async Task<IActionResult> RevokeCredential(
        string credentialId,
        RevokeCredentialRequest request,
        CancellationToken cancellationToken) =>
        await _credentialRegistry.RevokeAsync(credentialId, request.Reason, cancellationToken).ConfigureAwait(false)
            ? NoContent()
            : NotFound();

    [HttpPut("credentials/{credentialId}/role")]
    public async Task<IActionResult> ChangeCredentialRole(
        string credentialId,
        ChangeCredentialRoleRequest request,
        CancellationToken cancellationToken) =>
        await _credentialRegistry.ChangeRoleAsync(credentialId, request.Role, cancellationToken).ConfigureAwait(false)
            ? NoContent()
            : NotFound();
}

public sealed record RefreshTokenRequest(string CredentialId, string RefreshToken);

public sealed record PairingClaimRequest(string RequestId, string ClaimToken);

public sealed record OpenPairingRequest(ControlPlaneRole AllowedRole);

public sealed record RevokeCredentialRequest(string Reason);

public sealed record ChangeCredentialRoleRequest(ControlPlaneRole Role);

public sealed record TokenResponse(
    string CredentialId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    ControlPlaneRole Role,
    long CapabilityVersion)
{
    public static TokenResponse Create(
        CredentialSnapshot credential,
        string refreshToken,
        IssuedAccessToken accessToken) => new(
        credential.CredentialId,
        accessToken.Token,
        accessToken.ExpiresAt,
        refreshToken,
        credential.Role,
        credential.CapabilityVersion);
}