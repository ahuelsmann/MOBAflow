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
    /// Initializes the Statistics collection from the CountOfFeedbackPoints setting.
    /// Call this when a project is loaded or when the setting changes.
    /// </summary>
    public void InitializeStatisticsFromFeedbackPoints()
    {
        Statistics.Clear();

        // Create statistics based on CountOfFeedbackPoints setting
        int count = _settings.Counter.CountOfFeedbackPoints;
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
            this.Log($"✅ Initialized {Statistics.Count} track statistics from settings (InPorts 1-{count})");
        }
        else
        {
            this.Log("ℹ️ CountOfFeedbackPoints is 0 - no track statistics initialized. Set CountOfFeedbackPoints in settings to enable.");
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

    [RelayCommand(CanExecute = nameof(CanResetCounters))]
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

    private bool CanResetCounters() => true; // Always enabled

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


