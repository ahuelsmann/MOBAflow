// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

/// <summary>
/// MainWindowViewModel - Counter/Statistics Features
/// Lap counting and track statistics (used by OverviewPage in all platforms).
/// Moved from CounterViewModel to MainWindowViewModel for unified cross-platform ViewModel.
/// </summary>
public partial class MainWindowViewModel
{
    #region Counter Properties

    /// <summary>
    /// Collection of lap statistics per InPort (track).
    /// Dynamically populated from FeedbackPoints or defaults to InPorts 1-3.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<InPortStatistic> statistics = [];

    /// <summary>
    /// Global target lap count for all tracks.
    /// When changed, updates all existing statistics.
    /// </summary>
    [ObservableProperty]
    private int globalTargetLapCount = 10;

    /// <summary>
    /// Enables timer-based filtering to prevent multiple counts from long trains (e.g., 16 axles).
    /// </summary>
    [ObservableProperty]
    private bool useTimerFilter;

    /// <summary>
    /// Timer filter interval in seconds (prevents duplicate counts within this timeframe).
    /// </summary>
    [ObservableProperty]
    private double timerIntervalSeconds = 2.0;

    /// <summary>
    /// Main current in mA (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int mainCurrent;

    /// <summary>
    /// Temperature in Celsius (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int temperature;

    /// <summary>
    /// Supply voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int supplyVoltage;

    /// <summary>
    /// VCC voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int vccVoltage;

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = new();

    #endregion

    #region Counter Initialization

    /// <summary>
    /// Initializes the Statistics collection from the current project's FeedbackPoints.
    /// Call this when a project is loaded or when FeedbackPoints change.
    /// </summary>
    public void InitializeStatisticsFromFeedbackPoints()
    {
        Statistics.Clear();

        if (SelectedProject?.Model?.FeedbackPoints != null && SelectedProject.Model.FeedbackPoints.Count > 0)
        {
            // Create statistics from actual feedback points
            foreach (var feedbackPoint in SelectedProject.Model.FeedbackPoints.OrderBy(fp => fp.InPort))
            {
                Statistics.Add(new InPortStatistic
                {
                    InPort = (int)feedbackPoint.InPort,
                    Name = feedbackPoint.Name,
                    Count = 0,
                    TargetLapCount = GlobalTargetLapCount
                });
            }
            this.Log($"✅ Initialized {Statistics.Count} track statistics from FeedbackPoints");
        }
        else
        {
            // Fallback: Create default statistics (InPorts 1-3)
            Statistics.Add(new InPortStatistic { InPort = 1, Count = 0, TargetLapCount = GlobalTargetLapCount });
            Statistics.Add(new InPortStatistic { InPort = 2, Count = 0, TargetLapCount = GlobalTargetLapCount });
            Statistics.Add(new InPortStatistic { InPort = 3, Count = 0, TargetLapCount = GlobalTargetLapCount });
            this.Log("⚠️ No FeedbackPoints found - initialized default track statistics (InPorts 1-3)");
        }
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        // Update all existing statistics when global target changes
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
    }

    /// <summary>
    /// Called when SelectedProject changes.
    /// Re-initializes track statistics based on the new project's FeedbackPoints.
    /// </summary>
    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        _ = value; // Suppress unused parameter warning
        InitializeStatisticsFromFeedbackPoints();
    }

    #endregion

    #region Counter Commands

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
        this.Log("🔄 All counters reset");
    }

    #endregion

    #region Counter Event Handlers (Feedback Processing)

    /// <summary>
    /// Handles Z21 feedback events and updates track statistics.
    /// Called from OnFeedbackReceived in MainWindowViewModel.cs.
    /// </summary>
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
                    this.Log($"⏱️ InPort {inPort}: Ignored (timer filter: {elapsed.TotalSeconds:F1}s < {TimerIntervalSeconds}s)");
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

        this.Log($"📊 InPort {inPort}: Lap {stat.Count}/{stat.TargetLapCount} | Lap time: {stat.LastLapTimeFormatted}");
    }

    /// <summary>
    /// Handles Z21 feedback events (subscription in MainWindowViewModel.cs constructor).
    /// </summary>
    private void OnFeedbackReceived(Backend.FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            UpdateTrackStatistics((uint)feedback.InPort);
        });
    }

    #endregion
}

/// <summary>
/// Represents lap statistics for a single InPort (track).
/// Used by OverviewPage in WinUI, MAUI, and WebApp.
/// </summary>
public partial class InPortStatistic : ObservableObject
{
    [ObservableProperty]
    private int inPort;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private int count;

    [ObservableProperty]
    private int targetLapCount = 10;

    [ObservableProperty]
    private TimeSpan? lastLapTime;

    [ObservableProperty]
    private DateTime? lastFeedbackTime;

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
    /// </summary>
    public string LastFeedbackTimeFormatted => LastFeedbackTime.HasValue
        ? LastFeedbackTime.Value.ToLocalTime().ToString("HH:mm:ss")
        : "--:--:--";

    /// <summary>
    /// Formatted lap count for display (X/Y laps format).
    /// </summary>
    public string LapCountFormatted => $"{Count}/{TargetLapCount} laps";

    /// <summary>
    /// Display name: Uses Name if available, otherwise falls back to "InPort X".
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : $"InPort {InPort}";

    /// <summary>
    /// Background color name for track card.
    /// Material Design inspired colors:
    /// - #EF5350 (Red 400): Soft red for "no activity" state
    /// - #66BB6A (Green 400): Bright green for "active" state
    /// </summary>
    public string BackgroundColorName => HasReceivedFirstLap ? "#66BB6A" : "#EF5350";

    partial void OnCountChanged(int value)
    {
        if (value > 0 && !HasReceivedFirstLap)
        {
            HasReceivedFirstLap = true;
        }
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnHasReceivedFirstLapChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(BackgroundColorName));
    }

    partial void OnTargetLapCountChanged(int value)
    {
        _ = value;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnLastLapTimeChanged(TimeSpan? value)
    {
        _ = value;
        OnPropertyChanged(nameof(LastLapTimeFormatted));
    }

    partial void OnLastFeedbackTimeChanged(DateTime? value)
    {
        _ = value;
        OnPropertyChanged(nameof(LastFeedbackTimeFormatted));
    }
}


