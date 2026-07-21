// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

using Domain.Enum;

/// <summary>
/// Authoritative runtime-derived usage projection for one rolling-stock vehicle.
/// </summary>
public sealed record VehicleUsageRuntimeSnapshot
{
    public required Guid VehicleId { get; init; }

    public required TrainVehicleKind VehicleKind { get; init; }

    public long TrackedOperatingSeconds { get; init; }

    public long TrackedCompletedTrips { get; init; }

    public bool IsOperating { get; init; }
}

/// <summary>
/// Usage-specific diagnostics. These counters intentionally do not reuse global EventBus metrics.
/// </summary>
public sealed record VehicleUsageRuntimeDiagnosticsSnapshot
{
    public long RejectedUpdates { get; init; }

    public long DuplicateJourneyCompletions { get; init; }

    public long RecoveredVehicles { get; init; }

    public long CompletedCheckpoints { get; init; }

    public long CheckpointFailures { get; init; }

    public DateTimeOffset? LastCheckpointAt { get; init; }
}
