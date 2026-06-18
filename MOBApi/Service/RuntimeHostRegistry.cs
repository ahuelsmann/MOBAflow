// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

/// <summary>
/// Tracks the active MOBAflow SignalR host connection.
/// </summary>
public interface IRuntimeHostRegistry
{
    bool HasHost { get; }

    string? HostConnectionId { get; }

    void SetHost(string connectionId);

    bool IsHost(string connectionId);

    void ClearHost(string connectionId);
}

public sealed class RuntimeHostRegistry : IRuntimeHostRegistry
{
    private readonly object _lock = new();
    private string? _hostConnectionId;

    public bool HasHost
    {
        get
        {
            lock (_lock)
            {
                return !string.IsNullOrEmpty(_hostConnectionId);
            }
        }
    }

    public string? HostConnectionId
    {
        get
        {
            lock (_lock)
            {
                return _hostConnectionId;
            }
        }
    }

    public void SetHost(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_lock)
        {
            _hostConnectionId = connectionId;
        }
    }

    public bool IsHost(string connectionId)
    {
        lock (_lock)
        {
            return string.Equals(_hostConnectionId, connectionId, StringComparison.Ordinal);
        }
    }

    public void ClearHost(string connectionId)
    {
        lock (_lock)
        {
            if (string.Equals(_hostConnectionId, connectionId, StringComparison.Ordinal))
            {
                _hostConnectionId = null;
            }
        }
    }
}
