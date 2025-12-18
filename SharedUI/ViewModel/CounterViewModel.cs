// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using Common.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Moba.Backend.Interface;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

/// <summary>
/// ViewModel that connects to a Z21 and counts laps per InPort (simple demo feature).
/// - Platform usage: used by MAUI and Web to demonstrate UDP-driven workflows
/// - Threading: Z21 events arrive on background threads; UI updates are dispatched to main thread via IUiDispatcher
/// - Filtering: optional timer-based filter to ignore multiple axle detections
/// </summary>
public partial class CounterViewModel : ObservableObject, IDisposable
{
    #region Fields
    // Core Services (required)
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _dispatcher;
    private readonly ISettingsService _settingsService;

    // Configuration (derived from ISettingsService)
    private readonly AppSettings _settings;

    // Runtime State
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = new();
    private bool _disposed;
    #endregion

    public CounterViewModel(
        IZ21 z21,
        IUiDispatcher dispatcher,
        ISettingsService settingsService)
    {
        _z21 = z21;
        _dispatcher = dispatcher;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();

        // ✅ Initialize available IP addresses from AppSettings
        AvailableIpAddresses = new ObservableCollection<string>(_settings.Z21.RecentIpAddresses);

        // ✅ Load Z21 IP from AppSettings
        if (!string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            Z21IpAddress = _settings.Z21.CurrentIpAddress;
            this.Log($"✅ Loaded Z21 IP from AppSettings: {Z21IpAddress}");
        }
        else if (_settings.Z21.RecentIpAddresses.Count > 0)
        {
            Z21IpAddress = _settings.Z21.RecentIpAddresses[0];
            _settings.Z21.CurrentIpAddress = Z21IpAddress; // Set as current
            this.Log($"✅ Loaded first Z21 IP from RecentIpAddresses: {Z21IpAddress}");
        }
        else
        {
            // ✅ Fallback for new/empty solutions - use hardcoded default
            Z21IpAddress = "192.168.0.111"; // Default Z21 IP address
            _settings.Z21.CurrentIpAddress = Z21IpAddress;
            _settings.Z21.RecentIpAddresses.Add(Z21IpAddress);
            AvailableIpAddresses.Add(Z21IpAddress);
            this.Log("⚠️ No IP in AppSettings - using default: 192.168.0.111");
        }

        Statistics.Add(new InPortStatistic { InPort = 1, Count = 0, TargetLapCount = GlobalTargetLapCount });
        Statistics.Add(new InPortStatistic { InPort = 2, Count = 0, TargetLapCount = GlobalTargetLapCount });
        Statistics.Add(new InPortStatistic { InPort = 3, Count = 0, TargetLapCount = GlobalTargetLapCount });

        // ✅ Subscribe to Z21 events immediately (for WinUI simulate testing)
        // This works because Z21 is a singleton and events persist across Connect/Disconnect
        _z21.Received += OnFeedbackReceived;
        _z21.OnSystemStateChanged += OnSystemStateChanged;
        _z21.OnXBusStatusChanged += OnXBusStatusChanged;
        _z21.OnVersionInfoChanged += OnVersionInfoChanged;

        this.Log("✅ CounterViewModel: Subscribed to Z21 events (ready for simulation)");
    }

    /// <summary>
    /// Current Z21 IP address for connection. Synced with Solution.Settings.CurrentIpAddress.
    /// </summary>
    [ObservableProperty]
    private string z21IpAddress = "192.168.0.111"; // Default Z21 IP address

    /// <summary>
    /// Available Z21 IP addresses from Solution.Settings for ComboBox/Picker binding.
    /// </summary>
    public ObservableCollection<string> AvailableIpAddresses { get; }

    /// <summary>
    /// Syncs IP address changes back to AppSettings and adds new IPs to history.
    /// Persists changes if ISettingsService is available.
    /// </summary>
    partial void OnZ21IpAddressChanged(string value)
    {
        // Sync selected IP back to AppSettings.CurrentIpAddress
        if (!string.IsNullOrEmpty(value) && _settings.Z21.CurrentIpAddress != value)
        {
            _settings.Z21.CurrentIpAddress = value;
            this.Log($"✅ CurrentIpAddress updated in AppSettings: {value}");

            // Add to history if not already present
            if (!_settings.Z21.RecentIpAddresses.Contains(value))
            {
                _settings.Z21.RecentIpAddresses.Add(value);
                AvailableIpAddresses.Add(value);
                this.Log($"✅ Added new IP to history: {value}");
            }

            // Persist settings if service is available
            _ = SaveSettingsAsync();
        }
    }

    /// <summary>
    /// Persists current settings to disk (fire-and-forget, logs errors).
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(_settings);
            this.Log("✅ Settings persisted to disk");
        }
        catch (Exception ex)
        {
            this.Log($"⚠️ Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Indicates whether the Z21 is currently connected.
    /// </summary>
    [ObservableProperty]
    private bool isConnected;

    /// <summary>
    /// Indicates whether the track power is currently ON.
    /// </summary>
    [ObservableProperty]
    private bool isTrackPowerOn;

    /// <summary>
    /// Helper property for UI binding (inverse of IsConnected).
    /// </summary>
    public bool IsNotConnected => !IsConnected;

    partial void OnIsConnectedChanged(bool _)
    {
        // Notify UI about inverse property change
        OnPropertyChanged(nameof(IsNotConnected));

        // Update command states
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        ResetCountersCommand.NotifyCanExecuteChanged();
        SetTrackPowerCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Current status message for display (e.g., "Connected", "Connecting...", "Error: ...").
    /// </summary>
    [ObservableProperty]
    private string statusText = string.Empty;

    /// <summary>
    /// Collection of lap statistics for all configured tracks (InPorts).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<InPortStatistic> statistics = [];

    /// <summary>
    /// Enables/disables timer-based filtering to ignore multiple axle detections from the same train.
    /// </summary>
    [ObservableProperty]
    private bool useTimerFilter = true;

    /// <summary>
    /// Timer interval in seconds for filtering multiple feedback events (default: 5.0 seconds).
    /// </summary>
    [ObservableProperty]
    private double timerIntervalSeconds = 5.0;

    /// <summary>
    /// Global target lap count for all tracks. Changing this updates all track targets.
    /// </summary>
    [ObservableProperty]
    private int globalTargetLapCount = 10;

    // Z21 System State Properties

    /// <summary>
    /// Main track current in milliamperes (mA).
    /// </summary>
    [ObservableProperty]
    private int mainCurrent;

    /// <summary>
    /// Z21 internal temperature in degrees Celsius.
    /// </summary>
    [ObservableProperty]
    private int temperature;

    /// <summary>
    /// Z21 supply voltage in millivolts (mV).
    /// </summary>
    [ObservableProperty]
    private int supplyVoltage;

    /// <summary>
    /// Z21 VCC voltage in millivolts (mV).
    /// </summary>
    [ObservableProperty]
    private int vccVoltage;

    /// <summary>
    /// Z21 serial number (e.g., "101953").
    /// </summary>
    [ObservableProperty]
    private string serialNumber = "-";

    /// <summary>
    /// Z21 firmware version (e.g., "V1.43").
    /// </summary>
    [ObservableProperty]
    private string firmwareVersion = "-";

    /// <summary>
    /// Z21 hardware type (e.g., "Z21a", "z21start").
    /// </summary>
    [ObservableProperty]
    private string hardwareType = "-";

    /// <summary>
    /// Z21 hardware version/revision (typically "0").
    /// </summary>
    [ObservableProperty]
    private string hardwareVersion = "-";

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        // Update all tracks when global target changes
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
    }

    /// <summary>
    /// Connects to Z21 digital command station.
    /// Validates IP address format before attempting connection.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    public async Task ConnectAsync()
    {
        try
        {
            // Parse IP address
            if (!IPAddress.TryParse(Z21IpAddress, out var ipAddress))
            {
                StatusText = "❌ Invalid IP format";
                return;
            }

            // Check if IP is in local network range
            if (!IsLocalNetworkAddress(ipAddress))
            {
                StatusText = "❌ IP must be in local network";
                return;
            }

            // Determine port from settings (fallback 21105)
            var port = 21105;
            if (!string.IsNullOrEmpty(_settings.Z21.DefaultPort) && int.TryParse(_settings.Z21.DefaultPort, out var configuredPort))
            {
                port = configuredPort;
            }

            // Connect to Z21
            await _z21.ConnectAsync(ipAddress, port);

            IsConnected = true;
            StatusText = string.Empty; // Clear status after successful connection

            // Note: GetStatusAsync is now called inside Z21.ConnectAsync() with proper delays
            // to prevent overwhelming the Z21 with rapid requests

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ResetCountersCommand.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            StatusText = $"❌ Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Disconnects from Z21.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public async Task DisconnectAsync()
    {
        try
        {
            // Note: Do NOT unsubscribe events here - they remain active for WinUI simulation
            // Events are only unsubscribed when CounterViewModel is disposed
            await _z21.DisconnectAsync();
            // Note: Don't dispose Z21 here - it's a singleton managed by DI

            IsConnected = false;
            IsTrackPowerOn = false;
            StatusText = string.Empty; // Clear status after disconnect

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ResetCountersCommand.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Disconnect error: {ex.Message}";
            this.Log($"⚠️ Z21 Disconnect Error: {ex}");
        }
    }

    /// <summary>
    /// Resets all lap counters to zero and clears feedback timer history.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetCounters))]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastLapTime = null;
            stat.LastFeedbackTime = null;
            stat.HasReceivedFirstLap = false; // ← Reset to red background
        }

        _lastFeedbackTime.Clear();
        StatusText = "Counters reset";
        this.Log("🔄 All lap counters reset to 0");
    }

    private bool CanConnect() => !IsConnected;
    private bool CanDisconnect() => IsConnected;
    private bool CanResetCounters() => IsConnected;
    private bool CanToggleTrackPower() => IsConnected;

    /// <summary>
    /// Performs graceful cleanup before app termination.
    /// Disconnects from Z21 if connected, ensuring LAN_LOGOFF is sent.
    /// Called from App.OnWindowDestroying or Android MainActivity.OnDestroy.
    /// </summary>
    public async Task CleanupAsync()
    {
        this.Log("🔄 CounterViewModel: CleanupAsync starting...");

        try
        {
            if (IsConnected)
            {
                this.Log("📡 CounterViewModel: Disconnecting from Z21...");
                await DisconnectAsync();
                this.Log("✅ CounterViewModel: Z21 disconnect complete (LAN_LOGOFF sent)");
            }
            else
            {
                this.Log("ℹ️ CounterViewModel: Not connected, no disconnect needed");
            }

            // Persist any pending settings
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            this.Log($"⚠️ CounterViewModel: Cleanup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles the connection state (connect or disconnect).
    /// Used by Switch controls for unified connection toggle.
    /// </summary>
    /// <param name="connect">True to connect, false to disconnect.</param>
    [RelayCommand]
    public async Task SetConnectionAsync(bool connect)
    {
        if (connect)
        {
            await ConnectAsync();
        }
        else
        {
            await DisconnectAsync();
        }
    }

    /// <summary>
    /// Sets the track power ON or OFF.
    /// </summary>
    /// <param name="turnOn">True to turn ON the track power, false to turn it OFF.</param>
    [RelayCommand(CanExecute = nameof(CanToggleTrackPower))]
    public async Task SetTrackPowerAsync(bool turnOn)
    {
        try
        {
            if (turnOn)
            {
                await _z21.SetTrackPowerOnAsync();
                StatusText = "Track power ON";
                IsTrackPowerOn = true;
            }
            else
            {
                await _z21.SetTrackPowerOffAsync();
                StatusText = "Track power OFF";
                IsTrackPowerOn = false;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Track power error: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles Z21 system state changes and updates UI properties.
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread via IUiDispatcher.
    /// </summary>
    private void OnSystemStateChanged(Backend.SystemState systemState)
    {
        _dispatcher.InvokeOnUi(() => UpdateSystemState(systemState));
    }

    /// <summary>
    /// Handles Z21 XBus status changes (track power, emergency stop, etc.).
    /// This is called when GetStatusAsync() response arrives or when track power changes.
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread via IUiDispatcher.
    /// </summary>
    private void OnXBusStatusChanged(Backend.Protocol.XBusStatus xBusStatus)
    {
        _dispatcher.InvokeOnUi(() =>
        {
            // TrackOff=true means power is OFF, so invert it
            IsTrackPowerOn = !xBusStatus.TrackOff;

            this.Log($"📊 XBus status updated: TrackPowerOn={IsTrackPowerOn}, EmergencyStop={xBusStatus.EmergencyStop}, ShortCircuit={xBusStatus.ShortCircuit}");
        });
    }

    /// <summary>
    /// Handles Z21 version info changes (serial number, hardware type, firmware version).
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread via IUiDispatcher.
    /// </summary>
    private void OnVersionInfoChanged(Backend.Z21VersionInfo versionInfo)
    {
        _dispatcher.InvokeOnUi(() =>
        {
            SerialNumber = versionInfo.SerialNumber.ToString();
            FirmwareVersion = versionInfo.FirmwareVersion;
            HardwareType = versionInfo.HardwareType;
            HardwareVersion = versionInfo.HardwareVersion.ToString();

            this.Log($"📌 Z21 Version Info: S/N={SerialNumber}, HW={HardwareType}, FW={FirmwareVersion}");
        });
    }

    /// <summary>
    /// Updates system state properties on the main thread (UI thread).
    /// </summary>
    private void UpdateSystemState(Backend.SystemState systemState)
    {
        // Update track power status from Z21 (reflects external changes, e.g., from Z21 app)
        IsTrackPowerOn = systemState.IsTrackPowerOn;

        // Only update system state values when track power is ON
        // When OFF, reset to zero to avoid showing stale values
        if (systemState.IsTrackPowerOn)
        {
            MainCurrent = systemState.MainCurrent;
            Temperature = systemState.Temperature;
            SupplyVoltage = systemState.SupplyVoltage;
            VccVoltage = systemState.VccVoltage;
        }
        else
        {
            MainCurrent = 0;
            Temperature = 0;
            SupplyVoltage = 0;
            VccVoltage = 0;
        }

        this.Log($"📊 System state updated: MainCurrent={MainCurrent}mA, Temp={Temperature}C, Supply={SupplyVoltage}mV, TrackPower={IsTrackPowerOn}");
    }

    /// <summary>
    /// Handles Z21 feedback events and increments lap counter.
    /// Implements timer-based filtering to ignore multiple axle detections (e.g., 16-axle train).
    /// Tracks lap times and checks for target completion.
    /// Backend uses Feedback delegate: void Feedback(FeedbackResult feedbackContent)
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread via IUiDispatcher.
    /// </summary>
    private void OnFeedbackReceived(Backend.FeedbackResult feedbackResult)
    {
        this.Log($"🔔 OnFeedbackReceived called! InPort={feedbackResult.InPort}");

        // Check if feedback should be ignored based on timer (safe on any thread)
        if (ShouldIgnoreFeedback(feedbackResult.InPort))
        {
            this.Log($"⏭️ Feedback for InPort {feedbackResult.InPort} ignored (timer active)");
            return;
        }

        // ✅ Dispatch UI updates to main thread
        _dispatcher.InvokeOnUi(() => ProcessFeedback(feedbackResult));
    }

    /// <summary>
    /// Processes feedback on the main thread (UI thread).
    /// This method is safe to call from OnFeedbackReceived via IUiDispatcher.
    /// </summary>
    private void ProcessFeedback(Backend.FeedbackResult feedbackResult)
    {
        // Find the statistic for this InPort
        var stat = Statistics.FirstOrDefault(s => s.InPort == feedbackResult.InPort);
        if (stat == null)
        {
            this.Log($"⚠️ Feedback for unknown InPort {feedbackResult.InPort} (not in Statistics collection)");
            return;
        }

        var now = DateTime.UtcNow;

        // Calculate lap time (time between this feedback and last feedback)
        if (stat.LastFeedbackTime.HasValue)
        {
            stat.LastLapTime = now - stat.LastFeedbackTime.Value;
            this.Log($"⏱️ InPort {feedbackResult.InPort} lap time: {stat.LastLapTimeFormatted}");
        }

        // Update last feedback time
        stat.LastFeedbackTime = now;
        UpdateLastFeedbackTime(feedbackResult.InPort);

        // Increment lap count
        stat.Count++;

        // Note: StatusText no longer shows InPort feedback - this info is in Track Cards
        this.Log($"🔔 Feedback: InPort {feedbackResult.InPort} → Count: {stat.Count}/{stat.TargetLapCount}");
    }

    /// <summary>
    /// Determines whether feedback should be ignored based on timer settings.
    /// Similar to BaseFeedbackManager.ShouldIgnoreFeedback logic.
    /// </summary>
    /// <param name="inPort">The InPort number to check</param>
    /// <returns>True if feedback should be ignored, false otherwise</returns>
    private bool ShouldIgnoreFeedback(int inPort)
    {
        if (!UseTimerFilter)
        {
            return false;
        }

        if (_lastFeedbackTime.TryGetValue(inPort, out DateTime lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime.ToUniversalTime()).TotalSeconds;
            return elapsed < TimerIntervalSeconds;
        }

        return false;
    }

    /// <summary>
    /// Updates the last feedback time for the specified InPort.
    /// </summary>
    /// <param name="inPort">The InPort number to update</param>
    private void UpdateLastFeedbackTime(int inPort)
    {
        _lastFeedbackTime[inPort] = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the IP address is in a local network range.
    /// Z21 should always be in local network (192.168.x.x, 10.x.x.x, 172.16-31.x.x).
    /// </summary>
    private bool IsLocalNetworkAddress(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();

        // 192.168.x.x (most common for home networks)
        if (bytes[0] == 192 && bytes[1] == 168)
            return true;

        // 10.x.x.x (corporate networks)
        if (bytes[0] == 10)
            return true;

        // 172.16.x.x - 172.31.x.x (less common)
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;

        return false;
    }

    /// <summary>
    /// Disposes the CounterViewModel and unsubscribes from Z21 events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources (managed and unmanaged).
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Dispose managed resources
            try
            {
                _z21.Received -= OnFeedbackReceived;
                _z21.OnSystemStateChanged -= OnSystemStateChanged;
                _z21.OnXBusStatusChanged -= OnXBusStatusChanged;
                _z21.OnVersionInfoChanged -= OnVersionInfoChanged;
                this.Log("✅ CounterViewModel: Unsubscribed from Z21 events");
            }
            catch (Exception ex)
            {
                this.Log($"⚠️ Error unsubscribing from Z21 events: {ex.Message}");
            }

            _lastFeedbackTime.Clear();
        }

        // No unmanaged resources to dispose

        _disposed = true;
    }
}

/// <summary>
/// Represents lap statistics for a single InPort (track).
/// </summary>
public partial class InPortStatistic : ObservableObject
{
    [ObservableProperty]
    private int inPort;

    [ObservableProperty]
    private int count;

    [ObservableProperty]
    private int targetLapCount = 10; // Default: 10 laps

    [ObservableProperty]
    private TimeSpan? lastLapTime;

    [ObservableProperty]
    private DateTime? lastFeedbackTime;

    /// <summary>
    /// Indicates whether this track has received at least one lap.
    /// Used for UI: Red background (no laps) → Green background (has laps).
    /// </summary>
    [ObservableProperty]
    private bool hasReceivedFirstLap;

    /// <summary>
    /// Progress as percentage (0.0 to 1.0) for ProgressBar binding.
    /// </summary>
    public double Progress => TargetLapCount > 0 ? (double)Count / TargetLapCount : 0.0;

    /// <summary>
    /// Formatted last lap time for display (mm:ss or --:--).
    /// </summary>
    public string LastLapTimeFormatted => LastLapTime.HasValue
        ? $"{LastLapTime.Value.Minutes:D2}:{LastLapTime.Value.Seconds:D2}"
        : "--:--";

    /// <summary>
    /// Formatted last feedback time for display (HH:mm:ss or --:--:--).
    /// Shows the time when the last feedback was received.
    /// </summary>
    public string LastFeedbackTimeFormatted => LastFeedbackTime.HasValue
        ? LastFeedbackTime.Value.ToLocalTime().ToString("HH:mm:ss")
        : "--:--:--";

    /// <summary>
    /// Formatted lap count for display (X/Y laps format).
    /// </summary>
    public string LapCountFormatted => $"{Count}/{TargetLapCount} laps";

    /// <summary>
    /// Background color name for track card.
    /// Material Design inspired colors for better UI harmony:
    /// - #EF5350 (Red 400): Soft red for "no activity" state - not too harsh, good readability
    /// - #66BB6A (Green 400): Bright green for "active" state - visible on dark background
    /// Returns a color name string that can be converted to Color in XAML.
    /// </summary>
    public string BackgroundColorName => HasReceivedFirstLap ? "#66BB6A" : "#EF5350";

    partial void OnCountChanged(int value)
    {
        // Mark as received first lap when count > 0
        if (value > 0 && !HasReceivedFirstLap)
        {
            HasReceivedFirstLap = true;
        }
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnHasReceivedFirstLapChanged(bool _)
    {
        // Notify UI about background color change
        OnPropertyChanged(nameof(BackgroundColorName));
    }

    partial void OnTargetLapCountChanged(int _)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnLastLapTimeChanged(TimeSpan? _)
    {
        OnPropertyChanged(nameof(LastLapTimeFormatted));
    }

    partial void OnLastFeedbackTimeChanged(DateTime? _)
    {
        OnPropertyChanged(nameof(LastFeedbackTimeFormatted));
    }
}
