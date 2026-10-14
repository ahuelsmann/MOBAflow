// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

using Common.Configuration;

using Interface;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The persisted train-control preferences: ramp, speed steps, locomotive series and Vmax, and the last
/// selected locomotive. The desktop (WinUI) and phone (MAUI) hosts keep their own selected locomotive.
/// </summary>
public sealed partial class TrainControlSettingsStore
{
    private readonly ISettingsService _settingsService;
    private readonly TrainControlHost _host;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainControlSettingsStore"/> class.
    /// </summary>
    /// <param name="settingsService">Reads and saves the application settings.</param>
    /// <param name="host">The host whose locomotive selection is read and written.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public TrainControlSettingsStore(ISettingsService settingsService, TrainControlHost host, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        _settingsService = settingsService;
        _host = host;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the shared train-control preferences.</summary>
    public TrainControlSettings Preferences => _settingsService.GetSettings().TrainControl;

    /// <summary>Gets the locomotive this host selected last.</summary>
    public Guid? SavedLocomotiveId => HostSettings(_settingsService.GetSettings()).SelectedLocomotiveFromProjectId;

    /// <summary>Records this host's selected locomotive in memory; the next save persists it.</summary>
    public void RememberLocomotive(Guid locomotiveId) =>
        HostSettings(_settingsService.GetSettings()).SelectedLocomotiveFromProjectId = locomotiveId;

    /// <summary>Saves the locomotive series and its Vmax; a failing save is logged.</summary>
    public async Task SaveSeriesAsync(string series, int vmax)
    {
        try
        {
            var settings = _settingsService.GetSettings();
            settings.TrainControl.SelectedLocoSeries = series;
            settings.TrainControl.SelectedVmax = vmax;
            await _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogSeriesSaveFailed(_logger, ex);
        }
    }

    /// <summary>
    /// Saves the ramp, the speed steps and, when given, this host's selected locomotive; a failing save is logged.
    /// </summary>
    public async Task SaveAsync(double rampStepSize, double rampIntervalMs, DccSpeedSteps speedSteps, Guid? selectedLocomotiveId)
    {
        try
        {
            var settings = _settingsService.GetSettings();
            settings.TrainControl.SpeedRampStepSize = (int)rampStepSize;
            settings.TrainControl.SpeedRampIntervalMs = (int)rampIntervalMs;
            settings.TrainControl.SpeedSteps = speedSteps;
            if (selectedLocomotiveId.HasValue)
            {
                HostSettings(settings).SelectedLocomotiveFromProjectId = selectedLocomotiveId.Value;
            }

            await _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
            var savedLocomotiveId = HostSettings(settings).SelectedLocomotiveFromProjectId;
            LogSaved(_logger, _host, savedLocomotiveId);
        }
        catch (Exception ex)
        {
            LogSaveFailed(_logger, ex);
        }
    }

    private TrainControlHostSettings HostSettings(AppSettings settings) =>
        _host == TrainControlHost.Maui ? settings.MauiTrainControlHost : settings.WinUiTrainControlHost;

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save locomotive series settings")]
    private static partial void LogSeriesSaveFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved train control settings for host {Host}: LocomotiveId={LocoId}")]
    private static partial void LogSaved(ILogger logger, TrainControlHost host, Guid? locoId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save train control settings")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception);
}
