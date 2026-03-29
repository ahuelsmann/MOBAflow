// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Converter;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Service;
using SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// Settings are auto-saved immediately after changes.
/// </summary>
internal sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }

    public SettingsPage(MainWindowViewModel viewModel)
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
        RecordSettingsSectionUsage("REST API");
        PhotoPathToImageConverter.SetPhotoBasePath(path);
    }

    private async void ResetLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = App.Current.Services.GetService(typeof(ISettingsService)) as ISettingsService;
        var settings = App.Current.Services.GetService(typeof(AppSettings)) as AppSettings ?? settingsService?.GetSettings();

        if (settingsService == null || settings == null)
        {
            ShowLayoutResetStatus("Layout reset unavailable", "The settings service is not available.", InfoBarSeverity.Error);
            return;
        }

        try
        {
            ResetPersistedLayouts(settings);
            await settingsService.SaveSettingsAsync(settings);
            ResetSectionLayoutToDefaults();
            ShowLayoutResetStatus("Layouts reset", "Saved layouts were reset. Reopen pages to apply their default layout.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowLayoutResetStatus("Layout reset failed", ex.Message, InfoBarSeverity.Error);
        }
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

    private static void ResetPersistedLayouts(AppSettings settings)
    {
        settings.Layout = new LayoutSettings
        {
            TabVisibility = settings.Layout.TabVisibility
        };
    }

    private void ShowLayoutResetStatus(string title, string message, InfoBarSeverity severity)
    {
        LayoutResetInfoBar.IsOpen = false;
        LayoutResetInfoBar.Title = title;
        LayoutResetInfoBar.Message = message;
        LayoutResetInfoBar.Severity = severity;
        LayoutResetInfoBar.IsOpen = true;
    }
}