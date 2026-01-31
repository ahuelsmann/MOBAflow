// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

/// <summary>
/// Journeys page displaying journeys, stations, and city library with properties panel.
/// Supports drag & drop from city library to stations list.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class JourneysPage
{
    public MainWindowViewModel ViewModel { get; }

    public JourneysPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    #region Drag & Drop Event Handlers
    private void CityListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is City city)
        {
            e.Data.Properties.Add("City", city);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.SetText(city.Name);
        }
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

    private void StationListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private void StationListView_Drop(object sender, DragEventArgs e)
    {
        // Handle City drop (create new Station)
        if (e.DataView.Properties.TryGetValue("City", out object? cityObj) && cityObj is City city)
        {
            ViewModel.AddStationFromCityCommand.Execute(city);
        }
        // Handle Workflow drop (assign to selected Station)
        else if (e.DataView.Properties.TryGetValue("Workflow", out object? workflowObj) && workflowObj is WorkflowViewModel workflow)
        {
            ViewModel.AssignWorkflowToStationCommand.Execute(workflow);
        }
    }

    private void CityListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _ = e;
        // Delegate to ViewModel Command
        if (ViewModel.SelectedCity != null)
        {
            ViewModel.AddStationFromCityCommand.Execute(ViewModel.SelectedCity);
        }
    }

    private void JourneysListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Delete && ViewModel.DeleteJourneyCommand.CanExecute(null))
        {
            ViewModel.DeleteJourneyCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void StationsListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Delete && ViewModel.DeleteStationCommand.CanExecute(null))
        {
            ViewModel.DeleteStationCommand.Execute(null);
            e.Handled = true;
        }
    }
    #endregion
}