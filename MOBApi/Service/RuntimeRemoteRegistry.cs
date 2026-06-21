// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using System.Collections.Concurrent;

/// <summary>
/// Tracks MOBAsmart (or other) SignalR remote clients registered on the runtime hub.
/// </summary>
public interface IRuntimeRemoteRegistry
{
    void Register(string connectionId, string clientId);

    void Unregister(string connectionId);

    int Count { get; }

    IReadOnlyList<RuntimeRemoteClientInfo> GetAll();
}

public sealed record RuntimeRemoteClientInfo(string ConnectionId, string ClientId, DateTimeOffset ConnectedAt);

public sealed class RuntimeRemoteRegistry : IRuntimeRemoteRegistry
{
    private readonly ConcurrentDictionary<string, RuntimeRemoteClientInfo> _clients = new();

    public int Count => _clients.Count;

    public void Register(string connectionId, string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        _clients[connectionId] = new RuntimeRemoteClientInfo(connectionId, clientId, DateTimeOffset.UtcNow);
    }

    public void Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        _clients.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<RuntimeRemoteClientInfo> GetAll()
    {
        return _clients.Values
            .OrderBy(client => client.ConnectedAt)
            .ToList();
    }
}
