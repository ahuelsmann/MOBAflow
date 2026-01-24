// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Command;

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