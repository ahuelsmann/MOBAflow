namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net;
using System.Collections.Generic;
using Moba.Backend.Interface;
using Moba.SharedUI.Extensions;

/// <summary>
/// ViewModel that connects to a Z21 and counts laps per InPort (simple demo feature).
/// - Platform usage: used by MAUI and Web to demonstrate UDP-driven workflows
/// - Threading: Z21 events arrive on background threads; UI updates are dispatched to main thread
/// - Filtering: optional timer-based filter to ignore multiple axle detections
/// </summary>
public partial class CounterViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = new();

    public CounterViewModel(IZ21 z21)
    {
        _z21 = z21;

        Z21IpAddress = "192.168.0.111";
        Statistics.Add(new InPortStatistic { InPort = 1, Count = 0, TargetLapCount = GlobalTargetLapCount });
        Statistics.Add(new InPortStatistic { InPort = 2, Count = 0, TargetLapCount = GlobalTargetLapCount });
        Statistics.Add(new InPortStatistic { InPort = 3, Count = 0, TargetLapCount = GlobalTargetLapCount });
    }

    [ObservableProperty]
    private string z21IpAddress = "192.168.0.111";

    [ObservableProperty]
    private bool isConnected;

    /// <summary>
    /// Helper property for UI binding (inverse of IsConnected).
    /// </summary>
    public bool IsNotConnected => !IsConnected;

    partial void OnIsConnectedChanged(bool value)
    {
        // Notify UI about inverse property change
        OnPropertyChanged(nameof(IsNotConnected));
        
        // Update command states
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        ResetCountersCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string statusText = "Disconnected";

    [ObservableProperty]
    private ObservableCollection<InPortStatistic> statistics = [];

    [ObservableProperty]
    private bool useTimerFilter = true;

    [ObservableProperty]
    private double timerIntervalSeconds = 5.0;

    [ObservableProperty]
    private int globalTargetLapCount = 10;

    // Z21 System State Properties
    [ObservableProperty]
    private int mainCurrent;

    [ObservableProperty]
    private int temperature;

    [ObservableProperty]
    private int supplyVoltage;

    [ObservableProperty]
    private int vccVoltage;

    [ObservableProperty]
    private string centralState = "0x00";

    [ObservableProperty]
    private string centralStateEx = "0x00";

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        // Update all tracks when global target changes
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
            stat.IsCompleted = false; // Reset completion status
        }
    }

    /// <summary>
    /// Connects to Z21 digital command station.
    /// Validates IP address format before attempting connection.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        try
        {
            StatusText = "Connecting...";

            // Parse IP address
            if (!IPAddress.TryParse(Z21IpAddress, out var ipAddress))
            {
                StatusText = "Invalid IP address";
                return;
            }

            // ✅ Check if IP is in local network range (192.168.x.x or 10.x.x.x)
            if (!IsLocalNetworkAddress(ipAddress))
            {
                StatusText = "Error: Z21 must be in local network (e.g., 192.168.x.x)";
                this.Log($"❌ IP {ipAddress} is not in local network range");
                return;
            }

            // Connect to Z21 (will throw exception if unreachable)
            await _z21.ConnectAsync(ipAddress);

            // Subscribe to feedback events (Feedback delegate, not EventHandler)
            _z21.Received += OnFeedbackReceived;

            // Subscribe to system state changes
            _z21.OnSystemStateChanged += OnSystemStateChanged;

            IsConnected = true;
            StatusText = $"Connected to {Z21IpAddress}";

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ResetCountersCommand.NotifyCanExecuteChanged();
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Network-related errors (e.g., no route to host, network unreachable)
            StatusText = $"Network error: Cannot reach Z21. Check network connection.";
            this.Log($"❌ Z21 Connection Error (Socket): {ex.Message}");
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            this.Log($"❌ Z21 Connection Error: {ex}");
        }
    }

    /// <summary>
    /// Disconnects from Z21.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        try
        {
            StatusText = "Disconnecting...";

            if (_z21 != null)
            {
                _z21.Received -= OnFeedbackReceived;
                _z21.OnSystemStateChanged -= OnSystemStateChanged;
                await _z21.DisconnectAsync();
                _z21.Dispose();
                // do not set _z21 = null; kept via DI
            }

            IsConnected = false;
            StatusText = "Disconnected";

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ResetCountersCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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
            stat.IsCompleted = false;
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

    /// <summary>
    /// Handles Z21 system state changes and updates UI properties.
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread.
    /// </summary>
    private void OnSystemStateChanged(Backend.SystemState systemState)
    {
        // Dispatch to main thread for UI updates
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateSystemStateOnMainThread(systemState);
        });
#else
        // Fallback for other platforms (e.g., unit tests)
        UpdateSystemStateOnMainThread(systemState);
#endif
    }

    /// <summary>
    /// Updates system state properties on the main thread (UI thread).
    /// </summary>
    private void UpdateSystemStateOnMainThread(Backend.SystemState systemState)
    {
        MainCurrent = systemState.MainCurrent;
        Temperature = systemState.Temperature;
        SupplyVoltage = systemState.SupplyVoltage;
        VccVoltage = systemState.VccVoltage;
        CentralState = $"0x{systemState.CentralState:X2}";
        CentralStateEx = $"0x{systemState.CentralStateEx:X2}";
    }

    /// <summary>
    /// Handles Z21 feedback events and increments lap counter.
    /// Implements timer-based filtering to ignore multiple axle detections (e.g., 16-axle train).
    /// Tracks lap times and checks for target completion.
    /// Backend uses Feedback delegate: void Feedback(FeedbackResult feedbackContent)
    /// IMPORTANT: This method is called from a background thread (UDP callback),
    /// so all UI updates must be dispatched to the main thread.
    /// </summary>
    private void OnFeedbackReceived(Backend.FeedbackResult feedbackResult)
    {
        // Check if feedback should be ignored based on timer (safe on any thread)
        if (ShouldIgnoreFeedback(feedbackResult.InPort))
        {
            this.Log($"⏭️ Feedback for InPort {feedbackResult.InPort} ignored (timer active)");
            return;
        }

        // Dispatch to main thread for UI updates
        // ⚠️ CRITICAL: ObservableCollection and ObservableProperty changes MUST happen on UI thread!
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            ProcessFeedbackOnMainThread(feedbackResult);
        });
#else
        // Fallback for other platforms (e.g., unit tests)
        ProcessFeedbackOnMainThread(feedbackResult);
#endif
    }

    /// <summary>
    /// Processes feedback on the main thread (UI thread).
    /// This method is safe to call from OnFeedbackReceived via MainThread.BeginInvokeOnMainThread.
    /// </summary>
    private void ProcessFeedbackOnMainThread(Backend.FeedbackResult feedbackResult)
    {
        // Find the statistic for this InPort
        var stat = Statistics.FirstOrDefault(s => s.InPort == feedbackResult.InPort);
        if (stat == null)
        {
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

        // Check if target reached (only trigger once)
        if (stat.Count >= stat.TargetLapCount && !stat.IsCompleted)
        {
            stat.IsCompleted = true;
            OnTargetReached(stat);
        }

        // Note: StatusText no longer shows InPort feedback - this info is in Track Cards
        this.Log($"🔔 Feedback: InPort {feedbackResult.InPort} → Count: {stat.Count}/{stat.TargetLapCount}");
    }

    /// <summary>
    /// Event raised when a track reaches its target lap count.
    /// </summary>
    public event EventHandler<InPortStatistic>? TargetReached;

    /// <summary>
    /// Called when a track reaches its target lap count.
    /// Plays notification sound and raises event for UI to show alert.
    /// </summary>
    private void OnTargetReached(InPortStatistic stat)
    {
        this.Log($"🎉 Target reached! InPort {stat.InPort}: {stat.Count} laps");

        // Play notification sound
        PlayNotificationSound();

        // Raise event for UI (MAUI MainPage can subscribe)
        TargetReached?.Invoke(this, stat);
    }

    /// <summary>
    /// Plays a notification sound when target is reached.
    /// Uses system default notification sound.
    /// </summary>
    private void PlayNotificationSound()
    {
        try
        {
#if ANDROID
            // Android: Play default notification sound
            var notification = Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Notification);
            var ringtone = Android.Media.RingtoneManager.GetRingtone(Android.App.Application.Context, notification);
            ringtone?.Play();
            this.Log("🔔 Notification sound played");
#endif
        }
        catch (Exception ex)
        {
            this.Log($"⚠️ Failed to play notification sound: {ex.Message}");
        }
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
    /// Checks if Z21 is reachable by attempting a UDP connection test.
    /// Uses a timeout to avoid hanging when outside network.
    /// </summary>
    private async Task<bool> IsZ21ReachableAsync(IPAddress ipAddress)
    {
        try
        {
            // Simple UDP reachability test with timeout
            using var udpClient = new System.Net.Sockets.UdpClient();
            udpClient.Client.SendTimeout = 2000; // 2 seconds timeout
            udpClient.Client.ReceiveTimeout = 2000;
            
            // Try to connect to Z21 port (21105)
            var endpoint = new IPEndPoint(ipAddress, 21105);
            
            // Note: UDP doesn't have a "connect" in the TCP sense,
            // but we can try to send a small packet and see if it fails immediately
            // For now, we just check if we can create the client (basic validation)
            
            // Better approach: Check if we're on same subnet as Z21
            var localIPs = await GetLocalIPAddressesAsync();
            foreach (var localIP in localIPs)
            {
                if (IsInSameSubnet(localIP, ipAddress))
                {
                    this.Log($"✅ Z21 {ipAddress} is in same subnet as {localIP}");
                    return true;
                }
            }
            
            this.Log($"⚠️ Z21 {ipAddress} is not in same subnet as device");
            return false; // Not in same subnet → likely not reachable
        }
        catch (Exception ex)
        {
            this.Log($"⚠️ Network check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all local IP addresses of the device.
    /// </summary>
    private async Task<List<IPAddress>> GetLocalIPAddressesAsync()
    {
        var addresses = new List<IPAddress>();
        
        try
        {
            // Get all network interfaces
            var hostName = Dns.GetHostName();
            var hostEntry = await Dns.GetHostEntryAsync(hostName);
            
            foreach (var address in hostEntry.AddressList)
            {
                // Only IPv4 addresses
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    addresses.Add(address);
                }
            }
        }
        catch (Exception ex)
        {
            this.Log($"⚠️ Failed to get local IP addresses: {ex.Message}");
        }
        
        return addresses;
    }

    /// <summary>
    /// Checks if two IP addresses are in the same subnet (assuming /24 subnet mask).
    /// </summary>
    private bool IsInSameSubnet(IPAddress ip1, IPAddress ip2)
    {
        var bytes1 = ip1.GetAddressBytes();
        var bytes2 = ip2.GetAddressBytes();
        
        // Check first 3 octets (assumes /24 subnet, typical for home networks)
        return bytes1[0] == bytes2[0] && 
               bytes1[1] == bytes2[1] && 
               bytes1[2] == bytes2[2];
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
    private bool isCompleted;

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

    partial void OnHasReceivedFirstLapChanged(bool value)
    {
        // Notify UI about background color change
        OnPropertyChanged(nameof(BackgroundColorName));
    }

    partial void OnTargetLapCountChanged(int value)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnLastLapTimeChanged(TimeSpan? value)
    {
        OnPropertyChanged(nameof(LastLapTimeFormatted));
    }

    partial void OnLastFeedbackTimeChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(LastFeedbackTimeFormatted));
    }
}

