// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using SharedUI.ViewModel;

public partial class SignalBoxPage
{
    private readonly IServiceProvider _serviceProvider;
    private MauiViewModel? _viewModel;
    private Task? _viewModelInitializationTask;

    public SignalBoxPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActivateTab();
    }

    public void ActivateTab()
    {
        _viewModel ??= _serviceProvider.GetRequiredService<MauiViewModel>();
        BindingContext = _viewModel;
        _viewModel.SetSignalBoxTabActive(true);
        _viewModelInitializationTask ??= _viewModel.InitializeAsync();
    }

    public void DeactivateTab()
    {
        _viewModel?.SetSignalBoxTabActive(false);
    }
}