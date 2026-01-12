// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Layout;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.World;

public interface ILayoutEngine
{
    Dictionary<Guid, Point2D> Layout(TopologyGraph graph);
}