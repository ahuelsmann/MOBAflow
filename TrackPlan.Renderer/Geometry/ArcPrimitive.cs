// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

public sealed record ArcPrimitive(
    Point2D Center,
    double Radius,
    double StartAngleRad,
    double SweepAngleRad
) : IGeometryPrimitive;