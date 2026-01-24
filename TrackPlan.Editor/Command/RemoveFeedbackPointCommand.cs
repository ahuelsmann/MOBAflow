// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Command;

public sealed class RemoveFeedbackPointCommand
{
    private readonly TopologyGraph _graph;

    public RemoveFeedbackPointCommand(TopologyGraph graph)
    {
        _graph = graph;
    }

    public void Execute(Guid edgeId)
    {
        new AssignFeedbackPointToTrackUseCase(
            _graph,
            edgeId,
            null
        ).Execute();
    }
}