// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// Represents a line segment primitive.
/// Used for rendering straight tracks.
/// </summary>
public sealed record LinePrimitive(Point2D From, Point2D To) : IGeometryPrimitive;
