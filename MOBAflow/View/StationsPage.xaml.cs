// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;

internal sealed partial class StationsPage
{
    public MainWindowViewModel ViewModel { get; }

    public StationsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is WorkflowViewModel workflow)
        {
            e.Data.Properties.Add("Workflow", workflow);
            e.Data.RequestedOperation = DataPackageOperation.Link;
            e.Data.SetText(workflow.Name);
        }
    }
}
