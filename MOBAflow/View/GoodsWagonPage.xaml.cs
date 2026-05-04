// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Moba.SharedUI.ViewModel;

using SharedUI.Interface;

internal sealed partial class GoodsWagonPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<GoodsWagonPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    private double _listExpandedWidth = 250;
    private double _propertiesExpandedWidth = 600;

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
            if (!ViewModel.IsGoodsWagonListExpanded)
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
        else if (e.PropertyName == nameof(ViewModel.IsGoodsWagonPropertiesExpanded))
        {
            if (!ViewModel.IsGoodsWagonPropertiesExpanded)
            {
                if (ColProperties.Width.IsAbsolute)
                {
                    _propertiesExpandedWidth = ColProperties.Width.Value;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = new GridLength(_propertiesExpandedWidth);
            }
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.GoodsWagonPage;

        if (layout.ListColumnWidth > 0)
        {
            _listExpandedWidth = layout.ListColumnWidth;
        }
        if (layout.PropertiesColumnWidth > 0)
        {
            _propertiesExpandedWidth = layout.PropertiesColumnWidth;
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
            ColProperties.Width = new GridLength(_propertiesExpandedWidth);
        }
        else
        {
            ColProperties.Width = GridLength.Auto;
        }

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

        if (ColList.Width.IsAbsolute)
        {
            layout.ListColumnWidth = ColList.Width.Value;
        }
        else if (!ViewModel.IsGoodsWagonListExpanded)
        {
            layout.ListColumnWidth = _listExpandedWidth;
        }

        if (ColProperties.Width.IsAbsolute)
        {
            layout.PropertiesColumnWidth = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsGoodsWagonPropertiesExpanded)
        {
            layout.PropertiesColumnWidth = _propertiesExpandedWidth;
        }
    }
}
