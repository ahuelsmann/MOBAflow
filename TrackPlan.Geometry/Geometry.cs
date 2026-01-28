// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// TrackPlan.Geometry contains geometry primitives and calculators for rendering track elements.
/// 
/// Core types:
/// - World/Point2D: 2D coordinate point with vector operations
/// - World/WorldTransform: Viewport transformation (scale + offset)
/// - Geometry/IGeometryPrimitive: Base interface for all primitive shapes
/// - Geometry/LinePrimitive: Line segment (for straight tracks)
/// - Geometry/ArcPrimitive: Circular arc (for curves and switches)
/// - Geometry/StraightGeometry: Static calculator for straight track rendering
/// - Geometry/CurveGeometry: Static calculator for curved track rendering
/// - Geometry/SwitchGeometry: Static calculator for switch track rendering
/// - Geometry/ThreeWaySwitchGeometry: Static calculator for three-way switch rendering
/// - Geometry/GeometryCalculationEngine: Orchestrates geometry calculations
/// </summary>
internal static class GeometryMarker { }
