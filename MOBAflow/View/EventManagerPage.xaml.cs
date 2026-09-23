// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Controls;
using SharedUI.Interface;
using SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using System.Windows.Input;

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
    private double _eventPlanStarValue = 2;
    private double _workflowLibraryStarValue = 1;
    private double _propertiesStarValue = 1.2;
    private long _valuesExpansionToken;
    private long _propertiesExpansionToken;

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
        _eventPlanStarValue = ValidStarValue(layout.EventPlanColumnStarValue, 2);
        _workflowLibraryStarValue = ValidStarValue(layout.WorkflowLibraryColumnStarValue, 1);
        _propertiesStarValue = ValidStarValue(layout.PropertiesColumnStarValue, 1.2);
        ValuesPanel.IsExpanded = layout.IsValuesExpanded;
        PropertiesPanel.IsExpanded = layout.IsPropertiesExpanded;
        _valuesExpansionToken = ValuesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
        _propertiesExpansionToken = PropertiesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
        _isWide = false;
        _isLoaded = true;
        ApplyResponsiveLayout();
    }

    private static double ValidStarValue(double value, double fallback) => double.IsFinite(value) && value > 0 ? value : fallback;

    private void OnPanelExpansionChanged(DependencyObject sender, DependencyProperty property) => ApplyResponsiveLayout();

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoaded) ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        CaptureColumnWidths();
        _isWide = ActualWidth >= 900;
        ApplyJourneyHeaderLayout();
        ApplyEditorLayout();
    }

    private void ApplyJourneyHeaderLayout()
    {
        var narrowHeader = this.ActualWidth < 720;
        Grid.SetRow(JourneyCommands, narrowHeader ? 1 : 0);
        Grid.SetColumn(JourneyCommands, narrowHeader ? 0 : 1);
        Grid.SetColumnSpan(JourneyCommands, narrowHeader ? 2 : 1);
        JourneyCommands.HorizontalAlignment = narrowHeader ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        JourneyCommands.Margin = narrowHeader ? new Thickness(0, 12, 0, 0) : new Thickness(0);
    }

    private void CaptureColumnWidths()
    {
        if (!_isWide) return;
        if (EventPlanColumn.Width.IsStar) _eventPlanStarValue = EventPlanColumn.Width.Value;
        if (WorkflowLibraryColumn.Width.IsStar) _workflowLibraryStarValue = WorkflowLibraryColumn.Width.Value;
        if (PropertiesColumn.Width.IsStar) _propertiesStarValue = PropertiesColumn.Width.Value;
    }

    private void ApplyEditorLayout()
    {
        Grid.SetColumnSpan(PlanArea, _isWide ? 1 : 5);
        Grid.SetRow(ValuesPanel, _isWide ? 0 : 1);
        Grid.SetColumn(ValuesPanel, _isWide ? 2 : 0);
        Grid.SetColumnSpan(ValuesPanel, _isWide ? 1 : 3);
        Grid.SetRow(PropertiesPanel, _isWide ? 0 : 1);
        CompactPanelsRow.Height = _isWide ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        EventPlanColumn.Width = new GridLength(_isWide ? _eventPlanStarValue : 1, GridUnitType.Star);
        EventPlanColumn.MinWidth = _isWide ? 280 : 160;
        WorkflowLibraryColumn.MinWidth = _isWide ? 32 : 0;
        WorkflowLibraryColumn.Width = ValuesPanel.IsExpanded
            ? new GridLength(_workflowLibraryStarValue, GridUnitType.Star) : GridLength.Auto;
        if (!_isWide) WorkflowLibraryColumn.Width = new GridLength(0);
        var propertiesWidth = _isWide ? _propertiesStarValue : 1;
        PropertiesColumn.Width = PropertiesPanel.IsExpanded
            ? new GridLength(propertiesWidth, GridUnitType.Star) : GridLength.Auto;
        LibrarySplitter.Visibility = _isWide && ValuesPanel.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        PropertiesSplitter.Visibility = _isWide && ValuesPanel.IsExpanded && PropertiesPanel.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        ValuesPanel.UnregisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, _valuesExpansionToken);
        PropertiesPanel.UnregisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, _propertiesExpansionToken);
        ClearDropFeedback();
        SaveLayoutAsync().Observe(ex => _logger?.LogWarning(ex, "Persist Event Manager layout failed"));
    }

    private async Task SaveLayoutAsync()
    {
        var layout = _settings.Layout.EventManagerPage;
        CaptureColumnWidths();
        layout.IsValuesExpanded = ValuesPanel.IsExpanded;
        layout.IsPropertiesExpanded = PropertiesPanel.IsExpanded;
        layout.EventPlanColumnStarValue = _eventPlanStarValue;
        layout.WorkflowLibraryColumnStarValue = _workflowLibraryStarValue;
        layout.PropertiesColumnStarValue = _propertiesStarValue;
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

    private void EventRow_Tapped(object sender, TappedRoutedEventArgs e) => SelectEvent(sender);

    private void EventRow_GotFocus(object sender, RoutedEventArgs e) => SelectEvent(sender);

    private void EventRow_ContextRequested(UIElement sender, ContextRequestedEventArgs e) => SelectEvent(sender);

    private void EventActions_Click(object sender, RoutedEventArgs e) => SelectEvent(sender);

    private void SelectEvent(object sender)
    {
        if (sender is FrameworkElement { Tag: JourneyEventViewModel item } && ViewModel.ContainsEvent(item))
            ViewModel.SelectedEvent = item;
    }

    private void DeleteEvent_Click(object sender, RoutedEventArgs e)
    {
        SelectEvent(sender);
        ViewModel.DeleteSelectedEventCommand.Execute(null);
    }

    private void DuplicateEvent_Click(object sender, RoutedEventArgs e)
    {
        SelectEvent(sender);
        ViewModel.DuplicateSelectedEventCommand.Execute(null);
    }

    private void EventsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        AutomationProperties.SetPositionInSet(args.Element, args.Index + 1);
        AutomationProperties.SetSizeOfSet(args.Element, ViewModel.Events.Count);
    }

    private void EventsRepeater_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Row shortcuts must not consume input from property editors or action buttons.
        if (FocusManager.GetFocusedElement(XamlRoot) is not ListViewItem { Tag: JourneyEventViewModel item }) return;
        var index = ViewModel.Events.IndexOf(item);
        if (index < 0) return;
        ViewModel.SelectedEvent = item;
        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
        var command = GetRowCommand(e.Key, alt, IsControlDown(), item);
        if (command != null)
        {
            if (command.CanExecute(null)) command.Execute(null);
        }
        else if (e.Key is not (VirtualKey.Enter or VirtualKey.Space) && !TryNavigate(e.Key, index)) return;
        e.Handled = true;
        FocusSelectedEvent();
    }

    private ICommand? GetRowCommand(VirtualKey key, bool alt, bool control, JourneyEventViewModel item) => (key, alt, control) switch
    {
        (VirtualKey.Up, true, _) => ViewModel.MoveSelectedEventUpCommand,
        (VirtualKey.Down, true, _) => ViewModel.MoveSelectedEventDownCommand,
        (VirtualKey.D, _, true) => ViewModel.DuplicateSelectedEventCommand,
        (VirtualKey.Delete, _, true) => item.RemoveWorkflowCommand,
        (VirtualKey.Delete, _, false) => ViewModel.DeleteSelectedEventCommand,
        _ => null
    };

    private bool TryNavigate(VirtualKey key, int index)
    {
        var targetIndex = key switch
        {
            VirtualKey.Up => Math.Max(0, index - 1),
            VirtualKey.Down => Math.Min(ViewModel.Events.Count - 1, index + 1),
            VirtualKey.Home => 0,
            VirtualKey.End => ViewModel.Events.Count - 1,
            _ => -1
        };
        if (targetIndex < 0) return false;
        ViewModel.SelectedEvent = ViewModel.Events[targetIndex];
        return true;
    }

    private void FocusSelectedEvent()
    {
        var index = ViewModel.SelectedEvent == null ? -1 : ViewModel.Events.IndexOf(ViewModel.SelectedEvent);
        if (index < 0) return;
        var row = EventsRepeater.GetOrCreateElement(index);
        EventsRepeater.UpdateLayout();
        if (row is Control control) control.Focus(FocusState.Keyboard);
        row.StartBringIntoView();
    }
}
