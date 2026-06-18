// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using SharedUI.Interface;

using ViewModel;

internal sealed partial class MatrixPage
{
    public MatrixPageViewModel ViewModel { get; }

    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<MatrixPage>? _logger;
    private long _colorPaletteExpandedToken;
    private long _imagesExpandedToken;

    private double _colorPaletteExpandedWidth = 320;
    private double _imagesExpandedStarValue = 1;

    public MatrixPage(
        MatrixPageViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<MatrixPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _colorPaletteExpandedToken = ColorPalettePanel.RegisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, OnPanelIsExpandedChanged);
        _imagesExpandedToken = ImagesPanel.RegisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, OnPanelIsExpandedChanged);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestoreLayout();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
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

        if (_colorPaletteExpandedToken != 0)
        {
            ColorPalettePanel.UnregisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, _colorPaletteExpandedToken);
            _colorPaletteExpandedToken = 0;
        }
        if (_imagesExpandedToken != 0)
        {
            ImagesPanel.UnregisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, _imagesExpandedToken);
            _imagesExpandedToken = 0;
        }

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        ViewModel.Dispose();
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.MatrixPage;

        if (layout.ColorPaletteColumnWidth > 0)
        {
            _colorPaletteExpandedWidth = layout.ColorPaletteColumnWidth;
        }
        if (layout.ImagesColumnStarValue > 0)
        {
            _imagesExpandedStarValue = layout.ImagesColumnStarValue;
        }

        ColorPalettePanel.IsExpanded = layout.IsColorPaletteExpanded;
        ImagesPanel.IsExpanded = layout.IsImagesExpanded;
        ColColorPalette.Width = layout.IsColorPaletteExpanded ? new GridLength(_colorPaletteExpandedWidth) : GridLength.Auto;
        ColImages.Width = layout.IsImagesExpanded ? new GridLength(_imagesExpandedStarValue, GridUnitType.Star) : GridLength.Auto;
    }

    private void OnPanelIsExpandedChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender == ColorPalettePanel)
        {
            ApplyColorPaletteColumnState();
        }
        else if (sender == ImagesPanel)
        {
            ApplyImagesColumnState();
        }
    }

    private void ApplyColorPaletteColumnState()
    {
        if (!ColorPalettePanel.IsExpanded)
        {
            if (ColColorPalette.Width.IsAbsolute)
            {
                _colorPaletteExpandedWidth = ColColorPalette.Width.Value;
            }
            ColColorPalette.Width = GridLength.Auto;
        }
        else
        {
            ColColorPalette.Width = new GridLength(_colorPaletteExpandedWidth);
        }
    }

    private void ApplyImagesColumnState()
    {
        if (!ImagesPanel.IsExpanded)
        {
            if (ColImages.Width.IsStar)
            {
                _imagesExpandedStarValue = ColImages.Width.Value;
            }
            ColImages.Width = GridLength.Auto;
        }
        else
        {
            ColImages.Width = new GridLength(_imagesExpandedStarValue, GridUnitType.Star);
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.MatrixPage;

        layout.IsColorPaletteExpanded = ColorPalettePanel.IsExpanded;
        layout.IsImagesExpanded = ImagesPanel.IsExpanded;

        if (ColColorPalette.Width.IsAbsolute)
        {
            _colorPaletteExpandedWidth = ColColorPalette.Width.Value;
        }
        if (ColImages.Width.IsStar)
        {
            _imagesExpandedStarValue = ColImages.Width.Value;
        }

        layout.ColorPaletteColumnWidth = _colorPaletteExpandedWidth;
        layout.ImagesColumnStarValue = _imagesExpandedStarValue;
    }
}