// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Domain;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.ViewModel;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

/// <summary>
/// Journeys page displaying journeys, stations, and city library with properties panel.
/// Supports drag and drop from city library to stations list.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class JourneysPage
{
    private readonly Common.Configuration.AppSettings _settings;
    private readonly SharedUI.Interface.ISettingsService? _settingsService;

    public MainWindowViewModel ViewModel { get; }

    public JourneysPage(MainWindowViewModel viewModel, Common.Configuration.AppSettings settings, SharedUI.Interface.ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
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

        // Restore column widths
        var rootGrid = (Grid)Content;
        rootGrid.ColumnDefinitions[0].Width = new GridLength(layout.JourneysColumnWidth);
        rootGrid.ColumnDefinitions[2].Width = new GridLength(layout.StationsColumnWidth);

        // Restore CollapsibleColumn states
        ViewModel.IsCityLibraryVisible = layout.IsCityLibraryExpanded;
        ViewModel.IsWorkflowLibraryVisible = layout.IsWorkflowLibraryExpanded;
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.JourneysPage;
        var rootGrid = (Grid)Content;

        // Save column widths
        layout.JourneysColumnWidth = rootGrid.ColumnDefinitions[0].ActualWidth;
        layout.StationsColumnWidth = rootGrid.ColumnDefinitions[2].ActualWidth;

        // Save CollapsibleColumn states
        layout.IsCityLibraryExpanded = ViewModel.IsCityLibraryVisible;
        layout.IsWorkflowLibraryExpanded = ViewModel.IsWorkflowLibraryVisible;
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

    #region Column Splitter (Manual Resize)
    private bool _isSplitterDragging;
    private Windows.Foundation.Point _splitterDragStart;
    private double _splitterStartSize;

    private void OnSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement splitter) return;

        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void OnSplitterPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement splitter) return;

        _isSplitterDragging = true;
        _splitterDragStart = e.GetCurrentPoint(this).Position;

        var columnIndex = Grid.GetColumn(splitter) - 1;
        var column = ((Grid)splitter.Parent).ColumnDefinitions[columnIndex];
        _splitterStartSize = column.ActualWidth;

        splitter.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging) return;

        var current = e.GetCurrentPoint(this).Position;
        var delta = current.X - _splitterDragStart.X;

        if (sender is not FrameworkElement splitter) return;
        var columnIndex = Grid.GetColumn(splitter) - 1;
        var parentGrid = (Grid)splitter.Parent;
        var column = parentGrid.ColumnDefinitions[columnIndex];

        const double minSize = 150;
        const double maxSize = 600;
        var newWidth = Math.Clamp(_splitterStartSize + delta, minSize, maxSize);
        column.Width = new GridLength(newWidth);

        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging) return;

        _isSplitterDragging = false;

        if (sender is FrameworkElement splitter)
            splitter.ReleasePointerCapture(e.Pointer);

        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        e.Handled = true;
    }
    #endregion
}