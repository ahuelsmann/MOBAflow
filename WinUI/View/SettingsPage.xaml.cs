// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;

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

    private void CopyIpToClipboard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.LocalIpAddress);
        Clipboard.SetContent(dataPackage);
    }
}