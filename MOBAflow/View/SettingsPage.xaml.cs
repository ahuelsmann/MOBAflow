// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Common.Security;
using Converter;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Moba.WinUI.Service;
using SharedUI.Interface;
using SharedUI.Shell;
using SharedUI.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

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
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.LocalIpAddress)
                or nameof(MainWindowViewModel.RestApiPort)
                or nameof(MainWindowViewModel.RestApiApiKey))
            {
                RefreshPairingQrImage();
            }
        };
        Loaded += async (_, _) => await RefreshPairingQrImageWithRetryAsync().ConfigureAwait(true);
    }

    private async Task RefreshPairingQrImageWithRetryAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await RefreshPairingQrImageAsync().ConfigureAwait(true);
            if (PairingQrImage.Source != null)
            {
                return;
            }

            if (attempt < 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
            }
        }
    }

    private async Task RefreshPairingQrImageAsync()
    {
        var ip = ViewModel.LocalIpAddress;
        if (string.IsNullOrWhiteSpace(ip)
            || ip.Equals("No network connection", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(ViewModel.RestApiApiKey)
            || ViewModel.RestApiPort <= 0)
        {
            PairingQrImage.Source = null;
            return;
        }

        var payload = MobaPairingPayload.Create(ip, ViewModel.RestApiPort, ViewModel.RestApiApiKey);
        var png = MobaPairingQrEncoder.TryCreatePng(payload.ToJson());
        if (png == null || png.Length == 0)
        {
            PairingQrImage.Source = null;
            return;
        }

        using var stream = new MemoryStream(png);
        var image = new BitmapImage();
        await image.SetSourceAsync(stream.AsRandomAccessStream());
        PairingQrImage.Source = image;
    }

    private void RefreshPairingQrImage()
    {
        _ = RefreshPairingQrImageAsync();
    }

    private void CopyIpToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.LocalIpAddress);
        Clipboard.SetContent(dataPackage);
    }

    private void CopyApiKeyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.RestApiApiKey);
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