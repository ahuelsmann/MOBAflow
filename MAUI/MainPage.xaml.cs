// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Smart;

using Moba.SharedUI.Interface;
using SharedUI.ViewModel;

public partial class MainPage : ContentPage
{
    private readonly CounterViewModel _viewModel;
    private readonly IUiDispatcher _uiDispatcher;

    public MainPage(CounterViewModel viewModel, IUiDispatcher uiDispatcher)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _uiDispatcher = uiDispatcher;
        BindingContext = _viewModel;

        // Subscribe to TargetReached event for showing alert
        _viewModel.TargetReached += OnTargetReached;
    }

    private void OnTargetReached(object? sender, InPortStatistic stat)
    {
        // Show alert on UI thread using IUiDispatcher
        _uiDispatcher.InvokeOnUi(async () =>
        {
            await DisplayAlertAsync(
                "🎉 Target Reached!",
                $"Track {stat.InPort} has completed {stat.Count} laps!\n\nLast lap: {stat.LastLapTimeFormatted}",
                "OK"
            );
        });
    }

    private async void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (_viewModel.SetTrackPowerCommand.CanExecute(e.Value))
        {
            await _viewModel.SetTrackPowerCommand.ExecuteAsync(e.Value);
        }
    }
}
