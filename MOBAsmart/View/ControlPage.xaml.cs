// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using System.Windows.Input;

using SharedUI.ViewModel;

public partial class ControlPage
{
    private readonly MauiViewModel _mauiViewModel;
    private Task? _runtimeInitializationTask;

    public ICommand ChangeLocomotiveCommand { get; }

    public event EventHandler? NavigateToEnginesTabRequested;

    public ControlPage(TrainControlViewModel viewModel, MauiViewModel mauiViewModel)
    {
        _mauiViewModel = mauiViewModel;

        ChangeLocomotiveCommand = new Command(() => NavigateToEnginesTabRequested?.Invoke(this, EventArgs.Empty));

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
