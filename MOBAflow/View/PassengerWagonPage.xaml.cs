// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Domain.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Moba.SharedUI.ViewModel;
using SharedUI.Interface;

internal sealed partial class PassengerWagonPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<PassengerWagonPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    public RollingStockMaintenanceViewModel Maintenance { get; }

    private double _listExpandedWidth = 250;
    private double _propertiesExpandedStarValue = 1;

    public PassengerWagonPage(
        MainWindowViewModel viewModel,
        RollingStockMaintenanceViewModel maintenance,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<PassengerWagonPage>? logger = null)
    {
        ViewModel = viewModel;
        Maintenance = maintenance;
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
        Maintenance.Activate();
        RefreshMaintenance();
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        Maintenance.Deactivate();
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
        if (e.PropertyName == nameof(ViewModel.IsPassengerWagonListExpanded))
        {
            ApplyListColumnState();
        }
        else if (e.PropertyName == nameof(ViewModel.IsPassengerWagonPropertiesExpanded))
        {
            ApplyPropertiesColumnState();
        }
        else if (e.PropertyName is nameof(ViewModel.SelectedProject)
                 or nameof(ViewModel.SelectedPassengerWagon)
                 or nameof(ViewModel.PassengerWagonSearchText))
        {
            RefreshMaintenance();
        }
    }

    private void ApplyListColumnState()
    {
        if (!ViewModel.IsPassengerWagonListExpanded)
        {
            if (ColList.Width.IsAbsolute)
                _listExpandedWidth = ColList.Width.Value;
            ColList.Width = GridLength.Auto;
            return;
        }

        ColList.Width = new GridLength(_listExpandedWidth);
    }

    private void ApplyPropertiesColumnState()
    {
        if (!ViewModel.IsPassengerWagonPropertiesExpanded)
        {
            if (ColProperties.Width.IsStar)
                _propertiesExpandedStarValue = ColProperties.Width.Value;
            ColProperties.Width = GridLength.Auto;
            return;
        }

        ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
    }

    private void RefreshMaintenance()
        => Maintenance.SetContext(
            ViewModel.SelectedProject,
            TrainVehicleKind.PassengerWagon,
            ViewModel.SelectedPassengerWagon,
            ViewModel.PassengerWagonSearchText);

    private void RestoreLayout()
    {
        var layout = _settings.Layout.PassengerWagonPage;

        if (layout.ListColumnWidth > 0)
        {
            _listExpandedWidth = layout.ListColumnWidth;
        }
        if (layout.PropertiesColumnStarValue > 0)
        {
            _propertiesExpandedStarValue = layout.PropertiesColumnStarValue;
        }

        if (layout.IsListExpanded)
        {
            ColList.Width = new GridLength(_listExpandedWidth);
        }
        else
        {
            ColList.Width = GridLength.Auto;
        }

        if (layout.IsPropertiesExpanded)
        {
            ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
        }
        else
        {
            ColProperties.Width = GridLength.Auto;
        }

        if (ViewModel.IsPassengerWagonListExpanded != layout.IsListExpanded)
        {
            ViewModel.IsPassengerWagonListExpanded = layout.IsListExpanded;
        }
        if (ViewModel.IsPassengerWagonPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsPassengerWagonPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.PassengerWagonPage;

        layout.IsListExpanded = ViewModel.IsPassengerWagonListExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsPassengerWagonPropertiesExpanded;

        if (ColList.Width.IsAbsolute)
        {
            layout.ListColumnWidth = ColList.Width.Value;
        }
        else if (!ViewModel.IsPassengerWagonListExpanded)
        {
            layout.ListColumnWidth = _listExpandedWidth;
        }

        if (ColProperties.Width.IsStar)
        {
            layout.PropertiesColumnStarValue = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsPassengerWagonPropertiesExpanded)
        {
            layout.PropertiesColumnStarValue = _propertiesExpandedStarValue;
        }
    }
}
