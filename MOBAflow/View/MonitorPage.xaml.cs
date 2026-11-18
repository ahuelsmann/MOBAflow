// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Moba.SharedUI.ViewModel;

using SharedUI.Service;

using System.Collections.Specialized;
using System.Runtime.InteropServices;

// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class MonitorPage
{
    private readonly LayoutPersistenceService _layout;
    private readonly ILogger<MonitorPage> _logger;

    public MonitorPageViewModel ViewModel { get; }

    private double _trafficExpandedStarValue = 1;
    private double _activityLogExpandedStarValue = 2;

    public MonitorPage(
        MonitorPageViewModel viewModel,
        LayoutPersistenceService layout,
        ILogger<MonitorPage> logger)
    {
        ViewModel = viewModel;
        _layout = layout;
        _logger = logger;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitorPageViewModel.IsMonitorTrafficExpanded))
        {
            if (!ViewModel.IsMonitorTrafficExpanded)
            {
                if (ColTraffic.Width.IsStar)
                {
                    _trafficExpandedStarValue = ColTraffic.Width.Value;
                }
                ColTraffic.Width = GridLength.Auto;
            }
            else
            {
                ColTraffic.Width = new GridLength(_trafficExpandedStarValue, GridUnitType.Star);
            }
        }
        else if (e.PropertyName == nameof(MonitorPageViewModel.IsMonitorActivityLogExpanded))
        {
            if (!ViewModel.IsMonitorActivityLogExpanded)
            {
                if (ColLog.Width.IsStar)
                {
                    _activityLogExpandedStarValue = ColLog.Width.Value;
                }
                ColLog.Width = GridLength.Auto;
            }
            else
            {
                ColLog.Width = new GridLength(_activityLogExpandedStarValue, GridUnitType.Star);
            }
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RestoreLayout();

        ViewModel.TrafficPackets.CollectionChanged += OnTrafficPacketsChanged;
        ViewModel.ActivityLogs.CollectionChanged += OnActivityLogsChanged;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        HandlePageUnloadedAsync().Observe(ex => _logger.LogWarning(ex, "Persist layout on unload failed"));
        ViewModel.TrafficPackets.CollectionChanged -= OnTrafficPacketsChanged;
        ViewModel.ActivityLogs.CollectionChanged -= OnActivityLogsChanged;
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveLayout();
            await _layout.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void RestoreLayout()
    {
        var layout = _layout.Layout.MonitorPage;

        if (layout.TrafficColumnStarValue > 0)
        {
            _trafficExpandedStarValue = layout.TrafficColumnStarValue;
        }
        if (layout.ActivityLogColumnStarValue > 0)
        {
            _activityLogExpandedStarValue = layout.ActivityLogColumnStarValue;
        }

        if (layout.IsTrafficExpanded)
        {
            ColTraffic.Width = new GridLength(_trafficExpandedStarValue, GridUnitType.Star);
        }
        else
        {
            ColTraffic.Width = GridLength.Auto;
        }

        if (layout.IsActivityLogExpanded)
        {
            ColLog.Width = new GridLength(_activityLogExpandedStarValue, GridUnitType.Star);
        }
        else
        {
            ColLog.Width = GridLength.Auto;
        }

        if (ViewModel.IsMonitorTrafficExpanded != layout.IsTrafficExpanded)
        {
            ViewModel.IsMonitorTrafficExpanded = layout.IsTrafficExpanded;
        }
        if (ViewModel.IsMonitorActivityLogExpanded != layout.IsActivityLogExpanded)
        {
            ViewModel.IsMonitorActivityLogExpanded = layout.IsActivityLogExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _layout.Layout.MonitorPage;

        layout.IsTrafficExpanded = ViewModel.IsMonitorTrafficExpanded;
        layout.IsActivityLogExpanded = ViewModel.IsMonitorActivityLogExpanded;

        if (ColTraffic.Width.IsStar)
        {
            layout.TrafficColumnStarValue = ColTraffic.Width.Value;
        }
        else if (!ViewModel.IsMonitorTrafficExpanded)
        {
            layout.TrafficColumnStarValue = _trafficExpandedStarValue;
        }

        if (ColLog.Width.IsStar)
        {
            layout.ActivityLogColumnStarValue = ColLog.Width.Value;
        }
        else if (!ViewModel.IsMonitorActivityLogExpanded)
        {
            layout.ActivityLogColumnStarValue = _activityLogExpandedStarValue;
        }
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