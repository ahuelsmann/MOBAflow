// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using SharedUI.ViewModel;

public partial class MainPage
{
    private readonly MainWindowViewModel _viewModel;
    private bool _isUpdatingFromBinding;

    public MainPage(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Subscribe to property changes to detect binding updates
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Track when binding is updating the switches to avoid feedback loops
        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected) ||
            e.PropertyName == nameof(MainWindowViewModel.IsTrackPowerOn))
        {
            _isUpdatingFromBinding = true;
            // Reset flag after UI update completes
            Dispatcher.Dispatch(() => _isUpdatingFromBinding = false);
        }
    }

    private async void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        // Skip if this toggle was triggered by binding update (not user action)
        if (_isUpdatingFromBinding)
            return;

        // Skip if value already matches (prevents duplicate calls)
        if (_viewModel.IsTrackPowerOn == e.Value)
            return;

        await _viewModel.SetTrackPowerCommand.ExecuteAsync(e.Value);
    }

    private async void ConnectionSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        // Skip if this toggle was triggered by binding update (not user action)
        if (_isUpdatingFromBinding)
            return;

        // Skip if value already matches (prevents duplicate calls)
        if (_viewModel.IsConnected == e.Value)
            return;

        if (e.Value)
            await _viewModel.ConnectCommand.ExecuteAsync(null);
        else
            await _viewModel.DisconnectCommand.ExecuteAsync(null);
    }
}



