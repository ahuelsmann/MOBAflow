// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

/// <summary>
/// Entkuppler-Segment mit zwei Ports (A, B).
/// </summary>
public record Uncoupler : Segment
{
    /// <summary>
    /// Connection port A of the uncoupler segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the uncoupler segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
