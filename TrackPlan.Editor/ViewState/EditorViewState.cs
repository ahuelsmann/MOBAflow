// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.ViewState;

public sealed class EditorViewState
{
    public Guid? SelectedEdgeId { get; set; }

    public int? SelectedFeedbackPointNumber { get; set; }

    public bool ShowFeedbackPoint { get; set; } = true;

    /// <summary>
    /// Controls visibility of track code labels (e.g., "G231", "R9", "BWL").
    /// </summary>
    public bool ShowCodeLabels { get; set; } = true;
}