// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;
using Domain;

public sealed partial class TrainsPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public TrainsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void LocomotivesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Locomotive locomotive)
        {
            ViewModel.SelectedLocomotive = new LocomotiveViewModel(locomotive);
        }
    }

    private void PassengerWagonsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is PassengerWagon wagon)
        {
            ViewModel.SelectedPassengerWagon = new PassengerWagonViewModel(wagon);
        }
    }

    private void GoodsWagonsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is GoodsWagon wagon)
        {
            ViewModel.SelectedGoodsWagon = new GoodsWagonViewModel(wagon);
        }
    }
}
