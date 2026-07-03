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
        var isConnected = snapshotCacheInfo is { Available: true, IsConnected: true };

        return new
        {
            hasHost = hostRegistry.HasHost,
            remoteClientCount = remoteRegistry.Count,
            lastSnapshotBroadcastAt = broadcastMetrics.LastSnapshotBroadcastAt,
            lastSnapshotPayloadBytes = broadcastMetrics.LastSnapshotPayloadBytes,
            totalSnapshotBroadcastCount = broadcastMetrics.TotalSnapshotBroadcastCount,
            sessionOperational = hostRegistry.HasHost && isConnected,
            snapshotCache = snapshotCacheInfo
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

    private static SnapshotCacheStatus BuildSnapshotCacheInfo(IRuntimeSnapshotCache snapshotCache)
    {
        if (!snapshotCache.TryGet(out var entry))
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
