// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using SharedUI.ViewModel;
using Microsoft.Maui.Controls;

// ReSharper disable once PartialTypeWithSinglePart
public partial class MainPage
{
    public MauiViewModel ViewModel { get; }

    public MainPage(MauiViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = ViewModel;
        InitializeComponent();
    }

    private async void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(e.Value);
    }

    private async void ConnectionSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        if (e.Value)
            await ViewModel.ConnectCommand.ExecuteAsync(null);
        else
            await ViewModel.DisconnectCommand.ExecuteAsync(null);
    }
}







