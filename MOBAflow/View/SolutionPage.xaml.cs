// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Interface;
using SharedUI.ViewModel;

/// <summary>
/// Solution page displaying projects list with properties panel.
/// DeleteProject logic moved to MainWindowViewModel with IDialogService.
/// </summary>
internal sealed partial class SolutionPage
{
    public MainWindowViewModel ViewModel { get; }

    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<SolutionPage>? _logger;

    private GridLength _projectsExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(2.2, GridUnitType.Star);

    public SolutionPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<SolutionPage>? logger = null)
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
        if (e.PropertyName == nameof(ViewModel.IsProjectListExpanded))
        {
            ApplyColumnState(ViewModel.IsProjectListExpanded, ColProjects, ref _projectsExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsProjectPropertiesExpanded))
        {
            ApplyColumnState(ViewModel.IsProjectPropertiesExpanded, ColProperties, ref _propertiesExpandedWidth);
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.SolutionPage;

        _projectsExpandedWidth = ToStarGridLength(layout.ProjectListColumnStarValue, _projectsExpandedWidth);
        _propertiesExpandedWidth = ToStarGridLength(layout.PropertiesColumnStarValue, _propertiesExpandedWidth);

        RestoreColumnState(layout.IsProjectListExpanded, ColProjects, _projectsExpandedWidth);
        RestoreColumnState(layout.IsPropertiesExpanded, ColProperties, _propertiesExpandedWidth);

        if (ViewModel.IsProjectListExpanded != layout.IsProjectListExpanded)
        {
            ViewModel.IsProjectListExpanded = layout.IsProjectListExpanded;
        }
        if (ViewModel.IsProjectPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsProjectPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.SolutionPage;

        layout.IsProjectListExpanded = ViewModel.IsProjectListExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsProjectPropertiesExpanded;
        layout.ProjectListColumnStarValue = GetCurrentStarValue(ColProjects, _projectsExpandedWidth);
        layout.PropertiesColumnStarValue = GetCurrentStarValue(ColProperties, _propertiesExpandedWidth);
    }

    private static void ApplyColumnState(bool isExpanded, ColumnDefinition column, ref GridLength rememberedWidth)
    {
        if (!isExpanded)
        {
            if (!column.Width.IsAuto)
            {
                rememberedWidth = column.Width;
            }

            column.Width = GridLength.Auto;
        }
        else
        {
            column.Width = rememberedWidth;
        }
    }

    private static GridLength ToStarGridLength(double starValue, GridLength fallback)
    {
        return starValue > 0
            ? new GridLength(starValue, GridUnitType.Star)
            : fallback;
    }

    private static void RestoreColumnState(bool isExpanded, ColumnDefinition column, GridLength rememberedWidth)
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
}
