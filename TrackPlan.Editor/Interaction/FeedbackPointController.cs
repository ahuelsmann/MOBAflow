// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Interaction;

using Moba.TrackPlan.Editor.ViewState;

public sealed class FeedbackPointController
{
    private readonly EditorViewState _viewState;

    public FeedbackPointController(EditorViewState viewState)
    {
        _viewState = viewState;
    }

    public void Preview(int? number)
    {
        _viewState.SelectedFeedbackPointNumber = number;
    }

    public void ClearPreview()
    {
        _viewState.SelectedFeedbackPointNumber = null;
    }
}