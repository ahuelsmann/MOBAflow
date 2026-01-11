namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Graph;

public sealed class ValidationService
{
    public IReadOnlyList<ConstraintViolation> Validate(TopologyGraph graph)
        => graph.Validate().ToList();
}