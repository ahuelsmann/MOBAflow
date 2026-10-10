// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using System.Collections.Concurrent;

/// <summary>
/// Tracks MOBAsmart (or other) SignalR remote clients registered on the runtime hub, each for one project.
/// </summary>
public interface IRuntimeRemoteRegistry
{
    /// <summary>
    /// Registers a remote for one project and returns the project it was registered for before, if any.
    /// </summary>
    Guid? Register(string connectionId, string clientId, Guid projectId);

    void Unregister(string connectionId);

    /// <summary>Gets the project a remote connection is registered for.</summary>
    Guid? GetProject(string connectionId);

    int Count { get; }

    IReadOnlyList<RuntimeRemoteClientInfo> GetAll();
}

public sealed record RuntimeRemoteClientInfo(string ConnectionId, string ClientId, Guid ProjectId, DateTimeOffset ConnectedAt);

public sealed class RuntimeRemoteRegistry : IRuntimeRemoteRegistry
{
    private readonly ConcurrentDictionary<string, RuntimeRemoteClientInfo> _clients = new();

    public int Count => _clients.Count;

    public Guid? Register(string connectionId, string clientId, Guid projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var previous = GetProject(connectionId);
        _clients[connectionId] = new RuntimeRemoteClientInfo(connectionId, clientId, projectId, DateTimeOffset.UtcNow);
        return previous;
    }

    public void Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        _clients.TryRemove(connectionId, out _);
    }

    public Guid? GetProject(string connectionId) =>
        _clients.TryGetValue(connectionId, out var client) ? client.ProjectId : null;

    public IReadOnlyList<RuntimeRemoteClientInfo> GetAll()
    {
        return _clients.Values
            .OrderBy(client => client.ConnectedAt)
            .ToList();
    }
}
