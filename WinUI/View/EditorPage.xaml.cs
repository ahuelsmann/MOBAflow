// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with TabView navigation.
/// All editor tabs are defined as TabViewItems in XAML.
/// </summary>
public sealed partial class EditorPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public EditorPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        // Set initial selection to Solution tab
        EditorTabView.SelectedIndex = 0;
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
        if (e.Items.FirstOrDefault() is WorkflowViewModel workflow)
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
                var stationViewModel = new StationViewModel(station);
                ViewModel.SelectedJourney.Stations.Add(stationViewModel);
            }
        }
        // Handle Workflow drop (assign to selected Station)
        else if (e.DataView.Properties.TryGetValue("Workflow", out object workflowObj) && workflowObj is WorkflowViewModel workflow)
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
            var stationViewModel = new StationViewModel(station);
            ViewModel.SelectedJourney.Stations.Add(stationViewModel);
        }
    }

    private void JourneyListView_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Refresh PropertyGrid even if same Journey is clicked again
        ViewModel.RefreshPropertyGrid();
    }

    #endregion

    #region Train Composition - Drag & Drop

    private void LocomotiveLibrary_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is LocomotiveViewModel locomotiveVM)
        {
            e.Data.Properties.Add("Locomotive", locomotiveVM.Model);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(locomotiveVM.Name);
        }
    }

    private void LocomotiveLibrary_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // Add selected locomotive to current train on double-click
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is LocomotiveViewModel locomotiveVM &&
            ViewModel.SelectedTrain != null)
        {
            // Create a copy to avoid modifying the library object
            var locomotiveCopy = new Domain.Locomotive
            {
                Name = locomotiveVM.Model.Name,
                DigitalAddress = locomotiveVM.Model.DigitalAddress,
                Manufacturer = locomotiveVM.Model.Manufacturer,
                ArticleNumber = locomotiveVM.Model.ArticleNumber,
                Series = locomotiveVM.Model.Series,
                ColorPrimary = locomotiveVM.Model.ColorPrimary,
                ColorSecondary = locomotiveVM.Model.ColorSecondary,
                IsPushing = locomotiveVM.Model.IsPushing,
                Details = locomotiveVM.Model.Details
            };

            ViewModel.SelectedTrain.Model.Locomotives.Add(locomotiveCopy);
            var newLocomotiveVM = new LocomotiveViewModel(locomotiveCopy);
            ViewModel.SelectedTrain.Locomotives.Add(newLocomotiveVM);
        }
    }

    private void LocomotiveListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void LocomotiveListView_Drop(object sender, DragEventArgs e)
    {
        // Handle Locomotive drop from library
        if (e.DataView.Properties.TryGetValue("Locomotive", out object locoObj) &&
            locoObj is Domain.Locomotive locomotive)
        {
            if (ViewModel.SelectedTrain != null)
            {
                // Create a copy to avoid modifying the library object
                var locomotiveCopy = new Domain.Locomotive
                {
                    Name = locomotive.Name,
                    DigitalAddress = locomotive.DigitalAddress,
                    Manufacturer = locomotive.Manufacturer,
                    ArticleNumber = locomotive.ArticleNumber,
                    Series = locomotive.Series,
                    ColorPrimary = locomotive.ColorPrimary,
                    ColorSecondary = locomotive.ColorSecondary,
                    IsPushing = locomotive.IsPushing,
                    Details = locomotive.Details
                };

                ViewModel.SelectedTrain.Model.Locomotives.Add(locomotiveCopy);
                var locomotiveVM = new LocomotiveViewModel(locomotiveCopy);
                ViewModel.SelectedTrain.Locomotives.Add(locomotiveVM);
            }
        }
    }

    private void GoodsWagonListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is GoodsWagonViewModel goodsWagonVM)
        {
            e.Data.Properties.Add("GoodsWagon", goodsWagonVM.Model);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(goodsWagonVM.Name);
        }
    }

    private void GoodsWagonLibrary_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // Add selected goods wagon to current train on double-click
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is GoodsWagonViewModel goodsWagonVM &&
            ViewModel.SelectedTrain != null &&
            goodsWagonVM.Model is Domain.GoodsWagon goodsWagon)
        {
            // Create a copy to avoid modifying the library object
            var wagonCopy = new Domain.GoodsWagon
            {
                Name = goodsWagon.Name,
                Cargo = goodsWagon.Cargo,
                Manufacturer = goodsWagon.Manufacturer,
                ArticleNumber = goodsWagon.ArticleNumber,
                Series = goodsWagon.Series,
                ColorPrimary = goodsWagon.ColorPrimary,
                ColorSecondary = goodsWagon.ColorSecondary,
                Details = goodsWagon.Details
            };

            ViewModel.SelectedTrain.Model.Wagons.Add(wagonCopy);
            var newWagonVM = new GoodsWagonViewModel(wagonCopy);
            ViewModel.SelectedTrain.Wagons.Add(newWagonVM);
        }
    }

    private void PassengerWagonListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is PassengerWagonViewModel passengerWagonVM)
        {
            e.Data.Properties.Add("PassengerWagon", passengerWagonVM.Model);
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Data.SetText(passengerWagonVM.Name);
        }
    }

    private void PassengerWagonLibrary_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // Add selected passenger wagon to current train on double-click
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is PassengerWagonViewModel passengerWagonVM &&
            ViewModel.SelectedTrain != null &&
            passengerWagonVM.Model is Domain.PassengerWagon passengerWagon)
        {
            // Create a copy to avoid modifying the library object
            var wagonCopy = new Domain.PassengerWagon
            {
                Name = passengerWagon.Name,
                WagonClass = passengerWagon.WagonClass,
                Manufacturer = passengerWagon.Manufacturer,
                ArticleNumber = passengerWagon.ArticleNumber,
                Series = passengerWagon.Series,
                ColorPrimary = passengerWagon.ColorPrimary,
                ColorSecondary = passengerWagon.ColorSecondary,
                Details = passengerWagon.Details
            };

            ViewModel.SelectedTrain.Model.Wagons.Add(wagonCopy);
            var newWagonVM = new PassengerWagonViewModel(wagonCopy);
            ViewModel.SelectedTrain.Wagons.Add(newWagonVM);
        }
    }

    private void WagonListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void WagonListView_Drop(object sender, DragEventArgs e)
    {
        // Handle GoodsWagon drop from library
        if (e.DataView.Properties.TryGetValue("GoodsWagon", out object goodsWagonObj) &&
            goodsWagonObj is Domain.GoodsWagon goodsWagon)
        {
            if (ViewModel.SelectedTrain != null)
            {
                // Create a copy to avoid modifying the library object
                var wagonCopy = new Domain.GoodsWagon
                {
                    Name = goodsWagon.Name,
                    Cargo = goodsWagon.Cargo,
                    Manufacturer = goodsWagon.Manufacturer,
                    ArticleNumber = goodsWagon.ArticleNumber,
                    Series = goodsWagon.Series,
                    ColorPrimary = goodsWagon.ColorPrimary,
                    ColorSecondary = goodsWagon.ColorSecondary,
                    Details = goodsWagon.Details
                };

                ViewModel.SelectedTrain.Model.Wagons.Add(wagonCopy);
                var wagonViewModel = new GoodsWagonViewModel(wagonCopy);
                ViewModel.SelectedTrain.Wagons.Add(wagonViewModel);
            }
        }
        // Handle PassengerWagon drop from library
        else if (e.DataView.Properties.TryGetValue("PassengerWagon", out object passengerWagonObj) &&
                 passengerWagonObj is Domain.PassengerWagon passengerWagon)
        {
            if (ViewModel.SelectedTrain != null)
            {
                // Create a copy to avoid modifying the library object
                var wagonCopy = new Domain.PassengerWagon
                {
                    Name = passengerWagon.Name,
                    WagonClass = passengerWagon.WagonClass,
                    Manufacturer = passengerWagon.Manufacturer,
                    ArticleNumber = passengerWagon.ArticleNumber,
                    Series = passengerWagon.Series,
                    ColorPrimary = passengerWagon.ColorPrimary,
                    ColorSecondary = passengerWagon.ColorSecondary,
                    Details = passengerWagon.Details
                };

                ViewModel.SelectedTrain.Model.Wagons.Add(wagonCopy);
                var wagonViewModel = new PassengerWagonViewModel(wagonCopy);
                ViewModel.SelectedTrain.Wagons.Add(wagonViewModel);
            }
        }
    }

    private void LocomotiveListView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Delete key removes selected locomotive from train
        if (e.Key == Windows.System.VirtualKey.Delete &&
            sender is ListView listView &&
            listView.SelectedItem is LocomotiveViewModel locomotiveVM &&
            ViewModel.SelectedTrain != null)
        {
            ViewModel.SelectedTrain.Model.Locomotives.Remove(locomotiveVM.Model);
            ViewModel.SelectedTrain.Locomotives.Remove(locomotiveVM);
            e.Handled = true;
        }
    }

    private void TrainWagonListView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Delete key removes selected wagon from train
        if (e.Key == Windows.System.VirtualKey.Delete &&
            sender is ListView listView &&
            listView.SelectedItem is WagonViewModel wagonVM &&
            ViewModel.SelectedTrain != null)
        {
            ViewModel.SelectedTrain.Model.Wagons.Remove(wagonVM.Model);
            ViewModel.SelectedTrain.Wagons.Remove(wagonVM);
            e.Handled = true;
        }
    }

    private void RemoveLocomotiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Remove locomotive from train via context menu
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is LocomotiveViewModel locomotiveVM &&
            ViewModel.SelectedTrain != null)
        {
            ViewModel.SelectedTrain.Model.Locomotives.Remove(locomotiveVM.Model);
            ViewModel.SelectedTrain.Locomotives.Remove(locomotiveVM);
        }
    }

    private void DuplicateLocomotiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Duplicate locomotive in train
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is LocomotiveViewModel locomotiveVM &&
            ViewModel.SelectedTrain != null)
        {
            var locomotiveCopy = new Domain.Locomotive
            {
                Name = locomotiveVM.Model.Name + " (Copy)",
                DigitalAddress = locomotiveVM.Model.DigitalAddress,
                Manufacturer = locomotiveVM.Model.Manufacturer,
                ArticleNumber = locomotiveVM.Model.ArticleNumber,
                Series = locomotiveVM.Model.Series,
                ColorPrimary = locomotiveVM.Model.ColorPrimary,
                ColorSecondary = locomotiveVM.Model.ColorSecondary,
                IsPushing = locomotiveVM.Model.IsPushing,
                Details = locomotiveVM.Model.Details
            };

            ViewModel.SelectedTrain.Model.Locomotives.Add(locomotiveCopy);
            var newVM = new LocomotiveViewModel(locomotiveCopy);
            ViewModel.SelectedTrain.Locomotives.Add(newVM);
        }
    }

    private void RemoveWagonMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Remove wagon from train via context menu
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is WagonViewModel wagonVM &&
            ViewModel.SelectedTrain != null)
        {
            ViewModel.SelectedTrain.Model.Wagons.Remove(wagonVM.Model);
            ViewModel.SelectedTrain.Wagons.Remove(wagonVM);
        }
    }

    private void DuplicateWagonMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Duplicate wagon in train
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is WagonViewModel wagonVM &&
            ViewModel.SelectedTrain != null)
        {
            Domain.Wagon wagonCopy;

            if (wagonVM.Model is Domain.PassengerWagon passengerWagon)
            {
                wagonCopy = new Domain.PassengerWagon
                {
                    Name = passengerWagon.Name + " (Copy)",
                    WagonClass = passengerWagon.WagonClass,
                    Manufacturer = passengerWagon.Manufacturer,
                    ArticleNumber = passengerWagon.ArticleNumber,
                    Series = passengerWagon.Series,
                    ColorPrimary = passengerWagon.ColorPrimary,
                    ColorSecondary = passengerWagon.ColorSecondary,
                    Details = passengerWagon.Details
                };
            }
            else if (wagonVM.Model is Domain.GoodsWagon goodsWagon)
            {
                wagonCopy = new Domain.GoodsWagon
                {
                    Name = goodsWagon.Name + " (Copy)",
                    Cargo = goodsWagon.Cargo,
                    Manufacturer = goodsWagon.Manufacturer,
                    ArticleNumber = goodsWagon.ArticleNumber,
                    Series = goodsWagon.Series,
                    ColorPrimary = goodsWagon.ColorPrimary,
                    ColorSecondary = goodsWagon.ColorSecondary,
                    Details = goodsWagon.Details
                };
            }
            else
            {
                return; // Unknown wagon type
            }

            ViewModel.SelectedTrain.Model.Wagons.Add(wagonCopy);

            WagonViewModel newVM = wagonCopy switch
            {
                Domain.PassengerWagon pw => new PassengerWagonViewModel(pw),
                Domain.GoodsWagon gw => new GoodsWagonViewModel(gw),
                _ => new WagonViewModel(wagonCopy)
            };

            ViewModel.SelectedTrain.Wagons.Add(newVM);
        }
    }

    #endregion
}