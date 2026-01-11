namespace Moba.TrackPlan.UseCase;

using Moba.TrackPlan.Graph;

public sealed class AssignFeedbackPointToTrackUseCase
{
    private readonly TopologyGraph _graph;
    private readonly Guid _edgeId;
    private readonly int? _number;

    public AssignFeedbackPointToTrackUseCase(TopologyGraph graph, Guid edgeId, int? feedbackPointNumber)
    {
        _graph = graph;
        _edgeId = edgeId;
        _number = feedbackPointNumber;
    }

    public void Execute()
    {
        var edge = _graph.Edges.First(e => e.Id == _edgeId);
        edge.FeedbackPointNumber = _number;
    }
}