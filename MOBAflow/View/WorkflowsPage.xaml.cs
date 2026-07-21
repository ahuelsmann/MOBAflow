// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.WorkflowSteps;

using SharedUI.Interface;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;

/// <summary>
/// Workflows page displaying workflows and actions with properties panel.
/// Supports drag and drop of workflows to stations.
/// </summary>
internal sealed partial class WorkflowsPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<WorkflowsPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    private double _workflowsExpandedWidth = 200;
    private double _actionsExpandedWidth = 300;
    private double _propertiesExpandedStarValue = 1;

    public WorkflowsPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<WorkflowsPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsWorkflowListExpanded))
        {
            if (!ViewModel.IsWorkflowListExpanded)
            {
                if (ColWorkflows.Width.IsAbsolute)
                {
                    _workflowsExpandedWidth = ColWorkflows.Width.Value;
                }
                ColWorkflows.Width = GridLength.Auto;
            }
            else
            {
                ColWorkflows.Width = new GridLength(_workflowsExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowActionsExpanded))
        {
            if (!ViewModel.IsWorkflowActionsExpanded)
            {
                if (ColActions.Width.IsAbsolute)
                {
                    _actionsExpandedWidth = ColActions.Width.Value;
                }
                ColActions.Width = GridLength.Auto;
            }
            else
            {
                ColActions.Width = new GridLength(_actionsExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowPropertiesExpanded))
        {
            if (!ViewModel.IsWorkflowPropertiesExpanded)
            {
                if (ColProperties.Width.IsStar)
                {
                    _propertiesExpandedStarValue = ColProperties.Width.Value;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
            }
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.WorkflowsPage;

        if (layout.WorkflowListColumnWidth > 0)
        {
            _workflowsExpandedWidth = layout.WorkflowListColumnWidth;
        }
        if (layout.ActionsColumnWidth > 0)
        {
            _actionsExpandedWidth = layout.ActionsColumnWidth;
        }
        if (layout.PropertiesColumnStarValue > 0)
        {
            _propertiesExpandedStarValue = layout.PropertiesColumnStarValue;
        }

        if (layout.IsWorkflowListExpanded)
        {
            ColWorkflows.Width = new GridLength(_workflowsExpandedWidth);
        }
        else
        {
            ColWorkflows.Width = GridLength.Auto;
        }

        if (layout.IsActionsExpanded)
        {
            ColActions.Width = new GridLength(_actionsExpandedWidth);
        }
        else
        {
            ColActions.Width = GridLength.Auto;
        }

        if (layout.IsPropertiesExpanded)
        {
            ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
        }
        else
        {
            ColProperties.Width = GridLength.Auto;
        }

        if (ViewModel.IsWorkflowListExpanded != layout.IsWorkflowListExpanded)
        {
            ViewModel.IsWorkflowListExpanded = layout.IsWorkflowListExpanded;
        }
        if (ViewModel.IsWorkflowActionsExpanded != layout.IsActionsExpanded)
        {
            ViewModel.IsWorkflowActionsExpanded = layout.IsActionsExpanded;
        }
        if (ViewModel.IsWorkflowPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsWorkflowPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.WorkflowsPage;

        layout.IsWorkflowListExpanded = ViewModel.IsWorkflowListExpanded;
        layout.IsActionsExpanded = ViewModel.IsWorkflowActionsExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsWorkflowPropertiesExpanded;

        if (ColWorkflows.Width.IsAbsolute)
        {
            layout.WorkflowListColumnWidth = ColWorkflows.Width.Value;
        }
        else if (!ViewModel.IsWorkflowListExpanded)
        {
            layout.WorkflowListColumnWidth = _workflowsExpandedWidth;
        }

        if (ColActions.Width.IsAbsolute)
        {
            layout.ActionsColumnWidth = ColActions.Width.Value;
        }
        else if (!ViewModel.IsWorkflowActionsExpanded)
        {
            layout.ActionsColumnWidth = _actionsExpandedWidth;
        }

        if (ColProperties.Width.IsStar)
        {
            layout.PropertiesColumnStarValue = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsWorkflowPropertiesExpanded)
        {
            layout.PropertiesColumnStarValue = _propertiesExpandedStarValue;
        }
    }

    #region Drag & Drop Event Handlers
    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is WorkflowViewModel workflow)
        {
            e.Data.Properties.Add("Workflow", workflow);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.SetText(workflow.Name);
        }
    }

    private void WorkflowListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Delete && ViewModel.WorkflowLibrary.DeleteSelectedWorkflowCommand.CanExecute(null))
        {
            ViewModel.WorkflowLibrary.DeleteSelectedWorkflowCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActionListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is WorkflowStepViewModel step)
        {
            e.Data.Properties.Add("WorkflowStep", step);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void ActionListView_Drop(object sender, DragEventArgs e)
    {
        // No longer needed - DragItemsCompleted handles drag & drop reordering
        _ = e;
    }

    private void ActionListView_DragItemsCompleted(object sender, DragItemsCompletedEventArgs e)
    {
        if (ViewModel.SelectedWorkflow == null) return;

        ViewModel.SelectedWorkflow.UpdateStepOrder();
    }

    private void ActionListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var workflow = ViewModel.WorkflowLibrary.SelectedWorkflow;
        var step = ViewModel.WorkflowLibrary.SelectedStep;
        if (e.Key == VirtualKey.Delete && workflow?.DeleteStepCommand.CanExecute(step) == true)
        {
            workflow.DeleteStepCommand.Execute(step);
            e.Handled = true;
        }
    }
    #endregion
}
