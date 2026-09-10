// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SharedUI.Interface;
using SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

internal sealed partial class EventManagerPage
{
    private const string WorkflowFormat = "Workflow";
    private const string EventFormat = "JourneyEvent";
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<EventManagerPage>? _logger;
    private bool _isLoaded;
    private bool _isWide;
    private Border? _rowDropIndicator;
    private double _workflowLibraryStarValue = 1;

    public EventManagerPage(EventManagerViewModel viewModel, AppSettings settings,
        ISettingsService? settingsService = null, ILogger<EventManagerPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnPageSizeChanged;
    }

    public EventManagerViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var layout = _settings.Layout.EventManagerPage;
        EventPlanColumn.Width = new GridLength(ValidStarValue(layout.EventPlanColumnStarValue, 2), GridUnitType.Star);
        _workflowLibraryStarValue = ValidStarValue(layout.WorkflowLibraryColumnStarValue, 1);
        WorkflowLibraryToggle.IsChecked = layout.IsValuesExpanded;
        _isLoaded = true;
        ApplyResponsiveLayout();
    }

    private static double ValidStarValue(double value, double fallback) => double.IsFinite(value) && value > 0 ? value : fallback;

    private void WorkflowLibraryToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        if (WorkflowLibraryColumn.Width.IsStar) _workflowLibraryStarValue = WorkflowLibraryColumn.Width.Value;
        ApplyResponsiveLayout();
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoaded) ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        var isWide = ActualWidth >= 900;
        var showLibrary = WorkflowLibraryToggle.IsChecked == true;
        if (_isWide && WorkflowLibraryColumn.Width.IsStar)
            _workflowLibraryStarValue = WorkflowLibraryColumn.Width.Value;
        _isWide = isWide;

        var narrowHeader = ActualWidth < 720;
        Grid.SetRow(JourneyCommands, narrowHeader ? 1 : 0);
        Grid.SetColumn(JourneyCommands, narrowHeader ? 0 : 1);
        Grid.SetColumnSpan(JourneyCommands, narrowHeader ? 2 : 1);
        JourneyCommands.HorizontalAlignment = narrowHeader ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        JourneyCommands.Margin = narrowHeader ? new Thickness(0, 12, 0, 0) : new Thickness(0);

        Grid.SetColumnSpan(PlanArea, isWide && showLibrary ? 1 : 3);
        Grid.SetRow(WorkflowLibraryPanel, isWide ? 0 : 1);
        Grid.SetColumn(WorkflowLibraryPanel, isWide ? 2 : 0);
        Grid.SetColumnSpan(WorkflowLibraryPanel, isWide ? 1 : 3);
        WorkflowLibraryPanel.Visibility = showLibrary ? Visibility.Visible : Visibility.Collapsed;
        // Keep room for the plan in short windows; the compact library scrolls as a whole.
        WorkflowLibraryPanel.MaxHeight = isWide ? double.PositiveInfinity : Math.Min(240, EditorColumns.ActualHeight * 0.45);
        WorkflowLibraryScroller.VerticalScrollBarVisibility = isWide ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        LibrarySplitter.Visibility = isWide && showLibrary ? Visibility.Visible : Visibility.Collapsed;
        WorkflowLibraryColumn.MinWidth = isWide && showLibrary ? 240 : 0;
        WorkflowLibraryColumn.Width = isWide && showLibrary
            ? new GridLength(_workflowLibraryStarValue, GridUnitType.Star) : new GridLength(0);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        ClearDropFeedback();
        SaveLayoutAsync().Observe(ex => _logger?.LogWarning(ex, "Persist Event Manager layout failed"));
    }

    private async Task SaveLayoutAsync()
    {
        var layout = _settings.Layout.EventManagerPage;
        layout.IsValuesExpanded = WorkflowLibraryToggle.IsChecked == true;
        if (EventPlanColumn.Width.IsStar) layout.EventPlanColumnStarValue = EventPlanColumn.Width.Value;
        if (WorkflowLibraryColumn.Width.IsStar) _workflowLibraryStarValue = WorkflowLibraryColumn.Width.Value;
        layout.WorkflowLibraryColumnStarValue = _workflowLibraryStarValue;
        if (_settingsService != null) await _settingsService.SaveSettingsAsync(_settings);
    }

    private void WorkflowValues_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not WorkflowViewModel workflow || !ViewModel.CanEdit)
        {
            e.Cancel = true;
            return;
        }
        e.Data.Properties[WorkflowFormat] = workflow;
        e.Data.RequestedOperation = DataPackageOperation.Link;
        e.Data.SetText(workflow.Name);
    }

    private void WorkflowValues_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (WorkflowValuesList.SelectedItem is WorkflowViewModel workflow)
            ViewModel.SelectedEvent?.AssignWorkflowCommand.Execute(workflow);
    }

    private void EventHandle_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not JourneyEventViewModel item || !ViewModel.CanEdit)
        {
            e.Cancel = true;
            return;
        }
        ViewModel.SelectedEvent = item;
        e.Data.Properties[EventFormat] = item;
        e.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
        e.Data.SetText(item.AutomationName);
    }

    private void EventRow_DragOver(object sender, DragEventArgs e)
    {
        ShowDropFeedback(e, true);
        ClearDropFeedback();
        if (e.AcceptedOperation != DataPackageOperation.None && sender is FrameworkElement row
            && row.FindName("RowDropIndicator") is Border indicator)
        {
            _rowDropIndicator = indicator;
            indicator.Opacity = 1;
        }
    }

    private void Plan_DragOver(object sender, DragEventArgs e)
    {
        ShowDropFeedback(e, false);
        ClearDropFeedback();
        if (e.AcceptedOperation != DataPackageOperation.None) AddDropIndicator.Opacity = 1;
    }

    private void EventRow_DragLeave(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ClearDropFeedback();
    }

    private void Plan_DragLeave(object sender, DragEventArgs e) => ClearDropFeedback();

    private void ClearDropFeedback()
    {
        if (_rowDropIndicator != null) _rowDropIndicator.Opacity = 0;
        _rowDropIndicator = null;
        AddDropIndicator.Opacity = 0;
    }

    private void ShowDropFeedback(DragEventArgs e, bool onRow)
    {
        e.Handled = true;
        e.AcceptedOperation = DataPackageOperation.None;
        e.DragUIOverride.IsCaptionVisible = false;
        if (!ViewModel.CanEdit) return;
        if (GetWorkflow(e) != null)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = onRow ? "Assign workflow to event" : "Add event with this workflow";
        }
        else if (GetEvent(e) != null)
        {
            e.AcceptedOperation = IsControlDown() ? DataPackageOperation.Copy : DataPackageOperation.Move;
            e.DragUIOverride.Caption = IsControlDown() ? "Copy event here" : "Move event here";
        }
        e.DragUIOverride.IsCaptionVisible = e.AcceptedOperation != DataPackageOperation.None;
    }

    private WorkflowViewModel? GetWorkflow(DragEventArgs e) =>
        e.DataView.Properties.TryGetValue(WorkflowFormat, out var value)
        && value is WorkflowViewModel workflow && ViewModel.IsAvailableWorkflow(workflow) ? workflow : null;

    private JourneyEventViewModel? GetEvent(DragEventArgs e) =>
        e.DataView.Properties.TryGetValue(EventFormat, out var value)
        && value is JourneyEventViewModel item && ViewModel.ContainsEvent(item) ? item : null;

    private void EventRow_Drop(object sender, DragEventArgs e)
    {
        ClearDropFeedback();
        e.Handled = true;
        if (!ViewModel.CanEdit || sender is not FrameworkElement row || row.Tag is not JourneyEventViewModel target) return;
        if (GetWorkflow(e) is { } workflow)
        {
            ViewModel.SelectedEvent = target;
            target.AssignWorkflowCommand.Execute(workflow);
        }
        else if (GetEvent(e) is { } item)
        {
            var index = ViewModel.Events.IndexOf(target);
            if (e.GetPosition(row).Y >= row.ActualHeight / 2) index++;
            ViewModel.MoveOrCopyEvent(item, index, IsControlDown());
        }
    }

    private void Plan_Drop(object sender, DragEventArgs e)
    {
        ClearDropFeedback();
        e.Handled = true;
        if (!ViewModel.CanEdit) return;
        if (GetWorkflow(e) is { } workflow) ViewModel.InsertEvent(workflow, ViewModel.Events.Count);
        else if (GetEvent(e) is { } item) ViewModel.MoveOrCopyEvent(item, ViewModel.Events.Count, IsControlDown());
    }

    private static bool IsControlDown() => InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
        .HasFlag(CoreVirtualKeyStates.Down);

    private void EventList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (IsEditingInput(e.OriginalSource as DependencyObject)) return;
        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
        if (alt && e.Key == VirtualKey.Up) ViewModel.MoveSelectedEventUpCommand.Execute(null);
        else if (alt && e.Key == VirtualKey.Down) ViewModel.MoveSelectedEventDownCommand.Execute(null);
        else if (IsControlDown() && e.Key == VirtualKey.D) ViewModel.DuplicateSelectedEventCommand.Execute(null);
        else if (e.Key == VirtualKey.Delete) ViewModel.DeleteSelectedEventCommand.Execute(null);
        else return;
        e.Handled = true;
    }

    private static bool IsEditingInput(DependencyObject? source)
    {
        for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
            if (current is TextBox or NumberBox or ComboBox) return true;
        return false;
    }
}
