// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Controls;
using Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Service;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.ViewManagement;

/// <summary>
/// TrainControlPage - Theme-aware train control interface.
/// Supports multiple manufacturer-inspired color themes with system accent as default.
/// Dynamically applies theme colors to all UI elements.
/// </summary>
public sealed partial class TrainControlPage
{
    private readonly ILocomotiveService _locomotiveService;
    private readonly ISkinProvider _skinProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private List<LocomotiveSeries> _allLocomotives = [];

    // UI element references for theme application
    private SpeedometerControl? _speedometer;
    private TextBlock? _titleText;
    private Border? _headerBorder;
    private Grid? _headerGrid;
    private Border? _settingsPanel;
    private Border? _speedometerPanel;
    private Border? _functionsPanel;

    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage(
        TrainControlViewModel viewModel,
        ILocomotiveService locomotiveService,
        ISkinProvider skinProvider,
        AppSettings settings,
        ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _locomotiveService = locomotiveService;
        _skinProvider = skinProvider;
        _settings = settings;
        _settingsService = settingsService;

        InitializeComponent();

        // Subscribe to skin changes for dynamic updates
        _skinProvider.SkinChanged += OnSkinProviderChanged;
        _skinProvider.DarkModeChanged += OnDarkModeChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Initialize SpeedSteps ComboBox selection
        SpeedStepsSelectedIndex = ViewModel.SpeedSteps switch
        {
            DccSpeedSteps.Steps14 => 0,
            DccSpeedSteps.Steps28 => 1,
            DccSpeedSteps.Steps128 => 2,
            _ => 2
        };
    }

    /// <summary>
    /// Selected index for SpeedSteps ComboBox (for x:Bind).
    /// </summary>
    public int SpeedStepsSelectedIndex { get; set; } = 2; // Default: 128 Steps

    private void OnSkinProviderChanged(object? sender, SkinChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void OnDarkModeChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void ApplySkinColors()
    {
        var palette = SkinColors.GetPalette(_skinProvider.CurrentSkin, _skinProvider.IsDarkMode);

        // Set page RequestedTheme based on skin (controls Light/Dark for standard WinUI controls)
        RequestedTheme = palette.IsDarkTheme
            ? ElementTheme.Dark
            : ElementTheme.Light;

        // Check if this is the "System" skin (uses Windows accent color)
        var isSystemSkin = _skinProvider.CurrentSkin == AppSkin.System;

        // Header border background - use skin-appropriate color
        if (_headerBorder != null)
        {
            if (isSystemSkin)
            {
                // System skin: Use Acrylic background
                _headerBorder.ClearValue(Border.BackgroundProperty);
            }
            else
            {
                // Color skins: Use HeaderBackground color with opacity
                _headerBorder.Background = palette.HeaderBackgroundBrush;
            }
        }

        // Header grid background - use skin-appropriate color for solid colored header
        if (_headerGrid != null)
        {
            if (isSystemSkin)
            {
                // System skin: Use default/transparent background
                _headerGrid.ClearValue(Grid.BackgroundProperty);
            }
            else
            {
                // Color skins: Use HeaderBackground color (fallback if border not available)
                _headerGrid.Background = palette.HeaderBackgroundBrush;
            }
        }

        // Title text color - use skin-appropriate color
        if (_titleText != null)
        {
            if (isSystemSkin)
            {
                // System skin: Use standard text color
                _titleText.ClearValue(TextBlock.ForegroundProperty);
            }
            else
            {
                // Color skins: Use HeaderForeground color
                _titleText.Foreground = palette.HeaderForegroundBrush;
            }
        }

        // Speedometer needle - use system accent color for System skin
        if (_speedometer != null)
        {
            if (isSystemSkin)
            {
                // Use Windows system accent color
                _speedometer.AccentColor = GetSystemAccentColor();
            }
            else
            {
                _speedometer.AccentColor = palette.Accent;
            }
        }

        // Panel backgrounds and borders - only apply if not transparent (Alpha > 0)
        var hasCustomPanelColors = palette.PanelBackground.A > 0;

        // Get default WinUI card brushes for Original theme
        var defaultCardBackground = hasCustomPanelColors
            ? palette.PanelBackgroundBrush
            : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        var defaultCardBorder = hasCustomPanelColors
            ? palette.PanelBorderBrush
            : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        if (_settingsPanel != null)
        {
            _settingsPanel.Background = defaultCardBackground;
            _settingsPanel.BorderBrush = defaultCardBorder;
        }

        if (_speedometerPanel != null)
        {
            _speedometerPanel.Background = defaultCardBackground;
            _speedometerPanel.BorderBrush = defaultCardBorder;
        }

        if (_functionsPanel != null)
        {
            _functionsPanel.Background = defaultCardBackground;
            _functionsPanel.BorderBrush = defaultCardBorder;
        }

        // Update page background (only if not transparent)
        if (Content is Grid rootGrid)
        {
            if (hasCustomPanelColors)
            {
                rootGrid.Background = palette.PanelBackgroundBrush;
            }
            else
            {
                rootGrid.ClearValue(Grid.BackgroundProperty);
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SpeedKmh) or nameof(ViewModel.SelectedVmax) or nameof(ViewModel.HasValidLocoSeries))
        {
            UpdateVmaxDisplay();
            UpdateSpeedometerScale();  // This will update both MaxValue and VmaxKmh
        }
    }

    private void UpdateVmaxDisplay()
    {
        VmaxDisplay.Visibility = ViewModel.HasValidLocoSeries ? Visibility.Visible : Visibility.Collapsed;
        VmaxText.Text = ViewModel.SelectedVmax.ToString();
    }

    private void UpdateSpeedometerScale()
    {
        if (_speedometer is null)
            return;

        // Update speedometer scale based on:
        // 1. MaxSpeedStep: Controls DCC speed step range (13/27/126)
        // 2. SelectedVmax: Controls km/h display range (e.g., 200 km/h)
        
        // Set the DCC speed step range (for needle positioning)
        _speedometer.MaxValue = ViewModel.MaxSpeedStep;
        
        // Set Vmax for km/h markers display
        _speedometer.VmaxKmh = ViewModel.SelectedVmax > 0 
            ? ViewModel.SelectedVmax 
            : 200; // Default fallback
        
        // Note: DisplayValue shows km/h calculated as (Speed/MaxSpeedStep) * Vmax
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Find and store references to themed elements
        _speedometer = FindName("Speedometer") as SpeedometerControl;
        _titleText = FindName("TitleText") as TextBlock;
        _headerBorder = FindName("HeaderBorder") as Border;
        _headerGrid = FindName("HeaderGrid") as Grid;
        _settingsPanel = FindName("SettingsPanel") as Border;
        _speedometerPanel = FindName("SpeedometerPanel") as Border;
        _functionsPanel = FindName("FunctionsPanel") as Border;

        // Apply current skin colors
        ApplySkinColors();

        // Initialize speedometer scale based on current Vmax
        UpdateSpeedometerScale();

        // Initialize speedometer speed step markers
        UpdateSpeedStepMarkers();

        _allLocomotives = await _locomotiveService.GetAllSeriesAsync().ConfigureAwait(false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _skinProvider.SkinChanged -= OnSkinProviderChanged;
        _skinProvider.DarkModeChanged -= OnDarkModeChanged;
    }

    private void LocoSeriesBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var query = sender.Text.ToLowerInvariant();
        var filtered = _allLocomotives
            .Where(s => s.Name.ToLowerInvariant().Contains(query))
            .Take(5)
            .Select(s => s.Name)
            .ToList();

        sender.ItemsSource = filtered;
    }

    private void LocoSeriesBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selected = _allLocomotives.FirstOrDefault(s => s.Name == (string)args.SelectedItem);
        if (selected != null)
        {
            ViewModel.SelectedLocoSeries = selected.Name;
            ViewModel.SelectedVmax = selected.Vmax;
        }
    }

    private void LocoSeriesBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var selected = _allLocomotives.FirstOrDefault(s => s.Name == args.QueryText);
        if (selected != null)
        {
            ViewModel.SelectedLocoSeries = selected.Name;
            ViewModel.SelectedVmax = selected.Vmax;
        }
    }

    // Skin selection handlers for Flyout buttons
    private async void OnSkinGreenClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Green);
    private async void OnSkinBlueClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Blue);
    private async void OnSkinVioletClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Violet);
    private async void OnSkinOrangeClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Orange);
    private async void OnSkinDarkOrangeClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.DarkOrange);
    private async void OnSkinRedClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Red);
    private async void OnSkinSystemClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.System);

    private async Task SetSkinAsync(AppSkin skin)
    {
        _skinProvider.SetSkin(skin);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the Windows system accent color from UISettings.
    /// This matches the behavior of {ThemeResource AccentFillColorDefaultBrush}.
    /// </summary>
    private static Color GetSystemAccentColor()
    {
        var uiSettings = new UISettings();
        return uiSettings.GetColorValue(UIColorType.Accent);
    }

    private void SpeedStepsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
            return;

        var newSpeedSteps = item.Tag switch
        {
            "14" => DccSpeedSteps.Steps14,
            "28" => DccSpeedSteps.Steps28,
            "128" => DccSpeedSteps.Steps128,
            _ => DccSpeedSteps.Steps128
        };

        ViewModel.SpeedSteps = newSpeedSteps;

        // Update speedometer scale (MaxValue), VmaxKmh, and markers
        UpdateSpeedometerScale();
        UpdateSpeedStepMarkers();

        // Save settings
        _ = SaveSpeedStepsSettingAsync();
    }

    private void UpdateSpeedStepMarkers()
    {
        if (_speedometer is null)
            return;

        // Update speedometer SpeedSteps property to trigger marker re-rendering
        _speedometer.SpeedSteps = (int)ViewModel.SpeedSteps;
    }

    private async Task SaveSpeedStepsSettingAsync()
    {
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }
}
