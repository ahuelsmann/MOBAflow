// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Events;
using Backend.Interface;
using Backend.Model;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Serilog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// ViewModel for MonitorPage - displays Z21 traffic and application logs.
/// Left panel: Z21 UDP traffic of the selected project's runtime (sent/received packets)
/// Right panel: Application logs (from Serilog InMemorySink)
/// </summary>
public sealed partial class MonitorPageViewModel : ObservableObject, IDisposable
{
    private const int MaxTrafficPackets = 100;
    private readonly ITrafficMonitor _trafficMonitor;
    private readonly IEventBus _eventBus;
    private readonly ISolutionSession _session;
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MonitorPageViewModel> _logger;
    private readonly List<Guid> _eventBusSubscriptions = [];
    private readonly Queue<string> _pendingLogEntries = new();
    private readonly object _logBatchLock = new();
    private CancellationTokenSource? _batchUpdateCts;
    private const int BatchUpdateDelayMs = 50;
    private bool _isConnected;
    private bool _isMonitorTrafficExpanded;
    private bool _isMonitorActivityLogExpanded;
    private bool _isDisposed;

    /// <summary>
    /// Application log entries formatted as strings for UI display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _activityLogs = [];

    /// <summary>
    /// Indicates whether auto-scroll is paused for Traffic Monitor.
    /// True = User can scroll manually, False = Auto-scroll to newest packet.
    /// </summary>
    [ObservableProperty]
    private bool _isTrafficScrollPaused;

    /// <summary>
    /// Indicates whether auto-scroll is paused for Application Log.
    /// True = User can scroll manually, False = Auto-scroll to newest log.
    /// </summary>
    [ObservableProperty]
    private bool _isActivityLogScrollPaused;

    /// <summary>
    /// Z21 UDP traffic packets of the selected project's runtime, newest first.
    /// </summary>
    public ObservableCollection<Z21TrafficPacket> TrafficPackets { get; } = [];

    /// <summary>
    /// Connection status text naming the Z21 of the selected project.
    /// </summary>
    public string ConnectionStatus => _isConnected
        ? $"✅ Connected to {_session.SelectedProject?.Z21IpAddress}"
        : "❌ Not connected";

    /// <summary>
    /// Number of traffic packets.
    /// </summary>
    public int TrafficCount => TrafficPackets.Count;

    /// <summary>Gets or sets whether the traffic column is expanded; persisted under the Monitor layout keys.</summary>
    public bool IsMonitorTrafficExpanded
    {
        get => _isMonitorTrafficExpanded;
        set
        {
            if (SetProperty(ref _isMonitorTrafficExpanded, value))
                PersistLayoutState(layout => layout.MonitorPage.IsTrafficExpanded = value);
        }
    }

    /// <summary>Gets or sets whether the application-log column is expanded; persisted under the Monitor layout keys.</summary>
    public bool IsMonitorActivityLogExpanded
    {
        get => _isMonitorActivityLogExpanded;
        set
        {
            if (SetProperty(ref _isMonitorActivityLogExpanded, value))
                PersistLayoutState(layout => layout.MonitorPage.IsActivityLogExpanded = value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorPageViewModel"/> class for the Monitor page.
    /// </summary>
    /// <param name="trafficMonitor">Traffic recorded by the selected project's runtime.</param>
    /// <param name="runtimeSnapshots">Read access to the selected runtime's current snapshot.</param>
    /// <param name="eventBus">Delivers traffic packets, runtime selection and snapshot changes on the UI thread.</param>
    /// <param name="session">Owns the project selection whose Z21 the status names.</param>
    /// <param name="settingsService">Persists the Monitor layout.</param>
    /// <param name="uiDispatcher">Dispatcher used to update UI-bound collections on the UI thread.</param>
    /// <param name="logger">Logger used to write diagnostic log entries.</param>
    public MonitorPageViewModel(
        ITrafficMonitor trafficMonitor,
        IRuntimeSnapshotProvider runtimeSnapshots,
        IEventBus eventBus,
        ISolutionSession session,
        ISettingsService settingsService,
        IUiDispatcher uiDispatcher,
        ILogger<MonitorPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(trafficMonitor);
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(logger);
        _trafficMonitor = trafficMonitor;
        _eventBus = eventBus;
        _session = session;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        _isMonitorTrafficExpanded = _settings.Layout.MonitorPage.IsTrafficExpanded;
        _isMonitorActivityLogExpanded = _settings.Layout.MonitorPage.IsActivityLogExpanded;

        // Subscribe to TrafficPackets changes to update count
        TrafficPackets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TrafficCount));

        _isConnected = RuntimeSnapshotProjector.ProjectStatus(runtimeSnapshots.Current).IsConnected;
        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<Z21TrafficPacketLoggedEvent>(OnTrafficPacketLogged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<SelectedRuntimeChangedEvent>(_ => ReloadTrafficPackets()));
        ReloadTrafficPackets();

        // The status names the Z21 of the selected project, so it follows the selection as well.
        _session.PropertyChanged += OnSessionPropertyChanged;

        // Load existing log entries from Serilog InMemorySink
        foreach (var entry in InMemorySink.GetLogEntries())
        {
            ActivityLogs.Add(FormatLogEntry(entry));
        }

        // Subscribe to new log entries from Serilog InMemorySink
        InMemorySink.LogAdded += OnLogAdded;

        // Add initial log entry using Serilog
        _logger.LogInformation("Monitor started");
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        if (_isDisposed)
        {
            return;
        }

        var isConnected = RuntimeSnapshotProjector.ProjectStatus(e.Snapshot).IsConnected;
        if (isConnected != _isConnected)
        {
            _isConnected = isConnected;
            OnPropertyChanged(nameof(ConnectionStatus));
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            OnPropertyChanged(nameof(ConnectionStatus));
        }
    }

    /// <summary>Shows the traffic the runtime of the selected project has recorded.</summary>
    private void ReloadTrafficPackets()
    {
        if (_isDisposed)
        {
            return;
        }

        TrafficPackets.Clear();
        foreach (var packet in _trafficMonitor.GetTrafficPackets())
        {
            TrafficPackets.Add(packet);
        }
    }

    private void OnTrafficPacketLogged(Z21TrafficPacketLoggedEvent e)
    {
        if (_isDisposed)
        {
            return;
        }

        TrafficPackets.Insert(0, e.Packet);

        while (TrafficPackets.Count > MaxTrafficPackets)
        {
            TrafficPackets.RemoveAt(TrafficPackets.Count - 1);
        }
    }

    private void OnLogAdded(LogEntry entry)
    {
        lock (_logBatchLock)
        {
            _pendingLogEntries.Enqueue(FormatLogEntry(entry));

            // Cancel existing batch update timer
            _batchUpdateCts?.Cancel();
            _batchUpdateCts = new CancellationTokenSource();

            // Schedule batch update after delay
            ScheduleBatchUpdateAsync(_batchUpdateCts.Token).Observe(
                ex => _logger.LogWarning(ex, "Batch log update failed"));
        }
    }

    private async Task ScheduleBatchUpdateAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(BatchUpdateDelayMs, ct);

            List<string> entriesToAdd;
            lock (_logBatchLock)
            {
                if (_pendingLogEntries.Count == 0)
                    return;

                entriesToAdd = new List<string>(_pendingLogEntries);
                _pendingLogEntries.Clear();
            }

            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                foreach (var entry in entriesToAdd)
                {
                    ActivityLogs.Insert(0, entry);
                }

                while (ActivityLogs.Count > 200)
                {
                    ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
                }

                return Task.CompletedTask;
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when batch is rescheduled
        }
    }

    private static string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.TimestampFormatted}] {entry.SeverityIcon} [{entry.Source}] {entry.Message}";
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        _settingsService.SaveSettingsAsync(_settings)
            .Observe(ex => LogOperationFailed(_logger, "Persist Monitor layout", ex));
    }

    [RelayCommand]
    private void ClearActivityLogs()
    {
        ActivityLogs.Clear();
        InMemorySink.ClearLogs();
        _logger.LogInformation("Logs cleared");
    }

    /// <summary>Clears the displayed traffic and the runtime's recorded traffic.</summary>
    [RelayCommand]
    private void ClearTraffic()
    {
        TrafficPackets.Clear();
        _trafficMonitor.ClearTrafficMonitor();
        _logger.LogInformation("Traffic cleared");
    }

    /// <summary>
    /// Releases the event bus, session and log sink subscriptions and the batch timer; late packets are dropped.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var subscriptionId in _eventBusSubscriptions)
        {
            _eventBus.Unsubscribe(subscriptionId);
        }
        _eventBusSubscriptions.Clear();
        _session.PropertyChanged -= OnSessionPropertyChanged;
        InMemorySink.LogAdded -= OnLogAdded;
        _batchUpdateCts?.Cancel();
        _batchUpdateCts?.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);
}
