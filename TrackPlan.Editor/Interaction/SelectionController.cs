// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Interaction;

using Moba.TrackPlan.Editor.ViewState;

public sealed class SelectionController
{
    private readonly SelectionState _selection;

    public SelectionController(SelectionState selection)
    {
        _selection = selection;
    }

    public void SelectSingle(Guid edgeId)
        => _selection.Select(edgeId);

    public void Toggle(Guid edgeId)
        => _selection.Toggle(edgeId);

    public void Clear()
        => _selection.Clear();
}