// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

/// <summary>
/// Safe credential metadata that never contains reusable secret material.
/// </summary>
public sealed record CredentialSnapshot(
    string CredentialId,
    string DisplayName,
    ControlPlaneRole Role,
    long CapabilityVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt,
    DateTimeOffset AbsoluteExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason)
{
    public bool IsRevoked => RevokedAt is not null;
}

/// <summary>
/// Returns a newly issued refresh credential exactly once.
/// </summary>
public sealed record IssuedCredential(CredentialSnapshot Credential, string RefreshToken);

/// <summary>
/// Identifies the outcome of a rotating refresh exchange.
/// </summary>
public enum RefreshRotationStatus
{
    Succeeded,
    Invalid,
    Expired,
    Revoked,
    ReplayDetected
}

/// <summary>
/// Contains the result of a rotating refresh exchange.
/// </summary>
public sealed record RefreshRotationResult(
    RefreshRotationStatus Status,
    CredentialSnapshot? Credential = null,
    string? RefreshToken = null);

/// <summary>
/// Represents live authorization state for access-token validation.
/// </summary>
public sealed record CredentialAuthorizationState(
    string CredentialId,
    string DisplayName,
    ControlPlaneRole Role,
    long CapabilityVersion,
    IReadOnlySet<string> Capabilities);