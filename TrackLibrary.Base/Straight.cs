// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

/// <summary>
/// Straight track segment with two ports (A, B) and fixed length.
/// </summary>
public abstract record Straight(double LengthInMm) : Segment
{
    /// <summary>
    /// Connection port A of the straight segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the straight segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
