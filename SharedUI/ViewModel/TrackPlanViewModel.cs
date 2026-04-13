namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;

using Interface;

using Microsoft.Extensions.Logging;

using TrackPlan.Renderer;

/// <summary>
/// ViewModel wrapper for <see cref="TrackPlan"/> used by the track plan editor UI.
/// </summary>
public sealed class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrackPlanViewModel> _logger;
    private bool _isToolboxExpanded;
    private bool _isPropertiesExpanded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackPlanViewModel"/> class.
    /// </summary>
    /// <param name="model">The track plan domain model.</param>
    /// <param name="settings">Application settings (layout persistence).</param>
    /// <param name="settingsService">Settings service for persisting layout changes.</param>
    /// <param name="logger">Logger for persistence failures.</param>
    public TrackPlanViewModel(TrackPlan model, AppSettings settings, ISettingsService settingsService, ILogger<TrackPlanViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);
        _model = model;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        _isToolboxExpanded = _settings.Layout.TrackPlanPage.IsToolboxExpanded;
        _isPropertiesExpanded = _settings.Layout.TrackPlanPage.IsPropertiesExpanded;
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
            PersistSettingsSafely();
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
            PersistSettingsSafely();
        }
    }

    private void PersistSettingsSafely()
    {
        _settingsService.SaveSettingsAsync(_settings).ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogWarning(t.Exception.GetBaseException(), "Track plan layout settings save failed");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}