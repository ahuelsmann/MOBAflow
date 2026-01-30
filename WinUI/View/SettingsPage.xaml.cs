// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

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

    public SettingsPage(MainWindowViewModel viewModel, ISkinProvider skinProvider)
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

    private void AzureSpeechSetupButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is Frame frame)
            {
                frame.Navigate(typeof(HelpPage), "Azure Speech Setup");
                return;
            }
            parent = (parent as Microsoft.UI.Xaml.FrameworkElement)?.Parent;
        }
    }
}