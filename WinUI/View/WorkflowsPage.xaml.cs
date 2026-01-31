// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.ViewModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

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

        Debug.WriteLine("✅ ✅ ✅ WorkflowsPage LOADED - Debug Output is WORKING! ✅ ✅ ✅");
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

        // Log to file - check WinUI/bin/Debug/logs/mobaflow-YYYYMMDD.txt
        Trace.WriteLine("[DRAG] DragItemsCompleted - Items were reordered!");
        Debug.WriteLine("🔄 DragItemsCompleted - Items were reordered!");

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