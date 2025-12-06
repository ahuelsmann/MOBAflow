// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with SelectorBar and inline content switching.
/// All editor tabs are defined in XAML and switched via Visibility.
/// </summary>
public sealed partial class EditorPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public EditorPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        // Set initial selection to Solution
        EditorSelectorBar.SelectedItem = SolutionSelector;
    }

    /// <summary>
    /// Handles SelectorBar selection changes by showing/hiding content grids.
    /// </summary>
    private void EditorSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        // Hide all content grids
        SolutionContent.Visibility = Visibility.Collapsed;
        JourneysContent.Visibility = Visibility.Collapsed;
        WorkflowsContent.Visibility = Visibility.Collapsed;
        TrainsContent.Visibility = Visibility.Collapsed;

        // Show the selected content
        var selectedItem = EditorSelectorBar.SelectedItem;

        if (selectedItem == SolutionSelector)
        {
            SolutionContent.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Showing Solution content");
        }
        else if (selectedItem == JourneysSelector)
        {
            JourneysContent.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Showing Journeys content");
        }
        else if (selectedItem == WorkflowsSelector)
        {
            WorkflowsContent.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Showing Workflows content");
        }
        else if (selectedItem == TrainsSelector)
        {
            TrainsContent.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Showing Trains content");
        }
    }

    #region Drag & Drop Event Handlers

    private void CityListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is Domain.City city)
        {
            e.Data.SetData("City", city);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
    }

    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is SharedUI.ViewModel.WorkflowViewModel workflow)
        {
            e.Data.Properties.Add("Workflow", workflow);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(workflow.Name);
        }
    }

    private void StationListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void StationListView_Drop(object sender, DragEventArgs e)
    {
        // Handle City drop (create new Station)
        if (e.DataView.Properties.TryGetValue("City", out object cityObj) && cityObj is Domain.City city)
        {
            if (ViewModel.SelectedJourney != null)
            {
                var station = new Domain.Station { Name = city.Name };
                var stationViewModel = new SharedUI.ViewModel.StationViewModel(station);
                ViewModel.SelectedJourney.Stations.Add(stationViewModel);
            }
        }
        // Handle Workflow drop (assign to selected Station)
        else if (e.DataView.Properties.TryGetValue("Workflow", out object workflowObj) && workflowObj is SharedUI.ViewModel.WorkflowViewModel workflow)
        {
            if (ViewModel.SelectedStation != null)
            {
                ViewModel.SelectedStation.WorkflowId = workflow.Model.Id;
            }
        }
    }
    private void CityListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // Add selected city as station to current journey on double-click
        if (ViewModel.SelectedCity != null && ViewModel.SelectedJourney != null)
        {
            var station = new Domain.Station { Name = ViewModel.SelectedCity.Name };
            var stationViewModel = new SharedUI.ViewModel.StationViewModel(station);
            ViewModel.SelectedJourney.Stations.Add(stationViewModel);
        }
    }

    private void JourneyListView_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Refresh PropertyGrid even if same Journey is clicked again
        ViewModel.RefreshPropertyGrid();
    }

    private void GoodsWagonListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is Domain.GoodsWagon goodsWagon)
        {
            e.Data.Properties.Add("GoodsWagon", goodsWagon);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(goodsWagon.Name);
        }
    }

    private void PassengerWagonListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is Domain.PassengerWagon passengerWagon)
        {
            e.Data.Properties.Add("PassengerWagon", passengerWagon);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(passengerWagon.Name);
        }
    }

    private void WagonListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void WagonListView_Drop(object sender, DragEventArgs e)
    {
        // Handle GoodsWagon drop
        if (e.DataView.Properties.TryGetValue("GoodsWagon", out object goodsWagonObj) &&
            goodsWagonObj is Domain.GoodsWagon goodsWagon)
        {
            if (ViewModel.SelectedTrain != null)
            {
                var wagonViewModel = new SharedUI.ViewModel.GoodsWagonViewModel(goodsWagon);
                ViewModel.SelectedTrain.Wagons.Add(wagonViewModel);
            }
        }
        // Handle PassengerWagon drop
        else if (e.DataView.Properties.TryGetValue("PassengerWagon", out object passengerWagonObj) &&
                 passengerWagonObj is Domain.PassengerWagon passengerWagon)
        {
            if (ViewModel.SelectedTrain != null)
            {
                var wagonViewModel = new SharedUI.ViewModel.PassengerWagonViewModel(passengerWagon);
                ViewModel.SelectedTrain.Wagons.Add(wagonViewModel);
            }
        }
    }

    #endregion
}