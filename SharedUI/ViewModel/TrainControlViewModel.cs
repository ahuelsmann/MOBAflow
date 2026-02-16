// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;
using Backend.Model;

using Common.Configuration;
using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.ComponentModel;

/// <summary>
/// ViewModel for TrainControlPage - provides locomotive drive control interface.
/// Implements a "digital throttle" similar to the Roco Z21 app hand controller.
/// 
/// Features:
/// - 3 locomotive presets with persistent DCC addresses, speed, and function states
/// - Speed control (0-126 for 128 speed steps)
/// - Direction toggle (Forward/Backward)
/// - Function keys F0-F20
/// - Emergency stop
/// - Speed ramping for smooth direction changes
/// 
/// Cross-platform: Used by WinUI and MAUI.
/// </summary>
public sealed partial class TrainControlViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly ISettingsService _settingsService;
    private readonly Backend.Interface.ITripLogService? _tripLogService;
    private readonly ILogger<TrainControlViewModel>? _logger;

    private bool _isLoadingPreset;

    // === DCC Speed Steps Configuration ===

    /// <summary>
    /// DCC speed step configuration (14, 28, or 128 steps).
    /// This determines how many discrete speed levels are available.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxSpeedStep))]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    private DccSpeedSteps _speedSteps = DccSpeedSteps.Steps128;

    /// <summary>
    /// Maximum speed step value based on SpeedSteps configuration.
    /// Returns: 13 for 14 steps, 27 for 28 steps, 126 for 128 steps.
    /// </summary>
    public int MaxSpeedStep => SpeedSteps switch
    {
        DccSpeedSteps.Steps14 => 13,
        DccSpeedSteps.Steps28 => 27,
        _ => 126
    };

    /// <summary>
    /// DCC locomotive address (1-9999).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetSpeedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF0Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF1Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF2Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF3Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF4Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF5Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF6Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF7Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF8Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF14Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF15Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF16Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF17Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF18Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF19Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF20Command))]
    [NotifyCanExecuteChangedFor(nameof(EmergencyStopCommand))]
    private int _locoAddress = 3;

    // === Locomotive Presets ===

    /// <summary>
    /// Currently selected preset index (0, 1, or 2).
    /// </summary>
    [ObservableProperty]
    private int _selectedPresetIndex;

    /// <summary>
    /// First locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset _preset1 = new() { Name = "Lok 1", DccAddress = 3 };

    /// <summary>
    /// Second locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset _preset2 = new() { Name = "Lok 2", DccAddress = 4 };

    /// <summary>
    /// Third locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset _preset3 = new() { Name = "Lok 3", DccAddress = 5 };

    /// <summary>
    /// Gets the currently selected preset.
    /// </summary>
    public LocomotivePreset CurrentPreset => SelectedPresetIndex switch
    {
        0 => Preset1,
        1 => Preset2,
        2 => Preset3,
        _ => Preset1
    };

    // === Preset Address Wrapper Properties ===
    // These properties provide TwoWay binding for the NumberBox controls
    // and automatically sync LocoAddress when the active preset's address changes.

    /// <summary>
    /// DCC address for Preset 1. Syncs to LocoAddress when Preset 1 is selected.
    /// </summary>
    public int Preset1Address
    {
        get => Preset1.DccAddress;
        set
        {
            if (Preset1.DccAddress != value)
            {
                Preset1.DccAddress = value;
                OnPropertyChanged();
                if (SelectedPresetIndex == 0)
                {
                    LocoAddress = value;
                }
                _ = SavePresetsToSettingsAsync();
            }
        }
    }

    /// <summary>
    /// DCC address for Preset 2. Syncs to LocoAddress when Preset 2 is selected.
    /// </summary>
    public int Preset2Address
    {
        get => Preset2.DccAddress;
        set
        {
            if (Preset2.DccAddress != value)
            {
                Preset2.DccAddress = value;
                OnPropertyChanged();
                if (SelectedPresetIndex == 1)
                {
                    LocoAddress = value;
                }
                _ = SavePresetsToSettingsAsync();
            }
        }
    }

    /// <summary>
    /// DCC address for Preset 3. Syncs to LocoAddress when Preset 3 is selected.
    /// </summary>
    public int Preset3Address
    {
        get => Preset3.DccAddress;
        set
        {
            if (Preset3.DccAddress != value)
            {
                Preset3.DccAddress = value;
                OnPropertyChanged();
                if (SelectedPresetIndex == 2)
                {
                    LocoAddress = value;
                }
                _ = SavePresetsToSettingsAsync();
            }
        }
    }

    /// <summary>
    /// Current speed (0-126 for 128 speed steps).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    private int _speed;

    // === Locomotive Series (Baureihe) for Vmax calculation ===

    /// <summary>
    /// Selected locomotive series name (e.g., "BR 103", "ICE 3").
    /// Persisted in settings.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    private string _selectedLocoSeries = string.Empty;

    partial void OnSelectedLocoSeriesChanged(string value)
    {
        _ = value;
        _ = SaveLocoSeriesSettingsAsync();
    }

    /// <summary>
    /// Maximum speed (Vmax) of the selected locomotive series in km/h.
    /// Default: 200 km/h. Persisted in settings.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    private int _selectedVmax = 200;

    partial void OnSelectedVmaxChanged(int value)
    {
        _ = value;
        _ = SaveLocoSeriesSettingsAsync();
    }

    /// <summary>
    /// Calculated speed in km/h based on current speed step and selected Vmax.
    /// Always calculates even without selected locomotive series.
    /// Uses SelectedVmax (default 200 km/h if not set).
    /// Calculation: (Speed / MaxSpeedStep) * Vmax
    /// Example (128 Steps, Vmax 200 km/h):
    /// - Step 126 (max): (126/126) * 200 = 200 km/h
    /// - Step 63 (50%): (63/126) * 200 = 100 km/h
    /// </summary>
    public int SpeedKmh
    {
        get
        {
            // Use SelectedVmax (which defaults to 200 if not explicitly set)
            var vmax = SelectedVmax > 0 ? SelectedVmax : 200;

            // Avoid division by zero
            if (MaxSpeedStep == 0)
            {
                _logger?.LogWarning("MaxSpeedStep is 0! Returning 0 km/h. SpeedSteps={SpeedSteps}", SpeedSteps);
                return 0;
            }

            // Calculate: (Speed / MaxSpeedStep) * Vmax
            var result = (int)Math.Round((double)Speed / MaxSpeedStep * vmax);

            // VALIDATION: Check for unrealistic values (debugging aid)
            if (result > 500)
            {
                _logger?.LogWarning(
                    "SpeedKmh calculation resulted in unrealistic value: {Result} km/h. " +
                    "Speed={Speed}, MaxSpeedStep={MaxSpeedStep}, SelectedVmax={Vmax}, SpeedSteps={SpeedSteps}",
                    result, Speed, MaxSpeedStep, vmax, SpeedSteps);
            }

            return result;
        }
    }

    /// <summary>
    /// Direction: true = forward, false = backward.
    /// </summary>
    [ObservableProperty]
    private bool _isForward = true;

    /// <summary>
    /// Function states F0-F20. Array index corresponds to function number.
    /// </summary>
    [ObservableProperty]
    private bool _isF0On;

    [ObservableProperty]
    private bool _isF1On;

    [ObservableProperty]
    private bool _isF2On;

    [ObservableProperty]
    private bool _isF3On;

    [ObservableProperty]
    private bool _isF4On;

    [ObservableProperty]
    private bool _isF5On;

    [ObservableProperty]
    private bool _isF6On;

    [ObservableProperty]
    private bool _isF7On;

    [ObservableProperty]
    private bool _isF8On;

    [ObservableProperty]
    private bool _isF9On;

    [ObservableProperty]
    private bool _isF10On;

    [ObservableProperty]
    private bool _isF11On;

    [ObservableProperty]
    private bool _isF12On;

    [ObservableProperty]
    private bool _isF13On;

    [ObservableProperty]
    private bool _isF14On;

    [ObservableProperty]
    private bool _isF15On;

    [ObservableProperty]
    private bool _isF16On;

    [ObservableProperty]
    private bool _isF17On;

    [ObservableProperty]
    private bool _isF18On;

    [ObservableProperty]
    private bool _isF19On;

    [ObservableProperty]
    private bool _isF20On;

    /// <summary>
    /// Status message for UI feedback.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Indicates if Z21 is connected.
    /// </summary>
    public bool IsConnected => _z21.IsConnected;

    // === Amperemeter / Current Monitoring ===

    /// <summary>
    /// Main track current consumption in milliamperes (mA).
    /// Updated via Z21 SystemState broadcasts.
    /// </summary>
    [ObservableProperty]
    private int _mainTrackCurrent;

    /// <summary>
    /// Programming track current consumption in milliamperes (mA).
    /// Updated via Z21 SystemState broadcasts.
    /// </summary>
    [ObservableProperty]
    private int _progTrackCurrent;

    /// <summary>
    /// Z21 supply voltage in millivolts (mV).
    /// Typically ~16000 mV (16V) for normal operation.
    /// </summary>
    [ObservableProperty]
    private int _supplyVoltage;

    /// <summary>
    /// Z21 internal temperature in degrees Celsius.
    /// </summary>
    [ObservableProperty]
    private int _temperature;

    /// <summary>
    /// Filtered (smoothed) main track current in milliamperes (mA).
    /// This value is less noisy than MainTrackCurrent and better for trend analysis.
    /// Updated via Z21 SystemState broadcasts.
    /// </summary>
    [ObservableProperty]
    private int _filteredMainCurrent;

    /// <summary>
    /// Peak (maximum) main track current since connection or last reset, in milliamperes (mA).
    /// Useful for identifying maximum load during operation.
    /// </summary>
    [ObservableProperty]
    private int _peakMainCurrent;

    /// <summary>
    /// Peak (maximum) temperature in °C since connection or last reset.
    /// Useful for monitoring maximum thermal load.
    /// </summary>
    [ObservableProperty]
    private int _peakTemperature;

    // === Speed Ramp Configuration ===

    /// <summary>
    /// Enable/disable gradual acceleration when changing direction or starting.
    /// When enabled, speed changes happen gradually instead of instantly.
    /// </summary>
    [ObservableProperty]
    private bool _isRampEnabled = true;

    /// <summary>
    /// Speed step increment per ramp interval (1-20).
    /// Lower values = smoother but slower acceleration.
    /// Default: 5 (moderate acceleration).
    /// </summary>
    [ObservableProperty]
    private double _rampStepSize = 5;

    /// <summary>
    /// Delay between speed steps in milliseconds (50-500).
    /// Lower values = faster acceleration.
    /// Default: 100ms.
    /// </summary>
    [ObservableProperty]
    private double _rampIntervalMs = 100;

    /// <summary>
    /// Indicates if a ramp operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isRamping;

    private CancellationTokenSource? _rampCancellationTokenSource;

    // === Journey & Station Information (for Timetable Display) ===

    private readonly MainWindowViewModel? _mainWindowViewModel;

    /// <summary>
    /// Current journey being executed (if any).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousStationName))]
    [NotifyPropertyChangedFor(nameof(PreviousStationArrival))]
    [NotifyPropertyChangedFor(nameof(PreviousStationDeparture))]
    [NotifyPropertyChangedFor(nameof(PreviousStationTrack))]
    [NotifyPropertyChangedFor(nameof(PreviousStationHasValue))]
    [NotifyPropertyChangedFor(nameof(PreviousStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(CurrentStationName))]
    [NotifyPropertyChangedFor(nameof(CurrentStationArrival))]
    [NotifyPropertyChangedFor(nameof(CurrentStationDeparture))]
    [NotifyPropertyChangedFor(nameof(CurrentStationTrack))]
    [NotifyPropertyChangedFor(nameof(CurrentStationHasValue))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(NextStationName))]
    [NotifyPropertyChangedFor(nameof(NextStationArrival))]
    [NotifyPropertyChangedFor(nameof(NextStationDeparture))]
    [NotifyPropertyChangedFor(nameof(NextStationTrack))]
    [NotifyPropertyChangedFor(nameof(NextStationHasValue))]
    [NotifyPropertyChangedFor(nameof(NextStationIsExitOnLeft))]
    private Domain.Journey? _currentJourney;

    /// <summary>
    /// Current station index in the journey (0-based).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousStationName))]
    [NotifyPropertyChangedFor(nameof(PreviousStationArrival))]
    [NotifyPropertyChangedFor(nameof(PreviousStationDeparture))]
    [NotifyPropertyChangedFor(nameof(PreviousStationTrack))]
    [NotifyPropertyChangedFor(nameof(PreviousStationHasValue))]
    [NotifyPropertyChangedFor(nameof(PreviousStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(CurrentStationName))]
    [NotifyPropertyChangedFor(nameof(CurrentStationArrival))]
    [NotifyPropertyChangedFor(nameof(CurrentStationDeparture))]
    [NotifyPropertyChangedFor(nameof(CurrentStationTrack))]
    [NotifyPropertyChangedFor(nameof(CurrentStationHasValue))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(NextStationName))]
    [NotifyPropertyChangedFor(nameof(NextStationArrival))]
    [NotifyPropertyChangedFor(nameof(NextStationDeparture))]
    [NotifyPropertyChangedFor(nameof(NextStationTrack))]
    [NotifyPropertyChangedFor(nameof(NextStationHasValue))]
    [NotifyPropertyChangedFor(nameof(NextStationIsExitOnLeft))]
    private int _currentStationIndex;

    // === Computed Properties for TimetableStopsControl ===

    private const string StationPlaceholder = "\u2014";

    /// <summary>
    /// Provides TimetableStopsControl with the previous station name, using a placeholder when none.
    /// </summary>
    public string PreviousStationName => GetPreviousStation()?.Name ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the previous station arrival time.
    /// </summary>
    public string PreviousStationArrival => GetPreviousStation()?.Arrival?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the previous station departure time.
    /// </summary>
    public string PreviousStationDeparture => GetPreviousStation()?.Departure?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the previous station track value.
    /// </summary>
    public string PreviousStationTrack => GetPreviousStation()?.Track?.ToString() ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no previous station.
    /// </summary>
    public bool PreviousStationHasValue => GetPreviousStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the previous station exit direction icon.
    /// </summary>
    public bool PreviousStationIsExitOnLeft => GetPreviousStation()?.IsExitOnLeft ?? false;

    /// <summary>
    /// Provides TimetableStopsControl with the current station name, using a placeholder when none.
    /// </summary>
    public string CurrentStationName => GetCurrentStation()?.Name ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the current station arrival time.
    /// </summary>
    public string CurrentStationArrival => GetCurrentStation()?.Arrival?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the current station departure time.
    /// </summary>
    public string CurrentStationDeparture => GetCurrentStation()?.Departure?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the current station track value.
    /// </summary>
    public string CurrentStationTrack => GetCurrentStation()?.Track?.ToString() ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no current station.
    /// </summary>
    public bool CurrentStationHasValue => GetCurrentStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the current station exit direction icon.
    /// </summary>
    public bool CurrentStationIsExitOnLeft => GetCurrentStation()?.IsExitOnLeft ?? false;

    /// <summary>
    /// Provides TimetableStopsControl with the next station name, using a placeholder when none.
    /// </summary>
    public string NextStationName => GetNextStation()?.Name ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the next station arrival time.
    /// </summary>
    public string NextStationArrival => GetNextStation()?.Arrival?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the next station departure time.
    /// </summary>
    public string NextStationDeparture => GetNextStation()?.Departure?.ToString("HH:mm") ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the next station track value.
    /// </summary>
    public string NextStationTrack => GetNextStation()?.Track?.ToString() ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no next station.
    /// </summary>
    public bool NextStationHasValue => GetNextStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the next station exit direction icon.
    /// </summary>
    public bool NextStationIsExitOnLeft => GetNextStation()?.IsExitOnLeft ?? false;

    private Domain.Station? GetPreviousStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
            return null;

        var prevIndex = CurrentStationIndex - 1;
        if (prevIndex < 0 || prevIndex >= CurrentJourney.Stations.Count)
            return null;

        return CurrentJourney.Stations[prevIndex];
    }

    private Domain.Station? GetCurrentStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
            return null;

        if (CurrentStationIndex < 0 || CurrentStationIndex >= CurrentJourney.Stations.Count)
            return null;

        return CurrentJourney.Stations[CurrentStationIndex];
    }

    private Domain.Station? GetNextStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
            return null;

        var nextIndex = CurrentStationIndex + 1;
        if (nextIndex < 0 || nextIndex >= CurrentJourney.Stations.Count)
            return null;

        return CurrentJourney.Stations[nextIndex];
    }

    public TrainControlViewModel(
        IZ21 z21,
        IEventBus eventBus,
        ISettingsService settingsService,
        MainWindowViewModel? mainWindowViewModel = null,
        Backend.Interface.ITripLogService? tripLogService = null,
        ILogger<TrainControlViewModel>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(settingsService);
        _z21 = z21;
        _settingsService = settingsService;
        _mainWindowViewModel = mainWindowViewModel;
        _tripLogService = tripLogService;
        _logger = logger;

        // Load presets from settings
        LoadPresetsFromSettings();

        // Subscribe to Z21 events via EventBus (UiThreadEventBusDecorator führt Handler auf UI-Thread aus)
        eventBus.Subscribe<Z21ConnectionEstablishedEvent>(_ => OnZ21ConnectionChanged(true));
        eventBus.Subscribe<Z21ConnectionLostEvent>(_ => OnZ21ConnectionChanged(false));
        eventBus.Subscribe<LocomotiveInfoChangedEvent>(evt => OnLocoInfoReceived(CreateLocoInfo(evt)));
        eventBus.Subscribe<SystemStateChangedEvent>(evt => OnSystemStateChanged(CreateSystemState(evt)));

        // Subscribe to MainWindowViewModel.SelectedJourney changes
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.PropertyChanged += OnMainWindowViewModelPropertyChanged;

            // Initialize with current journey if available
            UpdateJourneyFromMainViewModel();
        }
    }

    /// <summary>
    /// Called when MainWindowViewModel properties change.
    /// Updates CurrentJourney when SelectedJourney changes.
    /// </summary>
    private void OnMainWindowViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedJourney))
        {
            UpdateJourneyFromMainViewModel();
        }
    }

    /// <summary>
    /// Updates CurrentJourney and CurrentStationIndex from MainWindowViewModel.SelectedJourney.
    /// </summary>
    private void UpdateJourneyFromMainViewModel()
    {
        if (_mainWindowViewModel?.SelectedJourney == null)
        {
            CurrentJourney = null;
            CurrentStationIndex = 0;
            return;
        }

        var journeyVm = _mainWindowViewModel.SelectedJourney;
        CurrentJourney = journeyVm.Model;
        CurrentStationIndex = journeyVm.CurrentPos;

        // Subscribe to journey CurrentPos changes
        journeyVm.PropertyChanged -= OnJourneyViewModelPropertyChanged;
        journeyVm.PropertyChanged += OnJourneyViewModelPropertyChanged;
    }

    /// <summary>
    /// Called when JourneyViewModel properties change.
    /// Updates CurrentStationIndex when CurrentPos changes.
    /// </summary>
    private void OnJourneyViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JourneyViewModel.CurrentPos) && sender is JourneyViewModel journeyVm)
        {
            CurrentStationIndex = journeyVm.CurrentPos;
        }
    }

    /// <summary>
    /// Loads locomotive presets from persistent settings.
    /// </summary>
    private void LoadPresetsFromSettings()
    {
        var settings = _settingsService.GetSettings();
        var trainControl = settings.TrainControl;

        if (trainControl.Presets.Count >= 3)
        {
            Preset1 = trainControl.Presets[0];
            Preset2 = trainControl.Presets[1];
            Preset3 = trainControl.Presets[2];

            // Notify UI about loaded addresses
            OnPropertyChanged(nameof(Preset1Address));
            OnPropertyChanged(nameof(Preset2Address));
            OnPropertyChanged(nameof(Preset3Address));
        }

        SelectedPresetIndex = trainControl.SelectedPresetIndex;
        RampStepSize = trainControl.SpeedRampStepSize;
        RampIntervalMs = trainControl.SpeedRampIntervalMs;
        SpeedSteps = trainControl.SpeedSteps;

        // Load locomotive series selection
        SelectedLocoSeries = trainControl.SelectedLocoSeries;
        SelectedVmax = trainControl.SelectedVmax;

        // Apply current preset
        ApplyCurrentPreset();
    }

    /// <summary>
    /// Saves locomotive series selection to persistent settings.
    /// </summary>
    private async Task SaveLocoSeriesSettingsAsync()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            settings.TrainControl.SelectedLocoSeries = SelectedLocoSeries;
            settings.TrainControl.SelectedVmax = SelectedVmax;

            await _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save locomotive series settings");
        }
    }

    /// <summary>
    /// Saves locomotive presets to persistent settings.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Get current settings from service
            var settings = _settingsService.GetSettings();

            // Update TrainControl presets with current values
            settings.TrainControl.Presets = [Preset1, Preset2, Preset3];
            settings.TrainControl.SelectedPresetIndex = SelectedPresetIndex;
            settings.TrainControl.SpeedRampStepSize = (int)RampStepSize;
            settings.TrainControl.SpeedRampIntervalMs = (int)RampIntervalMs;
            settings.TrainControl.SpeedSteps = SpeedSteps;

            // Save updated settings
            await _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);

            _logger?.LogInformation(
                "Saved train control settings: Preset1={P1Addr}, Preset2={P2Addr}, Preset3={P3Addr}",
                Preset1.DccAddress, Preset2.DccAddress, Preset3.DccAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save train control settings");
        }
    }

    /// <summary>
    /// Saves presets to persistent storage (wrapper for SaveSettingsAsync).
    /// </summary>
    private async Task SavePresetsToSettingsAsync()
    {
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the current preset to the ViewModel state.
    /// Speed and direction are always reset to safe defaults (0, forward).
    /// </summary>
    private void ApplyCurrentPreset()
    {
        _isLoadingPreset = true;
        try
        {
            var preset = CurrentPreset;
            LocoAddress = preset.DccAddress;

            // Always start at speed 0 (safety feature - no unexpected movement)
            Speed = 0;

            // Always start in forward direction
            IsForward = true;

            _logger?.LogInformation(
                "Applied preset: {Name} - DCC={DccAddress}, Speed={Speed} (always 0), SpeedKmh={SpeedKmh}, " +
                "MaxSpeedStep={MaxSpeedStep}, SpeedSteps={SpeedSteps}, SelectedVmax={Vmax}",
                preset.Name, preset.DccAddress, Speed, SpeedKmh, MaxSpeedStep, SpeedSteps, SelectedVmax);

            // Apply function states from bitmask
            for (int i = 0; i <= 20; i++)
            {
                SetFunctionState(i, preset.GetFunction(i));
            }

            StatusMessage = $"Loaded: {preset.Name} (DCC {preset.DccAddress})";
            OnPropertyChanged(nameof(CurrentPreset));
        }
        finally
        {
            _isLoadingPreset = false;
        }
    }

    /// <summary>
    /// Saves current state to the selected preset.
    /// Speed and direction are NOT saved (always reset to safe defaults on load).
    /// </summary>
    private void SaveCurrentStateToPreset()
    {
        if (_isLoadingPreset) return;

        var preset = CurrentPreset;
        preset.DccAddress = LocoAddress;

        // Speed and IsForward are NOT saved - always reset to 0/forward on load
        // This is a safety feature to prevent unexpected locomotive movement

        // Save function states to bitmask
        for (int i = 0; i <= 20; i++)
        {
            preset.SetFunction(i, GetFunctionState(i));
        }

        // Save to persistent storage (fire and forget)
        _ = SavePresetsToSettingsAsync();
    }

    /// <summary>
    /// Called when SelectedPresetIndex changes - save current and load new preset.
    /// </summary>
    partial void OnSelectedPresetIndexChanged(int value)
    {
        ApplyCurrentPreset();
    }

    private void OnZ21ConnectionChanged(bool isConnected)
    {
        OnPropertyChanged(nameof(IsConnected));
        SetSpeedCommand.NotifyCanExecuteChanged();
        ToggleF0Command.NotifyCanExecuteChanged();
        ToggleF1Command.NotifyCanExecuteChanged();
        ToggleF2Command.NotifyCanExecuteChanged();
        ToggleF3Command.NotifyCanExecuteChanged();
        ToggleF4Command.NotifyCanExecuteChanged();
        ToggleF5Command.NotifyCanExecuteChanged();
        ToggleF6Command.NotifyCanExecuteChanged();
        ToggleF7Command.NotifyCanExecuteChanged();
        ToggleF8Command.NotifyCanExecuteChanged();
        ToggleF9Command.NotifyCanExecuteChanged();
        ToggleF10Command.NotifyCanExecuteChanged();
        ToggleF11Command.NotifyCanExecuteChanged();
        ToggleF12Command.NotifyCanExecuteChanged();
        ToggleF13Command.NotifyCanExecuteChanged();
        ToggleF14Command.NotifyCanExecuteChanged();
        ToggleF15Command.NotifyCanExecuteChanged();
        ToggleF16Command.NotifyCanExecuteChanged();
        ToggleF17Command.NotifyCanExecuteChanged();
        ToggleF18Command.NotifyCanExecuteChanged();
        ToggleF19Command.NotifyCanExecuteChanged();
        ToggleF20Command.NotifyCanExecuteChanged();
        EmergencyStopCommand.NotifyCanExecuteChanged();
        StatusMessage = isConnected ? "Z21 Connected" : "Z21 Disconnected";
    }

    private void OnLocoInfoReceived(LocoInfo info)
    {
        // Only update if this is our current loco
        if (info.Address != LocoAddress) return;

        // Update local state from Z21 response (EventBus handler runs on UI thread)
        Speed = info.Speed;
        IsForward = info.IsForward;
        IsF0On = info.IsF0On;
        IsF1On = info.IsF1On;
            StatusMessage = $"Loco {info.Address}: {info.Speed} km/h {(info.IsForward ? "â†’" : "â†")}";
    }

    /// <summary>
    /// Called when LocoAddress changes - request current state from Z21 and save to preset.
    /// </summary>
    partial void OnLocoAddressChanged(int value)
    {
        // Save to current preset
        if (!_isLoadingPreset)
        {
            CurrentPreset.DccAddress = value;
            _ = SavePresetsToSettingsAsync();

            // Fahrtenbuch: Adresswechsel protokollieren
            _tripLogService?.RecordStateChange(
                _mainWindowViewModel?.SelectedProject?.Model,
                value,
                Speed,
                DateTime.UtcNow);
        }

        if (value >= 1 && value <= 9999 && _z21.IsConnected)
        {
            _ = RequestLocoInfoAsync();
        }
    }

    private async Task RequestLocoInfoAsync()
    {
        try
        {
            await _z21.GetLocoInfoAsync(LocoAddress);
            StatusMessage = $"Requesting loco {LocoAddress}...";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to request loco info for {Address}", LocoAddress);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Called when Speed changes - send to Z21 and save to preset.
    /// </summary>
    partial void OnSpeedChanged(int value)
    {
        if (_skipSpeedChangeHandler || _isLoadingPreset) return;

        // Save to current preset
        CurrentPreset.Speed = value;
        _ = SavePresetsToSettingsAsync();

        // Fahrtenbuch: Geschwindigkeitsänderung protokollieren
        _tripLogService?.RecordStateChange(
            _mainWindowViewModel?.SelectedProject?.Model,
            LocoAddress,
            value,
            DateTime.UtcNow);

        if (CanExecuteLocoCommand() && LocoAddress >= 1)
        {
            _ = SendDriveCommandAsync();
        }
    }

    /// <summary>
    /// Called when IsForward changes - ramp down to 0, then ramp up in new direction.
    /// This prevents derailment from sudden direction changes at speed.
    /// </summary>
    partial void OnIsForwardChanged(bool value)
    {
        if (_isLoadingPreset) return;

        // Save to current preset
        CurrentPreset.IsForward = value;
        _ = SavePresetsToSettingsAsync();

        if (_z21.IsConnected && LocoAddress >= 1)
        {
            _ = HandleDirectionChangeAsync(value);
        }
    }

    /// <summary>
    /// Handles direction change with optional ramping.
    /// If ramp is enabled and speed > 0, ramps down to 0, changes direction, then ramps back up.
    /// </summary>
    private async Task HandleDirectionChangeAsync(bool newDirection)
    {
        if (!IsRampEnabled || Speed == 0)
        {
            // No ramp needed - send command immediately
            await SendDriveCommandAsync();
            return;
        }

        // Cancel any existing ramp operation
        CancelRamp();

        var targetSpeed = Speed;
        _rampCancellationTokenSource = new CancellationTokenSource();
        var token = _rampCancellationTokenSource.Token;

        try
        {
            IsRamping = true;
            StatusMessage = "Ramping down for direction change...";
            _logger?.LogDebug("Direction change: ramping from {Speed} to 0", Speed);

            // Ramp down to 0 (in old direction - but IsForward already changed!)
            // We need to send commands with the OLD direction until we reach 0
            var oldDirection = !newDirection;
            await RampSpeedAsync(Speed, 0, oldDirection, token);

            if (token.IsCancellationRequested) return;

            // Now at speed 0, send the new direction
            await _z21.SetLocoDriveAsync(LocoAddress, 0, newDirection);

            if (token.IsCancellationRequested) return;

            // Ramp back up to target speed in new direction
            StatusMessage = "Ramping up in new direction...";
            _logger?.LogDebug("Direction change: ramping from 0 to {Speed}", targetSpeed);
            await RampSpeedAsync(0, targetSpeed, newDirection, token);

            StatusMessage = $"Loco {LocoAddress}: {Speed} {(newDirection ? "forward" : "backward")}";
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Ramp operation cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during direction change ramp");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRamping = false;
        }
    }

    /// <summary>
    /// Gradually changes speed from current to target value.
    /// </summary>
    /// <param name="fromSpeed">Starting speed</param>
    /// <param name="toSpeed">Target speed</param>
    /// <param name="direction">Direction to use for commands</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task RampSpeedAsync(int fromSpeed, int toSpeed, bool direction, CancellationToken cancellationToken)
    {
        var currentSpeed = fromSpeed;
        var stepSize = (int)Math.Clamp(RampStepSize, 1, 20);
        var intervalMs = (int)Math.Clamp(RampIntervalMs, 50, 500);
        var step = toSpeed > fromSpeed ? stepSize : -stepSize;

        while (currentSpeed != toSpeed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate next speed
            currentSpeed = step > 0 ? Math.Min(currentSpeed + stepSize, toSpeed) : Math.Max(currentSpeed - stepSize, toSpeed);

            // Update the Speed property (this triggers UI update but NOT another ramp)
            _skipSpeedChangeHandler = true;
            Speed = currentSpeed;
            _skipSpeedChangeHandler = false;

            // Send command to Z21
            await _z21.SetLocoDriveAsync(LocoAddress, currentSpeed, direction);

            // Wait before next step
            if (currentSpeed != toSpeed)
            {
                await Task.Delay(intervalMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Cancels any ongoing ramp operation.
    /// </summary>
    private void CancelRamp()
    {
        _rampCancellationTokenSource?.Cancel();
        _rampCancellationTokenSource?.Dispose();
        _rampCancellationTokenSource = null;
    }

    private bool _skipSpeedChangeHandler;

    /// <summary>
    /// Locomotive command execution check used for UI testing without hardware.
    /// </summary>
    /// <remarks>
    /// TODO: Re-enable the Z21 connection check when hardware is available.
    /// </remarks>
    private bool CanExecuteLocoCommand() => true;

    // private bool CanExecuteLocoCommand() => _z21.IsConnected && LocoAddress >= 1 && LocoAddress <= 9999;

    /// <summary>
    /// Sends the current speed and direction to Z21.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task SetSpeedAsync()
    {
        await SendDriveCommandAsync();
    }

    private async Task SendDriveCommandAsync()
    {
        try
        {
            await _z21.SetLocoDriveAsync(LocoAddress, Speed, IsForward);
            StatusMessage = $"Loco {LocoAddress}: {Speed} {(IsForward ? "FWD" : "REV")}";
            _logger?.LogDebug("Drive command sent: Loco {Address}, Speed {Speed}, Forward {Forward}",
                LocoAddress, Speed, IsForward);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send drive command");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles F0 (Light) function.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF0Async() => await ToggleFunctionAsync(0);

    /// <summary>
    /// Toggles F1 (Sound) function.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF1Async() => await ToggleFunctionAsync(1);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF2Async() => await ToggleFunctionAsync(2);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF3Async() => await ToggleFunctionAsync(3);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF4Async() => await ToggleFunctionAsync(4);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF5Async() => await ToggleFunctionAsync(5);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF6Async() => await ToggleFunctionAsync(6);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF7Async() => await ToggleFunctionAsync(7);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF8Async() => await ToggleFunctionAsync(8);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF9Async() => await ToggleFunctionAsync(9);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF10Async() => await ToggleFunctionAsync(10);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF11Async() => await ToggleFunctionAsync(11);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF12Async() => await ToggleFunctionAsync(12);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF13Async() => await ToggleFunctionAsync(13);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF14Async() => await ToggleFunctionAsync(14);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF15Async() => await ToggleFunctionAsync(15);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF16Async() => await ToggleFunctionAsync(16);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF17Async() => await ToggleFunctionAsync(17);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF18Async() => await ToggleFunctionAsync(18);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF19Async() => await ToggleFunctionAsync(19);

    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task ToggleF20Async() => await ToggleFunctionAsync(20);

    /// <summary>
    /// Generic function toggle implementation.
    /// Public method to allow direct UI event handling (bypasses CanExecute).
    /// </summary>
    public async Task ToggleFunctionAsync(int functionNumber)
    {
        try
        {
            var newState = !GetFunctionState(functionNumber);
            SetFunctionState(functionNumber, newState);

            // Save function state to current preset
            if (!_isLoadingPreset)
            {
                CurrentPreset.SetFunction(functionNumber, newState);
                _ = SavePresetsToSettingsAsync();
            }

            await _z21.SetLocoFunctionAsync(LocoAddress, functionNumber, newState);
            StatusMessage = $"F{functionNumber}: {(newState ? "ON" : "OFF")}";
            _logger?.LogDebug("F{Function} toggled: {State}", functionNumber, newState);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle F{Function}", functionNumber);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool GetFunctionState(int functionNumber) => functionNumber switch
    {
        0 => IsF0On,
        1 => IsF1On,
        2 => IsF2On,
        3 => IsF3On,
        4 => IsF4On,
        5 => IsF5On,
        6 => IsF6On,
        7 => IsF7On,
        8 => IsF8On,
        9 => IsF9On,
        10 => IsF10On,
        11 => IsF11On,
        12 => IsF12On,
        13 => IsF13On,
        14 => IsF14On,
        15 => IsF15On,
        16 => IsF16On,
        17 => IsF17On,
        18 => IsF18On,
        19 => IsF19On,
        20 => IsF20On,
        _ => false
    };

    private void SetFunctionState(int functionNumber, bool state)
    {
        switch (functionNumber)
        {
            case 0: IsF0On = state; break;
            case 1: IsF1On = state; break;
            case 2: IsF2On = state; break;
            case 3: IsF3On = state; break;
            case 4: IsF4On = state; break;
            case 5: IsF5On = state; break;
            case 6: IsF6On = state; break;
            case 7: IsF7On = state; break;
            case 8: IsF8On = state; break;
            case 9: IsF9On = state; break;
            case 10: IsF10On = state; break;
            case 11: IsF11On = state; break;
            case 12: IsF12On = state; break;
            case 13: IsF13On = state; break;
            case 14: IsF14On = state; break;
            case 15: IsF15On = state; break;
            case 16: IsF16On = state; break;
            case 17: IsF17On = state; break;
            case 18: IsF18On = state; break;
            case 19: IsF19On = state; break;
            case 20: IsF20On = state; break;
        }
    }

    /// <summary>
    /// Emergency stop for the current locomotive.
    /// Sets speed to 0 immediately.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task EmergencyStopAsync()
    {
        try
        {
            Speed = 0;
            await _z21.SetLocoDriveAsync(LocoAddress, 0, IsForward);
            StatusMessage = $"[STOP] Emergency stop - Loco {LocoAddress}";
            _logger?.LogWarning("Emergency stop executed for loco {Address}", LocoAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute emergency stop");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggle direction (Forward/Backward).
    /// </summary>
    [RelayCommand]
    private void ToggleDirection()
    {
        IsForward = !IsForward;
    }

    /// <summary>
    /// Stop command (sets speed to 0, keeps functions).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private async Task StopAsync()
    {
        try
        {
            Speed = 0;
            await _z21.SetLocoDriveAsync(LocoAddress, 0, IsForward);
            StatusMessage = $"Loco {LocoAddress} stopped";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop locomotive");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Sets speed to a preset value.
    /// Preset values based on typical railway speed limits:
    /// - 20: Shunting/Rangieren (~25 km/h)
    /// - 40: Slow/Station (~50 km/h)
    /// - 60: Normal (~80 km/h)
    /// - 80: Fast (~120 km/h)
    /// - 100: Express (~160 km/h)
    /// - 126: Maximum
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private void SetSpeedPreset(object? presetParam)
    {
        var preset = presetParam switch
        {
            int i => i,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => 0
        };

        Speed = Math.Clamp(preset, 0, 126);
        _logger?.LogDebug("Speed preset set to {Preset}", preset);
    }

    /// <summary>
    /// Selects locomotive preset 1.
    /// </summary>
    [RelayCommand]
    private void SelectPreset1()
    {
        if (SelectedPresetIndex != 0)
        {
            SaveCurrentStateToPreset();
            SelectedPresetIndex = 0;
        }
    }

    /// <summary>
    /// Selects locomotive preset 2.
    /// </summary>
    [RelayCommand]
    private void SelectPreset2()
    {
        if (SelectedPresetIndex != 1)
        {
            SaveCurrentStateToPreset();
            SelectedPresetIndex = 1;
        }
    }

    /// <summary>
    /// Selects locomotive preset 3.
    /// </summary>
    [RelayCommand]
    private void SelectPreset3()
    {
        if (SelectedPresetIndex != 2)
        {
            SaveCurrentStateToPreset();
            SelectedPresetIndex = 2;
        }
    }

    /// <summary>
    /// Resets the peak current tracking back to zero.
    /// Useful after analyzing maximum load or starting a new session.
    /// </summary>
    [RelayCommand]
    private void ResetPeakCurrent()
    {
        PeakMainCurrent = 0;
        PeakTemperature = 0;
        _logger?.LogInformation("Peak current and peak temperature reset");
    }

    // === Event Handlers ===

    /// <summary>
    /// Called when Z21 system state changes (main track current, temperature, voltage).
    /// Updates amperemeter display values on UI thread.
    /// </summary>
    private void OnSystemStateChanged(SystemState systemState)
    {
        MainTrackCurrent = systemState.MainCurrent;
        ProgTrackCurrent = systemState.ProgCurrent;
        FilteredMainCurrent = systemState.FilteredMainCurrent;
        SupplyVoltage = systemState.SupplyVoltage;
        Temperature = systemState.Temperature;

        if (systemState.MainCurrent > PeakMainCurrent)
            PeakMainCurrent = systemState.MainCurrent;
        if (systemState.Temperature > PeakTemperature)
            PeakTemperature = systemState.Temperature;

        _logger?.LogDebug(
            "SystemState updated: MainCurrent={MainCurrent}mA (Filtered={FilteredCurrent}mA, Peak={PeakCurrent}mA), " +
            "ProgCurrent={ProgCurrent}mA, SupplyVoltage={SupplyVoltage}mV, Temperature={Temperature}°C",
            MainTrackCurrent, FilteredMainCurrent, PeakMainCurrent, ProgTrackCurrent, SupplyVoltage, Temperature);
    }

    private static LocoInfo CreateLocoInfo(LocomotiveInfoChangedEvent evt)
    {
        return new LocoInfo
        {
            Address = evt.Address,
            Speed = evt.Speed,
            IsForward = evt.IsForward,
            Functions = BuildFunctions(evt)
        };
    }

    private static uint BuildFunctions(LocomotiveInfoChangedEvent evt)
    {
        uint functions = 0;
        if (evt.IsF0On) functions |= 1u << 0;
        if (evt.IsF1On) functions |= 1u << 1;
        if (evt.IsF2On) functions |= 1u << 2;
        if (evt.IsF3On) functions |= 1u << 3;
        if (evt.IsF4On) functions |= 1u << 4;
        if (evt.IsF5On) functions |= 1u << 5;
        if (evt.IsF6On) functions |= 1u << 6;
        if (evt.IsF7On) functions |= 1u << 7;
        if (evt.IsF8On) functions |= 1u << 8;
        if (evt.IsF9On) functions |= 1u << 9;
        if (evt.IsF10On) functions |= 1u << 10;
        if (evt.IsF11On) functions |= 1u << 11;
        if (evt.IsF12On) functions |= 1u << 12;
        if (evt.IsF13On) functions |= 1u << 13;
        if (evt.IsF14On) functions |= 1u << 14;
        if (evt.IsF15On) functions |= 1u << 15;
        if (evt.IsF16On) functions |= 1u << 16;
        if (evt.IsF17On) functions |= 1u << 17;
        if (evt.IsF18On) functions |= 1u << 18;
        if (evt.IsF19On) functions |= 1u << 19;
        if (evt.IsF20On) functions |= 1u << 20;
        return functions;
    }

    private static SystemState CreateSystemState(SystemStateChangedEvent evt)
    {
        return new SystemState
        {
            MainCurrent = evt.MainCurrent,
            ProgCurrent = evt.ProgCurrent,
            FilteredMainCurrent = evt.FilteredMainCurrent,
            Temperature = evt.Temperature,
            SupplyVoltage = evt.SupplyVoltage,
            VccVoltage = evt.VccVoltage,
            CentralState = unchecked((byte)evt.CentralState),
            CentralStateEx = unchecked((byte)evt.CentralStateEx)
        };
    }
}
