// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Persisted lifetime usage collected for one rolling-stock vehicle.
/// </summary>
public sealed class VehicleUsageData
{
    /// <summary>
    /// Gets or sets the automatically tracked powered or consist operating time in whole seconds.
    /// </summary>
    public long TrackedOperatingSeconds { get; set; }

    /// <summary>
    /// Gets or sets the automatically tracked number of completed trips.
    /// </summary>
    public long TrackedCompletedTrips { get; set; }

    /// <summary>
    /// Gets or sets the automatically tracked distance when an explicit distance source is available.
    /// </summary>
    public decimal? TrackedDistanceKilometres { get; set; }

    /// <summary>
    /// Gets or sets the append-only history of manual counter corrections.
    /// </summary>
    public List<VehicleUsageCorrection> Corrections { get; set; } = [];
}

/// <summary>
/// Describes one auditable manual adjustment without replacing tracked lifetime values.
/// </summary>
public sealed record VehicleUsageCorrection
{
    /// <summary>
    /// Gets the stable correction identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the time at which the correction was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the signed operating-time adjustment in whole seconds.
    /// </summary>
    public long OperatingSecondsDelta { get; init; }

    /// <summary>
    /// Gets the signed completed-trip adjustment.
    /// </summary>
    public long CompletedTripsDelta { get; init; }

    /// <summary>
    /// Gets the signed distance adjustment when distance tracking is available.
    /// </summary>
    public decimal? DistanceKilometresDelta { get; init; }

    /// <summary>
    /// Gets the required human-readable reason for the adjustment.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Effective lifetime totals after applying the auditable correction history.
/// </summary>
public sealed record VehicleUsageTotals(
    long OperatingSeconds,
    long CompletedTrips,
    decimal? DistanceKilometres);
