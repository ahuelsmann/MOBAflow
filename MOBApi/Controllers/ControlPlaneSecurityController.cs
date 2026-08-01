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
    private readonly ICompatibilityReadMigration _readMigration;
    private readonly IPairingService _pairingService;

    public ControlPlaneSecurityController(
        ICredentialRegistry credentialRegistry,
        IPairingService pairingService,
        ICompatibilityReadMigration readMigration)
    {
        _credentialRegistry = credentialRegistry;
        _pairingService = pairingService;
        _readMigration = readMigration;
    }

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

    [HttpPost("read-migration/window")]
    public async Task<IActionResult> BeginReadinessWindow(
        BeginReadinessWindowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _readMigration
                .BeginReadinessWindowAsync(request.StableClientRelease, cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return exception is InvalidOperationException
                ? Conflict(new ProblemDetails { Title = exception.Message })
                : BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    [HttpGet("read-migration")]
    public async Task<ActionResult<CompatibilityReadMigrationStatus>> GetReadMigrationStatus(
        CancellationToken cancellationToken) =>
        await _readMigration.GetStatusAsync(cancellationToken).ConfigureAwait(false);

    [HttpGet("read-migration/telemetry")]
    public async Task<ActionResult<CompatibilityReadTelemetry>> GetReadMigrationTelemetry(
        CancellationToken cancellationToken) =>
        await _readMigration.GetTelemetryAsync(cancellationToken).ConfigureAwait(false);

    [HttpPost("read-migration/critical-defect-fixed")]
    public async Task<IActionResult> RecordCriticalDefectFixed(
        RecordCriticalDefectFixedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _readMigration
                .RecordCriticalDefectFixedAsync(request.DefectCode, cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return exception is InvalidOperationException
                ? Conflict(new ProblemDetails { Title = exception.Message })
                : BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    [HttpPost("read-migration/critical-defect")]
    public async Task<IActionResult> RecordCriticalDefect(
        RecordCriticalDefectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _readMigration
                .RecordCriticalDefectAsync(request.DefectCode, cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return exception is InvalidOperationException
                ? Conflict(new ProblemDetails { Title = exception.Message })
                : BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    [HttpPost("read-migration/evidence")]
    public async Task<IActionResult> RecordReadinessEvidence(
        RecordReadinessEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _readMigration
                .RecordIssueEvidenceAsync(request.EvidenceReference, cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return exception is InvalidOperationException
                ? Conflict(new ProblemDetails { Title = exception.Message })
                : BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    [HttpPost("read-migration/enforce")]
    public async Task<IActionResult> EnableAuthenticatedReads(CancellationToken cancellationToken)
    {
        if (await _readMigration.EnableAuthenticatedReadsAsync(cancellationToken).ConfigureAwait(false))
            return NoContent();

        var status = await _readMigration.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Conflict(new ProblemDetails
        {
            Title = "Authenticated reads cannot be enforced yet.",
            Detail = status.BlockingReason.ToString()
        });
    }

    [HttpPost("read-migration/rollback")]
    public async Task<IActionResult> ActivateAnonymousReadRollback(
        ActivateAnonymousReadRollbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DurationHours is < 1 or > 168)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Anonymous read-only rollback must last between 1 and 168 hours."
            });
        }

        return await _readMigration
            .ActivateAnonymousReadRollbackAsync(TimeSpan.FromHours(request.DurationHours), cancellationToken)
            .ConfigureAwait(false)
            ? NoContent()
            : Conflict(new ProblemDetails
            {
                Title = "Anonymous read-only rollback requires authenticated-read enforcement."
            });
    }
}

public sealed record RefreshTokenRequest(string CredentialId, string RefreshToken);

public sealed record PairingClaimRequest(string RequestId, string ClaimToken);

public sealed record OpenPairingRequest(ControlPlaneRole AllowedRole);

public sealed record RevokeCredentialRequest(string Reason);

public sealed record ChangeCredentialRoleRequest(ControlPlaneRole Role);

/// <summary>Starts observation for a stable client release identifier.</summary>
public sealed record BeginReadinessWindowRequest(string StableClientRelease);

/// <summary>Records a critical defect that blocks migration readiness.</summary>
public sealed record RecordCriticalDefectRequest(string DefectCode);

/// <summary>Records a critical defect fix and restarts observation.</summary>
public sealed record RecordCriticalDefectFixedRequest(string DefectCode);

/// <summary>Links the migration gate to a concrete readiness-evidence comment in issue #50.</summary>
public sealed record RecordReadinessEvidenceRequest(string EvidenceReference);

/// <summary>Requests a time-bounded anonymous read-only rollback.</summary>
public sealed record ActivateAnonymousReadRollbackRequest(int DurationHours);

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