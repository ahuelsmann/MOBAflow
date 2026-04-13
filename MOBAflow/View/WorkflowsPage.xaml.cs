// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Moba.SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;

/// <summary>
/// Workflows page displaying workflows and actions with properties panel.
/// Supports drag and drop of workflows to stations.
/// </summary>
internal sealed partial class WorkflowsPage
{
    public MainWindowViewModel ViewModel { get; }

    private GridLength _workflowsExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _actionsExpandedWidth = new(1.5, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(1.5, GridUnitType.Star);

    public WorkflowsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsWorkflowListExpanded))
        {
            if (!ViewModel.IsWorkflowListExpanded)
            {
                if (!ColWorkflows.Width.IsAuto)
                {
                    _workflowsExpandedWidth = ColWorkflows.Width;
                }
                ColWorkflows.Width = GridLength.Auto;
            }
            else
            {
                ColWorkflows.Width = _workflowsExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowActionsExpanded))
        {
            if (!ViewModel.IsWorkflowActionsExpanded)
            {
                if (!ColActions.Width.IsAuto)
                {
                    _actionsExpandedWidth = ColActions.Width;
                }
                ColActions.Width = GridLength.Auto;
            }
            else
            {
                ColActions.Width = _actionsExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowPropertiesExpanded))
        {
            if (!ViewModel.IsWorkflowPropertiesExpanded)
            {
                if (!ColProperties.Width.IsAuto)
                {
                    _propertiesExpandedWidth = ColProperties.Width;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = _propertiesExpandedWidth;
            }
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
        if (e.Key == VirtualKey.Delete && ViewModel.DeleteWorkflowCommand.CanExecute(null))
        {
            ViewModel.DeleteWorkflowCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActionListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is { } action)
        {
            e.Data.Properties.Add("Action", action);
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

        // Update action numbers and save after drag & drop completes
        ViewModel.SelectedWorkflow.UpdateActionNumbers();
    }

    private void ActionListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Delete && ViewModel.DeleteActionCommand.CanExecute(null))
        {
            ViewModel.DeleteActionCommand.Execute(null);
            e.Handled = true;
        }
    }
    #endregion
}