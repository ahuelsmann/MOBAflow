// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Moba.SharedUI.ViewModel;

using SharedUI.Interface;

internal sealed partial class LocomotivesPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<LocomotivesPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    private double _listExpandedWidth = 250;
    private double _propertiesExpandedStarValue = 1;

    public LocomotivesPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<LocomotivesPage>? logger = null)
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
        if (e.PropertyName == nameof(ViewModel.IsLocomotivesListExpanded))
        {
            if (!ViewModel.IsLocomotivesListExpanded)
            {
                if (ColList.Width.IsAbsolute)
                {
                    _listExpandedWidth = ColList.Width.Value;
                }
                ColList.Width = GridLength.Auto;
            }
            else
            {
                ColList.Width = new GridLength(_listExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsLocomotivesPropertiesExpanded))
        {
            if (!ViewModel.IsLocomotivesPropertiesExpanded)
            {
                if (ColProperties.Width.IsStar)
                {
                    _propertiesExpandedStarValue = ColProperties.Width.Value;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
            }
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.LocomotivesPage;

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

        if (ColList.Width.IsAbsolute)
        {
            layout.ListColumnWidth = ColList.Width.Value;
        }
        else if (!ViewModel.IsLocomotivesListExpanded)
        {
            layout.ListColumnWidth = _listExpandedWidth;
        }

        if (ColProperties.Width.IsStar)
        {
            layout.PropertiesColumnStarValue = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsLocomotivesPropertiesExpanded)
        {
            layout.PropertiesColumnStarValue = _propertiesExpandedStarValue;
        }
    }
}
