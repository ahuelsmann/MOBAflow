// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Workflows page displaying workflows and actions with properties panel.
/// Supports drag & drop of workflows to stations.
/// </summary>
public sealed partial class WorkflowsPage
{
    public MainWindowViewModel ViewModel { get; }

    public WorkflowsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
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

    private void WorkflowListView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Delete && ViewModel.DeleteWorkflowCommand.CanExecute(null))
        {
            ViewModel.DeleteWorkflowCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActionListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is object action)
        {
            e.Data.Properties.Add("Action", action);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void ActionListView_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        _ = e;
        if (ViewModel.SelectedWorkflow == null) return;

        // ListView CanReorderItems="True" handles reordering automatically
        // We just need to update the Number property to reflect the new order
        UpdateActionNumbers();
    }

    private void ActionListView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Delete && ViewModel.DeleteActionCommand.CanExecute(null))
        {
            ViewModel.DeleteActionCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void UpdateActionNumbers()
    {
        if (ViewModel.SelectedWorkflow == null) return;

        for (int i = 0; i < ViewModel.SelectedWorkflow.Actions.Count; i++)
        {
            if (ViewModel.SelectedWorkflow.Actions[i] is SharedUI.ViewModel.Action.WorkflowActionViewModel actionVM)
            {
                actionVM.Number = (uint)(i + 1);
            }
        }

        // Mark solution as unsaved after reordering actions
        ViewModel.HasUnsavedChanges = true;
    }
    #endregion
}

