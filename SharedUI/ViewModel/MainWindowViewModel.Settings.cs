// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using Common.Navigation;

using CommunityToolkit.Mvvm.Input;

using Domain;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

/// <summary>
/// MainWindowViewModel - Settings Management
/// Handles application settings properties and persistence (Z21, Speech, Application, HealthCheck).
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
    /// <summary>
    /// Gets or sets the currently selected IP address for the Z21 command station.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the default UDP port used to connect to the Z21 command station.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the retry interval in seconds for the automatic Z21 auto-connect logic.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the polling interval in seconds for Z21 system state updates.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the API key used for Azure Cognitive Services speech synthesis.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the Azure region used for the speech service.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the speech rate used for synthesized announcements.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the speech output volume as a percentage value.
    /// </summary>
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
    /// Gets or sets the name of the Azure voice used for announcements.
    /// </summary>
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
    /// Custom test message for speech synthesis test.
    /// User can modify this text in Settings UI.
    /// </summary>
    public string SpeechTestMessage
    {
        get => _settings.Speech.TestMessage;
        set
        {
            if (_settings.Speech.TestMessage != value)
            {
                _settings.Speech.TestMessage = value;
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

    /// <summary>
    /// Gets or sets a value indicating whether the last used solution is automatically loaded on startup.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether the WebApp should be started automatically with the desktop app.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the TCP port used by the REST API hosted by the WebApp.
    /// </summary>
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
    /// <summary>
    /// Gets a comma-separated list of local IPv4 addresses for displaying REST API endpoints.
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

    /// <summary>
    /// Gets or sets a value indicating whether the periodic health check is enabled.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the interval in seconds between health check executions.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the configured number of feedback points used for lap counting statistics.
    /// </summary>
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

                // Immediately update Track Statistics on Overview page (replaces collection, safe in property setter)
                InitializeStatisticsFromFeedbackPoints();
            }
        }
    }

    #endregion

    #region Feature Toggle Items (dynamisch aus NavigationRegistration)

    /// <summary>
    /// Collection of feature toggle entries for the dynamic Settings UI.
    /// Populated from IFeatureTogglePageProvider (empty if provider is not injected).
    /// </summary>
    public ObservableCollection<FeatureToggleItemViewModel> FeatureToggleItems { get; } = [];

    /// <summary>
    /// Initializes FeatureToggleItems from the provider. Called by the constructor.
    /// </summary>
    internal void InitializeFeatureToggleItems()
    {
        if (_featureTogglePageProvider == null) return;

        FeatureToggleItems.Clear();
        foreach (var info in _featureTogglePageProvider.GetToggleablePages())
        {
            var initial = GetFeatureToggleValue(info.FeatureToggleKey);
            var item = new FeatureToggleItemViewModel(info, initial);
            item.OnIsCheckedChangedCallback = OnFeatureToggleItemChanged;
            FeatureToggleItems.Add(item);
        }
    }

    private void OnFeatureToggleItemChanged(string key, bool value)
    {
        SetFeatureToggleValue(key, value);
    }

    /// <summary>
    /// Reads the feature toggle value via reflection from FeatureToggleSettings.
    /// </summary>
    internal bool GetFeatureToggleValue(string key)
    {
        var prop = typeof(FeatureToggleSettings).GetProperty(key);
        if (prop == null) return true;
        return prop.GetValue(_settings.FeatureToggles) as bool? ?? true;
    }

    /// <summary>
    /// Writes the feature toggle value via reflection to FeatureToggleSettings and saves.
    /// Raises OnPropertyChanged for the corresponding read-only property (e.g. IsOverviewPageAvailable).
    /// </summary>
    internal void SetFeatureToggleValue(string key, bool value)
    {
        var prop = typeof(FeatureToggleSettings).GetProperty(key);
        if (prop == null) return;

        var current = prop.GetValue(_settings.FeatureToggles) as bool?;
        if (current == value) return;

        prop.SetValue(_settings.FeatureToggles, value);
        OnPropertyChanged(key); // z. B. IsOverviewPageAvailable
        _ = _settingsService?.SaveSettingsAsync(_settings);
    }

    /// <summary>
    /// Updates all FeatureToggleItems after a reset (e.g. Reset to Defaults).
    /// </summary>
    internal void RefreshFeatureToggleItems()
    {
        foreach (var item in FeatureToggleItems)
        {
            item.SetChecked(GetFeatureToggleValue(item.FeatureToggleKey));
        }
    }

    #endregion

    #region Feature Toggle Wrapper Properties (Legacy – still used for NavigationView)

    /// <summary>
    /// Gets or sets whether the Overview page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Solution page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Settings page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Journeys page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Workflows page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Track Plan Editor page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Signal Box page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Journey Map page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Monitor page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Trains page is enabled in the navigation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the Train Control page is enabled in the navigation.
    /// </summary>
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

    #region Signal Box / Viessmann Multiplex-Signale

    private void SetSignalBoxInvert(int offset, bool value)
    {
        if (_settings.SignalBox == null) return;
        var sb = _settings.SignalBox;
        bool changed;
        string? propertyName;
        switch (offset)
        {
            case 0:
                changed = sb.InvertPolarityOffset0 != value;
                if (changed) sb.InvertPolarityOffset0 = value;
                propertyName = nameof(InvertPolarityOffset0Setting);
                break;
            case 1:
                changed = sb.InvertPolarityOffset1 != value;
                if (changed) sb.InvertPolarityOffset1 = value;
                propertyName = nameof(InvertPolarityOffset1Setting);
                break;
            case 2:
                changed = sb.InvertPolarityOffset2 != value;
                if (changed) sb.InvertPolarityOffset2 = value;
                propertyName = nameof(InvertPolarityOffset2Setting);
                break;
            case 3:
                changed = sb.InvertPolarityOffset3 != value;
                if (changed) sb.InvertPolarityOffset3 = value;
                propertyName = nameof(InvertPolarityOffset3Setting);
                break;
            default:
                changed = false;
                propertyName = null;
                break;
        }
        if (changed && propertyName != null)
        {
            OnPropertyChanged(propertyName);
            _ = _settingsService?.SaveSettingsAsync(_settings);
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
    /// Gets whether the Docking page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsDockingPageAvailable => _settings.FeatureToggles.IsDockingPageAvailable;

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

    // Feature Toggle Labels (optional)

    /// <summary>
    /// Gets the optional label override for the Overview page.
    /// </summary>
    public string OverviewPageLabel => _settings.FeatureToggles.OverviewPageLabel;
    /// <summary>
    /// Gets the optional label override for the Solution page.
    /// </summary>
    public string SolutionPageLabel => _settings.FeatureToggles.SolutionPageLabel;
    /// <summary>
    /// Gets the optional label override for the Journeys page.
    /// </summary>
    public string JourneysPageLabel => _settings.FeatureToggles.JourneysPageLabel;
    /// <summary>
    /// Gets the optional label override for the Workflows page.
    /// </summary>
    public string WorkflowsPageLabel => _settings.FeatureToggles.WorkflowsPageLabel;
    /// <summary>
    /// Gets the optional label override for the Track Plan Editor page.
    /// </summary>
    public string TrackPlanEditorPageLabel => _settings.FeatureToggles.TrackPlanEditorPageLabel;
    /// <summary>
    /// Gets the optional label override for the Signal Box page.
    /// </summary>
    public string SignalBoxPageLabel => _settings.FeatureToggles.SignalBoxPageLabel;
    /// <summary>
    /// Gets the optional label override for the Journey Map page.
    /// </summary>
    public string JourneyMapPageLabel => _settings.FeatureToggles.JourneyMapPageLabel;
    /// <summary>
    /// Gets the optional label override for the Settings page.
    /// </summary>
    public string SettingsPageLabel => _settings.FeatureToggles.SettingsPageLabel;
    /// <summary>
    /// Gets the optional label override for the Docking page.
    /// </summary>
    public string DockingPageLabel => _settings.FeatureToggles.DockingPageLabel;
    /// <summary>
    /// Gets the optional label override for the Monitor page.
    /// </summary>
    public string MonitorPageLabel => _settings.FeatureToggles.MonitorPageLabel;
    /// <summary>
    /// Gets the optional label override for the Trains page.
    /// </summary>
    public string TrainsPageLabel => _settings.FeatureToggles.TrainsPageLabel;
    /// <summary>
    /// Gets the optional label override for the Train Control page.
    /// </summary>
    public string TrainControlPageLabel => _settings.FeatureToggles.TrainControlPageLabel;

    // Settings Page CheckBox Content (with labels)

    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Track Plan Editor page.
    /// </summary>
    public string TrackPlanEditorCheckBoxContent => FormatPageContent("Track Plan Editor Page", TrackPlanEditorPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Signal Box page.
    /// </summary>
    public string SignalBoxCheckBoxContent => FormatPageContent("Signal Box Page", SignalBoxPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Journey Map page.
    /// </summary>
    public string JourneyMapCheckBoxContent => FormatPageContent("Journey Map Page", JourneyMapPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Docking page.
    /// </summary>
    public string DockingCheckBoxContent => FormatPageContent("Docking Page", DockingPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Monitor page.
    /// </summary>
    public string MonitorCheckBoxContent => FormatPageContent("Monitor Page", MonitorPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Trains page.
    /// </summary>
    public string TrainsCheckBoxContent => FormatPageContent("Trains Page", TrainsPageLabel);
    /// <summary>
    /// Gets the checkbox label for enabling or disabling the Train Control page.
    /// </summary>
    public string TrainControlCheckBoxContent => FormatPageContent("Train Control Page", TrainControlPageLabel);

    private static string FormatPageContent(string pageName, string? label)
    {
        return string.IsNullOrWhiteSpace(label) ? pageName : $"{pageName} ({label})";
    }

    #endregion

    #region Settings Commands
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
            OnPropertyChanged(nameof(IsSignalBoxPageAvailableSetting));
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
            OnPropertyChanged(nameof(IsSignalBoxPageAvailable));
            OnPropertyChanged(nameof(IsJourneyMapPageAvailable));
            OnPropertyChanged(nameof(IsDockingPageAvailable));
            OnPropertyChanged(nameof(IsMonitorPageAvailable));
            OnPropertyChanged(nameof(IsTrainsPageAvailable));
            OnPropertyChanged(nameof(IsTrainControlPageAvailable));

            RefreshFeatureToggleItems();

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
        field ??= new RelayCommand<string>(engine => SelectedSpeechEngine = engine ?? string.Empty);

    [RelayCommand]
    private async Task TestSpeechAsync()
    {
        try
        {
            Debug.WriteLine("[TEST SPEECH] Button clicked");
            Debug.WriteLine($"[TEST SPEECH] Speech key configured: {!string.IsNullOrWhiteSpace(_settings.Speech.Key)}");
            Debug.WriteLine($"[TEST SPEECH] Speech key length: {_settings.Speech.Key.Length}");
            Debug.WriteLine($"[TEST SPEECH] Speech region: {_settings.Speech.Region}");
            Debug.WriteLine($"[TEST SPEECH] Selected engine: {_settings.Speech.SpeakerEngineName}");

            // FIX: Reset error state on UI thread
            _uiDispatcher.InvokeOnUi(() =>
            {
                ShowErrorMessage = false;
                ErrorMessage = string.Empty;
            });

            // Use custom test message from settings (user can modify in UI)
            var testMessage = SpeechTestMessage;

            Debug.WriteLine($"[TEST SPEECH] AnnouncementService available: {_announcementService != null}");

            // Use the announcement service if available
            if (_announcementService != null)
            {
                // FIX: Check if speaker engine is properly configured
                if (!_announcementService.IsSpeakerEngineAvailable)
                {
                    Debug.WriteLine("[TEST SPEECH] Speaker engine not available");

                    _uiDispatcher.InvokeOnUi(() =>
                    {
                        ErrorMessage = "Speech engine not configured. Please configure Azure Speech Service in Settings or select Windows SAPI engine.";
                        ShowErrorMessage = true;
                    });
                    return;
                }

                Debug.WriteLine("[TEST SPEECH] Calling GenerateAndSpeakAnnouncementAsync...");

                var testJourney = new Journey { Text = testMessage };
                var testStation = new Station { Name = "Test", IsExitOnLeft = false };
                await _announcementService.GenerateAndSpeakAnnouncementAsync(testJourney, testStation, 1).ConfigureAwait(false);

                Debug.WriteLine("[TEST SPEECH] GenerateAndSpeakAnnouncementAsync completed");

                // SUCCESS: Show success message on UI thread
                _uiDispatcher.InvokeOnUi(() =>
                {
                    ShowSuccessMessage = true;
                    ShowErrorMessage = false;
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }
    #endregion
}