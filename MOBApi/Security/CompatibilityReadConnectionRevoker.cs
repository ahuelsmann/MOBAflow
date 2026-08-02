// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Collections.Concurrent;

namespace Moba.MOBApi.Security;

internal interface ICompatibilityReadConnectionRevoker
{
    void Register(string connectionId, Action abort);

    void Unregister(string connectionId);

    void RevokeAll();
}

internal sealed class CompatibilityReadConnectionRevoker : ICompatibilityReadConnectionRevoker
{
    private readonly ConcurrentDictionary<string, Action> _connections = new(StringComparer.Ordinal);

    public void Register(string connectionId, Action abort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(abort);
        _connections[connectionId] = abort;
    }

    public void Unregister(string connectionId) => _connections.TryRemove(connectionId, out _);

    public void RevokeAll()
    {
        foreach (var connection in _connections)
        {
            if (_connections.TryRemove(connection.Key, out var abort))
                abort();
        }
    }
}
