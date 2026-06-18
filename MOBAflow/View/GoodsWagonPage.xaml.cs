// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

using SharedUI.Interface;

internal sealed partial class GoodsWagonPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<GoodsWagonPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    private double _listExpandedWidth = 250;
    private GridLength _propertiesExpandedWidth = new(1, GridUnitType.Star);

    public GoodsWagonPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<GoodsWagonPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsGoodsWagonListExpanded))
        {
            ApplyColumnState(ViewModel.IsGoodsWagonListExpanded, ColList, 0, ref _listExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsGoodsWagonPropertiesExpanded))
        {
            ApplyStarColumnState(ViewModel.IsGoodsWagonPropertiesExpanded, ColProperties, ref _propertiesExpandedWidth);
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.GoodsWagonPage;

        if (layout.ListColumnWidth > 0)
        {
            _listExpandedWidth = layout.ListColumnWidth;
            ViewModel.LayoutColumnWidths.SetColumnWidth("GoodsWagonPage", 0, _listExpandedWidth);
        }
        if (layout.PropertiesColumnStarValue > 0)
        {
            _propertiesExpandedWidth = new GridLength(layout.PropertiesColumnStarValue, GridUnitType.Star);
        }
        ViewModel.LayoutColumnWidths.ClearColumnWidth("GoodsWagonPage", 2, _settings.Layout);

        RestoreColumnState(layout.IsListExpanded, ColList, 0, ref _listExpandedWidth);
        RestoreStarColumnState(layout.IsPropertiesExpanded, ColProperties, _propertiesExpandedWidth);

        if (ViewModel.IsGoodsWagonListExpanded != layout.IsListExpanded)
        {
            ViewModel.IsGoodsWagonListExpanded = layout.IsListExpanded;
        }
        if (ViewModel.IsGoodsWagonPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsGoodsWagonPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.GoodsWagonPage;

        layout.IsListExpanded = ViewModel.IsGoodsWagonListExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsGoodsWagonPropertiesExpanded;

        layout.ListColumnWidth = ViewModel.IsGoodsWagonListExpanded
            ? GetCurrentPixelWidth(ColList, _listExpandedWidth)
            : _listExpandedWidth;
        layout.PropertiesColumnStarValue = GetCurrentStarValue(ColProperties, _propertiesExpandedWidth);
        ViewModel.LayoutColumnWidths.SetColumnWidth("GoodsWagonPage", 0, layout.ListColumnWidth);
        _settings.Layout.ColumnWidths["GoodsWagonPage:0"] = layout.ListColumnWidth;
        ViewModel.LayoutColumnWidths.ClearColumnWidth("GoodsWagonPage", 2, _settings.Layout);
    }

    private void ApplyColumnState(bool isExpanded, ColumnDefinition column, int columnIndex, ref double rememberedWidth)
    {
        if (!isExpanded)
        {
            rememberedWidth = GetCurrentPixelWidth(column, rememberedWidth);
            ViewModel.LayoutColumnWidths.SetColumnWidth("GoodsWagonPage", columnIndex, rememberedWidth);
            column.Width = GridLength.Auto;
            return;
        }

        column.Width = new GridLength(rememberedWidth);
    }

    private static void ApplyStarColumnState(bool isExpanded, ColumnDefinition column, ref GridLength rememberedWidth)
    {
        if (!isExpanded)
        {
            if (column.Width.IsStar)
            {
                rememberedWidth = column.Width;
            }

            column.Width = GridLength.Auto;
            return;
        }

        column.Width = rememberedWidth;
    }

    private void RestoreColumnState(bool isExpanded, ColumnDefinition column, int columnIndex, ref double rememberedWidth)
    {
        var configuredWidth = ViewModel.LayoutColumnWidths.GetColumnWidth("GoodsWagonPage", columnIndex);
        if (configuredWidth > 0)
        {
            rememberedWidth = configuredWidth;
        }

        column.Width = isExpanded ? new GridLength(rememberedWidth) : GridLength.Auto;
    }

    private static void RestoreStarColumnState(bool isExpanded, ColumnDefinition column, GridLength rememberedWidth)
    {
        column.Width = isExpanded ? rememberedWidth : GridLength.Auto;
    }

    private static double GetCurrentStarValue(ColumnDefinition column, GridLength fallback)
    {
        if (column.Width.IsStar)
        {
            return column.Width.Value;
        }

        return fallback.IsStar ? fallback.Value : 1;
    }

    private static double GetCurrentPixelWidth(ColumnDefinition column, double fallback)
    {
        if (column.Width.IsAbsolute && column.Width.Value > 0)
        {
            return column.Width.Value;
        }

        return column.ActualWidth > 0 ? column.ActualWidth : fallback;
    }
}