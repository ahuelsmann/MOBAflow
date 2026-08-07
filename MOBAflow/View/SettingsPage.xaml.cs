// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Converter;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Moba.WinUI.Service;
using Moba.WinUI.ViewModel;
using SharedUI.Interface;
using SharedUI.Shell;
using SharedUI.ViewModel;

/// <summary>
/// Settings page for application-wide configuration.
/// Uses MainWindowViewModel.Settings for data binding.
/// Settings are auto-saved immediately after changes.
/// </summary>
internal sealed partial class SettingsPage
{
    public MainWindowViewModel ViewModel { get; }
    public RestApiPairingViewModel PairingViewModel { get; }
    private readonly ISettingsService? _settingsService;
    private readonly AppSettings? _settings;
    private readonly ILogger<SettingsPage>? _logger;
    private readonly INavigationService? _navigationService;
    public SettingsPage(
        MainWindowViewModel viewModel,
        RestApiPairingViewModel pairingViewModel,
        ISettingsService? settingsService = null,
        AppSettings? settings = null,
        ILogger<SettingsPage>? logger = null,
        INavigationService? navigationService = null)
    {
        ViewModel = viewModel;
        PairingViewModel = pairingViewModel;
        _settingsService = settingsService;
        _settings = settings;
        _logger = logger;
        _navigationService = navigationService;
        InitializeComponent();
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

    private void BrowsePiperExecutable_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleBrowsePiperFileAsync(".exe", path => ViewModel.PiperExecutablePath = path).Observe(ex => _logger?.LogWarning(ex, "Browse Piper executable failed"));
    }

    private void BrowsePiperModel_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleBrowsePiperFileAsync(".onnx", path => ViewModel.PiperModelPath = path).Observe(ex => _logger?.LogWarning(ex, "Browse Piper model failed"));
    }

    private void BrowsePiperConfig_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleBrowsePiperFileAsync(".json", path => ViewModel.PiperConfigPath = path).Observe(ex => _logger?.LogWarning(ex, "Browse Piper config failed"));
    }

    private static async Task HandleBrowsePiperFileAsync(string fileType, Action<string> applyPath)
    {
        var window = App.MainWindow;
        if (window == null) return;
        var picker = new FileOpenPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(fileType);
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;
        applyPath(file.Path);
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