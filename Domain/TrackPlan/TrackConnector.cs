// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a physical connector point on a track segment.
/// Connectors define where segments can be joined together.
/// Track-Graph Architecture: Connectors are the nodes that define the topology.
/// </summary>
public class TrackConnector
{
    /// <summary>
    /// Local position of the connector relative to segment origin.
    /// Example: (0, 0) for entry point, (230.93, 0) for exit point of G231 straight track.
    /// </summary>
    public TrackPoint LocalPosition { get; set; } = new();

    /// <summary>
    /// Local heading angle in degrees (0° = right/east, 90° = down/south).
    /// Defines the tangent direction at this connector.
    /// Example: 0° for entry, 180° for exit (pointing opposite direction).
    /// </summary>
    public double LocalHeadingDegrees { get; set; }

    /// <summary>
    /// Type of connector (defines compatibility and constraints).
    /// </summary>
    public ConnectorType Type { get; set; } = ConnectorType.Track;

    /// <summary>
    /// Optional index for multiple connectors of same type.
    /// Example: Switch has 3 connectors: [0] = main, [1] = straight, [2] = branch
    /// </summary>
    public int Index { get; set; }
}

/// <summary>
/// Simple 2D point in local track coordinate system.
/// </summary>
public class TrackPoint
{
    public double X { get; set; }
    public double Y { get; set; }

    public TrackPoint() { }

    public TrackPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Type of track connector (defines connection constraints).
/// </summary>
public enum ConnectorType
{
    /// <summary>
    /// Standard track connector (most common - straight and curved tracks).
    /// Constraint: Rigid (exact position + heading alignment ±180°).
    /// </summary>
    Track,

    /// <summary>
    /// Main line connector on a switch/turnout.
    /// Constraint: Rigid (same as Track).
    /// </summary>
    SwitchMain,

    /// <summary>
    /// Branch line connector on a switch/turnout.
    /// Constraint: Parametric (position rigid, heading depends on switch angle).
    /// </summary>
    SwitchBranch,

    /// <summary>
    /// Turntable/rotating bridge connector.
    /// Constraint: Rotational (position fixed, heading can rotate freely).
    /// </summary>
    Rotational
}
