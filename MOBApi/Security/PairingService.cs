// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

/// <summary>
/// Coordinates one short-lived, explicitly approved local pairing ceremony.
/// </summary>
public interface IPairingService
{
    Task<PairingWindowResult> OpenAsync(ControlPlaneRole allowedRole, CancellationToken cancellationToken = default);

    Task CancelAsync(CancellationToken cancellationToken = default);

    Task<PairingSubmissionResult> SubmitAsync(PairingSubmission submission, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingPairingRequest>> ListPendingAsync(CancellationToken cancellationToken = default);

    Task<bool> ApproveAsync(string requestId, CancellationToken cancellationToken = default);

    Task<bool> RejectAsync(string requestId, CancellationToken cancellationToken = default);

    Task<PairingClaimResult> ClaimAsync(string requestId, string claimToken, CancellationToken cancellationToken = default);
}

public sealed record PairingWindowResult(
    string PairingSecret,
    string ServerPublicKeyFingerprint,
    DateTimeOffset ExpiresAt);

public sealed record PairingSubmission(
    string PairingSecret,
    string ClientNonce,
    string DisplayName,
    ControlPlaneRole RequestedRole);

public enum PairingSubmissionStatus
{
    Accepted,
    Invalid,
    Expired,
    Closed,
    RoleNotAllowed,
    ReplayDetected,
    Cooldown
}

public sealed record PairingSubmissionResult(
    PairingSubmissionStatus Status,
    string? RequestId = null,
    string? ClaimToken = null,
    string? ConfirmationCode = null);

public sealed record PendingPairingRequest(
    string RequestId,
    string DisplayName,
    ControlPlaneRole RequestedRole,
    string ConfirmationCode,
    DateTimeOffset CreatedAt,
    string Status);

public enum PairingClaimStatus
{
    Succeeded,
    PendingApproval,
    Rejected,
    Invalid,
    Expired,
    AlreadyClaimed
}

public sealed record PairingClaimResult(PairingClaimStatus Status, IssuedCredential? Credential = null);

internal sealed class PairingService : IPairingService
{
    private const int SecretLength = 43;
    private const int MaximumNonceLength = 128;
    private const int MaximumDisplayNameLength = 100;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ICredentialRegistry _credentialRegistry;
    private readonly IServerIdentityProvider _identityProvider;
    private readonly ControlPlaneSecurityOptions _options;
    private readonly TimeProvider _timeProvider;
    private PairingWindow? _window;
    private DateTimeOffset? _cooldownUntil;

    public PairingService(
        ICredentialRegistry credentialRegistry,
        IServerIdentityProvider identityProvider,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider)
    {
        _credentialRegistry = credentialRegistry;
        _identityProvider = identityProvider;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<PairingWindowResult> OpenAsync(
        ControlPlaneRole allowedRole,
        CancellationToken cancellationToken = default)
    {
        EnsurePairableRole(allowedRole);
        var identity = await _identityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            if (_cooldownUntil > now)
                throw new InvalidOperationException("Pairing is in cooldown after repeated failed attempts.");

            var secret = CreateSecret();
            _window = new PairingWindow
            {
                SecretHash = Hash(secret),
                AllowedRole = allowedRole,
                ExpiresAt = now.Add(_options.PairingWindowLifetime)
            };
            return new PairingWindowResult(secret, identity.PublicKeyFingerprint, _window.ExpiresAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _window = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PairingSubmissionResult> SubmitAsync(
        PairingSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return SubmitLocked(submission, _timeProvider.GetUtcNow());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<PendingPairingRequest>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_window is null)
                return [];

            return _window.Requests.Values.Select(ToPending).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<bool> ApproveAsync(string requestId, CancellationToken cancellationToken = default) =>
        SetDecisionAsync(requestId, PairingDecision.Approved, cancellationToken);

    public Task<bool> RejectAsync(string requestId, CancellationToken cancellationToken = default) =>
        SetDecisionAsync(requestId, PairingDecision.Rejected, cancellationToken);

    public async Task<PairingClaimResult> ClaimAsync(
        string requestId,
        string claimToken,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = FindRequest(requestId);
            var status = ValidateClaim(request, claimToken, _timeProvider.GetUtcNow());
            if (status != PairingClaimStatus.Succeeded || request is null)
                return new PairingClaimResult(status);

            var credential = await _credentialRegistry
                .CreateAsync(request.DisplayName, request.RequestedRole, cancellationToken)
                .ConfigureAwait(false);
            request.Claimed = true;
            return new PairingClaimResult(PairingClaimStatus.Succeeded, credential);
        }
        finally
        {
            _gate.Release();
        }
    }

    private PairingSubmissionResult SubmitLocked(PairingSubmission submission, DateTimeOffset now)
    {
        if (_cooldownUntil > now)
            return new PairingSubmissionResult(PairingSubmissionStatus.Cooldown);
        if (_window is null || !_window.AcceptingSubmissions)
            return new PairingSubmissionResult(PairingSubmissionStatus.Closed);
        if (now >= _window.ExpiresAt)
            return new PairingSubmissionResult(PairingSubmissionStatus.Expired);
        if (!IsValidSubmission(submission))
            return new PairingSubmissionResult(PairingSubmissionStatus.Invalid);
        if (!FixedTimeEquals(_window.SecretHash, Hash(submission.PairingSecret)))
            return RecordFailedAttempt(now);
        if (!IsAllowedRole(_window.AllowedRole, submission.RequestedRole))
            return new PairingSubmissionResult(PairingSubmissionStatus.RoleNotAllowed);
        if (_window.ClientNonces.Contains(submission.ClientNonce))
            return new PairingSubmissionResult(PairingSubmissionStatus.ReplayDetected);

        return AcceptSubmission(submission, now);
    }

    private PairingSubmissionResult AcceptSubmission(PairingSubmission submission, DateTimeOffset now)
    {
        var claimToken = CreateSecret();
        var request = new PairingRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            ClaimTokenHash = Hash(claimToken),
            DisplayName = submission.DisplayName.Trim(),
            RequestedRole = submission.RequestedRole,
            ConfirmationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(System.Globalization.CultureInfo.InvariantCulture),
            CreatedAt = now
        };
        _window!.ClientNonces.Add(submission.ClientNonce);
        _window.Requests.Add(request.RequestId, request);
        _window.AcceptingSubmissions = false;
        return new PairingSubmissionResult(
            PairingSubmissionStatus.Accepted,
            request.RequestId,
            claimToken,
            request.ConfirmationCode);
    }

    private PairingSubmissionResult RecordFailedAttempt(DateTimeOffset now)
    {
        _window!.FailedAttempts++;
        if (_window.FailedAttempts < _options.PairingMaximumFailedAttempts)
            return new PairingSubmissionResult(PairingSubmissionStatus.Invalid);

        _window = null;
        _cooldownUntil = now.Add(_options.PairingCooldown);
        return new PairingSubmissionResult(PairingSubmissionStatus.Cooldown);
    }

    private async Task<bool> SetDecisionAsync(
        string requestId,
        PairingDecision decision,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = FindRequest(requestId);
            if (request is null || request.Claimed || request.Decision != PairingDecision.Pending)
                return false;

            request.Decision = decision;
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private PairingClaimStatus ValidateClaim(PairingRequest? request, string claimToken, DateTimeOffset now)
    {
        if (_window is null || now >= _window.ExpiresAt)
            return PairingClaimStatus.Expired;
        if (claimToken?.Length != SecretLength)
            return PairingClaimStatus.Invalid;
        if (request is null || !FixedTimeEquals(request.ClaimTokenHash, Hash(claimToken)))
            return PairingClaimStatus.Invalid;
        if (request.Claimed)
            return PairingClaimStatus.AlreadyClaimed;
        return request.Decision switch
        {
            PairingDecision.Pending => PairingClaimStatus.PendingApproval,
            PairingDecision.Rejected => PairingClaimStatus.Rejected,
            PairingDecision.Approved => PairingClaimStatus.Succeeded,
            _ => PairingClaimStatus.Invalid
        };
    }

    private PairingRequest? FindRequest(string requestId)
    {
        if (_window is null)
            return null;

        return _window.Requests.GetValueOrDefault(requestId);
    }

    private static PendingPairingRequest ToPending(PairingRequest request) => new(
        request.RequestId,
        request.DisplayName,
        request.RequestedRole,
        request.ConfirmationCode,
        request.CreatedAt,
        request.Claimed ? "Claimed" : request.Decision.ToString());

    private static bool IsAllowedRole(ControlPlaneRole allowed, ControlPlaneRole requested) =>
        requested == ControlPlaneRole.ReadOnly ||
        allowed == ControlPlaneRole.RemoteControl && requested == ControlPlaneRole.RemoteControl;

    private static bool IsValidSubmission(PairingSubmission submission) =>
        submission.PairingSecret?.Length == SecretLength &&
        !string.IsNullOrWhiteSpace(submission.ClientNonce) &&
        submission.ClientNonce.Length <= MaximumNonceLength &&
        !string.IsNullOrWhiteSpace(submission.DisplayName) &&
        submission.DisplayName.Length <= MaximumDisplayNameLength;

    private static void EnsurePairableRole(ControlPlaneRole role)
    {
        if (role == ControlPlaneRole.Host)
            throw new ArgumentOutOfRangeException(nameof(role), role, "Host credentials are enrolled separately.");
    }

    private static string CreateSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool FixedTimeEquals(string first, string second) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(first), Convert.FromHexString(second));

    private sealed class PairingWindow
    {
        public string SecretHash { get; init; } = string.Empty;
        public ControlPlaneRole AllowedRole { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public int FailedAttempts { get; set; }
        public bool AcceptingSubmissions { get; set; } = true;
        public HashSet<string> ClientNonces { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, PairingRequest> Requests { get; } = new(StringComparer.Ordinal);
    }

    private sealed class PairingRequest
    {
        public string RequestId { get; init; } = string.Empty;
        public string ClaimTokenHash { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public ControlPlaneRole RequestedRole { get; init; }
        public string ConfirmationCode { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public PairingDecision Decision { get; set; }
        public bool Claimed { get; set; }
    }

    private enum PairingDecision
    {
        Pending,
        Approved,
        Rejected
    }
}