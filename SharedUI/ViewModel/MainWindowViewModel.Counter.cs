// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;
using System.Diagnostics;

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
    private ObservableCollection<InPortStatistic> _statistics = [];

    /// <summary>
    /// Global target lap count for all tracks.
    /// When changed, updates all existing statistics.
    /// </summary>
    [ObservableProperty]
    private int _globalTargetLapCount = 10;

    /// <summary>
    /// Enables timer-based filtering to prevent multiple counts from long trains (e.g., 16 axles).
    /// </summary>
    [ObservableProperty]
    private bool _useTimerFilter;

    /// <summary>
    /// Timer filter interval in seconds (prevents duplicate counts within this timeframe).
    /// Synchronized with AppSettings.Counter.TimerIntervalSeconds.
    /// </summary>
    [ObservableProperty]
    private double _timerIntervalSeconds;

    /// <summary>
    /// Main current in mA (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int _mainCurrent;

    /// <summary>
    /// Temperature in Celsius (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int _temperature;

    /// <summary>
    /// Supply voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int _supplyVoltage;

    /// <summary>
    /// VCC voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    private int _vccVoltage;

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = [];

    #endregion

    #region Counter Initialization

    /// <summary>
    /// Initializes the Statistics collection from the CountOfFeedbackPoints setting.
    /// Call this when a project is loaded or when the setting changes.
    /// IMPORTANT: Must be called from UI thread (caller responsible for dispatching).
    /// </summary>
    public void InitializeStatisticsFromFeedbackPoints()
    {
        // CRITICAL FIX: Do not use InvokeOnUi here.
        // This method is already called via EnqueueOnUi from OnSelectedProjectChanged,
        // so we are guaranteed to be on the UI thread. A second InvokeOnUi causes reentrancy.
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
        }
        else
        {
            _logger.LogInformation("CountOfFeedbackPoints is 0 - no track statistics initialized. Set CountOfFeedbackPoints in settings to enable");
        }
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        // Save to AppSettings (RAM)
        _settings.Counter.TargetLapCount = value;

        // Persist to appsettings.json (Disk) - fire and forget
        _settingsService?.SaveSettingsAsync(_settings);

        // Update all existing statistics when global target changes
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        // Save to AppSettings (RAM)
        _settings.Counter.UseTimerFilter = value;

        // Persist to appsettings.json (Disk) - fire and forget
        _settingsService?.SaveSettingsAsync(_settings);
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        // Save to AppSettings (RAM)
        _settings.Counter.TimerIntervalSeconds = value;

        Debug.WriteLine($"OnTimerIntervalSecondsChanged called. Value={value}, SettingsService={(_settingsService != null ? "EXISTS" : "NULL")}");

        // Persist to appsettings.json (Disk) - fire and forget
        if (_settingsService != null)
        {
            Debug.WriteLine($"Calling SaveSettingsAsync with TimerIntervalSeconds={value}");
            _settingsService.SaveSettingsAsync(_settings);
        }
        else
        {
            Debug.WriteLine("SettingsService is NULL - cannot save.");
        }
    }

    /// <summary>
    /// Called when SelectedProject changes.
    /// Re-initializes track statistics based on the new project's FeedbackPoints.
    /// Subscribes to PropertyChanged for auto-save (Project + all Workflows).
    /// Auto-selects first journey if available.
    /// </summary>
    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        _ = value; // Suppress unused parameter warning

        // CRITICAL: Use EnqueueOnUi (not InvokeOnUi) to force async execution.
        // This breaks out of the PropertyChanged notification chain and prevents COMException
        // when modifying the Statistics ObservableCollection during WinUI binding updates.
        _uiDispatcher.EnqueueOnUi(() =>
        {
            InitializeStatisticsFromFeedbackPoints();
        });

        // Subscribe to PropertyChanged for auto-save
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;

            // Subscribe to PropertyChanged events for all workflows (including newly loaded ones)
            foreach (var workflow in value.Workflows)
            {
                // Avoid duplicate subscriptions
                workflow.PropertyChanged -= OnViewModelPropertyChanged;
                workflow.PropertyChanged += OnViewModelPropertyChanged;
            }

            // Auto-select first journey when project is selected
            if (value.Journeys.Count > 0)
            {
                SelectedJourney = value.Journeys.FirstOrDefault();
            }
        }
        else
        {
            // Clear journey selection when no project is selected
            SelectedJourney = null;
        }
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
        _logger.LogInformation("All counters reset");
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
                    _logger.LogDebug("InPort {InPort}: Ignored (timer filter: {Elapsed:F1}s < {Threshold}s)",
                        inPort, elapsed.TotalSeconds, TimerIntervalSeconds);
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

        _logger.LogInformation("InPort {InPort}: Lap {Count}/{Target} | Lap time: {LapTime}",
            inPort, stat.Count, stat.TargetLapCount, stat.LastLapTimeFormatted);
    }

    /// <summary>
    /// Handles Z21 feedback events (subscription in MainWindowViewModel.cs constructor).
    /// </summary>
    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateTrackStatistics((uint)feedback.InPort));
    }

    #endregion
}