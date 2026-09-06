// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

/// <summary>
/// Stores rotating device credentials in a purpose-protected local registry.
/// </summary>
public interface ICredentialRegistry
{
    Task<IssuedCredential> CreateAsync(string displayName, ControlPlaneRole role, CancellationToken cancellationToken = default);

    Task<RefreshRotationResult> RotateAsync(string credentialId, string refreshToken, CancellationToken cancellationToken = default);

    Task<CredentialAuthorizationState?> GetAuthorizationStateAsync(string credentialId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CredentialSnapshot>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(string credentialId, string reason, CancellationToken cancellationToken = default);

    Task<bool> ChangeRoleAsync(string credentialId, ControlPlaneRole role, CancellationToken cancellationToken = default);
}

internal sealed class CredentialRegistry : ICredentialRegistry
{
    private const string RegistryPurpose = "MOBApi.ControlPlane.CredentialRegistry.v1";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IControlPlaneConnectionRevoker _connectionRevoker;
    private readonly ProtectedDocumentStore<CredentialRegistryDocument> _store;
    private readonly ControlPlaneSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    public CredentialRegistry(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider,
        IControlPlaneConnectionRevoker connectionRevoker)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _connectionRevoker = connectionRevoker;
        var path = Path.Combine(_options.ResolveStorageDirectory(), "credentials.dat");
        _store = new ProtectedDocumentStore<CredentialRegistryDocument>(dataProtectionProvider, RegistryPurpose, path);
    }

    public async Task<IssuedCredential> CreateAsync(
        string displayName,
        ControlPlaneRole role,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadInitializedAsync(cancellationToken).ConfigureAwait(false);
            var now = _timeProvider.GetUtcNow();
            var refreshToken = CreateSecret();
            var record = CreateRecord(document, displayName.Trim(), role, refreshToken, now);
            document.Credentials.Add(record);
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            return new IssuedCredential(ToSnapshot(record), refreshToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RefreshRotationResult> RotateAsync(
        string credentialId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (credentialId?.Length != 32 || refreshToken?.Length != 43)
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadInitializedAsync(cancellationToken).ConfigureAwait(false);
            var record = Find(document, credentialId);
            return await RotateRecordAsync(document, record, refreshToken, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CredentialAuthorizationState?> GetAuthorizationStateAsync(
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadInitializedAsync(cancellationToken).ConfigureAwait(false);
            var record = Find(document, credentialId);
            if (record is null || record.RevokedAt is not null || IsExpired(record, _timeProvider.GetUtcNow()))
                return null;

            return new CredentialAuthorizationState(
                record.CredentialId,
                record.DisplayName,
                record.Role,
                record.CapabilityVersion,
                ControlPlaneCapabilities.ForRole(record.Role));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<CredentialSnapshot>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadInitializedAsync(cancellationToken).ConfigureAwait(false);
            return document.Credentials.Select(ToSnapshot).OrderBy(x => x.DisplayName, StringComparer.Ordinal).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> RevokeAsync(
        string credentialId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 256)
            return false;

        var revoked = await MutateAsync(credentialId, record =>
        {
            if (record.RevokedAt is not null)
                return false;

            record.RevokedAt = _timeProvider.GetUtcNow();
            record.RevocationReason = reason.Trim();
            return true;
        }, cancellationToken).ConfigureAwait(false);
        if (revoked)
            _connectionRevoker.Revoke(credentialId);
        return revoked;
    }

    public async Task<bool> ChangeRoleAsync(
        string credentialId,
        ControlPlaneRole role,
        CancellationToken cancellationToken = default)
    {
        if (role == ControlPlaneRole.Host)
            return false;

        var changed = await MutateAsync(credentialId, record =>
        {
            if (record.RevokedAt is not null || record.Role == role)
                return false;

            record.Role = role;
            record.CapabilityVersion++;
            return true;
        }, cancellationToken).ConfigureAwait(false);
        if (changed)
            _connectionRevoker.Revoke(credentialId);
        return changed;
    }

    private async Task<bool> MutateAsync(
        string credentialId,
        Func<CredentialRecord, bool> mutation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadInitializedAsync(cancellationToken).ConfigureAwait(false);
            var record = Find(document, credentialId);
            if (record is null || !mutation(record))
                return false;

            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<RefreshRotationResult> RotateRecordAsync(
        CredentialRegistryDocument document,
        CredentialRecord? record,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (record is null)
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);
        if (record.RevokedAt is not null)
            return new RefreshRotationResult(RefreshRotationStatus.Revoked);

        var now = _timeProvider.GetUtcNow();
        if (IsExpired(record, now))
            return new RefreshRotationResult(RefreshRotationStatus.Expired);

        var candidateHash = HashRefreshToken(document, refreshToken);
        if (record.ConsumedRefreshHashes.Any(hash => FixedTimeEquals(hash, candidateHash)))
            return await RevokeReplayAsync(document, record, now, cancellationToken).ConfigureAwait(false);
        if (!FixedTimeEquals(record.RefreshTokenHash, candidateHash))
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);

        return await CompleteRotationAsync(document, record, now, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RefreshRotationResult> CompleteRotationAsync(
        CredentialRegistryDocument document,
        CredentialRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        record.ConsumedRefreshHashes.Add(record.RefreshTokenHash);
        var refreshToken = CreateSecret();
        record.RefreshTokenHash = HashRefreshToken(document, refreshToken);
        record.LastUsedAt = now;
        await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return new RefreshRotationResult(RefreshRotationStatus.Succeeded, ToSnapshot(record), refreshToken);
    }

    private async Task<RefreshRotationResult> RevokeReplayAsync(
        CredentialRegistryDocument document,
        CredentialRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        record.RevokedAt = now;
        record.RevocationReason = "refresh_replay";
        await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        _connectionRevoker.Revoke(record.CredentialId);
        return new RefreshRotationResult(RefreshRotationStatus.ReplayDetected);
    }

    private async Task<CredentialRegistryDocument> LoadInitializedAsync(CancellationToken cancellationToken)
    {
        var document = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(document.HashKey))
            return document;

        document.HashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return document;
    }

    private CredentialRecord CreateRecord(
        CredentialRegistryDocument document,
        string displayName,
        ControlPlaneRole role,
        string refreshToken,
        DateTimeOffset now)
    {
        if (displayName.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(displayName), "Credential display name cannot exceed 100 characters.");

        return new CredentialRecord
        {
            CredentialId = Guid.NewGuid().ToString("N"),
            DisplayName = displayName,
            Role = role,
            CapabilityVersion = 1,
            RefreshTokenHash = HashRefreshToken(document, refreshToken),
            CreatedAt = now,
            LastUsedAt = now,
            AbsoluteExpiresAt = now.Add(_options.RefreshAbsoluteLifetime)
        };
    }

    private string HashRefreshToken(CredentialRegistryDocument document, string refreshToken)
    {
        var key = Convert.FromBase64String(document.HashKey);
        var bytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
        return Convert.ToHexString(HMACSHA256.HashData(key, bytes));
    }

    private bool IsExpired(CredentialRecord record, DateTimeOffset now) =>
        now >= record.AbsoluteExpiresAt || now >= record.LastUsedAt.Add(_options.RefreshInactivityLifetime);

    private static CredentialRecord? Find(CredentialRegistryDocument document, string credentialId) =>
        document.Credentials.SingleOrDefault(x => string.Equals(x.CredentialId, credentialId, StringComparison.Ordinal));

    private static string CreateSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static bool FixedTimeEquals(string first, string second) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(first), Convert.FromHexString(second));

    private static CredentialSnapshot ToSnapshot(CredentialRecord record) => new(
        record.CredentialId,
        record.DisplayName,
        record.Role,
        record.CapabilityVersion,
        record.CreatedAt,
        record.LastUsedAt,
        record.AbsoluteExpiresAt,
        record.RevokedAt,
        record.RevocationReason);

    private sealed class CredentialRegistryDocument
    {
        public string HashKey { get; set; } = string.Empty;

        public List<CredentialRecord> Credentials { get; set; } = [];
    }

    private sealed class CredentialRecord
    {
        public string CredentialId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ControlPlaneRole Role { get; set; }
        public long CapabilityVersion { get; set; }
        public string RefreshTokenHash { get; set; } = string.Empty;
        public List<string> ConsumedRefreshHashes { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastUsedAt { get; set; }
        public DateTimeOffset AbsoluteExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
    }
}
