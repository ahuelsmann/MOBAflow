// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using System.Collections.Frozen;

using Domain;

/// <summary>
/// Runtime lifecycle of a semantic turnout command.
/// </summary>
public enum TurnoutLifecycle
{
    Unknown,
    Requested,
    Pending,
    Confirmed,
    Failed
}

/// <summary>
/// Fail-safe occupancy state. Only <see cref="Free"/> satisfies a route prerequisite.
/// </summary>
public enum BlockOccupancy
{
    Unknown,
    Free,
    Occupied,
    Fault
}

/// <summary>
/// Pure route lifecycle used before any hardware effects are dispatched.
/// </summary>
public enum RouteLifecycle
{
    Available,
    Selected,
    Setting,
    Established,
    Occupied,
    Releasing,
    Failed,
    Conflicting
}

public sealed record TurnoutRuntimeState(
    Guid TurnoutId,
    TurnoutLifecycle Lifecycle,
    TurnoutPosition? RequestedPosition,
    TurnoutPosition? ConfirmedPosition,
    Guid? LockOwnerRouteId);

public sealed record BlockRuntimeState(
    Guid BlockId,
    BlockOccupancy Occupancy,
    Guid? ReservationOwnerRouteId);

public sealed record SignalRuntimeState(
    Guid SignalId,
    SignalAspect Aspect,
    Guid? LockOwnerRouteId);

public sealed record RouteRuntimeState(
    Guid RouteId,
    RouteLifecycle Lifecycle,
    string? FailureCode);

/// <summary>
/// Immutable, revisioned state consumed and returned by the pure safety engine.
/// </summary>
public sealed record InterlockingRuntimeState
{
    public required long Revision { get; init; }

    public required IReadOnlyDictionary<Guid, TurnoutRuntimeState> Turnouts { get; init; }

    public required IReadOnlyDictionary<Guid, BlockRuntimeState> Blocks { get; init; }

    public required IReadOnlyDictionary<Guid, SignalRuntimeState> Signals { get; init; }

    public required IReadOnlyDictionary<Guid, RouteRuntimeState> Routes { get; init; }

    public required IReadOnlySet<Guid> ProcessedCorrelationIds { get; init; }

    internal static InterlockingRuntimeState Create(
        long revision,
        IEnumerable<TurnoutRuntimeState> turnouts,
        IEnumerable<BlockRuntimeState> blocks,
        IEnumerable<SignalRuntimeState> signals,
        IEnumerable<RouteRuntimeState> routes,
        IEnumerable<Guid> processedCorrelationIds) =>
        new()
        {
            Revision = revision,
            Turnouts = turnouts.ToFrozenDictionary(state => state.TurnoutId),
            Blocks = blocks.ToFrozenDictionary(state => state.BlockId),
            Signals = signals.ToFrozenDictionary(state => state.SignalId),
            Routes = routes.ToFrozenDictionary(state => state.RouteId),
            ProcessedCorrelationIds = processedCorrelationIds.ToFrozenSet()
        };
}

public enum InterlockingDecisionStatus
{
    Accepted,
    Rejected,
    IgnoredDuplicate,
    IgnoredStale
}

/// <summary>
/// Structured result of one deterministic state transition.
/// </summary>
public sealed record InterlockingDecision(
    InterlockingDecisionStatus Status,
    string Code,
    string Message,
    IReadOnlyList<Guid> AffectedIds,
    InterlockingRuntimeState State)
{
    public bool IsAccepted => Status == InterlockingDecisionStatus.Accepted;
}
