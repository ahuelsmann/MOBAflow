// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Settings page for application-wide configuration.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage(SettingsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}