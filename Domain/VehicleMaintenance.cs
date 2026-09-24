// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Optional maintenance history and recurring plans shared by all rolling-stock vehicles.
/// </summary>
public sealed class VehicleMaintenanceData
{
    /// <summary>
    /// Gets or sets the recorded maintenance history.
    /// </summary>
    public List<VehicleMaintenanceEntry> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets recurring calendar-based plans.
    /// </summary>
    public List<VehicleMaintenancePlan> Plans { get; set; } = [];
}

/// <summary>
/// Describes one performed maintenance task.
/// </summary>
public sealed class VehicleMaintenanceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;

    public MaintenanceCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public MoneyAmount? Cost { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Defines one maintenance task that becomes due when its calendar interval is reached.
/// </summary>
public sealed class VehicleMaintenancePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public MaintenanceCategory Category { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? IntervalDays { get; set; }

    public DateTimeOffset? LastCompletedAt { get; set; }
}

public sealed class MoneyAmount
{
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code, for example EUR.
    /// </summary>
    public string Currency { get; set; } = "EUR";
}
