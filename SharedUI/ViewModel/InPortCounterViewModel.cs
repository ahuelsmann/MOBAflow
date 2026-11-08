// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Common.Configuration;
using Common.Events;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Contracts.Runtime;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Owns the feedback-point lap statistics of the selected project's runtime: the counter settings, the
/// authoritative counter snapshot projection, the persistence error and the reset-all command.
/// OverviewPage and the Event Manager share the one host-owned instance; the runtime owns the counters.
/// </summary>
public sealed partial class InPortCounterViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IEventBus _eventBus;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly SettingsPageViewModel _settingsPage;
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<InPortCounterViewModel> _logger;
    private readonly Guid _snapshotSubscription;
    private MobaRuntimeSnapshot _latestRuntimeSnapshot;
    private int _globalTargetLapCount;
    private bool _useTimerFilter;
    private double _timerIntervalSeconds;
    private bool _isDisposed;

    /// <param name="session">Owns the project selection; a project change rebuilds the statistics.</param>
    /// <param name="runtimeSnapshots">Read access to the selected runtime's current snapshot.</param>
    /// <param name="eventBus">Delivers runtime snapshot changes on the UI thread.</param>
    /// <param name="runtimeCommandGateway">Route for the counter commands.</param>
    /// <param name="settingsPage">Shared settings state; raises the feedback-point count change.</param>
    /// <param name="settingsService">Persists the counter settings.</param>
    /// <param name="logger">Logger for persistence and command failures.</param>
    public InPortCounterViewModel(
        ISolutionSession session,
        IRuntimeSnapshotProvider runtimeSnapshots,
        IEventBus eventBus,
        IRuntimeCommandGateway runtimeCommandGateway,
        SettingsPageViewModel settingsPage,
        ISettingsService settingsService,
        ILogger<InPortCounterViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(settingsPage);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _eventBus = eventBus;
        _runtimeCommandGateway = runtimeCommandGateway;
        _settingsPage = settingsPage;
        _settingsService = settingsService;
        _settings = settingsPage.Settings;
        _logger = logger;

        // The settings are read before the statistics are built so every statistic starts with the saved target.
        _globalTargetLapCount = _settings.Counter.TargetLapCount;
        _useTimerFilter = _settings.Counter.UseTimerFilter;
        _timerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;
        _latestRuntimeSnapshot = runtimeSnapshots.Current;
        InitializeStatisticsFromFeedbackPoints();

        _snapshotSubscription = _eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
        _settingsPage.FeedbackPointCountChanged += OnFeedbackPointCountChanged;
        _session.PropertyChanged += OnSessionPropertyChanged;
    }

    /// <summary>
    /// Lap statistics per InPort (feedback point), built from the configured feedback-point count.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<InPortStatistic> Statistics { get; private set; } = [];

    /// <summary>Operator-visible failure to load or save the runtime's local counter file.</summary>
    [ObservableProperty]
    public partial string CounterPersistenceError { get; private set; } = string.Empty;

    /// <summary>Result of the last reset-all counter command.</summary>
    [ObservableProperty]
    public partial string CounterCommandStatus { get; private set; } = string.Empty;

    /// <summary>Target lap count for every feedback point; a change updates the existing statistics.</summary>
    public int GlobalTargetLapCount
    {
        get => _globalTargetLapCount;
        set
        {
            if (!SetProperty(ref _globalTargetLapCount, value))
            {
                return;
            }

            _settings.Counter.TargetLapCount = value;
            PersistCounterSettings();
            foreach (var statistic in Statistics)
            {
                statistic.TargetLapCount = value;
            }
        }
    }

    /// <summary>Prevents multiple counts from one long train passing a feedback point.</summary>
    public bool UseTimerFilter
    {
        get => _useTimerFilter;
        set
        {
            if (SetProperty(ref _useTimerFilter, value))
            {
                _settings.Counter.UseTimerFilter = value;
                PersistCounterSettings();
            }
        }
    }

    /// <summary>Timer filter interval in seconds.</summary>
    public double TimerIntervalSeconds
    {
        get => _timerIntervalSeconds;
        set
        {
            if (SetProperty(ref _timerIntervalSeconds, value))
            {
                _settings.Counter.TimerIntervalSeconds = value;
                PersistCounterSettings();
            }
        }
    }

    /// <summary>
    /// Rebuilds the statistics from the configured feedback-point count and reapplies the latest counters.
    /// The collection is replaced instead of mutated so WinUI receives one PropertyChanged and no
    /// CollectionChanged during a binding update.
    /// </summary>
    public void InitializeStatisticsFromFeedbackPoints()
    {
        var count = _settings.Counter.CountOfFeedbackPoints;
        var list = new List<InPortStatistic>();
        if (count > 0)
        {
            for (var inPort = 1; inPort <= count; inPort++)
            {
                list.Add(new InPortStatistic(_runtimeCommandGateway)
                {
                    InPort = inPort,
                    Name = $"Feedback Point {inPort}",
                    Count = 0,
                    TargetLapCount = GlobalTargetLapCount
                });
            }
        }
        else
        {
            LogNoFeedbackPoints(_logger);
        }

        Statistics = new ObservableCollection<InPortStatistic>(list);
        ApplyInPortCounterSnapshot(_latestRuntimeSnapshot);
    }

    [RelayCommand]
    private async Task ResetCountersAsync()
    {
        try
        {
            await _runtimeCommandGateway.ResetInPortCountersAsync().ConfigureAwait(true);
            CounterCommandStatus = "InPort counters reset.";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            CounterCommandStatus = ex.Message;
            LogOperationFailed(_logger, "Resetting InPort counters", ex);
        }
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        if (_isDisposed)
        {
            return;
        }

        _latestRuntimeSnapshot = e.Snapshot;
        ApplyInPortCounterSnapshot(e.Snapshot);
    }

    private void OnFeedbackPointCountChanged(object? sender, EventArgs e) => InitializeStatisticsFromFeedbackPoints();

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            InitializeStatisticsFromFeedbackPoints();
        }
    }

    /// <summary>Projects the authoritative counters of the selected runtime onto the statistics.</summary>
    private void ApplyInPortCounterSnapshot(MobaRuntimeSnapshot snapshot)
    {
        CounterPersistenceError = snapshot.InPortCounterPersistenceError ?? string.Empty;
        foreach (var counter in snapshot.InPortCounters)
        {
            var statistic = Statistics.FirstOrDefault(candidate => candidate.InPort == counter.InPort);
            if (statistic == null)
            {
                continue;
            }

            statistic.Count = counter.Count;
            statistic.LastFeedbackTime = counter.LastFeedbackTime?.UtcDateTime;
            statistic.LastLapTime = counter.LastLapTime;
            statistic.HasReceivedFirstLap = counter.Count > 0;
        }

        foreach (var statistic in Statistics.Where(candidate => !snapshot.InPortCounters.Any(counter => counter.InPort == candidate.InPort)))
        {
            statistic.Count = 0;
            statistic.LastFeedbackTime = null;
            statistic.LastLapTime = null;
            statistic.HasReceivedFirstLap = false;
        }
    }

    private void PersistCounterSettings() =>
        _settingsService.SaveSettingsAsync(_settings)
            .Observe(ex => LogOperationFailed(_logger, "Persist counter settings", ex));

    /// <summary>Detaches the runtime, settings and session subscriptions; later snapshots are ignored.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _eventBus.Unsubscribe(_snapshotSubscription);
        _settingsPage.FeedbackPointCountChanged -= OnFeedbackPointCountChanged;
        _session.PropertyChanged -= OnSessionPropertyChanged;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CountOfFeedbackPoints is 0 - no track statistics initialized. Set CountOfFeedbackPoints in settings to enable")]
    private static partial void LogNoFeedbackPoints(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);
}
