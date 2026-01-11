namespace Moba.TrackPlan.Editor.Command;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.UseCase;

public sealed class AssignFeedbackPointCommand
{
    private readonly TopologyGraph _graph;

    public AssignFeedbackPointCommand(TopologyGraph graph)
    {
        _graph = graph;
    }

    public void Execute(Guid edgeId, int? feedbackPointNumber)
    {
        new AssignFeedbackPointToTrackUseCase(
            _graph,
            edgeId,
            feedbackPointNumber
        ).Execute();
    }
}