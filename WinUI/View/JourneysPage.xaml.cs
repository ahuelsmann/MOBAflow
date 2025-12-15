// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using SharedUI.ViewModel;

/// <summary>
/// Journeys page displaying journeys, stations, and city library with properties panel.
/// Supports drag & drop from city library to stations list.
/// </summary>
public sealed partial class JourneysPage : Page
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
        if (e.Items.FirstOrDefault() is Domain.City city)
        {
            e.Data.Properties.Add("City", city);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(city.Name);
        }
    }

    private void StationListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void StationListView_Drop(object sender, DragEventArgs e)
    {
        // Handle City drop (create new Station)
        if (e.DataView.Properties.TryGetValue("City", out object? cityObj) && cityObj is Domain.City city)
        {
            ViewModel.AddStationFromCityCommand.Execute(city);
        }
        // Handle Workflow drop (assign to selected Station)
        else if (e.DataView.Properties.TryGetValue("Workflow", out object? workflowObj) && workflowObj is WorkflowViewModel workflow)
        {
            ViewModel.AssignWorkflowToStationCommand.Execute(workflow);
                }
            }

            private void CityListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
            {
                // Delegate to ViewModel Command
                if (ViewModel.SelectedCity != null)
                {
                    ViewModel.AddStationFromCityCommand.Execute(ViewModel.SelectedCity);
                }
            }

            private void StationListView_DeleteKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
            {
                // Delegate to ViewModel Command
                if (ViewModel.DeleteStationCommand.CanExecute(null))
                {
                    ViewModel.DeleteStationCommand.Execute(null);
                    args.Handled = true;
                }
            }
            #endregion
        }