namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Moba.SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

internal sealed partial class TrainsPage
{
    public MainWindowViewModel ViewModel { get; }

    private GridLength _trainsExpandedWidth = new(1.1, GridUnitType.Star);
    private GridLength _locomotiveLibraryExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _passengerLibraryExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _goodsLibraryExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(1.25, GridUnitType.Star);

    public TrainsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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
