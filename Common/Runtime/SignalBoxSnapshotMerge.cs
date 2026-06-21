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

}

