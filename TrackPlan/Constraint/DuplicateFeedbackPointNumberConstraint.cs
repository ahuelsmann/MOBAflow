namespace Moba.TrackPlan.Constraint;

using Moba.TrackPlan.Graph;

public sealed class DuplicateFeedbackPointNumberConstraint : ITopologyConstraint
{
    public IEnumerable<ConstraintViolation> Validate(TopologyGraph graph)
    {
        var map = new Dictionary<int, List<Guid>>();

        foreach (var edge in graph.Edges)
        {
            if (edge.FeedbackPointNumber is null)
                continue;

            var n = edge.FeedbackPointNumber.Value;

            if (!map.TryGetValue(n, out var list))
                map[n] = list = [];

            list.Add(edge.Id);
        }

        foreach (var kv in map.Where(kv => kv.Value.Count > 1))
        {
            yield return new ConstraintViolation(
                "duplicate-feedback-point",
                $"FeedbackPoint {kv.Key} assigned multiple times",
                kv.Value
            );
        }
    }
}