// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

public sealed record RuntimeSnapshotCacheEntry(Guid ProjectId, string Json, DateTimeOffset UpdatedAt, bool IsConnected);

/// <summary>
/// Thread-safe cache for the latest MOBAflow runtime snapshot of each project. Every project has its own runtime.
/// </summary>
public interface IRuntimeSnapshotCache
{
    /// <summary>
    /// Gets the latest snapshot of a project's runtime.
    /// </summary>
    bool TryGet(Guid projectId, out RuntimeSnapshotCacheEntry entry);

    /// <summary>
    /// Gets the latest snapshot of every project's runtime.
    /// </summary>
    IReadOnlyList<RuntimeSnapshotCacheEntry> GetAll();

    /// <summary>
    /// Stores a snapshot for the project it names.
    /// </summary>
    /// <returns>The cached entry.</returns>
    RuntimeSnapshotCacheEntry Set(string json);
}

public sealed class RuntimeSnapshotCache : IRuntimeSnapshotCache
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, RuntimeSnapshotCacheEntry> _entries = [];

    /// <inheritdoc />
    public bool TryGet(Guid projectId, out RuntimeSnapshotCacheEntry entry)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(projectId, out entry!);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RuntimeSnapshotCacheEntry> GetAll()
    {
        lock (_lock)
        {
            return [.. _entries.Values];
        }
    }

    /// <inheritdoc />
    public RuntimeSnapshotCacheEntry Set(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = RuntimeJsonSerializer.Deserialize(json)
            ?? throw new ArgumentException("Invalid runtime snapshot JSON.", nameof(json));
        if (snapshot.ProjectId == Guid.Empty)
        {
            throw new ArgumentException("The runtime snapshot names no project.", nameof(json));
        }

        lock (_lock)
        {
            if (_entries.TryGetValue(snapshot.ProjectId, out var previousEntry))
            {
                var previous = RuntimeJsonSerializer.Deserialize(previousEntry.Json);
                snapshot = RuntimeSnapshotPreservation.PreserveProjectElementsFrom(snapshot, previous);
                json = RuntimeJsonSerializer.Serialize(snapshot);
            }

            var entry = new RuntimeSnapshotCacheEntry(snapshot.ProjectId, json, DateTimeOffset.UtcNow, snapshot.IsConnected);
            _entries[snapshot.ProjectId] = entry;
            return entry;
        }
    }
}
