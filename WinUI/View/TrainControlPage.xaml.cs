// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Navigation;

using Controls;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;
using System.Diagnostics;

using ViewModel;

using Windows.UI;
using Windows.UI.ViewManagement;

/// <summary>
/// TrainControlPage - Theme-aware train control interface.
/// Supports multiple manufacturer-inspired color themes with system accent as default.
/// Dynamically applies theme colors to all UI elements.
/// </summary>
[NavigationItem(
    Tag = "traincontrol",
    Title = "Train Control",
    Icon = "\uEC49",
    Category = NavigationCategory.TrainControl,
    Order = 10,
    FeatureToggleKey = "IsTrainControlPageAvailable",
    BadgeLabelKey = "TrainControlPageLabel",
    IsBold = true)]
internal sealed partial class TrainControlPage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// DataTemplate for the door release button (DoorOpen or DoorClose depending on ViewModel.ShowDoorCloseIcon).
    /// </summary>
    public DataTemplate? DoorReleaseIconTemplate => ViewModel.ShowDoorCloseIcon ? _doorCloseTemplate : _doorOpenTemplate;

    /// <summary>
    /// DataTemplate for the brake button: BrakeActiveIcon (yellow with exclamation mark) when brake is on, otherwise BrakeReleasedIcon (theme, without exclamation mark).
    /// </summary>
    public DataTemplate? BrakeIconTemplate => ViewModel.BrakeEngaged ? _brakeActiveTemplate : _brakeReleasedTemplate;

    private DataTemplate? _doorOpenTemplate;
    private DataTemplate? _doorCloseTemplate;
    private DataTemplate? _brakeActiveTemplate;
    private DataTemplate? _brakeReleasedTemplate;

    private readonly ILocomotiveService _locomotiveService;
    private readonly ISkinProvider _skinProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private List<LocomotiveSeries> _allLocomotives = [];

    /// <summary>
    /// Skin selection ViewModel for this page.
    /// </summary>
    public SkinSelectorViewModel SkinViewModel { get; }

    // UI element references for theme application
    private SpeedometerControl? _speedometer;
    private AmperemeterControl? _amperemeter;
    private TextBlock? _titleText;
    private Border? _headerBorder;
    private Grid? _headerGrid;
    private Border? _settingsPanel;
    private Border? _speedometerPanel;
    private Border? _amperemeterPanel;
    private Border? _functionsPanel;

    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage(
        TrainControlViewModel viewModel,
        ILocomotiveService locomotiveService,
        ISkinProvider skinProvider,
        AppSettings settings,
        SkinSelectorViewModel skinViewModel,
        ISettingsService? settingsService = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(locomotiveService);
        ArgumentNullException.ThrowIfNull(skinProvider);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(skinViewModel);

        ViewModel = viewModel;
        _locomotiveService = locomotiveService;
        _skinProvider = skinProvider;
        _settings = settings;
        _settingsService = settingsService;
        SkinViewModel = skinViewModel;

        InitializeComponent();

        // Load icons immediately so brake and door release buttons are visible at startup (not only in OnLoaded)
        LoadIconTemplates();

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
            DccSpeedSteps.Steps128 => 2
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

        // Panel backgrounds and borders - only apply if not transparent (Alpha > 0)
        var hasCustomPanelColors = palette.PanelBackground.A > 0;

        if (_speedometer != null)
        {
            _speedometer.AccentColor = null;
        }

        if (_amperemeter != null)
        {
            _amperemeter.AccentColor = null;
        }

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

        if (_amperemeterPanel != null)
        {
            _amperemeterPanel.Background = defaultCardBackground;
            _amperemeterPanel.BorderBrush = defaultCardBorder;
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
        if (e.PropertyName is nameof(ViewModel.SpeedKmh) or nameof(ViewModel.SelectedVmax) or nameof(ViewModel.SelectedLocoSeries))
        {
            UpdateVmaxDisplay();
            UpdateSpeedometerScale();  // This will update both MaxValue and VmaxKmh
        }

        if (e.PropertyName is nameof(ViewModel.DoorReleaseLocked) or nameof(ViewModel.DoorReleaseBlinking) or nameof(ViewModel.DoorReleaseLockedNext))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoorReleaseIconTemplate)));
        if (e.PropertyName is nameof(ViewModel.BrakeEngaged))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrakeIconTemplate)));
    }

    private void LoadIconTemplates()
    {
        var appRes = Application.Current.Resources;
        _doorOpenTemplate = appRes.ContainsKey("DoorOpenIcon") ? appRes["DoorOpenIcon"] as DataTemplate : null;
        _doorCloseTemplate = appRes.ContainsKey("DoorCloseIcon") ? appRes["DoorCloseIcon"] as DataTemplate : null;
        _brakeActiveTemplate = appRes.ContainsKey("BrakeActiveIcon") ? appRes["BrakeActiveIcon"] as DataTemplate : null;
        _brakeReleasedTemplate = appRes.ContainsKey("BrakeReleasedIcon") ? appRes["BrakeReleasedIcon"] as DataTemplate : null;
    }

    private void UpdateVmaxDisplay()
    {
        // Show Vmax display if a loco series is selected
        VmaxDisplay.Visibility = !string.IsNullOrEmpty(ViewModel.SelectedLocoSeries) ? Visibility.Visible : Visibility.Collapsed;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to skin and ViewModel events when page enters visual tree
        _skinProvider.SkinChanged += OnSkinProviderChanged;
        _skinProvider.DarkModeChanged += OnDarkModeChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Reload templates if needed (if not yet available at startup) and update UI
        LoadIconTemplates();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrakeIconTemplate)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoorReleaseIconTemplate)));

        // Find and store references to themed elements
        _speedometer = FindName("Speedometer") as SpeedometerControl;
        _amperemeter = FindName("Amperemeter") as AmperemeterControl;
        _titleText = FindName("TitleText") as TextBlock;
        _headerBorder = FindName("HeaderBorder") as Border;
        _headerGrid = FindName("HeaderGrid") as Grid;
        _settingsPanel = FindName("SettingsPanel") as Border;
        _speedometerPanel = FindName("SpeedometerPanel") as Border;
        _amperemeterPanel = FindName("AmperemeterPanel") as Border;
        _functionsPanel = FindName("FunctionsPanel") as Border;

        // Apply current skin colors
        ApplySkinColors();

        // Initialize speedometer scale based on current Vmax
        UpdateSpeedometerScale();

        // Initialize speedometer speed step markers
        UpdateSpeedStepMarkers();

        // Load locomotes asynchronously (fire-and-forget with error handling)
        _ = LoadLocomotivesAsync();

        // Initialize AutoSuggestBox with saved locomotive series
        if (!string.IsNullOrEmpty(ViewModel.SelectedLocoSeries))
        {
            LocoSeriesBox.Text = ViewModel.SelectedLocoSeries;
            UpdateVmaxDisplay(); // Show Vmax display if a series is loaded from settings
        }
    }

    /// <summary>
    /// Loads all locomotive series asynchronously.
    /// Fire-and-forget pattern with error logging.
    /// </summary>
    private async Task LoadLocomotivesAsync()
    {
        try
        {
            _allLocomotives = await _locomotiveService.GetAllSeriesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load locomotives: {ex.Message}");
            // Fail silently - app continues with empty list
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
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
        LocomotiveSeries? selected = null;

        // Priority 1: User chose a suggestion (clicked or pressed Enter on highlighted item)
        if (args.ChosenSuggestion != null)
        {
            selected = _allLocomotives.FirstOrDefault(s => s.Name == (string)args.ChosenSuggestion);
        }
        // Priority 2: Exact match with query text
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            selected = _allLocomotives.FirstOrDefault(s => s.Name.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));
        }
        // Priority 3: Partial match (first item that contains the query)
        if (selected == null && !string.IsNullOrWhiteSpace(args.QueryText))
        {
            var query = args.QueryText.ToLowerInvariant();
            selected = _allLocomotives.FirstOrDefault(s => s.Name.ToLowerInvariant().Contains(query));
        }

        // Apply selection if found
        if (selected != null)
        {
            ViewModel.SelectedLocoSeries = selected.Name;
            ViewModel.SelectedVmax = selected.Vmax;

            // Update AutoSuggestBox text to show full series name
            sender.Text = selected.Name;
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

    private async void FunctionButton_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string tag || !int.TryParse(tag, out var functionIndex) || functionIndex < 0 || functionIndex > 20)
            return;

        var picker = new FunctionSymbolPickerDialog();
        picker.XamlRoot = XamlRoot;
        await picker.ShowAsync();

        if (picker.SelectedGlyph != null)
        {
            if (!ViewModel.SetFunctionSymbol(functionIndex, picker.SelectedGlyph))
                ViewModel.StatusMessage = $"Keine Lok mit Adresse {ViewModel.LocoAddress} im Projekt. Bitte zuerst eine Lok mit dieser Digitaladresse anlegen.";
        }
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

        // Save settings asynchronously (fire-and-forget with error handling)
        _ = SaveSpeedStepsSettingAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.WriteLine($"Failed to save speed steps setting: {task.Exception?.InnerException?.Message}");
                }
            });
    }

    private async Task SaveSpeedStepsSettingAsync()
    {
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
    }

    private void UpdateSpeedStepMarkers()
    {
        if (_speedometer is null)
            return;

        // Update speedometer SpeedSteps property to trigger marker re-rendering
        _speedometer.SpeedSteps = (int)ViewModel.SpeedSteps;
    }
}
