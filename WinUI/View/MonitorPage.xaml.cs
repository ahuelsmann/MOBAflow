// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Dispatching;
using SharedUI.ViewModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class MonitorPage
{
    public MonitorPageViewModel ViewModel { get; }

    public MonitorPage(MonitorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Subscribe to collection changes
        // Safe: ViewModel is Transient (same lifetime as Page)
        ViewModel.TrafficPackets.CollectionChanged += OnTrafficPacketsChanged;
        ViewModel.ActivityLogs.CollectionChanged += OnActivityLogsChanged;
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
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
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
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
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