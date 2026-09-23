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
        _valuesWidth = layout.ValuesColumnWidth;
        _propertiesWidth = layout.PropertiesColumnWidth;
        ValuesPanel.IsExpanded = layout.IsValuesExpanded;
        PropertiesPanel.IsExpanded = layout.IsPropertiesExpanded;
        ApplyPanelWidths();
        ValuesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
        PropertiesPanel.RegisterPropertyChangedCallback(CollapsibleColumnBase.IsExpandedProperty, OnPanelExpansionChanged);
    }

    private void OnPanelExpansionChanged(DependencyObject sender, DependencyProperty property) => ApplyPanelWidths();

    private void ApplyPanelWidths()
    {
        ValuesColumn.Width = ValuesPanel.IsExpanded ? new GridLength(_valuesWidth) : GridLength.Auto;
        PropertiesColumn.Width = PropertiesPanel.IsExpanded ? new GridLength(_propertiesWidth) : GridLength.Auto;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => SaveLayoutAsync().Observe(ex => _logger?.LogWarning(ex, "Persist Event Manager layout failed"));

    private async Task SaveLayoutAsync()
    {
        var layout = _settings.Layout.EventManagerPage;
        layout.IsValuesExpanded = ValuesPanel.IsExpanded;
        layout.IsPropertiesExpanded = PropertiesPanel.IsExpanded;
        if (ValuesColumn.Width.IsAbsolute) layout.ValuesColumnWidth = ValuesColumn.Width.Value;
        if (PropertiesColumn.Width.IsAbsolute) layout.PropertiesColumnWidth = PropertiesColumn.Width.Value;
        if (_settingsService != null) await _settingsService.SaveSettingsAsync(_settings);
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

    private void StepRow_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not JourneyFeedbackStepViewModel step) return;
        ViewModel.SelectedStep = step;
        e.Data.Properties["JourneyFeedbackStep"] = step;
        e.Data.RequestedOperation = DataPackageOperation.Move;
        e.Data.SetText($"InPort {step.InPort}");
    }

    private void StepRow_Tapped(object sender, TappedRoutedEventArgs e) => SelectStep(sender);

    private void StepRow_GotFocus(object sender, RoutedEventArgs e) => SelectStep(sender);

    private void StepRow_ContextRequested(UIElement sender, ContextRequestedEventArgs e) => SelectStep(sender);

    private void StepActions_Click(object sender, RoutedEventArgs e) => SelectStep(sender);

    private void SelectStep(object sender)
    {
        if ((sender as FrameworkElement)?.Tag is JourneyFeedbackStepViewModel step) ViewModel.SelectedStep = step;
    }

    private void DeleteStep_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is JourneyFeedbackStepViewModel step)
            ViewModel.DeleteStepCommand.Execute(step);
    }

    private void StepsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        AutomationProperties.SetPositionInSet(args.Element, args.Index + 1);
        AutomationProperties.SetSizeOfSet(args.Element, ViewModel.Steps.Count);
    }

    private void Sequence_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("JourneyFeedbackStep")) e.AcceptedOperation = DataPackageOperation.Move;
        else e.AcceptedOperation = DataPackageOperation.None;
    }

    private void Sequence_Drop(object sender, DragEventArgs e)
    {
        var targetIndex = GetDropIndex(e.GetPosition(StepsRepeater));
        if (e.DataView.Properties.TryGetValue("JourneyFeedbackStep", out var stepValue) && stepValue is JourneyFeedbackStepViewModel step)
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

    private void StepsRepeater_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Only list rows own these shortcuts; editing a property must never delete an event.
        if (FocusManager.GetFocusedElement(XamlRoot) is not ListViewItem { Tag: JourneyFeedbackStepViewModel step }) return;
        var index = ViewModel.Steps.IndexOf(step);
        if (index < 0) return;

        var targetIndex = e.Key switch
        {
            VirtualKey.Up => Math.Max(0, index - 1),
            VirtualKey.Down => Math.Min(ViewModel.Steps.Count - 1, index + 1),
            VirtualKey.Home => 0,
            VirtualKey.End => ViewModel.Steps.Count - 1,
            _ => -1
        };
        if (targetIndex >= 0)
        {
            FocusStep(targetIndex);
        }
        else if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            ViewModel.SelectedStep = step;
        }
        else if (e.Key == VirtualKey.Delete)
        {
            var controlDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (controlDown) step.RemoveWorkflowCommand.Execute(null);
            else
            {
                ViewModel.DeleteStepCommand.Execute(step);
                if (ViewModel.Steps.Count > 0) FocusStep(Math.Min(index, ViewModel.Steps.Count - 1));
            }
        }
        else return;

        e.Handled = true;
    }

    private void FocusStep(int index)
    {
        ViewModel.SelectedStep = ViewModel.Steps[index];
        var row = StepsRepeater.GetOrCreateElement(index);
        StepsRepeater.UpdateLayout();
        if (row is Control control) control.Focus(FocusState.Keyboard);
        row.StartBringIntoView();
    }

}
