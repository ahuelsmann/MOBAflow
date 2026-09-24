// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Interface;
using SharedUI.ViewModel;

internal sealed partial class TimetablePage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<TimetablePage>? _logger;
    private bool? _isCompactLayout;

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
        SizeChanged += OnSizeChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (_isCompactLayout != true) RestoreColumnWidths();
        UpdateResponsiveLayout();
        try
        {
            await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Load timetable page failed");
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded) UpdateResponsiveLayout();
    }

    private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded) UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        var compact = ActualWidth < 1000;
        var surroundingHeight = PageHeader.ActualHeight + ValidationPanel.ActualHeight + StatusMessage.ActualHeight
            + PageContent.Padding.Top + PageContent.Padding.Bottom + (PageContent.RowSpacing * 3);
        // Keep the list viewport bounded and reachable when the page needs to scroll.
        BoardGrid.Height = Math.Max(compact ? 800 : 360, ActualHeight - surroundingHeight);
        if (compact == _isCompactLayout) return;

        if (compact && _isCompactLayout == false) RememberColumnWidths();
        _isCompactLayout = compact;
        ServicesColumn.MinWidth = compact ? 0 : 280;
        DetailsColumn.MinWidth = compact ? 0 : 360;
        BoardGrid.ColumnSpacing = compact ? 0 : 12;
        BoardGrid.RowSpacing = compact ? 16 : 0;
        ServicesRow.Height = new GridLength(compact ? 2 : 1, GridUnitType.Star);
        DetailsRow.Height = compact ? new GridLength(3, GridUnitType.Star) : new GridLength(0);
        Grid.SetRow(DetailsPanel, compact ? 1 : 0);
        Grid.SetColumn(DetailsPanel, compact ? 0 : 2);
        BoardSplitter.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        if (compact)
        {
            ServicesColumn.Width = new GridLength(1, GridUnitType.Star);
            DetailsColumn.Width = new GridLength(0);
        }
        else
        {
            RestoreColumnWidths();
        }
    }

    private void RestoreColumnWidths()
    {
        var layout = _settings.Layout.TimetablePage;
        ServicesColumn.Width = new GridLength(Math.Max(0.1, layout.ServicesColumnStarValue), GridUnitType.Star);
        DetailsColumn.Width = new GridLength(Math.Max(0.1, layout.DetailsColumnStarValue), GridUnitType.Star);
    }

    private void RememberColumnWidths()
    {
        if (_isCompactLayout != false) return;
        var total = ServicesColumn.ActualWidth + DetailsColumn.ActualWidth;
        if (total <= 0) return;
        _settings.Layout.TimetablePage.ServicesColumnStarValue = ServicesColumn.ActualWidth / total;
        _settings.Layout.TimetablePage.DetailsColumnStarValue = DetailsColumn.ActualWidth / total;
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        try
        {
            RememberColumnWidths();
            if (_settingsService is not null) await _settingsService.SaveSettingsAsync(_settings);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist timetable layout on unload failed");
        }
    }
}
