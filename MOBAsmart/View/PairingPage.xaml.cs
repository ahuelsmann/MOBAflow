// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using Common.Extension;
using SharedUI.ViewModel;

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
}