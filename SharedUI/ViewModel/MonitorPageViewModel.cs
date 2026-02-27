// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using Common.Serilog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

/// <summary>
/// ViewModel for MonitorPage - displays Z21 traffic and application logs.
/// Left panel: Z21 UDP traffic (sent/received packets)
/// Right panel: Application logs (from Serilog InMemorySink)
/// </summary>
public sealed partial class MonitorPageViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MonitorPageViewModel> _logger;
    private readonly Queue<string> _pendingLogEntries = new();
    private readonly object _logBatchLock = new();
    private CancellationTokenSource? _batchUpdateCts;
    private const int BatchUpdateDelayMs = 50;
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
    /// Z21 UDP traffic packets from MainWindowViewModel.
    /// </summary>
    public ObservableCollection<Z21TrafficPacket> TrafficPackets => _mainWindowViewModel.TrafficPackets;

    /// <summary>
    /// Connection status text.
    /// </summary>
    public string ConnectionStatus => _mainWindowViewModel.IsConnected
        ? $"✅ Connected to {_mainWindowViewModel.IpAddress}"
        : "❌ Not connected";

    /// <summary>
    /// Number of traffic packets.
    /// </summary>
    public int TrafficCount => TrafficPackets.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorPageViewModel"/> class for the Monitor page.
    /// </summary>
    /// <param name="mainWindowViewModel">The main window ViewModel providing traffic packets and connection state.</param>
    /// <param name="uiDispatcher">Dispatcher used to update UI-bound collections on the UI thread.</param>
    /// <param name="logger">Logger used to write diagnostic log entries.</param>
    public MonitorPageViewModel(MainWindowViewModel mainWindowViewModel, IUiDispatcher uiDispatcher, ILogger<MonitorPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(mainWindowViewModel);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(logger);
        _mainWindowViewModel = mainWindowViewModel;
        _uiDispatcher = uiDispatcher;
        _logger = logger;

        // Subscribe to TrafficPackets changes to update count
        TrafficPackets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TrafficCount));

        // Subscribe to connection status changes
        _mainWindowViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsConnected))
            {
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        };

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

    private void OnLogAdded(LogEntry entry)
    {
        lock (_logBatchLock)
        {
            _pendingLogEntries.Enqueue(FormatLogEntry(entry));

            // Cancel existing batch update timer
            _batchUpdateCts?.Cancel();
            _batchUpdateCts = new CancellationTokenSource();

            // Schedule batch update after delay
            _ = ScheduleBatchUpdateAsync(_batchUpdateCts.Token);
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

    [RelayCommand]
    private void ClearActivityLogs()
    {
        ActivityLogs.Clear();
        InMemorySink.ClearLogs();
        _logger.LogInformation("Logs cleared");
    }

    [RelayCommand]
    private void ClearTraffic()
    {
        _mainWindowViewModel.ClearTrafficMonitorCommand.Execute(null);
        _logger.LogInformation("Traffic cleared");
    }

    /// <summary>
    /// Releases event subscriptions and timers associated with this ViewModel instance.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        InMemorySink.LogAdded -= OnLogAdded;
        _batchUpdateCts?.Cancel();
        _batchUpdateCts?.Dispose();
    }
}