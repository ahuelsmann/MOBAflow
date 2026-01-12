// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Constraint;

using Moba.TrackPlan.Graph;

public interface ITopologyConstraint
{
    IEnumerable<ConstraintViolation> Validate(TopologyGraph graph);
}