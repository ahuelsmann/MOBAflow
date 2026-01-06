// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Represents a connection between two track segment connectors.
/// Track-Graph Architecture: Connections are edges in the topology graph with geometric constraints.
/// The constraint type determines how world transforms are calculated during rendering.
/// 
/// Connector indices:
/// - Simple tracks (2 connectors): 0 = start, 1 = end
/// - Turnouts (3 connectors): 0 = main, 1 = straight, 2 = branch
/// - Double slips (4 connectors): 0-3 for each direction
/// </summary>
public class TrackConnection
{
    /// <summary>
    /// ID of the first connected segment.
    /// </summary>
    public string Segment1Id { get; set; } = string.Empty;

    /// <summary>
    /// Connector index on segment 1 (0 = first connector, 1 = second, etc.).
    /// Maps to TrackSegment.Connectors[index] or TrackGeometry.Endpoints[index].
    /// </summary>
    public int Segment1ConnectorIndex { get; set; }

    /// <summary>
    /// ID of the second connected segment.
    /// </summary>
    public string Segment2Id { get; set; } = string.Empty;

    /// <summary>
    /// Connector index on segment 2 (0 = first connector, 1 = second, etc.).
    /// Maps to TrackSegment.Connectors[index] or TrackGeometry.Endpoints[index].
    /// </summary>
    public int Segment2ConnectorIndex { get; set; }

    /// <summary>
    /// Geometric constraint type for this connection.
    /// Determines how world transforms are calculated.
    /// Default: Rigid (exact position + heading alignment).
    /// </summary>
    public ConstraintType ConstraintType { get; set; } = ConstraintType.Rigid;

    /// <summary>
    /// Optional parameters for parametric constraints.
    /// Example: Branch angle for switch turnouts.
    /// </summary>
    public Dictionary<string, double>? Parameters { get; set; }

    /// <summary>
        /// Optional: Switch state required for this connection to be active.
        /// If null, connection is always active.
        /// If not null, connection is only active when parent segment SwitchState matches.
        /// This enables parametric switch control without coordinate changes.
        /// </summary>
        public SwitchState? ActiveWhen { get; set; }
    }
