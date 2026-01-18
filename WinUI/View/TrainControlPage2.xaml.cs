// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Controls;

using Domain;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Model;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using AppTheme = Service.ApplicationTheme;

/// <summary>
/// TrainControlPage2 - Theme-aware variant of TrainControlPage.
/// Demonstrates Skin-System with manufacturer-inspired color themes.
/// Dynamically applies complete theme colors to all UI elements.
/// </summary>
public sealed partial class TrainControlPage2
{
    private readonly ILocomotiveService _locomotiveService;
    private readonly IThemeProvider _themeProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private List<LocomotiveSeries> _allLocomotives = [];

    // UI element references for theme application
    private SpeedometerControl? _speedometer;
    private Border? _headerBorder;
    private TextBlock? _titleText;
    private Border? _settingsPanel;
    private Border? _speedometerPanel;
    private Border? _functionsPanel;

    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage2(
        TrainControlViewModel viewModel,
        ILocomotiveService locomotiveService,
        IThemeProvider themeProvider,
        AppSettings settings,
        ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _locomotiveService = locomotiveService;
        _themeProvider = themeProvider;
        _settings = settings;
        _settingsService = settingsService;

        InitializeComponent();

        // Subscribe to theme changes for dynamic updates
        _themeProvider.ThemeChanged += OnThemeProviderChanged;
        _themeProvider.DarkModeChanged += OnDarkModeChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnThemeProviderChanged(object? sender, ThemeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyThemeColors);
    }

    private void OnDarkModeChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyThemeColors);
    }

    private void ApplyThemeColors()
    {
        var palette = ThemeColors.GetPalette(_themeProvider.CurrentTheme, _themeProvider.IsDarkMode);

        // Set page RequestedTheme based on skin (controls Light/Dark for standard WinUI controls)
        RequestedTheme = palette.IsDarkTheme
            ? ElementTheme.Dark
            : ElementTheme.Light;

        // Speedometer needle
        if (_speedometer != null)
        {
            _speedometer.AccentColor = palette.Accent;
        }

        // Check if this is the "Original" theme (transparent header = no colored strip)
        var isOriginalTheme = _themeProvider.CurrentTheme == Service.ApplicationTheme.Original;

        // Header background (themed strip at top)
        if (_headerBorder != null)
        {
            if (isOriginalTheme)
            {
                // Original theme: No colored header strip - make background transparent
                // but keep the header content (title, skin buttons, emergency stop) visible
                _headerBorder.Background = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                // Other themes: Show colored header strip
                _headerBorder.Background = palette.HeaderBackgroundBrush;
            }
        }

        // Title text color - use theme-appropriate color for Original theme
        if (_titleText != null)
        {
            if (isOriginalTheme)
            {
                // Original theme: Use standard text color based on light/dark mode
                _titleText.Foreground = _themeProvider.IsDarkMode 
                    ? new SolidColorBrush(Colors.White) 
                    : new SolidColorBrush(Colors.Black);
            }
            else
            {
                _titleText.Foreground = palette.HeaderForegroundBrush;
            }
        }

        // Panel backgrounds and borders - only apply if not transparent (Alpha > 0)
        var hasCustomPanelColors = palette.PanelBackground.A > 0;

        if (_settingsPanel != null)
        {
            if (hasCustomPanelColors)
            {
                _settingsPanel.Background = palette.PanelBackgroundBrush;
                _settingsPanel.BorderBrush = palette.PanelBorderBrush;
            }
            else
            {
                // Use default WinUI ThemeResources
                _settingsPanel.ClearValue(Border.BackgroundProperty);
                _settingsPanel.ClearValue(Border.BorderBrushProperty);
            }
        }

        if (_speedometerPanel != null)
        {
            if (hasCustomPanelColors)
            {
                _speedometerPanel.Background = palette.PanelBackgroundBrush;
                _speedometerPanel.BorderBrush = palette.PanelBorderBrush;
            }
            else
            {
                _speedometerPanel.ClearValue(Border.BackgroundProperty);
                _speedometerPanel.ClearValue(Border.BorderBrushProperty);
            }
        }

        if (_functionsPanel != null)
        {
            if (hasCustomPanelColors)
            {
                _functionsPanel.Background = palette.PanelBackgroundBrush;
                _functionsPanel.BorderBrush = palette.PanelBorderBrush;
            }
            else
            {
                _functionsPanel.ClearValue(Border.BackgroundProperty);
                _functionsPanel.ClearValue(Border.BorderBrushProperty);
            }
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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SpeedKmh) or nameof(ViewModel.SelectedVmax) or nameof(ViewModel.HasValidLocoSeries))
        {
            UpdateVmaxDisplay();
        }
    }

    private void UpdateVmaxDisplay()
    {
        VmaxDisplay.Visibility = ViewModel.HasValidLocoSeries ? Visibility.Visible : Visibility.Collapsed;
        VmaxText.Text = ViewModel.SelectedVmax.ToString();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Find and store references to themed elements
        _speedometer = FindName("Speedometer") as SpeedometerControl;
        _headerBorder = FindName("HeaderBorder") as Border;
        _titleText = FindName("TitleText") as TextBlock;
        _settingsPanel = FindName("SettingsPanel") as Border;
        _speedometerPanel = FindName("SpeedometerPanel") as Border;
        _functionsPanel = FindName("FunctionsPanel") as Border;

        // Apply current theme colors
        ApplyThemeColors();

        _allLocomotives = await _locomotiveService.GetAllSeriesAsync().ConfigureAwait(false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _themeProvider.ThemeChanged -= OnThemeProviderChanged;
        _themeProvider.DarkModeChanged -= OnDarkModeChanged;
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
            ViewModel.SelectedVmax = selected.Vmax;
        }
    }

    private void LocoSeriesBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var selected = _allLocomotives.FirstOrDefault(s => s.Name == args.QueryText);
        if (selected != null)
        {
            ViewModel.SelectedVmax = selected.Vmax;
        }
    }

    // Skin selection handlers for CommandBar buttons
    private async void OnSkinClassicClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.Classic);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinModernClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.Modern);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinDarkClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.Dark);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinEsuClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.EsuCabControl);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinRocoClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.RocoZ21);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinMaerklinClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.MaerklinCS);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private async void OnSkinOriginalClicked(object sender, RoutedEventArgs e)
    {
        _themeProvider.SetTheme(AppTheme.Original);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }
}

