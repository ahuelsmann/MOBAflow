// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Cryptography;
using System.Text;
using Moba.Common.Security;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

public sealed record HostBootstrapMaterial(byte[] SecretHash, DateTimeOffset ExpiresAt, int ParentProcessId)
{
    public static HostBootstrapMaterial Unavailable { get; } = new([], DateTimeOffset.MinValue, 0);

    public static HostBootstrapMaterial Create(
        HostBootstrapPipeRequest request,
        TimeProvider timeProvider,
        TimeSpan lifetime) => new(
        SHA256.HashData(Encoding.UTF8.GetBytes(request.Secret)),
        timeProvider.GetUtcNow().Add(lifetime),
        request.ParentProcessId);
}

public enum HostCredentialExchangeStatus
{
    Succeeded,
    Invalid,
    Expired,
    ReplayDetected,
    RateLimited
}

public sealed record HostCredentialExchangeResult(
    HostCredentialExchangeStatus Status,
    string? CredentialId = null,
    string? RenewalToken = null);

public interface IHostCredentialService
{
    Task<HostCredentialExchangeResult> BootstrapAsync(string secret, CancellationToken cancellationToken = default);

    Task<HostCredentialExchangeResult> RenewAsync(
        string credentialId,
        string renewalToken,
        CancellationToken cancellationToken = default);

    Task<CredentialAuthorizationState?> GetAuthorizationStateAsync(
        string credentialId,
        CancellationToken cancellationToken = default);

    void ConfirmHostConnection();

    void BeginDisconnectGrace();

    void Revoke();
}

internal sealed class HostCredentialService : IHostCredentialService, IDisposable
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _disconnectGrace;
    private byte[]? _bootstrapHash;
    private readonly DateTimeOffset _bootstrapExpiresAt;
    private readonly int _maximumAttempts;
    private int _failedAttempts;
    private string? _credentialId;
    private byte[]? _renewalHash;
    private byte[]? _consumedRenewalHash;
    private CancellationTokenSource? _disconnectCts;
    private bool _revoked;

    public HostCredentialService(
        HostBootstrapMaterial bootstrapMaterial,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _disconnectGrace = options.Value.HostDisconnectGrace;
        _maximumAttempts = options.Value.HostBootstrapMaximumFailedAttempts;
        _bootstrapHash = bootstrapMaterial.SecretHash.Length == 0 ? null : bootstrapMaterial.SecretHash;
        _bootstrapExpiresAt = bootstrapMaterial.ExpiresAt;
    }

    public Task<HostCredentialExchangeResult> BootstrapAsync(
        string secret,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_failedAttempts >= _maximumAttempts)
                return Task.FromResult(new HostCredentialExchangeResult(HostCredentialExchangeStatus.RateLimited));
            if (_bootstrapHash is null || _revoked || _timeProvider.GetUtcNow() >= _bootstrapExpiresAt)
                return Task.FromResult(new HostCredentialExchangeResult(HostCredentialExchangeStatus.Expired));

            var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            if (!CryptographicOperations.FixedTimeEquals(_bootstrapHash, candidateHash))
            {
                _failedAttempts++;
                return Task.FromResult(new HostCredentialExchangeResult(
                    _failedAttempts >= _maximumAttempts
                        ? HostCredentialExchangeStatus.RateLimited
                        : HostCredentialExchangeStatus.Invalid));
            }

            CryptographicOperations.ZeroMemory(_bootstrapHash);
            _bootstrapHash = null;
            _credentialId = Guid.NewGuid().ToString("N");
            var renewalToken = HostBootstrapProtocol.CreateSecret();
            _renewalHash = Hash(renewalToken);
            return Task.FromResult(new HostCredentialExchangeResult(
                HostCredentialExchangeStatus.Succeeded,
                _credentialId,
                renewalToken));
        }
    }

    public Task<HostCredentialExchangeResult> RenewAsync(
        string credentialId,
        string renewalToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_revoked || _renewalHash is null || !string.Equals(_credentialId, credentialId, StringComparison.Ordinal))
                return Task.FromResult(new HostCredentialExchangeResult(HostCredentialExchangeStatus.Invalid));

            var candidateHash = Hash(renewalToken ?? string.Empty);
            if (_consumedRenewalHash is not null && CryptographicOperations.FixedTimeEquals(_consumedRenewalHash, candidateHash))
            {
                RevokeLocked();
                return Task.FromResult(new HostCredentialExchangeResult(HostCredentialExchangeStatus.ReplayDetected));
            }

            if (!CryptographicOperations.FixedTimeEquals(_renewalHash, candidateHash))
                return Task.FromResult(new HostCredentialExchangeResult(HostCredentialExchangeStatus.Invalid));

            _consumedRenewalHash = _renewalHash;
            var rotatedToken = HostBootstrapProtocol.CreateSecret();
            _renewalHash = Hash(rotatedToken);
            return Task.FromResult(new HostCredentialExchangeResult(
                HostCredentialExchangeStatus.Succeeded,
                _credentialId,
                rotatedToken));
        }
    }

    public Task<CredentialAuthorizationState?> GetAuthorizationStateAsync(
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            CredentialAuthorizationState? state = !_revoked &&
                                                   _renewalHash is not null &&
                                                   string.Equals(_credentialId, credentialId, StringComparison.Ordinal)
                ? new CredentialAuthorizationState(
                    credentialId,
                    "MOBAflow host",
                    ControlPlaneRole.Host,
                    1,
                    ControlPlaneCapabilities.ForRole(ControlPlaneRole.Host))
                : null;
            return Task.FromResult(state);
        }
    }

    public void ConfirmHostConnection()
    {
        lock (_gate)
        {
            _disconnectCts?.Cancel();
            _disconnectCts?.Dispose();
            _disconnectCts = null;
        }
    }

    public void BeginDisconnectGrace()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_revoked || _credentialId is null)
                return;

            _disconnectCts?.Cancel();
            _disconnectCts?.Dispose();
            _disconnectCts = new CancellationTokenSource();
            token = _disconnectCts.Token;
        }

        _ = RevokeAfterGraceAsync(token);
    }

    public void Revoke()
    {
        lock (_gate)
        {
            RevokeLocked();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            RevokeLocked();
            _disconnectCts?.Dispose();
            _disconnectCts = null;
        }
    }

    private async Task RevokeAfterGraceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_disconnectGrace, _timeProvider, cancellationToken).ConfigureAwait(false);
            Revoke();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the host reconnects or the credential is revoked during the grace period.
            return;
        }
    }

    private void RevokeLocked()
    {
        _revoked = true;
        if (_bootstrapHash is not null)
            CryptographicOperations.ZeroMemory(_bootstrapHash);
        if (_renewalHash is not null)
            CryptographicOperations.ZeroMemory(_renewalHash);
        if (_consumedRenewalHash is not null)
            CryptographicOperations.ZeroMemory(_consumedRenewalHash);
        _bootstrapHash = null;
        _renewalHash = null;
        _consumedRenewalHash = null;
        _credentialId = null;
        _disconnectCts?.Cancel();
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
