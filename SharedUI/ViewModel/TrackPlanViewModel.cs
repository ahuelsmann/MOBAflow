namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Interface;

using TrackPlan.Renderer;

public partial class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;

    public TrackPlanViewModel(TrackPlan model)
    {
        _model = model;
    }

    public TrackPlan Model => _model;
}