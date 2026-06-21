// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

/// <summary>
/// Tracks the last runtime snapshot broadcast from the MOBAflow host via SignalR.
/// </summary>
public interface IRuntimeBroadcastMetrics
{
    DateTimeOffset? LastSnapshotBroadcastAt { get; }

    void RecordSnapshotBroadcast();
}

public sealed class RuntimeBroadcastMetrics : IRuntimeBroadcastMetrics
{
    private readonly object _lock = new();
    private DateTimeOffset? _lastSnapshotBroadcastAt;

    public DateTimeOffset? LastSnapshotBroadcastAt
    {
        get
        {
            lock (_lock)
            {
                return _lastSnapshotBroadcastAt;
            }
        }
    }

    public void RecordSnapshotBroadcast()
    {
        lock (_lock)
        {
            _lastSnapshotBroadcastAt = DateTimeOffset.UtcNow;
        }
    }
}
