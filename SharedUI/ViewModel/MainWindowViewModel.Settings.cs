// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Service;

using System.Collections.ObjectModel;

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

    /// <summary>
    /// List of available speech engines for selection.
    /// </summary>
    public string[] AvailableSpeechEngines { get; } =
    [
        "System Speech (Windows SAPI)",
        "Azure Cognitive Services"
    ];

    /// <summary>
    /// Currently selected speech engine name.
    /// </summary>
    public string? SelectedSpeechEngine
    {
        get => _settings.Speech.SpeakerEngineName ?? AvailableSpeechEngines.FirstOrDefault() ?? "None";
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
        SelectedSpeechEngine?.Contains("Azure", StringComparison.OrdinalIgnoreCase) == true;

    public bool ResetWindowLayoutOnStart
    {
        get => _settings.Application.ResetWindowLayoutOnStart;
        set
        {
            if (_settings.Application.ResetWindowLayoutOnStart != value)
            {
                _settings.Application.ResetWindowLayoutOnStart = value;
                OnPropertyChanged();
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

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
                var addresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();

                return addresses.Count > 0
                    ? string.Join(", ", addresses)
                    : "No network connection";
            }
            catch
            {
                return "Unable to determine";
            }
        }
    }

    /// <summary>
    /// Gets the full REST API URL including port.
    /// </summary>
    public string RestApiUrl => $"http://{LocalIpAddress}:{RestApiPort}";

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
        get => _settings.FeatureToggles?.IsOverviewPageAvailable ?? true;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsOverviewPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsSolutionPageAvailable ?? true;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsSolutionPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsSettingsPageAvailable ?? true;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsSettingsPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsJourneysPageAvailable ?? true;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsJourneysPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsWorkflowsPageAvailable ?? true;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsWorkflowsPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsTrackPlanEditorPageAvailable ?? false;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsTrackPlanEditorPageAvailable != value)
            {
                _settings.FeatureToggles.IsTrackPlanEditorPageAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTrackPlanEditorPageAvailable));
                _ = _settingsService?.SaveSettingsAsync(_settings);
            }
        }
    }

    public bool IsJourneyMapPageAvailableSetting
    {
        get => _settings.FeatureToggles?.IsJourneyMapPageAvailable ?? false;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsJourneyMapPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsMonitorPageAvailable ?? false;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsMonitorPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsTrainsPageAvailable ?? false;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsTrainsPageAvailable != value)
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
        get => _settings.FeatureToggles?.IsTrainControlPageAvailable ?? false;
        set
        {
            if (_settings.FeatureToggles != null && _settings.FeatureToggles.IsTrainControlPageAvailable != value)
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
    public bool IsOverviewPageAvailable => _settings.FeatureToggles is { IsOverviewPageAvailable: true };

    /// <summary>
    /// Gets whether the Solution page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsSolutionPageAvailable => _settings.FeatureToggles is { IsSolutionPageAvailable: true };

    /// <summary>
    /// Gets whether the Journeys page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsJourneysPageAvailable => _settings.FeatureToggles is { IsJourneysPageAvailable: true };

    /// <summary>
    /// Gets whether the Workflows page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsWorkflowsPageAvailable => _settings.FeatureToggles is { IsWorkflowsPageAvailable: true };

    /// <summary>
    /// Gets whether the Track Plan Editor page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailable => _settings.FeatureToggles is { IsTrackPlanEditorPageAvailable: true };

    /// <summary>
    /// Gets whether the Journey Map page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsJourneyMapPageAvailable => _settings.FeatureToggles is { IsJourneyMapPageAvailable: true };

    /// <summary>
    /// Gets whether the Settings page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsSettingsPageAvailable => _settings.FeatureToggles is { IsSettingsPageAvailable: true };

    /// <summary>
    /// Gets whether the Monitor page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsMonitorPageAvailable => _settings.FeatureToggles is { IsMonitorPageAvailable: true };

    /// <summary>
    /// Gets whether the Trains page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrainsPageAvailable => _settings.FeatureToggles is { IsTrainsPageAvailable: true };

    /// <summary>
    /// Gets whether the Train Control page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrainControlPageAvailable => _settings.FeatureToggles is { IsTrainControlPageAvailable: true };

    // Feature Toggle Labels (optional)

    public string? OverviewPageLabel => _settings.FeatureToggles?.OverviewPageLabel;
    public string? SolutionPageLabel => _settings.FeatureToggles?.SolutionPageLabel;
    public string? JourneysPageLabel => _settings.FeatureToggles?.JourneysPageLabel;
    public string? WorkflowsPageLabel => _settings.FeatureToggles?.WorkflowsPageLabel;
    public string? TrackPlanEditorPageLabel => _settings.FeatureToggles?.TrackPlanEditorPageLabel;
    public string? JourneyMapPageLabel => _settings.FeatureToggles?.JourneyMapPageLabel;
    public string? SettingsPageLabel => _settings.FeatureToggles?.SettingsPageLabel;
    public string? MonitorPageLabel => _settings.FeatureToggles?.MonitorPageLabel;

    // Settings Page CheckBox Content (with labels)

    public string TrackPlanEditorCheckBoxContent => FormatPageContent("Track Plan Editor Page", TrackPlanEditorPageLabel);
    public string JourneyMapCheckBoxContent => FormatPageContent("Journey Map Page", JourneyMapPageLabel);
    public string MonitorCheckBoxContent => FormatPageContent("Monitor Page", MonitorPageLabel);

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
            OnPropertyChanged(nameof(SpeechKey));
            OnPropertyChanged(nameof(SpeechRegion));
            OnPropertyChanged(nameof(SpeechRate));
            OnPropertyChanged(nameof(SpeechVolume));
            OnPropertyChanged(nameof(SelectedSpeechEngine));
            OnPropertyChanged(nameof(IsAzureSpeechEngineSelected));
            OnPropertyChanged(nameof(ResetWindowLayoutOnStart));
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
    public IRelayCommand<string?> SelectSpeechEngineCommand =>
        field ??= new RelayCommand<string?>(engine => SelectedSpeechEngine = engine);

    [RelayCommand]
    private async Task TestSpeechAsync()
    {
        try
        {
            ShowErrorMessage = false;

            // Get the current speaker engine from DI (injected via AnnouncementService)
            var testMessage = "Dies ist ein Test der Sprachausgabe. Nächster Halt: Bielefeld Hauptbahnhof.";

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