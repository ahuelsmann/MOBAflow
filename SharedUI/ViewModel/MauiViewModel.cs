// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using Backend.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Configuration;
using SharedUI.Interface;
using SharedUI.ViewModel;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public partial class MauiViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;

    public MauiViewModel(IZ21 z21, IUiDispatcher uiDispatcher, AppSettings settings, ISettingsService settingsService)
    {
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;

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
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine("🔄 MauiViewModel.LoadSettingsIntoViewModel START");
        System.Diagnostics.Debug.WriteLine($"   AppSettings.Counter.CountOfFeedbackPoints: {_settings.Counter.CountOfFeedbackPoints}");
        System.Diagnostics.Debug.WriteLine($"   AppSettings.Counter.TargetLapCount: {_settings.Counter.TargetLapCount}");
        System.Diagnostics.Debug.WriteLine($"   AppSettings.Counter.UseTimerFilter: {_settings.Counter.UseTimerFilter}");
        System.Diagnostics.Debug.WriteLine($"   AppSettings.Counter.TimerIntervalSeconds: {_settings.Counter.TimerIntervalSeconds}");
        System.Diagnostics.Debug.WriteLine($"   AppSettings.Z21.CurrentIpAddress: {_settings.Z21.CurrentIpAddress}");
        
        Z21IpAddress = _settings.Z21.CurrentIpAddress;
        CountOfFeedbackPoints = _settings.Counter.CountOfFeedbackPoints;
        GlobalTargetLapCount = _settings.Counter.TargetLapCount;
        UseTimerFilter = _settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;
        
        System.Diagnostics.Debug.WriteLine("───────────────────────────────────────────────────────");
        System.Diagnostics.Debug.WriteLine("✅ Values loaded into ViewModel:");
        System.Diagnostics.Debug.WriteLine($"   Z21IpAddress: {Z21IpAddress}");
        System.Diagnostics.Debug.WriteLine($"   CountOfFeedbackPoints: {CountOfFeedbackPoints}");
        System.Diagnostics.Debug.WriteLine($"   GlobalTargetLapCount: {GlobalTargetLapCount}");
        System.Diagnostics.Debug.WriteLine($"   UseTimerFilter: {UseTimerFilter}");
        System.Diagnostics.Debug.WriteLine($"   TimerIntervalSeconds: {TimerIntervalSeconds}s");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════");
    }

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
            var address = System.Net.IPAddress.Parse(Z21IpAddress);
            int port = int.Parse(_settings.Z21.DefaultPort);
            await _z21.ConnectAsync(address, port);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _z21.DisconnectAsync();
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        if (turnOn)
            await _z21.SetTrackPowerOnAsync();
        else
            await _z21.SetTrackPowerOffAsync();
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
        System.Diagnostics.Debug.WriteLine($"🔔 OnCountOfFeedbackPointsChanged: {value}");
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        System.Diagnostics.Debug.WriteLine($"🔔 OnGlobalTargetLapCountChanged: {value}");
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
        System.Diagnostics.Debug.WriteLine($"🔔 OnUseTimerFilterChanged: {value}");
        _settings.Counter.UseTimerFilter = value;
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        System.Diagnostics.Debug.WriteLine($"🔔 OnTimerIntervalSecondsChanged: {value}");
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
            System.Diagnostics.Debug.WriteLine("✅ Counter settings saved");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
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

    private void OnZ21SystemStateChanged(Backend.SystemState state)
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

    private void OnFeedbackReceived(Backend.FeedbackResult feedback)
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
}
