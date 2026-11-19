// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

/// <summary>
/// Sensor segment with two ports (A, B).
/// </summary>
public abstract record Sensor : Segment
{
    /// <summary>
    /// Connection port A of the sensor segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the sensor segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
