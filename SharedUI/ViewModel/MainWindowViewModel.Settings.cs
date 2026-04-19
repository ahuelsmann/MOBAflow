// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Microsoft.Extensions.Logging;

using Moba.TrackLibrary.PikoA.Import;

using Moba.Vision;

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// MainWindowViewModel - Settings Management
/// Handles application settings properties and persistence (Z21, Speech, Application, HealthCheck).
/// Automatically saves settings immediately after each change.
/// </summary>
public partial class MainWindowViewModel
{
    private void PersistSettings()
    {
        PersistSettingsSafely();
    }

    private bool UpdateSetting<T>(
        Func<T> currentValue,
        Action<T> applyValue,
        T newValue,
        string propertyName,
        params string[] additionalPropertyNames)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue(), newValue))
        {
            return false;
        }

        applyValue(newValue);
        OnPropertyChanged(propertyName);
        foreach (var additionalPropertyName in additionalPropertyNames)
        {
            OnPropertyChanged(additionalPropertyName);
        }

        PersistSettings();
        return true;
    }

    private void UpdateFeatureToggleSetting(
        Func<bool> currentValue,
        Action<bool> applyValue,
        bool newValue,
        string settingPropertyName,
        string availabilityPropertyName)
    {
        _ = UpdateSetting(currentValue, applyValue, newValue, settingPropertyName, availabilityPropertyName);
    }

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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets the Azure AI Vision subscription key.
    /// </summary>
    public string? VisionKey
    {
        get => _settings.Vision.Key;
        set
        {
            var v = value ?? string.Empty;
            if (_settings.Vision.Key != v)
            {
                _settings.Vision.Key = v;
                OnPropertyChanged();
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets the Azure AI Vision endpoint URL (e.g. <c>https://&lt;resource&gt;.cognitiveservices.azure.com/</c>).
    /// </summary>
    public string VisionEndpoint
    {
        get => _settings.Vision.Endpoint;
        set
        {
            var v = value ?? string.Empty;
            if (_settings.Vision.Endpoint != v)
            {
                _settings.Vision.Endpoint = v;
                OnPropertyChanged();
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets an optional local image path used by the "Test Vision" button.
    /// </summary>
    public string VisionTestImagePath
    {
        get => _settings.Vision.TestImagePath;
        set
        {
            var v = value ?? string.Empty;
            if (_settings.Vision.TestImagePath != v)
            {
                _settings.Vision.TestImagePath = v;
                OnPropertyChanged();
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Human-readable result of the last "Test Vision" run, shown inline next to the button.
    /// Empty string = no result yet.
    /// </summary>
    [ObservableProperty]
    private string _visionTestResult = string.Empty;

    /// <summary>
    /// Severity of the last "Test Vision" run: "Success", "Error", "Warning" or "Informational".
    /// Bound to an InfoBar Severity converter in the Settings page.
    /// </summary>
    [ObservableProperty]
    private string _visionTestSeverity = "Informational";

    /// <summary>
    /// Controls visibility of the inline Vision test result InfoBar.
    /// </summary>
    [ObservableProperty]
    private bool _showVisionTestResult;

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
                PersistSettings();
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
                OnPropertyChanged(nameof(SpeechStatusDisplayText));
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Returns true if Azure Cognitive Services is selected.
    /// Used to show/hide Azure-specific settings.
    /// </summary>
    public bool IsAzureSpeechEngineSelected =>
        SpeechSpeakerEngineSelection.ShouldUseAzureCognitive(SelectedSpeechEngine);

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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets the base folder for MOBAflow photos (e.g. OneDrive path). Empty = My Documents\MOBAflow\Photos.
    /// </summary>
    public string PhotoStoragePath
    {
        get => _settings.Application.PhotoStoragePath;
        set
        {
            var v = value;
            if (_settings.Application.PhotoStoragePath != v)
            {
                _settings.Application.PhotoStoragePath = v;
                OnPropertyChanged();
                PersistSettings();
            }
        }
    }

    /// <summary>
    /// Gets the recommended local IPv4 for the REST API (MOBAsmart hint).
    /// Uses the same preference as UDP discovery: 192.168.x.x, then 10.x.x.x, then any other IPv4.
    /// Virtual adapters (WSL2/Docker vEthernet) often add extra 172.16–31.x addresses; those are not listed first.
    /// </summary>
    public string LocalIpAddress
    {
        get
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipv4 = host.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();

                if (ipv4.Count == 0)
                    return "No network connection";

                var s192 = ipv4.FirstOrDefault(ip => ip.ToString().StartsWith("192.168.", StringComparison.Ordinal));
                if (s192 is not null)
                    return s192.ToString();

                var s10 = ipv4.FirstOrDefault(ip => ip.ToString().StartsWith("10.", StringComparison.Ordinal));
                if (s10 is not null)
                    return s10.ToString();

                return ipv4[0].ToString();
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
                PersistSettings();
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
                PersistSettings();
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
                PersistSettings();

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
    /// Reads the feature toggle value from <see cref="FeatureToggleRegistry"/> (no reflection).
    /// </summary>
    internal bool GetFeatureToggleValue(string key)
    {
        if (FeatureToggleRegistry.TryGetPageAvailability(_settings.FeatureToggles, key, out var v))
            return v;
        return true;
    }

    /// <summary>
    /// Writes the feature toggle value via <see cref="FeatureToggleRegistry"/> and saves.
    /// Raises OnPropertyChanged for the corresponding read-only property (e.g. IsOverviewPageAvailable).
    /// </summary>
    internal void SetFeatureToggleValue(string key, bool value)
    {
        if (!FeatureToggleRegistry.TryGetPageAvailability(_settings.FeatureToggles, key, out var previous))
            return;
        if (previous == value)
            return;

        FeatureToggleRegistry.TrySetPageAvailability(_settings.FeatureToggles, key, value);
        OnPropertyChanged(key);
        PersistSettings();
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
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsOverviewPageAvailable,
            updated => _settings.FeatureToggles.IsOverviewPageAvailable = updated,
            value,
            nameof(IsOverviewPageAvailableSetting),
            nameof(IsOverviewPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Solution page is enabled in the navigation.
    /// </summary>
    public bool IsSolutionPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsSolutionPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsSolutionPageAvailable,
            updated => _settings.FeatureToggles.IsSolutionPageAvailable = updated,
            value,
            nameof(IsSolutionPageAvailableSetting),
            nameof(IsSolutionPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Journeys page is enabled in the navigation.
    /// </summary>
    public bool IsJourneysPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsJourneysPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsJourneysPageAvailable,
            updated => _settings.FeatureToggles.IsJourneysPageAvailable = updated,
            value,
            nameof(IsJourneysPageAvailableSetting),
            nameof(IsJourneysPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Workflows page is enabled in the navigation.
    /// </summary>
    public bool IsWorkflowsPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsWorkflowsPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsWorkflowsPageAvailable,
            updated => _settings.FeatureToggles.IsWorkflowsPageAvailable = updated,
            value,
            nameof(IsWorkflowsPageAvailableSetting),
            nameof(IsWorkflowsPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Track Plan Editor page is enabled in the navigation.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable,
            updated => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable = updated,
            value,
            nameof(IsTrackPlanEditorPageAvailableSetting),
            nameof(IsTrackPlanEditorPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Signal Box page is enabled in the navigation.
    /// </summary>
    public bool IsSignalBoxPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsSignalBoxPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsSignalBoxPageAvailable,
            updated => _settings.FeatureToggles.IsSignalBoxPageAvailable = updated,
            value,
            nameof(IsSignalBoxPageAvailableSetting),
            nameof(IsSignalBoxPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Journey Map page is enabled in the navigation.
    /// </summary>
    public bool IsJourneyMapPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsJourneyMapPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsJourneyMapPageAvailable,
            updated => _settings.FeatureToggles.IsJourneyMapPageAvailable = updated,
            value,
            nameof(IsJourneyMapPageAvailableSetting),
            nameof(IsJourneyMapPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Monitor page is enabled in the navigation.
    /// </summary>
    public bool IsMonitorPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsMonitorPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsMonitorPageAvailable,
            updated => _settings.FeatureToggles.IsMonitorPageAvailable = updated,
            value,
            nameof(IsMonitorPageAvailableSetting),
            nameof(IsMonitorPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Trains page is enabled in the navigation.
    /// </summary>
    public bool IsTrainsPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrainsPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsTrainsPageAvailable,
            updated => _settings.FeatureToggles.IsTrainsPageAvailable = updated,
            value,
            nameof(IsTrainsPageAvailableSetting),
            nameof(IsTrainsPageAvailable));
    }

    /// <summary>
    /// Gets or sets whether the Train Control page is enabled in the navigation.
    /// </summary>
    public bool IsTrainControlPageAvailableSetting
    {
        get => _settings.FeatureToggles.IsTrainControlPageAvailable;
        set => UpdateFeatureToggleSetting(
            () => _settings.FeatureToggles.IsTrainControlPageAvailable,
            updated => _settings.FeatureToggles.IsTrainControlPageAvailable = updated,
            value,
            nameof(IsTrainControlPageAvailableSetting),
            nameof(IsTrainControlPageAvailable));
    }

    #endregion

    #region Signal Box / Viessmann Multiplex-Signale

    private void SetSignalBoxInvert(int offset, bool value)
    {
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
            PersistSettings();
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
    public bool IsSettingsPageAvailable => true;

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
            OnPropertyChanged(nameof(IsJourneysPageAvailable));
            OnPropertyChanged(nameof(IsWorkflowsPageAvailable));
            OnPropertyChanged(nameof(IsTrackPlanEditorPageAvailable));
            OnPropertyChanged(nameof(IsSignalBoxPageAvailable));
            OnPropertyChanged(nameof(IsJourneyMapPageAvailable));
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
            // FIX: Reset error state on UI thread
            _uiDispatcher.InvokeOnUi(() =>
            {
                ShowErrorMessage = false;
                ErrorMessage = string.Empty;
            });

            // Use custom test message from settings (user can modify in UI)
            var testMessage = SpeechTestMessage;

            // Use the announcement service if available
            if (_announcementService != null)
            {
                // FIX: Check if speaker engine is properly configured
                if (!_announcementService.IsSpeakerEngineAvailable)
                {
                    _uiDispatcher.InvokeOnUi(() =>
                    {
                        ErrorMessage = "Speech engine not configured. Please configure Azure Speech Service in Settings or select Windows SAPI engine.";
                        ShowErrorMessage = true;
                    });
                    return;
                }

                var testJourney = new Journey { Text = testMessage };
                var testStation = new Station { Name = "Test", IsExitOnLeft = false };
                await _announcementService.GenerateAndSpeakAnnouncementAsync(testJourney, testStation, 1).ConfigureAwait(false);

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
            _logger.LogWarning(ex, "Speech test failed");
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    /// <summary>
    /// Runs a connectivity + OCR roundtrip against Azure AI Vision using the configured key and endpoint.
    /// If <see cref="VisionTestImagePath"/> points at an existing file, the image is sent to the Read API
    /// and the number of recognized lines and words is reported. Otherwise only the credential shape is validated.
    /// </summary>
    [RelayCommand]
    private async Task TestVisionAsync()
    {
        _logger.LogInformation("Vision test: starting");
        SetVisionResult("Running Azure AI Vision test…", "Informational");

        try
        {
            if (_visionService is null)
            {
                SetVisionResult("Vision service not available (not registered in DI).", "Error");
                return;
            }

            if (!_visionService.IsConfigured)
            {
                SetVisionResult("Azure AI Vision not configured. Please set Key and Endpoint above.", "Warning");
                return;
            }

            var imagePath = VisionTestImagePath;
            if (string.IsNullOrWhiteSpace(imagePath) || !System.IO.File.Exists(imagePath))
            {
                SetVisionResult("Please select an existing PNG/JPEG file in the 'Test image' field.", "Warning");
                return;
            }

            var result = await _visionService.ReadTextAsync(imagePath).ConfigureAwait(false);
            _logger.LogInformation(
                "Azure AI Vision test OK. Image {Width}x{Height}px, lines={Lines}, words={Words}",
                result.ImageWidth, result.ImageHeight, result.Lines.Count, result.WordCount);

            // Full dump of recognized lines (text + bounding-box center) so we can design the
            // PIKO A code extractor against real OCR output without a second round-trip.
            for (var i = 0; i < result.Lines.Count; i++)
            {
                var line = result.Lines[i];
                var cx = line.BoundingPolygon.Count == 0 ? 0d : line.BoundingPolygon.Average(p => (double)p.X);
                var cy = line.BoundingPolygon.Count == 0 ? 0d : line.BoundingPolygon.Average(p => (double)p.Y);
                _logger.LogInformation(
                    "Vision line {Index:D3}: \"{Text}\" center=({Cx:F0},{Cy:F0}) words={Words}",
                    i, line.Text, cx, cy, line.Words.Count);
            }

            // Second pass: strict PIKO A catalog extraction so the user gets an immediate
            // bill of materials + a list of OCR artifacts that need review.
            var extraction = PikoACodeExtractor.Extract(result);
            var sortedCounts = extraction.MatchCountsByCode
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            _logger.LogInformation(
                "PIKO A extraction: {Matches} matches, {Unresolved} unresolved. Counts: {Counts}",
                extraction.Matches.Count,
                extraction.Unresolved.Count,
                string.Join(", ", sortedCounts.Select(kv => $"{kv.Key}×{kv.Value}")));
            foreach (var unresolved in extraction.Unresolved)
            {
                _logger.LogInformation(
                    "PIKO A unresolved: \"{Raw}\" @ ({X:F0},{Y:F0}) (line {Line})",
                    unresolved.RawText, unresolved.CenterX, unresolved.CenterY, unresolved.SourceLineIndex);
            }

            var countsPreview = sortedCounts.Count == 0
                ? "(no PIKO codes)"
                : string.Join(", ", sortedCounts.Take(5).Select(kv => $"{kv.Key}×{kv.Value}"));
            SetVisionResult(
                $"OK — {result.ImageWidth}×{result.ImageHeight}px, {result.Lines.Count} lines. PIKO: {extraction.Matches.Count} matches ({countsPreview}), {extraction.Unresolved.Count} unresolved.",
                "Success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vision test failed");
            SetVisionResult($"Failed: {ex.Message}", "Error");
        }
    }

    private void SetVisionResult(string message, string severity)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            VisionTestResult = message;
            VisionTestSeverity = severity;
            ShowVisionTestResult = true;
        });
    }
    #endregion
}