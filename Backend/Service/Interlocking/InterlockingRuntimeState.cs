// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using System.Collections.Frozen;
using Domain;

/// <summary>Observed lifecycle of a direct turnout command.</summary>
public enum TurnoutLifecycle
{
    Unknown,
    Requested,
    Pending,
    Confirmed,
    Failed
}

/// <summary>Occupancy derived from explicit feedback; it does not authorize commands.</summary>
public enum BlockOccupancy
{
    Unknown,
    Free,
    Occupied,
    Fault
}

public sealed record TurnoutRuntimeState(
    Guid TurnoutId,
    TurnoutLifecycle Lifecycle,
    TurnoutPosition? RequestedPosition,
    TurnoutPosition? ConfirmedPosition);

public sealed record BlockRuntimeState(Guid BlockId, BlockOccupancy Occupancy);

public sealed record SignalRuntimeState(Guid SignalId, SignalAspect? Aspect);

/// <summary>Immutable operational observations shared by both layout pages.</summary>
public sealed record InterlockingRuntimeState
{
    public static InterlockingRuntimeState Empty { get; } = Create(0, [], [], [], []);

    public required long Revision { get; init; }
    public required IReadOnlyDictionary<Guid, TurnoutRuntimeState> Turnouts { get; init; }
    public required IReadOnlyDictionary<Guid, BlockRuntimeState> Blocks { get; init; }
    public required IReadOnlyDictionary<Guid, SignalRuntimeState> Signals { get; init; }
    public required IReadOnlySet<Guid> ProcessedCorrelationIds { get; init; }

    internal static InterlockingRuntimeState Create(
        long revision,
        IEnumerable<TurnoutRuntimeState> turnouts,
        IEnumerable<BlockRuntimeState> blocks,
        IEnumerable<SignalRuntimeState> signals,
        IEnumerable<Guid> processedCorrelationIds) => new()
        {
            Revision = revision,
            Turnouts = turnouts.ToFrozenDictionary(state => state.TurnoutId),
            Blocks = blocks.ToFrozenDictionary(state => state.BlockId),
            Signals = signals.ToFrozenDictionary(state => state.SignalId),
            ProcessedCorrelationIds = processedCorrelationIds.ToFrozenSet()
        };
}

public enum TurnoutCoordinatorStatus
{
    Accepted,
    Pending,
    Rejected,
    Failed
}

/// <summary>Correlated result of a direct turnout command.</summary>
public sealed record TurnoutCoordinatorResult(
    TurnoutCoordinatorStatus Status,
    string Code,
    string Message,
    Guid CorrelationId,
    InterlockingRuntimeState State);
