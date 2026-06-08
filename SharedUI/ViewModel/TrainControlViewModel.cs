// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Configuration;
using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// ViewModel for TrainControlPage - provides locomotive drive control interface.
/// Implements a "digital throttle" similar to the Roco Z21 app hand controller.
/// 
/// Features:
/// - 3 locomotive presets with persistent DCC addresses, speed, and function states
/// - Speed control (0-126 for 128 speed steps)
/// - Direction toggle (Forward/Backward)
/// - Function keys F0-F31
/// - Emergency stop
/// - Speed ramping for smooth direction changes
/// 
/// Cross-platform: Used by WinUI and MAUI.
/// </summary>
public sealed partial class TrainControlViewModel : ObservableObject
{
    private readonly IMobaRuntime _mobaRuntime;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrainControlViewModel>? _logger;
    private readonly IUiDispatcher? _uiDispatcher;

    private bool _isLoadingPreset;
    private bool _isApplyingRuntimeLocomotiveState;

    // When a locomotive is selected we force all functions off. The decoder's loco-info response
    // may still report the old (on) function bits and race our OFF command, so we ignore incoming
    // snapshot function bits until the user manually toggles a function again.
    private bool _suppressSnapshotFunctionState;
    private int _previousSpeed;
    private CancellationTokenSource? _doorReleaseBlinkCts;

    private const string SignalWhiteHex = "#8A8A8A";
    private const string SignalGreenHex = "#06D6A0";
    private const string SignalYellowHex = "#FFD700";
    private const string SignalRedHex = "#E63946";
    private const string SignalGrayHex = "#888888";  // Neutral gray for inactive state (visible in both light/dark)

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
    public int MaxSpeedStep => TrainControlDccSpeed.GetMaxSpeedStep(SpeedSteps);

    /// <summary>
    /// DCC locomotive address (1-9999).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetSpeedCommand))]
    [NotifyCanExecuteChangedFor(nameof(EmergencyStopCommand))]
    private int _locoAddress = 3;

    // === Locomotive Presets ===

    /// <summary>
    /// Currently selected preset index (0, 1, or 2). -1 = locomotive selected from project combo box.
    /// </summary>
    [ObservableProperty]
    private int _selectedPresetIndex;

    private static readonly ObservableCollection<LocomotiveViewModel> EmptyProjectLocomotives = [];

    /// <summary>
    /// Locomotives of the current project for combo box selection.
    /// Either preset (Loco 1/2/3) or a locomotive from this list is controlled.
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> ProjectLocomotives =>
        _mainWindowViewModel?.SelectedProject?.Locomotives ?? EmptyProjectLocomotives;

    /// <summary>
    /// Locomotive selected from project combo box. When set, this one is controlled (SelectedPresetIndex = -1).
    /// When switching to a preset, this is set to null.
    /// </summary>
    [ObservableProperty]
    private LocomotiveViewModel? _selectedLocomotiveFromProject;

    partial void OnSelectedLocomotiveFromProjectChanged(LocomotiveViewModel? value)
    {
        if (value != null)
        {
            SelectedPresetIndex = -1;
            var addr = value.Model.DigitalAddress;
            LocoAddress = addr.HasValue ? (int)addr.Value : 0;
            Speed = 0;
            IsForward = true;
            StatusMessage = $"Loco from project: {value.Name} (DCC {LocoAddress})";

            // Turn all function keys off on selection (UI + explicit OFF to Z21).
            // Set synchronously so an incoming loco-info snapshot cannot re-enable functions.
            _suppressSnapshotFunctionState = true;
            QueueBackgroundTask(TurnOffAllFunctionsAsync(), "Turn off all functions");
        }

        if (!_isLoadingPreset)
        {
            QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
        }
    }

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
                QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
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
                QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
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
                QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
            }
        }
    }

    /// <summary>
    /// Current speed (0-126 for 128 speed steps).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    [NotifyPropertyChangedFor(nameof(DoorReleaseButtonColorHex))]
    [NotifyPropertyChangedFor(nameof(IsDoorReleaseButtonEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDoorReleaseCommand))]
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
        QueueBackgroundTask(SaveLocoSeriesSettingsAsync(), "Save locomotive series settings");
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
        QueueBackgroundTask(SaveLocoSeriesSettingsAsync(), "Save locomotive series settings");
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
    /// Function buttons F0–F31. Replaces the former 32 IsF#On properties; each item holds its
    /// index, label, backlight color and on/off state. The symbol and color are refreshed
    /// per locomotive via <see cref="NotifyAllFunctionAppearanceChanged"/>.
    /// </summary>
    public ObservableCollection<FunctionButtonViewModel> Functions { get; } = CreateFunctionButtons();

    /// <summary>
    /// Default backlight accent colors for F0–F31 (migrated from the former XAML converter parameters).
    /// </summary>
    private static readonly string[] FunctionBacklightColors =
    {
        "#FFD700", "#0078D4", "#FF8C00", "#E81123", "#107C10", "#00B7C3", "#FFB900", "#767676",
        "#E81B23", "#7B68EE", "#8764B8", "#038387", "#C239B3", "#FF1493", "#7A7574", "#567C73",
        "#8E562E", "#847545", "#525E54", "#4A5459", "#69797E", "#69797E", "#69797E", "#69797E",
        "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E"
    };

    private static ObservableCollection<FunctionButtonViewModel> CreateFunctionButtons()
    {
        var collection = new ObservableCollection<FunctionButtonViewModel>();
        for (int i = 0; i < FunctionBacklightColors.Length; i++)
            collection.Add(new FunctionButtonViewModel(i, FunctionBacklightColors[i]));
        return collection;
    }

    // === Brake (locomotive: parking brake/spring brake) and door release ===
    // Flow: speed 0 → brake on → release door; end: close door → release brake → drive.

    /// <summary>
    /// Brake applied (true = red, train cannot move; false = green, train can move if doors closed).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakeButtonColorHex))]
    [NotifyPropertyChangedFor(nameof(DoorReleaseButtonColorHex))]
    [NotifyPropertyChangedFor(nameof(IsBrakeButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(IsDoorReleaseButtonEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ToggleBrakeCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDoorReleaseCommand))]
    private bool _isParkingBrakeEnabled;

    /// <summary>
    /// Door release locked (doors closed): Icon DoorClose. When released (doors open), speed cannot be increased.
    /// Default true = doors closed, so brake can be released again after applying.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDoorCloseIconVisible))]
    [NotifyPropertyChangedFor(nameof(DoorReleaseButtonColorHex))]
    [NotifyPropertyChangedFor(nameof(IsDoorReleaseButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(IsBrakeButtonEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDoorReleaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleBrakeCommand))]
    private bool _isDoorReleaseLocked = true;

    /// <summary>
    /// During the 5-second transition phase the button blinks yellow (color + opacity change).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDoorCloseIconVisible))]
    [NotifyPropertyChangedFor(nameof(DoorReleaseButtonColorHex))]
    [NotifyPropertyChangedFor(nameof(IsDoorReleaseButtonEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDoorReleaseCommand))]
    private bool _isDoorReleaseBlinking;

    /// <summary>
    /// Opacity of the door release button: alternating 1.0 / 0.35 during blinking, otherwise 1.0.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DoorBlockedIndicatorOpacity))]
    private double _doorReleaseBlinkOpacity = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DoorBlockedIndicatorColorHex))]
    [NotifyPropertyChangedFor(nameof(DoorBlockedIndicatorIconColorHex))]
    [NotifyPropertyChangedFor(nameof(DoorBlockedIndicatorOpacity))]
    private bool _isDoorBlocked;

    /// <summary>
    /// Target state of door release after the 5-second blink phase expires.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDoorCloseIconVisible))]
    private bool _isDoorReleaseLockedNext;

    /// <summary>
    /// True = show DoorClose icon, False = DoorOpen icon (for ContentTemplate binding).
    /// </summary>
    public bool IsDoorCloseIconVisible => (IsDoorReleaseBlinking && IsDoorReleaseLockedNext) || (!IsDoorReleaseBlinking && IsDoorReleaseLocked);

    /// <summary>
    /// Background color of brake button: green = released, red = applied.
    /// </summary>
    public string BrakeButtonColorHex => IsParkingBrakeEnabled ? SignalRedHex : SignalGreenHex;

    /// <summary>
    /// Brake button: apply always possible (sets speed to 0). Release only when door release ended (doors closed).
    /// </summary>
    public bool IsBrakeButtonEnabled => !IsParkingBrakeEnabled || IsDoorReleaseLocked;

    /// <summary>
    /// Segoe Fluent glyph for brake: Pause (applied) or Play (released).
    /// </summary>
    public string BrakeButtonGlyph => IsParkingBrakeEnabled ? "\uE72E" : "\uE102";

    /// <summary>
    /// Background color of door release button: white when not relevant, green when released, red when locked, yellow when blinking.
    /// </summary>
    public string DoorReleaseButtonColorHex => !IsParkingBrakeEnabled ? SignalWhiteHex : IsDoorReleaseBlinking ? SignalYellowHex : (!IsDoorReleaseLocked && SpeedKmh == 0 ? SignalGreenHex : SignalRedHex);

    public string DoorBlockedIndicatorColorHex => IsDoorBlocked ? SignalYellowHex : SignalWhiteHex;

    public string DoorBlockedIndicatorIconColorHex => IsDoorBlocked ? SignalRedHex : SignalGrayHex;

    public double DoorBlockedIndicatorOpacity => IsDoorBlocked ? DoorReleaseBlinkOpacity : 1.0;

    /// <summary>
    /// Door release button only clickable when brake applied, 0 km/h, and not during blinking.
    /// </summary>
    public bool IsDoorReleaseButtonEnabled => IsParkingBrakeEnabled && SpeedKmh == 0 && !IsDoorReleaseBlinking;

    /// <summary>
    /// Speed may only be increased when brake released and doors closed (door release locked).
    /// </summary>
    private bool CanIncreaseSpeed => !IsParkingBrakeEnabled && IsDoorReleaseLocked;

    // === Function button symbols (F0–F31) – per-locomotive SVG asset filenames, from Domain.Locomotive.FunctionSymbols ===

    /// <summary>
    /// Default SVG asset filenames (relative to MOBAflow/Assets) for F0–F31 when no locomotive
    /// in project or no customization saved. Empty string = no default symbol for this function.
    /// </summary>
    private static readonly string[] DefaultFunctionAssets =
    {
        "scheinwerfer.svg",          // F0  – default headlight
        "f1__fahrgeräusch.svg",      // F1  – default sound
        "", "", "", "", "", "",
        "", "", "", "", "", "", "", "",
        "", "", "", "", "", "", "", "",
        "", "", "", "", "", "", "", ""
    };

    // Per-button symbols are exposed via Functions[i].IconAsset (see NotifyAllFunctionGlyphsChanged).

    /// <summary>
    /// Status message for UI feedback.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Indicates if the active MOBA runtime is connected to the Z21.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetSpeedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleFunctionCommand))]
    [NotifyCanExecuteChangedFor(nameof(EmergencyStopCommand))]
    [NotifyPropertyChangedFor(nameof(IsSpeedControlEnabled))]
    private bool _isConnected;

    public bool IsSpeedControlEnabled => IsConnected;

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
    [NotifyPropertyChangedFor(nameof(PreviousStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(PreviousStationShowsExitDirection))]
    [NotifyPropertyChangedFor(nameof(CurrentStationName))]
    [NotifyPropertyChangedFor(nameof(CurrentStationArrival))]
    [NotifyPropertyChangedFor(nameof(CurrentStationDeparture))]
    [NotifyPropertyChangedFor(nameof(CurrentStationTrack))]
    [NotifyPropertyChangedFor(nameof(CurrentStationHasValue))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(CurrentStationShowsExitDirection))]
    [NotifyPropertyChangedFor(nameof(NextStationName))]
    [NotifyPropertyChangedFor(nameof(NextStationArrival))]
    [NotifyPropertyChangedFor(nameof(NextStationDeparture))]
    [NotifyPropertyChangedFor(nameof(NextStationTrack))]
    [NotifyPropertyChangedFor(nameof(NextStationHasValue))]
    [NotifyPropertyChangedFor(nameof(NextStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(NextStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(NextStationShowsExitDirection))]
    private Journey? _currentJourney;

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
    [NotifyPropertyChangedFor(nameof(PreviousStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(PreviousStationShowsExitDirection))]
    [NotifyPropertyChangedFor(nameof(CurrentStationName))]
    [NotifyPropertyChangedFor(nameof(CurrentStationArrival))]
    [NotifyPropertyChangedFor(nameof(CurrentStationDeparture))]
    [NotifyPropertyChangedFor(nameof(CurrentStationTrack))]
    [NotifyPropertyChangedFor(nameof(CurrentStationHasValue))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(CurrentStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(CurrentStationShowsExitDirection))]
    [NotifyPropertyChangedFor(nameof(NextStationName))]
    [NotifyPropertyChangedFor(nameof(NextStationArrival))]
    [NotifyPropertyChangedFor(nameof(NextStationDeparture))]
    [NotifyPropertyChangedFor(nameof(NextStationTrack))]
    [NotifyPropertyChangedFor(nameof(NextStationHasValue))]
    [NotifyPropertyChangedFor(nameof(NextStationIsExitOnLeft))]
    [NotifyPropertyChangedFor(nameof(NextStationIsEvent))]
    [NotifyPropertyChangedFor(nameof(NextStationShowsExitDirection))]
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
    public string PreviousStationArrival => ResolveArrivalText(GetPreviousStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the previous station departure time.
    /// </summary>
    public string PreviousStationDeparture => ResolveDepartureText(GetPreviousStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the previous station track value.
    /// </summary>
    public string PreviousStationTrack => ResolveTimetableDetailText(GetPreviousStation());

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no previous station.
    /// </summary>
    public bool PreviousStationHasValue => GetPreviousStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the previous station exit direction icon.
    /// </summary>
    public bool PreviousStationIsExitOnLeft => GetPreviousStation()?.IsExitOnLeft ?? false;

    public bool PreviousStationIsEvent => GetPreviousStation()?.IsVirtual ?? false;

    public bool PreviousStationShowsExitDirection => GetPreviousStation() is { IsVirtual: false };

    /// <summary>
    /// Provides TimetableStopsControl with the current station name, using a placeholder when none.
    /// </summary>
    public string CurrentStationName => GetCurrentStation()?.Name ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the current station arrival time.
    /// </summary>
    public string CurrentStationArrival => ResolveArrivalText(GetCurrentStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the current station departure time.
    /// </summary>
    public string CurrentStationDeparture => ResolveDepartureText(GetCurrentStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the current station track value.
    /// </summary>
    public string CurrentStationTrack => ResolveTimetableDetailText(GetCurrentStation());

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no current station.
    /// </summary>
    public bool CurrentStationHasValue => GetCurrentStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the current station exit direction icon.
    /// </summary>
    public bool CurrentStationIsExitOnLeft => GetCurrentStation()?.IsExitOnLeft ?? false;

    public bool CurrentStationIsEvent => GetCurrentStation()?.IsVirtual ?? false;

    public bool CurrentStationShowsExitDirection => GetCurrentStation() is { IsVirtual: false };

    /// <summary>
    /// Provides TimetableStopsControl with the next station name, using a placeholder when none.
    /// </summary>
    public string NextStationName => GetNextStation()?.Name ?? StationPlaceholder;

    /// <summary>
    /// Used by TimetableStopsControl to display the next station arrival time.
    /// </summary>
    public string NextStationArrival => ResolveArrivalText(GetNextStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the next station departure time.
    /// </summary>
    public string NextStationDeparture => ResolveDepartureText(GetNextStation());

    /// <summary>
    /// Used by TimetableStopsControl to display the next station track value.
    /// </summary>
    public string NextStationTrack => ResolveTimetableDetailText(GetNextStation());

    /// <summary>
    /// Used by TimetableStopsControl to hide exit direction icons when there is no next station.
    /// </summary>
    public bool NextStationHasValue => GetNextStation() != null;

    /// <summary>
    /// Used by TimetableStopsControl to choose the next station exit direction icon.
    /// </summary>
    public bool NextStationIsExitOnLeft => GetNextStation()?.IsExitOnLeft ?? false;

    public bool NextStationIsEvent => GetNextStation()?.IsVirtual ?? false;

    public bool NextStationShowsExitDirection => GetNextStation() is { IsVirtual: false };

    private Station? GetPreviousStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
        {
            return null;
        }

        var currentIndex = Math.Clamp(CurrentStationIndex, 0, CurrentJourney.Stations.Count);
        return currentIndex <= 0
            ? null
            : CurrentJourney.Stations[currentIndex - 1];
    }

    private Station? GetCurrentStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
            return null;

        if (CurrentStationIndex < 0 || CurrentStationIndex >= CurrentJourney.Stations.Count)
        {
            return null;
        }

        return CurrentJourney.Stations[CurrentStationIndex];
    }

    private Station? GetNextStation()
    {
        if (CurrentJourney == null || CurrentJourney.Stations.Count == 0)
            return null;

        var nextIndex = CurrentStationIndex + 1;
        if (nextIndex < 0 || nextIndex >= CurrentJourney.Stations.Count)
        {
            return null;
        }

        return CurrentJourney.Stations[nextIndex];
    }

    private string ResolveArrivalText(Station? station)
    {
        if (station == null || station.IsVirtual)
        {
            return StationPlaceholder;
        }

        return station.Arrival?.ToString("HH:mm") ?? StationPlaceholder;
    }

    private string ResolveDepartureText(Station? station)
    {
        if (station == null || station.IsVirtual)
        {
            return StationPlaceholder;
        }

        return station.Departure?.ToString("HH:mm") ?? StationPlaceholder;
    }

    private string ResolveTimetableDetailText(Station? station)
    {
        if (station == null)
        {
            return StationPlaceholder;
        }

        if (station.IsVirtual)
        {
            return ResolveEventSignalText(station);
        }

        return ResolvePlatformText(station);
    }

    private string ResolveEventSignalText(Station station)
    {
        var workflow = station.WorkflowId.HasValue
            ? ResolveWorkflow(station.WorkflowId.Value)
            : null;
        var signalAction = workflow?.Actions
            .OrderBy(action => action.Number)
            .FirstOrDefault(action => action.Type == ActionType.SelectSignalAspect && action.SelectSignalAspect != null);

        return signalAction?.SelectSignalAspect != null
            ? $"Signal: {signalAction.SelectSignalAspect.SignalAspect}"
            : "Event";
    }

    private Workflow? ResolveWorkflow(Guid workflowId)
    {
        var selectedProjectWorkflow = _mainWindowViewModel?.SelectedProject?.Model.Workflows.FirstOrDefault(workflow => workflow.Id == workflowId);
        if (selectedProjectWorkflow != null)
        {
            return selectedProjectWorkflow;
        }

        return _mainWindowViewModel?.SolutionViewModel?.Projects
            .SelectMany(project => project.Model.Workflows)
            .FirstOrDefault(workflow => workflow.Id == workflowId);
    }

    private static string ResolvePlatformText(Station? station)
    {
        if (station == null)
        {
            return StationPlaceholder;
        }

        if (station.PlatformId.HasValue)
        {
            var platform = station.Platforms.FirstOrDefault(platform => platform.Id == station.PlatformId.Value);
            if (platform != null)
            {
                return platform.Number.ToString();
            }
        }

        return station.Platforms.FirstOrDefault()?.Number.ToString() ?? StationPlaceholder;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainControlViewModel"/> class that implements the digital throttle UI.
    /// </summary>
    /// <param name="mobaRuntime">In-process MOBA runtime (Z21, locomotive commands, snapshots).</param>
    /// <param name="settingsService">Service used to persist train control presets and options.</param>
    /// <param name="mainWindowViewModel">Optional main window ViewModel used to access the current project and journey.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="uiDispatcher">Optional UI dispatcher for updating UI-bound properties.</param>
    public TrainControlViewModel(
        IMobaRuntime mobaRuntime,
        ISettingsService settingsService,
        MainWindowViewModel? mainWindowViewModel = null,
        ILogger<TrainControlViewModel>? logger = null,
        IUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(settingsService);
        _mobaRuntime = mobaRuntime;
        _settingsService = settingsService;
        _mainWindowViewModel = mainWindowViewModel;
        _logger = logger;
        _uiDispatcher = uiDispatcher;

        // Load presets from settings
        LoadPresetsFromSettings();

        _mobaRuntime.SnapshotChanged += OnRuntimeSnapshotChanged;
        ApplyRuntimeSnapshot(_mobaRuntime.Current);

        // Subscribe to MainWindowViewModel.SelectedJourney changes
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.PropertyChanged += OnMainWindowViewModelPropertyChanged;

            // Initialize with current journey if available
            UpdateJourneyFromMainViewModel();
        }

        // Initialize function button symbols for the current locomotive.
        NotifyAllFunctionGlyphsChanged();
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
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            OnPropertyChanged(nameof(ProjectLocomotives));
            TryRestoreSelectedLocomotiveFromProject();
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
    /// Restores the combo box selection "Loco from project" when the saved locomotive
    /// appears in the current project list. Called when loading settings and when
    /// switching projects.
    /// </summary>
    private void TryRestoreSelectedLocomotiveFromProject()
    {
        var settings = _settingsService.GetSettings();
        var savedId = settings.TrainControl.SelectedLocomotiveFromProjectId;
        if (!savedId.HasValue || ProjectLocomotives.Count == 0)
            return;
        var match = ProjectLocomotives.FirstOrDefault(l => l.Model.Id == savedId.Value);
        if (match == null || SelectedLocomotiveFromProject != null)
            return;
        _isLoadingPreset = true;
        try
        {
            SelectedPresetIndex = -1;
            SelectedLocomotiveFromProject = match;
        }
        finally
        {
            _isLoadingPreset = false;
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

        // Restore combo box selection "Loco from project" if saved and present in project
        TryRestoreSelectedLocomotiveFromProject();

        // Apply current preset only when a preset (0–2) is selected; -1 = Combobox-Auswahl
        if (SelectedPresetIndex >= 0 && SelectedPresetIndex <= 2)
        {
            ApplyCurrentPreset();
        }
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
            settings.TrainControl.SelectedLocomotiveFromProjectId =
                (SelectedPresetIndex == -1 && SelectedLocomotiveFromProject?.Model != null)
                    ? SelectedLocomotiveFromProject.Model.Id
                    : null;

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
            _previousSpeed = 0;

            // Always start in forward direction
            IsForward = true;

            _logger?.LogInformation(
                "Applied preset: {Name} - DCC={DccAddress}, Speed={Speed} (always 0), SpeedKmh={SpeedKmh}, " +
                "MaxSpeedStep={MaxSpeedStep}, SpeedSteps={SpeedSteps}, SelectedVmax={Vmax}",
                preset.Name, preset.DccAddress, Speed, SpeedKmh, MaxSpeedStep, SpeedSteps, SelectedVmax);

            // Turn all function keys off on selection instead of restoring the saved bitmask.
            // Set synchronously so an incoming loco-info snapshot cannot re-enable functions.
            _suppressSnapshotFunctionState = true;
            for (int i = 0; i <= 31; i++)
            {
                SetFunctionState(i, false);
            }

            StatusMessage = $"Loaded: {preset.Name} (DCC {preset.DccAddress})";

            // Turn all function keys off on selection: send explicit OFF for all functions to the Z21.
            QueueBackgroundTask(TurnOffAllFunctionsAsync(), "Turn off all functions");
            OnPropertyChanged(nameof(CurrentPreset));
            NotifyAllFunctionGlyphsChanged();
        }
        finally
        {
            _isLoadingPreset = false;
        }
    }

    /// <summary>
    /// Returns the locomotive for the current preset (LocoAddress) from the selected project, if present.
    /// </summary>
    private Locomotive? GetCurrentLocomotive()
    {
        var project = _mainWindowViewModel?.SelectedProject?.Model;
        if (project?.Locomotives == null) return null;
        return project.Locomotives.FirstOrDefault(l => l.DigitalAddress.HasValue && l.DigitalAddress.Value == LocoAddress);
    }

    /// <summary>
    /// SVG asset filename for the function button with index 0–31. Uses Domain.Locomotive.FunctionSymbols when set
    /// (and pointing to an .svg asset), otherwise the default asset. Returns empty string when no symbol is configured.
    /// Legacy Unicode-codepoint values from earlier versions are ignored.
    /// </summary>
    private string GetFunctionGlyph(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
            return string.Empty;
        var loco = GetCurrentLocomotive();
        if (loco?.FunctionSymbols != null && functionIndex < loco.FunctionSymbols.Count)
        {
            var stored = loco.FunctionSymbols[functionIndex];
            if (IsValidAssetReference(stored))
                return stored;
        }
        return functionIndex < DefaultFunctionAssets.Length ? DefaultFunctionAssets[functionIndex] : string.Empty;
    }

    /// <summary>
    /// Returns true when the stored value is a non-empty SVG asset filename. Filters out legacy
    /// Segoe MDL2 codepoint strings from earlier versions (single-character entries without .svg suffix).
    /// </summary>
    private static bool IsValidAssetReference(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private string GetFunctionColor(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
            return SignalGrayHex;

        var loco = GetCurrentLocomotive();
        if (loco?.FunctionColors != null && functionIndex < loco.FunctionColors.Count)
        {
            var stored = loco.FunctionColors[functionIndex];
            if (IsValidHexColor(stored))
                return stored;
        }

        return functionIndex < FunctionBacklightColors.Length ? FunctionBacklightColors[functionIndex] : SignalGrayHex;
    }

    private static bool IsValidHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 7 || value[0] != '#')
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!char.IsAsciiHexDigit(value[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sets the SVG asset filename for the specified function (0–31) for the current locomotive (LocoAddress)
    /// and saves the Solution. Only effective when a locomotive with this digital address exists in the selected project.
    /// </summary>
    /// <param name="functionIndex">Funktionsindex 0–31 (F0–F31).</param>
    /// <param name="glyph">SVG asset filename relative to Assets/ (e.g. "scheinwerfer.svg").</param>
    /// <returns>True, wenn gespeichert; False, wenn keine passende Lok im Projekt.</returns>
    public bool SetFunctionSymbol(int functionIndex, string glyph)
    {
        if (functionIndex < 0 || functionIndex > 31 || string.IsNullOrEmpty(glyph))
            return false;
        var loco = GetCurrentLocomotive();
        if (loco == null)
            return false;
        loco.FunctionSymbols ??= new List<string>();
        while (loco.FunctionSymbols.Count <= functionIndex)
            loco.FunctionSymbols.Add(string.Empty);
        loco.FunctionSymbols[functionIndex] = glyph;
        NotifyAllFunctionAppearanceChanged();
        QueueBackgroundTask(_mainWindowViewModel?.SaveSolutionInternalAsync(), "Auto-save solution");
        return true;
    }

    public bool SetFunctionAppearance(int functionIndex, string? glyph, string? colorHex)
    {
        if (functionIndex < 0 || functionIndex > 31)
            return false;

        var loco = GetCurrentLocomotive();
        if (loco == null)
            return false;

        if (!string.IsNullOrEmpty(glyph))
        {
            loco.FunctionSymbols ??= new List<string>();
            while (loco.FunctionSymbols.Count <= functionIndex)
                loco.FunctionSymbols.Add(string.Empty);
            loco.FunctionSymbols[functionIndex] = glyph;
        }

        if (IsValidHexColor(colorHex))
        {
            loco.FunctionColors ??= new List<string>();
            while (loco.FunctionColors.Count <= functionIndex)
                loco.FunctionColors.Add(string.Empty);
            loco.FunctionColors[functionIndex] = colorHex!;
        }

        NotifyAllFunctionAppearanceChanged();
        QueueBackgroundTask(_mainWindowViewModel?.SaveSolutionInternalAsync(), "Auto-save solution");
        return true;
    }

    private void NotifyAllFunctionGlyphsChanged()
    {
        NotifyAllFunctionAppearanceChanged();
    }

    private void NotifyAllFunctionAppearanceChanged()
    {
        for (int i = 0; i < Functions.Count; i++)
        {
            Functions[i].IconAsset = GetFunctionGlyph(i);
            Functions[i].BacklightColorHex = GetFunctionColor(i);
        }
    }

    /// <summary>
    /// Saves current state to the selected preset.
    /// Speed and direction are NOT saved (always reset to safe defaults on load).
    /// </summary>
    private void SaveCurrentStateToPreset()
    {
        if (_isLoadingPreset || _isApplyingRuntimeLocomotiveState) return;

        var preset = CurrentPreset;
        preset.DccAddress = LocoAddress;

        // Speed and IsForward are NOT saved - always reset to 0/forward on load
        // This is a safety feature to prevent unexpected locomotive movement

        // Save function states to bitmask
        for (int i = 0; i <= 31; i++)
        {
            preset.SetFunction(i, GetFunctionState(i));
        }

        // Save to persistent storage (fire and forget)
        QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
    }

    /// <summary>
    /// Called when SelectedPresetIndex changes - save current and load new preset.
    /// Index -1 = Combobox-Auswahl, dann kein Preset anwenden.
    /// </summary>
    partial void OnSelectedPresetIndexChanged(int value)
    {
        if (value >= 0 && value <= 2)
            ApplyCurrentPreset();
    }

    private void OnRuntimeSnapshotChanged(object? sender, MobaRuntimeSnapshot snapshot)
    {
        _ = sender;

        if (_uiDispatcher != null)
        {
            _uiDispatcher.InvokeOnUi(() => ApplyRuntimeSnapshot(snapshot));
            return;
        }

        ApplyRuntimeSnapshot(snapshot);
    }

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var previousConnectionState = IsConnected;
        IsConnected = snapshot.IsConnected;
        if (previousConnectionState != snapshot.IsConnected)
        {
            OnZ21ConnectionChanged(snapshot.IsConnected);
        }

        ApplySystemStateFromRuntime(snapshot);

        if (snapshot.LocomotiveStates.TryGetValue(LocoAddress, out var locomotiveState))
        {
            ApplyLocomotiveState(locomotiveState);
        }
    }

    private void OnZ21ConnectionChanged(bool isConnected)
    {
        SetSpeedCommand.NotifyCanExecuteChanged();
        ToggleFunctionCommand.NotifyCanExecuteChanged();
        EmergencyStopCommand.NotifyCanExecuteChanged();
        StatusMessage = isConnected ? "Z21 Connected" : "Z21 Disconnected";
    }

    private void ApplyLocomotiveState(LocomotiveRuntimeSnapshot locomotiveState)
    {
        if (locomotiveState.Address != LocoAddress)
        {
            return;
        }

        _isApplyingRuntimeLocomotiveState = true;
        _skipSpeedChangeHandler = true;

        try
        {
            Speed = locomotiveState.Speed;
            _previousSpeed = locomotiveState.Speed;
            IsForward = locomotiveState.IsForward;

            for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
            {
                var isOn = !_suppressSnapshotFunctionState
                    && (locomotiveState.Functions & (1u << functionIndex)) != 0;
                SetFunctionState(functionIndex, isOn);
            }

            StatusMessage = $"Loco {locomotiveState.Address}: {locomotiveState.Speed} {(locomotiveState.IsForward ? "FWD" : "REV")}";
        }
        finally
        {
            _skipSpeedChangeHandler = false;
            _isApplyingRuntimeLocomotiveState = false;
        }
    }

    /// <summary>
    /// Called when LocoAddress changes - request current state from runtime and save to preset.
    /// </summary>
    partial void OnLocoAddressChanged(int value)
    {
        // Save to current preset
        if (!_isLoadingPreset)
        {
            CurrentPreset.DccAddress = value;
            QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
        }

        if (value >= 1 && value <= 9999 && IsConnected)
        {
            QueueBackgroundTask(RequestLocoInfoAsync(), "Request locomotive info");
        }

        NotifyAllFunctionGlyphsChanged();
        ToggleFunctionCommand.NotifyCanExecuteChanged();
    }

    private async Task RequestLocoInfoAsync()
    {
        try
        {
            await _mobaRuntime.RequestLocomotiveInfoAsync(LocoAddress);
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
    /// Speed increase prevented when brake applied or door release active (doors open).
    /// </summary>
    partial void OnSpeedChanged(int value)
    {
        if (_skipSpeedChangeHandler || _isLoadingPreset || _isApplyingRuntimeLocomotiveState) return;

        if (!IsConnected && value > _previousSpeed)
        {
            _skipSpeedChangeHandler = true;
            Speed = _previousSpeed;
            _skipSpeedChangeHandler = false;
            return;
        }

        if (!CanIncreaseSpeed && value > _previousSpeed)
        {
            _skipSpeedChangeHandler = true;
            Speed = _previousSpeed;
            _skipSpeedChangeHandler = false;
            return;
        }

        _previousSpeed = value;

        // Save to current preset
        CurrentPreset.Speed = value;
        QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");

        if (CanExecuteLocoCommand() && LocoAddress >= 1)
        {
            QueueBackgroundTask(SendDriveCommandAsync(), "Send drive command");
        }
    }

    /// <summary>
    /// Called when IsForward changes - ramp down to 0, then ramp up in new direction.
    /// This prevents derailment from sudden direction changes at speed.
    /// </summary>
    partial void OnIsForwardChanged(bool value)
    {
        if (_isLoadingPreset || _isApplyingRuntimeLocomotiveState) return;

        // Save to current preset
        CurrentPreset.IsForward = value;
        QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");

        if (IsConnected && LocoAddress >= 1)
        {
            QueueBackgroundTask(HandleDirectionChangeAsync(value), "Handle direction change");
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
            await _mobaRuntime.SetLocomotiveDriveAsync(LocoAddress, 0, newDirection);

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
            await _mobaRuntime.SetLocomotiveDriveAsync(LocoAddress, currentSpeed, direction);

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
    /// Validates whether locomotive commands can be executed.
    /// Requires active Z21 connection and valid DCC address (1-9999).
    /// </summary>
    private bool CanExecuteLocoCommand() => IsConnected && LocoAddress >= 1 && LocoAddress <= 9999;

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
            await _mobaRuntime.SetLocomotiveDriveAsync(LocoAddress, Speed, IsForward);
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
    /// Toggles the function with the given index (0–31). Parameterized command bound by every
    /// function button via its <see cref="FunctionButtonViewModel.Index"/>.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private Task ToggleFunction(int index) => ToggleFunctionAsync(index);

    /// <summary>
    /// Generic function toggle implementation.
    /// Public method to allow direct UI event handling (bypasses CanExecute).
    /// </summary>
    public async Task ToggleFunctionAsync(int functionNumber)
    {
        try
        {
            // User takes manual control: resume applying decoder function bits from snapshots.
            _suppressSnapshotFunctionState = false;

            var newState = !GetFunctionState(functionNumber);
            SetFunctionState(functionNumber, newState);

            // Save function state to current preset
            if (!_isLoadingPreset)
            {
                CurrentPreset.SetFunction(functionNumber, newState);
                QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
            }

            await _mobaRuntime.SetLocomotiveFunctionAsync(LocoAddress, functionNumber, newState);
            StatusMessage = $"F{functionNumber}: {(newState ? "ON" : "OFF")}";
            _logger?.LogDebug("F{Function} toggled: {State}", functionNumber, newState);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle F{Function}", functionNumber);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Turns off all function buttons F0-F31 in the UI and - when connected - sends an explicit
    /// OFF command (TT=00, never a toggle) for the current locomotive to the Z21. Snapshot function
    /// bits are suppressed until the user toggles a function again. When a preset is selected, the
    /// new (all-off) state is also persisted to the preset. Used when a locomotive is selected.
    /// </summary>
    public async Task TurnOffAllFunctionsAsync()
    {
        for (int i = 0; i < Functions.Count; i++)
            Functions[i].IsOn = false;
        // Ignore decoder-reported function bits from upcoming snapshots until the user toggles again.
        _suppressSnapshotFunctionState = true;

        // Persist the new (all-off) function state per locomotive when a preset is selected.
        if (SelectedPresetIndex is >= 0 and <= 2)
        {
            for (int i = 0; i <= 31; i++)
                CurrentPreset.SetFunction(i, false);
            QueueBackgroundTask(SavePresetsToSettingsAsync(), "Save train control presets");
        }

        if (!IsConnected || LocoAddress < 1)
            return;

        try
        {
            await _mobaRuntime.SetAllLocomotiveFunctionsOffAsync(LocoAddress);
            StatusMessage = $"Loco {LocoAddress}: all functions OFF";
            _logger?.LogDebug("All functions turned off for loco {Address}", LocoAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to turn off all functions for loco {Address}", LocoAddress);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool GetFunctionState(int functionNumber) =>
        functionNumber >= 0 && functionNumber < Functions.Count && Functions[functionNumber].IsOn;

    private void SetFunctionState(int functionNumber, bool state)
    {
        if (functionNumber >= 0 && functionNumber < Functions.Count)
            Functions[functionNumber].IsOn = state;
    }

    /// <summary>
    /// Apply brake (speed to 0, then brake on) or release (only when doors closed).
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsBrakeButtonEnabled))]
    private async Task ToggleBrakeAsync()
    {
        if (!IsBrakeButtonEnabled) return;

        if (IsParkingBrakeEnabled)
        {
            IsParkingBrakeEnabled = false;
            _logger?.LogDebug("Bremse gelöst");
        }
        else
        {
            _skipSpeedChangeHandler = true;
            Speed = 0;
            _skipSpeedChangeHandler = false;
            await SendDriveCommandAsync();
            IsParkingBrakeEnabled = true;
            _logger?.LogDebug("Bremse angelegt, Geschwindigkeit 0");
        }
    }

    /// <summary>
    /// Toggles door release (release/lock). Closing uses 5 seconds plus a random additional blocking delay.
    /// Only executable when brake applied (button red) and 0 km/h.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsDoorReleaseButtonEnabled))]
    private async Task ToggleDoorReleaseAsync()
    {
        if (!IsDoorReleaseButtonEnabled || IsDoorReleaseBlinking) return;

        IsDoorReleaseLockedNext = !IsDoorReleaseLocked;
        var isClosingDoor = IsDoorReleaseLockedNext;
        var blockingDelaySeconds = isClosingDoor ? Random.Shared.Next(0, 61) : 0;
        var transitionDelay = TimeSpan.FromSeconds(5 + blockingDelaySeconds);

        if (!isClosingDoor)
        {
            SetDoorBlocked(false);
        }

        IsDoorReleaseBlinking = true;
        DoorReleaseBlinkOpacity = 1.0;
        _doorReleaseBlinkCts?.Cancel();
        _doorReleaseBlinkCts = new CancellationTokenSource();
        var token = _doorReleaseBlinkCts.Token;
        var blinkHigh = true;

        async Task RunBlinkLoopAsync()
        {
            const int intervalMs = 400;
            var totalMs = (int)transitionDelay.TotalMilliseconds;
            for (var elapsed = 0; elapsed < totalMs && !token.IsCancellationRequested; elapsed += intervalMs)
            {
                try
                {
                    await Task.Delay(intervalMs, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                blinkHigh = !blinkHigh;
                var opacity = blinkHigh ? 1.0 : 0.35;
                if (_uiDispatcher != null)
                    _uiDispatcher.InvokeOnUi(() => DoorReleaseBlinkOpacity = opacity);
                else
                    DoorReleaseBlinkOpacity = opacity;
            }
        }

        try
        {
            if (isClosingDoor)
            {
                await Task.WhenAll(
                    RunDoorBlockingTimerAsync(blockingDelaySeconds, token),
                    RunBlinkLoopAsync());
            }
            else
            {
                await Task.WhenAll(
                    Task.Delay(TimeSpan.FromSeconds(5), token),
                    RunBlinkLoopAsync());
            }
        }
        catch (OperationCanceledException)
        {
            if (_uiDispatcher != null)
                _uiDispatcher.InvokeOnUi(() => { DoorReleaseBlinkOpacity = 1.0; IsDoorReleaseBlinking = false; IsDoorBlocked = false; });
            else
            {
                DoorReleaseBlinkOpacity = 1.0;
                IsDoorReleaseBlinking = false;
                SetDoorBlocked(false);
            }
            return;
        }

        if (_uiDispatcher != null)
        {
            _uiDispatcher.InvokeOnUi(() =>
            {
                IsDoorReleaseLocked = IsDoorReleaseLockedNext;
                IsDoorReleaseBlinking = false;
                DoorReleaseBlinkOpacity = 1.0;
                IsDoorBlocked = false;
            });
        }
        else
        {
            IsDoorReleaseLocked = IsDoorReleaseLockedNext;
            IsDoorReleaseBlinking = false;
            DoorReleaseBlinkOpacity = 1.0;
            SetDoorBlocked(false);
        }

        _logger?.LogDebug("Türfreigabe {State}", IsDoorReleaseLocked ? "gesperrt" : "freigegeben");
    }

    private async Task RunDoorBlockingTimerAsync(int blockingDelaySeconds, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        SetDoorBlocked(true);
        await Task.Delay(TimeSpan.FromSeconds(blockingDelaySeconds), cancellationToken);
    }

    private void SetDoorBlocked(bool value)
    {
        if (_uiDispatcher != null)
            _uiDispatcher.InvokeOnUi(() => IsDoorBlocked = value);
        else
            IsDoorBlocked = value;
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
            await _mobaRuntime.SetLocomotiveDriveAsync(LocoAddress, 0, IsForward);
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
            await _mobaRuntime.SetLocomotiveDriveAsync(LocoAddress, 0, IsForward);
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

        var maxSpeed = CanIncreaseSpeed ? 126 : Speed;
        Speed = Math.Clamp(preset, 0, maxSpeed);
        _logger?.LogDebug("Speed preset set to {Preset}", preset);
    }

    /// <summary>
    /// Selects locomotive preset 1.
    /// </summary>
    [RelayCommand]
    private void SelectPreset1()
    {
        if (SelectedPresetIndex >= 0 && SelectedPresetIndex <= 2)
            SaveCurrentStateToPreset();
        SelectedLocomotiveFromProject = null;
        if (SelectedPresetIndex != 0)
            SelectedPresetIndex = 0;
    }

    /// <summary>
    /// Selects locomotive preset 2.
    /// </summary>
    [RelayCommand]
    private void SelectPreset2()
    {
        if (SelectedPresetIndex >= 0 && SelectedPresetIndex <= 2)
            SaveCurrentStateToPreset();
        SelectedLocomotiveFromProject = null;
        if (SelectedPresetIndex != 1)
            SelectedPresetIndex = 1;
    }

    /// <summary>
    /// Selects locomotive preset 3.
    /// </summary>
    [RelayCommand]
    private void SelectPreset3()
    {
        if (SelectedPresetIndex >= 0 && SelectedPresetIndex <= 2)
            SaveCurrentStateToPreset();
        SelectedLocomotiveFromProject = null;
        if (SelectedPresetIndex != 2)
            SelectedPresetIndex = 2;
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

    // === Runtime Projection ===

    private void ApplySystemStateFromRuntime(MobaRuntimeSnapshot snapshot)
    {
        MainTrackCurrent = snapshot.MainCurrent;
        ProgTrackCurrent = snapshot.ProgCurrent;
        FilteredMainCurrent = snapshot.FilteredMainCurrent;
        SupplyVoltage = snapshot.SupplyVoltage;
        Temperature = snapshot.Temperature;

        if (snapshot.MainCurrent > PeakMainCurrent)
            PeakMainCurrent = snapshot.MainCurrent;
        if (snapshot.Temperature > PeakTemperature)
            PeakTemperature = snapshot.Temperature;

        _logger?.LogDebug(
            "SystemState updated: MainCurrent={MainCurrent}mA (Filtered={FilteredCurrent}mA, Peak={PeakCurrent}mA), " +
            "ProgCurrent={ProgCurrent}mA, SupplyVoltage={SupplyVoltage}mV, Temperature={Temperature}°C",
            MainTrackCurrent, FilteredMainCurrent, PeakMainCurrent, ProgTrackCurrent, SupplyVoltage, Temperature);
    }

    private void QueueBackgroundTask(Task? task, string operationName)
    {
        if (task == null)
        {
            return;
        }

        task.ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogWarning(t.Exception, "{OperationName} failed", operationName);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
