namespace Moba.TrackPlan.Editor.ViewState;

public sealed class EditorViewState
{
    public Guid? SelectedEdgeId { get; set; }

    public int? SelectedFeedbackPointNumber { get; set; }

    public bool ShowFeedbackPoint { get; set; } = true;
}