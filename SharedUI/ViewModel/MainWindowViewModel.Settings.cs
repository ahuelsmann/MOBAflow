// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Service;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// MainWindowViewModel - Settings Management
/// Handles application settings properties and persistence (Z21, CityLibrary, Speech, Application, HealthCheck).
/// Automatically saves settings immediately after each change.
/// </summary>
public partial class MainWindowViewModel
{
    #region Settings Properties
    /// <summary>
    /// Application settings - exposed for direct binding.
    /// Settings are stored in appsettings.json (not in Solution).
    /// </summary>
    public AppSettings Settings => _settings;

    /// <summary>
    /// Available IP addresses for Z21 connection (from recent connections).
    /// </summary>
    public ObservableCollection<string> AvailableIpAddresses => new(_settings.Z21.RecentIpAddresses);

    // Wrapper properties for Settings page bindings
    public string IpAddress
    {
        get => _settings.Z21.CurrentIpAddress;
        set
        {
            if (_settings.Z21.CurrentIpAddress != value)
            {
                _settings.Z21.CurrentIpAddress = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string Port
    {
        get => _settings.Z21.DefaultPort;
        set
        {
            if (_settings.Z21.DefaultPort != value)
            {
                _settings.Z21.DefaultPort = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public double Z21AutoConnectRetryInterval
    {
        get => _settings.Z21.AutoConnectRetryIntervalSeconds;
        set
        {
            if (_settings.Z21.AutoConnectRetryIntervalSeconds != (int)value)
            {
                _settings.Z21.AutoConnectRetryIntervalSeconds = (int)value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public double Z21SystemStatePollingInterval
    {
        get => _settings.Z21.SystemStatePollingIntervalSeconds;
        set
        {
            if (_settings.Z21.SystemStatePollingIntervalSeconds != (int)value)
            {
                _settings.Z21.SystemStatePollingIntervalSeconds = (int)value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string CityLibraryPath
    {
        get => _settings.CityLibrary.FilePath;
        set
        {
            if (_settings.CityLibrary.FilePath != value)
            {
                _settings.CityLibrary.FilePath = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool CityLibraryAutoReload
    {
        get => _settings.CityLibrary.AutoReload;
        set
        {
            if (_settings.CityLibrary.AutoReload != value)
            {
                _settings.CityLibrary.AutoReload = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string LocomotiveLibraryPath
    {
        get => _settings.LocomotiveLibrary.FilePath;
        set
        {
            if (_settings.LocomotiveLibrary.FilePath != value)
            {
                _settings.LocomotiveLibrary.FilePath = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool LocomotiveLibraryAutoReload
    {
        get => _settings.LocomotiveLibrary.AutoReload;
        set
        {
            if (_settings.LocomotiveLibrary.AutoReload != value)
            {
                _settings.LocomotiveLibrary.AutoReload = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string? SpeechKey
    {
        get => _settings.Speech.Key;
        set
        {
            if (_settings.Speech.Key != value)
            {
                _settings.Speech.Key = value ?? string.Empty;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string SpeechRegion
    {
        get => _settings.Speech.Region;
        set
        {
            if (_settings.Speech.Region != value)
            {
                _settings.Speech.Region = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public int SpeechRate
    {
        get => _settings.Speech.Rate;
        set
        {
            if (_settings.Speech.Rate != value)
            {
                _settings.Speech.Rate = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public double SpeechVolume
    {
        get => _settings.Speech.Volume;
        set
        {
            if ((uint)value != _settings.Speech.Volume)
            {
                _settings.Speech.Volume = (uint)value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public string VoiceName
    {
        get => _settings.Speech.VoiceName;
        set
        {
            if (_settings.Speech.VoiceName != value)
            {
                _settings.Speech.VoiceName = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    /// <summary>
    /// List of available speech engines for selection.
    /// </summary>
    public ObservableCollection<string> AvailableSpeechEngines { get; } =
    [
        "System Speech (Windows SAPI)",
        "Azure Cognitive Services"
    ];

    /// <summary>
    /// List of available Azure Cognitive Services voices (German).
    /// </summary>
    public ObservableCollection<string> AvailableVoiceNames { get; } =
    [
        "de-DE-KatjaNeural",
        "de-DE-ConradNeural",
        "de-DE-AmalaNeural",
        "de-DE-BerndNeural",
        "de-DE-ChristophNeural",
        "de-DE-ElkeNeural",
        "de-DE-GiselaNeural",
        "de-DE-KasperNeural",
        "de-DE-KillianNeural",
        "de-DE-KlarissaNeural",
        "de-DE-KlausNeural",
        "de-DE-LouisaNeural",
        "de-DE-MajaNeural",
        "de-DE-RalfNeural",
        "de-DE-TanjaNeural"
    ];

    /// <summary>
    /// Currently selected speech engine name.
    /// </summary>
    public string SelectedSpeechEngine
    {
        get => _settings.Speech.SpeakerEngineName;
        set
        {
            if (_settings.Speech.SpeakerEngineName != value)
            {
                _settings.Speech.SpeakerEngineName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAzureSpeechEngineSelected));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    /// <summary>
    /// Returns true if Azure Cognitive Services is selected.
    /// Used to show/hide Azure-specific settings.
    /// </summary>
    public bool IsAzureSpeechEngineSelected =>
        SelectedSpeechEngine.Contains("Azure", StringComparison.OrdinalIgnoreCase);

    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set
        {
            if (_settings.Application.AutoLoadLastSolution != value)
            {
                _settings.Application.AutoLoadLastSolution = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool AutoStartWebApp
    {
        get => _settings.Application.AutoStartWebApp;
        set
        {
            if (_settings.Application.AutoStartWebApp != value)
            {
                _settings.Application.AutoStartWebApp = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    // REST API Settings
    public int RestApiPort
    {
        get => _settings.RestApi.Port;
        set
        {
            if (_settings.RestApi.Port != value)
            {
                _settings.RestApi.Port = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    /// <summary>
    /// Gets the local IP address(es) for the REST API endpoint.
    /// Returns all IPv4 addresses of the machine.
    /// </summary>
    public string LocalIpAddress
    {
        get
        {
            try
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();

                return addresses.Count > 0
                    ? string.Join(", ", addresses)
                    : "No network connection";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                ShowErrorMessage = true;

                return $"Unable to determine {ex.Message}";
            }
        }
    }

    public bool HealthCheckEnabled
    {
        get => _settings.HealthCheck.Enabled;
        set
        {
            if (_settings.HealthCheck.Enabled != value)
            {
                _settings.HealthCheck.Enabled = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public double HealthCheckIntervalSeconds
    {
        get => _settings.HealthCheck.IntervalSeconds;
        set
        {
            if (_settings.HealthCheck.IntervalSeconds != (int)value)
            {
                _settings.HealthCheck.IntervalSeconds = (int)value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public double CountOfFeedbackPoints
    {
        get => _settings.Counter.CountOfFeedbackPoints;
        set
        {
            if (_settings.Counter.CountOfFeedbackPoints != (int)value)
            {
                _settings.Counter.CountOfFeedbackPoints = (int)value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);

                // Immediately update Track Statistics on Overview page
                InitializeStatisticsFromFeedbackPoints();
            }
        }
    }

    #endregion

    #region Feature Toggle Wrapper Properties

    public bool IsOverviewPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsOverviewPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsOverviewPageAvailable != value)
            {
                _settings.FeatureToggles.IsOverviewPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOverviewPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsSolutionPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsSolutionPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsSolutionPageAvailable != value)
            {
                _settings.FeatureToggles.IsSolutionPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSolutionPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsSettingsPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsSettingsPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsSettingsPageAvailable != value)
            {
                _settings.FeatureToggles.IsSettingsPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSettingsPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsJourneysPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsJourneysPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsJourneysPageAvailable != value)
            {
                _settings.FeatureToggles.IsJourneysPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsJourneysPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsWorkflowsPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsWorkflowsPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsWorkflowsPageAvailable != value)
            {
                _settings.FeatureToggles.IsWorkflowsPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsWorkflowsPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsTrackPlanEditorPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsTrackPlanEditorPageAvailable != value)
            {
                _settings.FeatureToggles.IsTrackPlanEditorPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTrackPlanEditorPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsSignalBoxPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsSignalBoxPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsSignalBoxPageAvailable != value)
            {
                _settings.FeatureToggles.IsSignalBoxPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSignalBoxPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsJourneyMapPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsJourneyMapPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsJourneyMapPageAvailable != value)
            {
                _settings.FeatureToggles.IsJourneyMapPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsJourneyMapPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsMonitorPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsMonitorPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsMonitorPageAvailable != value)
            {
                _settings.FeatureToggles.IsMonitorPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonitorPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsTrainsPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrainsPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsTrainsPageAvailable != value)
            {
                _settings.FeatureToggles.IsTrainsPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTrainsPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsTrainControlPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrainControlPageAvailable;
        set
        {
            if (_settings.FeatureToggles.IsTrainControlPageAvailable != value)
            {
                _settings.FeatureToggles.IsTrainControlPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTrainControlPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    #endregion

    #region Feature Toggle Properties (Read-Only for NavigationView Visibility)

    /// <summary>
    /// Gets whether the Overview page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsOverviewPageAvailable => _settings.FeatureToggles.IsOverviewPageAvailable;

    /// <summary>
    /// Gets whether the Solution page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsSolutionPageAvailable => _settings.FeatureToggles.IsSolutionPageAvailable;

    /// <summary>
    /// Gets whether the Journeys page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsJourneysPageAvailable => _settings.FeatureToggles.IsJourneysPageAvailable;

    /// <summary>
    /// Gets whether the Workflows page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsWorkflowsPageAvailable => _settings.FeatureToggles.IsWorkflowsPageAvailable;

    /// <summary>
    /// Gets whether the Track Plan Editor page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailable => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable;

    /// <summary>
    /// Gets whether the Signal Box page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsSignalBoxPageAvailable => _settings.FeatureToggles.IsSignalBoxPageAvailable;

    /// <summary>
    /// Gets whether the Journey Map page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsJourneyMapPageAvailable => _settings.FeatureToggles.IsJourneyMapPageAvailable;

    /// <summary>
    /// Gets whether the Settings page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsSettingsPageAvailable => _settings.FeatureToggles.IsSettingsPageAvailable;

    /// <summary>
    /// Gets whether the Monitor page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsMonitorPageAvailable => _settings.FeatureToggles.IsMonitorPageAvailable;

    /// <summary>
    /// Gets whether the Trains page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrainsPageAvailable => _settings.FeatureToggles.IsTrainsPageAvailable;

    /// <summary>
    /// Gets whether the Train Control page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrainControlPageAvailable => _settings.FeatureToggles.IsTrainControlPageAvailable;

    /// <summary>
    /// Gets whether the Feedback Points page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsFeedbackPointsPageAvailable => _settings.FeatureToggles.IsFeedbackPointsPageAvailable;

    // Feature Toggle Labels (optional)

    public string OverviewPageLabel => _settings.FeatureToggles.OverviewPageLabel;
    public string SolutionPageLabel => _settings.FeatureToggles.SolutionPageLabel;
    public string JourneysPageLabel => _settings.FeatureToggles.JourneysPageLabel;
    public string WorkflowsPageLabel => _settings.FeatureToggles.WorkflowsPageLabel;
    public string TrackPlanEditorPageLabel => _settings.FeatureToggles.TrackPlanEditorPageLabel;
    public string SignalBoxPageLabel => _settings.FeatureToggles.SignalBoxPageLabel;
    public string JourneyMapPageLabel => _settings.FeatureToggles.JourneyMapPageLabel;
    public string SettingsPageLabel => _settings.FeatureToggles.SettingsPageLabel;
    public string MonitorPageLabel => _settings.FeatureToggles.MonitorPageLabel;
    public string TrainsPageLabel => _settings.FeatureToggles.TrainsPageLabel;
    public string TrainControlPageLabel => _settings.FeatureToggles.TrainControlPageLabel;
    public string FeedbackPointsPageLabel => _settings.FeatureToggles.FeedbackPointsPageLabel;

    // Settings Page CheckBox Content (with labels)

    public string TrackPlanEditorCheckBoxContent => FormatPageContent("Track Plan Editor Page", TrackPlanEditorPageLabel);
    public string SignalBoxCheckBoxContent => FormatPageContent("Signal Box Page", SignalBoxPageLabel);
    public string JourneyMapCheckBoxContent => FormatPageContent("Journey Map Page", JourneyMapPageLabel);
    public string MonitorCheckBoxContent => FormatPageContent("Monitor Page", MonitorPageLabel);
    public string TrainsCheckBoxContent => FormatPageContent("Trains Page", TrainsPageLabel);
    public string TrainControlCheckBoxContent => FormatPageContent("Train Control Page", TrainControlPageLabel);
    public string FeedbackPointsCheckBoxContent => FormatPageContent("Feedback Points Page", FeedbackPointsPageLabel);

    private static string FormatPageContent(string pageName, string? label)
    {
        return string.IsNullOrWhiteSpace(label) ? pageName : $"{pageName} ({label})";
    }

    #endregion

    #region Settings Commands
    [RelayCommand]
    private async Task BrowseCityLibraryAsync()
    {
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
        {
            ErrorMessage = "File browsing not supported on this platform";
            ShowErrorMessage = true;
            return;
        }

        try
        {
            var path = await _ioService.BrowseForJsonFileAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(path))
            {
                CityLibraryPath = path;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    [RelayCommand]
    private async Task BrowseLocomotiveLibraryAsync()
    {
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
        {
            ErrorMessage = "File browsing not supported on this platform";
            ShowErrorMessage = true;
            return;
        }

        try
        {
            var path = await _ioService.BrowseForJsonFileAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(path))
            {
                LocomotiveLibraryPath = path;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        if (_settingsService == null) return;

        try
        {
            ShowErrorMessage = false;
            await _settingsService.ResetToDefaultsAsync().ConfigureAwait(false);

            // Notify all settings properties changed
            OnPropertyChanged(nameof(IpAddress));
            OnPropertyChanged(nameof(Port));
            OnPropertyChanged(nameof(Z21AutoConnectRetryInterval));
            OnPropertyChanged(nameof(Z21SystemStatePollingInterval));
            OnPropertyChanged(nameof(CityLibraryPath));
            OnPropertyChanged(nameof(CityLibraryAutoReload));
            OnPropertyChanged(nameof(LocomotiveLibraryPath));
            OnPropertyChanged(nameof(LocomotiveLibraryAutoReload));
            OnPropertyChanged(nameof(SpeechKey));
            OnPropertyChanged(nameof(SpeechRegion));
            OnPropertyChanged(nameof(SpeechRate));
            OnPropertyChanged(nameof(SpeechVolume));
            OnPropertyChanged(nameof(VoiceName));
            OnPropertyChanged(nameof(SelectedSpeechEngine));
            OnPropertyChanged(nameof(IsAzureSpeechEngineSelected));
            OnPropertyChanged(nameof(AutoLoadLastSolution));
            OnPropertyChanged(nameof(AutoStartWebApp));
            OnPropertyChanged(nameof(RestApiPort));
            OnPropertyChanged(nameof(HealthCheckEnabled));
            OnPropertyChanged(nameof(HealthCheckIntervalSeconds));
            OnPropertyChanged(nameof(CountOfFeedbackPoints));

            // FeatureToggle wrapper properties
            OnPropertyChanged(nameof(IsOverviewPageAvailableSetting));
            OnPropertyChanged(nameof(IsSolutionPageAvailableSetting));
            OnPropertyChanged(nameof(IsSettingsPageAvailableSetting));
            OnPropertyChanged(nameof(IsJourneysPageAvailableSetting));
            OnPropertyChanged(nameof(IsWorkflowsPageAvailableSetting));
            OnPropertyChanged(nameof(IsTrackPlanEditorPageAvailableSetting));
            OnPropertyChanged(nameof(IsJourneyMapPageAvailableSetting));
            OnPropertyChanged(nameof(IsMonitorPageAvailableSetting));
            OnPropertyChanged(nameof(IsTrainsPageAvailableSetting));
            OnPropertyChanged(nameof(IsTrainControlPageAvailableSetting));

            // FeatureToggle read-only properties (for NavigationView)
            OnPropertyChanged(nameof(IsOverviewPageAvailable));
            OnPropertyChanged(nameof(IsSolutionPageAvailable));
            OnPropertyChanged(nameof(IsSettingsPageAvailable));
            OnPropertyChanged(nameof(IsJourneysPageAvailable));
            OnPropertyChanged(nameof(IsWorkflowsPageAvailable));
            OnPropertyChanged(nameof(IsTrackPlanEditorPageAvailable));
            OnPropertyChanged(nameof(IsJourneyMapPageAvailable));
            OnPropertyChanged(nameof(IsMonitorPageAvailable));
            OnPropertyChanged(nameof(IsTrainsPageAvailable));
            OnPropertyChanged(nameof(IsTrainControlPageAvailable));

            ShowSuccessMessage = true;
            await Task.Delay(3000).ConfigureAwait(false);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    /// <summary>
    /// Command to select a speech engine from the UI.
    /// </summary>
    public IRelayCommand<string> SelectSpeechEngineCommand =>
        field ??= new RelayCommand<string>(engine => SelectedSpeechEngine = engine);

    [RelayCommand]
    private async Task TestSpeechAsync()
    {
        try
        {
            ShowErrorMessage = false;

            // Test message for speech synthesis
            var testMessage = "This is a test of speech synthesis. Next stop: Central Station.";

            // Use the announcement service if available
            if (_announcementService != null)
            {
                var testJourney = new Journey { Text = testMessage };
                var testStation = new Station { Name = "Test", IsExitOnLeft = false };
                await _announcementService.GenerateAndSpeakAnnouncementAsync(testJourney, testStation, 1).ConfigureAwait(false);
            }
            else
            {
                ErrorMessage = "Speech service not available";
                ShowErrorMessage = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Speech test failed: {ex.Message}";
            ShowErrorMessage = true;
        }
    }
    #endregion
}