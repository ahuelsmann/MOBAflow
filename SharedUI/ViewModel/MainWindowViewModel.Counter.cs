// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using Common.Runtime;

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

    private MobaRuntimeSnapshot _latestRuntimeSnapshot = MobaRuntimeSnapshot.Empty;

    #endregion

    #region Counter Initialization

    /// <summary>
    /// Initializes the Statistics collection from the CountOfFeedbackPoints setting.
    /// Call this when a project is loaded or when the setting changes.
    /// Replaces the collection (new instance) instead of Clear+Add to avoid COMException:
    /// no CollectionChanged during WinUI binding updates, only a single PropertyChanged.
    /// </summary>
    public void InitializeStatisticsFromFeedbackPoints()
    {
        int count = _settings.Counter.CountOfFeedbackPoints;
        var list = new List<InPortStatistic>();
        if (count > 0)
        {
            for (int i = 1; i <= count; i++)
            {
                list.Add(new InPortStatistic
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

        Statistics = new ObservableCollection<InPortStatistic>(list);
        ApplyInPortCounterSnapshot(_latestRuntimeSnapshot);
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        // Save to AppSettings (RAM)
        _settings.Counter.TargetLapCount = value;

        PersistSettingsSafely();

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

        PersistSettingsSafely();
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        // Save to AppSettings (RAM)
        _settings.Counter.TimerIntervalSeconds = value;

        PersistSettingsSafely();
    }

    /// <summary>
    /// Called when SelectedProject changes.
    /// Re-initializes track statistics based on the new project's FeedbackPoints.
    /// Subscribes to PropertyChanged for auto-save (Project + all Workflows).
    /// Auto-selects first journey if available.
    /// </summary>
    partial void OnSelectedProjectChanging(ProjectViewModel? value)
    {
        _ = value;
        var checkpointTask = _mobaRuntime.CheckpointUsageAsync();
        SynchronizeVehicleUsageFromRuntime();
        ObserveBackgroundTask(checkpointTask, "Checkpoint project usage before selection change");
    }

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        _locomotiveWhistleAutomation?.Activate(value?.Model);
        RefreshProjectDiagnostics();

        // Statistics are replaced (new ObservableCollection), not mutated in place,
        // so no Enqueue needed – only one PropertyChanged, no CollectionChanged during binding.
        InitializeStatisticsFromFeedbackPoints();
        ApplyJourneyRuntimeSnapshots(_latestRuntimeSnapshot.JourneyStates);

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

            foreach (var train in value.Trains)
            {
                train.PropertyChanged -= OnViewModelPropertyChanged;
                train.PropertyChanged += OnViewModelPropertyChanged;
            }

            // Auto-select first journey when project is selected
            if (value.Journeys.Count > 0)
            {
                SelectedJourney = value.Journeys.FirstOrDefault();
            }
            else
            {
                SelectedJourney = null;
            }

            SelectedTrain = value.Trains.FirstOrDefault();

            OnPropertyChanged(nameof(FilteredTrains));
            OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
            OnPropertyChanged(nameof(FilteredPassengerWagonLibrary));
            OnPropertyChanged(nameof(FilteredGoodsWagonLibrary));

            ObserveBackgroundTask(RefreshActiveProjectRuntimeAsync(), "Activate project runtime");
        }
        else
        {
            // Clear journey selection when no project is selected
            SelectedJourney = null;
            SelectedTrain = null;
        }

        UpdateSolutionLoadedStatus();
    }

    #endregion

    #region Counter Commands

    [RelayCommand(CanExecute = nameof(CanResetCounters))]
    private async Task ResetCounters()
    {
        try
        {
            await _runtimeCommandGateway.ResetInPortCountersAsync();
            JourneyCommandStatus = "InPort counters reset.";
        }
        catch (Exception ex)
        {
            JourneyCommandStatus = ex.Message;
            _logger.LogWarning(ex, "Resetting InPort counters failed");
        }
    }

    private bool CanResetCounters() => _latestRuntimeSnapshot.CanResetInPortCounters;

    #endregion

    #region Counter Event Handlers (Feedback Processing)

    /// <summary>
    /// Projects the authoritative session counters from the selected runtime.
    /// </summary>
    private void ApplyInPortCounterSnapshot(MobaRuntimeSnapshot snapshot)
    {
        foreach (var counter in snapshot.InPortCounters)
        {
            var stat = Statistics.FirstOrDefault(s => s.InPort == counter.InPort);
            if (stat == null)
            {
                stat = new InPortStatistic { InPort = checked((int)counter.InPort),
                    Name = $"Feedback Point {counter.InPort}", TargetLapCount = GlobalTargetLapCount };
                Statistics.Add(stat);
            }
            stat.Count = counter.Count;
            stat.LastFeedbackTime = counter.LastFeedbackTime?.UtcDateTime;
            stat.LastLapTime = counter.LastLapTime;
            stat.HasReceivedFirstLap = counter.Count > 0;
        }
        foreach (var stat in Statistics.Where(stat => !snapshot.InPortCounters.Any(c => c.InPort == stat.InPort)))
        {
            stat.Count = 0;
            stat.LastFeedbackTime = null;
            stat.LastLapTime = null;
            stat.HasReceivedFirstLap = false;
        }
    }

    #endregion
}
