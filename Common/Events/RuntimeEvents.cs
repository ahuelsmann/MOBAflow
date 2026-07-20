// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

using Runtime;

/// <summary>
/// Published when the in-process runtime has produced a new immutable state snapshot.
/// </summary>
public sealed record RuntimeSnapshotChangedEvent(MobaRuntimeSnapshot Snapshot) : EventBase;

/// <summary>
/// Published after an atomic local vehicle-usage checkpoint has been committed.
/// Consumers may use this coalesced signal to persist the editable solution.
/// </summary>
public sealed record VehicleUsageCheckpointCommittedEvent(
    Guid ProjectId,
    DateTimeOffset CommittedAt,
    IReadOnlyDictionary<Guid, VehicleUsageRuntimeSnapshot> Usage) : EventBase;

/// <summary>
/// Published when MOBApi forwards a MOBAflow runtime snapshot to MOBAsmart.
/// </summary>
public sealed record RemoteRuntimeSnapshotChangedEvent(MobaRuntimeSnapshot Snapshot) : EventBase;

/// <summary>
/// Published when the mobile Control tab fleet list was rebuilt from runtime or cached fleet data.
/// </summary>
public sealed record LocomotiveFleetUpdatedEvent(IReadOnlyList<LocomotiveFleetSnapshot> Fleet) : EventBase;

/// <summary>
/// Published when MOBAsmart command routing switches between MOBAflow remote and local Z21.
/// </summary>
public sealed record RuntimeCommandAvailabilityChangedEvent : EventBase;
