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

        // Add Skin Selector to header
        AddSkinSelector();

        // Subscribe to theme changes for dynamic updates
        _themeProvider.ThemeChanged += OnThemeProviderChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void AddSkinSelector()
    {
        var skinPanel = SkinSelectorComboBox.CreateWithLabel(_themeProvider, _settings, _settingsService, out var skinComboBox);
        skinComboBox.SkinChanged += OnSkinChanged;

        // Find HeaderGrid and add to right side
        if (FindName("HeaderGrid") is Grid headerGrid)
        {
            Grid.SetColumn(skinPanel, 1);
            skinPanel.HorizontalAlignment = HorizontalAlignment.Right;
            skinPanel.Margin = new Thickness(0, 0, 16, 0);
            headerGrid.Children.Add(skinPanel);
        }
    }

    private void OnSkinChanged(object? sender, Service.ApplicationTheme newTheme)
    {
        ApplyThemeColors();
    }

    private void OnThemeProviderChanged(object? sender, ThemeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyThemeColors);
    }

    private void ApplyThemeColors()
    {
        var palette = ThemeColors.GetPalette(_themeProvider.CurrentTheme);

        // Set page RequestedTheme based on skin (controls Light/Dark for standard WinUI controls)
        RequestedTheme = palette.IsDarkTheme
            ? ElementTheme.Dark
            : ElementTheme.Light;

        // Speedometer needle
        if (_speedometer != null)
        {
            _speedometer.AccentColor = palette.Accent;
        }

        // Header background (themed strip at top)
        if (_headerBorder != null)
        {
            _headerBorder.Background = palette.HeaderBackgroundBrush;
        }

        // Title text color
        if (_titleText != null)
        {
            _titleText.Foreground = palette.HeaderForegroundBrush;
        }

        // Panel backgrounds and borders
        if (_settingsPanel != null)
        {
            _settingsPanel.Background = palette.PanelBackgroundBrush;
            _settingsPanel.BorderBrush = palette.PanelBorderBrush;
        }

        if (_speedometerPanel != null)
        {
            _speedometerPanel.Background = palette.PanelBackgroundBrush;
            _speedometerPanel.BorderBrush = palette.PanelBorderBrush;
        }

        if (_functionsPanel != null)
        {
            _functionsPanel.Background = palette.PanelBackgroundBrush;
            _functionsPanel.BorderBrush = palette.PanelBorderBrush;
        }

        // Update page background
        if (Content is Grid rootGrid)
        {
            rootGrid.Background = palette.PanelBackgroundBrush;
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
}
