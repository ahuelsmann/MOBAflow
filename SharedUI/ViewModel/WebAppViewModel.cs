// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;
using Backend.Model;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;

/// <summary>
/// Web-optimized ViewModel for Blazor Server - focused on Z21 monitoring and feedback statistics.
/// Similar to MauiViewModel but optimized for web browser constraints.
/// </summary>
public sealed partial class WebAppViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;

    public WebAppViewModel(IZ21 z21, IUiDispatcher uiDispatcher, AppSettings settings, ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;

        // Subscribe to Z21 events
        _z21.Received += OnFeedbackReceived;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnVersionInfoChanged += OnZ21VersionInfoChanged;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;

        // Load settings into ViewModel
        LoadSettingsIntoViewModel();
        
        // Apply polling interval to Z21 on startup (5 seconds)
        _z21.SetSystemStatePollingInterval(5);
        
        InitializeStatistics();
    }
    
    /// <summary>
    /// Loads settings from AppSettings singleton into ViewModel properties.
    /// </summary>
    private void LoadSettingsIntoViewModel()
    {
        Z21IpAddress = _settings.Z21.CurrentIpAddress;
        
        // Ensure CountOfFeedbackPoints has a sensible default
        CountOfFeedbackPoints = _settings.Counter.CountOfFeedbackPoints > 0 
            ? _settings.Counter.CountOfFeedbackPoints 
            : 3; // Default to 3 feedback points if not configured
            
        GlobalTargetLapCount = _settings.Counter.TargetLapCount;
        UseTimerFilter = _settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;
        
        Debug.WriteLine("═══════════════════════════════════════════════════════");
        Debug.WriteLine("✅ WebApp Settings loaded:");
        Debug.WriteLine($"   IP Address: {Z21IpAddress}");
        Debug.WriteLine($"   Feedback Points: {CountOfFeedbackPoints}");
        Debug.WriteLine($"   Target Laps: {GlobalTargetLapCount}");
        Debug.WriteLine("═══════════════════════════════════════════════════════");
    }

    #region Z21 Connection

    [ObservableProperty]
    private string _z21IpAddress = "192.168.0.111";

    /// <summary>
    /// Available IP addresses for Z21 connection (from recent connections).
    /// </summary>
    public ObservableCollection<string> AvailableIpAddresses => new(_settings.Z21.RecentIpAddresses);

    /// <summary>
    /// Wrapper property for UI binding (matches WinUI property name).
    /// </summary>
    public string IpAddress
    {
        get => Z21IpAddress;
        set => Z21IpAddress = value;
    }

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isTrackPowerOn;

    [ObservableProperty]
    private int _mainCurrent;

    [ObservableProperty]
    private int _temperature;

    [ObservableProperty]
    private int _supplyVoltage;

    [ObservableProperty]
    private int _vccVoltage;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private string _serialNumber = "-";

    [ObservableProperty]
    private string _firmwareVersion = "-";

    [ObservableProperty]
    private string _hardwareType = "-";

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
            StatusText = "Connecting...";
            var address = IPAddress.Parse(Z21IpAddress);
            int port = int.Parse(_settings.Z21.DefaultPort);
            await _z21.ConnectAsync(address, port).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            Debug.WriteLine($"❌ Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            StatusText = "Disconnecting...";
            await _z21.DisconnectAsync().ConfigureAwait(false);
            StatusText = "Disconnected";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        try
        {
            if (turnOn)
                await _z21.SetTrackPowerOnAsync().ConfigureAwait(false);
            else
                await _z21.SetTrackPowerOffAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusText = $"Track power error: {ex.Message}";
        }
    }

    #endregion

    #region Feedback Statistics

    [ObservableProperty]
    private ObservableCollection<InPortStatistic> _statistics = [];

    [ObservableProperty]
    private int _countOfFeedbackPoints = 3;

    [ObservableProperty]
    private int _globalTargetLapCount = 10;

    [ObservableProperty]
    private bool _useTimerFilter;

    [ObservableProperty]
    private double _timerIntervalSeconds = 2.0;

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = [];

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        _settings.Counter.TargetLapCount = value;
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        _settings.Counter.UseTimerFilter = value;
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        _settings.Counter.TimerIntervalSeconds = value;
    }

    private void InitializeStatistics()
    {
        Statistics.Clear();

        int count = CountOfFeedbackPoints;
        if (count > 0)
        {
            for (int i = 1; i <= count; i++)
            {
                Statistics.Add(new InPortStatistic
                {
                    InPort = i,
                    Name = $"Feedback Point {i}",
                    Count = 0,
                    TargetLapCount = GlobalTargetLapCount
                });
            }
            Debug.WriteLine($"✅ Initialized {Statistics.Count} track statistics (InPorts 1-{count})");
        }
        else
        {
            Debug.WriteLine("⚠️ CountOfFeedbackPoints is 0 - no track statistics initialized");
        }
        
        // Notify UI that Statistics collection has changed
        OnPropertyChanged(nameof(Statistics));
    }

    [RelayCommand]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastLapTime = null;
            stat.LastFeedbackTime = null;
            stat.HasReceivedFirstLap = false;
        }
        _lastFeedbackTime.Clear();
        Debug.WriteLine("🔄 All counters reset");
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateTrackStatistics((uint)feedback.InPort));
    }

    private void UpdateTrackStatistics(uint inPort)
    {
        var stat = Statistics.FirstOrDefault(s => s.InPort == inPort);
        if (stat == null) return;

        // Timer filter: Ignore if feedback was received recently
        if (UseTimerFilter)
        {
            if (_lastFeedbackTime.TryGetValue((int)inPort, out var lastTime))
            {
                var elapsed = DateTime.Now - lastTime;
                if (elapsed.TotalSeconds < TimerIntervalSeconds)
                {
                    Debug.WriteLine($"⏱️ InPort {inPort}: Ignored (timer filter: {elapsed.TotalSeconds:F1}s < {TimerIntervalSeconds}s)");
                    return;
                }
            }
        }

        // Update last feedback time
        _lastFeedbackTime[(int)inPort] = DateTime.Now;

        // Calculate lap time
        if (stat.LastFeedbackTime.HasValue)
        {
            stat.LastLapTime = DateTime.Now - stat.LastFeedbackTime.Value;
        }

        // Update statistics
        stat.Count++;
        stat.LastFeedbackTime = DateTime.Now;

        Debug.WriteLine($"📊 InPort {inPort}: Lap {stat.Count}/{stat.TargetLapCount} | Lap time: {stat.LastLapTimeFormatted}");
    }

    #endregion

    #region Z21 Event Handlers

    private void OnZ21SystemStateChanged(SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsTrackPowerOn = systemState.IsTrackPowerOn;
            MainCurrent = systemState.MainCurrent;
            Temperature = systemState.Temperature;
            SupplyVoltage = systemState.SupplyVoltage;
            VccVoltage = systemState.VccVoltage;

            // Update status text
            var warnings = new List<string>();
            if (systemState.IsEmergencyStop) warnings.Add("EMERGENCY STOP");
            if (systemState.IsShortCircuit) warnings.Add("SHORT CIRCUIT");
            if (systemState.IsProgrammingMode) warnings.Add("Programming");

            StatusText = warnings.Count > 0 
                ? $"Connected | {string.Join(" | ", warnings)}" 
                : "Connected";
        });
    }

    private void OnZ21ConnectedChanged(bool connected)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = connected;
            StatusText = connected ? "Connected" : "Disconnected";
            
            Debug.WriteLine(connected 
                ? "✅ Z21 connection confirmed" 
                : "❌ Z21 disconnected");
        });
    }

    private void OnZ21VersionInfoChanged(Z21VersionInfo versionInfo)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            SerialNumber = versionInfo.SerialNumber.ToString();
            FirmwareVersion = versionInfo.FirmwareVersion;
            HardwareType = versionInfo.HardwareType;

            Debug.WriteLine($"✅ Z21 Version: S/N={SerialNumber}, HW={HardwareType}, FW={FirmwareVersion}");
        });
    }

    #endregion

    #region Settings Commands

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
            Debug.WriteLine("✅ Settings saved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
        }
    }

    #endregion
}
