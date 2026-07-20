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
