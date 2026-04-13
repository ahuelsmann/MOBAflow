// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Moba.SharedUI.ViewModel;

using System.Collections.Specialized;
using System.Runtime.InteropServices;

// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class MonitorPage
{
    public MonitorPageViewModel ViewModel { get; }

    private GridLength _trafficExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _activityLogExpandedWidth = new(2, GridUnitType.Star);

    public MonitorPage(MonitorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // ✅ FIX: Use Loaded/Unloaded pattern to prevent memory leaks and NullReferenceException
        // Subscribe to page lifecycle events
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;

        // Ensure MainWindowViewModel isn't null if we are observing it directly, although here it is not directly observable 
        // We will need to observe MainWindowViewModel.PropertyChanged from ViewModel
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsMonitorTrafficExpanded))
        {
            if (!ViewModel.MainWindowViewModel.IsMonitorTrafficExpanded)
            {
                if (!ColTraffic.Width.IsAuto)
                {
                    _trafficExpandedWidth = ColTraffic.Width;
                }
                ColTraffic.Width = GridLength.Auto;
            }
            else
            {
                ColTraffic.Width = _trafficExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsMonitorActivityLogExpanded))
        {
            if (!ViewModel.MainWindowViewModel.IsMonitorActivityLogExpanded)
            {
                if (!ColLog.Width.IsAuto)
                {
                    _activityLogExpandedWidth = ColLog.Width;
                }
                ColLog.Width = GridLength.Auto;
            }
            else
            {
                ColLog.Width = _activityLogExpandedWidth;
            }
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Restore layout state initially
        if (!ViewModel.MainWindowViewModel.IsMonitorTrafficExpanded)
            ColTraffic.Width = GridLength.Auto;
        if (!ViewModel.MainWindowViewModel.IsMonitorActivityLogExpanded)
            ColLog.Width = GridLength.Auto;

        // Subscribe when page enters visual tree (DispatcherQueue is valid)
        ViewModel.TrafficPackets.CollectionChanged += OnTrafficPacketsChanged;
        ViewModel.ActivityLogs.CollectionChanged += OnActivityLogsChanged;
        ViewModel.MainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe when page leaves visual tree (prevents NullReferenceException)
        ViewModel.TrafficPackets.CollectionChanged -= OnTrafficPacketsChanged;
        ViewModel.ActivityLogs.CollectionChanged -= OnActivityLogsChanged;
        ViewModel.MainWindowViewModel.PropertyChanged -= MainWindowViewModel_PropertyChanged;
    }

    private void OnTrafficPacketsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;

        // Only auto-scroll if not paused
        if (ViewModel.IsTrafficScrollPaused) return;

        // When new items are added at the top (index 0), scroll to show them
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0)
        {
            // Defer ScrollIntoView to next UI cycle to avoid COMException during collection update
            DispatcherQueue?.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                // Guard against uninitialized ListView during page construction
                if (TrafficListView?.Items is null) return;

                var firstItem = TrafficListView.Items.FirstOrDefault();
                if (firstItem != null)
                {
                    try
                    {
                        TrafficListView.ScrollIntoView(firstItem);
                    }
                    catch (COMException)
                    {
                        // Ignore scroll failures during rapid updates
                    }
                }
            });
        }
    }

    private void OnActivityLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;

        // Only auto-scroll if not paused
        if (ViewModel.IsActivityLogScrollPaused) return;

        // When new items are added at the top (index 0), scroll to show them
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0)
        {
            // Defer ScrollIntoView to next UI cycle to avoid COMException during collection update
            DispatcherQueue?.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                // Guard against uninitialized ListView during page construction
                if (ActivityLogListView?.Items is null) return;

                var firstItem = ActivityLogListView.Items.FirstOrDefault();
                if (firstItem != null)
                {
                    try
                    {
                        ActivityLogListView.ScrollIntoView(firstItem);
                    }
                    catch (COMException)
                    {
                        // Ignore scroll failures during rapid updates
                    }
                }
            });
        }
    }
}