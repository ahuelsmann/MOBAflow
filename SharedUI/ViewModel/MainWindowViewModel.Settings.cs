// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

/// <summary>
/// MainWindowViewModel - Settings Management
/// Handles application settings properties and persistence (Z21, CityLibrary, Speech, Application, HealthCheck).
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
                // TODO: Restart auto-connect timer with new interval
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
                // Timer update happens on next connect or can be applied live via Z21.SetSystemStatePollingInterval()
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
        get => _settings.Speech.SpeakerEngineName ?? AvailableSpeechEngines[0];
        set
        {
            if (_settings.Speech.SpeakerEngineName != value)
            {
                _settings.Speech.SpeakerEngineName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAzureSpeechEngineSelected));
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
    /// Gets whether the Feedback Points page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsFeedbackPointsPageAvailable => _settings.FeatureToggles.IsFeedbackPointsPageAvailable;

    /// <summary>
    /// Gets whether the Track Plan Editor page is available.
    /// Bound to NavigationView item visibility.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailable => _settings.FeatureToggles.IsTrackPlanEditorPageAvailable;

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

    // Feature Toggle Labels (optional)
    
    public string? OverviewPageLabel => _settings.FeatureToggles.OverviewPageLabel;
    public string? SolutionPageLabel => _settings.FeatureToggles.SolutionPageLabel;
    public string? JourneysPageLabel => _settings.FeatureToggles.JourneysPageLabel;
    public string? WorkflowsPageLabel => _settings.FeatureToggles.WorkflowsPageLabel;
    public string? FeedbackPointsPageLabel => _settings.FeatureToggles.FeedbackPointsPageLabel;
    public string? TrackPlanEditorPageLabel => _settings.FeatureToggles.TrackPlanEditorPageLabel;
    public string? JourneyMapPageLabel => _settings.FeatureToggles.JourneyMapPageLabel;
    public string? SettingsPageLabel => _settings.FeatureToggles.SettingsPageLabel;
    public string? MonitorPageLabel => _settings.FeatureToggles.MonitorPageLabel;
    
    // Settings Page CheckBox Content (with labels)
    
    public string FeedbackPointsCheckBoxContent => FormatPageContent("Feedback Points Page", FeedbackPointsPageLabel);
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
    private async Task SaveSettingsAsync()
    {
        if (_settingsService == null) return;

        try
        {
            ShowErrorMessage = false;
            await _settingsService.SaveSettingsAsync(_settings);

            ShowSuccessMessage = true;

            // Auto-hide success message after 3 seconds
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    [RelayCommand]
    private async Task BrowseCityLibraryAsync()
    {
        try
        {
            var path = await _ioService.BrowseForJsonFileAsync();
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
            await _settingsService.ResetToDefaultsAsync();

            // Notify all settings properties changed
            OnPropertyChanged(nameof(IpAddress));
            OnPropertyChanged(nameof(Port));
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
            OnPropertyChanged(nameof(HealthCheckEnabled));
            OnPropertyChanged(nameof(HealthCheckIntervalSeconds));

            ShowSuccessMessage = true;
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    private IRelayCommand<string?>? _selectSpeechEngineCommand;

    /// <summary>
    /// Command to select a speech engine from the UI.
    /// </summary>
    public IRelayCommand<string?> SelectSpeechEngineCommand =>
        _selectSpeechEngineCommand ??= new RelayCommand<string?>(engine =>
        {
            SelectedSpeechEngine = engine;
        });

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
                var testJourney = new Domain.Journey { Text = testMessage };
                var testStation = new Domain.Station { Name = "Test", IsExitOnLeft = false };
                await _announcementService.GenerateAndSpeakAnnouncementAsync(testJourney, testStation, 1);
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

