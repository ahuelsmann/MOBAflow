// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

/// <summary>
/// Geometry specification for a track piece.
/// </summary>
/// <param name="GeometryKind">Type of geometry (Straight, Curve, Switch)</param>
/// <param name="LengthMm">Length in mm (for straight tracks)</param>
/// <param name="RadiusMm">Radius in mm (for curves and switches)</param>
/// <param name="AngleDeg">Angle in degrees (for curves and switches)</param>
public sealed record TrackGeometrySpec(
    TrackGeometryKind GeometryKind,
    double? LengthMm = null,
    double? RadiusMm = null,
    double? AngleDeg = null,
    double? JunctionOffsetMm = null  // neu: nur für Weichen
);
