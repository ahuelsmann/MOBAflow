// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using SharedUI.ViewModel;

public partial class ControlPage
{
    private readonly MauiViewModel _mauiViewModel;
    private Task? _runtimeInitializationTask;

    public ControlPage(TrainControlViewModel viewModel, MauiViewModel mauiViewModel)
    {
        _mauiViewModel = mauiViewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ActivateTab()
    {
        Dispatcher.DispatchAsync(async () =>
        {
            _runtimeInitializationTask ??= _mauiViewModel.InitializeAsync();
            await _runtimeInitializationTask.ConfigureAwait(false);
        });
    }

    public void DeactivateTab()
    {
    }
}
