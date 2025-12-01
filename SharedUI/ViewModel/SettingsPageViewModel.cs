// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.SharedUI.Service;
using Moba.Common.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// ViewModel for the Settings Page.
/// Binds to appsettings.json via ISettingsService.
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFilePickerService? _filePickerService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private bool _showSuccessMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public SettingsPageViewModel(ISettingsService settingsService, IFilePickerService? filePickerService = null)
    {
        _settingsService = settingsService;
        _filePickerService = filePickerService;
        _settings = settingsService.GetSettings();
    }

    #region Z21 Settings

    public string Z21IpAddress
    {
        get => _settings.Z21.CurrentIpAddress;
        set => SetProperty(_settings.Z21.CurrentIpAddress, value, _settings.Z21, (s, v) => s.CurrentIpAddress = v);
    }

    public string Z21Port
    {
        get => _settings.Z21.DefaultPort;
        set => SetProperty(_settings.Z21.DefaultPort, value, _settings.Z21, (s, v) => s.DefaultPort = v);
    }

    #endregion

    #region City Library Settings

    public string CityLibraryPath
    {
        get => _settings.CityLibrary.FilePath;
        set => SetProperty(_settings.CityLibrary.FilePath, value, _settings.CityLibrary, (s, v) => s.FilePath = v);
    }

    public bool CityLibraryAutoReload
    {
        get => _settings.CityLibrary.AutoReload;
        set => SetProperty(_settings.CityLibrary.AutoReload, value, _settings.CityLibrary, (s, v) => s.AutoReload = v);
    }

    [RelayCommand]
    private async Task BrowseCityLibraryAsync()
    {
        if (_filePickerService == null) return;

        var path = await _filePickerService.PickJsonFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            CityLibraryPath = path;
        }
    }

    #endregion

    #region Speech Settings

    public string? SpeechKey
    {
        get => _settings.Speech.Key;
        set => SetProperty(_settings.Speech.Key, value, _settings.Speech, (s, v) => s.Key = v ?? string.Empty);
    }

    public string SpeechRegion
    {
        get => _settings.Speech.Region;
        set => SetProperty(_settings.Speech.Region, value, _settings.Speech, (s, v) => s.Region = v);
    }

    public int SpeechRate
    {
        get => _settings.Speech.Rate;
        set => SetProperty(_settings.Speech.Rate, value, _settings.Speech, (s, v) => s.Rate = v);
    }

    public double SpeechVolume
    {
        get => _settings.Speech.Volume;
        set => SetProperty(_settings.Speech.Volume, (uint)value, _settings.Speech, (s, v) => s.Volume = v);
    }

    #endregion

    #region Application Settings

    public bool ResetWindowLayoutOnStart
    {
        get => _settings.Application.ResetWindowLayoutOnStart;
        set => SetProperty(_settings.Application.ResetWindowLayoutOnStart, value, _settings.Application, (s, v) => s.ResetWindowLayoutOnStart = v);
    }

    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set => SetProperty(_settings.Application.AutoLoadLastSolution, value, _settings.Application, (s, v) => s.AutoLoadLastSolution = v);
    }

    #endregion

    #region Health Check Settings

    public bool HealthCheckEnabled
    {
        get => _settings.HealthCheck.Enabled;
        set => SetProperty(_settings.HealthCheck.Enabled, value, _settings.HealthCheck, (s, v) => s.Enabled = v);
    }

    public double HealthCheckIntervalSeconds
    {
        get => _settings.HealthCheck.IntervalSeconds;
        set => SetProperty(_settings.HealthCheck.IntervalSeconds, (int)value, _settings.HealthCheck, (s, v) => s.IntervalSeconds = v);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
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
    private async Task ResetToDefaultsAsync()
    {
        try
        {
            ShowErrorMessage = false;
            await _settingsService.ResetToDefaultsAsync();
            
            // Reload settings from file
            var newSettings = _settingsService.GetSettings();
            
            // Update all properties
            OnPropertyChanged(nameof(Z21IpAddress));
            OnPropertyChanged(nameof(Z21Port));
            OnPropertyChanged(nameof(CityLibraryPath));
            OnPropertyChanged(nameof(CityLibraryAutoReload));
            OnPropertyChanged(nameof(SpeechKey));
            OnPropertyChanged(nameof(SpeechRegion));
            OnPropertyChanged(nameof(SpeechRate));
            OnPropertyChanged(nameof(SpeechVolume));
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

    #endregion
}
