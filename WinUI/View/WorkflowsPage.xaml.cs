// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Workflows page displaying workflows and actions with properties panel.
/// Supports drag & drop of workflows to stations.
/// </summary>
public sealed partial class WorkflowsPage : Page
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
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(workflow.Name);
        }
    }
    #endregion
}

