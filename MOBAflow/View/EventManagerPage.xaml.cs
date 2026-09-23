// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.Interface;
using SharedUI.ViewModel;
using Moba.WinUI.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

internal sealed partial class EventManagerPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<EventManagerPage>? _logger;
    private double _toolboxWidth;
    private double _valuesWidth;
    private double _propertiesWidth;

    public EventManagerPage(EventManagerViewModel viewModel, AppSettings settings, ISettingsService? settingsService = null, ILogger<EventManagerPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public EventManagerViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var layout = _settings.Layout.EventManagerPage;
        _toolboxWidth = layout.ToolboxColumnWidth;
        _valuesWidth = layout.ValuesColumnWidth;
        _propertiesWidth = layout.PropertiesColumnWidth;
        ToolboxPanel.IsExpanded = layout.IsToolboxExpanded;
        ValuesPanel.IsExpanded = layout.IsValuesExpanded;
        PropertiesPanel.IsExpanded = layout.IsPropertiesExpanded;
        ApplyPanelWidths();
        ToolboxPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
        ValuesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
        PropertiesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
    }

    private void OnPanelExpansionChanged(DependencyObject sender, DependencyProperty property) => ApplyPanelWidths();

    private void ApplyPanelWidths()
    {
        ToolboxColumn.Width = ToolboxPanel.IsExpanded ? new GridLength(_toolboxWidth) : GridLength.Auto;
        ValuesColumn.Width = ValuesPanel.IsExpanded ? new GridLength(_valuesWidth) : GridLength.Auto;
        PropertiesColumn.Width = PropertiesPanel.IsExpanded ? new GridLength(_propertiesWidth) : GridLength.Auto;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => SaveLayoutAsync().Observe(ex => _logger?.LogWarning(ex, "Persist Event Manager layout failed"));

    private async Task SaveLayoutAsync()
    {
        var layout = _settings.Layout.EventManagerPage;
        layout.IsToolboxExpanded = ToolboxPanel.IsExpanded;
        layout.IsValuesExpanded = ValuesPanel.IsExpanded;
        layout.IsPropertiesExpanded = PropertiesPanel.IsExpanded;
        if (ToolboxColumn.Width.IsAbsolute) layout.ToolboxColumnWidth = ToolboxColumn.Width.Value;
        if (ValuesColumn.Width.IsAbsolute) layout.ValuesColumnWidth = ValuesColumn.Width.Value;
        if (PropertiesColumn.Width.IsAbsolute) layout.PropertiesColumnWidth = PropertiesColumn.Width.Value;
        if (_settingsService != null) await _settingsService.SaveSettingsAsync(_settings);
    }

    private void ToolboxList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not EventElementDescriptor descriptor) return;
        e.Data.Properties["EventElement"] = descriptor;
        e.Data.RequestedOperation = DataPackageOperation.Copy;
        e.Data.SetText(descriptor.Name);
    }

    private void ToolboxList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ToolboxList.SelectedItem is EventElementDescriptor descriptor) ViewModel.AddElementCommand.Execute(descriptor);
    }

    private void WorkflowValues_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not WorkflowViewModel workflow) return;
        e.Data.Properties["Workflow"] = workflow;
        e.Data.RequestedOperation = DataPackageOperation.Link;
        e.Data.SetText(workflow.Name);
    }

    private void WorkflowValues_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (WorkflowValuesList.SelectedItem is WorkflowViewModel workflow && ViewModel.SelectedStep != null)
            ViewModel.SelectedStep.AssignWorkflowCommand.Execute(workflow);
    }

    private void StationValues_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not StationAssignmentOption option) return;
        e.Data.Properties["StationAssignment"] = option;
        e.Data.RequestedOperation = DataPackageOperation.Link;
        e.Data.SetText(option.Name);
    }

    private void StationValues_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (StationValuesList.SelectedItem is StationAssignmentOption option && ViewModel.SelectedStep != null)
            ViewModel.SelectedStep.AssignStationCommand.Execute(option);
    }

    private void StepCard_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not JourneyFeedbackStepViewModel step) return;
        e.Data.Properties["JourneyFeedbackStep"] = step;
        e.Data.RequestedOperation = DataPackageOperation.Move;
        e.Data.SetText($"InPort {step.InPort}");
    }

    private void StepCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is JourneyFeedbackStepViewModel step) ViewModel.SelectedStep = step;
    }

    private void Sequence_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("EventElement")) e.AcceptedOperation = DataPackageOperation.Copy;
        else if (e.DataView.Properties.ContainsKey("JourneyFeedbackStep")) e.AcceptedOperation = DataPackageOperation.Move;
        else e.AcceptedOperation = DataPackageOperation.None;
    }

    private void Sequence_Drop(object sender, DragEventArgs e)
    {
        var targetIndex = GetDropIndex(e.GetPosition(StepsRepeater));
        if (e.DataView.Properties.TryGetValue("EventElement", out var descriptorValue) && descriptorValue is EventElementDescriptor descriptor)
            ViewModel.InsertElement(descriptor, targetIndex);
        else if (e.DataView.Properties.TryGetValue("JourneyFeedbackStep", out var stepValue) && stepValue is JourneyFeedbackStepViewModel step)
            ViewModel.MoveStep(step, targetIndex);
    }

    private int GetDropIndex(Point position)
    {
        for (var index = 0; index < ViewModel.Steps.Count; index++)
        {
            var element = StepsRepeater.TryGetElement(index);
            if (element == null) continue;
            var origin = element.TransformToVisual(StepsRepeater).TransformPoint(new Point());
            if (position.Y < origin.Y + element.ActualSize.Y / 2) return index;
        }
        return ViewModel.Steps.Count;
    }

    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Delete || ViewModel.SelectedStep == null) return;
        var controlDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        if (controlDown) ViewModel.SelectedStep.RemoveWorkflowCommand.Execute(null);
        else ViewModel.DeleteStepCommand.Execute(ViewModel.SelectedStep);
        e.Handled = true;
    }
}
