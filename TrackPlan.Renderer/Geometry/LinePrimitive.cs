// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

public sealed record LinePrimitive(Point2D From, Point2D To) : IGeometryPrimitive;