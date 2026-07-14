// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DecoderProtocol
{
    Unknown,
    Dcc,
    Motorola,
    Selectrix,
    Mfx
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaintenanceCategory
{
    Inspection,
    Cleaning,
    Lubrication,
    Repair,
    Decoder,
    WheelService,
    Other
}

/// <summary>
/// Optional decoder inventory and CV backup data for a locomotive.
/// </summary>
public sealed class LocomotiveDecoderProfile
{
    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public string? SerialNumber { get; set; }

    public string? FirmwareVersion { get; set; }

    public DecoderProtocol Protocol { get; set; }

    public DateTimeOffset? InstalledAt { get; set; }

    public string? Notes { get; set; }

    public List<DecoderCvSnapshot> CvSnapshots { get; set; } = [];
}

/// <summary>
/// Immutable-in-purpose snapshot of CV values captured at one point in time.
/// </summary>
public sealed class DecoderCvSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Source { get; set; }

    public List<DecoderCvValue> Values { get; set; } = [];
}

public sealed class DecoderCvValue
{
    public int Number { get; set; }

    public int Value { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// Optional maintenance history, counters and recurring plans.
/// </summary>
public sealed class LocomotiveMaintenanceData
{
    public decimal? OperatingHours { get; set; }

    public decimal? DistanceKilometres { get; set; }

    public List<LocomotiveMaintenanceEntry> Entries { get; set; } = [];

    public List<LocomotiveMaintenancePlan> Plans { get; set; } = [];
}

public sealed class LocomotiveMaintenanceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;

    public MaintenanceCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal? OperatingHoursAtService { get; set; }

    public decimal? DistanceKilometresAtService { get; set; }

    public MoneyAmount? Cost { get; set; }

    public string? Notes { get; set; }
}

public sealed class LocomotiveMaintenancePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public MaintenanceCategory Category { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? IntervalDays { get; set; }

    public decimal? IntervalOperatingHours { get; set; }

    public decimal? IntervalDistanceKilometres { get; set; }

    public DateTimeOffset? LastCompletedAt { get; set; }

    public decimal? OperatingHoursAtLastCompletion { get; set; }

    public decimal? DistanceKilometresAtLastCompletion { get; set; }
}

public sealed class MoneyAmount
{
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code, for example EUR.
    /// </summary>
    public string Currency { get; set; } = "EUR";
}
