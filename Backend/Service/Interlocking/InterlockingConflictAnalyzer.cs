// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using System.Collections.Frozen;

using Domain;

public enum RouteConflictReason
{
    Explicit,
    SharedBlock,
    SharedSignal,
    SharedPath,
    IncompatibleTurnoutPosition
}

public sealed record RouteConflict(
    Guid FirstRouteId,
    Guid SecondRouteId,
    IReadOnlyList<RouteConflictReason> Reasons,
    IReadOnlyList<Guid> RelatedResourceIds);

/// <summary>
/// Symmetric, deterministic route-conflict lookup.
/// </summary>
public sealed class InterlockingConflictMatrix
{
    private readonly IReadOnlyDictionary<(Guid First, Guid Second), RouteConflict> _conflicts;
    private readonly IReadOnlyList<RouteConflict> _orderedConflicts;

    internal InterlockingConflictMatrix(IEnumerable<RouteConflict> conflicts)
    {
        _conflicts = conflicts.ToFrozenDictionary(
            conflict => Key(conflict.FirstRouteId, conflict.SecondRouteId));
        _orderedConflicts = _conflicts.Values
            .OrderBy(conflict => conflict.FirstRouteId)
            .ThenBy(conflict => conflict.SecondRouteId)
            .ToArray();
    }

    public IReadOnlyList<RouteConflict> Conflicts => _orderedConflicts;

    public bool AreConflicting(Guid firstRouteId, Guid secondRouteId) =>
        _conflicts.ContainsKey(Key(firstRouteId, secondRouteId));

    public RouteConflict? GetConflict(Guid firstRouteId, Guid secondRouteId) =>
        _conflicts.GetValueOrDefault(Key(firstRouteId, secondRouteId));

    private static (Guid First, Guid Second) Key(Guid firstRouteId, Guid secondRouteId) =>
        firstRouteId.CompareTo(secondRouteId) <= 0
            ? (firstRouteId, secondRouteId)
            : (secondRouteId, firstRouteId);
}

/// <summary>
/// Derives conflicts from explicit declarations and shared safety resources.
/// </summary>
public static class InterlockingConflictAnalyzer
{
    public static InterlockingConflictMatrix Analyze(InterlockingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var conflicts = new List<RouteConflict>();
        var routes = definition.Routes.OrderBy(route => route.Id).ToArray();
        for (var firstIndex = 0; firstIndex < routes.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < routes.Length; secondIndex++)
            {
                var conflict = AnalyzePair(routes[firstIndex], routes[secondIndex]);
                if (conflict != null)
                    conflicts.Add(conflict);
            }
        }

        return new InterlockingConflictMatrix(conflicts);
    }

    private static RouteConflict? AnalyzePair(RouteDefinition first, RouteDefinition second)
    {
        var reasons = new HashSet<RouteConflictReason>();
        var resources = new HashSet<Guid>();

        if (first.ConflictingRouteIds.Contains(second.Id) || second.ConflictingRouteIds.Contains(first.Id))
            reasons.Add(RouteConflictReason.Explicit);

        AddShared(first.ProtectedBlockIds, second.ProtectedBlockIds, RouteConflictReason.SharedBlock, reasons, resources);
        AddShared(
            first.SignalRequirements.Select(requirement => requirement.SignalId),
            second.SignalRequirements.Select(requirement => requirement.SignalId),
            RouteConflictReason.SharedSignal,
            reasons,
            resources);

        var firstPath = new[] { first.EntryElementId }.Concat(first.PathElementIds).Append(first.ExitElementId);
        var secondPath = new[] { second.EntryElementId }.Concat(second.PathElementIds).Append(second.ExitElementId);
        AddShared(firstPath, secondPath, RouteConflictReason.SharedPath, reasons, resources);

        var secondTurnoutRequirements = second.TurnoutRequirements
            .GroupBy(requirement => requirement.TurnoutId)
            .ToDictionary(group => group.Key, group => group.Select(requirement => requirement.Position).Distinct().ToArray());
        foreach (var firstRequirement in first.TurnoutRequirements)
        {
            if (!secondTurnoutRequirements.TryGetValue(firstRequirement.TurnoutId, out var secondPositions)
                || secondPositions.Contains(firstRequirement.Position))
                continue;

            reasons.Add(RouteConflictReason.IncompatibleTurnoutPosition);
            resources.Add(firstRequirement.TurnoutId);
        }

        return reasons.Count == 0
            ? null
            : new RouteConflict(
                first.Id,
                second.Id,
                reasons.Order().ToArray(),
                resources.Order().ToArray());
    }

    private static void AddShared(
        IEnumerable<Guid> first,
        IEnumerable<Guid> second,
        RouteConflictReason reason,
        ISet<RouteConflictReason> reasons,
        ISet<Guid> resources)
    {
        var shared = first.Intersect(second).Where(id => id != Guid.Empty).ToArray();
        if (shared.Length == 0)
            return;

        reasons.Add(reason);
        foreach (var id in shared)
            resources.Add(id);
    }
}
