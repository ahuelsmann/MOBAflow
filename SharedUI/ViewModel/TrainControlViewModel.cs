// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Configuration;

using Common.Runtime;
using Common.Display;
using Common.Events;
using Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>
/// ViewModel for TrainControlPage - provides locomotive drive control interface.
/// Implements a "digital throttle" similar to the Roco Z21 app hand controller.
/// 
/// Features:
/// - Project locomotive selection with persistent last choice
/// - Speed control (0-126 for 128 speed steps)
/// - Direction toggle (Forward/Backward)
/// - Function keys F0-F31
/// - Emergency stop
/// - Speed ramping for smooth direction changes
/// 
/// Cross-platform: Used by WinUI and MAUI.
/// </summary>
public sealed partial class TrainControlViewModel : ObservableObject, IDisposable
{
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrainControlViewModel>? _logger;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly IEventBus _eventBus;
    private readonly List<Guid> _eventBusSubscriptions = [];

    private bool _isRestoringLocomotive;
    private bool _isApplyingRuntimeLocomotiveState;
    private bool _disposed;
    private bool _updatesPaused;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _pendingLocomotiveFleet;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _lastAppliedLocomotiveFleet;
    private readonly bool _useRemoteRuntimeSnapshots;
    private readonly bool _hybridRuntimeSnapshots;
    private readonly TrainControlHost _trainControlHost;
    private readonly IMobileRuntimeCoordinator? _mobileRuntimeCoordinator;
    private readonly IFunctionAppearancePicker? _functionAppearancePicker;
    private CancellationTokenSource? _sendDriveCommandDebounceCts;

    private const int SendDriveCommandDebounceMs = 75;

    // Snapshot function bits are ignored while an explicit all-off reset is in flight, or briefly
    // after a local function command so decoder/snapshot races do not fight the UI.
    private bool _suppressSnapshotFunctionState;
    private CancellationTokenSource? _allFunctionsOffCts;
    private int _functionControlVersion;
    private int _previousLocoAddressForFunctionCache;
    private readonly Dictionary<int, uint> _locomotiveFunctionStateCache = [];
    private readonly Dictionary<int, Dictionary<int, DateTimeOffset>> _lastLocalFunctionCommandAt = [];
    private readonly Dictionary<int, DateTimeOffset> _lastLocalDriveCommandAt = [];
    private MobaRuntimeSnapshot? _lastRemoteRuntimeSnapshot;
    private static readonly TimeSpan FunctionCommandGracePeriod = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DriveCommandGracePeriod = TimeSpan.FromSeconds(3);
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

    // === Project Locomotives ===

    /// <summary>
    /// Locomotives of the current project for combo box selection.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LocomotiveViewModel> _projectLocomotives = [];

    /// <summary>
    /// Gets whether the synced project exposes locomotives for selection.
    /// </summary>
    public bool HasProjectLocomotives => ProjectLocomotives.Count > 0;

    /// <summary>
    /// Height hint for the non-scrolling locomotive picker list on MAUI.
    /// </summary>
    public double ProjectLocomotiveListHeight => ProjectLocomotives.Count * 76d;

    /// <summary>
    /// Display title for the active locomotive (project name when selected, otherwise address).
    /// </summary>
    public string LocomotiveTitle =>
        SelectedLocomotiveFromProject?.Name ?? $"Loco {LocoAddress}";

    /// <summary>
    /// Locomotive selected from project combo box.
    /// </summary>
    [ObservableProperty]
    private LocomotiveViewModel? _selectedLocomotiveFromProject;

    partial void OnSelectedLocomotiveFromProjectChanged(LocomotiveViewModel? value)
    {
        SyncLocomotivePickerSelection();

        if (value != null)
        {
            CancelAllFunctionsOffOperation();
            var addr = value.Model.DigitalAddress;
            LocoAddress = addr.HasValue ? (int)addr.Value : 0;
            Speed = 0;
            IsForward = true;
            StatusMessage = addr.HasValue ? $"DCC {addr.Value}" : string.Empty;
            OnPropertyChanged(nameof(LocomotiveTitle));
        }

        NotifyAllFunctionAppearanceChanged();

        if (!_isRestoringLocomotive)
        {
            QueueBackgroundTask(SaveTrainControlSettingsAsync(), "Save train control settings");
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
    private string _selectedLocoSeries = string.Empty;

    partial void OnSelectedLocoSeriesChanged(string value)
    {
        _ = value;
        QueueBackgroundTask(SaveLocoSeriesSettingsAsync(), "Save locomotive series settings");
    }

    /// <summary>
    /// Maximum speed (Vmax) of the selected locomotive series in km/h.
    /// Default: 200 km/h. Persisted in settings. Shown as Vmax marker on the gauge only.
    /// </summary>
    [ObservableProperty]
    private int _selectedVmax = 200;

    partial void OnSelectedVmaxChanged(int value)
    {
        _ = value;
        QueueBackgroundTask(SaveLocoSeriesSettingsAsync(), "Save locomotive series settings");
    }

    /// <summary>
    /// Full-scale maximum on the speed gauge and slider (km/h), aligned with tachometer <c>GaugeMaxKmh</c>.
    /// </summary>
    public int SpeedGaugeMaxKmh => TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh;

    /// <summary>
    /// Calculated speed in km/h based on current DCC step and gauge full scale (<see cref="SpeedGaugeMaxKmh"/>).
    /// Locomotive <see cref="SelectedVmax"/> is shown on the gauge as a separate Vmax marker only.
    /// Example (128 steps, gauge 400 km/h):
    /// - Step 126 (max): (126/126) * 400 = 400 km/h
    /// - Step 63 (50%): (63/126) * 400 = 200 km/h
    /// </summary>
    public int SpeedKmh
    {
        get
        {
            if (MaxSpeedStep == 0)
            {
                _logger?.LogWarning("MaxSpeedStep is 0! Returning 0 km/h. SpeedSteps={SpeedSteps}", SpeedSteps);
                return 0;
            }

            var result = TrainControlDccSpeed.SpeedStepToKmh(Speed, MaxSpeedStep, SpeedGaugeMaxKmh);

            if (result > SpeedGaugeMaxKmh + 1)
            {
                _logger?.LogWarning(
                    "SpeedKmh calculation resulted in unrealistic value: {Result} km/h. " +
                    "Speed={Speed}, MaxSpeedStep={MaxSpeedStep}, SpeedGaugeMaxKmh={GaugeMax}, SpeedSteps={SpeedSteps}",
                    result, Speed, MaxSpeedStep, SpeedGaugeMaxKmh, SpeedSteps);
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

    // === Function button symbols (F0–F31) – per-locomotive PNG asset filenames, from Domain.Locomotive.FunctionSymbols ===

    /// <summary>
    /// Default PNG asset filenames (relative to MOBAflow/Assets/FunctionSymbols) for F0–F31 when no locomotive
    /// in project or no customization saved. Empty string = no default symbol for this function.
    /// </summary>
    private static readonly string[] DefaultFunctionAssets =
    {
        "headlight.png",               // F0  – default headlight
        "f1__driving_sound.png",     // F1  – default sound
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
    [NotifyCanExecuteChangedFor(nameof(TurnOffAllFunctionsCommand))]
    [NotifyCanExecuteChangedFor(nameof(EmergencyStopCommand))]
    [NotifyPropertyChangedFor(nameof(IsSpeedControlEnabled))]
    private bool _isConnected;

    public bool IsSpeedControlEnabled => CanExecuteLocomotiveControl;

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

    private readonly IProjectContext? _projectContext;
    private JourneyViewModel? _observedJourneyViewModel;
    private ProjectViewModel? _observedProjectForLocomotives;

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
        if (station == null)
        {
            return StationPlaceholder;
        }

        return station.Arrival?.ToString("HH:mm") ?? StationPlaceholder;
    }

    private string ResolveDepartureText(Station? station)
    {
        if (station == null)
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

        return ResolvePlatformText(station);
    }

    private Workflow? ResolveWorkflow(Guid workflowId)
    {
        var selectedProjectWorkflow = _projectContext?.SelectedProject?.Model.Workflows.FirstOrDefault(workflow => workflow.Id == workflowId);
        if (selectedProjectWorkflow != null)
        {
            return selectedProjectWorkflow;
        }

        return _projectContext?.SolutionViewModel?.Projects
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
    /// <param name="settingsService">Service used to persist train control options.</param>
    /// <param name="projectContext">Optional project context used to access the current project and journey.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="uiDispatcher">Optional UI dispatcher for updating UI-bound properties.</param>
    /// <param name="eventBus">Event bus for runtime snapshot updates.</param>
    /// <param name="runtimeCommandGateway">Optional gateway for locomotive commands (defaults to local runtime).</param>
    /// <param name="useRemoteRuntimeSnapshots">When true, locomotive state is driven by MOBAflow snapshots via MOBApi.</param>
    /// <param name="options">Optional host-specific options; overrides <paramref name="useRemoteRuntimeSnapshots"/> when set.</param>
    /// <param name="mobileRuntimeCoordinator">Optional MOBAsmart coordinator for hybrid local/remote snapshot routing.</param>
    /// <param name="functionAppearancePicker">Optional WinUI picker for locomotive function appearance editing.</param>
    public TrainControlViewModel(
        IMobaRuntime mobaRuntime,
        ISettingsService settingsService,
        IProjectContext? projectContext = null,
        ILogger<TrainControlViewModel>? logger = null,
        IUiDispatcher? uiDispatcher = null,
        IEventBus eventBus = null!,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        bool useRemoteRuntimeSnapshots = false,
        TrainControlViewModelOptions? options = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IFunctionAppearancePicker? functionAppearancePicker = null)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(eventBus);
        _mobaRuntime = mobaRuntime;
        _functionAppearancePicker = functionAppearancePicker;
        if (mobileRuntimeCoordinator != null)
        {
            _mobileRuntimeCoordinator = mobileRuntimeCoordinator;
            _runtimeCommandGateway = mobileRuntimeCoordinator;
        }
        else
        {
            _mobileRuntimeCoordinator = runtimeCommandGateway as IMobileRuntimeCoordinator;
            _runtimeCommandGateway = runtimeCommandGateway ?? new LocalRuntimeCommandGateway(mobaRuntime);
        }
        _settingsService = settingsService;
        _projectContext = projectContext;
        _logger = logger;
        _uiDispatcher = uiDispatcher;
        _eventBus = eventBus;
        _useRemoteRuntimeSnapshots = options?.UseRemoteRuntimeSnapshots ?? useRemoteRuntimeSnapshots;
        _hybridRuntimeSnapshots = options?.HybridRuntimeSnapshots ?? false;
        _trainControlHost = options?.Host ?? TrainControlHost.WinUi;

        LoadTrainControlSettings();

        if (_hybridRuntimeSnapshots)
        {
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RemoteRuntimeSnapshotChangedEvent>(OnRemoteRuntimeSnapshotChanged));
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeCommandAvailabilityChangedEvent>(OnRuntimeCommandAvailabilityChanged));
            ApplyRuntimeSnapshot(_mobaRuntime.Current);
        }
        else if (_useRemoteRuntimeSnapshots)
        {
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RemoteRuntimeSnapshotChangedEvent>(OnRemoteRuntimeSnapshotChanged));
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeCommandAvailabilityChangedEvent>(OnRuntimeCommandAvailabilityChanged));
        }
        else
        {
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
            ApplyRuntimeSnapshot(_mobaRuntime.Current);
        }

        _eventBusSubscriptions.Add(_eventBus.Subscribe<SolutionSyncedEvent>(_ => OnSolutionSynced()));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<LocomotiveFleetUpdatedEvent>(OnLocomotiveFleetUpdated));

        if (_projectContext != null)
        {
            _projectContext.PropertyChanged += OnProjectContextPropertyChanged;
            AttachObservedProjectLocomotives();
            UpdateJourneyFromProjectContext();
            RefreshLocomotiveList();
        }

        if (_mobileRuntimeCoordinator != null)
        {
            RefreshLocomotiveCommandCanExecute();
        }
    }

    private void OnSolutionSynced()
    {
        RefreshLocomotiveList();
    }

    private void OnLocomotiveFleetUpdated(LocomotiveFleetUpdatedEvent e)
    {
        if (_updatesPaused)
        {
            if (e.Fleet.Count > 0)
            {
                _pendingLocomotiveFleet = e.Fleet;
            }

            return;
        }

        RefreshLocomotiveList(e.Fleet);
    }

    /// <summary>
    /// Rebuilds the MAUI-bound locomotive list from runtime fleet snapshots or the synced project context.
    /// </summary>
    public void RefreshLocomotiveList(IReadOnlyList<LocomotiveFleetSnapshot>? fleetOverride = null)
    {
        void ApplyRefresh()
        {
            if (fleetOverride is { Count: 0 } && ProjectLocomotives.Count > 0)
            {
                return;
            }

            IReadOnlyList<LocomotiveViewModel> sourceItems;
            if (fleetOverride is { Count: > 0 })
            {
                sourceItems = fleetOverride
                    .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(CreateLocomotiveViewModelFromFleetSnapshot)
                    .ToList();
            }
            else if (_mobaRuntime.Current.LocomotiveFleet.Count > 0)
            {
                sourceItems = _mobaRuntime.Current.LocomotiveFleet
                    .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(CreateLocomotiveViewModelFromFleetSnapshot)
                    .ToList();
            }
            else
            {
                sourceItems = _projectContext?.SelectedProject?.Locomotives
                    ?? (IReadOnlyList<LocomotiveViewModel>)[];
            }

            if (fleetOverride is { Count: > 0 })
            {
                var orderedFleet = fleetOverride
                    .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                if (_lastAppliedLocomotiveFleet is not null
                    && LocomotiveFleetSnapshotComparer.OrderedContentEquals(orderedFleet, _lastAppliedLocomotiveFleet))
                {
                    return;
                }

                ApplyFleetItemsInPlace(orderedFleet);
                _lastAppliedLocomotiveFleet = orderedFleet;
                return;
            }

            ProjectLocomotives = sourceItems.Count == 0
                ? []
                : new ObservableCollection<LocomotiveViewModel>(sourceItems);
            _lastAppliedLocomotiveFleet = null;
            OnPropertyChanged(nameof(HasProjectLocomotives));
            OnPropertyChanged(nameof(ProjectLocomotiveListHeight));
            OnPropertyChanged(nameof(LocomotiveTitle));
            TryRestoreSelectedLocomotiveFromProject();
            if (GetCurrentLocomotive() != null)
            {
                NotifyAllFunctionAppearanceChanged();
            }
        }

        if (_uiDispatcher != null)
        {
            _uiDispatcher.InvokeOnUi(ApplyRefresh);
        }
        else
        {
            ApplyRefresh();
        }
    }

    private void ApplyFleetItemsInPlace(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        var ordered = fleet
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var existingById = ProjectLocomotives.ToDictionary(item => item.Model.Id);
        var snapshotById = ordered.ToDictionary(item => item.LocomotiveId);

        for (var index = ProjectLocomotives.Count - 1; index >= 0; index--)
        {
            var existing = ProjectLocomotives[index];
            if (snapshotById.ContainsKey(existing.Model.Id))
            {
                continue;
            }

            ProjectLocomotives.RemoveAt(index);
        }

        var collectionChanged = ProjectLocomotives.Count != ordered.Count;
        var metadataChanged = false;

        for (var index = 0; index < ordered.Count; index++)
        {
            var snapshot = ordered[index];
            if (existingById.TryGetValue(snapshot.LocomotiveId, out var existing))
            {
                if (!existing.MatchesFleetSnapshot(snapshot))
                {
                    existing.ApplyFleetSnapshot(snapshot);
                    metadataChanged = true;
                }

                if (!ReferenceEquals(ProjectLocomotives[index], existing))
                {
                    collectionChanged = true;
                }

                continue;
            }

            ProjectLocomotives.Insert(index, CreateLocomotiveViewModelFromFleetSnapshot(snapshot));
            collectionChanged = true;
            metadataChanged = true;
        }

        if (collectionChanged)
        {
            OnPropertyChanged(nameof(HasProjectLocomotives));
            OnPropertyChanged(nameof(ProjectLocomotiveListHeight));
            OnPropertyChanged(nameof(LocomotiveTitle));
            TryRestoreSelectedLocomotiveFromProject();
            SyncLocomotivePickerSelection();
        }

        if (metadataChanged && GetCurrentLocomotive() != null)
        {
            NotifyAllFunctionAppearanceChanged();
        }
    }

    partial void OnProjectLocomotivesChanged(ObservableCollection<LocomotiveViewModel> value)
    {
        OnPropertyChanged(nameof(HasProjectLocomotives));
        OnPropertyChanged(nameof(ProjectLocomotiveListHeight));
        SyncLocomotivePickerSelection();
    }

    private void SyncLocomotivePickerSelection()
    {
        var selectedId = SelectedLocomotiveFromProject?.Model.Id;
        foreach (var locomotive in ProjectLocomotives)
        {
            locomotive.IsPickerSelected = selectedId.HasValue && locomotive.Model.Id == selectedId.Value;
        }
    }

    /// <summary>
    /// Refreshes remote photo bindings for the MOBAsmart locomotive list.
    /// </summary>
    public void RefreshLocomotivePhotoBindings()
    {
        foreach (var locomotive in ProjectLocomotives)
        {
            locomotive.InvalidatePhotoBinding();
        }
    }

    /// <summary>
    /// Suppresses runtime snapshot UI updates while the Control tab is not visible.
    /// </summary>
    public void PauseUpdates() => _updatesPaused = true;

    /// <summary>
    /// Re-enables snapshot updates and syncs the current runtime state once.
    /// </summary>
    public void ResumeUpdates()
    {
        _updatesPaused = false;
        if (_pendingLocomotiveFleet is { Count: > 0 })
        {
            var pendingFleet = _pendingLocomotiveFleet;
            _pendingLocomotiveFleet = null;
            RefreshLocomotiveList(pendingFleet);
        }

        if (_hybridRuntimeSnapshots || !_useRemoteRuntimeSnapshots)
        {
            ApplyRuntimeSnapshot(_mobaRuntime.Current);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var subscriptionId in _eventBusSubscriptions)
        {
            _eventBus.Unsubscribe(subscriptionId);
        }

        _eventBusSubscriptions.Clear();

        if (_projectContext != null)
        {
            _projectContext.PropertyChanged -= OnProjectContextPropertyChanged;
        }

        DetachObservedProjectLocomotives();
        DetachObservedJourney();
        CancelRamp();
        _doorReleaseBlinkCts?.Cancel();
        _doorReleaseBlinkCts?.Dispose();
        _doorReleaseBlinkCts = null;
        _sendDriveCommandDebounceCts?.Cancel();
        _sendDriveCommandDebounceCts?.Dispose();
        _sendDriveCommandDebounceCts = null;
        _allFunctionsOffCts?.Cancel();
        _allFunctionsOffCts?.Dispose();
        _allFunctionsOffCts = null;
    }

    /// <summary>
    /// Called when MainWindowViewModel properties change.
    /// Updates CurrentJourney when SelectedJourney changes.
    /// </summary>
    private void OnProjectContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.PropertyName == nameof(IProjectContext.SelectedJourney))
        {
            UpdateJourneyFromProjectContext();
        }
        if (e.PropertyName == nameof(IProjectContext.SelectedProject))
        {
            AttachObservedProjectLocomotives();
            RefreshLocomotiveList();
        }
    }

    private void AttachObservedProjectLocomotives()
    {
        DetachObservedProjectLocomotives();
        _observedProjectForLocomotives = _projectContext?.SelectedProject;
        if (_observedProjectForLocomotives != null)
        {
            _observedProjectForLocomotives.Locomotives.CollectionChanged += OnProjectLocomotivesCollectionChanged;
        }
    }

    private void DetachObservedProjectLocomotives()
    {
        if (_observedProjectForLocomotives == null)
        {
            return;
        }

        _observedProjectForLocomotives.Locomotives.CollectionChanged -= OnProjectLocomotivesCollectionChanged;
        _observedProjectForLocomotives = null;
    }

    private void OnProjectLocomotivesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshLocomotiveList();
    }

    /// <summary>
    /// Updates CurrentJourney and CurrentStationIndex from the active project context.
    /// </summary>
    private void UpdateJourneyFromProjectContext()
    {
        if (_projectContext?.SelectedJourney == null)
        {
            DetachObservedJourney();
            CurrentJourney = null;
            CurrentStationIndex = 0;
            return;
        }

        var journeyVm = _projectContext.SelectedJourney;
        if (!ReferenceEquals(_observedJourneyViewModel, journeyVm))
        {
            DetachObservedJourney();
            _observedJourneyViewModel = journeyVm;
            _observedJourneyViewModel.PropertyChanged += OnJourneyViewModelPropertyChanged;
        }

        CurrentJourney = journeyVm.Model;
        CurrentStationIndex = journeyVm.CurrentPos;
    }

    private void DetachObservedJourney()
    {
        if (_observedJourneyViewModel == null)
        {
            return;
        }

        _observedJourneyViewModel.PropertyChanged -= OnJourneyViewModelPropertyChanged;
        _observedJourneyViewModel = null;
    }

    /// <summary>
    /// Called when JourneyViewModel properties change.
    /// Updates CurrentStationIndex when CurrentPos changes.
    /// </summary>
    private void OnJourneyViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.PropertyName == nameof(JourneyViewModel.CurrentPos) && sender is JourneyViewModel journeyVm)
        {
            CurrentStationIndex = journeyVm.CurrentPos;
        }
    }

    /// <summary>
    /// Restores this host's locomotive picker selection when the saved locomotive
    /// appears in the current project list.
    /// </summary>
    private void TryRestoreSelectedLocomotiveFromProject()
    {
        if (ProjectLocomotives.Count == 0 || SelectedLocomotiveFromProject != null)
        {
            return;
        }

        var settings = _settingsService.GetSettings();
        var savedId = GetHostTrainControlSettings(settings).SelectedLocomotiveFromProjectId;
        var match = savedId.HasValue
            ? ProjectLocomotives.FirstOrDefault(l => l.Model.Id == savedId.Value)
            : null;

        var usedFallback = match == null;
        if (match == null)
        {
            match = ProjectLocomotives[0];
        }

        if (match == null)
        {
            return;
        }

        _isRestoringLocomotive = true;
        try
        {
            SelectedLocomotiveFromProject = match;
        }
        finally
        {
            _isRestoringLocomotive = false;
        }

        if (!savedId.HasValue || usedFallback)
        {
            PersistHostLocomotiveSelection(match);
        }
    }

    private TrainControlHostSettings GetHostTrainControlSettings(AppSettings settings) =>
        _trainControlHost == TrainControlHost.Maui
            ? settings.MauiTrainControlHost
            : settings.WinUiTrainControlHost;

    private void PersistHostLocomotiveSelection(LocomotiveViewModel locomotive)
    {
        var settings = _settingsService.GetSettings();
        GetHostTrainControlSettings(settings).SelectedLocomotiveFromProjectId = locomotive.Model.Id;
        QueueBackgroundTask(SaveTrainControlSettingsAsync(), "Save train control settings");
    }

    /// <summary>
    /// Loads train control settings from persistent storage.
    /// </summary>
    private void LoadTrainControlSettings()
    {
        _isRestoringLocomotive = true;
        try
        {
            var settings = _settingsService.GetSettings();
            var trainControl = settings.TrainControl;

            RampStepSize = trainControl.SpeedRampStepSize;
            RampIntervalMs = trainControl.SpeedRampIntervalMs;
            SpeedSteps = trainControl.SpeedSteps;

            SelectedLocoSeries = trainControl.SelectedLocoSeries;
            SelectedVmax = trainControl.SelectedVmax;

            TryRestoreSelectedLocomotiveFromProject();
            NotifyAllFunctionGlyphsChanged();
        }
        finally
        {
            _isRestoringLocomotive = false;
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
    /// Saves train control settings to persistent storage.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = _settingsService.GetSettings();

            settings.TrainControl.SpeedRampStepSize = (int)RampStepSize;
            settings.TrainControl.SpeedRampIntervalMs = (int)RampIntervalMs;
            settings.TrainControl.SpeedSteps = SpeedSteps;
            if (SelectedLocomotiveFromProject?.Model != null)
            {
                GetHostTrainControlSettings(settings).SelectedLocomotiveFromProjectId =
                    SelectedLocomotiveFromProject.Model.Id;
            }

            await _settingsService.SaveSettingsAsync(settings).ConfigureAwait(false);

            _logger?.LogInformation(
                "Saved train control settings for host {Host}: LocomotiveId={LocoId}",
                _trainControlHost,
                GetHostTrainControlSettings(settings).SelectedLocomotiveFromProjectId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save train control settings");
        }
    }

    /// <summary>
    /// Saves train control settings to persistent storage.
    /// </summary>
    private async Task SaveTrainControlSettingsAsync()
    {
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the locomotive for the current LocoAddress from the selected project or synced fleet snapshot.
    /// </summary>
    private Locomotive? GetCurrentLocomotive()
    {
        var project = _projectContext?.SelectedProject?.Model;
        if (project?.Locomotives != null)
        {
            var fromProject = project.Locomotives.FirstOrDefault(
                l => l.DigitalAddress.HasValue && l.DigitalAddress.Value == LocoAddress);
            if (fromProject != null)
            {
                return fromProject;
            }
        }

        if (SelectedLocomotiveFromProject?.Model.DigitalAddress == (uint)LocoAddress)
        {
            return SelectedLocomotiveFromProject.Model;
        }

        return ProjectLocomotives.FirstOrDefault(l => l.Model.DigitalAddress == (uint)LocoAddress)?.Model;
    }

    /// <summary>
    /// PNG asset filename for the function button with index 0–31. Uses Domain.Locomotive.FunctionSymbols when set
    /// (and pointing to a .png asset), otherwise the default asset. Returns empty string when no symbol is configured.
    /// Legacy Unicode-codepoint values from earlier versions are ignored.
    /// </summary>
    private string GetFunctionGlyph(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
        {
            return string.Empty;
        }

        return LocomotiveFunctionAppearanceResolver.GetGlyph(GetCurrentLocomotive(), functionIndex);
    }

    /// <summary>
    /// Returns true when the stored value is a non-empty PNG asset filename.
    /// Filters out Segoe MDL2 codepoint strings from earlier versions.
    /// </summary>
    private static bool IsValidAssetReference(string? value) =>
        LocomotiveFunctionAppearanceResolver.IsValidAssetReference(value);

    private string GetFunctionColor(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
        {
            return SignalGrayHex;
        }

        return LocomotiveFunctionAppearanceResolver.GetColor(GetCurrentLocomotive(), functionIndex);
    }

    private static bool IsValidHexColor(string? value) =>
        LocomotiveFunctionAppearanceResolver.IsValidHexColor(value);

    /// <summary>
    /// Sets the PNG asset filename for the specified function (0–31) for the current locomotive (LocoAddress)
    /// and saves the Solution. Only effective when a locomotive with this digital address exists in the selected project.
    /// </summary>
    /// <param name="functionIndex">Funktionsindex 0–31 (F0–F31).</param>
    /// <param name="glyph">PNG asset filename relative to Assets/FunctionSymbols/ (e.g. "headlight.png").</param>
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
        QueueBackgroundTask(_projectContext?.SaveSolutionInternalAsync(), "Auto-save solution");
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
        QueueBackgroundTask(_projectContext?.SaveSolutionInternalAsync(), "Auto-save solution");
        return true;
    }

    public bool ClearFunctionAppearance(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
            return false;

        var loco = GetCurrentLocomotive();
        if (loco == null)
            return false;

        loco.FunctionSymbols ??= new List<string>();
        while (loco.FunctionSymbols.Count <= functionIndex)
            loco.FunctionSymbols.Add(string.Empty);
        loco.FunctionSymbols[functionIndex] = "none";

        loco.FunctionColors ??= new List<string>();
        while (loco.FunctionColors.Count <= functionIndex)
            loco.FunctionColors.Add(string.Empty);
        loco.FunctionColors[functionIndex] = "none";

        NotifyAllFunctionAppearanceChanged();
        QueueBackgroundTask(_projectContext?.SaveSolutionInternalAsync(), "Auto-save solution");
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
            var glyph = GetFunctionGlyph(i);
            if (!string.Equals(Functions[i].IconAsset, glyph, StringComparison.Ordinal))
            {
                Functions[i].IconAsset = glyph;
            }

            var color = GetFunctionColor(i);
            if (!string.Equals(Functions[i].BacklightColorHex, color, StringComparison.Ordinal))
            {
                Functions[i].BacklightColorHex = color;
            }

            var description = GetFunctionDescription(i);
            if (!string.Equals(Functions[i].Description, description, StringComparison.Ordinal))
            {
                Functions[i].Description = description;
            }
        }
    }

    private string GetFunctionDescription(int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
        {
            return string.Empty;
        }

        return LocomotiveFunctionAppearanceResolver.GetDescription(GetCurrentLocomotive(), functionIndex);
    }

    private bool UsesLocalZ21LocomotiveFeedback =>
        !_hybridRuntimeSnapshots
        || (_mobileRuntimeCoordinator?.IsLocalZ21Connected ?? IsConnected);

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        if (_disposed || _updatesPaused)
        {
            return;
        }

        if (_hybridRuntimeSnapshots && _mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
        {
            // Slim remote sync: locomotive state comes from local Z21 feedback when connected.
            if (UsesLocalZ21LocomotiveFeedback)
            {
                ApplyLocalLocomotiveStateFromSnapshot(e.Snapshot);
            }

            return;
        }

        if (!_hybridRuntimeSnapshots && _useRemoteRuntimeSnapshots)
        {
            return;
        }

        ApplyRuntimeSnapshot(e.Snapshot);
    }

    private void OnRemoteRuntimeSnapshotChanged(RemoteRuntimeSnapshotChangedEvent e)
    {
        if (_disposed || _updatesPaused)
        {
            return;
        }

        if (_hybridRuntimeSnapshots && _mobileRuntimeCoordinator?.PreferRemoteRuntime != true)
        {
            return;
        }

        if (!_hybridRuntimeSnapshots && !_useRemoteRuntimeSnapshots)
        {
            return;
        }

        _lastRemoteRuntimeSnapshot = e.Snapshot;
        var snapshotToApply = _hybridRuntimeSnapshots && UsesLocalZ21LocomotiveFeedback
            ? RuntimeSnapshotRemoteFilter.ForMobasmartBroadcast(e.Snapshot)
            : e.Snapshot;
        ApplyRuntimeSnapshot(snapshotToApply);
    }

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var projection = RuntimeSnapshotProjector.ProjectTrainControl(snapshot, IsConnected, LocoAddress);
        IsConnected = projection.IsConnected;
        if (projection.ConnectionChanged)
        {
            OnZ21ConnectionChanged(projection.IsConnected);
        }

        ApplySystemStateFromRuntime(snapshot);

        ApplyLocomotiveStatesFromSnapshot(snapshot);

        if (projection.LocomotiveState != null)
        {
            ApplyLocomotiveState(projection.LocomotiveState);
        }

        if (snapshot.LocomotiveFleet.Count > 0)
        {
            RefreshLocomotiveList(snapshot.LocomotiveFleet);
        }
    }

    private static LocomotiveViewModel CreateLocomotiveViewModelFromFleetSnapshot(LocomotiveFleetSnapshot snapshot)
    {
        return new LocomotiveViewModel(new Locomotive
        {
            Id = snapshot.LocomotiveId,
            Name = snapshot.Name,
            DigitalAddress = snapshot.DigitalAddress,
            PhotoPath = snapshot.PhotoPath,
            FunctionSymbols = snapshot.FunctionSymbols?.ToList(),
            FunctionColors = snapshot.FunctionColors?.ToList(),
            FunctionLabels = snapshot.FunctionLabels?.ToList()
        });
    }

    private void OnZ21ConnectionChanged(bool isConnected)
    {
        RefreshLocomotiveCommandCanExecute();
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
            if (ShouldApplySnapshotDriveState(locomotiveState.Address))
            {
                Speed = locomotiveState.Speed;
                _previousSpeed = locomotiveState.Speed;
                IsForward = locomotiveState.IsForward;
            }

            if (ShouldApplySnapshotFunctionBits())
            {
                ApplyFunctionBitsFromSnapshot(locomotiveState.Functions, locomotiveState.Address);
            }

            // MOBAsmart keeps StatusMessage user-driven; snapshot churn would re-layout all function rows.
            if (!_hybridRuntimeSnapshots)
            {
                StatusMessage = $"Loco {locomotiveState.Address}: {locomotiveState.Speed} {(locomotiveState.IsForward ? "FWD" : "REV")}";
            }
        }
        finally
        {
            _skipSpeedChangeHandler = false;
            _isApplyingRuntimeLocomotiveState = false;
        }
    }

    /// <summary>
    /// Called when LocoAddress changes - request current state from runtime.
    /// </summary>
    partial void OnLocoAddressChanged(int value)
    {
        if (SelectedLocomotiveFromProject == null)
        {
            OnPropertyChanged(nameof(LocomotiveTitle));
        }

        if (!_isRestoringLocomotive && !_isApplyingRuntimeLocomotiveState && _previousLocoAddressForFunctionCache != value)
        {
            SaveFunctionStatesToCache(_previousLocoAddressForFunctionCache);
            CancelAllFunctionsOffOperation();
            _suppressSnapshotFunctionState = false;
            RestoreFunctionStatesForAddress(value);
        }

        _previousLocoAddressForFunctionCache = value;

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
    /// Called when Speed changes - send to Z21.
    /// Speed increase prevented when brake applied or door release active (doors open).
    /// </summary>
    partial void OnSpeedChanged(int value)
    {
        if (_skipSpeedChangeHandler || _isRestoringLocomotive || _isApplyingRuntimeLocomotiveState) return;

        if (!CanExecuteLocomotiveControl && value > _previousSpeed)
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

        if (CanExecuteLocoCommand() && LocoAddress >= 1)
        {
            QueueSendDriveCommandDebounced();
        }
    }

    private void QueueSendDriveCommandDebounced()
    {
        if (_disposed)
        {
            return;
        }

        _sendDriveCommandDebounceCts?.Cancel();
        _sendDriveCommandDebounceCts?.Dispose();
        _sendDriveCommandDebounceCts = new CancellationTokenSource();
        var token = _sendDriveCommandDebounceCts.Token;
        _ = DebouncedSendDriveCommandAsync(token);
    }

    private async Task DebouncedSendDriveCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SendDriveCommandDebounceMs, cancellationToken).ConfigureAwait(false);
            QueueBackgroundTask(SendDriveCommandAsync(), "Send drive command");
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer speed change.
        }
    }

    /// <summary>
    /// Called when IsForward changes - ramp down to 0, then ramp up in new direction.
    /// This prevents derailment from sudden direction changes at speed.
    /// </summary>
    partial void OnIsForwardChanged(bool value)
    {
        OnPropertyChanged(nameof(DirectionStatusText));

        if (_isRestoringLocomotive || _isApplyingRuntimeLocomotiveState) return;

        if (CanExecuteLocoCommand() && LocoAddress >= 1)
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
            await SendLocomotiveDriveAsync(LocoAddress, 0, newDirection);

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
            await SendLocomotiveDriveAsync(LocoAddress, currentSpeed, direction);

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
    /// Requires active MOBAflow session or local Z21 connection and valid DCC address (1-9999).
    /// </summary>
    private bool CanExecuteLocomotiveControl =>
        LocoAddress >= 1
        && LocoAddress <= 9999
        && (IsConnected || (_mobileRuntimeCoordinator?.CanExecuteCommands ?? false));

    /// <summary>
    /// Refreshes locomotive command CanExecute state after MOBAsmart routing changes.
    /// </summary>
    public void RefreshLocomotiveCommandCanExecute()
    {
        SetSpeedCommand.NotifyCanExecuteChanged();
        ToggleFunctionCommand.NotifyCanExecuteChanged();
        TurnOffAllFunctionsCommand.NotifyCanExecuteChanged();
        EmergencyStopCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsSpeedControlEnabled));
    }

    private void OnRuntimeCommandAvailabilityChanged(RuntimeCommandAvailabilityChangedEvent e)
    {
        _ = e;
        RefreshLocomotiveCommandCanExecute();
    }

    private bool CanExecuteLocoCommand() => CanExecuteLocomotiveControl;

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
            await SendLocomotiveDriveAsync(LocoAddress, Speed, IsForward);
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
    [RelayCommand]
    private Task ToggleFunction(int index) => ToggleFunctionAsync(index);

    /// <summary>
    /// Opens the function appearance picker (WinUI) and persists symbol/color for the current locomotive.
    /// </summary>
    [RelayCommand]
    private async Task EditFunctionAppearanceAsync(int functionIndex)
    {
        if (_functionAppearancePicker is null || functionIndex is < 0 or > 31)
        {
            return;
        }

        try
        {
            var initialColor = Functions[functionIndex].BacklightColorHex;
            var result = await _functionAppearancePicker
                .PickAsync(new FunctionAppearancePickerRequest(initialColor))
                .ConfigureAwait(true);

            if (result is null || !result.IsConfirmed)
            {
                return;
            }

            var applied = result.IsSelectionCleared
                ? ClearFunctionAppearance(functionIndex)
                : result.Glyph != null || result.ColorHex != null
                    ? SetFunctionAppearance(functionIndex, result.Glyph, result.ColorHex)
                    : true;

            if (!applied)
            {
                StatusMessage =
                    $"No locomotive with address {LocoAddress} in the project. Please create one with this digital address first.";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Function symbol selection failed for F{FunctionIndex}", functionIndex);
        }
    }

    /// <summary>
    /// Generic function toggle implementation used by <see cref="ToggleFunctionCommand"/> and unit tests.
    /// </summary>
    public async Task ToggleFunctionAsync(int functionNumber)
    {
        try
        {
            InvalidatePendingFunctionUiResets();

            var newState = !GetFunctionState(functionNumber);
            SetFunctionState(functionNumber, newState);
            MarkLocalFunctionCommand(LocoAddress, functionNumber);
            SaveFunctionStatesToCache(LocoAddress);

            await _runtimeCommandGateway.SetLocomotiveFunctionAsync(LocoAddress, functionNumber, newState);
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
    /// bits are suppressed until the reset completes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private Task TurnOffAllFunctions() => TurnOffAllFunctionsAsync();

    public async Task TurnOffAllFunctionsAsync(bool resetUi = true)
    {
        var uiResetVersion = _functionControlVersion;
        CancelAllFunctionsOffOperation();
        _allFunctionsOffCts = new CancellationTokenSource();
        var token = _allFunctionsOffCts.Token;

        // Suppress decoder function bits before touching UI so snapshots cannot re-enable keys mid-reset.
        _suppressSnapshotFunctionState = true;

        try
        {
            if (resetUi)
            {
                ResetFunctionUiStates(uiResetVersion, token);
                SaveFunctionStatesToCache(LocoAddress);
            }

            if (!CanExecuteLocomotiveControl || LocoAddress < 1)
            {
                return;
            }

            await SendAllFunctionsOffAsync(token).ConfigureAwait(false);
            StatusMessage = $"Loco {LocoAddress}: all functions OFF";
            _logger?.LogDebug("All functions turned off for loco {Address}", LocoAddress);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("All-functions-off cancelled for loco {Address}", LocoAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to turn off all functions for loco {Address}", LocoAddress);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _suppressSnapshotFunctionState = false;
        }
    }

    private bool ShouldApplySnapshotDriveState(int address)
    {
        if (!_hybridRuntimeSnapshots)
        {
            return true;
        }

        // Slim remote sync: prefer local Z21 locomotive feedback when directly connected.
        if (UsesLocalZ21LocomotiveFeedback)
        {
            return !ShouldPreserveLocalDriveCommand(address);
        }

        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime != true)
        {
            return false;
        }

        return !ShouldPreserveLocalDriveCommand(address);
    }

    private void ApplyLocalLocomotiveStateFromSnapshot(MobaRuntimeSnapshot snapshot)
    {
        var projection = RuntimeSnapshotProjector.ProjectTrainControl(snapshot, IsConnected, LocoAddress);
        ApplyLocomotiveStatesFromSnapshot(snapshot);

        if (projection.LocomotiveState != null)
        {
            ApplyLocomotiveState(projection.LocomotiveState);
        }
    }

    private bool ShouldApplySnapshotFunctionBits()
    {
        if (_suppressSnapshotFunctionState)
        {
            return false;
        }

        if (_useRemoteRuntimeSnapshots && !_hybridRuntimeSnapshots)
        {
            return false;
        }

        return true;
    }

    private void InvalidatePendingFunctionUiResets()
    {
        _functionControlVersion++;
        CancelAllFunctionsOffOperation();
    }

    private void CancelAllFunctionsOffOperation()
    {
        _allFunctionsOffCts?.Cancel();
        _allFunctionsOffCts?.Dispose();
        _allFunctionsOffCts = null;
    }

    private void ResetFunctionUiStates(int uiResetVersion, CancellationToken token)
    {
        void ApplyReset()
        {
            for (int i = 0; i < Functions.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                if (uiResetVersion != _functionControlVersion)
                {
                    return;
                }

                if (Functions[i].IsOn)
                {
                    Functions[i].IsOn = false;
                }
            }
        }

        if (_uiDispatcher != null)
        {
            _uiDispatcher.InvokeOnUi(ApplyReset);
        }
        else
        {
            ApplyReset();
        }
    }

    private async Task SendAllFunctionsOffAsync(CancellationToken cancellationToken)
    {
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
        {
            for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _runtimeCommandGateway
                    .SetLocomotiveFunctionAsync(LocoAddress, functionIndex, false, cancellationToken)
                    .ConfigureAwait(false);
            }

            return;
        }

        await _mobaRuntime.SetAllLocomotiveFunctionsOffAsync(LocoAddress, cancellationToken).ConfigureAwait(false);
    }

    private bool GetFunctionState(int functionNumber) =>
        functionNumber >= 0 && functionNumber < Functions.Count && Functions[functionNumber].IsOn;

    private void SetFunctionState(int functionNumber, bool state)
    {
        if (functionNumber >= 0
            && functionNumber < Functions.Count
            && Functions[functionNumber].IsOn != state)
        {
            Functions[functionNumber].IsOn = state;
        }
    }

    private void ApplyFunctionBits(uint functions)
    {
        for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
        {
            SetFunctionState(functionIndex, (functions & (1u << functionIndex)) != 0);
        }
    }

    private void ApplyLocomotiveStatesFromSnapshot(MobaRuntimeSnapshot snapshot)
    {
        foreach (var (address, locomotiveState) in snapshot.LocomotiveStates)
        {
            if (address == LocoAddress)
            {
                continue;
            }

            _locomotiveFunctionStateCache[address] = locomotiveState.Functions;
        }
    }

    private void ApplyFunctionBitsFromSnapshot(uint snapshotFunctions, int address)
    {
        if (address != LocoAddress)
        {
            _locomotiveFunctionStateCache[address] = snapshotFunctions;
            return;
        }

        // Z21 LAN_X_LOCO_INFO broadcasts after drive commands can carry stale or decoder-specific
        // function bits; keep the UI stable while local throttle/direction changes are in flight.
        if (ShouldPreserveLocalDriveCommand(address))
        {
            return;
        }

        for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
        {
            if (ShouldPreserveLocalFunctionCommand(address, functionIndex))
            {
                continue;
            }

            SetFunctionState(functionIndex, (snapshotFunctions & (1u << functionIndex)) != 0);
        }

        _locomotiveFunctionStateCache[address] = GetFunctionBitmask();
    }

    private uint GetFunctionBitmask()
    {
        uint mask = 0;
        for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
        {
            if (GetFunctionState(functionIndex))
            {
                mask |= 1u << functionIndex;
            }
        }

        return mask;
    }

    private void SaveFunctionStatesToCache(int address)
    {
        if (address < 1)
        {
            return;
        }

        _locomotiveFunctionStateCache[address] = GetFunctionBitmask();
    }

    private void RestoreFunctionStatesForAddress(int address)
    {
        if (address < 1)
        {
            ApplyFunctionBits(0);
            return;
        }

        var snapshot = GetActiveRuntimeSnapshot();
        if (snapshot.LocomotiveStates.TryGetValue(address, out var runtimeState))
        {
            ApplyFunctionBits(runtimeState.Functions);
            _locomotiveFunctionStateCache[address] = runtimeState.Functions;
            return;
        }

        if (_locomotiveFunctionStateCache.TryGetValue(address, out var cached))
        {
            ApplyFunctionBits(cached);
            return;
        }

        ApplyFunctionBits(0);
    }

    private MobaRuntimeSnapshot GetActiveRuntimeSnapshot()
    {
        if (_hybridRuntimeSnapshots && UsesLocalZ21LocomotiveFeedback)
        {
            return _mobaRuntime.Current;
        }

        if (_hybridRuntimeSnapshots
            && _mobileRuntimeCoordinator?.PreferRemoteRuntime == true
            && _lastRemoteRuntimeSnapshot != null)
        {
            return _lastRemoteRuntimeSnapshot;
        }

        return _mobaRuntime.Current;
    }

    private void MarkLocalFunctionCommand(int address, int functionIndex)
    {
        if (address < 1 || functionIndex is < 0 or > 31)
        {
            return;
        }

        if (!_lastLocalFunctionCommandAt.TryGetValue(address, out var perFunction))
        {
            perFunction = [];
            _lastLocalFunctionCommandAt[address] = perFunction;
        }

        perFunction[functionIndex] = DateTimeOffset.UtcNow;
    }

    private async Task SendLocomotiveDriveAsync(int address, int speed, bool forward)
    {
        MarkLocalDriveCommand(address);
        await _runtimeCommandGateway.SetLocomotiveDriveAsync(address, speed, forward);
    }

    private void MarkLocalDriveCommand(int address)
    {
        if (address < 1)
        {
            return;
        }

        _lastLocalDriveCommandAt[address] = DateTimeOffset.UtcNow;
    }

    private bool ShouldPreserveLocalDriveCommand(int address)
    {
        if (address < 1)
        {
            return false;
        }

        if (!_lastLocalDriveCommandAt.TryGetValue(address, out var commandedAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - commandedAt.ToUniversalTime() > DriveCommandGracePeriod)
        {
            _lastLocalDriveCommandAt.Remove(address);
            return false;
        }

        return true;
    }

    private bool ShouldPreserveLocalFunctionCommand(int address, int functionIndex)
    {
        if (functionIndex is < 0 or > 31)
        {
            return false;
        }

        if (!_lastLocalFunctionCommandAt.TryGetValue(address, out var perFunction)
            || !perFunction.TryGetValue(functionIndex, out var commandedAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - commandedAt.ToUniversalTime() > FunctionCommandGracePeriod)
        {
            perFunction.Remove(functionIndex);
            return false;
        }

        return true;
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
            await SendLocomotiveDriveAsync(LocoAddress, 0, IsForward);
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
    /// Sets direction explicitly (forward or reverse) without toggling when already selected.
    /// </summary>
    [RelayCommand]
    private void SetDirection(object? parameter)
    {
        if (!TryParseDirection(parameter, out var forward) || IsForward == forward)
        {
            return;
        }

        IsForward = forward;
    }

    private static bool TryParseDirection(object? parameter, out bool forward)
    {
        switch (parameter)
        {
            case bool isForward:
                forward = isForward;
                return true;
            case string text when bool.TryParse(text, out var parsed):
                forward = parsed;
                return true;
            default:
                forward = false;
                return false;
        }
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
            await SendLocomotiveDriveAsync(LocoAddress, 0, IsForward);
            StatusMessage = $"Loco {LocoAddress} stopped";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop locomotive");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Sets speed to a preset km/h value (converted to the nearest DCC speed step).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
    private void SetSpeedPreset(object? presetParam)
    {
        var presetKmh = presetParam switch
        {
            int i => i,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => 0
        };

        var clampedKmh = Math.Clamp(presetKmh, 0, SpeedGaugeMaxKmh);
        var targetStep = TrainControlDccSpeed.KmhToSpeedStep(clampedKmh, SpeedGaugeMaxKmh, MaxSpeedStep);
        var maxSpeed = CanIncreaseSpeed ? MaxSpeedStep : Speed;
        Speed = Math.Clamp(targetStep, 0, maxSpeed);
        _logger?.LogDebug("Speed preset set to {PresetKmh} km/h (step {Step})", clampedKmh, Speed);
    }

    /// <summary>
    /// Selects a locomotive from the synced MOBAflow project.
    /// </summary>
    [RelayCommand]
    private void SelectProjectLocomotive(LocomotiveViewModel? locomotive)
    {
        if (locomotive == null)
        {
            return;
        }

        SaveFunctionStatesToCache(LocoAddress);
        if (!ReferenceEquals(SelectedLocomotiveFromProject, locomotive))
        {
            SelectedLocomotiveFromProject = locomotive;
        }
        else
        {
            PersistHostLocomotiveSelection(locomotive);
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

    // === Runtime Projection ===

    private void ApplySystemStateFromRuntime(MobaRuntimeSnapshot snapshot)
    {
        if (_updatesPaused)
        {
            return;
        }

        if (MainTrackCurrent == snapshot.MainCurrent
            && ProgTrackCurrent == snapshot.ProgCurrent
            && FilteredMainCurrent == snapshot.FilteredMainCurrent
            && SupplyVoltage == snapshot.SupplyVoltage
            && Temperature == snapshot.Temperature)
        {
            return;
        }

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
        if (task == null || _disposed)
        {
            return;
        }

        task.Observe(ex => _logger?.LogWarning(ex, "{OperationName} failed", operationName));
    }
}
