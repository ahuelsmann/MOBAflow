// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.ViewState;

public sealed class SelectionState
{
    public HashSet<Guid> SelectedEdgeIds { get; } = [];

    public void Clear() => SelectedEdgeIds.Clear();

    public void Select(Guid edgeId)
    {
        SelectedEdgeIds.Clear();
        SelectedEdgeIds.Add(edgeId);
    }

    public void Toggle(Guid edgeId)
    {
        if (!SelectedEdgeIds.Add(edgeId))
            SelectedEdgeIds.Remove(edgeId);
    }
}