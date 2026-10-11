// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

/// <summary>
/// Builds runtime sync diagnostics for GET /api/status.
/// </summary>
public static class RuntimeStatusBuilder
{
    public static object BuildRuntimeStatus(
        IRuntimeHostRegistry hostRegistry,
        IRuntimeRemoteRegistry remoteRegistry,
        IRuntimeBroadcastMetrics broadcastMetrics,
        IRuntimeSnapshotCache snapshotCache)
    {
        var snapshotCacheInfo = BuildSnapshotCacheInfo(snapshotCache);
        var entries = snapshotCache.GetAll();

        return new
        {
            hasHost = hostRegistry.HasHost,
            remoteClientCount = remoteRegistry.Count,
            lastSnapshotBroadcastAt = broadcastMetrics.LastSnapshotBroadcastAt,
            lastSnapshotPayloadBytes = broadcastMetrics.LastSnapshotPayloadBytes,
            totalSnapshotBroadcastCount = broadcastMetrics.TotalSnapshotBroadcastCount,
            sessionOperational = hostRegistry.HasHost && IsSessionConnected(remoteRegistry, entries),
            snapshotCache = snapshotCacheInfo,
            projects = entries
                .Select(entry => new { projectId = entry.ProjectId, isConnected = entry.IsConnected, updatedAt = entry.UpdatedAt })
                .ToList()
        };
    }

    public static object BuildSolutionStatus(ISolutionCache solutionCache)
    {
        if (!solutionCache.TryGet(out var entry))
        {
            return new
            {
                available = false,
                updatedAt = (DateTimeOffset?)null,
                activeProjectName = (string?)null
            };
        }

        return new
        {
            available = true,
            updatedAt = entry.UpdatedAt,
            activeProjectName = entry.ActiveProjectName
        };
    }

    /// <summary>
    /// Every project has its own runtime: the session works when every project a remote controls is connected,
    /// or, without remotes, when at least one project is connected.
    /// </summary>
    private static bool IsSessionConnected(
        IRuntimeRemoteRegistry remoteRegistry,
        IReadOnlyList<RuntimeSnapshotCacheEntry> entries)
    {
        var controlled = remoteRegistry.GetAll()
            .Select(remote => remote.ProjectId)
            .Where(projectId => projectId != Guid.Empty)
            .Distinct()
            .ToList();
        return controlled.Count == 0
            ? entries.Any(entry => entry.IsConnected)
            : controlled.All(projectId => entries.Any(entry => entry.ProjectId == projectId && entry.IsConnected));
    }

    private static SnapshotCacheStatus BuildSnapshotCacheInfo(IRuntimeSnapshotCache snapshotCache)
    {
        // Every project has its own runtime; the status shows the most recently updated one.
        var entry = snapshotCache.GetAll().MaxBy(cached => cached.UpdatedAt);
        if (entry is null)
        {
            return new SnapshotCacheStatus(false, null, false, 0, 0);
        }

        var signalBoxElementCount = 0;
        var locomotiveFleetCount = 0;

        try
        {
            var snapshot = RuntimeJsonSerializer.Deserialize(entry.Json);
            if (snapshot != null)
            {
                signalBoxElementCount = snapshot.SignalBoxElements.Count;
                locomotiveFleetCount = snapshot.LocomotiveFleet.Count;
            }
        }
        catch
        {
            // Best-effort counts only.
        }

        return new SnapshotCacheStatus(
            true,
            entry.UpdatedAt,
            entry.IsConnected,
            signalBoxElementCount,
            locomotiveFleetCount);
    }

    private sealed record SnapshotCacheStatus(
        bool Available,
        DateTimeOffset? UpdatedAt,
        bool IsConnected,
        int SignalBoxElementCount,
        int LocomotiveFleetCount);
}
