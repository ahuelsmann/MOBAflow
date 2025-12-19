// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    // Wrapper properties for Settings page bindings
    public string Z21IpAddress
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

    public string Z21Port
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

    [ObservableProperty]
    private bool _showSuccessMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _selectedThemeIndex = 2; // Default: Use system setting
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
            OnPropertyChanged(nameof(Z21IpAddress));
            OnPropertyChanged(nameof(Z21Port));
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
