// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Model;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using Microsoft.Extensions.Logging;

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
public partial class TrainControlViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrainControlViewModel>? _logger;

    private bool _isLoadingPreset;

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
    [NotifyCanExecuteChangedFor(nameof(ToggleF9Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF10Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF11Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF12Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF13Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF14Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF15Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF16Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF17Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF18Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF19Command))]
    [NotifyCanExecuteChangedFor(nameof(ToggleF20Command))]
    [NotifyCanExecuteChangedFor(nameof(EmergencyStopCommand))]
    private int locoAddress = 3;

    // === Locomotive Presets ===

    /// <summary>
    /// Currently selected preset index (0, 1, or 2).
    /// </summary>
    [ObservableProperty]
    private int selectedPresetIndex;

    /// <summary>
    /// First locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset preset1 = new() { Name = "Lok 1", DccAddress = 3 };

    /// <summary>
    /// Second locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset preset2 = new() { Name = "Lok 2", DccAddress = 4 };

    /// <summary>
    /// Third locomotive preset.
    /// </summary>
    [ObservableProperty]
    private LocomotivePreset preset3 = new() { Name = "Lok 3", DccAddress = 5 };

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
    [NotifyPropertyChangedFor(nameof(ApproximateKmh))]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    private int speed;

    /// <summary>
    /// Approximate km/h based on speed step (assuming 200 km/h max for typical model trains).
    /// </summary>
    public int ApproximateKmh => (int)Math.Round(Speed * 1.6); // ~200 km/h at max speed (126)

    // === Locomotive Series (Baureihe) for Vmax calculation ===

    /// <summary>
    /// Selected locomotive series name (e.g., "BR 103", "ICE 3").
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    [NotifyPropertyChangedFor(nameof(HasValidLocoSeries))]
    private string selectedLocoSeries = string.Empty;

    /// <summary>
    /// Maximum speed (Vmax) of the selected locomotive series in km/h.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedKmh))]
    [NotifyPropertyChangedFor(nameof(HasValidLocoSeries))]
    private int selectedVmax;

    /// <summary>
    /// Indicates whether a valid locomotive series with Vmax is selected.
    /// </summary>
    public bool HasValidLocoSeries => SelectedVmax > 0;

    /// <summary>
    /// Calculated speed in km/h based on current speed step and selected Vmax.
    /// Returns 0 if no valid locomotive series is selected.
    /// </summary>
    public int SpeedKmh => HasValidLocoSeries
        ? (int)Math.Round(Speed / 126.0 * SelectedVmax)
        : 0;

    /// <summary>
    /// Direction: true = forward, false = backward.
    /// </summary>
    [ObservableProperty]
    private bool isForward = true;

    /// <summary>
    /// Function states F0-F20. Array index corresponds to function number.
    /// </summary>
    [ObservableProperty]
    private bool isF0On;

    [ObservableProperty]
    private bool isF1On;

    [ObservableProperty]
    private bool isF2On;

    [ObservableProperty]
    private bool isF3On;

    [ObservableProperty]
    private bool isF4On;

    [ObservableProperty]
    private bool isF5On;

    [ObservableProperty]
    private bool isF6On;

    [ObservableProperty]
    private bool isF7On;

    [ObservableProperty]
    private bool isF8On;

    [ObservableProperty]
    private bool isF9On;

    [ObservableProperty]
    private bool isF10On;

    [ObservableProperty]
    private bool isF11On;

    [ObservableProperty]
    private bool isF12On;

    [ObservableProperty]
    private bool isF13On;

    [ObservableProperty]
    private bool isF14On;

    [ObservableProperty]
    private bool isF15On;

    [ObservableProperty]
    private bool isF16On;

    [ObservableProperty]
    private bool isF17On;

    [ObservableProperty]
    private bool isF18On;

    [ObservableProperty]
    private bool isF19On;

    [ObservableProperty]
    private bool isF20On;

    /// <summary>
    /// Status message for UI feedback.
    /// </summary>
    [ObservableProperty]
    private string statusMessage = "Ready";

    /// <summary>
    /// Indicates if Z21 is connected.
    /// </summary>
    public bool IsConnected => _z21.IsConnected;

    // === Speed Ramp Configuration ===

    /// <summary>
    /// Enable/disable gradual acceleration when changing direction or starting.
    /// When enabled, speed changes happen gradually instead of instantly.
    /// </summary>
    [ObservableProperty]
    private bool isRampEnabled = true;

    /// <summary>
    /// Speed step increment per ramp interval (1-20).
    /// Lower values = smoother but slower acceleration.
    /// Default: 5 (moderate acceleration).
    /// </summary>
    [ObservableProperty]
    private double rampStepSize = 5;

    /// <summary>
    /// Delay between speed steps in milliseconds (50-500).
    /// Lower values = faster acceleration.
    /// Default: 100ms.
    /// </summary>
    [ObservableProperty]
    private double rampIntervalMs = 100;

    /// <summary>
    /// Indicates if a ramp operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    private bool isRamping;

    private CancellationTokenSource? _rampCancellationTokenSource;

    public TrainControlViewModel(
        IZ21 z21,
        IUiDispatcher uiDispatcher,
        ISettingsService settingsService,
        ILogger<TrainControlViewModel>? logger = null)
    {
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settingsService = settingsService;
        _logger = logger;

        // Load presets from settings
        LoadPresetsFromSettings();

        // Subscribe to connection changes
        _z21.OnConnectedChanged += OnZ21ConnectionChanged;

        // Subscribe to loco info updates
        _z21.OnLocoInfoChanged += OnLocoInfoReceived;
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

        // Apply current preset
        ApplyCurrentPreset();
    }

    /// <summary>
    /// Saves locomotive presets to persistent settings.
    /// </summary>
    private async Task SavePresetsToSettingsAsync()
    {
        var settings = _settingsService.GetSettings();
        settings.TrainControl.Presets =
        [
            Preset1,
            Preset2,
            Preset3
        ];
        settings.TrainControl.SelectedPresetIndex = SelectedPresetIndex;
        settings.TrainControl.SpeedRampStepSize = (int)RampStepSize;
        settings.TrainControl.SpeedRampIntervalMs = (int)RampIntervalMs;

        await _settingsService.SaveSettingsAsync(settings);
        _logger?.LogDebug("Locomotive presets saved to settings");
    }

    /// <summary>
    /// Applies the current preset to the ViewModel state.
    /// </summary>
    private void ApplyCurrentPreset()
    {
        _isLoadingPreset = true;
        try
        {
            var preset = CurrentPreset;
            LocoAddress = preset.DccAddress;
            Speed = preset.Speed;
            IsForward = preset.IsForward;

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
    /// </summary>
    private void SaveCurrentStateToPreset()
    {
        if (_isLoadingPreset) return;

        var preset = CurrentPreset;
        preset.DccAddress = LocoAddress;
        preset.Speed = Speed;
        preset.IsForward = IsForward;

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
        // Save current preset before switching - if not loading
        if (!_isLoadingPreset)
        {
            // Get the OLD preset and save state to it
            var oldPreset = value switch
            {
                0 => Preset2, // was 1, now 0
                1 => SelectedPresetIndex == 0 ? Preset1 : Preset3,
                2 => Preset2, // was 1, now 2
                _ => Preset1
            };
            // Actually, we should save to the PREVIOUS preset, but we don't have it here
            // So we save after applying the new preset
        }

        ApplyCurrentPreset();
    }

    private void OnZ21ConnectionChanged(bool isConnected)
    {
        _uiDispatcher.InvokeOnUi(() =>
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
        });
    }

    private void OnLocoInfoReceived(LocoInfo info)
    {
        // Only update if this is our current loco
        if (info.Address != LocoAddress) return;

        _uiDispatcher.InvokeOnUi(() =>
        {
            // Update local state from Z21 response
            Speed = info.Speed;
            IsForward = info.IsForward;
            IsF0On = info.IsF0On;
            IsF1On = info.IsF1On;
            StatusMessage = $"Loco {info.Address}: {info.Speed} km/h {(info.IsForward ? "â†’" : "â†")}";
        });
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

        if (_z21.IsConnected && LocoAddress >= 1)
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
    /// Locomotive command execution check.
    /// TEMP: Disabled Z21 connection check for UI testing (2026-01-16).
    /// Commands will be attempted even without Z21 hardware, but will fail gracefully.
    /// </summary>
    /// <remarks>
    /// TODO: Re-enable Z21 connection check when hardware is available.
    /// To restore: Uncomment the line below and delete the "=> true" line.
    /// This was temporarily disabled to test function button UI without Z21 connected.
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
}
