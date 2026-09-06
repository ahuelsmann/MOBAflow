// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using Common.Extension;
using SharedUI.ViewModel;
using ZXing.Net.Maui;

public partial class PairingPage
{
    private readonly RemotePairingViewModel _viewModel;
    private Task? _initializationTask;

    public PairingPage(RemotePairingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        BindingContext = _viewModel;
    }

    public void ActivateTab()
    {
        _initializationTask ??= _viewModel.InitializeAsync();
        _initializationTask.Observe();
    }

    private void PairingQrScanner_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        _ = sender;
        var payload = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_viewModel.ScanQrCodeCommand.CanExecute(payload))
            {
                _viewModel.ScanQrCodeCommand.Execute(payload);
            }
        });
    }
}