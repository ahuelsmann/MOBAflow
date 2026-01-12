// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

public sealed class TopologyGraph
{
    public List<TrackNode> Nodes { get; init; } = [];
    public List<TrackEdge> Edges { get; init; } = [];
    public List<Endcap> Endcaps { get; init; } = [];
    public List<Section> Sections { get; init; } = [];
    public List<Isolator> Isolators { get; init; } = [];

    public List<ITopologyConstraint> Constraints { get; } = [];

    public IEnumerable<ConstraintViolation> Validate()
        => Constraints.SelectMany(c => c.Validate(this));
}