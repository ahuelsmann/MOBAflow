// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// </summary>
public sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }

    public SettingsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}