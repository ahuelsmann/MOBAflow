// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// TrackPlan.Geometry contains geometry primitives for rendering track elements.
/// Re-exports types from TrackPlan.Renderer in the Moba.TrackPlan.Geometry namespace.
/// 
/// Core types:
/// - Point2D: 2D coordinate point
/// - IGeometryPrimitive: Base interface for all primitive shapes
/// - ArcPrimitive: Circular arc (for curves and switches)
/// - LinePrimitive: Line segment (for straight tracks)
/// - StraightGeometry, CurveGeometry, SwitchGeometry: Static geometry calculators
/// </summary>
internal static class GeometryMarker { }
