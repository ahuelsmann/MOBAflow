namespace Moba.TrackPlan.Editor.ViewModel;

using Moba.TrackPlan.Editor.ViewState;
using Moba.TrackPlan.Graph;

public sealed class TrackPlanEditorViewModel
{
    public TopologyGraph Graph { get; }

    public EditorViewState ViewState { get; } = new();
    public SelectionState Selection { get; } = new();
    public VisibilityState Visibility { get; } = new();

    public TrackPlanEditorViewModel(TopologyGraph graph)
    {
        Graph = graph;
    }
}