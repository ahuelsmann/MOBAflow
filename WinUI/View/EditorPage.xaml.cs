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

        // Set initial selection to Trains
        EditorSelectorBar.SelectedItem = TrainsSelector;
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
            e.Data.SetData("Workflow", workflow);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
    }

    private void StationListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private async void StationListView_Drop(object sender, DragEventArgs e)
    {
        if (await e.DataView.GetDataAsync("City") is Domain.City city)
        {
            if (ViewModel.SelectedJourney != null)
            {
                // Add station to journey (create StationViewModel from City)
                var station = new Domain.Station
                {
                    Name = city.Name
                };
                var stationViewModel = new SharedUI.ViewModel.StationViewModel(station);
                ViewModel.SelectedJourney.Stations.Add(stationViewModel);
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

    #endregion
}