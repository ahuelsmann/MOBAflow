// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public partial class MauiViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IRestDiscoveryService _restDiscoveryService;
    private readonly IPhotoUploadService _photoUploadService;
    private readonly IPhotoCaptureService _photoCaptureService;

    public MauiViewModel(
        IZ21 z21,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        ISettingsService settingsService,
        IRestDiscoveryService restDiscoveryService,
        IPhotoUploadService photoUploadService,
        IPhotoCaptureService photoCaptureService)
    {
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;
        _restDiscoveryService = restDiscoveryService;
        _photoUploadService = photoUploadService;
        _photoCaptureService = photoCaptureService;

        // Subscribe to Z21 events
        _z21.Received += OnFeedbackReceived;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;

        // ✅ Initialize with loaded settings (settings were loaded in SettingsService constructor)
        LoadSettingsIntoViewModel();
        
        // Apply polling interval to Z21 on startup (5 seconds - not configurable)
        _z21.SetSystemStatePollingInterval(5);
        
        InitializeStatistics();
    }
    
    /// <summary>
    /// Loads settings from AppSettings singleton into ViewModel properties.
    /// Called during constructor after SettingsService has loaded the file.
    /// </summary>
    private void LoadSettingsIntoViewModel()
    {
        Debug.WriteLine("═══════════════════════════════════════════════════════");
        Debug.WriteLine("🔄 MauiViewModel.LoadSettingsIntoViewModel START");
        Debug.WriteLine($"   AppSettings.Counter.CountOfFeedbackPoints: {_settings.Counter.CountOfFeedbackPoints}");
        Debug.WriteLine($"   AppSettings.Counter.TargetLapCount: {_settings.Counter.TargetLapCount}");
        Debug.WriteLine($"   AppSettings.Counter.UseTimerFilter: {_settings.Counter.UseTimerFilter}");
        Debug.WriteLine($"   AppSettings.Counter.TimerIntervalSeconds: {_settings.Counter.TimerIntervalSeconds}");
        Debug.WriteLine($"   AppSettings.Z21.CurrentIpAddress: {_settings.Z21.CurrentIpAddress}");
        Debug.WriteLine($"   AppSettings.RestApi.CurrentIpAddress: {_settings.RestApi.CurrentIpAddress}");
        
        Z21IpAddress = _settings.Z21.CurrentIpAddress;
        RestApiIpAddress = _settings.RestApi.CurrentIpAddress;
        RestApiPort = _settings.RestApi.Port;
        CountOfFeedbackPoints = _settings.Counter.CountOfFeedbackPoints;
        GlobalTargetLapCount = _settings.Counter.TargetLapCount;
        UseTimerFilter = _settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;
        
        Debug.WriteLine("───────────────────────────────────────────────────────");
        Debug.WriteLine("✅ Values loaded into ViewModel:");
        Debug.WriteLine($"   Z21IpAddress: {Z21IpAddress}");
        Debug.WriteLine($"   RestApiIpAddress: {RestApiIpAddress}");
        Debug.WriteLine($"   RestApiPort: {RestApiPort}");
        Debug.WriteLine($"   CountOfFeedbackPoints: {CountOfFeedbackPoints}");
        Debug.WriteLine($"   GlobalTargetLapCount: {GlobalTargetLapCount}");
        Debug.WriteLine($"   UseTimerFilter: {UseTimerFilter}");
        Debug.WriteLine($"   TimerIntervalSeconds: {TimerIntervalSeconds}s");
        Debug.WriteLine("═══════════════════════════════════════════════════════");
    }

    #region REST-API Connection

    [ObservableProperty]
    private string restApiIpAddress = string.Empty;

    [ObservableProperty]
    private int restApiPort = 5001;

    partial void OnRestApiIpAddressChanged(string value)
    {
        _settings.RestApi.CurrentIpAddress = value;
        _settingsService.SaveSettingsAsync(_settings); // Auto-save when IP changes
    }

    #endregion

    #region Z21 Connection

    [ObservableProperty]
    private string z21IpAddress = "192.168.0.111";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isTrackPowerOn;

    [ObservableProperty]
    private int mainCurrent;

    [ObservableProperty]
    private int temperature;

    [ObservableProperty]
    private int supplyVoltage;

    [ObservableProperty]
    private int vccVoltage;

    partial void OnZ21IpAddressChanged(string value)
    {
        _settings.Z21.CurrentIpAddress = value;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(Z21IpAddress)) return;

        try
        {
            var address = IPAddress.Parse(Z21IpAddress);
            int port = int.Parse(_settings.Z21.DefaultPort);
            await _z21.ConnectAsync(address, port).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _z21.DisconnectAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        if (turnOn)
            await _z21.SetTrackPowerOnAsync().ConfigureAwait(false);
        else
            await _z21.SetTrackPowerOffAsync().ConfigureAwait(false);
    }

    #endregion

    #region Feedback Statistics

    [ObservableProperty]
    private ObservableCollection<InPortStatistic> statistics = [];

    [ObservableProperty]
    private int countOfFeedbackPoints = 3;

    [ObservableProperty]
    private int globalTargetLapCount = 10;

    [ObservableProperty]
    private bool useTimerFilter;

    [ObservableProperty]
    private double timerIntervalSeconds = 2.0;

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = new();

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        Debug.WriteLine($"🔔 OnCountOfFeedbackPointsChanged: {value}");
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        Debug.WriteLine($"🔔 OnGlobalTargetLapCountChanged: {value}");
        _settings.Counter.TargetLapCount = value;
        
        // Update all existing statistics
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
        
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        Debug.WriteLine($"🔔 OnUseTimerFilterChanged: {value}");
        _settings.Counter.UseTimerFilter = value;
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        Debug.WriteLine($"🔔 OnTimerIntervalSecondsChanged: {value}");
        _settings.Counter.TimerIntervalSeconds = value;
        _ = SaveSettingsAsync(); // Auto-save
    }

    private void InitializeStatistics()
    {
        Statistics.Clear();
        for (int i = 1; i <= CountOfFeedbackPoints; i++)
        {
            Statistics.Add(new InPortStatistic
            {
                InPort = i,
                Name = $"Track {i}",
                Count = 0,
                TargetLapCount = GlobalTargetLapCount
            });
        }
    }

    [RelayCommand]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastFeedbackTime = null;
            stat.LastLapTime = TimeSpan.Zero;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDecrementFeedbackPoints))]
    private void DecrementFeedbackPoints()
    {
        if (CountOfFeedbackPoints > 1)
        {
            CountOfFeedbackPoints--;
        }
    }

    private bool CanDecrementFeedbackPoints() => CountOfFeedbackPoints > 1;

    [RelayCommand]
    private void IncrementFeedbackPoints()
    {
        CountOfFeedbackPoints++;
    }

    [RelayCommand(CanExecute = nameof(CanDecrementTargetLapCount))]
    private void DecrementTargetLapCount()
    {
        if (GlobalTargetLapCount > 1)
        {
            GlobalTargetLapCount--;
        }
    }

    private bool CanDecrementTargetLapCount() => GlobalTargetLapCount > 1;

    [RelayCommand]
    private void IncrementTargetLapCount()
    {
        GlobalTargetLapCount++;
    }

    [RelayCommand(CanExecute = nameof(CanDecrementTimerInterval))]
    private void DecrementTimerInterval()
    {
        if (TimerIntervalSeconds > 1.0)
        {
            TimerIntervalSeconds = Math.Round(TimerIntervalSeconds - 1.0, 1);
        }
    }

    private bool CanDecrementTimerInterval() => TimerIntervalSeconds > 1.0;

    [RelayCommand]
    private void IncrementTimerInterval()
    {
        TimerIntervalSeconds = Math.Round(TimerIntervalSeconds + 1.0, 1);
    }

    /// <summary>
    /// Saves all settings to persistent storage.
    /// Called automatically when any counter setting changes.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
            Debug.WriteLine("✅ Counter settings saved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
        }
    }

    #endregion

    #region Z21 Event Handlers

    private void OnZ21ConnectedChanged(bool connected)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = connected;
        });
    }

    private void OnZ21SystemStateChanged(SystemState state)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsTrackPowerOn = state.IsTrackPowerOn;
            MainCurrent = state.MainCurrent;
            Temperature = state.Temperature;
            SupplyVoltage = state.SupplyVoltage;
            VccVoltage = state.VccVoltage;
        });
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            var stat = Statistics.FirstOrDefault(s => s.InPort == feedback.InPort);
            if (stat != null)
            {
                // Timer filter: Prevent duplicate counts from long trains
                if (UseTimerFilter)
                {
                    if (_lastFeedbackTime.TryGetValue(feedback.InPort, out DateTime lastTime))
                    {
                        var elapsed = (DateTime.Now - lastTime).TotalSeconds;
                        if (elapsed < TimerIntervalSeconds)
                        {
                            // Skip: Too soon after last feedback (same train still passing)
                            return;
                        }
                    }
                    _lastFeedbackTime[feedback.InPort] = DateTime.Now;
                }

                // Calculate lap time (time between two consecutive feedbacks)
                DateTime now = DateTime.Now;
                if (stat.LastFeedbackTime.HasValue)
                {
                    stat.LastLapTime = now - stat.LastFeedbackTime.Value;
                }

                // Update count and timestamp
                stat.Count++;
                stat.LastFeedbackTime = now;
            }
        });
    }


    #endregion

    #region Photo Upload

    [ObservableProperty]
    private bool isPhotoUploading;

    [ObservableProperty]
    private string? photoUploadStatus;

    [ObservableProperty]
    private bool photoUploadSuccess;

    [RelayCommand]
    private async Task CaptureAndUploadPhotoAsync()
    {
        try
        {
            IsPhotoUploading = true;
            PhotoUploadSuccess = false;
            PhotoUploadStatus = null;

            var localPath = await _photoCaptureService.CapturePhotoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(localPath))
            {
                PhotoUploadStatus = "Capture cancelled or not available.";
                return;
            }

            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(ip) || port == null)
            {
                PhotoUploadStatus = "⚠️ REST Server not configured\n\n" +
                                    "Enter server IP in settings above:\n" +
                                    "• Use your PC's local IP address\n" +
                                    "  (e.g., 192.168.0.79)\n\n" +
                                    "Server must be running on port 5001.";
                return;
            }

            // ✅ Upload photo WITHOUT entityId - WinUI will assign it to the currently selected item
            var tempId = Guid.NewGuid(); // Temporary ID for filename only
            var (success, serverPhotoPath, error) = await _photoUploadService.UploadPhotoAsync(ip, port.Value, localPath, "latest", tempId).ConfigureAwait(false);
            if (success)
            {
                PhotoUploadSuccess = true;
                PhotoUploadStatus = serverPhotoPath ?? "Uploaded successfully.";
            }
            else
            {
                PhotoUploadStatus = error ?? "Upload failed.";
            }
        }
        catch (Exception ex)
        {
            PhotoUploadStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsPhotoUploading = false;
        }
    }

    #endregion
}
