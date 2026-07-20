// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using SharedUI.Interface;
using SharedUI.ViewModel;

internal sealed partial class TimetablePage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<TimetablePage>? _logger;

    public TimetablePageViewModel ViewModel { get; }

    public TimetablePage(
        TimetablePageViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<TimetablePage>? logger = null)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        var layout = _settings.Layout.TimetablePage;
        ServicesColumn.Width = new GridLength(Math.Max(0.1, layout.ServicesColumnStarValue), GridUnitType.Star);
        DetailsColumn.Width = new GridLength(Math.Max(0.1, layout.DetailsColumnStarValue), GridUnitType.Star);
        try
        {
            await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Load timetable page failed");
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        try
        {
            var total = ServicesColumn.ActualWidth + DetailsColumn.ActualWidth;
            if (total > 0)
            {
                _settings.Layout.TimetablePage.ServicesColumnStarValue = ServicesColumn.ActualWidth / total;
                _settings.Layout.TimetablePage.DetailsColumnStarValue = DetailsColumn.ActualWidth / total;
            }

            if (_settingsService is not null) await _settingsService.SaveSettingsAsync(_settings);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist timetable layout on unload failed");
        }
    }
}
