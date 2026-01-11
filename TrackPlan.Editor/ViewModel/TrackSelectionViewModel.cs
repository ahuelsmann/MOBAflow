namespace Moba.TrackPlan.Editor.ViewModel;

using Moba.TrackPlan.Editor.ViewState;

public sealed class TrackSelectionViewModel
{
    private readonly SelectionState _selection;

    public TrackSelectionViewModel(SelectionState selection)
    {
        _selection = selection;
    }

    public IReadOnlyCollection<Guid> SelectedEdges => _selection.SelectedEdgeIds;
}