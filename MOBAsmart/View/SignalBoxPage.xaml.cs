// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using SharedUI.ViewModel;

public partial class SignalBoxPage
{
    private readonly MauiViewModel _viewModel;
    private Task? _viewModelInitializationTask;

    public SignalBoxPage(MauiViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = _viewModel;

        InitializeComponent();
    }

    public void ActivateTab()
    {

        Dispatcher.DispatchAsync(async () =>
        {

            _viewModelInitializationTask ??= _viewModel.InitializeAsync();

            await _viewModelInitializationTask.ConfigureAwait(false);

        });
    }

    public void DeactivateTab()
    {

    }
}