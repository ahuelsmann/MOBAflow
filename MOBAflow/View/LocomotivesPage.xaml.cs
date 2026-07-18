// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

using SharedUI.Interface;

internal sealed partial class LocomotivesPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<LocomotivesPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    public LocomotiveManagementViewModel Management { get; }

    private double _listExpandedWidth = 250;
    private GridLength _propertiesExpandedWidth = new(1, GridUnitType.Star);

    public LocomotivesPage(
        MainWindowViewModel viewModel,
        LocomotiveManagementViewModel management,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<LocomotivesPage>? logger = null)
    {
        ViewModel = viewModel;
        Management = management;
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
        RefreshManagement();
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
        if (e.PropertyName == nameof(ViewModel.IsLocomotivesListExpanded))
        {
            ApplyColumnState(ViewModel.IsLocomotivesListExpanded, ColList, 0, ref _listExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsLocomotivesPropertiesExpanded))
        {
            ApplyStarColumnState(ViewModel.IsLocomotivesPropertiesExpanded, ColProperties, ref _propertiesExpandedWidth);
        }
        else if (e.PropertyName is nameof(ViewModel.SelectedProject) or nameof(ViewModel.SelectedLocomotive))
        {
            RefreshManagement();
        }
    }

    private void RefreshManagement()
        => Management.SetContext(ViewModel.SelectedProject?.Model, ViewModel.SelectedLocomotive?.Model);

    private void RestoreLayout()
    {
        var layout = _settings.Layout.LocomotivesPage;

        if (layout.ListColumnWidth > 0)
        {
            _listExpandedWidth = layout.ListColumnWidth;
            ViewModel.LayoutColumnWidths.SetColumnWidth("LocomotivesPage", 0, _listExpandedWidth);
        }
        if (layout.PropertiesColumnStarValue > 0)
        {
            _propertiesExpandedWidth = new GridLength(layout.PropertiesColumnStarValue, GridUnitType.Star);
        }
        ViewModel.LayoutColumnWidths.ClearColumnWidth("LocomotivesPage", 2, _settings.Layout);

        RestoreColumnState(layout.IsListExpanded, ColList, 0, ref _listExpandedWidth);
        RestoreStarColumnState(layout.IsPropertiesExpanded, ColProperties, _propertiesExpandedWidth);

        if (ViewModel.IsLocomotivesListExpanded != layout.IsListExpanded)
        {
            ViewModel.IsLocomotivesListExpanded = layout.IsListExpanded;
        }
        if (ViewModel.IsLocomotivesPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsLocomotivesPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.LocomotivesPage;

        layout.IsListExpanded = ViewModel.IsLocomotivesListExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsLocomotivesPropertiesExpanded;

        layout.ListColumnWidth = ViewModel.IsLocomotivesListExpanded
            ? GetCurrentPixelWidth(ColList, _listExpandedWidth)
            : _listExpandedWidth;
        layout.PropertiesColumnStarValue = GetCurrentStarValue(ColProperties, _propertiesExpandedWidth);
        ViewModel.LayoutColumnWidths.SetColumnWidth("LocomotivesPage", 0, layout.ListColumnWidth);
        _settings.Layout.ColumnWidths["LocomotivesPage:0"] = layout.ListColumnWidth;
        ViewModel.LayoutColumnWidths.ClearColumnWidth("LocomotivesPage", 2, _settings.Layout);
    }

    private void ApplyColumnState(bool isExpanded, ColumnDefinition column, int columnIndex, ref double rememberedWidth)
    {
        if (!isExpanded)
        {
            rememberedWidth = GetCurrentPixelWidth(column, rememberedWidth);
            ViewModel.LayoutColumnWidths.SetColumnWidth("LocomotivesPage", columnIndex, rememberedWidth);
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
        var configuredWidth = ViewModel.LayoutColumnWidths.GetColumnWidth("LocomotivesPage", columnIndex);
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
