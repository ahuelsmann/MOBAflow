// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Service;

using SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// Settings are auto-saved immediately after changes.
/// </summary>
public sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }

    public SettingsPage(MainWindowViewModel viewModel, IThemeProvider themeProvider)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // TODO: ThemeSelectorControl Umbenennung zu SkinSelector - siehe issue #XXX
        // var skinSelector = new SkinSelector(themeProvider);
        // ThemeSelectorContainer.Children.Add(skinSelector);
    }

    private void CopyIpToClipboard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.LocalIpAddress);
        Clipboard.SetContent(dataPackage);
    }
}