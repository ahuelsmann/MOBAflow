// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using Common.Security;

using SharedUI.ViewModel;

using ZXing.Net.Maui;

public partial class PairingPage
{
    private readonly MauiViewModel _viewModel;
    private bool _scanHandled;

    public PairingPage(MauiViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _scanHandled = false;
        _viewModel.ClearPairingStatus();

        var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(true);
        if (cameraStatus != PermissionStatus.Granted)
        {
            cameraStatus = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(true);
        }

        if (cameraStatus != PermissionStatus.Granted)
        {
            _viewModel.PairingStatusText = "Camera permission is required to scan the QR code.";
            _viewModel.HasPairingError = true;
            BarcodeReader.IsDetecting = false;
            return;
        }

        BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        BarcodeReader.IsDetecting = false;
        base.OnDisappearing();
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_scanHandled)
        {
            return;
        }

        var raw = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(raw) || !MobaPairingPayload.TryParse(raw, out var payload) || payload == null)
        {
            return;
        }

        _scanHandled = true;
        BarcodeReader.IsDetecting = false;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (await _viewModel.TryApplyPairingPayloadAsync(payload).ConfigureAwait(true))
            {
                await ClosePageAsync().ConfigureAwait(true);
            }
            else
            {
                _scanHandled = false;
                BarcodeReader.IsDetecting = true;
            }
        });
    }

    private async void OnManualPairingClicked(object? sender, EventArgs e)
    {
        await _viewModel.ApplyManualPairingCommand.ExecuteAsync(null).ConfigureAwait(true);
        if (!_viewModel.HasPairingError)
        {
            await ClosePageAsync().ConfigureAwait(true);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await ClosePageAsync().ConfigureAwait(true);
    }

    private static async Task ClosePageAsync()
    {
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation != null)
        {
            await navigation.PopModalAsync().ConfigureAwait(false);
        }
    }
}
