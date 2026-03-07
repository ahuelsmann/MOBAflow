// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Windowing;
using Microsoft.Windows.Storage.Pickers;

using Service;

using SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;

using Converter;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// Settings are auto-saved immediately after changes.
/// </summary>
[NavigationItem(
    Tag = "settings",
    Title = "Settings",
    Icon = "\uE115",
    Category = NavigationCategory.Help,
    Order = 30,
    FeatureToggleKey = "IsSettingsPageAvailable",
    BadgeLabelKey = "SettingsPageLabel")]
internal sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }

    public SettingsPage(MainWindowViewModel viewModel, ISkinProvider skinProvider)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void CopyIpToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.LocalIpAddress);
        Clipboard.SetContent(dataPackage);
    }

    private async void BrowsePhotoFolder_Click(object sender, RoutedEventArgs e)
    {
        var window = App.MainWindow;
        if (window == null) return;

        var picker = new FolderPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        var path = folder.Path;
        ViewModel.PhotoStoragePath = path;
        PhotoPathToImageConverter.SetPhotoBasePath(path);
    }

    private void AzureSpeechSetupButton_Click(object sender, RoutedEventArgs e)
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is Frame frame)
            {
                frame.Navigate(typeof(HelpPage), "Azure Speech Setup");
                return;
            }
            parent = (parent as FrameworkElement)?.Parent;
        }
    }
}