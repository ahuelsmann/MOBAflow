namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net;
using System.Collections.Generic;

/// <summary>
/// ViewModel for lap counter functionality.
/// Connects directly to Z21 via UDP and tracks laps per InPort.
/// Implements timer-based feedback filtering to handle multiple axle detections from the same train pass.
/// </summary>
public partial class CounterViewModel : ObservableObject
{
    private Backend.Z21? _z21;
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = new();

    public CounterViewModel()
    {
        // Initialize with default IP and 3 InPorts
        Z21IpAddress = "192.168.0.111";
        
        // Setup 3 parallel tracks (InPorts 1, 2, 3)
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

            // Create Z21 instance
            _z21 = new Backend.Z21();

            // Connect to Z21
            await _z21.ConnectAsync(ipAddress);

            // Subscribe to feedback events (Feedback delegate, not EventHandler)
            _z21.Received += OnFeedbackReceived;

            IsConnected = true;
            StatusText = $"Connected to {Z21IpAddress}";

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ResetCountersCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ Z21 Connection Error: {ex}");
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
                await _z21.DisconnectAsync();
                _z21.Dispose();
                _z21 = null;
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
            System.Diagnostics.Debug.WriteLine($"⚠️ Z21 Disconnect Error: {ex}");
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
        }

        _lastFeedbackTime.Clear();
        StatusText = "Counters reset";
        System.Diagnostics.Debug.WriteLine("🔄 All lap counters reset to 0");
    }

    private bool CanConnect() => !IsConnected;
    private bool CanDisconnect() => IsConnected;
    private bool CanResetCounters() => IsConnected;

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
            System.Diagnostics.Debug.WriteLine($"⏭️ Feedback for InPort {feedbackResult.InPort} ignored (timer active)");
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
            System.Diagnostics.Debug.WriteLine($"⏱️ InPort {feedbackResult.InPort} lap time: {stat.LastLapTimeFormatted}");
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

        StatusText = $"InPort {feedbackResult.InPort}: {stat.Count}/{stat.TargetLapCount} laps";
        System.Diagnostics.Debug.WriteLine($"🔔 Feedback: InPort {feedbackResult.InPort} → Count: {stat.Count}/{stat.TargetLapCount}");
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
        System.Diagnostics.Debug.WriteLine($"🎉 Target reached! InPort {stat.InPort}: {stat.Count} laps");

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
            System.Diagnostics.Debug.WriteLine("🔔 Notification sound played");
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to play notification sound: {ex.Message}");
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
    /// Progress as percentage (0.0 to 1.0) for ProgressBar binding.
    /// </summary>
    public double Progress => TargetLapCount > 0 ? (double)Count / TargetLapCount : 0.0;

    /// <summary>
    /// Formatted last lap time for display (mm:ss or --:--).
    /// </summary>
    public string LastLapTimeFormatted => LastLapTime.HasValue 
        ? $"{LastLapTime.Value.Minutes:D2}:{LastLapTime.Value.Seconds:D2}" 
        : "--:--";

    partial void OnCountChanged(int value)
    {
        OnPropertyChanged(nameof(Progress));
    }

    partial void OnTargetLapCountChanged(int value)
    {
        OnPropertyChanged(nameof(Progress));
    }

    partial void OnLastLapTimeChanged(TimeSpan? value)
    {
        OnPropertyChanged(nameof(LastLapTimeFormatted));
    }
}
