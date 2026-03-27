// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

/// <summary>
/// Journeys page displaying journeys, stations, and city library with properties panel.
/// Supports drag and drop from city library to stations list.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class JourneysPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;

    public MainWindowViewModel ViewModel { get; }

    public JourneysPage(MainWindowViewModel viewModel, AppSettings settings, ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private double _journeysExpandedWidth = 250;
    private double _stationsExpandedWidth = 250;
    private double _cityLibExpandedWidth = 250;
    private double _workflowLibExpandedWidth = 250;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsJourneyListExpanded))
        {
            if (!ViewModel.IsJourneyListExpanded)
            {
                if (ColJourneys.Width.IsAbsolute)
                {
                    _journeysExpandedWidth = ColJourneys.Width.Value;
                }
                ColJourneys.Width = GridLength.Auto;
            }
            else
            {
                ColJourneys.Width = new GridLength(_journeysExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsStationListExpanded))
        {
            if (!ViewModel.IsStationListExpanded)
            {
                if (ColStations.Width.IsAbsolute)
                {
                    _stationsExpandedWidth = ColStations.Width.Value;
                }
                ColStations.Width = GridLength.Auto;
            }
            else
            {
                ColStations.Width = new GridLength(_stationsExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsCityLibraryVisible))
        {
            if (!ViewModel.IsCityLibraryVisible)
            {
                if (ColCityLib.Width.IsAbsolute)
                {
                    _cityLibExpandedWidth = ColCityLib.Width.Value;
                }
                ColCityLib.Width = GridLength.Auto;
            }
            else
            {
                ColCityLib.Width = new GridLength(_cityLibExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowLibraryVisible))
        {
            if (!ViewModel.IsWorkflowLibraryVisible)
            {
                if (ColWorkflowLib.Width.IsAbsolute)
                {
                    _workflowLibExpandedWidth = ColWorkflowLib.Width.Value;
                }
                ColWorkflowLib.Width = GridLength.Auto;
            }
            else
            {
                ColWorkflowLib.Width = new GridLength(_workflowLibExpandedWidth);
            }
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        RestoreLayout();
    }

    private async void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        SaveLayout();
        if (_settingsService != null)
            await _settingsService.SaveSettingsAsync(_settings);
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.JourneysPage;

        // Restore Column Widths First
        if (layout.JourneysColumnWidth > 0)
        {
            _journeysExpandedWidth = layout.JourneysColumnWidth;
        }

        if (layout.IsJourneyListExpanded)
        {
            ColJourneys.Width = new GridLength(_journeysExpandedWidth);
        }
        else
        {
            ColJourneys.Width = GridLength.Auto;
        }

        if (layout.StationsColumnWidth > 0)
        {
            _stationsExpandedWidth = layout.StationsColumnWidth;
        }

        if (layout.IsStationListExpanded)
        {
            ColStations.Width = new GridLength(_stationsExpandedWidth);
        }
        else
        {
            ColStations.Width = GridLength.Auto;
        }

        if (layout.CityLibraryColumnWidth > 0)
        {
            _cityLibExpandedWidth = layout.CityLibraryColumnWidth;
        }
        
        if (layout.IsCityLibraryExpanded)
        {
            ColCityLib.Width = new GridLength(_cityLibExpandedWidth);
        }
        else
        {
            ColCityLib.Width = GridLength.Auto;
        }

        if (layout.WorkflowLibraryColumnWidth > 0)
        {
            _workflowLibExpandedWidth = layout.WorkflowLibraryColumnWidth;
        }
        
        if (layout.IsWorkflowLibraryExpanded)
        {
            ColWorkflowLib.Width = new GridLength(_workflowLibExpandedWidth);
        }
        else
        {
            ColWorkflowLib.Width = GridLength.Auto;
        }

        // Restore CollapsibleColumn states
        if (ViewModel.IsJourneyListExpanded != layout.IsJourneyListExpanded)
        {
            ViewModel.IsJourneyListExpanded = layout.IsJourneyListExpanded;
        }
        if (ViewModel.IsStationListExpanded != layout.IsStationListExpanded)
        {
            ViewModel.IsStationListExpanded = layout.IsStationListExpanded;
        }
        if (ViewModel.IsCityLibraryVisible != layout.IsCityLibraryExpanded)
        {
            ViewModel.IsCityLibraryVisible = layout.IsCityLibraryExpanded;
        }
        if (ViewModel.IsWorkflowLibraryVisible != layout.IsWorkflowLibraryExpanded)
        {
            ViewModel.IsWorkflowLibraryVisible = layout.IsWorkflowLibraryExpanded;
        }
        if (ViewModel.IsJourneyPropertiesExpanded != layout.IsJourneyPropertiesExpanded)
        {
            ViewModel.IsJourneyPropertiesExpanded = layout.IsJourneyPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.JourneysPage;

        // Save CollapsibleColumn states
        layout.IsJourneyListExpanded = ViewModel.IsJourneyListExpanded;
        layout.IsStationListExpanded = ViewModel.IsStationListExpanded;
        layout.IsCityLibraryExpanded = ViewModel.IsCityLibraryVisible;
        layout.IsWorkflowLibraryExpanded = ViewModel.IsWorkflowLibraryVisible;
        layout.IsJourneyPropertiesExpanded = ViewModel.IsJourneyPropertiesExpanded;

        // Save Column Widths
        if (ColJourneys.Width.IsAbsolute)
        {
            layout.JourneysColumnWidth = ColJourneys.Width.Value;
        }
        else if (!ViewModel.IsJourneyListExpanded)
        {
            layout.JourneysColumnWidth = _journeysExpandedWidth;
        }

        if (ColStations.Width.IsAbsolute)
        {
            layout.StationsColumnWidth = ColStations.Width.Value;
        }
        else if (!ViewModel.IsStationListExpanded)
        {
            layout.StationsColumnWidth = _stationsExpandedWidth;
        }

        if (ColCityLib.Width.IsAbsolute)
        {
            layout.CityLibraryColumnWidth = ColCityLib.Width.Value;
        }
        else if (!ViewModel.IsCityLibraryVisible)
        {
            layout.CityLibraryColumnWidth = _cityLibExpandedWidth;
        }
        if (ColWorkflowLib.Width.IsAbsolute)
        {
            layout.WorkflowLibraryColumnWidth = ColWorkflowLib.Width.Value;
        }
        else if (!ViewModel.IsWorkflowLibraryVisible)
        {
            layout.WorkflowLibraryColumnWidth = _workflowLibExpandedWidth;
        }
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