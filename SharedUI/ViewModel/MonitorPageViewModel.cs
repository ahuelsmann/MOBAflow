// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using Common.Serilog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for MonitorPage - displays Z21 traffic and application logs.
/// Left panel: Z21 UDP traffic (sent/received packets)
/// Right panel: Application logs (from Serilog InMemorySink)
/// </summary>
public partial class MonitorPageViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MonitorPageViewModel> _logger;

    /// <summary>
    /// Application log entries formatted as strings for UI display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> activityLogs = [];

    /// <summary>
    /// Indicates whether auto-scroll is paused for Traffic Monitor.
    /// True = User can scroll manually, False = Auto-scroll to newest packet.
    /// </summary>
    [ObservableProperty]
    private bool isTrafficScrollPaused;

    /// <summary>
    /// Indicates whether auto-scroll is paused for Application Log.
    /// True = User can scroll manually, False = Auto-scroll to newest log.
    /// </summary>
    [ObservableProperty]
    private bool isActivityLogScrollPaused;

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

    public MonitorPageViewModel(MainWindowViewModel mainWindowViewModel, IUiDispatcher uiDispatcher, ILogger<MonitorPageViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _uiDispatcher = uiDispatcher;
        _logger = logger;

        // Subscribe to TrafficPackets changes to update count
        TrafficPackets.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TrafficCount));
        };

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
        // Ensure UI updates happen on UI thread
        _uiDispatcher.InvokeOnUi(() =>
        {
            ActivityLogs.Insert(0, FormatLogEntry(entry)); // Add to top (newest first)

            // Keep only last 200 logs in UI
            while (ActivityLogs.Count > 200)
            {
                ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
            }
        });
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
}

























