// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public SettingsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedIndex >= 0)
        {
            var theme = comboBox.SelectedIndex switch
            {
                0 => ElementTheme.Light,
                1 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            // Apply theme to page content
            if (Content is FrameworkElement contentRoot)
            {
                contentRoot.RequestedTheme = theme;
            }
        }
    }
}
