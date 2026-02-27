namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Interface;
using TrackPlan.Renderer;

/// <summary>
/// ViewModel wrapper for <see cref="TrackPlan"/> used by the track plan editor UI.
/// </summary>
public sealed class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackPlanViewModel"/> class.
    /// </summary>
    /// <param name="model">The track plan domain model.</param>
    public TrackPlanViewModel(TrackPlan model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    /// <summary>
    /// Gets the underlying track plan domain model.
    /// </summary>
    public TrackPlan Model => _model;
}