// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// A trip log entry – records a travel or stop phase of a locomotive
/// via the TrainControlPage.
/// </summary>
public class TripLogEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TripLogEntry"/> class with a new identifier.
    /// </summary>
    public TripLogEntry()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Gets or sets the unique identifier of the trip log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// DCC address of the locomotive (1–9999).
    /// </summary>
    public int LocoAddress { get; set; }

    /// <summary>
    /// Start time of the phase (travel or stop).
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the phase. Null = phase is still running.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Speed (0–126 for 128 speed steps). 0 = stop.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// true = stop phase (speed was 0), false = travel phase.
    /// </summary>
    public bool IsStopSegment { get; set; }

    /// <summary>
    /// Duration of the phase. Null when EndTime is not yet set (ongoing phase).
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}
