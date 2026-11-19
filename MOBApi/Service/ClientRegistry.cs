// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Models;

using System.Collections.Concurrent;

/// <summary>
/// Shared in-memory registry for connected clients.
/// </summary>
public interface IClientRegistry
{
    void Add(ConnectedClientInfo info);

    void Remove(string clientId);

    void PruneExpired(int expiryMinutes);

    IReadOnlyList<ConnectedClientInfo> GetAll();
}

public sealed class ClientRegistry : IClientRegistry
{
    private readonly ConcurrentDictionary<string, ConnectedClientInfo> _clients = new();

    public void Add(ConnectedClientInfo info)
    {
        _clients[info.ClientId] = info;
    }

    public void Remove(string clientId)
    {
        _clients.TryRemove(clientId, out _);
    }

    public void PruneExpired(int expiryMinutes)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-expiryMinutes);
        foreach (var kv in _clients.ToArray())
        {
            if (kv.Value.ConnectedAt < cutoff)
            {
                _clients.TryRemove(kv.Key, out _);
            }
        }
    }

    public IReadOnlyList<ConnectedClientInfo> GetAll()
    {
        return _clients.Values.ToList();
    }
}
