// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Events;

using Common.Events;

using Service.Interlocking;

/// <summary>
/// Immutable interlocking state published after an ordered runtime transition.
/// </summary>
public sealed record InterlockingRuntimeSnapshotChangedEvent(
    InterlockingRuntimeState Snapshot,
    bool IsSynchronized,
    Guid CorrelationId,
    string Code) : EventBase;
