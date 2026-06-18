// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Moba.SharedUI.ViewModel;

using SharedUI.Interface;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

internal sealed partial class TrainsPage
{
    public MainWindowViewModel ViewModel { get; }

    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<TrainsPage>? _logger;

    private GridLength _trainsExpandedWidth = new(0.9, GridUnitType.Star);
    private GridLength _locomotiveLibraryExpandedWidth = new(0.8, GridUnitType.Star);
    private GridLength _passengerLibraryExpandedWidth = new(0.8, GridUnitType.Star);
    private GridLength _goodsLibraryExpandedWidth = new(0.8, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(1.9, GridUnitType.Star);

    public TrainsPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<TrainsPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsTrainsListExpanded))
        {
            ApplyColumnState(ViewModel.IsTrainsListExpanded, ColTrains, ref _trainsExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsTrainsLocomotiveLibraryExpanded))
        {
            ApplyColumnState(ViewModel.IsTrainsLocomotiveLibraryExpanded, ColLocomotives, ref _locomotiveLibraryExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsTrainsPassengerLibraryExpanded))
        {
            ApplyColumnState(ViewModel.IsTrainsPassengerLibraryExpanded, ColPassengerWagons, ref _passengerLibraryExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsTrainsGoodsLibraryExpanded))
        {
            ApplyColumnState(ViewModel.IsTrainsGoodsLibraryExpanded, ColGoodsWagons, ref _goodsLibraryExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsTrainsPropertiesExpanded))
        {
            ApplyColumnState(ViewModel.IsTrainsPropertiesExpanded, ColProperties, ref _propertiesExpandedWidth);
        }
    }

    private static void ApplyColumnState(bool isExpanded, ColumnDefinition column, ref GridLength rememberedWidth)
    {
        if (!isExpanded)
        {
            if (!column.Width.IsAuto)
            {
                rememberedWidth = column.Width;
            }

            column.Width = GridLength.Auto;
        }
        else
        {
            column.Width = rememberedWidth;
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.TrainsPage;

        _trainsExpandedWidth = ToStarGridLength(layout.TrainListColumnStarValue, _trainsExpandedWidth);
        _locomotiveLibraryExpandedWidth = ToStarGridLength(layout.LocomotiveLibraryColumnStarValue, _locomotiveLibraryExpandedWidth);
        _passengerLibraryExpandedWidth = ToStarGridLength(layout.PassengerLibraryColumnStarValue, _passengerLibraryExpandedWidth);
        _goodsLibraryExpandedWidth = ToStarGridLength(layout.GoodsLibraryColumnStarValue, _goodsLibraryExpandedWidth);
        _propertiesExpandedWidth = ToStarGridLength(layout.PropertiesColumnStarValue, _propertiesExpandedWidth);

        RestoreColumnState(layout.IsTrainListExpanded, ColTrains, _trainsExpandedWidth);
        RestoreColumnState(layout.IsLocomotiveLibraryExpanded, ColLocomotives, _locomotiveLibraryExpandedWidth);
        RestoreColumnState(layout.IsPassengerLibraryExpanded, ColPassengerWagons, _passengerLibraryExpandedWidth);
        RestoreColumnState(layout.IsGoodsLibraryExpanded, ColGoodsWagons, _goodsLibraryExpandedWidth);
        RestoreColumnState(layout.IsPropertiesExpanded, ColProperties, _propertiesExpandedWidth);
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.TrainsPage;

        layout.IsTrainListExpanded = ViewModel.IsTrainsListExpanded;
        layout.IsLocomotiveLibraryExpanded = ViewModel.IsTrainsLocomotiveLibraryExpanded;
        layout.IsPassengerLibraryExpanded = ViewModel.IsTrainsPassengerLibraryExpanded;
        layout.IsGoodsLibraryExpanded = ViewModel.IsTrainsGoodsLibraryExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsTrainsPropertiesExpanded;

        layout.TrainListColumnStarValue = GetCurrentStarValue(ColTrains, _trainsExpandedWidth);
        layout.LocomotiveLibraryColumnStarValue = GetCurrentStarValue(ColLocomotives, _locomotiveLibraryExpandedWidth);
        layout.PassengerLibraryColumnStarValue = GetCurrentStarValue(ColPassengerWagons, _passengerLibraryExpandedWidth);
        layout.GoodsLibraryColumnStarValue = GetCurrentStarValue(ColGoodsWagons, _goodsLibraryExpandedWidth);
        layout.PropertiesColumnStarValue = GetCurrentStarValue(ColProperties, _propertiesExpandedWidth);
    }

    private static GridLength ToStarGridLength(double starValue, GridLength fallback)
    {
        return starValue > 0
            ? new GridLength(starValue, GridUnitType.Star)
            : fallback;
    }

    private static void RestoreColumnState(bool isExpanded, ColumnDefinition column, GridLength rememberedWidth)
    {
        column.Width = isExpanded ? rememberedWidth : GridLength.Auto;
    }

    private static double GetCurrentStarValue(ColumnDefinition column, GridLength fallback)
    {
        if (column.Width.IsStar)
        {
            return column.Width.Value;
        }

        return fallback.IsStar ? fallback.Value : 1;
    }

    private void LocomotiveLibraryListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is LocomotiveViewModel locomotive)
        {
            e.Data.Properties.Add("Locomotive", locomotive);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.SetText(locomotive.Name);
        }
    }

    private void PassengerWagonLibraryListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is PassengerWagonViewModel wagon)
        {
            e.Data.Properties.Add("PassengerWagon", wagon);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.SetText(wagon.Name);
        }
    }

    private void GoodsWagonLibraryListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is GoodsWagonViewModel wagon)
        {
            e.Data.Properties.Add("GoodsWagon", wagon);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.SetText(wagon.Name);
        }
    }

    private void VehicleListView_DragOver(object sender, DragEventArgs e)
    {
        if (ViewModel.SelectedTrain == null)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        if (e.DataView.Properties.ContainsKey("Locomotive")
            || e.DataView.Properties.ContainsKey("PassengerWagon")
            || e.DataView.Properties.ContainsKey("GoodsWagon"))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void VehicleListView_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel.SelectedTrain == null)
        {
            return;
        }

        var insertIndex = GetVehicleDropIndex(e.GetPosition(VehicleListView));

        if (e.DataView.Properties.TryGetValue("Locomotive", out var locomotiveObject) && locomotiveObject is LocomotiveViewModel locomotive)
        {
            ViewModel.AddLocomotiveToSelectedTrain(locomotive, insertIndex);
        }
        else if (e.DataView.Properties.TryGetValue("PassengerWagon", out var passengerObject) && passengerObject is PassengerWagonViewModel passengerWagon)
        {
            ViewModel.AddPassengerWagonToSelectedTrain(passengerWagon, insertIndex);
        }
        else if (e.DataView.Properties.TryGetValue("GoodsWagon", out var goodsObject) && goodsObject is GoodsWagonViewModel goodsWagon)
        {
            ViewModel.AddGoodsWagonToSelectedTrain(goodsWagon, insertIndex);
        }
    }

    private void VehicleListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        _ = sender;
        _ = args;
        ViewModel.SynchronizeSelectedVehicles();
    }

    private int GetVehicleDropIndex(Point position)
    {
        for (var i = 0; i < VehicleListView.Items.Count; i++)
        {
            if (VehicleListView.ContainerFromIndex(i) is not ListViewItem container)
            {
                continue;
            }

            var topLeft = container.TransformToVisual(VehicleListView).TransformPoint(new Point(0, 0));
            var center = topLeft.X + (container.ActualWidth / 2);
            if (position.X < center)
            {
                return i;
            }
        }

        return VehicleListView.Items.Count;
    }

    private void TrainsListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key == VirtualKey.Delete && ViewModel.DeleteTrainCommand.CanExecute(null))
        {
            ViewModel.DeleteTrainCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void VehicleListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key == VirtualKey.Delete && ViewModel.SelectedVehicle != null)
        {
            ViewModel.RemoveSelectedVehicle(ViewModel.SelectedVehicle);
            e.Handled = true;
        }
    }
}