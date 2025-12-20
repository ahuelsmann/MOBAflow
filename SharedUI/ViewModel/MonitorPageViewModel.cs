// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;
using Common.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for MonitorPage - displays Z21 traffic and application logs.
/// Left panel: Z21 UDP traffic (sent/received packets)
/// Right panel: Application logs (from LoggingExtensions)
/// </summary>
public partial class MonitorPageViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IUiDispatcher _uiDispatcher;

    /// <summary>
    /// Application log entries formatted as strings for UI display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> activityLogs = [];

    /// <summary>
    /// Z21 UDP traffic packets from MainWindowViewModel.
    /// </summary>
    public ObservableCollection<Z21TrafficPacket> TrafficPackets => _mainWindowViewModel.TrafficPackets;

    /// <summary>
    /// Connection status text.
    /// </summary>
    public string ConnectionStatus => _mainWindowViewModel.IsZ21Connected 
        ? $"✅ Connected to {_mainWindowViewModel.Z21IpAddress}" 
        : "❌ Not connected";

    /// <summary>
    /// Number of traffic packets.
    /// </summary>
    public int TrafficCount => TrafficPackets.Count;

    public MonitorPageViewModel(MainWindowViewModel mainWindowViewModel, IUiDispatcher uiDispatcher)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _uiDispatcher = uiDispatcher;

        // Subscribe to TrafficPackets changes to update count
        TrafficPackets.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TrafficCount));
        };

        // Subscribe to connection status changes
        _mainWindowViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsZ21Connected))
            {
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        };

        // Load existing log entries
        foreach (var entry in LoggingExtensions.GetLogEntries())
        {
            ActivityLogs.Add(FormatLogEntry(entry));
        }

        // Subscribe to new log entries from LoggingExtensions
        LoggingExtensions.LogAdded += OnLogAdded;

        // Add initial log entry
        this.Log("Monitor started");
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
        LoggingExtensions.ClearLogs();
        this.Log("Logs cleared");
    }

        [RelayCommand]
        private void ClearTraffic()
        {
            _mainWindowViewModel.ClearTrafficMonitorCommand.Execute(null);
            this.Log("Traffic cleared");
        }
    }
