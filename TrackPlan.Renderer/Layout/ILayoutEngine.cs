// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Layout;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;

public interface ILayoutEngine
{
    Dictionary<Guid, Point2D> Layout(TopologyGraph graph);
}