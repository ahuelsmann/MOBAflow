namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Interface;
using TrackPlan.Renderer;
using Common.Configuration;

/// <summary>
/// ViewModel wrapper for <see cref="TrackPlan"/> used by the track plan editor UI.
/// </summary>
public sealed class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private bool _isToolboxExpanded;
    private bool _isPropertiesExpanded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackPlanViewModel"/> class.
    /// </summary>
    /// <param name="model">The track plan domain model.</param>
    public TrackPlanViewModel(TrackPlan model, AppSettings settings, ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        _model = model;
        _settings = settings;
        _settingsService = settingsService;
        _isToolboxExpanded = _settings.Layout.TrackPlanPage?.IsToolboxExpanded ?? true;
        _isPropertiesExpanded = _settings.Layout.TrackPlanPage?.IsPropertiesExpanded ?? true;
    }

    /// <summary>
    /// Gets the underlying track plan domain model.
    /// </summary>
    public TrackPlan Model => _model;

    public bool IsToolboxExpanded
    {
        get => _isToolboxExpanded;
        set
        {
            if (!SetProperty(ref _isToolboxExpanded, value))
                return;

            _settings.Layout.TrackPlanPage.IsToolboxExpanded = value;
            _ = _settingsService.SaveSettingsAsync(_settings);
        }
    }

    public bool IsPropertiesExpanded
    {
        get => _isPropertiesExpanded;
        set
        {
            if (!SetProperty(ref _isPropertiesExpanded, value))
                return;

            _settings.Layout.TrackPlanPage.IsPropertiesExpanded = value;
            _ = _settingsService.SaveSettingsAsync(_settings);
        }
    }
}