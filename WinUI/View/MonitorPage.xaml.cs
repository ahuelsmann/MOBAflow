// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

using System.Collections.Specialized;

// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class MonitorPage
{
    public MonitorPageViewModel ViewModel { get; }

    public MonitorPage(MonitorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Subscribe to TrafficPackets changes to auto-scroll to top
        ViewModel.TrafficPackets.CollectionChanged += OnTrafficPacketsChanged;

        // Subscribe to ActivityLogs changes to auto-scroll to top
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
            // Scroll to the first item (newest) - use FirstOrDefault for safety
            var firstItem = TrafficListView.Items.FirstOrDefault();
            if (firstItem != null)
            {
                TrafficListView.ScrollIntoView(firstItem);
            }
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
            // Scroll to the first item (newest) - use FirstOrDefault for safety
            var firstItem = ActivityLogListView.Items.FirstOrDefault();
            if (firstItem != null)
            {
                ActivityLogListView.ScrollIntoView(firstItem);
            }
        }
    }
}