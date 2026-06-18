// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using SharedUI.ViewModel;

public partial class ControlPage
{
    private readonly MauiViewModel _mauiViewModel;
    private Task? _runtimeInitializationTask;

    public ControlPage(
        TrainControlViewModel viewModel,
        MauiViewModel mauiViewModel)
    {
        _mauiViewModel = mauiViewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActivateTab();
    }

    public void ActivateTab()
    {
        _runtimeInitializationTask ??= _mauiViewModel.InitializeAsync();
        if (BindingContext is TrainControlViewModel viewModel)
        {
            viewModel.ResumeUpdates();
        }
    }

    public void DeactivateTab()
    {
        if (BindingContext is TrainControlViewModel viewModel)
        {
            viewModel.PauseUpdates();
        }
    }
}