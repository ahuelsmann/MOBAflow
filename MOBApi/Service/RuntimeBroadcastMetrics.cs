// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

/// <summary>
/// Tracks the last runtime snapshot broadcast from the MOBAflow host via SignalR.
/// </summary>
public interface IRuntimeBroadcastMetrics
{
    DateTimeOffset? LastSnapshotBroadcastAt { get; }

    int LastSnapshotPayloadBytes { get; }

    long TotalSnapshotBroadcastCount { get; }

    void RecordSnapshotBroadcast(int payloadBytes);
}

public sealed class RuntimeBroadcastMetrics : IRuntimeBroadcastMetrics
{
    private readonly object _lock = new();
    private DateTimeOffset? _lastSnapshotBroadcastAt;
    private int _lastSnapshotPayloadBytes;
    private long _totalSnapshotBroadcastCount;

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

    public int LastSnapshotPayloadBytes
    {
        get
        {
            lock (_lock)
            {
                return _lastSnapshotPayloadBytes;
            }
        }
    }

    public long TotalSnapshotBroadcastCount
    {
        get
        {
            lock (_lock)
            {
                return _totalSnapshotBroadcastCount;
            }
        }
    }

    public void RecordSnapshotBroadcast(int payloadBytes)
    {
        if (payloadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadBytes));
        }

        lock (_lock)
        {
            _lastSnapshotBroadcastAt = DateTimeOffset.UtcNow;
            _lastSnapshotPayloadBytes = payloadBytes;
            _totalSnapshotBroadcastCount++;
        }
    }

    public void RecordSnapshotBroadcast()
    {
        RecordSnapshotBroadcast(0);
    }
}
