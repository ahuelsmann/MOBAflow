// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

public sealed record RuntimeSnapshotCacheEntry(string Json, DateTimeOffset UpdatedAt, bool IsConnected);

/// <summary>
/// Thread-safe cache for the latest MOBAflow runtime snapshot.
/// </summary>
public interface IRuntimeSnapshotCache
{
    bool TryGet(out RuntimeSnapshotCacheEntry entry);

    void Set(string json, bool isConnected);
}

public sealed class RuntimeSnapshotCache : IRuntimeSnapshotCache
{
    private readonly object _lock = new();
    private RuntimeSnapshotCacheEntry? _entry;

    public bool TryGet(out RuntimeSnapshotCacheEntry entry)
    {
        lock (_lock)
        {
            if (_entry == null)
            {
                entry = null!;
                return false;
            }

            entry = _entry;
            return true;
        }
    }

    public void Set(string json, bool isConnected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var snapshot = RuntimeJsonSerializer.Deserialize(json)
            ?? throw new ArgumentException("Invalid runtime snapshot JSON.", nameof(json));

        lock (_lock)
        {
            if (_entry != null)
            {
                var previous = RuntimeJsonSerializer.Deserialize(_entry.Json);
                snapshot = RuntimeSnapshotPreservation.PreserveProjectElementsFrom(snapshot, previous);
                json = RuntimeJsonSerializer.Serialize(snapshot);
            }

            _entry = new RuntimeSnapshotCacheEntry(json, DateTimeOffset.UtcNow, isConnected);
        }
    }
}
