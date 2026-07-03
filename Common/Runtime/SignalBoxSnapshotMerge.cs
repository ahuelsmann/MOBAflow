// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Merges cached signal-box aspect state into freshly activated local runtime snapshots.
/// </summary>
public static class SignalBoxSnapshotMerge
{
    /// <summary>
    /// Returns <paramref name="incoming"/> with signal aspects and switch positions taken from
    /// <paramref name="cached"/> when the incoming snapshot does not carry those values.
    /// </summary>
    public static IReadOnlyList<SignalBoxElementRuntimeSnapshot> MergeAspectsFromCache(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> incoming,
        IReadOnlyList<SignalBoxElementRuntimeSnapshot>? cached)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        if (cached is not { Count: > 0 })
        {
            return incoming;
        }

        if (incoming.Count == 0)
        {
            return cached;
        }

        var cacheById = cached.ToDictionary(element => element.ElementId);
        var merged = new List<SignalBoxElementRuntimeSnapshot>(incoming.Count);

        foreach (var element in incoming)
        {
            if (!cacheById.TryGetValue(element.ElementId, out var cachedElement))
            {
                merged.Add(element);
                continue;
            }

            merged.Add(element with
            {
                SignalAspect = element.SignalAspect ?? cachedElement.SignalAspect,
                SwitchPosition = element.SwitchPosition ?? cachedElement.SwitchPosition
            });
        }

        return merged;
    }

    /// <summary>
    /// Merges <paramref name="incoming"/> over <paramref name="previous"/> by element id.
    /// Incoming aspect and switch values win when present; elements only in <paramref name="previous"/> are kept.
    /// </summary>
    public static IReadOnlyList<SignalBoxElementRuntimeSnapshot> MergeIncomingOverPrevious(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> incoming,
        IReadOnlyList<SignalBoxElementRuntimeSnapshot>? previous)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        if (incoming.Count == 0)
        {
            return previous ?? incoming;
        }

        if (previous is not { Count: > 0 })
        {
            return incoming;
        }

        var previousById = previous.ToDictionary(element => element.ElementId);
        var incomingIds = incoming.Select(element => element.ElementId).ToHashSet();
        var merged = new List<SignalBoxElementRuntimeSnapshot>(incoming.Count + previous.Count);

        foreach (var element in incoming)
        {
            if (!previousById.TryGetValue(element.ElementId, out var previousElement))
            {
                merged.Add(element);
                continue;
            }

            merged.Add(element with
            {
                SignalAspect = element.SignalAspect ?? previousElement.SignalAspect,
                SwitchPosition = element.SwitchPosition ?? previousElement.SwitchPosition
            });
        }

        foreach (var previousElement in previous)
        {
            if (!incomingIds.Contains(previousElement.ElementId))
            {
                merged.Add(previousElement);
            }
        }

        return merged;
    }
}
