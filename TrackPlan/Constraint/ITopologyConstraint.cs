namespace Moba.TrackPlan.Constraint;

using Moba.TrackPlan.Graph;

public interface ITopologyConstraint
{
    IEnumerable<ConstraintViolation> Validate(TopologyGraph graph);
}