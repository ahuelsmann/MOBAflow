// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Controls;
using Domain;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.ComponentModel;

/// <summary>
/// TrainControlPage - train control interface.
/// </summary>
internal sealed partial class TrainControlPage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// DataTemplate for the door release button (DoorOpen or DoorClose depending on ViewModel.IsDoorCloseIconVisible).
    /// </summary>
    public DataTemplate? DoorReleaseIconTemplate => ViewModel.IsDoorCloseIconVisible ? _doorCloseTemplate : _doorOpenTemplate;

    /// <summary>
    /// DataTemplate for the brake button: BrakeActiveIcon (yellow with exclamation mark) when brake is on, otherwise BrakeReleasedIcon (theme, without exclamation mark).
    /// </summary>
    public DataTemplate? BrakeIconTemplate => ViewModel.IsParkingBrakeEnabled ? _brakeActiveTemplate : _brakeReleasedTemplate;

    public DataTemplate? DoorBlockedIconTemplate => _doorBlockedTemplate;

    private DataTemplate? _doorOpenTemplate;
    private DataTemplate? _doorCloseTemplate;
    private DataTemplate? _doorBlockedTemplate;
    private DataTemplate? _brakeActiveTemplate;
    private DataTemplate? _brakeReleasedTemplate;

    private readonly ILocomotiveService _locomotiveService;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<TrainControlPage>? _logger;
    private List<LocomotiveSeries> _allLocomotives = [];

    // UI element references
    private SpeedometerControl? _speedometer;
    private AmperemeterControl? _amperemeter;

    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage(
        TrainControlViewModel viewModel,
        ILocomotiveService locomotiveService,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<TrainControlPage>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(locomotiveService);
        ArgumentNullException.ThrowIfNull(settings);

        ViewModel = viewModel;
        _locomotiveService = locomotiveService;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;

        InitializeComponent();

        // Load icons immediately so brake and door release buttons are visible at startup (not only in OnLoaded)
        LoadIconTemplates();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Initialize SpeedSteps ComboBox selection
        SpeedStepsSelectedIndex = ViewModel.SpeedSteps switch
        {
            DccSpeedSteps.Steps14 => 0,
            DccSpeedSteps.Steps28 => 1,
            _ => 2
        };
    }

    /// <summary>
    /// Selected index for SpeedSteps ComboBox (for x:Bind).
    /// </summary>
    public int SpeedStepsSelectedIndex { get; set; }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SpeedKmh) or nameof(ViewModel.SelectedVmax) or nameof(ViewModel.SelectedLocoSeries))
        {
            UpdateVmaxDisplay();
            UpdateSpeedometerScale();  // This will update both MaxValue and VmaxKmh
        }

        if (e.PropertyName is nameof(ViewModel.IsDoorReleaseLocked) or nameof(ViewModel.IsDoorReleaseBlinking) or nameof(ViewModel.IsDoorReleaseLockedNext))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoorReleaseIconTemplate)));
        if (e.PropertyName is nameof(ViewModel.IsParkingBrakeEnabled))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrakeIconTemplate)));
    }

    private void LoadIconTemplates()
    {
        var appRes = Application.Current.Resources;
        _doorOpenTemplate = appRes.ContainsKey("DoorOpenIcon") ? appRes["DoorOpenIcon"] as DataTemplate : null;
        _doorCloseTemplate = appRes.ContainsKey("DoorCloseIcon") ? appRes["DoorCloseIcon"] as DataTemplate : null;
        _doorBlockedTemplate = appRes.ContainsKey("DoorBlockedIcon") ? appRes["DoorBlockedIcon"] as DataTemplate : null;
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
        // 2. GaugeMaxKmh: Controls km/h display range on the outer ring (400 km/h)

        // Set the DCC speed step range (for inner step markers)
        _speedometer.MaxValue = ViewModel.MaxSpeedStep;

        // Full-scale km/h ring for needle, arc, and outer markers
        _speedometer.GaugeMaxKmh = ViewModel.SpeedGaugeMaxKmh;

        // Locomotive Vmax marker (arc color threshold), not the gauge full scale
        _speedometer.VmaxKmh = ViewModel.SelectedVmax > 0
            ? ViewModel.SelectedVmax
            : 200;

        // DisplayValue (SpeedKmh) uses gauge full scale via ViewModel, not SelectedVmax
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Reload templates if needed (if not yet available at startup) and update UI
        LoadIconTemplates();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrakeIconTemplate)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoorReleaseIconTemplate)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoorBlockedIconTemplate)));

        // Find and store references to themed elements
        _speedometer = Speedometer;
        _amperemeter = Amperemeter;

        // Initialize speedometer scale based on current Vmax
        UpdateSpeedometerScale();

        // Initialize speedometer speed step markers
        UpdateSpeedStepMarkers();

        // Load locomotes asynchronously (fire-and-forget with error handling)
        LoadLocomotivesAsync().Observe(ex => _logger?.LogWarning(ex, "Load locomotive series failed"));

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
            _logger?.LogWarning(ex, "Failed to load locomotives");
            // Fail silently - app continues with empty list
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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

    private void SpeedStepsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
            return;

        var newSpeedSteps = item.Tag switch
        {
            "14" => DccSpeedSteps.Steps14,
            "28" => DccSpeedSteps.Steps28,
            _ => DccSpeedSteps.Steps128
        };

        ViewModel.SpeedSteps = newSpeedSteps;

        // Update speedometer scale (MaxValue), VmaxKmh, and markers
        UpdateSpeedometerScale();
        UpdateSpeedStepMarkers();

        SaveSpeedStepsSettingAsync().Observe(ex => _logger?.LogWarning(ex, "Save speed step settings failed"));
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