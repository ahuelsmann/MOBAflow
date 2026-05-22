// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Converter;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using Common.Extension;
using SharedUI.Interface;
using SharedUI.Shell;
using SharedUI.ViewModel;

using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// Settings are auto-saved immediately after changes.
/// </summary>
internal sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }
    private readonly ISettingsService? _settingsService;
    private readonly AppSettings? _settings;
    private readonly ILogger<SettingsPage>? _logger;
    private readonly INavigationService? _navigationService;

    public SettingsPage(
        MainWindowViewModel viewModel,
        ISettingsService? settingsService = null,
        AppSettings? settings = null,
        ILogger<SettingsPage>? logger = null,
        INavigationService? navigationService = null)
    {
        ViewModel = viewModel;
        _settingsService = settingsService;
        _settings = settings;
        _logger = logger;
        _navigationService = navigationService;
        InitializeComponent();
    }

    private void CopyIpToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.LocalIpAddress);
        Clipboard.SetContent(dataPackage);
    }

    private void BrowsePhotoFolder_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleBrowsePhotoFolderAsync().Observe(ex => _logger?.LogWarning(ex, "Browse photo folder failed"));
    }

    private async Task HandleBrowsePhotoFolderAsync()
    {
        try
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
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Browse photo folder failed");
        }
    }

    private void ResetLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleResetLayoutButtonAsync().Observe(ex => _logger?.LogWarning(ex, "Reset layout failed"));
    }

    private async Task HandleResetLayoutButtonAsync()
    {
        var settingsService = _settingsService;
        var settings = _settings ?? settingsService?.GetSettings();

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
        _ = sender;
        _ = e;
        NavigateToAzureSpeechSetupAsync().Observe(ex => _logger?.LogWarning(ex, "Navigate to Azure Speech setup failed"));
    }

    private async Task NavigateToAzureSpeechSetupAsync()
    {
        if (_navigationService != null)
        {
            await _navigationService.NavigateToAsync("help", "Azure Speech Setup");
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