// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Collections.Concurrent;

namespace Moba.MOBApi.Security;

/// <summary>
/// Tracks authenticated hub connections so credential changes and token expiry can abort them immediately.
/// </summary>
public interface IControlPlaneConnectionRevoker
{
    /// <summary>
    /// Registers one authenticated hub connection and its expiry callback.
    /// </summary>
    void Register(
        string connectionId,
        string credentialId,
        DateTimeOffset accessTokenExpiresAt,
        Action abort);

    /// <summary>
    /// Stops tracking a disconnected hub connection.
    /// </summary>
    void Unregister(string connectionId);

    /// <summary>
    /// Aborts every active hub connection owned by <paramref name="credentialId"/>.
    /// </summary>
    void Revoke(string credentialId);
}

internal sealed class ControlPlaneConnectionRevoker : IControlPlaneConnectionRevoker
{
    private readonly ConcurrentDictionary<string, ConnectionRegistration> _connections = new();
    private readonly TimeProvider _timeProvider;

    public ControlPlaneConnectionRevoker(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void Register(
        string connectionId,
        string credentialId,
        DateTimeOffset accessTokenExpiresAt,
        Action abort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        ArgumentNullException.ThrowIfNull(abort);

        var registration = new ConnectionRegistration(credentialId, abort);
        if (_connections.TryRemove(connectionId, out var previous))
            previous.Dispose();
        _connections[connectionId] = registration;
        _ = ExpireAsync(connectionId, registration, accessTokenExpiresAt);
    }

    public void Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        if (_connections.TryRemove(connectionId, out var registration))
            registration.Dispose();
    }

    public void Revoke(string credentialId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        foreach (var candidate in _connections.Where(candidate =>
                     string.Equals(candidate.Value.CredentialId, credentialId, StringComparison.Ordinal)))
        {
            if (_connections.TryRemove(candidate.Key, out var registration))
                registration.AbortAndDispose();
        }
    }

    private async Task ExpireAsync(
        string connectionId,
        ConnectionRegistration registration,
        DateTimeOffset accessTokenExpiresAt)
    {
        var delay = accessTokenExpiresAt - _timeProvider.GetUtcNow();
        if (delay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(delay, _timeProvider, registration.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (_connections.TryGetValue(connectionId, out var current) &&
            ReferenceEquals(current, registration) &&
            _connections.TryRemove(connectionId, out var expired))
        {
            expired.AbortAndDispose();
        }
    }

    private sealed class ConnectionRegistration(string credentialId, Action abort) : IDisposable
    {
        private readonly CancellationTokenSource _expiryCancellation = new();

        public string CredentialId { get; } = credentialId;

        public CancellationToken CancellationToken => _expiryCancellation.Token;

        public void AbortAndDispose()
        {
            try
            {
                abort();
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            _expiryCancellation.Cancel();
            _expiryCancellation.Dispose();
        }
    }
}
