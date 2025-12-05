// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page 1 with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// Works directly with MainWindowViewModel (no wrapper ViewModel).
/// </summary>
public sealed partial class EditorPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public EditorPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        this.DataContext = viewModel; // Set DataContext for {Binding} expressions in DataTemplates
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine("EditorPage initialized with MainWindowViewModel and dynamic PropertyGrid");
    }

    /// <summary>
    /// Helper function to get the currently selected object for PropertyGrid binding.
    /// Returns the most specific selection (Station > Journey > Workflow > Train > Project).
    /// </summary>
    private object? GetCurrentSelectedObject(
        ProjectViewModel? project,
        JourneyViewModel? journey,
        StationViewModel? station,
        WorkflowViewModel? workflow,
        TrainViewModel? train)
    {
        // Priority: Station > Journey > Workflow > Train > Project
        if (station != null) return station;
        if (journey != null) return journey;
        if (workflow != null) return workflow;
        if (train != null) return train;
        if (project != null) return project;
        return null;
    }

    private void CityItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement element && element.DataContext is Moba.Domain.City city)
        {
            // Use MainWindowViewModel command directly
            ViewModel.AddStationFromCityCommand.Execute(city);

            // Close the flyout
            if (element.FindName("CityListView") is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                var flyout = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(listView);
                flyout?.Hide();
            }
        }
    }

    /// <summary>
    /// Handler for double-click on City item to add as Station.
    /// </summary>
    private void CityItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement element && element.DataContext is Moba.Domain.City city)
        {
            ViewModel.AddStationFromCityCommand.Execute(city);
        }
    }

    /// <summary>
    /// Starts drag operation when dragging a City from the library.
    /// </summary>
    private void CityListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is Moba.Domain.City city)
        {
            // Store the city in the data package
            e.Data.Properties.Add("City", city);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
    }

    /// <summary>
    /// Starts drag operation when dragging a Workflow from the selection helper.
    /// </summary>
    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is WorkflowViewModel workflow)
        {
            // Store the workflow in the data package
            e.Data.Properties.Add("Workflow", workflow.Model);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
    }

    /// <summary>
    /// Validates drag-over for Stations ListView.
    /// </summary>
    private void StationsListView_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        // Only accept if dragging from CityListView
        if (e.DataView.Properties.ContainsKey("City"))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Add Station";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    /// <summary>
    /// Handles drop of City onto Stations ListView.
    /// </summary>
    private async void StationsListView_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("City", out object cityObj) && cityObj is Moba.Domain.City city)
        {
            ViewModel.AddStationFromCityCommand.Execute(city);
        }
    }

    /// <summary>
    /// Handles SelectorBar selection changes for view switching.
    /// </summary>
    private void EditorSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = EditorSelectorBar.SelectedItem;

        // For now, we don't change anything - all columns are always visible
        // In the future, we can conditionally show/hide columns based on selection

        // Example: Show different columns for different views
        // if (selectedItem == SolutionSelector) { ... }
        // if (selectedItem == JourneysSelector) { ... }
    }
}